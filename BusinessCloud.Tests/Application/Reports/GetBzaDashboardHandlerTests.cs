using BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Reports;

/// <summary>
/// Pruebas del dashboard: conteos, KPIs semanales (ventas, cobrado, pendiente), volumen por
/// recolector y clientes morosos. Solo cuentan como cobrado los pagos verificados.
/// </summary>
public class GetBzaDashboardHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    /// <summary>Construye el handler del dashboard con un IdentityDbContext InMemory vacío y el tenant de prueba.</summary>
    private static GetBzaDashboardHandler CreateHandler(BazaresDbContext ctx)
    {
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;
        var identity = new IdentityDbContext(identityOptions);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(u => u.TenantId).Returns(Tenant);

        return new GetBzaDashboardHandler(ctx, identity, currentUser.Object);
    }

    private static async Task SeedAsync(BazaresDbContext ctx)
    {
        var today = DateTime.UtcNow.Date;

        var group = new BzaCollectorGroup { Id = 1, TenantId = Tenant, Description = "Grupo", IsActive = true };
        var collector = new BzaCollector { Id = 1, TenantId = Tenant, Name = "Rec", IsActive = true, BzaCollectorGroupId = 1, CollectorGroup = group };
        var ana = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana", Phone = "5510000001", BzaCollectorId = 1, Collector = collector };
        var beto = new BzaCustomer { Id = 2, TenantId = Tenant, Name = "Beto", Phone = "5510000002", BzaCollectorId = 1, Collector = collector };
        ctx.Customers.AddRange(ana, beto);

        var closedEvent = new BzaEvent
        {
            Id = 1,
            TenantId = Tenant,
            Description = "Evento",
            Status = 1, // abierto y con fecha límite vencida => moroso
            PaymentDeadline = today.AddDays(-1),
            Sales = new List<BzaSale>
            {
                new()
                {
                    Id = 1, TenantId = Tenant, BzaEventId = 1, BzaCustomerId = 1, Customer = ana,
                    Products = new List<BzaSoldProduct>
                    {
                        new() { Id = 1, TenantId = Tenant, Description = "A", Price = 100m },
                        new() { Id = 2, TenantId = Tenant, Description = "B", Price = 50m },
                    },
                },
                new()
                {
                    Id = 2, TenantId = Tenant, BzaEventId = 1, BzaCustomerId = 2, Customer = beto,
                    Products = new List<BzaSoldProduct> { new() { Id = 3, TenantId = Tenant, Description = "C", Price = 200m } },
                },
            },
            Payments = new List<BzaPayment>
            {
                new() { Id = 1, TenantId = Tenant, BzaEventId = 1, BzaCustomerId = 1, Amount = 100m, IsVerified = true },
                new() { Id = 2, TenantId = Tenant, BzaEventId = 1, BzaCustomerId = 2, Amount = 999m, IsVerified = false },
            },
        };
        ctx.Events.Add(closedEvent);

        await ctx.SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_CalculaConteosYKpisSemanales()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx);

        var dto = await CreateHandler(ctx).Handle(new GetBzaDashboardQuery(), default);

        Assert.Equal(2, dto.TotalCustomers);
        Assert.Equal(1, dto.TotalCollectors);
        Assert.Equal(350m, dto.WeeklySales);      // 100 + 50 + 200
        Assert.Equal(100m, dto.TotalPaid);         // solo el pago verificado
        Assert.Equal(250m, dto.TotalPending);      // 350 - 100
    }

    [Fact]
    public async Task Handle_AgrupaVolumenPorRecolector()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx);

        var dto = await CreateHandler(ctx).Handle(new GetBzaDashboardQuery(), default);

        var vol = Assert.Single(dto.CollectorVolumes);
        Assert.Equal(1, vol.CollectorId);
        Assert.Equal(2, vol.CustomerCount);
        Assert.Equal(350m, vol.TotalSales);
        Assert.Equal(100m, vol.TotalCollected);
    }

    [Fact]
    public async Task Handle_IdentificaMorososOrdenadosPorSaldo()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx);

        var dto = await CreateHandler(ctx).Handle(new GetBzaDashboardQuery(), default);

        Assert.Equal(2, dto.DelinquentsCount);
        // Ordenados por saldo descendente: Beto (200) antes que Ana (50).
        Assert.Equal("Beto", dto.Delinquents[0].CustomerName);
        Assert.Equal(200m, dto.Delinquents[0].Balance);
        Assert.Equal("Ana", dto.Delinquents[1].CustomerName);
        Assert.Equal(50m, dto.Delinquents[1].Balance);
    }
}
