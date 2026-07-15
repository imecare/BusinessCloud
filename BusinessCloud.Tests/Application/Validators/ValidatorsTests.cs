using BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;
using BusinessCloud.Application.Bazares.Commands.CreatePaymentCard;
using BusinessCloud.Application.Bazares.Commands.CreateBzaSaleWithProducts;
using BusinessCloud.Application.Payments.Commands.CreateSeller;
using BusinessCloud.Application.Payments.Commands.MarkCommissionPaid;
using BusinessCloud.Application.Payments.Commands.RegisterPayment;
using BusinessCloud.Application.Payments.Queries.GetPublicHistory;
using Xunit;

namespace BusinessCloud.Tests.Application.Validators;

/// <summary>
/// Pruebas de los validadores FluentValidation de mayor valor (reglas de negocio de
/// entrada: montos, longitudes, requeridos, formato de teléfono y de tarjeta).
/// </summary>
public class ValidatorsTests
{
    // ---- RegisterPaymentValidator ----

    [Fact]
    public void RegisterPayment_Valido_NoTieneErrores()
    {
        var cmd = new RegisterPaymentCommand(1, 100m, "Efectivo", DateTime.UtcNow);
        var result = new RegisterPaymentValidator().Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RegisterPayment_MontoNoPositivo_Falla(decimal amount)
    {
        var cmd = new RegisterPaymentCommand(1, amount, "Efectivo", DateTime.UtcNow);
        var result = new RegisterPaymentValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterPaymentCommand.Amount));
    }

    [Fact]
    public void RegisterPayment_ReferenciaMuyLarga_Falla()
    {
        var cmd = new RegisterPaymentCommand(1, 10m, new string('x', 101), DateTime.UtcNow);
        var result = new RegisterPaymentValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterPaymentCommand.Reference));
    }

    // ---- CreateBzaCustomerValidator ----

    [Fact]
    public void CreateBzaCustomer_Valido_NoTieneErrores()
    {
        var cmd = new CreateBzaCustomerCommand { Name = "Ana", Phone = "5512345678", BzaCollectorId = 3 };
        var result = new CreateBzaCustomerValidator().Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateBzaCustomer_SinNombre_Falla()
    {
        var cmd = new CreateBzaCustomerCommand { Name = "", Phone = "5512345678", BzaCollectorId = 3 };
        var result = new CreateBzaCustomerValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBzaCustomerCommand.Name));
    }

    [Fact]
    public void CreateBzaCustomer_TelefonoCorto_Falla()
    {
        var cmd = new CreateBzaCustomerCommand { Name = "Ana", Phone = "123", BzaCollectorId = 3 };
        var result = new CreateBzaCustomerValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBzaCustomerCommand.Phone));
    }

    [Fact]
    public void CreateBzaCustomer_RecolectorInvalido_Falla()
    {
        var cmd = new CreateBzaCustomerCommand { Name = "Ana", Phone = "5512345678", BzaCollectorId = 0 };
        var result = new CreateBzaCustomerValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBzaCustomerCommand.BzaCollectorId));
    }

    // ---- CreatePaymentCardValidator ----

    [Fact]
    public void CreatePaymentCard_Valido_NoTieneErrores()
    {
        var cmd = new CreatePaymentCardCommand("4242424242424242", "Ana Perez", "BBVA", null);
        var result = new CreatePaymentCardValidator().Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreatePaymentCard_NumeroInvalido_Falla()
    {
        var cmd = new CreatePaymentCardCommand("1234567890123456", "Ana Perez", "BBVA", null);
        var result = new CreatePaymentCardValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCardCommand.CardNumber));
    }

    [Fact]
    public void CreatePaymentCard_SinTitular_Falla()
    {
        var cmd = new CreatePaymentCardCommand("4242424242424242", "", null, null);
        var result = new CreatePaymentCardValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCardCommand.CardHolderName));
    }

    // ---- CreateBzaSaleWithProductsValidator ----

    [Fact]
    public void CreateBzaSaleWithProducts_Valido_NoTieneErrores()
    {
        var cmd = new CreateBzaSaleWithProductsCommand
        {
            BzaEventId = 1,
            BzaCustomerId = 2,
            Products = [new CreateBzaSaleProductItem { Description = "Blusa", Price = 150m }],
        };
        var result = new CreateBzaSaleWithProductsValidator().Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateBzaSaleWithProducts_SinProductos_Falla()
    {
        var cmd = new CreateBzaSaleWithProductsCommand { BzaEventId = 1, BzaCustomerId = 2, Products = [] };
        var result = new CreateBzaSaleWithProductsValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBzaSaleWithProductsCommand.Products));
    }

    [Fact]
    public void CreateBzaSaleWithProducts_ProductoConPrecioNoPositivo_Falla()
    {
        var cmd = new CreateBzaSaleWithProductsCommand
        {
            BzaEventId = 1,
            BzaCustomerId = 2,
            Products = [new CreateBzaSaleProductItem { Description = "Blusa", Price = 0m }],
        };
        var result = new CreateBzaSaleWithProductsValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Price"));
    }

    // ---- CreateSellerValidator ----

    [Theory]
    [InlineData("Juan", "Perez", "5512345678", true)]
    [InlineData("Juan", "Perez", "+525512345678", true)]
    [InlineData("", "Perez", "5512345678", false)]     // sin nombre
    [InlineData("Juan", "Perez", "12", false)]          // teléfono corto
    [InlineData("Juan", "Perez", "abcdefghij", false)]  // teléfono no numérico
    public void CreateSeller_ValidaSegunReglas(string name, string lastName, string phone, bool esperadoValido)
    {
        var cmd = new CreateSellerCommand(name, lastName, phone);
        var result = new CreateSellerValidator().Validate(cmd);
        Assert.Equal(esperadoValido, result.IsValid);
    }

    // ---- MarkCommissionPaidValidator ----

    [Fact]
    public void MarkCommissionPaid_Valido_NoTieneErrores()
    {
        var cmd = new MarkCommissionPaidCommand(5, true, "Pago en efectivo");
        var result = new MarkCommissionPaidValidator().Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MarkCommissionPaid_SaleIdInvalido_Falla()
    {
        var cmd = new MarkCommissionPaidCommand(0, true, null);
        var result = new MarkCommissionPaidValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(MarkCommissionPaidCommand.SaleId));
    }

    [Fact]
    public void MarkCommissionPaid_NotaMuyLarga_Falla()
    {
        var cmd = new MarkCommissionPaidCommand(5, true, new string('x', 501));
        var result = new MarkCommissionPaidValidator().Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(MarkCommissionPaidCommand.Note));
    }

    // ---- GetPublicHistoryQueryValidator ----

    [Fact]
    public void GetPublicHistory_Valido_NoTieneErrores()
    {
        var query = new GetPublicHistoryQuery("5512345678", "PEGJ850101ABC", "EMP001");
        var result = new GetPublicHistoryQueryValidator().Validate(query);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "PEGJ850101ABC", "EMP001")]        // sin teléfono
    [InlineData("5512345678", "COR", "EMP001")]         // RFC muy corto
    [InlineData("5512345678", "PEGJ850101ABC", "")]     // sin código de empresa
    public void GetPublicHistory_DatosInvalidos_Falla(string phone, string rfc, string company)
    {
        var query = new GetPublicHistoryQuery(phone, rfc, company);
        var result = new GetPublicHistoryQueryValidator().Validate(query);
        Assert.False(result.IsValid);
    }
}
