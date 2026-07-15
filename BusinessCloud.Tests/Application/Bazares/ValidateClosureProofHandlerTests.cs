using BusinessCloud.Application.Bazares.Commands.ValidateClosureProof;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

/// <summary>
/// Pruebas de la validación del comprobante de cierre: transiciones de estado válidas/erróneas,
/// idempotencia, aprobación de pagos preautorizados y cierre del evento cuando todos los
/// comprobantes quedan validados.
/// </summary>
public class ValidateClosureProofHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;
    private const int EventId = 100;

    /// <summary>
    /// Siembra un cierre con sus totales por cliente y (opcionalmente) un pago preautorizado
    /// por comprobante para el primer total.
    /// </summary>
    private static async Task SeedAsync(BazaresDbContext ctx, int[] totalStatuses, bool withPreauthPayment = false)
    {
        var closure = new BzaClosureEvent
        {
            Id = 1,
            TenantId = Tenant,
            Description = "Cierre",
            Status = BzaClosureEventStatus.ProofReceived,
            Items = new List<BzaClosureEventItem>
            {
                new() { Id = 1, TenantId = Tenant, BzaEventId = EventId },
            },
            CustomerTotals = new List<BzaClosureCustomerTotal>(),
        };

        for (var i = 0; i < totalStatuses.Length; i++)
        {
            closure.CustomerTotals.Add(new BzaClosureCustomerTotal
            {
                Id = i + 1,
                TenantId = Tenant,
                BzaClosureEventId = 1,
                BzaCustomerId = i + 1,
                Status = totalStatuses[i],
                ClosureEvent = closure,
            });
        }

        ctx.ClosureEvents.Add(closure);

        if (withPreauthPayment)
        {
            ctx.Payments.Add(new BzaPayment
            {
                Id = 1,
                TenantId = Tenant,
                BzaCustomerId = 1,
                BzaEventId = EventId,
                Amount = 250m,
                PaymentMethod = "Comprobante",
                IsVerified = false,
                PaymentStatus = 1,
            });
        }

        await ctx.SaveChangesAsync(default);
    }

    private static ValidateClosureProofHandler Handler(BazaresDbContext ctx) => new(ctx);

    [Fact]
    public async Task Handle_TotalPendiente_LanzaExcepcion()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, new[] { BzaClosureCustomerTotalStatus.Pending });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(ctx).Handle(new ValidateClosureProofCommand(1), default));
    }

    [Fact]
    public async Task Handle_TotalRechazado_LanzaExcepcion()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, new[] { BzaClosureCustomerTotalStatus.Rejected });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(ctx).Handle(new ValidateClosureProofCommand(1), default));
    }

    [Fact]
    public async Task Handle_TotalYaValidado_EsIdempotente()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, new[] { BzaClosureCustomerTotalStatus.Validated });

        var result = await Handler(ctx).Handle(new ValidateClosureProofCommand(1), default);

        Assert.Equal(BzaClosureCustomerTotalStatus.Validated, result.TotalStatus);
    }

    [Fact]
    public async Task Handle_TotalInexistente_LanzaKeyNotFound()
    {
        using var ctx = BazaresContextFactory.Create();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => Handler(ctx).Handle(new ValidateClosureProofCommand(999), default));
    }

    [Fact]
    public async Task Handle_ComprobanteRecibido_ValidaYApruebaPagos()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, new[] { BzaClosureCustomerTotalStatus.ProofReceived }, withPreauthPayment: true);

        var result = await Handler(ctx).Handle(new ValidateClosureProofCommand(1), default);

        Assert.Equal(BzaClosureCustomerTotalStatus.Validated, result.TotalStatus);
        var payment = await ctx.Payments.FindAsync(1);
        Assert.True(payment!.IsVerified);
        Assert.Equal(2, payment.PaymentStatus);
        Assert.NotNull(payment.VerifiedAt);
    }

    [Fact]
    public async Task Handle_TodosLosTotalesValidados_CierraElEvento()
    {
        using var ctx = BazaresContextFactory.Create();
        // El total 1 se valida ahora; el total 2 ya estaba validado.
        await SeedAsync(ctx, new[] { BzaClosureCustomerTotalStatus.ProofReceived, BzaClosureCustomerTotalStatus.Validated });

        var result = await Handler(ctx).Handle(new ValidateClosureProofCommand(1), default);

        Assert.Equal(BzaClosureEventStatus.Validated, result.ClosureStatus);
    }

    [Fact]
    public async Task Handle_QuedanTotalesPendientes_NoCierraElEvento()
    {
        using var ctx = BazaresContextFactory.Create();
        // El total 1 se valida; el total 2 sigue pendiente => el evento NO se cierra.
        await SeedAsync(ctx, new[] { BzaClosureCustomerTotalStatus.ProofReceived, BzaClosureCustomerTotalStatus.Pending });

        var result = await Handler(ctx).Handle(new ValidateClosureProofCommand(1), default);

        Assert.Equal(BzaClosureCustomerTotalStatus.Validated, result.TotalStatus);
        Assert.NotEqual(BzaClosureEventStatus.Validated, result.ClosureStatus);
    }
}
