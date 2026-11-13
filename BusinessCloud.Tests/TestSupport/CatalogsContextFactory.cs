using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BusinessCloud.Tests.TestSupport;

/// <summary>
/// Fábrica de <see cref="CatalogsDbContext"/> con EF Core InMemory. Permite crear varios contextos
/// sobre la misma base (distinto tenant) pasando el mismo <paramref name="databaseName"/>.
/// </summary>
public static class CatalogsContextFactory
{
    public const string TenantId = "tenant-cat";

    public static CatalogsDbContext Create(string? tenantId = TenantId, string? databaseName = null)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(u => u.TenantId).Returns(tenantId);
        currentUser.SetupGet(u => u.UserId).Returns("user-test");
        currentUser.Setup(u => u.GetRequiredTenantId()).Returns(tenantId ?? TenantId);

        var options = new DbContextOptionsBuilder<CatalogsDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"catalogs-{Guid.NewGuid():N}")
            .Options;

        return new CatalogsDbContext(options, currentUser.Object);
    }

    public static ICurrentUserService CurrentUser(string tenantId = TenantId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(u => u.TenantId).Returns(tenantId);
        currentUser.Setup(u => u.GetRequiredTenantId()).Returns(tenantId);
        return currentUser.Object;
    }
}
