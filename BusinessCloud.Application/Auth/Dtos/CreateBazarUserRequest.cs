namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud para crear un usuario del bazar (rol "BazarUser") con permisos por módulo
/// y una contraseña temporal que deberá cambiar en el primer inicio de sesión.
/// </summary>
public class CreateBazarUserRequest
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>Teléfono de contacto del usuario (para futura verificación por WhatsApp).</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Contraseña temporal asignada por el SuperAdmin.</summary>
    public string TemporaryPassword { get; set; } = null!;

    /// <summary>Claves de los módulos/secciones que el usuario podrá ver.</summary>
    public string[] AllowedModules { get; set; } = System.Array.Empty<string>();

    /// <summary>Permite ver los totales de venta. Por defecto true.</summary>
    public bool CanViewTotals { get; set; } = true;

    /// <summary>Identificador del desafío OTP (obtenido de verification/request).</summary>
    public string? ChallengeId { get; set; }

    /// <summary>Código de verificación recibido por WhatsApp.</summary>
    public string? VerificationCode { get; set; }
}
