using BusinessCloud.Application.Bazares.Queries.GetRejectedProofsReport;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Reports;

/// <summary>
/// Pruebas del reporte de comprobantes rechazados: conteos, agrupación por cliente
/// (reincidencias), separación de URLs de comprobantes y filtro por fecha.
/// </summary>
public class GetRejectedProofsReportHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    private static BzaProofRejection Rejection(
        int id, int customerId, string name, DateTime rejectedAt,
        string? proofUrls = null, decimal amount = 100m)
    {
        return new BzaProofRejection
        {
            Id = id,
            TenantId = Tenant,
            BzaCustomerId = customerId,
            CustomerName = name,
            CustomerPhone = "5510002000",
            EventDescription = "Cierre",
            TotalAmount = amount,
            Reason = "Comprobante ilegible",
            RejectedAt = rejectedAt,
            ProofUrls = proofUrls,
        };
    }

    [Fact]
    public async Task Handle_CuentaRechazosYClientesAfectados()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.ProofRejections.AddRange(
            Rejection(1, 100, "Ana", new DateTime(2026, 1, 1)),
            Rejection(2, 100, "Ana", new DateTime(2026, 1, 5)),
            Rejection(3, 200, "Beto", new DateTime(2026, 1, 3)));
        await ctx.SaveChangesAsync(default);

        var result = await new GetRejectedProofsReportHandler(ctx)
            .Handle(new GetRejectedProofsReportQuery(), default);

        Assert.Equal(3, result.TotalRejections);
        Assert.Equal(2, result.CustomersAffected);
    }

    [Fact]
    public async Task Handle_AgrupaReincidenciasPorClienteConUltimaFecha()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.ProofRejections.AddRange(
            Rejection(1, 100, "Ana", new DateTime(2026, 1, 1)),
            Rejection(2, 100, "Ana", new DateTime(2026, 1, 9)),
            Rejection(3, 200, "Beto", new DateTime(2026, 1, 3)));
        await ctx.SaveChangesAsync(default);

        var result = await new GetRejectedProofsReportHandler(ctx)
            .Handle(new GetRejectedProofsReportQuery(), default);

        // El de más rechazos primero (Ana con 2).
        var ana = result.Customers.First();
        Assert.Equal(100, ana.CustomerId);
        Assert.Equal(2, ana.RejectionCount);
        Assert.Equal(new DateTime(2026, 1, 9), ana.LastRejectedAt);
    }

    [Fact]
    public async Task Handle_SeparaUrlsDeComprobantesPorSaltoDeLinea()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.ProofRejections.Add(Rejection(1, 100, "Ana", new DateTime(2026, 1, 1),
            proofUrls: "http://x/a.png\nhttp://x/b.png"));
        await ctx.SaveChangesAsync(default);

        var result = await new GetRejectedProofsReportHandler(ctx)
            .Handle(new GetRejectedProofsReportQuery(), default);

        var item = Assert.Single(result.Rejections);
        Assert.Equal(new[] { "http://x/a.png", "http://x/b.png" }, item.ProofUrls);
    }

    [Fact]
    public async Task Handle_SinUrls_DevuelveListaVacia()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.ProofRejections.Add(Rejection(1, 100, "Ana", new DateTime(2026, 1, 1), proofUrls: null));
        await ctx.SaveChangesAsync(default);

        var result = await new GetRejectedProofsReportHandler(ctx)
            .Handle(new GetRejectedProofsReportQuery(), default);

        Assert.Empty(Assert.Single(result.Rejections).ProofUrls);
    }

    [Fact]
    public async Task Handle_FiltraPorRangoDeFechas()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.ProofRejections.AddRange(
            Rejection(1, 100, "Ana", new DateTime(2026, 1, 1)),
            Rejection(2, 200, "Beto", new DateTime(2026, 3, 1)));
        await ctx.SaveChangesAsync(default);

        var result = await new GetRejectedProofsReportHandler(ctx).Handle(
            new GetRejectedProofsReportQuery(From: new DateTime(2026, 2, 1), To: new DateTime(2026, 4, 1)),
            default);

        Assert.Equal(1, result.TotalRejections);
        Assert.Equal(200, result.Rejections.Single().CustomerId);
    }
}
