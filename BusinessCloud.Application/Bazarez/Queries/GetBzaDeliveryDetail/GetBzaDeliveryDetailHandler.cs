using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaDeliveryDetail;

public class GetBzaDeliveryDetailHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaDeliveryDetailQuery, BzaDeliveryDetailDto>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Programada" },
        { 2, "En Proceso" },
        { 3, "Completada" },
        { 4, "Cancelada" }
    };

    public async Task<BzaDeliveryDetailDto> Handle(GetBzaDeliveryDetailQuery request, CancellationToken cancellationToken)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.CollectorGroup)
            .Include(d => d.Items)
                .ThenInclude(i => i.Sale)
                    .ThenInclude(s => s.SoldProducts)
                        .ThenInclude(p => p.Customer)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Entrega no encontrada.");

        return new BzaDeliveryDetailDto
        {
            Id = delivery.Id,
            GroupId = delivery.BzaCollectorGroupId,
            GroupDescription = delivery.CollectorGroup.Description,
            DeliveryDate = delivery.DeliveryDate,
            Status = delivery.Status,
            StatusName = StatusNames.GetValueOrDefault(delivery.Status, "Desconocido"),
            Notes = delivery.Notes,
            CreatedAt = delivery.CreatedAt,
            Items = delivery.Items.Select(i =>
            {
                var firstCustomer = i.Sale.SoldProducts.FirstOrDefault()?.Customer;
                return new BzaDeliveryItemDto
                {
                    Id = i.Id,
                    SaleId = i.BzaSaleId,
                    SaleDescription = i.Sale.Description,
                    CustomerName = firstCustomer?.Name ?? "Varios Clientes",
                    Delivered = i.Delivered,
                    DeliveredAt = i.DeliveredAt,
                    Notes = i.Notes
                };
            }).ToList()
        };
    }
}
