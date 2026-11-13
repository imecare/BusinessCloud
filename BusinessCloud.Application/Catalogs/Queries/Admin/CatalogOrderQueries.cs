using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Queries.Admin;

public record GetOrdersQuery(string? Status, int Page, int PageSize) : IRequest<PagedResult<OrderListDto>>;
public record GetOrderQuery(int Id) : IRequest<OrderDto>;

public class GetOrdersHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderListDto>>
{
    public async Task<PagedResult<OrderListDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);

        var query = db.Orders.Where(o => o.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<CatOrderStatus>(request.Status, true, out var status))
                throw new ArgumentException("Estado de pedido inválido.");
            query = query.Where(o => o.Status == status);
        }

        var total = await query.CountAsync(ct);
        var orders = await query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<OrderListDto>(orders.Select(CatalogMappers.ToListDto).ToList(), total, page, pageSize);
    }
}

public class GetOrderHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var order = await db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("El pedido no existe.");
        return CatalogMappers.ToDto(order);
    }
}
