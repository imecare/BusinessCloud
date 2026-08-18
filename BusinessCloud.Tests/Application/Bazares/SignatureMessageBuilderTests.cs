using BusinessCloud.Application.Bazares.Common;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class SignatureMessageBuilderTests
{
    [Fact]
    public void Build_FormatsBoldHeadingAndEachUrlOnItsOwnLine()
    {
        var message = SignatureMessageBuilder.Build(
            "Bazar Luna",
            "Ana",
            ["https://blob.test/general.jpg", "https://blob.test/group.jpg"]);

        Assert.Equal(
            "Hola Ana 👋\n\n📦 *¡Tu paquete fue entregado!*\n" +
            "Consulta tu firma / comprobante de recibido en las siguientes imágenes:\n" +
            "https://blob.test/general.jpg\nhttps://blob.test/group.jpg\n\n" +
            "¡Gracias por tu compra con Bazar Luna! 💛",
            message);
    }

    [Fact]
    public void Build_WithoutProofs_UsesAvailabilityFallback()
    {
        var message = SignatureMessageBuilder.Build("Mi Bazar", "Cliente Uno", []);

        Assert.Contains("(Aún no hay imágenes de firma disponibles.)", message);
    }

    [Fact]
    public void Build_WithBlankNames_UsesBazarAndCustomerFallbacks()
    {
        var message = SignatureMessageBuilder.Build(" ", "", []);

        Assert.StartsWith("Hola cliente 👋", message);
        Assert.EndsWith("¡Gracias por tu compra con el Bazar! 💛", message);
    }
}
