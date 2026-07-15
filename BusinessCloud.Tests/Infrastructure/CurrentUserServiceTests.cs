using System.Security.Claims;
using BusinessCloud.Domain.Common.Exceptions;
using BusinessCloud.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Infrastructure;

/// <summary>
/// Pruebas del servicio de usuario actual (resolución multi-tenant desde los claims del JWT).
/// Crítico para la seguridad: la ausencia de TenantId debe bloquear el acceso.
/// </summary>
public class CurrentUserServiceTests
{
    private static CurrentUserService WithClaims(params Claim[] claims)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext);
        return new CurrentUserService(accessor.Object);
    }

    [Fact]
    public void ExtraeClaimsBasicosDelContexto()
    {
        var service = WithClaims(
            new Claim("tenant_id", "tenant-9"),
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim("seller_id", "42"));

        Assert.Equal("tenant-9", service.TenantId);
        Assert.Equal("user-1", service.UserId);
        Assert.Equal("SuperAdmin", service.Role);
        Assert.Equal(42, service.SellerId);
    }

    [Fact]
    public void SellerId_ConValorNoNumerico_DevuelveNull()
    {
        var service = WithClaims(new Claim("seller_id", "no-es-numero"));
        Assert.Null(service.SellerId);
    }

    [Fact]
    public void SellerId_SinClaim_DevuelveNull()
    {
        var service = WithClaims(new Claim("tenant_id", "tenant-9"));
        Assert.Null(service.SellerId);
    }

    [Fact]
    public void GetRequiredTenantId_ConTenant_DevuelveElValor()
    {
        var service = WithClaims(new Claim("tenant_id", "tenant-9"));
        Assert.Equal("tenant-9", service.GetRequiredTenantId());
    }

    [Fact]
    public void GetRequiredTenantId_SinTenant_LanzaTenantResolutionException()
    {
        var service = WithClaims(new Claim(ClaimTypes.NameIdentifier, "user-1"));
        Assert.Throws<TenantResolutionException>(() => service.GetRequiredTenantId());
    }

    [Fact]
    public void SinHttpContext_PropiedadesNulasYGetRequiredLanza()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var service = new CurrentUserService(accessor.Object);

        Assert.Null(service.TenantId);
        Assert.Null(service.UserId);
        Assert.Null(service.SellerId);
        Assert.Throws<TenantResolutionException>(() => service.GetRequiredTenantId());
    }
}
