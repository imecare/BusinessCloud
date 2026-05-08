using Microsoft.AspNetCore.Authorization;

namespace BusinessCloud.Api.Authorization;

/// <summary>
/// Requiere que el tenant del usuario tenga habilitado el módulo especificado.
/// Uso: [RequireModule("Bazares")] en un controller o action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireModuleAttribute : AuthorizeAttribute
{
    public RequireModuleAttribute(string module)
        : base(policy: $"Module_{module}")
    {
    }
}
