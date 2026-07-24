using System.Collections.Generic;

namespace BusinessCloud.Application.Common.Interfaces;

/// <summary>Resultado del envÃ­o de un mensaje de WhatsApp por la Cloud API.</summary>
public record WhatsAppSendResult(bool Success, string? MessageId, string? ErrorCode, string? ErrorMessage);

/// <summary>
/// EnvÃ­o de mensajes por WhatsApp (Meta Cloud API).
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>Indica si la integraciÃ³n estÃ¡ configurada (token + phone number id).</summary>
    bool IsConfigured { get; }

    /// <summary>EnvÃ­a un cÃ³digo de verificaciÃ³n OTP al nÃºmero indicado (formato E.164, con o sin '+').</summary>
    Task<bool> SendOtpAsync(string toPhone, string code, CancellationToken cancellationToken = default);

    /// <summary>EnvÃ­a un OTP y devuelve el detalle (id del mensaje / error) para registrar su estatus.</summary>
    Task<WhatsAppSendResult> SendOtpWithResultAsync(string toPhone, string code, CancellationToken cancellationToken = default);

    /// <summary>EnvÃ­a una plantilla aprobada de WhatsApp y devuelve el detalle del envÃ­o.</summary>
    Task<WhatsAppSendResult> SendTemplateWithResultAsync(
        string toPhone,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default,
        string? buttonUrlParameter = null);

    /// <summary>EnvÃ­a una plantilla aprobada de WhatsApp.</summary>
    Task<bool> SendTemplateAsync(
        string toPhone,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default,
        string? buttonUrlParameter = null);

    /// <summary>EnvÃ­a un mensaje de texto simple.</summary>
    Task<bool> SendTextAsync(string toPhone, string message, CancellationToken cancellationToken = default);

    /// <summary>EnvÃ­a un mensaje de texto y devuelve el detalle (id del mensaje / error) para registrar su estatus.</summary>
    Task<WhatsAppSendResult> SendTextWithResultAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}
