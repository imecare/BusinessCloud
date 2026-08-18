using BusinessCloud.Application.Bazares.Common;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class WelcomeMessageBuilderTests
{
    [Fact]
    public void Build_WhatsApp_IncludesSystemNumberAndComplementBeforeClosing()
    {
        var message = WelcomeMessageBuilder.Build(
            WelcomeMessageBuilder.WhatsAppChannel,
            "Banana Bazar",
            "Ana López",
            "521234567890",
            "https://wa.me/525550001111",
            "https://m.me/banana.bazar",
            "Horario de entrega: sábados de 10 a 14 h.");

        Assert.Contains("*¡Bienvenido(a) a Bazar Banana Bazar! 👋*", message);
        Assert.Contains("Hola Ana López,", message);
        // Bloque WhatsApp: número del sistema de la plataforma.
        Assert.Contains("número del sistema de Bazares", message);
        Assert.Contains("📞 521234567890", message);
        Assert.Contains("no del bazar", message);
        // Canales del bazar.
        Assert.Contains("https://wa.me/525550001111", message);
        Assert.Contains("https://m.me/banana.bazar", message);
        // El complemento va antes del cierre "Gracias".
        var complementIndex = message.IndexOf("Horario de entrega: sábados", System.StringComparison.Ordinal);
        var closingIndex = message.IndexOf("¡Gracias por ser parte de Banana Bazar!", System.StringComparison.Ordinal);
        Assert.True(complementIndex >= 0 && closingIndex >= 0 && complementIndex < closingIndex);
    }

    [Fact]
    public void Build_Messenger_UsesMessengerDeliveryBlockWithoutSystemNumber()
    {
        var message = WelcomeMessageBuilder.Build(
            WelcomeMessageBuilder.MessengerChannel,
            "Banana Bazar",
            "Beto",
            "521234567890",
            null,
            null,
            null);

        Assert.Contains("por este mismo Messenger", message);
        Assert.DoesNotContain("número del sistema de Bazares", message);
        Assert.DoesNotContain("📞 521234567890", message);
        Assert.EndsWith("¡Gracias por ser parte de Banana Bazar! 💛", message);
    }

    [Fact]
    public void Build_WithoutBazarName_UsesGenericFallback()
    {
        var message = WelcomeMessageBuilder.Build(
            WelcomeMessageBuilder.WhatsAppChannel, null, "Cliente", null, null, null, null);

        Assert.Contains("Bienvenido(a) a Bazar el Bazar", message);
        Assert.Contains("¡Gracias por ser parte de el Bazar!", message);
    }
}
