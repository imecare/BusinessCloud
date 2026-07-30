namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud publica (sin autenticar) para iniciar el flujo de recuperacion de
/// contrasena. Genera un codigo OTP que se envia por WhatsApp al numero autorizado.
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>Correo de la cuenta cuya contrasena se desea restablecer.</summary>
    public string Email { get; set; } = null!;
}
