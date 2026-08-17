namespace BusinessCloud.Application.Bazares.Common;

/// <summary>
/// Arma el texto de cobro que se COPIA A MEMORIA / se envía manualmente (WhatsApp o Messenger)
/// para un cliente de un cierre. A diferencia del envío automático por plantilla de Meta —que
/// debe respetar <c>WhatsApp:ClosureTotalsTemplateName</c> para no fallar si la plantilla aún no
/// está activa— este texto SIEMPRE usa el formato de la última versión de la plantilla
/// (<see cref="ClosureTotalsWhatsAppTemplate"/>), con la única diferencia de que el enlace del
/// comprobante va en el cuerpo (marcador <c>__UPLOAD_LINK__</c>) en lugar de un botón.
/// Copiar texto nunca produce error de Meta, por eso se unifica de una vez.
/// </summary>
public static class ClosureCopyMessageBuilder
{
    /// <summary>
    /// Construye el texto de cobro (con <c>__UPLOAD_LINK__</c>) con el formato de la plantilla
    /// más reciente. El frontend reemplaza <c>__UPLOAD_LINK__</c> por la URL pública del comprobante.
    /// </summary>
    public static string BuildLatest(
        string? bazarName,
        string customerName,
        decimal totalAmount,
        System.DateTime? deliveryDate,
        System.DateTime paymentDeadline,
        string? paymentCutoffTime,
        string? closureDescription,
        int productCount,
        System.Collections.Generic.IReadOnlyList<string> productNames,
        string uploadToken)
        => ClosureTotalsWhatsAppTemplate.Build(
            bazarName,
            customerName,
            totalAmount,
            deliveryDate ?? paymentDeadline,
            paymentDeadline,
            paymentCutoffTime,
            closureDescription,
            productCount,
            productNames,
            uploadToken).Preview;
}
