namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud para configurar o cambiar el PIN de seguridad del SuperAdmin.
/// </summary>
public class SetSecurityPinRequest
{
    /// <summary>Nuevo PIN numérico (4-8 dígitos).</summary>
    public string NewPin { get; set; } = null!;

    /// <summary>PIN actual (obligatorio si ya se tiene uno configurado).</summary>
    public string? CurrentPin { get; set; }
}
