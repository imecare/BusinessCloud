using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using Microsoft.AspNetCore.Identity;

namespace BusinessCloud.Infrastructure.Common.Services;

/// <summary>
/// Implementacion de IAdminPinService usando UserManager + IPasswordHasher (PBKDF2).
/// </summary>
public class AdminPinService(
    UserManager<ApplicationUser> userManager,
    IPasswordHasher<ApplicationUser> passwordHasher) : IAdminPinService
{
    public async Task<bool> VerifyPinAsync(string userId, string pin, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user?.AdminSecurityPinHash == null) return false;
        var result = passwordHasher.VerifyHashedPassword(user, user.AdminSecurityPinHash, pin);
        return result != PasswordVerificationResult.Failed;
    }

    public async Task<(bool Success, string? Error)> SetPinAsync(
        string userId, string newPin, string? currentPin, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return (false, "Usuario no encontrado.");

        // Si ya tiene PIN y se proporciona currentPin, valida el actual primero.
        if (user.AdminSecurityPinHash != null && currentPin != null)
        {
            var check = passwordHasher.VerifyHashedPassword(user, user.AdminSecurityPinHash, currentPin);
            if (check == PasswordVerificationResult.Failed)
                return (false, "El PIN actual es incorrecto.");
        }

        user.AdminSecurityPinHash = passwordHasher.HashPassword(user, newPin);
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, string.Join(" ", updateResult.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<bool> HasPinAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return !string.IsNullOrEmpty(user?.AdminSecurityPinHash);
    }
}
