using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetDeliveriesByGroup;

public class GetDeliveriesByGroupHandler : IRequestHandler<GetDeliveriesByGroupQuery, List<BzaDeliveryByGroupDto>>
{
    private readonly IBazaresDbContext _context;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Programada" },
        { 2, "En Proceso" },
        { 3, "Completada" },
        { 4, "Cancelada" }
    };

    public GetDeliveriesByGroupHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<BzaDeliveryByGroupDto>> Handle(GetDeliveriesByGroupQuery request, CancellationToken cancellationToken)
    {
        var deliveries = await _context.Deliveries
            .Include(d => d.Items)
            .Where(d => d.BzaCollectorGroupId == request.BzaCollectorGroupId)
            .OrderByDescending(d => d.DeliveryDate)
            .Select(d => new BzaDeliveryByGroupDto
            {
                Id = d.Id,
                DeliveryDate = d.DeliveryDate,
                Status = d.Status,
                StatusName = StatusNames.ContainsKey(d.Status) ? StatusNames[d.Status] : "Desconocido",
                Notes = d.Notes,
                ItemCount = d.Items.Count,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return deliveries;
    }
}
