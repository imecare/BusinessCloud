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

    public const string Name = "totales_cobro_v2";
    public const string UploadLinkPlaceholder = "__UPLOAD_LINK__";

    public static ClosureTotalsWhatsAppTemplatePayload Build(
        string? bazarName,
        string customerName,
        decimal totalAmount,
        DateTime deliveryDate,
        DateTime paymentDeadline,
        string? paymentCutoffTime,
        string? closureDescription,
        int productCount,
        IReadOnlyList<string> productNames,
        string buttonUrlParameter)
    {
        var header = string.IsNullOrWhiteSpace(bazarName) ? "Bazar" : bazarName.Trim();
        var bodyParameters = new[]
        {
            string.IsNullOrWhiteSpace(customerName) ? "Cliente" : customerName.Trim(),
            "$" + totalAmount.ToString("N2", Culture),
            FormatLongDate(deliveryDate),
            FormatDeadlineWithTime(paymentDeadline, paymentCutoffTime),
            string.IsNullOrWhiteSpace(closureDescription) ? "Cierre" : closureDescription.Trim(),
            productCount.ToString(Culture),
            FormatProductNames(productNames),
        };

        return new ClosureTotalsWhatsAppTemplatePayload(
            Name,
            header,
            bodyParameters,
            buttonUrlParameter,
            BuildPreview(header, bodyParameters));
    }

    private static string BuildPreview(string header, IReadOnlyList<string> bodyParameters)
    {
        // Reconstrucción fiel del mensaje real (plantilla totales_cobro_v4): encabezado,
        // cuerpo con los 7 parámetros y negritas de la plantilla, y el pie de página.
        var preview = new StringBuilder()
            .Append("Aviso de pago de ").Append(header).AppendLine(" (mensaje automático)")
            .AppendLine()
            .Append("Hola ").Append(bodyParameters[0]).AppendLine(" 👋")
            .AppendLine()
            .Append("💰 Total a pagar: *").Append(bodyParameters[1]).AppendLine("*")
            .Append("🚚 Entrega: *").Append(bodyParameters[2]).AppendLine("*")
            .Append("📅 Límite de pago: *").Append(bodyParameters[3]).AppendLine("*")
            .Append("📦 ").Append(bodyParameters[4]).Append(": *Total de producto(s) · ")
                .Append(bodyParameters[5]).Append("* - (").Append(bodyParameters[6]).AppendLine(")")
            .AppendLine()
            .AppendLine("⚠️ NO ENVIÉS TU COMPROBANTE DE COMPRA POR ESTE CHAT: es automático y el bazar NO lo recibe. Solo cuenta si lo subes en el enlace.")
            .AppendLine("👇 Sube tu comprobante y consulta las tarjetas de pago en tu enlace personal (botón de abajo).")
            .AppendLine(UploadLinkPlaceholder)
            .AppendLine()
            .Append("Sistema automático · No respondas por este chat");

        return preview.ToString();
    }

    private static string FormatProductNames(IReadOnlyList<string> productNames)
    {
        const int maxNames = 8;
        var normalizedNames = productNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToList();

        if (normalizedNames.Count == 0)
        {
            return "—";
        }

        var displayedNames = normalizedNames.Take(maxNames).ToList();
        if (normalizedNames.Count > maxNames)
        {
            displayedNames.Add($"… y {normalizedNames.Count - maxNames} más");
        }

        return string.Join(", ", displayedNames);
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
    string Preview);