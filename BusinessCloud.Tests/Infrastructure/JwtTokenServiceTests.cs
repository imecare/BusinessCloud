using System.IdentityModel.Tokens.Jwt;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Common.Services;
using BusinessCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BusinessCloud.Tests.Infrastructure;

/// <summary>
/// Pruebas de la generación del token JWT: claims de identidad/tenant, permiso de ver
/// totales según el rol y la inclusión únicamente de los módulos activos del tenant.
/// </summary>
public class JwtTokenServiceTests
{
    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-super-secreta-de-pruebas-con-mas-de-32-bytes-1234567890",
            ["Jwt:Issuer"] = "BusinessCloud.Api",
            ["Jwt:Audience"] = "BusinessCloud.App",
            ["Jwt:ExpireMinutes"] = "120",
        })
        .Build();

    private static IdentityDbContext IdentityDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }

    private static ApplicationUser User(string role = "BazarUser", bool canViewTotals = true,
        string? allowedModules = null, int? sellerId = null)
    {
        return new ApplicationUser
        {
            Id = "user-1",
            Email = "user@empresa.com",
            UserName = "user@empresa.com",
            FirstName = "Ana",
            LastName = "Perez",
            Role = role,
            TenantId = "tenant-9",
            CanViewTotals = canViewTotals,
            AllowedModules = allowedModules,
            SellerId = sellerId,
        };
    }

    private static JwtSecurityToken Decode(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public async Task GenerateToken_IncluyeClaimsDeIdentidadYTenant()
    {
        using var db = IdentityDb();
        var token = Decode(await new JwtTokenService(Config(), db).GenerateTokenAsync(User(sellerId: 42)));

        Assert.Equal("BusinessCloud.Api", token.Issuer);
        Assert.Contains("BusinessCloud.App", token.Audiences);
        Assert.Equal("tenant-9", token.Claims.First(c => c.Type == "tenant_id").Value);
        Assert.Equal("42", token.Claims.First(c => c.Type == "seller_id").Value);
        Assert.Equal("Ana", token.Claims.First(c => c.Type == "first_name").Value);
    }

    [Fact]
    public async Task GenerateToken_BazarUserSinPermiso_CanViewTotalsEsFalse()
    {
        using var db = IdentityDb();
        var token = Decode(await new JwtTokenService(Config(), db)
            .GenerateTokenAsync(User(role: "BazarUser", canViewTotals: false)));

        Assert.Equal("false", token.Claims.First(c => c.Type == "can_view_totals").Value);
    }

    [Fact]
    public async Task GenerateToken_NoBazarUser_CanViewTotalsSiempreTrue()
    {
        using var db = IdentityDb();
        // Aunque CanViewTotals sea false, un rol distinto de BazarUser siempre puede ver totales.
        var token = Decode(await new JwtTokenService(Config(), db)
            .GenerateTokenAsync(User(role: "SuperAdmin", canViewTotals: false)));

        Assert.Equal("true", token.Claims.First(c => c.Type == "can_view_totals").Value);
    }

    [Fact]
    public async Task GenerateToken_IncluyeSoloModulosActivosDelTenant()
    {
        using var db = IdentityDb();
        db.TenantModules.AddRange(
            new TenantModule { Id = 1, TenantId = "tenant-9", Module = "Bazares", IsActive = true },
            new TenantModule { Id = 2, TenantId = "tenant-9", Module = "Payments", IsActive = false }, // inactivo
            new TenantModule { Id = 3, TenantId = "otro-tenant", Module = "Commissions", IsActive = true }); // otro tenant
        await db.SaveChangesAsync();

        var token = Decode(await new JwtTokenService(Config(), db).GenerateTokenAsync(User()));

        var modules = token.Claims.Where(c => c.Type == "module").Select(c => c.Value).ToList();
        Assert.Contains("Bazares", modules);
        Assert.DoesNotContain("Payments", modules);
        Assert.DoesNotContain("Commissions", modules);
    }

    [Fact]
    public async Task GenerateToken_ConModulosPermitidos_AgregaClaimAllowedModules()
    {
        using var db = IdentityDb();
        var token = Decode(await new JwtTokenService(Config(), db)
            .GenerateTokenAsync(User(allowedModules: "sales,reports")));

        Assert.Equal("sales,reports", token.Claims.First(c => c.Type == "allowed_modules").Value);
    }
}
