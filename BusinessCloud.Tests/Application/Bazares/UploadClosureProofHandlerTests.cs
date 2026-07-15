using BusinessCloud.Application.Bazares.Commands.UploadClosureProof;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

/// <summary>
/// Pruebas de la subida de comprobante de cierre, enfocadas en la captura del banco de
/// "retiro sin tarjeta", el método de pago declarado y la transición de estado a
/// "comprobante recibido". Se usa un cierre sin eventos para aislar de la lógica de pagos.
/// </summary>
public class UploadClosureProofHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;
    private const string Token = "tok-123";

    private static UploadClosureProofCommand Command(int? paymentMethod, string? bank)
    {
        var file = new ClosureProofFileInput(new MemoryStream(new byte[] { 1, 2, 3 }), "comprobante.jpg", "image/jpeg");
        return new UploadClosureProofCommand(Token, new[] { file }, PaymentMethod: paymentMethod, WithdrawalBank: bank);
    }

    private static async Task<BazaresDbContextWrapper> SeedAsync(int status = BzaClosureCustomerTotalStatus.Pending, string? existingBank = null)
    {
        var ctx = BazaresContextFactory.Create();
        ctx.ClosureCustomerTotals.Add(new BzaClosureCustomerTotal
        {
            Id = 1,
            TenantId = Tenant,
            BzaCustomerId = 1,
            BzaClosureEventId = 1,
            UploadToken = Token,
            Status = status,
            WithdrawalBank = existingBank,
            Customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana", Phone = "5511112222" },
            ClosureEvent = new BzaClosureEvent
            {
                Id = 1,
                TenantId = Tenant,
                Description = "Cierre",
                Status = BzaClosureEventStatus.PendingPayment,
                Items = new List<BzaClosureEventItem>(),
            },
            Proofs = new List<BzaClosureProof>(),
        });
        await ctx.SaveChangesAsync(default);
        return new BazaresDbContextWrapper(ctx);
    }

    private static UploadClosureProofHandler Handler(IBazaresDbContext ctx)
    {
        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://blob/proof.jpg");
        return new UploadClosureProofHandler(ctx, blob.Object);
    }

    [Fact]
    public async Task Handle_RetiroSinTarjeta_GuardaBancoYCambiaEstado()
    {
        using var wrapper = await SeedAsync();
        var result = await Handler(wrapper.Context).Handle(Command(paymentMethod: 3, bank: "BBVA"), default);

        Assert.True(result.Success);
        var total = await wrapper.Context.ClosureCustomerTotals.FindAsync(1);
        Assert.Equal(3, total!.PaymentMethod);
        Assert.Equal("BBVA", total.WithdrawalBank);
        Assert.Equal(BzaClosureCustomerTotalStatus.ProofReceived, total.Status);
    }

    [Fact]
    public async Task Handle_OtroMetodoDePago_LimpiaElBanco()
    {
        using var wrapper = await SeedAsync(existingBank: "Banco Viejo");
        await Handler(wrapper.Context).Handle(Command(paymentMethod: 1, bank: "Ignorado"), default);

        var total = await wrapper.Context.ClosureCustomerTotals.FindAsync(1);
        Assert.Equal(1, total!.PaymentMethod);
        Assert.Null(total.WithdrawalBank);
    }

    [Fact]
    public async Task Handle_RetiroSinTarjetaSinBancoNuevo_ConservaElBancoExistente()
    {
        using var wrapper = await SeedAsync(existingBank: "Santander");
        await Handler(wrapper.Context).Handle(Command(paymentMethod: 3, bank: null), default);

        var total = await wrapper.Context.ClosureCustomerTotals.FindAsync(1);
        Assert.Equal("Santander", total!.WithdrawalBank);
    }

    [Fact]
    public async Task Handle_ComprobanteYaValidado_LanzaExcepcion()
    {
        using var wrapper = await SeedAsync(status: BzaClosureCustomerTotalStatus.Validated);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(wrapper.Context).Handle(Command(paymentMethod: 3, bank: "BBVA"), default));
    }

    /// <summary>Pequeño wrapper para exponer y liberar el contexto en las pruebas.</summary>
    private sealed class BazaresDbContextWrapper(BazaresDbContext ctx) : IDisposable
    {
        public BazaresDbContext Context { get; } = ctx;
        public void Dispose() => Context.Dispose();
    }
}
