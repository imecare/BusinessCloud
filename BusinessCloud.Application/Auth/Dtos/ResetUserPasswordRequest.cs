namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud del SuperAdmin para asignar una nueva contraseña temporal a un usuario.
/// El usuario deberá cambiarla en su próximo inicio de sesión.
/// </summary>
public class ResetUserPasswordRequest
{
    public string TemporaryPassword { get; set; } = null!;

    /// <summary>Identificador del desafío OTP (obtenido de verification/request).</summary>
    public string? ChallengeId { get; set; }

    /// <summary>Código de verificación recibido por WhatsApp.</summary>
    public string? VerificationCode { get; set; }
}
