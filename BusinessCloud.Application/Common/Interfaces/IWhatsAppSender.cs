namespace BusinessCloud.Application.Common.Interfaces;

/// <summary>Resultado del envío de un mensaje de WhatsApp por la Cloud API.</summary>
public record WhatsAppSendResult(bool Success, string? MessageId, string? ErrorCode, string? ErrorMessage);

/// <summary>
/// Envío de mensajes por WhatsApp (Meta Cloud API).
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>Indica si la integración está configurada (token + phone number id).</summary>
    bool IsConfigured { get; }

    /// <summary>Envía un código de verificación OTP al número indicado (formato E.164, con o sin '+').</summary>
    Task<bool> SendOtpAsync(string toPhone, string code, CancellationToken cancellationToken = default);

    /// <summary>Envía un OTP y devuelve el detalle (id del mensaje / error) para registrar su estatus.</summary>
    Task<WhatsAppSendResult> SendOtpWithResultAsync(string toPhone, string code, CancellationToken cancellationToken = default);

    /// <summary>Envía un mensaje de texto simple.</summary>
    Task<bool> SendTextAsync(string toPhone, string message, CancellationToken cancellationToken = default);

    /// <summary>Envía un mensaje de texto y devuelve el detalle (id del mensaje / error) para registrar su estatus.</summary>
    Task<WhatsAppSendResult> SendTextWithResultAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}
