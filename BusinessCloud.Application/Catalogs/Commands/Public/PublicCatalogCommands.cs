using System.Net;
using System.Text;
using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Catalogs.Queries.Public;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessCloud.Application.Catalogs.Commands.Public;

public record CreatePublicOrderCommand(string Slug, PublicOrderInput Input) : IRequest<PublicOrderResultDto>;
public record CreatePublicInterestCommand(string Slug, PublicInterestInput Input) : IRequest;

public class CreatePublicOrderHandler(
    ICatalogsDbContext db,
    IIdentityDbContext identity,
    IEmailSender emailSender,
    ILogger<CreatePublicOrderHandler> logger)
    : IRequestHandler<CreatePublicOrderCommand, PublicOrderResultDto>
{
    private const int MaxLines = 50;
    private const int MaxQuantity = 99;

    public async Task<PublicOrderResultDto> Handle(CreatePublicOrderCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var name = CatalogText.Clean(input.CustomerName, 120)
            ?? throw new ArgumentException("El nombre es obligatorio.");
        var phone = CatalogText.NormalizePhone(input.CustomerPhone);
        var note = CatalogText.Clean(input.Note, 500);

        if (input.Items is null || input.Items.Count is 0 or > MaxLines)
            throw new ArgumentException($"El pedido debe tener entre 1 y {MaxLines} productos.");
        if (input.Items.Any(i => i.Quantity is < 1 or > MaxQuantity))
            throw new ArgumentException($"La cantidad de cada producto debe estar entre 1 y {MaxQuantity}.");

        var settings = await PublicCatalogResolver.ResolveAsync(db, identity, request.Slug, ct);
        var tenantId = settings.TenantId;

        // Una misma variante repetida se acumula en una sola línea.
        var lines = input.Items
            .GroupBy(i => (i.ProductId, i.VariantId))
            .Select(g => (g.Key.ProductId, g.Key.VariantId, Quantity: Math.Min(g.Sum(x => x.Quantity), MaxQuantity)))
            .ToList();

        CatOrder? created = null;
        await db.ExecuteInTransactionAsync(async token =>
        {
            var storeSettings = await db.StoreSettings.IgnoreQueryFilters()
                .FirstAsync(s => s.Id == settings.Id, token);

            var categories = await PublicCatalogData.VisibleCategoryIdsAsync(db, tenantId, token);
            var variantIds = lines.Select(l => l.VariantId).ToList();
            var variants = await db.ProductVariants.IgnoreQueryFilters()
                .Include(v => v.Product!).ThenInclude(p => p.Images)
                .Where(v => v.TenantId == tenantId && v.IsActive && variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, token);

            var order = new CatOrder
            {
                TenantId = tenantId,
                CustomerName = name,
                CustomerPhone = phone,
                Note = note,
                Status = CatOrderStatus.New,
                StockDiscounted = true,
            };

            foreach (var (productId, variantId, quantity) in lines)
            {
                if (!variants.TryGetValue(variantId, out var variant)
                    || variant.ProductId != productId
                    || variant.Product is null
                    || variant.Product.IsDeleted
                    || variant.Product.Status == CatProductStatus.Hidden
                    || (variant.Product.CategoryId is { } categoryId && !categories.Contains(categoryId)))
                {
                    throw new ArgumentException("Uno de los productos del pedido ya no está disponible. Actualiza tu carrito.");
                }

                var product = variant.Product;
                var available = product.Status == CatProductStatus.SoldOut ? 0 : variant.Stock;
                var discounted = Math.Min(quantity, available);
                var outOfStock = quantity > available;
                variant.Stock -= discounted;

                order.Items.Add(new CatOrderItem
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    ProductName = product.Name,
                    VariantLabel = variant.Label,
                    ImageUrl = product.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => i.Url).FirstOrDefault(),
                    UnitPrice = product.DiscountPrice ?? product.Price,
                    RequestedQuantity = quantity,
                    QuantityDiscounted = discounted,
                    FinalQuantity = discounted,
                    OutOfStockAtSubmit = outOfStock,
                    LineNote = outOfStock
                        ? $"Sin existencia al momento del envío (solicitó {quantity}, había {available})"
                        : null,
                });
            }

            order.EstimatedTotal = order.Items.Sum(i => i.UnitPrice * i.RequestedQuantity);
            order.FinalTotal = order.Items.Sum(i => i.UnitPrice * i.FinalQuantity);
            order.HasOutOfStockItems = order.Items.Any(i => i.OutOfStockAtSubmit);

            storeSettings.LastOrderNumber++;
            order.OrderNumber = $"P-{storeSettings.LastOrderNumber:D6}";

            db.Orders.Add(order);
            await db.SaveChangesAsync(token);
            created = order;
        }, ct);

        var result = created!;
        await NotifyCompanyAsync(settings, result, ct);

        return new PublicOrderResultDto(
            result.OrderNumber,
            result.EstimatedTotal,
            result.HasOutOfStockItems,
            result.HasOutOfStockItems
                ? $"Recibimos tu pedido {result.OrderNumber}. Algunos productos no tenían existencia completa; {settings.PublicName} se pondrá en contacto contigo."
                : $"Recibimos tu pedido {result.OrderNumber}. {settings.PublicName} confirmará disponibilidad y total final.");
    }

    private async Task NotifyCompanyAsync(CatStoreSettings settings, CatOrder order, CancellationToken ct)
    {
        try
        {
            var to = settings.OrderEmail;
            if (string.IsNullOrWhiteSpace(to))
            {
                to = await identity.Users
                    .Where(u => u.TenantId == settings.TenantId && u.IsActive && u.Role == SystemRoles.SuperAdmin)
                    .OrderBy(u => u.Email)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync(ct);
            }

            if (string.IsNullOrWhiteSpace(to) || !emailSender.IsConfigured)
            {
                logger.LogWarning("Pedido {OrderNumber} sin notificación por correo (destinatario o servicio no configurado).", order.OrderNumber);
                return;
            }

            var result = await emailSender.SendAsync(
                to, $"Nuevo pedido {order.OrderNumber} - {settings.PublicName}",
                CatalogOrderEmail.BuildHtml(settings, order), ct);
            if (!result.Success)
                logger.LogWarning("No se pudo enviar el correo del pedido {OrderNumber}: {Error}", order.OrderNumber, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            // El pedido ya está guardado: un fallo de correo no debe fallar la compra del cliente.
            logger.LogError(ex, "Error enviando el correo del pedido {OrderNumber}", order.OrderNumber);
        }
    }
}

public static class CatalogOrderEmail
{
    public static string BuildHtml(CatStoreSettings settings, CatOrder order)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
        var money = (decimal v) => v.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " " + settings.Currency;

        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Arial,sans-serif;max-width:600px\">");
        sb.Append($"<h2>Nuevo pedido {E(order.OrderNumber)}</h2>");
        sb.Append($"<p><strong>Cliente:</strong> {E(order.CustomerName)}<br/><strong>Teléfono:</strong> {E(order.CustomerPhone)}</p>");
        if (!string.IsNullOrWhiteSpace(order.Note))
            sb.Append($"<p><strong>Nota del cliente:</strong> {E(order.Note)}</p>");

        sb.Append("<table style=\"width:100%;border-collapse:collapse\" border=\"1\" cellpadding=\"6\">");
        sb.Append("<tr><th align=\"left\">Producto</th><th>Cant.</th><th align=\"right\">Precio</th></tr>");
        foreach (var item in order.Items)
        {
            var label = item.VariantLabel is null ? string.Empty : $" ({E(item.VariantLabel)})";
            sb.Append($"<tr><td>{E(item.ProductName)}{label}</td><td align=\"center\">{item.RequestedQuantity}</td><td align=\"right\">{money(item.UnitPrice)}</td></tr>");
        }
        sb.Append("</table>");
        sb.Append($"<p><strong>Pre-total:</strong> {money(order.EstimatedTotal)}</p>");

        var outOfStock = order.Items.Where(i => i.OutOfStockAtSubmit).ToList();
        if (outOfStock.Count > 0)
        {
            sb.Append("<h3 style=\"color:#b45309\">Productos sin existencia al momento del envío</h3><ul>");
            foreach (var item in outOfStock)
            {
                var label = item.VariantLabel is null ? string.Empty : $" ({E(item.VariantLabel)})";
                sb.Append($"<li>{E(item.ProductName)}{label}: {E(item.LineNote)}</li>");
            }
            sb.Append("</ul><p>Comunícate con el cliente para resolver estas piezas.</p>");
        }

        sb.Append("<p>Revisa y gestiona el pedido desde la bandeja de pedidos de tu catálogo.</p></div>");
        return sb.ToString();
    }
}

public class CreatePublicInterestHandler(ICatalogsDbContext db, IIdentityDbContext identity)
    : IRequestHandler<CreatePublicInterestCommand>
{
    public async Task Handle(CreatePublicInterestCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var phone = CatalogText.NormalizePhone(input.CustomerPhone);
        var customerName = CatalogText.Clean(input.CustomerName, 120);
        var note = CatalogText.Clean(input.Note, 300);

        var settings = await PublicCatalogResolver.ResolveAsync(db, identity, request.Slug, ct);
        var tenantId = settings.TenantId;

        var categoryIds = await PublicCatalogData.VisibleCategoryIdsAsync(db, tenantId, ct);
        var product = await db.Products.IgnoreQueryFilters()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == input.ProductId && p.TenantId == tenantId
                && !p.IsDeleted && p.Status != CatProductStatus.Hidden, ct)
            ?? throw new KeyNotFoundException("El producto no está disponible.");

        if (product.CategoryId is { } categoryId && !categoryIds.Contains(categoryId))
            throw new KeyNotFoundException("El producto no está disponible.");
        if (!product.IsSoldOut)
            throw new ArgumentException("Este producto tiene existencia; puedes agregarlo al carrito.");

        string? variantLabel = null;
        if (input.VariantId is { } variantId)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId && v.IsActive)
                ?? throw new ArgumentException("La variante no pertenece al producto.");
            variantLabel = variant.Label;
        }

        // Evita registros duplicados si el cliente envía varias veces lo mismo.
        var since = DateTime.UtcNow.AddHours(-24);
        var duplicate = await db.InterestRequests.IgnoreQueryFilters().AnyAsync(i =>
            i.TenantId == tenantId && i.ProductId == product.Id && i.VariantId == input.VariantId
            && i.CustomerPhone == phone && i.CreatedAt >= since, ct);
        if (duplicate) return;

        db.InterestRequests.Add(new CatInterestRequest
        {
            TenantId = tenantId,
            ProductId = product.Id,
            VariantId = input.VariantId,
            ProductName = product.Name,
            VariantLabel = variantLabel,
            CustomerName = customerName,
            CustomerPhone = phone,
            Note = note,
        });
        await db.SaveChangesAsync(ct);
    }
}
