using BusinessCloud.Application.Bazares.Commands.CommitBzaImport;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

/// <summary>
/// Pruebas del commit de importacion: cuando un cliente nuevo tiene el telefono ya
/// registrado, el registro NO se pierde: se conservan sus datos y productos en
/// <see cref="CommitBzaImportResult.FailedRecords"/> para corregir y reintentar.
/// </summary>
public class CommitBzaImportHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    private static async Task SeedAsync(BazaresDbContext ctx)
    {
        ctx.Events.Add(new BzaEvent { Id = 1, TenantId = Tenant, Description = "Evento", Status = 1 });

        var group = new BzaCollectorGroup { Id = 1, TenantId = Tenant, Description = "Grupo A", IsActive = true };
        var collector = new BzaCollector { Id = 1, TenantId = Tenant, Name = "Recolector", IsActive = true, BzaCollectorGroupId = 1, CollectorGroup = group };
        ctx.Collectors.Add(collector);
        ctx.Customers.Add(new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Ana Existente",
            Phone = "525511112222",
            BzaCollectorId = 1,
            Collector = collector,
            Status = 1,
        });

        await ctx.SaveChangesAsync(default);
    }

    private static CommitBzaImportHandler Handler(BazaresDbContext ctx)
    {
        var mongo = new Mock<IMongoContext>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(u => u.TenantId).Returns(Tenant);
        return new CommitBzaImportHandler(ctx, mongo.Object, currentUser.Object);
    }

    private static CommitBzaImportCommand NewCustomerCommand(string name, string phone) =>
        new(
            EventId: 1,
            ConfirmDuplicate: true,
            NewCollectors: [],
            Customers:
            [
                new CommitImportCustomerDto
                {
                    NewCustomer = new CommitImportNewCustomerDto
                    {
                        Name = name,
                        Phone = phone,
                        CollectorName = "Recolector",
                    },
                    Products = [ new CommitImportProductDto { Description = "Blusa", Price = 100m } ],
                },
            ]);

    [Fact]
    public async Task Handle_TelefonoDuplicado_ConservaRegistroEnFailedRecords()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx);
        var handler = Handler(ctx);

        var result = await handler.Handle(NewCustomerCommand("Jesus Nuevo", "5511112222"), default);

        Assert.Equal(1, result.IgnoredRecords);
        Assert.Equal(0, result.NewCustomersCreated);
        Assert.Equal(0, result.ImportedProducts);

        var failed = Assert.Single(result.FailedRecords);
        Assert.Equal("PhoneDuplicate", failed.ConflictType);
        Assert.Equal("Jesus Nuevo", failed.Name);
        Assert.Equal("525511112222", failed.Phone);
        Assert.Equal("Recolector", failed.CollectorName);
        Assert.Equal("Ana Existente", failed.ConflictCustomerName);
        Assert.Single(failed.Products);

        // No se creo el cliente conflictivo.
        Assert.Equal(1, await ctx.Customers.CountAsync());
    }

    [Fact]
    public async Task Handle_ReintentoConTelefonoCorregido_GuardaCliente()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx);
        var handler = Handler(ctx);

        var failedResult = await handler.Handle(NewCustomerCommand("Jesus Nuevo", "5511112222"), default);
        Assert.Single(failedResult.FailedRecords);

        var retry = await handler.Handle(NewCustomerCommand("Jesus Nuevo", "5599998888"), default);

        Assert.Empty(retry.FailedRecords);
        Assert.Equal(0, retry.IgnoredRecords);
        Assert.Equal(1, retry.NewCustomersCreated);
        Assert.Equal(1, retry.ImportedProducts);
        Assert.Equal(1, retry.SalesCreated);
        Assert.Equal(2, await ctx.Customers.CountAsync());
    }

    [Fact]
    public async Task Handle_ClientePendienteConCambiosAutorizados_LimpiaBanderaPendiente()
    {
        using var ctx = BazaresContextFactory.Create();

        ctx.Events.Add(new BzaEvent { Id = 1, TenantId = Tenant, Description = "Evento", Status = 1 });
        var group = new BzaCollectorGroup { Id = 1, TenantId = Tenant, Description = "Grupo A", IsActive = true };
        var placeholderCollector = new BzaCollector
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Aún sin recolector",
            IsActive = true,
            BzaCollectorGroupId = 1,
            CollectorGroup = group
        };
        var realCollector = new BzaCollector
        {
            Id = 2,
            TenantId = Tenant,
            Name = "Recolector Real",
            IsActive = true,
            BzaCollectorGroupId = 1,
            CollectorGroup = group
        };
        ctx.Collectors.AddRange(placeholderCollector, realCollector);
        ctx.Customers.Add(new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente Pendiente",
            Phone = "0000000001",
            HasNoWhatsApp = true,
            BzaCollectorId = 1,
            Collector = placeholderCollector,
            IsPendingInfo = true,
            Status = 1,
        });
        await ctx.SaveChangesAsync();

        var handler = Handler(ctx);
        var result = await handler.Handle(
            new CommitBzaImportCommand(
                EventId: 1,
                ConfirmDuplicate: true,
                NewCollectors: [],
                Customers:
                [
                    new CommitImportCustomerDto
                    {
                        CustomerId = 1,
                        ChangeCollectorToName = "Recolector Real",
                        ChangePhoneTo = "5512345678",
                        Products = [new CommitImportProductDto { Description = "Blusa", Price = 100m }],
                    },
                ]),
            CancellationToken.None);

        var customer = await ctx.Customers.AsNoTracking().SingleAsync(c => c.Id == 1);
        Assert.Equal(1, result.CollectorsChanged);
        Assert.Equal(1, result.CustomersUpdated);
        Assert.False(customer.IsPendingInfo);
        Assert.False(customer.HasNoWhatsApp);
        Assert.Equal(2, customer.BzaCollectorId);
        Assert.Equal("525512345678", customer.Phone);
    }
}
