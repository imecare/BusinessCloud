using System.Security.Claims;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace BusinessCloud.Tests.Api;

/// <summary>
/// Pruebas de la autorización por módulo: solo se concede acceso si el token contiene
/// un claim "module" con el módulo requerido.
/// </summary>
public class ModuleRequirementHandlerTests
{
    private static async Task<bool> Evaluate(string requiredModule, params string[] userModules)
    {
        var claims = userModules.Select(m => new Claim("module", m));
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var requirement = new ModuleRequirement(requiredModule);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);

        await new ModuleRequirementHandler().HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task Concede_CuandoElUsuarioTieneElModulo()
    {
        Assert.True(await Evaluate("Bazares", "Payments", "Bazares"));
    }

    [Fact]
    public async Task Deniega_CuandoElUsuarioNoTieneElModulo()
    {
        Assert.False(await Evaluate("Bazares", "Payments", "Commissions"));
    }

    [Fact]
    public async Task Deniega_CuandoNoHayModulos()
    {
        Assert.False(await Evaluate("Bazares"));
    }
}
