using BusinessCloud.Application.Payments.Queries.GetCustomerDebtsExcel;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Tests.TestSupport;
using ClosedXML.Excel;
using Xunit;

namespace BusinessCloud.Tests.Application.Payments;

public class GetCustomerDebtsExcelHandlerTests
{
    [Fact]
    public async Task Excel_IncludesOnlyActivePurchasesAndTheirPaymentsWithMatchingTotals()
    {
        var database = $"debts-excel-{Guid.NewGuid():N}";
        await using var db = PaymentsContextFactory.Create(databaseName: database);
        db.Sellers.Add(new Seller { Id = 1, Name = "Ana", LastName = "Perez" });
        db.Customers.AddRange(
            new Customer { Id = 1, Name = "=1+1", LastName = "Cliente", SellerId = 1 },
            new Customer { Id = 2, Name = "Sin abonos", SellerId = 1 });
        db.Sales.AddRange(
            new Sale { Id = 1, CustomerId = 1, SellerId = 1, TotalAmount = 1000m, ProductDescription = "=2+2" },
            new Sale { Id = 2, CustomerId = 1, TotalAmount = 500m },
            new Sale { Id = 3, CustomerId = 1, TotalAmount = 100m, IsPaid = true },
            new Sale { Id = 4, CustomerId = 2, TotalAmount = 200m },
            new Sale { Id = 5, CustomerId = 2, TotalAmount = 10m });
        db.Payments.AddRange(
            new Payment { Id = 1, SaleId = 1, Amount = 1000m, PaymentTypeId = 1 },
            new Payment { Id = 2, SaleId = 1, Amount = 100m, PaymentDate = new DateTime(2026, 8, 1) },
            new Payment { Id = 3, SaleId = 2, Amount = 50m, PaymentDate = new DateTime(2026, 9, 1) },
            new Payment { Id = 4, SaleId = 3, Amount = 100m, PaymentDate = new DateTime(2026, 11, 1) },
            new Payment { Id = 5, SaleId = 5, Amount = 10m });
        await db.SaveChangesAsync(default);
        await using (var other = PaymentsContextFactory.Create("other", database))
        {
            other.Customers.Add(new Customer { Id = 10, Name = "Oculto" });
            other.Sales.Add(new Sale { Id = 10, CustomerId = 10, TotalAmount = 9000m });
            await other.SaveChangesAsync(default);
        }

        var result = await new GetCustomerDebtsExcelHandler(db).Handle(new GetCustomerDebtsExcelQuery(), default);
        using var workbook = new XLWorkbook(new MemoryStream(result.Content));
        var summary = workbook.Worksheet("Deudas activas");
        var purchases = workbook.Worksheet("Compras activas");
        var payments = workbook.Worksheet("Abonos");
        Assert.EndsWith(".xlsx", result.FileName);
        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.Equal(3, summary.LastRowUsed()!.RowNumber());
        Assert.Equal("=1+1 Cliente", summary.Cell(2, 2).GetString());
        Assert.False(summary.Cell(2, 2).HasFormula);
        Assert.Equal("Ana Perez", summary.Cell(2, 3).GetString());
        Assert.Equal(1500m, summary.Cell(2, 4).GetValue<decimal>());
        Assert.Equal(150m, summary.Cell(2, 5).GetValue<decimal>());
        Assert.Equal(1350m, summary.Cell(2, 6).GetValue<decimal>());
        Assert.Equal(50m, summary.Cell(2, 7).GetValue<decimal>());
        Assert.Equal(new DateTime(2026, 9, 1), summary.Cell(2, 8).GetDateTime());
        Assert.True(summary.Cell(3, 7).IsEmpty());
        Assert.True(summary.Cell(3, 8).IsEmpty());
        Assert.Equal(4, purchases.LastRowUsed()!.RowNumber());
        Assert.Equal("=2+2", purchases.Cell(2, 6).GetString());
        Assert.False(purchases.Cell(2, 6).HasFormula);
        Assert.Equal(3, payments.LastRowUsed()!.RowNumber());
        Assert.Equal(100m, payments.Cell(2, 6).GetValue<decimal>());
        Assert.Equal(50m, payments.Cell(3, 6).GetValue<decimal>());
    }

    [Fact]
    public async Task Excel_WithoutDebts_StillReturnsWorkbookWithHeaders()
    {
        await using var db = PaymentsContextFactory.Create();
        var result = await new GetCustomerDebtsExcelHandler(db).Handle(new GetCustomerDebtsExcelQuery(), default);
        using var workbook = new XLWorkbook(new MemoryStream(result.Content));
        Assert.All(workbook.Worksheets, sheet => Assert.Equal(1, sheet.LastRowUsed()!.RowNumber()));
    }
}
