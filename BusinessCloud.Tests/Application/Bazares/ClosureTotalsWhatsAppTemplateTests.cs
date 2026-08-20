using BusinessCloud.Application.Bazares.Common;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class ClosureTotalsWhatsAppTemplateTests
{
    [Fact]
    public void Build_UsesLatestCobroStructureForPayloadAndPreview()
    {
        var payload = ClosureTotalsWhatsAppTemplate.Build(
            "Bazar Test",
            "Ana",
            500m,
            new DateTime(2026, 8, 8),
            new DateTime(2026, 8, 5),
            "19:30",
            "Cierre semanal",
            3,
            [" Blusa ", "Bolsa", "Zapatos"],
            "token-22");

        Assert.Equal("totales_cobro_v4", payload.TemplateName);
        Assert.Equal("Bazar Test", payload.HeaderParameter);
        Assert.Equal(
            ["Ana — Te saluda Bazar Test", "$500.00", "Sábado 08 de agosto", "Miércoles 05 de agosto a las 07:30 p.\u00A0m.", "Cierre semanal", "3", "Blusa, Bolsa, Zapatos"],
            payload.BodyParameters);
        Assert.Equal("token-22", payload.ButtonUrlParameter);

        var expectedPreview = string.Join(Environment.NewLine,
        [
            "Aviso de pago de Bazar Test (mensaje automático)",
            "",
            "Hola Ana — Te saluda Bazar Test 👋",
            "",
            "💰 Total a pagar: *$500.00*",
            "🚚 Entrega: *Sábado 08 de agosto*",
            "📅 Límite de pago: *Miércoles 05 de agosto a las 07:30 p.\u00A0m.*",
            "📦 Cierre semanal: *Total de producto(s) · 3* - (Blusa, Bolsa, Zapatos)",
            "",
            "ENVÍA TU COMPROBANTE DE COMPRA POR ESTE CHAT",
            "O",
            "👇 Sube tu comprobante y consulta las tarjetas de pago en tu enlace personal (botón de abajo).",
            "__UPLOAD_LINK__",
        ]);
        Assert.Equal(expectedPreview, payload.Preview);
    }

    [Fact]
    public void Build_FormatsProductNamesWithCapAndEmptyFallback()
    {
        var cappedPayload = ClosureTotalsWhatsAppTemplate.Build(
            null,
            "Cliente",
            1m,
            new DateTime(2026, 8, 8),
            new DateTime(2026, 8, 5),
            null,
            null,
            11,
            [" Uno ", "Dos", "", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve", "   "],
            "token");
        var emptyPayload = ClosureTotalsWhatsAppTemplate.Build(
            null,
            "Cliente",
            1m,
            new DateTime(2026, 8, 8),
            new DateTime(2026, 8, 5),
            null,
            null,
            0,
            ["", "   "],
            "token");

        Assert.Equal("Uno, Dos, Tres, Cuatro, Cinco, Seis, Siete, Ocho, … y 1 más", cappedPayload.BodyParameters[6]);
        Assert.Equal("—", emptyPayload.BodyParameters[6]);
    }
}