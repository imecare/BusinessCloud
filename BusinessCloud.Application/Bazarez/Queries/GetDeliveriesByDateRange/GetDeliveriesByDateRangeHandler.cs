using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetDeliveriesByDateRange;

public class GetDeliveriesByDateRangeHandler : IRequestHandler<GetDeliveriesByDateRangeQuery, List<BzaDeliveryByDateDto>>
{
    private readonly IBazaresDbContext _context;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Programada" },
        { 2, "En Proceso" },
        { 3, "Completada" },
        { 4, "Cancelada" }
    };

    public GetDeliveriesByDateRangeHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<BzaDeliveryByDateDto>> Handle(GetDeliveriesByDateRangeQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Deliveries
            .Include(d => d.CollectorGroup)
            .Include(d => d.Items)
            .Where(d => d.DeliveryDate >= request.FromDate && d.DeliveryDate <= request.ToDate.AddDays(1).AddSeconds(-1));

        // Filtro opcional por grupo
        if (request.BzaCollectorGroupId.HasValue)
            query = query.Where(d => d.BzaCollectorGroupId == request.BzaCollectorGroupId.Value);

        var deliveries = await query
            .OrderByDescending(d => d.DeliveryDate)
            .Select(d => new BzaDeliveryByDateDto
            {
                Id = d.Id,
                GroupId = d.BzaCollectorGroupId,
                GroupDescription = d.CollectorGroup.Description,
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
