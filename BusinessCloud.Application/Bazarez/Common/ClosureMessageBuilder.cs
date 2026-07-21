using System.Globalization;
using System.Text;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Common;

/// <summary>Producto incluido en un mensaje de totales.</summary>
public sealed record ClosureMessageProduct(string Description, decimal Price);

/// <summary>Venta (evento) incluida en un mensaje de totales, con sus productos.</summary>
public sealed record ClosureMessageSale(string EventDescription, decimal Amount, IReadOnlyList<ClosureMessageProduct> Products);

/// <summary>
/// Construye el mensaje de cobro (Envío de Totales) que se comparte con el cliente.
/// Se usa tanto al enviar los totales como al reconstruir el mensaje para reenviarlo
/// desde el detalle del cierre, garantizando un formato idéntico.
/// El enlace del comprobante se representa con el marcador <c>__UPLOAD_LINK__</c>,
/// que el frontend reemplaza por la URL pública del cliente.
/// </summary>
public static class ClosureMessageBuilder
{
    private static readonly CultureInfo Culture = new("es-MX");

    public const string UploadLinkPlaceholder = "__UPLOAD_LINK__";

    /// <summary>
    /// Construye el mensaje corto de cobro (compatible con plantilla de utilidad):
    /// nombre del bazar, saludo con el nombre del cliente, total, fecha límite y un
    /// enlace donde el cliente consulta tarjetas, sube su comprobante y ve el detalle.
    /// </summary>
    public static string Build(
        string? bazarName,
        string customerName,
        decimal total,
        DateTime? deliveryDate,
        DateTime paymentDeadline,
        string? salesWhatsApp)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(bazarName))
        {
            sb.Append('*').Append(bazarName.Trim()).AppendLine("*");
        }

        sb.Append("Hola ").Append(customerName).AppendLine(" 👋");
        sb.AppendLine();
        sb.Append("💰 Total a pagar: ").Append(Money(total)).AppendLine();

        if (deliveryDate.HasValue)
        {
            sb.Append("🚚 Fecha de entrega: ").AppendLine(FormatLongDate(deliveryDate.Value));
        }

        sb.Append("📅 Fecha límite de pago: ").AppendLine(FormatLongDate(paymentDeadline));
        sb.AppendLine();
        sb.AppendLine("Para consultar las tarjetas de pago, subir tu comprobante y ver el detalle de tu pedido, entra aquí:");
        sb.AppendLine(UploadLinkPlaceholder);

        return sb.ToString().TrimEnd();
    }

    private static string? BuildWhatsAppLink(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        // Si llega en formato nacional (10 dígitos), asumir MX para wa.me.
        if (digits.Length == 10)
        {
            digits = "52" + digits;
        }

        return $"https://wa.me/{digits}";
    }

    private static string FormatLongDate(DateTime date)
    {
        var text = date.ToString("dddd dd 'de' MMMM", Culture);
        return text.Length > 0 ? char.ToUpper(text[0], Culture) + text[1..] : text;
    }

    private static string Money(decimal amount) => amount.ToString("C", Culture);
}
