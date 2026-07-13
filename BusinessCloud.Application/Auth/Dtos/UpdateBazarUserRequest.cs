namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud para actualizar los datos y permisos de un usuario del bazar.
/// </summary>
public class UpdateBazarUserRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string[] AllowedModules { get; set; } = System.Array.Empty<string>();
    public bool CanViewTotals { get; set; } = true;
    public bool IsActive { get; set; } = true;

    /// <summary>Identificador del desafío OTP (obtenido de verification/request).</summary>
    public string? ChallengeId { get; set; }

    /// <summary>Código de verificación recibido por WhatsApp.</summary>
    public string? VerificationCode { get; set; }
}
