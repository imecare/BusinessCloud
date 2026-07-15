using BusinessCloud.Application.Bazares.Queries.GetPendingWithdrawalsReport;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Reports;

/// <summary>
/// Pruebas del reporte de retiros sin tarjeta pendientes de validar.
/// Verifica el filtrado (PaymentMethod=3 + Status=ProofReceived), el mapeo de banco/venta/
/// imágenes y los totales agregados.
/// </summary>
public class GetPendingWithdrawalsReportHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    private static BzaClosureCustomerTotal Total(
        int id, int paymentMethod, int status, decimal amount,
        string? bank = null, string customerName = "Cliente", string sale = "Cierre",
        DateTime? uploadedAt = null, List<BzaClosureProof>? proofs = null)
    {
        return new BzaClosureCustomerTotal
        {
            Id = id,
            TenantId = Tenant,
            BzaCustomerId = id,
            BzaClosureEventId = id,
            PaymentMethod = paymentMethod,
            Status = status,
            TotalAmount = amount,
            WithdrawalBank = bank,
            ProofUploadedAt = uploadedAt,
            Customer = new BzaCustomer { Id = id, TenantId = Tenant, Name = customerName, Phone = "5511112222" },
            ClosureEvent = new BzaClosureEvent { Id = id, TenantId = Tenant, Description = sale },
            Proofs = proofs ?? new List<BzaClosureProof>(),
        };
    }

    [Fact]
    public async Task Handle_SoloIncluyeRetirosSinTarjetaPendientesDeValidar()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.ClosureCustomerTotals.AddRange(
            // Sí: retiro sin tarjeta (3) + comprobante recibido (2)
            Total(1, paymentMethod: 3, status: BzaClosureCustomerTotalStatus.ProofReceived, amount: 100m, bank: "BBVA"),
            // No: retiro sin tarjeta pero ya validado
            Total(2, paymentMethod: 3, status: BzaClosureCustomerTotalStatus.Validated, amount: 200m, bank: "BBVA"),
            // No: transferencia (1) pendiente de validar
            Total(3, paymentMethod: 1, status: BzaClosureCustomerTotalStatus.ProofReceived, amount: 300m),
            // No: retiro sin tarjeta pero aún pendiente (sin comprobante)
            Total(4, paymentMethod: 3, status: BzaClosureCustomerTotalStatus.Pending, amount: 400m));
        await ctx.SaveChangesAsync(default);

        var result = await new GetPendingWithdrawalsReportHandler(ctx)
            .Handle(new GetPendingWithdrawalsReportQuery(), default);

        Assert.Equal(1, result.TotalPending);
        Assert.Equal(100m, result.TotalAmount);
        var item = Assert.Single(result.Items);
        Assert.Equal("BBVA", item.Bank);
    }

    [Fact]
    public async Task Handle_MapeaClienteVentaEImagenesDeComprobante()
    {
        using var ctx = BazaresContextFactory.Create();
        var proofs = new List<BzaClosureProof>
        {
            new() { Id = 1, TenantId = Tenant, ImageUrl = "http://x/1.png", UploadedAt = new DateTime(2026, 1, 1) },
            new() { Id = 2, TenantId = Tenant, ImageUrl = "http://x/2.png", UploadedAt = new DateTime(2026, 1, 2) },
        };
        ctx.ClosureCustomerTotals.Add(Total(
            10, paymentMethod: 3, status: BzaClosureCustomerTotalStatus.ProofReceived, amount: 500m,
            bank: "Santander", customerName: "Ana", sale: "Cierre Marzo", proofs: proofs));
        await ctx.SaveChangesAsync(default);

        var result = await new GetPendingWithdrawalsReportHandler(ctx)
            .Handle(new GetPendingWithdrawalsReportQuery(), default);

        var item = Assert.Single(result.Items);
        Assert.Equal("Ana", item.CustomerName);
        Assert.Equal("Cierre Marzo", item.SaleDescription);
        Assert.Equal("Santander", item.Bank);
        Assert.Equal(new[] { "http://x/1.png", "http://x/2.png" }, item.ProofUrls);
    }

    [Fact]
    public async Task Handle_FiltraPorFechaDeSubidaDelComprobante()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.ClosureCustomerTotals.AddRange(
            Total(1, 3, BzaClosureCustomerTotalStatus.ProofReceived, 100m, uploadedAt: new DateTime(2026, 1, 10)),
            Total(2, 3, BzaClosureCustomerTotalStatus.ProofReceived, 100m, uploadedAt: new DateTime(2026, 3, 10)));
        await ctx.SaveChangesAsync(default);

        var result = await new GetPendingWithdrawalsReportHandler(ctx).Handle(
            new GetPendingWithdrawalsReportQuery(From: new DateTime(2026, 2, 1), To: new DateTime(2026, 4, 1)),
            default);

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.Id);
    }

    [Fact]
    public async Task Handle_SinRegistros_DevuelveReporteVacio()
    {
        using var ctx = BazaresContextFactory.Create();
        var result = await new GetPendingWithdrawalsReportHandler(ctx)
            .Handle(new GetPendingWithdrawalsReportQuery(), default);

        Assert.Equal(0, result.TotalPending);
        Assert.Equal(0m, result.TotalAmount);
        Assert.Empty(result.Items);
    }
}
