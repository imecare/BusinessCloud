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
    public const string UploadLinkPlaceholder = "__UPLOAD_LINK__";

    /// <summary>
    /// Construye el mensaje con el mismo formato que la plantilla de WhatsApp.
    /// </summary>
    public static string Build(
        string? bazarName,
        string customerName,
        decimal total,
        DateTime? deliveryDate,
        DateTime paymentDeadline,
        string? salesWhatsApp)
    {
        return ClosureTotalsWhatsAppTemplate.Build(
            bazarName,
            customerName,
            total,
            deliveryDate ?? paymentDeadline,
            paymentDeadline,
            null,
            null,
            0,
            null,
            UploadLinkPlaceholder).ManualPreview;
    }

    public static string? BuildWhatsAppLink(string? phone)
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

}
