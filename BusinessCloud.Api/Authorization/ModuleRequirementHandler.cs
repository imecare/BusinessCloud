using Microsoft.AspNetCore.Authorization;

namespace BusinessCloud.Api.Authorization;

/// <summary>
/// Requirement que valida que el claim "module" contenga el módulo solicitado.
/// </summary>
public class ModuleRequirement : IAuthorizationRequirement
{
    public string Module { get; }
    public ModuleRequirement(string module) => Module = module;
}

public class ModuleRequirementHandler : AuthorizationHandler<ModuleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModuleRequirement requirement)
    {
        var modules = context.User.FindAll("module").Select(c => c.Value);

        if (modules.Contains(requirement.Module))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
