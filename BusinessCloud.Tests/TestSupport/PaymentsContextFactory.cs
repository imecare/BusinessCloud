using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BusinessCloud.Tests.TestSupport;

/// <summary>
/// Fábrica de <see cref="PaymentsDbContext"/> con proveedor InMemory y un
/// <see cref="ICurrentUserService"/> simulado (TenantId fijo) para probar handlers del
/// módulo de pagos/comisiones sin base de datos real.
/// </summary>
public static class PaymentsContextFactory
{
    public const string TenantId = "tenant-test";

    public static PaymentsDbContext Create(string? tenantId = TenantId, string? databaseName = null)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(u => u.TenantId).Returns(tenantId);
        currentUser.Setup(u => u.GetRequiredTenantId()).Returns(tenantId ?? TenantId);

        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"payments-{Guid.NewGuid():N}")
            .Options;

        return new PaymentsDbContext(options, currentUser.Object);
    }
}
