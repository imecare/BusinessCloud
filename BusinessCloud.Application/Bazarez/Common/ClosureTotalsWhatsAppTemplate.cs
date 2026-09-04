using System.Globalization;
using System.Text;

namespace BusinessCloud.Application.Bazares.Common;

/// <summary>
/// Contrato único de la plantilla de WhatsApp usada por Envío de Totales.
/// Mantiene alineados los parámetros enviados a Meta y la vista previa del frontend.
/// </summary>
public static class ClosureTotalsWhatsAppTemplate
{
    private static readonly CultureInfo Culture = new("es-MX");

    public const string Name = "totales_cobro_v6";
    public const string UploadLinkPlaceholder = "__UPLOAD_LINK__";

    /// <summary>Nombre del recolector cuando el cliente todavía no tiene uno asignado.</summary>
    private const string NoCollectorLabel = "Por asignar";

    public static ClosureTotalsWhatsAppTemplatePayload Build(
        string? bazarName,
        string customerName,
        decimal totalAmount,
        DateTime deliveryDate,
        DateTime paymentDeadline,
        string? paymentCutoffTime,
        string? closureDescription,
        int productCount,
        string? collectorName,
        string buttonUrlParameter)
    {
        var header = string.IsNullOrWhiteSpace(bazarName) ? "Bazar" : bazarName.Trim();
        var customer = string.IsNullOrWhiteSpace(customerName) ? "Cliente" : customerName.Trim();
        var manualBodyParameters = new[]
        {
            $"{customer}",
            "$" + totalAmount.ToString("N2", Culture),
            FormatLongDate(deliveryDate),
            FormatDeadlineWithTime(paymentDeadline, paymentCutoffTime),
            string.IsNullOrWhiteSpace(closureDescription) ? "Cierre" : closureDescription.Trim(),
            productCount.ToString(Culture),
            string.IsNullOrWhiteSpace(collectorName) ? NoCollectorLabel : collectorName.Trim(),
        };

        // La plantilla v6 usa la URL del comprobante como séptimo parámetro del cuerpo
        // y el nombre del recolector como octavo parámetro.
        var templateBodyParameters = new[]
        {
            manualBodyParameters[0],
            manualBodyParameters[1],
            manualBodyParameters[2],
            manualBodyParameters[3],
            manualBodyParameters[4],
            manualBodyParameters[5],
            UploadLinkPlaceholder,
            manualBodyParameters[6],
        };

        return new ClosureTotalsWhatsAppTemplatePayload(
            Name,
            header,
                templateBodyParameters,
            buttonUrlParameter,
                BuildAutomaticPreview(header, templateBodyParameters),
                BuildManualPreview(header, manualBodyParameters));
    }

    private static string BuildAutomaticPreview(string header, IReadOnlyList<string> bodyParameters)
    {
        // Reconstrucción fiel del mensaje que Meta entrega al cliente (v6): encabezado, cuerpo
        // con los 7 parámetros, el recolector asignado y el aviso de "sistema automático". Se usa
        // tanto para la vista previa/copia manual como para la plantilla enviada por WhatsApp.
        var preview = new StringBuilder()
            .Append("*!Total de sus apartados! - ").Append(header).AppendLine("*")
            .AppendLine()
            .Append("Hola ").Append(bodyParameters[0]).AppendLine(" 👋")
            .AppendLine()
            .Append("💰 *Total a pagar: ").Append(bodyParameters[1]).AppendLine("*")
            .Append("🚚 Entrega: *").Append(bodyParameters[2]).AppendLine("*")
            .Append("📅 *Límite de pago: ").Append(bodyParameters[3]).AppendLine("*")
            .Append("📦 ").Append(bodyParameters[4]).Append(": Total de producto(s): ").AppendLine(bodyParameters[5])
            .AppendLine("Consulta tu listado de productos en el link:")
            .AppendLine(UploadLinkPlaceholder)
            .AppendLine()
            .Append("Se entregará al recolector: ").AppendLine(bodyParameters[7])
            .AppendLine()
            .AppendLine("⚠️ NO ENVÍES TU COMPROBANTE DE COMPRA POR ESTE CHAT,")
            .AppendLine("ya que es automático y el bazar NO lo recibe.")
            .AppendLine("Solo cuenta si lo subes en el enlace.")
            .AppendLine("👇 Sube tu comprobante y consulta las tarjetas de pago en tu enlace personal (botón de abajo).")
            .AppendLine()
            .Append("Este número no pertenece al Bazar. Es un sistema automático");

        return preview.ToString();
    }

    private static string BuildManualPreview(string header, IReadOnlyList<string> bodyParameters)
    {
        var preview = new StringBuilder()
            .Append("*!Total de sus apartados! - ").Append(header).AppendLine("*")
            .AppendLine()
            .Append("Hola ").Append(bodyParameters[0]).AppendLine(" 👋")
            .AppendLine()
            .Append("💰 *Total a pagar: ").Append(bodyParameters[1]).AppendLine("*")
            .Append("🚚 Entrega: *").Append(bodyParameters[2]).AppendLine("*")
            .Append("📅 *Límite de pago: ").Append(bodyParameters[3]).AppendLine("*")
            .Append("📦 ").Append(bodyParameters[4]).Append(": Total de producto(s): ").AppendLine(bodyParameters[5])
            .AppendLine("Consulta tu listado de productos en el link:")
            .AppendLine(UploadLinkPlaceholder)
            .AppendLine()
            .Append("Se entregará al recolector: ").AppendLine(bodyParameters[6])
            .AppendLine()
            .AppendLine("ENVÍA TU COMPROBANTE DE COMPRA POR ESTE CHAT")
            .AppendLine("👇 Sube tu comprobante y consulta las tarjetas de pago en tu enlace personal (botón de abajo).")
            .AppendLine()
            .Append("Este número no pertenece al Bazar. Es un sistema automático");

        return preview.ToString();
    }

    private static string FormatLongDate(DateTime date)
    {
        var text = date.ToString("dddd dd 'de' MMMM", Culture);
        return text.Length > 0 ? char.ToUpper(text[0], Culture) + text[1..] : text;
    }

    private static string FormatDeadlineWithTime(DateTime deadline, string? cutoffTime)
    {
        var date = FormatLongDate(deadline);

        if (deadline.Hour != 0 || deadline.Minute != 0)
        {
            return $"{date} a las {FormatTime(deadline.Hour, deadline.Minute)}";
        }

        var time = (cutoffTime ?? string.Empty).Trim();
        var match = System.Text.RegularExpressions.Regex.Match(time, "^([01]?[0-9]|2[0-3]):([0-5][0-9])$");
        if (!match.Success)
        {
            return date;
        }

        var hour = int.Parse(match.Groups[1].Value, Culture);
        var minute = int.Parse(match.Groups[2].Value, Culture);
        return $"{date} a las {FormatTime(hour, minute)}";
    }

    private static string FormatTime(int hour, int minute)
    {
        var reference = new DateTime(2000, 1, 1, hour, minute, 0);
        return reference.ToString("hh:mm tt", Culture);
    }
}

public sealed record ClosureTotalsWhatsAppTemplatePayload(
    string TemplateName,
    string HeaderParameter,
    IReadOnlyList<string> BodyParameters,
    string ButtonUrlParameter,
    string Preview,
    string ManualPreview);