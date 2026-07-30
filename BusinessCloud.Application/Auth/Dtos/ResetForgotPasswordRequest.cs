namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud publica (sin autenticar) para confirmar el codigo OTP recibido por
/// WhatsApp y asignar una nueva contrasena a la cuenta indicada.
/// </summary>
public class ResetForgotPasswordRequest
{
    /// <summary>Correo de la cuenta cuya contrasena se desea restablecer.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Identificador del desafio OTP obtenido de forgot-password/request.</summary>
    public string ChallengeId { get; set; } = null!;

    /// <summary>Codigo de verificacion recibido por WhatsApp.</summary>
    public string VerificationCode { get; set; } = null!;

    /// <summary>Nueva contrasena que reemplazara a la actual.</summary>
    public string NewPassword { get; set; } = null!;
}
