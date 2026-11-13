using BusinessCloud.Application.Admin.Commands.SetCompanyModules;
using BusinessCloud.Application.Admin.Queries.GetCompanyById;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Tests.TestSupport;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessCloud.Tests.Application.Admin;

public class SetCompanyModulesTests
{
    [Fact]
    public async Task Agregar_catalogos_conserva_aplicaciones_y_no_modifica_otra_empresa()
    {
        using var db = IdentityContextFactory.Create();
        db.Tenants.AddRange(
            new Tenant { Id = "existing", Name = "Existing" },
            new Tenant { Id = "other", Name = "Other" });
        var activation = DateTime.UtcNow.AddDays(-30);
        db.TenantModules.AddRange(
            new TenantModule { TenantId = "existing", Module = SystemModules.Bazares, ActivatedAt = activation },
            new TenantModule { TenantId = "other", Module = SystemModules.Payments });
        await db.SaveChangesAsync();

        var handler = new SetCompanyModulesHandler(db, new SetCompanyModulesValidator());
        var request = new SetCompanyModulesCommand("existing",
            [SystemModules.Bazares, SystemModules.Catalogs, SystemModules.Catalogs]);
        await handler.Handle(request, default);
        await handler.Handle(request, default);

        var detail = await new GetCompanyByIdHandler(db).Handle(new GetCompanyByIdQuery("existing"), default);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.Modules.Count);
        Assert.Contains(SystemModules.Bazares, detail.Modules);
        Assert.Contains(SystemModules.Catalogs, detail.Modules);
        var bazares = await db.TenantModules.SingleAsync(m => m.TenantId == "existing" && m.Module == SystemModules.Bazares);
        Assert.Equal(activation, bazares.ActivatedAt);
        var other = await db.TenantModules.SingleAsync(m => m.TenantId == "other");
        Assert.Equal(SystemModules.Payments, other.Module);
        Assert.True(other.IsActive);
        Assert.Empty(await db.TenantSubscriptions.ToListAsync());
    }

    [Fact]
    public async Task Reactiva_sin_duplicar_y_desactiva_sin_borrar_aplicaciones()
    {
        using var db = IdentityContextFactory.Create();
        db.Tenants.Add(new Tenant { Id = "existing", Name = "Existing", IsActive = false });
        var activation = DateTime.UtcNow.AddDays(-30);
        var catalogs = new TenantModule
        {
            TenantId = "existing", Module = SystemModules.Catalogs, IsActive = false,
            ActivatedAt = activation, DeactivatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var bazares = new TenantModule
        {
            TenantId = "existing", Module = SystemModules.Bazares, ActivatedAt = activation
        };
        db.TenantModules.AddRange(catalogs, bazares);
        await db.SaveChangesAsync();
        var catalogsId = catalogs.Id;

        await new SetCompanyModulesHandler(db, new SetCompanyModulesValidator())
            .Handle(new SetCompanyModulesCommand("existing", [SystemModules.Catalogs]), default);

        Assert.Equal(2, await db.TenantModules.CountAsync());
        Assert.Equal(catalogsId, catalogs.Id);
        Assert.True(catalogs.IsActive);
        Assert.Null(catalogs.DeactivatedAt);
        Assert.True(catalogs.ActivatedAt > activation);
        Assert.False(bazares.IsActive);
        Assert.NotNull(bazares.DeactivatedAt);
        Assert.Equal(activation, bazares.ActivatedAt);
        Assert.False((await db.Tenants.SingleAsync()).IsActive);
    }

    [Theory]
    [InlineData()]
    [InlineData("Unknown")]
    [InlineData("Bazares", "")]
    public async Task Rechaza_seleccion_invalida_sin_cambiar_aplicaciones(params string[] modules)
    {
        using var db = IdentityContextFactory.Create();
        db.Tenants.Add(new Tenant { Id = "existing", Name = "Existing" });
        db.TenantModules.Add(new TenantModule { TenantId = "existing", Module = SystemModules.Bazares });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            new SetCompanyModulesHandler(db, new SetCompanyModulesValidator())
                .Handle(new SetCompanyModulesCommand("existing", modules), default));

        var existing = await db.TenantModules.SingleAsync();
        Assert.True(existing.IsActive);
        Assert.Equal(SystemModules.Bazares, existing.Module);
    }

    [Fact]
    public async Task Rechaza_empresa_inexistente()
    {
        using var db = IdentityContextFactory.Create();
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new SetCompanyModulesHandler(db, new SetCompanyModulesValidator())
                .Handle(new SetCompanyModulesCommand("missing", [SystemModules.Catalogs]), default));
        Assert.Empty(await db.TenantModules.ToListAsync());
    }
}
