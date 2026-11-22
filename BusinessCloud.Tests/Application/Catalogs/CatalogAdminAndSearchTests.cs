using BusinessCloud.Application.Catalogs.Commands.Categories;
using BusinessCloud.Application.Catalogs.Commands.Products;
using BusinessCloud.Application.Catalogs.Commands.Settings;
using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Catalogs.Queries.Admin;
using BusinessCloud.Application.Catalogs.Queries.Public;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Catalogs;

public class CatalogAdminAndSearchTests
{
    private const string Tenant = CatalogsContextFactory.TenantId;

    private static async Task<IdentityDbContext> SeedIdentityAsync(params (string id, string name)[] tenants)
    {
        var identity = IdentityContextFactory.Create();
        foreach (var (id, name) in tenants)
        {
            identity.Tenants.Add(new Tenant { Id = id, Name = name, IsActive = true });
            identity.TenantModules.Add(new TenantModule { TenantId = id, Module = SystemModules.Catalogs, IsActive = true });
        }
        await identity.SaveChangesAsync();
        return identity;
    }

    private static ProductInput Product(string name, int? categoryId = null, string status = "Active", int stock = 3,
        decimal? discountPrice = null) => new()
    {
        Name = name,
        Description = $"Descripción de {name}",
        CategoryId = categoryId,
        Price = 50,
        DiscountPrice = discountPrice,
        Status = status,
        Variants = [new VariantInput { Size = "M", Stock = stock }],
    };

    // ---------- Slug ----------

    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    [InlineData(39.99)]
    public async Task Precios_se_guardan_y_se_exponen_en_admin_listado_destacados_y_detalle_publico(double? value)
    {
        var discount = value is { } number ? (decimal?)Convert.ToDecimal(number) : null;
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync((Tenant, "Mi Tienda"));
        var settings = await CatalogSettingsProvider.GetOrCreateAsync(db, identity, Tenant, default);
        var input = Product("Blusa", discountPrice: discount);
        input.IsFeatured = true;

        var created = await new CreateProductHandler(db, CatalogsContextFactory.CurrentUser())
            .Handle(new CreateProductCommand(input), default);
        Assert.Equal(discount, created.DiscountPrice);
        db.ChangeTracker.Clear();
        var persisted = await db.Products.SingleAsync();
        Assert.Equal(50m, persisted.Price);
        Assert.Equal(discount, persisted.DiscountPrice);

        var admin = CatalogMappers.ToListDto(persisted, null);
        Assert.Equal(discount, admin.DiscountPrice);
        var list = await new GetPublicProductsHandler(db, identity)
            .Handle(new GetPublicProductsQuery(settings.Slug, null, null, null, 1, 24), default);
        Assert.Equal(discount, Assert.Single(list.Items).DiscountPrice);
        var featured = await new GetPublicFeaturedHandler(db, identity)
            .Handle(new GetPublicFeaturedQuery(settings.Slug), default);
        Assert.Equal(discount, Assert.Single(featured).DiscountPrice);
        var detail = await new GetPublicProductHandler(db, identity)
            .Handle(new GetPublicProductQuery(settings.Slug, created.Id), default);
        Assert.Equal(50m, detail.Price);
        Assert.Equal(discount, detail.DiscountPrice);
    }

    [Fact]
    public async Task Editar_permite_cambiar_y_eliminar_el_precio_promocional()
    {
        using var db = CatalogsContextFactory.Create();
        var user = CatalogsContextFactory.CurrentUser();
        var created = await new CreateProductHandler(db, user)
            .Handle(new CreateProductCommand(Product("Blusa", discountPrice: 40m)), default);
        var handler = new UpdateProductHandler(db, user);
        var changed = await handler.Handle(new UpdateProductCommand(created.Id,
            Product("Blusa", discountPrice: 35m)), default);
        Assert.Equal(35m, changed.DiscountPrice);

        var removed = await handler.Handle(new UpdateProductCommand(created.Id, Product("Blusa")), default);
        Assert.Null(removed.DiscountPrice);
        db.ChangeTracker.Clear();
        var persisted = await db.Products.SingleAsync();
        Assert.Equal(50m, persisted.Price);
        Assert.Null(persisted.DiscountPrice);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(49.999)]
    public async Task Promocion_invalida_se_rechaza_al_crear_y_editar(double value)
    {
        using var db = CatalogsContextFactory.Create();
        var user = CatalogsContextFactory.CurrentUser();
        var create = new CreateProductHandler(db, user);
        var invalid = Product("Blusa", discountPrice: Convert.ToDecimal(value));
        await Assert.ThrowsAsync<ArgumentException>(() => create.Handle(new CreateProductCommand(invalid), default));
        Assert.Empty(await db.Products.ToListAsync());

        var created = await create.Handle(new CreateProductCommand(Product("Blusa", discountPrice: 40m)), default);
        await Assert.ThrowsAsync<ArgumentException>(() => new UpdateProductHandler(db, user)
            .Handle(new UpdateProductCommand(created.Id, invalid), default));
        db.ChangeTracker.Clear();
        Assert.Equal(40m, (await db.Products.SingleAsync()).DiscountPrice);
    }

    [Theory]
    [InlineData("Mi Tienda Bonita", "mi-tienda-bonita")]
    [InlineData("  Ropa & Más  ", "ropa-mas")]
    [InlineData("Ñandú Café", "nandu-cafe")]
    [InlineData("!!", "tienda")]
    public void Slug_se_genera_sin_acentos_ni_simbolos(string name, string expected) =>
        Assert.Equal(expected, CatalogSlug.FromName(name));

    [Fact]
    public async Task Slug_se_genera_unico_entre_empresas_con_el_mismo_nombre()
    {
        var dbName = $"catalogs-{Guid.NewGuid():N}";
        using var identity = await SeedIdentityAsync(("t1", "Mi Tienda"), ("t2", "Mi Tienda"));
        using var db1 = CatalogsContextFactory.Create("t1", dbName);
        using var db2 = CatalogsContextFactory.Create("t2", dbName);

        var s1 = await CatalogSettingsProvider.GetOrCreateAsync(db1, identity, "t1", default);
        var s2 = await CatalogSettingsProvider.GetOrCreateAsync(db2, identity, "t2", default);

        Assert.Equal("mi-tienda", s1.Slug);
        Assert.Equal("mi-tienda-2", s2.Slug);
    }

    [Fact]
    public async Task Cambiar_slug_a_uno_en_uso_por_otra_empresa_devuelve_conflicto()
    {
        var dbName = $"catalogs-{Guid.NewGuid():N}";
        using var identity = await SeedIdentityAsync(("t1", "Tienda Uno"), ("t2", "Tienda Dos"));
        using var db1 = CatalogsContextFactory.Create("t1", dbName);
        using var db2 = CatalogsContextFactory.Create("t2", dbName);
        await CatalogSettingsProvider.GetOrCreateAsync(db1, identity, "t1", default);
        var s2 = await CatalogSettingsProvider.GetOrCreateAsync(db2, identity, "t2", default);

        var handler = new UpdateStoreSettingsHandler(db2, identity, CatalogsContextFactory.CurrentUser("t2"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new UpdateStoreSettingsCommand(new StoreSettingsInput { Slug = "tienda-uno", PublicName = s2.PublicName }), default));

        var ok = await handler.Handle(
            new UpdateStoreSettingsCommand(new StoreSettingsInput { Slug = "dos-boutique", PublicName = "Dos" }), default);
        Assert.Equal("/c/dos-boutique", ok.PublicUrlPath);
    }

    [Fact]
    public async Task Slug_invalido_se_rechaza()
    {
        using var identity = await SeedIdentityAsync((Tenant, "Mi Tienda"));
        using var db = CatalogsContextFactory.Create();
        var handler = new UpdateStoreSettingsHandler(db, identity, CatalogsContextFactory.CurrentUser());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new UpdateStoreSettingsCommand(new StoreSettingsInput { Slug = "Mi Tienda!", PublicName = "X" }), default));
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("PlayfairDisplay")]
    [InlineData("DancingScript")]
    [InlineData("Montserrat")]
    public async Task Tipografia_se_guarda_y_se_expone_en_el_catalogo_publico(string font)
    {
        using var identity = await SeedIdentityAsync((Tenant, "Mi Tienda"));
        using var db = CatalogsContextFactory.Create();
        var settings = await CatalogSettingsProvider.GetOrCreateAsync(db, identity, Tenant, default);
        settings.TitleFont = CatTitleFont.DancingScript;
        await db.SaveChangesAsync(default);
        var handler = new UpdateStoreSettingsHandler(db, identity, CatalogsContextFactory.CurrentUser());

        var updated = await handler.Handle(new UpdateStoreSettingsCommand(new StoreSettingsInput
        {
            Slug = settings.Slug, PublicName = settings.PublicName, TitleFont = font
        }), default);
        Assert.Equal(font, updated.TitleFont);

        db.ChangeTracker.Clear();
        var store = await new GetPublicStoreHandler(db, identity)
            .Handle(new GetPublicStoreQuery(updated.Slug), default);
        Assert.Equal(font, store.TitleFont);
    }

    [Fact]
    public async Task Cliente_anterior_que_omite_tipografia_conserva_la_seleccion()
    {
        using var identity = await SeedIdentityAsync((Tenant, "Mi Tienda"));
        using var db = CatalogsContextFactory.Create();
        var handler = new UpdateStoreSettingsHandler(db, identity, CatalogsContextFactory.CurrentUser());
        await handler.Handle(new UpdateStoreSettingsCommand(new StoreSettingsInput
        {
            Slug = "mi-tienda", PublicName = "Mi Tienda", TitleFont = "Montserrat"
        }), default);

        var updated = await handler.Handle(new UpdateStoreSettingsCommand(new StoreSettingsInput
        {
            Slug = "mi-tienda", PublicName = "Nuevo nombre"
        }), default);

        db.ChangeTracker.Clear();
        Assert.Equal("Montserrat", updated.TitleFont);
        Assert.Equal(CatTitleFont.Montserrat, (await db.StoreSettings.SingleAsync()).TitleFont);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("playfairdisplay")]
    [InlineData("Arial; color: red")]
    public async Task Tipografia_invalida_se_rechaza_sin_modificar_la_configuracion(string font)
    {
        using var identity = await SeedIdentityAsync((Tenant, "Mi Tienda"));
        using var db = CatalogsContextFactory.Create();
        var settings = await CatalogSettingsProvider.GetOrCreateAsync(db, identity, Tenant, default);
        var handler = new UpdateStoreSettingsHandler(db, identity, CatalogsContextFactory.CurrentUser());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new UpdateStoreSettingsCommand(new StoreSettingsInput
            {
                Slug = settings.Slug, PublicName = "No guardar", TitleFont = font
            }), default));

        db.ChangeTracker.Clear();
        var persisted = await db.StoreSettings.SingleAsync();
        Assert.Equal(CatTitleFont.Default, persisted.TitleFont);
        Assert.Equal("Mi Tienda", persisted.PublicName);
    }

    [Fact]
    public async Task Tipografia_de_una_empresa_no_cambia_la_de_otra()
    {
        var dbName = $"catalogs-{Guid.NewGuid():N}";
        using var identity = await SeedIdentityAsync(("t1", "Uno"), ("t2", "Dos"));
        using var db1 = CatalogsContextFactory.Create("t1", dbName);
        using var db2 = CatalogsContextFactory.Create("t2", dbName);
        await CatalogSettingsProvider.GetOrCreateAsync(db2, identity, "t2", default);
        await new UpdateStoreSettingsHandler(db1, identity, CatalogsContextFactory.CurrentUser("t1"))
            .Handle(new UpdateStoreSettingsCommand(new StoreSettingsInput
            {
                Slug = "uno", PublicName = "Uno", TitleFont = "PlayfairDisplay"
            }), default);

        var publicHandler = new GetPublicStoreHandler(db1, identity);
        Assert.Equal("PlayfairDisplay", (await publicHandler.Handle(new GetPublicStoreQuery("uno"), default)).TitleFont);
        Assert.Equal("Default", (await publicHandler.Handle(new GetPublicStoreQuery("dos"), default)).TitleFont);
    }

    // ---------- Aislamiento por tenant ----------

    [Fact]
    public async Task Admin_solo_ve_los_productos_de_su_empresa()
    {
        var dbName = $"catalogs-{Guid.NewGuid():N}";
        using var db1 = CatalogsContextFactory.Create("t1", dbName);
        using var db2 = CatalogsContextFactory.Create("t2", dbName);
        await new CreateProductHandler(db1, CatalogsContextFactory.CurrentUser("t1")).Handle(new CreateProductCommand(Product("De uno")), default);
        await new CreateProductHandler(db2, CatalogsContextFactory.CurrentUser("t2")).Handle(new CreateProductCommand(Product("De dos")), default);

        var list = await new GetProductsHandler(db1, CatalogsContextFactory.CurrentUser("t1"))
            .Handle(new GetProductsQuery(null, null, null, null, 1, 20), default);

        var only = Assert.Single(list.Items);
        Assert.Equal("De uno", only.Name);

        var otherId = (await db2.Products.SingleAsync()).Id;
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new GetProductHandler(db1, CatalogsContextFactory.CurrentUser("t1")).Handle(new GetProductQuery(otherId), default));
    }

    // ---------- Productos ----------

    [Fact]
    public async Task Producto_sin_variantes_recibe_una_variante_por_defecto()
    {
        using var db = CatalogsContextFactory.Create();
        var input = Product("Sin variantes");
        input.Variants = [];

        var dto = await new CreateProductHandler(db, CatalogsContextFactory.CurrentUser()).Handle(new CreateProductCommand(input), default);

        var variant = Assert.Single(dto.Variants);
        Assert.Equal(0, variant.Stock);
        Assert.True(dto.IsSoldOut);
    }

    [Fact]
    public async Task Maximo_cinco_imagenes_por_producto()
    {
        using var db = CatalogsContextFactory.Create();
        var user = CatalogsContextFactory.CurrentUser();
        var product = await new CreateProductHandler(db, user).Handle(new CreateProductCommand(Product("Con fotos")), default);
        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string c, string n, Stream _, string _, CancellationToken _) => $"https://blob/{c}/{n}");
        var handler = new UploadProductImageHandler(db, user, blob.Object);
        byte[] Png() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

        for (var i = 0; i < 5; i++)
            await handler.Handle(new UploadProductImageCommand(product.Id, new MemoryStream(Png())), default);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new UploadProductImageCommand(product.Id, new MemoryStream(Png())), default));
        Assert.Equal(5, await db.ProductImages.CountAsync());
    }

    [Fact]
    public async Task Archivo_que_no_es_imagen_se_rechaza_aunque_diga_ser_jpg()
    {
        using var db = CatalogsContextFactory.Create();
        var user = CatalogsContextFactory.CurrentUser();
        var product = await new CreateProductHandler(db, user).Handle(new CreateProductCommand(Product("X")), default);
        var handler = new UploadProductImageHandler(db, user, Mock.Of<IBlobStorageService>());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new UploadProductImageCommand(product.Id, new MemoryStream("<script>alert(1)</script>"u8.ToArray())), default));
    }

    [Fact]
    public async Task Variante_usada_en_pedidos_se_desactiva_en_lugar_de_borrarse()
    {
        using var db = CatalogsContextFactory.Create();
        var user = CatalogsContextFactory.CurrentUser();
        var created = await new CreateProductHandler(db, user).Handle(new CreateProductCommand(new ProductInput
        {
            Name = "Vestido", Price = 10, Status = "Active",
            Variants = [new VariantInput { Size = "S", Stock = 1 }, new VariantInput { Size = "L", Stock = 1 }],
        }), default);
        var usedVariantId = created.Variants[0].Id;
        var keepVariant = created.Variants[1];
        db.OrderItems.Add(new CatOrderItem { TenantId = Tenant, OrderId = 1, VariantId = usedVariantId, ProductId = created.Id, ProductName = "Vestido" });
        await db.SaveChangesAsync(default);

        var updated = await new UpdateProductHandler(db, user).Handle(new UpdateProductCommand(created.Id, new ProductInput
        {
            Name = "Vestido", Price = 10, Status = "Active",
            Variants = [new VariantInput { Id = keepVariant.Id, Size = "L", Stock = 1 }],
        }), default);

        Assert.Single(updated.Variants);
        Assert.False((await db.ProductVariants.SingleAsync(v => v.Id == usedVariantId)).IsActive);
    }

    [Fact]
    public async Task Eliminar_categoria_con_productos_o_subcategorias_devuelve_conflicto()
    {
        using var db = CatalogsContextFactory.Create();
        var user = CatalogsContextFactory.CurrentUser();
        var parent = await new CreateCategoryHandler(db, user).Handle(new CreateCategoryCommand(new CategoryInput { Name = "Ropa" }), default);
        await new CreateCategoryHandler(db, user).Handle(new CreateCategoryCommand(new CategoryInput { Name = "Blusas", ParentId = parent.Id }), default);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DeleteCategoryHandler(db, user, Mock.Of<IBlobStorageService>()).Handle(new DeleteCategoryCommand(parent.Id), default));
    }

    [Fact]
    public async Task Solo_se_permiten_dos_niveles_de_categorias()
    {
        using var db = CatalogsContextFactory.Create();
        var user = CatalogsContextFactory.CurrentUser();
        var handler = new CreateCategoryHandler(db, user);
        var root = await handler.Handle(new CreateCategoryCommand(new CategoryInput { Name = "Ropa" }), default);
        var child = await handler.Handle(new CreateCategoryCommand(new CategoryInput { Name = "Blusas", ParentId = root.Id }), default);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new CreateCategoryCommand(new CategoryInput { Name = "Manga larga", ParentId = child.Id }), default));
    }

    // ---------- Búsqueda y visibilidad pública ----------

    private const string Slug = "tienda";

    private static async Task<(CatalogsDbContext db, IdentityDbContext identity)> SeedCatalogAsync()
    {
        var identity = await SeedIdentityAsync((Tenant, "Tienda"));
        var db = CatalogsContextFactory.Create();
        db.StoreSettings.Add(new CatStoreSettings { TenantId = Tenant, Slug = Slug, PublicName = "Tienda" });
        await db.SaveChangesAsync(default);

        var user = CatalogsContextFactory.CurrentUser();
        var create = new CreateCategoryHandler(db, user);
        var ropa = await create.Handle(new CreateCategoryCommand(new CategoryInput { Name = "Ropa" }), default);
        var blusas = await create.Handle(new CreateCategoryCommand(new CategoryInput { Name = "Blusas", ParentId = ropa.Id }), default);
        var oculta = await create.Handle(new CreateCategoryCommand(new CategoryInput { Name = "Secreta", IsVisible = false }), default);

        var products = new CreateProductHandler(db, user);
        await products.Handle(new CreateProductCommand(Product("Blusa floral", blusas.Id)), default);
        await products.Handle(new CreateProductCommand(Product("Pantalón mezclilla", ropa.Id)), default);
        await products.Handle(new CreateProductCommand(Product("Bolsa oculta", null, "Hidden")), default);
        await products.Handle(new CreateProductCommand(Product("Producto secreto", oculta.Id)), default);
        await products.Handle(new CreateProductCommand(Product("Collar agotado", null, "SoldOut")), default);
        return (db, identity);
    }

    [Fact]
    public async Task Busqueda_publica_por_nombre_descripcion_y_categoria_excluye_ocultos()
    {
        var (db, identity) = await SeedCatalogAsync();
        using var _ = db; using var __ = identity;
        var handler = new GetPublicProductsHandler(db, identity);

        var all = await handler.Handle(new GetPublicProductsQuery(Slug, null, null, null, 1, 24), default);
        Assert.Equal(new[] { "Blusa floral", "Collar agotado", "Pantalón mezclilla" },
            all.Items.Select(i => i.Name).ToArray());

        var byName = await handler.Handle(new GetPublicProductsQuery(Slug, "FLORAL", null, null, 1, 24), default);
        Assert.Equal("Blusa floral", Assert.Single(byName.Items).Name);

        var byDescription = await handler.Handle(new GetPublicProductsQuery(Slug, "descripción mezclilla", null, null, 1, 24), default);
        Assert.Equal("Pantalón mezclilla", Assert.Single(byDescription.Items).Name);

        var byCategoryName = await handler.Handle(new GetPublicProductsQuery(Slug, "blusas", null, null, 1, 24), default);
        Assert.Equal("Blusa floral", Assert.Single(byCategoryName.Items).Name);

        var hidden = await handler.Handle(new GetPublicProductsQuery(Slug, "secreto", null, null, 1, 24), default);
        Assert.Empty(hidden.Items);
    }

    [Fact]
    public async Task Filtrar_por_categoria_padre_incluye_subcategorias()
    {
        var (db, identity) = await SeedCatalogAsync();
        using var _ = db; using var __ = identity;
        var ropaId = (await db.Categories.SingleAsync(c => c.Name == "Ropa")).Id;

        var result = await new GetPublicProductsHandler(db, identity)
            .Handle(new GetPublicProductsQuery(Slug, null, ropaId, null, 1, 24), default);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task Categorias_publicas_solo_muestran_visibles_con_productos()
    {
        var (db, identity) = await SeedCatalogAsync();
        using var _ = db; using var __ = identity;

        var tree = await new GetPublicCategoriesHandler(db, identity).Handle(new GetPublicCategoriesQuery(Slug), default);

        var ropa = Assert.Single(tree);
        Assert.Equal("Ropa", ropa.Name);
        Assert.Equal("Blusas", Assert.Single(ropa.Children).Name);
    }

    [Fact]
    public async Task Producto_agotado_se_muestra_con_bandera_de_agotado_y_no_permite_stock()
    {
        var (db, identity) = await SeedCatalogAsync();
        using var _ = db; using var __ = identity;
        var id = (await db.Products.SingleAsync(p => p.Name == "Collar agotado")).Id;

        var dto = await new GetPublicProductHandler(db, identity).Handle(new GetPublicProductQuery(Slug, id), default);

        Assert.True(dto.IsSoldOut);
        Assert.All(dto.Variants, v => Assert.False(v.InStock));
    }

    [Fact]
    public async Task Detalle_publico_de_producto_oculto_devuelve_no_encontrado()
    {
        var (db, identity) = await SeedCatalogAsync();
        using var _ = db; using var __ = identity;
        var id = (await db.Products.SingleAsync(p => p.Name == "Bolsa oculta")).Id;

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new GetPublicProductHandler(db, identity).Handle(new GetPublicProductQuery(Slug, id), default));
    }

    [Fact]
    public async Task Carrusel_vacio_si_no_hay_banners_activos_y_panel_promo_requiere_dos_categorias()
    {
        var (db, identity) = await SeedCatalogAsync();
        using var _ = db; using var __ = identity;
        var settings = await db.StoreSettings.SingleAsync();
        var ropa = await db.Categories.SingleAsync(c => c.Name == "Ropa");
        settings.PromoPanelEnabled = true;
        settings.PromoCategory1Id = ropa.Id;
        db.Banners.Add(new CatBanner { TenantId = Tenant, ImageUrl = "https://x/y.png", BlobName = "b", IsActive = false });
        await db.SaveChangesAsync(default);

        var store = await new GetPublicStoreHandler(db, identity).Handle(new GetPublicStoreQuery(Slug), default);

        Assert.Empty(store.Banners);
        Assert.False(store.PromoPanel.Enabled);
    }

    // ---------- Interés en agotados ----------

    [Fact]
    public async Task Interes_solo_se_registra_en_agotados_y_no_se_duplica()
    {
        var (db, identity) = await SeedCatalogAsync();
        using var _ = db; using var __ = identity;
        var soldOut = await db.Products.SingleAsync(p => p.Name == "Collar agotado");
        var inStock = await db.Products.SingleAsync(p => p.Name == "Blusa floral");
        var handler = new BusinessCloud.Application.Catalogs.Commands.Public.CreatePublicInterestHandler(db, identity);

        var input = new PublicInterestInput { ProductId = soldOut.Id, CustomerPhone = "5511223344", CustomerName = "Luz" };
        await handler.Handle(new BusinessCloud.Application.Catalogs.Commands.Public.CreatePublicInterestCommand(Slug, input), default);
        await handler.Handle(new BusinessCloud.Application.Catalogs.Commands.Public.CreatePublicInterestCommand(Slug, input), default);
        Assert.Single(await db.InterestRequests.ToListAsync());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new BusinessCloud.Application.Catalogs.Commands.Public.CreatePublicInterestCommand(Slug,
                new PublicInterestInput { ProductId = inStock.Id, CustomerPhone = "5511223344" }), default));
    }
}
