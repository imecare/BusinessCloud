using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Queries.GetCustomerDebtsExcel;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Migrations.PaymentsDb;
using BusinessCloud.Tests.Application.Payments;
using BusinessCloud.Tests.TestSupport;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Infrastructure;

public sealed class LocalCashSqlFactAttribute : FactAttribute
{
    public LocalCashSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("PAYMENTS_LOCAL_SQL_TESTS") != "1")
            Skip = "Opt-in: PAYMENTS_LOCAL_SQL_TESTS=1. Creates and deletes isolated PaymentsCashTests_* LocalDB databases.";
    }
}

public class CashSaleSqlTests
{
    private static PaymentsDbContext Context(string connection, bool failAfterSave = false)
    {
        var user = new Mock<ICurrentUserService>();
        user.SetupGet(u => u.TenantId).Returns(PaymentsContextFactory.TenantId);
        user.Setup(u => u.GetRequiredTenantId()).Returns(PaymentsContextFactory.TenantId);
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseSqlServer(connection, sql => sql.EnableRetryOnFailure());
        if (failAfterSave)
            options.AddInterceptors(new FailAfterSaveInterceptor());
        return new PaymentsDbContext(options.Options, user.Object);
    }

    private static async Task RunAsync(Func<string, Task> test, bool previousSchema = false)
    {
        var connection = new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\MSSQLLocalDB",
            InitialCatalog = $"PaymentsCashTests_{Guid.NewGuid():N}",
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;
        await using var db = Context(connection);
        try
        {
            if (previousSchema)
            {
                await db.GetService<IRelationalDatabaseCreator>().CreateAsync();
                var model = db.GetService<IModelRuntimeInitializer>()
                    .Initialize(new AddSaleReservations().TargetModel, designTime: true);
                var operations = db.GetService<IMigrationsModelDiffer>()
                    .GetDifferences(null, model.GetRelationalModel());
                foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(operations, model))
                    await db.Database.ExecuteSqlRawAsync(command.CommandText);
            }
            else
            {
                await db.Database.EnsureCreatedAsync();
            }
            // SQL Server generates seller identities.
            db.Sellers.AddRange(new() { Name = "Ana" }, new() { Name = "Luis" });
            await db.SaveChangesAsync(default);
            await test(connection);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [LocalCashSqlFact]
    public Task Migration_PreservesExistingCreditSaleAndEnablesCash() => RunAsync(async connection =>
    {
        await using var db = Context(connection);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO [Customers] ([Name],[LastName],[RFC],[Phone],[SellerId],[TenantId],[CreatedAt])
            VALUES ('Cliente previo','','','',1,'tenant-test',GETUTCDATE());
            INSERT INTO [Sales] ([CustomerId],[SellerId],[TotalAmount],[CostPrice],[CommissionAmount],
                [ProductDescription],[IsCommissionPaid],[IsPaid],[Date],[TenantId],[CreatedAt])
            VALUES (1,1,1000,700,100,'Venta previa',0,0,'2026-11-01','tenant-test',GETUTCDATE());
            """);
        var model = db.GetService<IDesignTimeModel>().Model;
        var commands = db.GetService<IMigrationsSqlGenerator>().Generate(new AddCashSales().UpOperations, model);
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            foreach (var command in commands)
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            await transaction.CommitAsync();
        });

        var previous = await db.Sales.SingleAsync();
        Assert.False(previous.IsCashSale);
        Assert.False(previous.IsPaid);
        Assert.Equal(1000m, previous.TotalAmount);
        await CreateSaleHandlerTests.Handler(db).Handle(CreateSaleHandlerTests.Cash(), default);
        Assert.Equal(2, await db.Sales.CountAsync());
        Assert.Single(await db.Customers.Where(c => c.IsGeneralPublic && c.SellerId == null).ToListAsync());
        var export = await new GetCustomerDebtsExcelHandler(db).Handle(new GetCustomerDebtsExcelQuery(), default);
        using var workbook = new XLWorkbook(new MemoryStream(export.Content));
        Assert.Equal(2, workbook.Worksheet("Deudas activas").LastRowUsed()!.RowNumber());
        Assert.Equal(1000m, workbook.Worksheet("Deudas activas").Cell(2, 6).GetValue<decimal>());
    }, previousSchema: true);

    [LocalCashSqlFact]
    public Task ConcurrentSales_CreateOnlyOneGeneralCustomer() => RunAsync(async connection =>
    {
        await Task.WhenAll(Enumerable.Range(1, 4).Select(async i =>
        {
            await using var db = Context(connection);
            await CreateSaleHandlerTests.Handler(db).Handle(CreateSaleHandlerTests.Cash($"sale-{i}", sellerId: i % 2 + 1), default);
        }));
        await using var verify = Context(connection);
        Assert.Single(await verify.Customers.ToListAsync());
        Assert.Equal(4, await verify.Sales.CountAsync(s => s.IsCashSale && s.IsPaid));
        Assert.Equal(4800m, await verify.Payments.Where(p => p.PaymentTypeId == 2).SumAsync(p => p.Amount));
    });

    [LocalCashSqlFact]
    public Task ConcurrentRetries_PersistOneSaleAndOnePayment() => RunAsync(async connection =>
    {
        var ids = await Task.WhenAll(Enumerable.Range(1, 4).Select(async _ =>
        {
            await using var db = Context(connection);
            return await CreateSaleHandlerTests.Handler(db).Handle(CreateSaleHandlerTests.Cash(), default);
        }));
        Assert.Single(ids.Distinct());
        await using var verify = Context(connection);
        Assert.Single(await verify.Sales.ToListAsync());
        Assert.Single(await verify.Payments.Where(p => p.PaymentTypeId == 2).ToListAsync());
    });

    [LocalCashSqlFact]
    public Task FailureAfterSave_RollsBackCustomerSaleAndPayment() => RunAsync(async connection =>
    {
        await using (var db = Context(connection, failAfterSave: true))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateSaleHandlerTests.Handler(db).Handle(CreateSaleHandlerTests.Cash(), default));
        }
        await using var verify = Context(connection);
        Assert.Empty(await verify.Customers.ToListAsync());
        Assert.Empty(await verify.Sales.ToListAsync());
        Assert.Empty(await verify.Payments.ToListAsync());
    });

    private sealed class FailAfterSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated failure after SQL writes, before commit.");
    }
}
