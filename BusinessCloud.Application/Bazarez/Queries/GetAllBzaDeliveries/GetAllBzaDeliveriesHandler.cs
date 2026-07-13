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
        // Materializar datos primero para evitar memory leak en proyección con Dictionary
        var rawDeliveries = await _context.Deliveries
            .Include(d => d.CollectorGroup)
            .Include(d => d.Items)
            .OrderByDescending(d => d.DeliveryDate)
            .Select(d => new
            {
                d.Id,
                GroupId = d.BzaCollectorGroupId,
                GroupDescription = d.CollectorGroup.Description,
                d.DeliveryDate,
                d.Status,
                d.Notes,
                ItemCount = d.Items.Count,
                d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Mapear StatusName en memoria (evita EF Core memory leak warning)
        return rawDeliveries.Select(d => new BzaDeliveryListDto
        {
            Id = d.Id,
            GroupId = d.GroupId,
            GroupDescription = d.GroupDescription,
            DeliveryDate = d.DeliveryDate,
            Status = d.Status,
            StatusName = StatusNames.GetValueOrDefault(d.Status, "Desconocido"),
            Notes = d.Notes,
            ItemCount = d.ItemCount,
            CreatedAt = d.CreatedAt
        }).ToList();
    }
}
