using BusinessCloud.Application.Auth.Dtos;
using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Common.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IdentityDbContext _identityContext;
    private readonly JwtTokenService _jwtService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IdentityDbContext identityContext,
        JwtTokenService jwtService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _identityContext = identityContext;
        _jwtService = jwtService;
    }

    [HttpPost("register-company")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // 1. Iniciamos una transacción para que si falla el usuario, no se cree la empresa (y viceversa)
        using var transaction = await _identityContext.Database.BeginTransactionAsync();

        try
        {
            // 2. Crear el Tenant
            var tenantId = Guid.NewGuid().ToString().Substring(0, 8); // Generamos un ID corto y único
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = request.CompanyName
            };

            _identityContext.Tenants.Add(tenant);
            await _identityContext.SaveChangesAsync();

            // 3. Crear el Usuario vinculado al Tenant
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = tenantId // Vinculación Multi-tenant
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                Message = "Empresa y Usuario creados con éxito",
                TenantId = tenantId
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            return Unauthorized("Credenciales inválidas");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
            return Unauthorized("Credenciales inválidas");

        // Generamos el Token con el TenantId incluido en los Claims
        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            Token = token,
            User = new { user.FirstName, user.LastName, user.Email, user.TenantId }
        });
    }
}