using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Commands.Orders;

public record AdjustOrderCommand(int Id, OrderAdjustInput Input) : IRequest<OrderDto>;
public record SetOrderStatusCommand(int Id, string Status) : IRequest<OrderDto>;
public record SetInterestAttendedCommand(int Id, bool IsAttended) : IRequest;

public class AdjustOrderHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AdjustOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(AdjustOrderCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var order = await db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("El pedido no existe.");

        if (order.Status == CatOrderStatus.Cancelled)
            throw new InvalidOperationException("Un pedido cancelado no se puede modificar.");

        foreach (var adjust in request.Input.Items)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == adjust.Id)
                ?? throw new KeyNotFoundException("Una línea no pertenece al pedido.");
            if (adjust.FinalQuantity < 0 || adjust.FinalQuantity > item.RequestedQuantity)
                throw new ArgumentException($"La cantidad final de '{item.ProductName}' debe estar entre 0 y {item.RequestedQuantity}.");
            if (order.Status != CatOrderStatus.New && adjust.FinalQuantity != item.FinalQuantity)
                throw new InvalidOperationException("Solo se pueden ajustar cantidades en pedidos nuevos.");
            item.FinalQuantity = adjust.FinalQuantity;
        }

        order.InternalNote = CatalogText.Clean(request.Input.InternalNote, 1000);
        order.FinalTotal = order.Items.Sum(i => i.UnitPrice * i.FinalQuantity);
        await db.SaveChangesAsync(ct);
        return CatalogMappers.ToDto(order);
    }
}

public class SetOrderStatusHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetOrderStatusCommand, OrderDto>
{
    public async Task<OrderDto> Handle(SetOrderStatusCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        if (!Enum.TryParse<CatOrderStatus>(request.Status, true, out var target) || !Enum.IsDefined(target))
            throw new ArgumentException("Estado de pedido inválido. Usa New, Confirmed o Cancelled.");

        OrderDto? result = null;
        await db.ExecuteInTransactionAsync(async token =>
        {
            var order = await db.Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.Id && o.TenantId == tenantId, token)
                ?? throw new KeyNotFoundException("El pedido no existe.");

            if (order.Status == CatOrderStatus.Cancelled)
                throw new InvalidOperationException("Un pedido cancelado no puede cambiar de estado.");
            if (target == CatOrderStatus.New && order.Status != CatOrderStatus.New)
                throw new InvalidOperationException("Un pedido no puede regresar a Nuevo.");

            if (target != order.Status)
            {
                var variantIds = order.Items.Select(i => i.VariantId).Distinct().ToList();
                var variants = await db.ProductVariants
                    .Where(v => v.TenantId == tenantId && variantIds.Contains(v.Id))
                    .ToDictionaryAsync(v => v.Id, token);

                if (target == CatOrderStatus.Confirmed)
                    ConfirmStock(order, variants);
                else
                    RestoreStock(order, variants);

                order.Status = target;
                await db.SaveChangesAsync(token);
            }

            result = CatalogMappers.ToDto(order);
        }, ct);

        return result!;
    }

    /// <summary>Ajusta la existencia descontada a lo que la empresa confirmó entregar.</summary>
    private static void ConfirmStock(CatOrder order, Dictionary<int, CatProductVariant> variants)
    {
        foreach (var item in order.Items)
        {
            var delta = item.FinalQuantity - item.QuantityDiscounted;
            if (delta == 0) continue;

            if (variants.TryGetValue(item.VariantId, out var variant))
            {
                if (delta > 0 && variant.Stock < delta)
                    throw new InvalidOperationException(
                        $"No hay existencia suficiente de '{item.ProductName}' para confirmar {item.FinalQuantity} pieza(s).");
                variant.Stock -= delta;
            }
            else if (delta > 0)
            {
                throw new InvalidOperationException($"La variante de '{item.ProductName}' ya no existe.");
            }

            item.QuantityDiscounted = item.FinalQuantity;
        }

        order.FinalTotal = order.Items.Sum(i => i.UnitPrice * i.FinalQuantity);
    }

    private static void RestoreStock(CatOrder order, Dictionary<int, CatProductVariant> variants)
    {
        if (!order.StockDiscounted) return;
        foreach (var item in order.Items)
        {
            if (item.QuantityDiscounted > 0 && variants.TryGetValue(item.VariantId, out var variant))
                variant.Stock += item.QuantityDiscounted;
        }
        order.StockDiscounted = false;
    }
}

public class SetInterestAttendedHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetInterestAttendedCommand>
{
    public async Task Handle(SetInterestAttendedCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var interest = await db.InterestRequests
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("El registro de interés no existe.");
        interest.IsAttended = request.IsAttended;
        await db.SaveChangesAsync(ct);
    }
}
