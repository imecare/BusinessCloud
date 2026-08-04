using BusinessCloud.Application.Bazares.Commands.CommitBzaCustomersImport;
using BusinessCloud.Application.Bazares.Queries.ValidateBzaCustomersImport;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class CustomerImportTests
{
    [Fact]
    public async Task Validate_TwoColumnWorkbook_NormalizesDeduplicatesAndReportsCollectorConflict()
    {
        await using var context = BazaresContextFactory.Create();
        var group = new BzaCollectorGroup
        {
            Id = 1,
            TenantId = BazaresContextFactory.TenantId,
            Description = "Grupo",
            IsActive = true,
        };
        context.CollectorGroups.Add(group);
        context.Collectors.AddRange(
            new BzaCollector
            {
                Id = 1,
                TenantId = BazaresContextFactory.TenantId,
                Name = "RECOLECTORA",
                BzaCollectorGroupId = 1,
                CollectorGroup = group,
                IsActive = true,
            },
            new BzaCollector
            {
                Id = 2,
                TenantId = BazaresContextFactory.TenantId,
                Name = "OTRA",
                BzaCollectorGroupId = 1,
                CollectorGroup = group,
                IsActive = true,
            });
        await context.SaveChangesAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Datos");
        sheet.Cell(1, 1).Value = "CLIENTA";
        sheet.Cell(1, 2).Value = "RECOLECTORA";
        sheet.Cell(2, 1).Value = "  ANA   LUZ ";
        sheet.Cell(2, 2).Value = "8.- RECOLECTORA";
        sheet.Cell(3, 1).Value = "ana luz";
        sheet.Cell(3, 2).Value = "RECOLECTORA";
        sheet.Cell(4, 1).Value = "CONFLICTO";
        sheet.Cell(4, 2).Value = "RECOLECTORA";
        sheet.Cell(5, 1).Value = " conflicto ";
        sheet.Cell(5, 2).Value = "OTRA";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var handler = new ValidateBzaCustomersImportHandler(context);
        var result = await handler.Handle(
            new ValidateBzaCustomersImportQuery(stream.ToArray()),
            CancellationToken.None);

        Assert.Equal(4, result.TotalRows);
        Assert.Equal(2, result.Customers.Count);
        Assert.Equal(2, result.ExactDuplicateRows);
        Assert.Equal(1, result.CollectorConflictCount);
        var ana = Assert.Single(result.Customers, customer => customer.Name == "ANA LUZ");
        Assert.Equal("RECOLECTORA", ana.CollectorNameFromFile);
        Assert.True(ana.CollectorExists);
        Assert.True(ana.WillBePendingInfo);
        var conflict = Assert.Single(result.Customers, customer => customer.Name == "CONFLICTO");
        Assert.True(conflict.HasCollectorConflict);
        Assert.Equal(["OTRA", "RECOLECTORA"], conflict.CollectorConflictNames);
        Assert.Equal(string.Empty, conflict.CollectorNameFromFile);
    }

    [Fact]
    public async Task Commit_WithoutContact_CreatesPendingCustomerWithRealCollector()
    {
        await using var context = BazaresContextFactory.Create();
        await SeedCollectorAsync(context, "RECOLECTORA", 1, 1);
        var (handler, mongo) = CreateHandler(context);
        var command = new CommitBzaCustomersImportCommand([],
        [
            new CommitImportCustomerDto
            {
                Name = "  ANA   LUZ ",
                CollectorName = "8.- RECOLECTORA",
            },
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, result.CustomersCreated);
        Assert.Equal(1, result.PendingInfoCustomersCreated);
        Assert.Equal(0, result.IgnoredRecords);
        var customer = await context.Customers.SingleAsync();
        Assert.Equal("ANA LUZ", customer.Name);
        Assert.True(customer.IsPendingInfo);
        Assert.True(customer.HasNoWhatsApp);
        Assert.Equal("0000000001", customer.Phone);
        Assert.Equal(1, customer.BzaCollectorId);
        mongo.Verify(service => service.InsertAuditLogAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Commit_RejectsPlaceholderAndAmbiguousCollectors()
    {
        await using var context = BazaresContextFactory.Create();
        await SeedCollectorAsync(context, "SIN ASIGNAR", 1, 1);
        await SeedCollectorAsync(context, "DUPLICADA", 2, 2);
        await SeedCollectorAsync(context, "DUPLICADA", 3, 3);
        var (handler, _) = CreateHandler(context);
        var command = new CommitBzaCustomersImportCommand([],
        [
            new CommitImportCustomerDto { Name = "SIN RECOLECTOR", CollectorName = "SIN ASIGNAR" },
            new CommitImportCustomerDto { Name = "AMBIGUO", CollectorName = "DUPLICADA" },
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, result.CustomersCreated);
        Assert.Equal(2, result.IgnoredRecords);
        Assert.Empty(await context.Customers.ToListAsync());
        Assert.Contains(result.Errors, error => error.Contains("recolector real", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("ambiguo", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task SeedCollectorAsync(
        BazaresDbContext context,
        string name,
        int collectorId,
        int groupId)
    {
        var group = new BzaCollectorGroup
        {
            Id = groupId,
            TenantId = BazaresContextFactory.TenantId,
            Description = $"Grupo {groupId}",
            IsActive = true,
        };
        context.CollectorGroups.Add(group);
        context.Collectors.Add(new BzaCollector
        {
            Id = collectorId,
            TenantId = BazaresContextFactory.TenantId,
            Name = name,
            BzaCollectorGroupId = groupId,
            CollectorGroup = group,
            IsActive = true,
        });
        await context.SaveChangesAsync();
    }

    private static (CommitBzaCustomersImportHandler Handler, Mock<IMongoContext> Mongo) CreateHandler(
        BazaresDbContext context)
    {
        var mongo = new Mock<IMongoContext>();
        mongo.Setup(service => service.InsertAuditLogAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.TenantId).Returns(BazaresContextFactory.TenantId);
        currentUser.Setup(service => service.GetRequiredTenantId()).Returns(BazaresContextFactory.TenantId);
        return (new CommitBzaCustomersImportHandler(context, mongo.Object, currentUser.Object), mongo);
    }
}
