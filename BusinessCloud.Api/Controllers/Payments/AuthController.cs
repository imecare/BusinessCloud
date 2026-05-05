using BusinessCloud.Application.Auth.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Common.Services;
using Microsoft.AspNetCore.Authorization;
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
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentsDbContext _paymentsDb;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IdentityDbContext identityContext,
        JwtTokenService jwtService,
        ICurrentUserService currentUser,
        IPaymentsDbContext paymentsDb)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _identityContext = identityContext;
        _jwtService = jwtService;
        _currentUser = currentUser;
        _paymentsDb = paymentsDb;
    }

    [HttpPost("register-company")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        using var transaction = await _identityContext.Database.BeginTransactionAsync();

        try
        {
            var tenantId = Guid.NewGuid().ToString().Substring(0, 8);
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = request.CompanyName
            };

            _identityContext.Tenants.Add(tenant);
            await _identityContext.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = tenantId,
                Role = "SuperAdmin",
                IsActive = true
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

        if (!user.IsActive)
            return Unauthorized("Usuario desactivado. Contacte al administrador.");

        if (user.Role == "Commissionist" && !user.SellerId.HasValue)
            return BadRequest(new { success = false, message = "Comisionista sin vendedor asignado. Contacte al administrador." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
            return Unauthorized("Credenciales inválidas");

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            user.Email,
            user.Role,
            user.FirstName,
            user.LastName,
            user.SellerId,
            user.TenantId,
            user.IsActive
        });
    }

    /// <summary>
    /// Crear usuario comisionista vinculado a un Seller existente.
    /// FirstName/LastName se copian automáticamente del Seller.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("commissionists")]
    public async Task<IActionResult> CreateCommissionist([FromBody] CreateCommissionistRequest request)
    {
        var tenantId = _currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized(new { success = false, message = "No se pudo determinar la empresa." });

        // 1. Validar que el Seller exista en el tenant actual
        var seller = await _paymentsDb.Sellers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SellerId);

        if (seller is null)
            return BadRequest(new { success = false, message = "El vendedor (SellerId) no existe en su empresa." });

        // 2. Validar email no duplicado
        var emailExists = await _userManager.FindByEmailAsync(request.Email);
        if (emailExists is not null)
            return BadRequest(new { success = false, message = "El email ya está registrado." });

        // 3. Validar que no haya otro comisionista con ese SellerId
        var duplicateSeller = await _userManager.Users
            .AnyAsync(u => u.TenantId == tenantId && u.SellerId == request.SellerId);

        if (duplicateSeller)
            return Conflict(new { success = false, message = "Ya existe un usuario comisionista para ese vendedor." });

        // 4. Crear usuario copiando FirstName/LastName del Seller
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = seller.Name,
            LastName = seller.LastName,
            TenantId = tenantId,
            Role = "Commissionist",
            SellerId = request.SellerId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetCommissionists), null, new
        {
            Id = user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.SellerId,
            user.IsActive
        });
    }

    /// <summary>
    /// Listar todos los comisionistas del tenant.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("commissionists")]
    public async Task<IActionResult> GetCommissionists()
    {
        var tenantId = _currentUser.TenantId;

        var commissionists = await _userManager.Users
            .Where(u => u.TenantId == tenantId && u.Role == "Commissionist")
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.SellerId,
                u.IsActive
            })
            .ToListAsync();

        return Ok(commissionists);
    }

    /// <summary>
    /// Activar/desactivar usuario comisionista.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("commissionists/{id}/status")]
    public async Task<IActionResult> UpdateCommissionistStatus(string id, [FromBody] UpdateCommissionistStatusRequest request)
    {
        var tenantId = _currentUser.TenantId;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "Commissionist");

        if (user is null)
            return NotFound(new { success = false, message = "Comisionista no encontrado." });

        user.IsActive = request.IsActive;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            Message = request.IsActive ? "Comisionista activado." : "Comisionista desactivado.",
            UserId = user.Id,
            user.IsActive
        });
    }
}