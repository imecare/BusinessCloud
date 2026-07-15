using BusinessCloud.Application.Commissions.Commands.PayCommissions;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Payments;

/// <summary>
/// Pruebas de la liquidación de comisiones: filtra por vendedor, respeta el corte por fecha,
/// omite comisiones ya pagadas y aísla por tenant.
/// </summary>
public class PayCommissionsHandlerTests
{
    private const string Tenant = PaymentsContextFactory.TenantId;

    private static Sale Sale(int id, int sellerId, DateTime date, bool commissionPaid = false, string tenant = Tenant)
    {
        return new Sale
        {
            Id = id,
            TenantId = tenant,
            CustomerId = 1,
            SellerId = sellerId,
            Date = date,
            IsCommissionPaid = commissionPaid,
            ProductDescription = "Producto",
        };
    }

    [Fact]
    public async Task Handle_LiquidaSoloPendientesDelVendedorHastaLaFecha()
    {
        using var ctx = PaymentsContextFactory.Create();
        ctx.Sales.AddRange(
            Sale(1, sellerId: 10, date: new DateTime(2026, 1, 5)),   // sí
            Sale(2, sellerId: 10, date: new DateTime(2026, 1, 20)),  // no: posterior al corte
            Sale(3, sellerId: 99, date: new DateTime(2026, 1, 5)),   // no: otro vendedor
            Sale(4, sellerId: 10, date: new DateTime(2026, 1, 1), commissionPaid: true)); // no: ya pagada
        await ctx.SaveChangesAsync(default);

        var affected = await new PayCommissionsHandler(ctx)
            .Handle(new PayCommissionsCommand(SellerId: 10, ToDate: new DateTime(2026, 1, 10)), default);

        Assert.Equal(1, affected);
        Assert.True((await ctx.Sales.FindAsync(1))!.IsCommissionPaid);
        Assert.False((await ctx.Sales.FindAsync(2))!.IsCommissionPaid);
        Assert.False((await ctx.Sales.FindAsync(3))!.IsCommissionPaid);
    }

    [Fact]
    public async Task Handle_IncluyeVentasExactamenteEnLaFechaDeCorte()
    {
        using var ctx = PaymentsContextFactory.Create();
        ctx.Sales.Add(Sale(1, sellerId: 10, date: new DateTime(2026, 2, 15)));
        await ctx.SaveChangesAsync(default);

        var affected = await new PayCommissionsHandler(ctx)
            .Handle(new PayCommissionsCommand(10, new DateTime(2026, 2, 15)), default);

        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task Handle_SinComisionesPendientes_DevuelveCero()
    {
        using var ctx = PaymentsContextFactory.Create();
        ctx.Sales.Add(Sale(1, sellerId: 10, date: new DateTime(2026, 1, 1), commissionPaid: true));
        await ctx.SaveChangesAsync(default);

        var affected = await new PayCommissionsHandler(ctx)
            .Handle(new PayCommissionsCommand(10, new DateTime(2026, 12, 31)), default);

        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task Handle_NoAfectaVentasDeOtroTenant()
    {
        var dbName = $"payments-shared-{Guid.NewGuid():N}";

        // Siembra una venta de OTRO tenant usando un contexto con ese tenant (el
        // override de SaveChanges estampa el TenantId según el usuario actual).
        using (var otherCtx = PaymentsContextFactory.Create("otro-tenant", dbName))
        {
            otherCtx.Sales.Add(Sale(2, sellerId: 10, date: new DateTime(2026, 1, 5), tenant: "otro-tenant"));
            await otherCtx.SaveChangesAsync(default);
        }

        using var ctx = PaymentsContextFactory.Create(Tenant, dbName);
        ctx.Sales.Add(Sale(1, sellerId: 10, date: new DateTime(2026, 1, 5)));
        await ctx.SaveChangesAsync(default);

        var affected = await new PayCommissionsHandler(ctx)
            .Handle(new PayCommissionsCommand(10, new DateTime(2026, 1, 31)), default);

        // El filtro global de tenant deja fuera la venta de "otro-tenant".
        Assert.Equal(1, affected);
    }
}
