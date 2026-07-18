using BusinessCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Tests.TestSupport;

/// <summary>
/// Fábrica de <see cref="IdentityDbContext"/> respaldado por el proveedor EF Core InMemory.
/// El contexto de identidad no aplica filtro multi-tenant, por lo que no requiere un
/// <c>ICurrentUserService</c> simulado.
/// </summary>
public static class IdentityContextFactory
{
    public static IdentityDbContext Create()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;

        return new IdentityDbContext(options);
    }
}
