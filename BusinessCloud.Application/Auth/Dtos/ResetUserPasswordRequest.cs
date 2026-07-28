namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud del SuperAdmin para asignar una nueva contraseña temporal a un usuario.
/// El usuario deberá cambiarla en su próximo inicio de sesión.
/// </summary>
public class ResetUserPasswordRequest
{
    public string TemporaryPassword { get; set; } = null!;

    /// <summary>Identificador del desafío OTP (obtenido de verification/request). Alternativa al PIN.</summary>
    public string? ChallengeId { get; set; }

    /// <summary>Código de verificación recibido por WhatsApp. Alternativa al PIN.</summary>
    public string? VerificationCode { get; set; }

    /// <summary>PIN de seguridad del SuperAdmin. Alternativa al código OTP de WhatsApp.</summary>
    public string? AdminPin { get; set; }
}
