using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Commands.MarkCommissionPaid;
using BusinessCloud.Application.Payments.Queries.GetAllSales;
using BusinessCloud.Application.Payments.Queries.GetMySales;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Payments;

public class SalesCommissionPaymentTests
{
    private static Mock<ICurrentUserService> CurrentUser()
    {
        var user = new Mock<ICurrentUserService>();
        user.SetupGet(x => x.SellerId).Returns(10);
        user.SetupGet(x => x.UserId).Returns("admin-test");
        return user;
    }

    private static Sale PaidSale(int id, int sellerId = 10) => new()
    {
        Id = id,
        CustomerId = 1,
        SellerId = sellerId,
        IsPaid = true,
        TotalAmount = 100m,
        CommissionAmount = 10m,
        Date = new DateTime(2026, 1, 5),
    };

    [Theory]
    [InlineData("Pagado por transferencia.\nReferencia de prueba.")]
    [InlineData(null)]
    public async Task Pago_expone_nota_y_fecha_guardadas_en_ventas_de_admin_y_comisionista(string? note)
    {
        using var db = PaymentsContextFactory.Create();
        db.Customers.Add(new Customer { Id = 1, Name = "Cliente", LastName = "Prueba" });
        db.Sellers.Add(new Seller { Id = 10, Name = "Vendedor", LastName = "Prueba" });
        db.Sales.Add(PaidSale(1));
        await db.SaveChangesAsync();
        var user = CurrentUser();
        var mongo = new Mock<IMongoContext>();
        mongo.Setup(x => x.InsertAuditLogAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new MarkCommissionPaidHandler(db, user.Object, mongo.Object);

        var result = await handler.Handle(new MarkCommissionPaidCommand(1, true, note), default);
        Assert.True(result.Success);
        db.ChangeTracker.Clear();
        var saved = await db.Sales.SingleAsync();
        Assert.NotNull(saved.CommissionPaidAt);
        Assert.Equal(note, saved.CommissionPaymentNote);

        var admin = Assert.Single(await new GetAllSalesQueryHandler(db).Handle(new GetAllSalesQuery(), default));
        var mine = Assert.Single(await new GetMySalesQueryHandler(db, user.Object).Handle(new GetMySalesQuery(), default));
        Assert.Equal(saved.CommissionPaidAt, admin.CommissionPaidAt);
        Assert.Equal(saved.CommissionPaidAt, mine.CommissionPaidAt);
        Assert.Equal(note, admin.CommissionPaymentNote);
        Assert.Equal(note, mine.CommissionPaymentNote);

        await handler.Handle(new MarkCommissionPaidCommand(1, false, "Error de registro"), default);
        db.ChangeTracker.Clear();
        admin = Assert.Single(await new GetAllSalesQueryHandler(db).Handle(new GetAllSalesQuery(), default));
        mine = Assert.Single(await new GetMySalesQueryHandler(db, user.Object).Handle(new GetMySalesQuery(), default));
        Assert.False(admin.IsCommissionPaid);
        Assert.False(mine.IsCommissionPaid);
        Assert.Null(admin.CommissionPaidAt);
        Assert.Null(mine.CommissionPaidAt);
        Assert.Null(admin.CommissionPaymentNote);
        Assert.Null(mine.CommissionPaymentNote);
    }

    [Fact]
    public async Task Comisionista_solo_recibe_notas_de_sus_ventas_y_su_empresa()
    {
        var databaseName = $"sales-commissions-{Guid.NewGuid():N}";
        using (var other = PaymentsContextFactory.Create("otro-tenant", databaseName))
        {
            other.Customers.Add(new Customer { Id = 2, Name = "Otro", LastName = "Cliente" });
            other.Sellers.Add(new Seller { Id = 20, Name = "Otro", LastName = "Vendedor" });
            var foreignSale = PaidSale(3, 10);
            foreignSale.CustomerId = 2;
            foreignSale.IsCommissionPaid = true;
            foreignSale.CommissionPaymentNote = "Nota de otra empresa";
            other.Sales.Add(foreignSale);
            await other.SaveChangesAsync();
        }

        using var db = PaymentsContextFactory.Create(databaseName: databaseName);
        db.Customers.Add(new Customer { Id = 1, Name = "Cliente", LastName = "Prueba" });
        db.Sellers.Add(new Seller { Id = 10, Name = "Vendedor", LastName = "Prueba" });
        var own = PaidSale(1);
        own.IsCommissionPaid = true;
        own.CommissionPaymentNote = "Nota propia";
        var anotherSeller = PaidSale(2, 20);
        anotherSeller.IsCommissionPaid = true;
        anotherSeller.CommissionPaymentNote = "Nota de otro vendedor";
        db.Sales.AddRange(own, anotherSeller);
        await db.SaveChangesAsync();

        var mine = Assert.Single(await new GetMySalesQueryHandler(db, CurrentUser().Object)
            .Handle(new GetMySalesQuery(), default));
        Assert.Equal(1, mine.Id);
        Assert.Equal("Nota propia", mine.CommissionPaymentNote);
        var admin = await new GetAllSalesQueryHandler(db).Handle(new GetAllSalesQuery(), default);
        Assert.Equal(2, admin.Count);
        Assert.DoesNotContain(admin, x => x.Id == 3);
    }
}
