namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud del SuperAdmin para enviar un código de verificación por WhatsApp
/// antes de ejecutar una operación sensible sobre usuarios.
/// </summary>
public class RequestVerificationRequest
{
    /// <summary>
    /// Propósito de la verificación: "user.create", "user.update",
    /// "user.status" o "user.reset-password".
    /// </summary>
    public string Purpose { get; set; } = null!;
}
