namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud del usuario autenticado para cambiar su propia contraseña
/// (contraseña actual + nueva contraseña).
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
