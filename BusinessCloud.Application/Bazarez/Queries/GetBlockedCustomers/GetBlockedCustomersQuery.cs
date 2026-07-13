using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBlockedCustomers;

/// <summary>Lista los registros de la lista de bloqueo de clientes.</summary>
public record GetBlockedCustomersQuery(bool IncludeInactive = false) : IRequest<List<BlockedCustomerDto>>;

public record BlockedCustomerDto(
    int Id,
    string Name,
    string? FacebookName,
    string? Phone,
    string Reason,
    int? BzaCustomerId,
    bool IsActive,
    DateTime CreatedAt);

public class GetBlockedCustomersHandler(IBazaresDbContext context)
    : IRequestHandler<GetBlockedCustomersQuery, List<BlockedCustomerDto>>
{
    public async Task<List<BlockedCustomerDto>> Handle(GetBlockedCustomersQuery request, CancellationToken ct)
    {
        var query = context.BlockedCustomers.AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(b => b.IsActive);

        return await query
            .OrderByDescending(b => b.IsActive)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new BlockedCustomerDto(
                b.Id, b.Name, b.FacebookName, b.Phone, b.Reason, b.BzaCustomerId, b.IsActive, b.CreatedAt))
            .ToListAsync(ct);
    }
}
