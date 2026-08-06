using BusinessCloud.Domain.Common.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Api.Controllers.Admin;

/// <summary>
/// Utilidad SOLO para entorno de desarrollo: permite crear o actualizar de forma
/// local el usuario PlatformAdmin (super administrador del panel FrontAdmin) sin
/// depender del seeding ni de tocar la base de datos a mano.
///
/// Todos los endpoints devuelven 404 fuera de Development para no quedar expuestos
/// en produccion.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/dev/platform-admin")]
public class DevPlatformAdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public DevPlatformAdminController(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DevPlatformAdminCreateRequest request)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Email y contrasena son obligatorios." });

        if (request.Password.Length < 6)
            return BadRequest(new { success = false, message = "La contrasena debe tener al menos 6 caracteres." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { success = false, message = "Ya existe un usuario con ese email. Usa PUT /password para actualizar la contrasena." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? "Platform" : request.FirstName,
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? "Admin" : request.LastName,
            TenantId = string.Empty,
            Role = SystemRoles.PlatformAdmin,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return Ok(new { success = true, message = "PlatformAdmin creado.", email = user.Email });
    }

    [HttpPut("password")]
    public async Task<IActionResult> UpdatePassword([FromBody] DevPlatformAdminUpdatePasswordRequest request)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { success = false, message = "Email y nueva contrasena son obligatorios." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { success = false, message = "La contrasena debe tener al menos 6 caracteres." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.Role != SystemRoles.PlatformAdmin)
            return NotFound(new { success = false, message = "No existe un PlatformAdmin con ese email." });

        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        user.IsActive = true;
        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok(new { success = true, message = "Contrasena del PlatformAdmin actualizada.", email = user.Email });
    }

    [HttpPut("company-password")]
    public async Task<IActionResult> UpdateCompanyPassword([FromBody] DevCompanyAdminUpdatePasswordRequest request)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var tenantId = (request.TenantId ?? string.Empty).Trim();
        var password = request.NewPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { success = false, message = "Email, tenantId y nueva contrasena son obligatorios." });

        if (password.Length < 6)
            return BadRequest(new { success = false, message = "La contrasena debe tener al menos 6 caracteres." });

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email && u.TenantId == tenantId && u.Role == SystemRoles.SuperAdmin);
        if (user is null)
            return NotFound(new { success = false, message = "No existe un SuperAdmin para ese email y tenant." });

        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, password);
        if (!result.Succeeded)
            return BadRequest(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        user.IsActive = true;
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            success = true,
            message = "Contrasena provisional asignada.",
            email = user.Email,
            tenantId = user.TenantId
        });
    }
}

public sealed record DevPlatformAdminCreateRequest(
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null);

public sealed record DevPlatformAdminUpdatePasswordRequest(
    string Email,
    string NewPassword);

public sealed record DevCompanyAdminUpdatePasswordRequest(
    string Email,
    string TenantId,
    string NewPassword);

