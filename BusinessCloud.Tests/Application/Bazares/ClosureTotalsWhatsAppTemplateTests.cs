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
            "Any Lopez",
            "token-22");

        Assert.Equal("totales_cobro_v5", payload.TemplateName);
        Assert.Equal("Bazar Test", payload.HeaderParameter);
        Assert.Equal(
            ["Ana", "$500.00", "Sábado 08 de agosto", "Miércoles 05 de agosto a las 07:30 p.\u00A0m.", "Cierre semanal", "3", "__UPLOAD_LINK__"],
            payload.BodyParameters);
        Assert.Equal("token-22", payload.ButtonUrlParameter);
        Assert.DoesNotContain("Any Lopez", payload.Preview);
        Assert.Contains("Se entregará al recolector: Any Lopez", payload.ManualPreview);
        Assert.Contains("ENVÍA TU COMPROBANTE DE COMPRA POR ESTE CHAT", payload.ManualPreview);

        var expectedPreview = string.Join(Environment.NewLine,
        [
            "*!Total de sus apartados! - Bazar Test*",
            "",
            "Hola Ana 👋",
            "",
            "💰 *Total a pagar: $500.00*",
            "🚚 Entrega: *Sábado 08 de agosto*",
            "📅 *Límite de pago: Miércoles 05 de agosto a las 07:30 p.\u00A0m.*",
            "📦 Cierre semanal: Total de producto(s): 3",
            "Consulta tu listado de productos en el link:",
            "__UPLOAD_LINK__",
            "",
            // "Se entregará al recolector: Any Lopez",
            // "",
            "⚠️ NO ENVÍES TU COMPROBANTE DE COMPRA POR ESTE CHAT,",
            "ya que es automático y el bazar NO lo recibe.",
            "Solo cuenta si lo subes en el enlace.",
            "👇 Sube tu comprobante y consulta las tarjetas de pago en tu enlace personal (botón de abajo).",
            "",
            "Este número no pertenece al Bazar. Es un sistema automático",
        ]);
        Assert.Equal(expectedPreview, payload.Preview);
    }

    [Fact]
    public void Build_FallsBackToPendingCollectorLabelWhenMissing()
    {
        var payload = ClosureTotalsWhatsAppTemplate.Build(
            null,
            "Cliente",
            1m,
            new DateTime(2026, 8, 8),
            new DateTime(2026, 8, 5),
            null,
            null,
            0,
            "   ",
            "token");

        Assert.Equal(7, payload.BodyParameters.Count);
        Assert.Contains("Se entregará al recolector: Por asignar", payload.ManualPreview);
    }
}
