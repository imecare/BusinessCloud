using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BusinessCloud.Tests.TestSupport;

/// <summary>
/// Fábrica de <see cref="BazaresDbContext"/> respaldado por el proveedor EF Core InMemory,
/// con un <see cref="ICurrentUserService"/> simulado que fija el TenantId. Permite probar los
/// handlers reales (con filtros globales de tenant, Include y consultas async) sin base de datos.
/// </summary>
public static class BazaresContextFactory
{
    public const string TenantId = "tenant-test";

    public static BazaresDbContext Create(string? tenantId = TenantId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(u => u.TenantId).Returns(tenantId);
        currentUser.Setup(u => u.GetRequiredTenantId()).Returns(tenantId ?? TenantId);

        var options = new DbContextOptionsBuilder<BazaresDbContext>()
            // Nombre único por instancia => aislamiento entre pruebas.
            .UseInMemoryDatabase($"bazares-{Guid.NewGuid():N}")
            .Options;

        return new BazaresDbContext(options, currentUser.Object);
    }
}
