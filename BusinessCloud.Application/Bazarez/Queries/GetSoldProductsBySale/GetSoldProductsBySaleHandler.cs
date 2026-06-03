using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetSoldProductsBySale;

public class GetSoldProductsBySaleHandler(IBazaresDbContext context)
    : IRequestHandler<GetSoldProductsBySaleQuery, SoldProductsBySaleDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<SoldProductsBySaleDto> Handle(GetSoldProductsBySaleQuery request, CancellationToken cancellationToken)
    {
        var saleEvent = await _context.Sales
            .FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        var soldProductsQuery = _context.SoldProducts
            .Include(p => p.Customer)
            .Where(p => p.BzaSaleId == request.BzaSaleId);

        // Filtrar por cliente si se especifica
        if (request.CustomerId.HasValue)
        {
            soldProductsQuery = soldProductsQuery.Where(p => p.BzaCustomerId == request.CustomerId.Value);
        }

        var soldProducts = await soldProductsQuery
            .OrderBy(p => p.Customer.Name)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new SoldProductItemDto
            {
                Id = p.Id,
                CustomerId = p.BzaCustomerId,
                CustomerName = p.Customer.Name,
                CustomerPhone = p.Customer.Phone ?? string.Empty,
                Description = p.Description,
                Price = p.Price,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new SoldProductsBySaleDto
        {
            BzaSaleId = saleEvent.Id,
            EventDescription = saleEvent.Description,
            DeliveryDate = saleEvent.DeliveryDate,
            Items = soldProducts
        };
    }
}
