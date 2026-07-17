using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BusinessCloud.Infrastructure.Common.Services;

public class JwtTokenService
{
    private readonly IConfiguration _config;
    private readonly IdentityDbContext _identityDb;

    public JwtTokenService(IConfiguration config, IdentityDbContext identityDb)
    {
        _config = config;
        _identityDb = identityDb;
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Role, user.Role),
            new("tenant_id", user.TenantId ?? string.Empty),
            new("first_name", user.FirstName),
            new("last_name", user.LastName),
            new("must_change_password", user.MustChangePassword ? "true" : "false"),
            new("can_view_totals", (user.Role == "BazarUser" ? user.CanViewTotals : true) ? "true" : "false")
        };

        if (!string.IsNullOrWhiteSpace(user.AllowedModules))
        {
            claims.Add(new Claim("allowed_modules", user.AllowedModules));
        }

        if (user.SellerId.HasValue)
        {
            claims.Add(new Claim("seller_id", user.SellerId.Value.ToString()));
        }

        // Agregar módulos habilitados del tenant como claims
        var modules = await _identityDb.TenantModules
            .Where(tm => tm.TenantId == user.TenantId && tm.IsActive)
            .Select(tm => tm.Module)
            .ToListAsync();

        foreach (var module in modules)
        {
            claims.Add(new Claim("module", module));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expireMinutes = _config.GetValue<int>("Jwt:ExpireMinutes");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Mantener versión síncrona para compatibilidad (sin módulos)
    public string GenerateToken(ApplicationUser user)
    {
        return GenerateTokenAsync(user).GetAwaiter().GetResult();
    }
}
