using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetAllBzaDeliveries;

public class GetAllBzaDeliveriesHandler : IRequestHandler<GetAllBzaDeliveriesQuery, List<BzaDeliveryListDto>>
{
    private readonly IBazaresDbContext _context;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Programada" },
        { 2, "En Proceso" },
        { 3, "Completada" },
        { 4, "Cancelada" }
    };

    public GetAllBzaDeliveriesHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<BzaDeliveryListDto>> Handle(GetAllBzaDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var deliveries = await _context.Deliveries
            .Include(d => d.CollectorGroup)
            .Include(d => d.Items)
            .OrderByDescending(d => d.DeliveryDate)
            .Select(d => new BzaDeliveryListDto
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
