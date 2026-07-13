namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud para activar/desactivar (deshabilitar) un usuario.
/// </summary>
public class SetUserStatusRequest
{
    public bool IsActive { get; set; }

    /// <summary>Identificador del desafío OTP (obtenido de verification/request).</summary>
    public string? ChallengeId { get; set; }

    /// <summary>Código de verificación recibido por WhatsApp.</summary>
    public string? VerificationCode { get; set; }
}
