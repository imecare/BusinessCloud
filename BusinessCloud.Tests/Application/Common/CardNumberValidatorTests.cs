using BusinessCloud.Application.Bazares.Common;
using Xunit;

namespace BusinessCloud.Tests.Application.Common;

/// <summary>
/// Pruebas del validador de números de tarjeta (formato + algoritmo de Luhn).
/// Lógica pura y crítica para el registro de tarjetas de pago.
/// </summary>
public class CardNumberValidatorTests
{
    [Theory]
    [InlineData("4242424242424242")] // Visa de prueba, pasa Luhn
    [InlineData("4111111111111111")] // Visa de prueba, pasa Luhn
    [InlineData("5555555555554444")] // Mastercard de prueba, pasa Luhn
    [InlineData("4242 4242 4242 4242")] // con espacios
    [InlineData("4242-4242-4242-4242")] // con guiones
    public void IsValid_ConNumeroValido_DevuelveTrue(string cardNumber)
    {
        Assert.True(CardNumberValidator.IsValid(cardNumber));
    }

    [Theory]
    [InlineData("4242424242424241")] // falla Luhn (último dígito alterado)
    [InlineData("1234567890123456")] // falla Luhn
    public void IsValid_ConDigitoDeControlIncorrecto_DevuelveFalse(string cardNumber)
    {
        Assert.False(CardNumberValidator.IsValid(cardNumber));
    }

    [Theory]
    [InlineData("42424242")]              // menos de 13 dígitos
    [InlineData("42424242424242424242")]  // más de 19 dígitos
    public void IsValid_ConLongitudFueraDeRango_DevuelveFalse(string cardNumber)
    {
        Assert.False(CardNumberValidator.IsValid(cardNumber));
    }

    [Theory]
    [InlineData("4242abcd42424242")] // contiene letras
    [InlineData("4242.4242.4242")]   // separador no permitido (punto)
    public void IsValid_ConCaracteresNoNumericos_DevuelveFalse(string cardNumber)
    {
        Assert.False(CardNumberValidator.IsValid(cardNumber));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ConNuloOVacio_DevuelveFalse(string? cardNumber)
    {
        Assert.False(CardNumberValidator.IsValid(cardNumber));
    }
}
