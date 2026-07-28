namespace BusinessCloud.Application.Common.Interfaces;

/// <summary>
/// Servicio de PIN de seguridad del SuperAdmin.
/// Permite configurar y verificar el PIN que reemplaza el OTP de WhatsApp
/// para autorizar operaciones sensibles.
/// </summary>
public interface IAdminPinService
{
    /// <summary>
    /// Verifica que el PIN ingresado coincida con el hash almacenado del usuario.
    /// Devuelve false si el usuario no tiene PIN configurado o si el PIN es incorrecto.
    /// </summary>
    Task<bool> VerifyPinAsync(string userId, string pin, CancellationToken ct = default);

    /// <summary>
    /// Genera el hash del PIN y lo almacena en el perfil del usuario.
    /// Si ya tiene un PIN y se proporciona <paramref name="currentPin"/>, valida primero el actual.
    /// </summary>
    Task<(bool Success, string? Error)> SetPinAsync(string userId, string newPin, string? currentPin, CancellationToken ct = default);

    /// <summary>
    /// Indica si el usuario tiene un PIN configurado.
    /// </summary>
    Task<bool> HasPinAsync(string userId, CancellationToken ct = default);
}
