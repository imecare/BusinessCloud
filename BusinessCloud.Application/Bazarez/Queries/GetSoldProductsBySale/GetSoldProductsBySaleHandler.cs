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
        var saleEvent = await _context.Events
            .FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        var soldProductsQuery = _context.SoldProducts
            .Include(p => p.Sale).ThenInclude(s => s.Customer)
            .Where(p => p.Sale.BzaEventId == request.BzaSaleId);

        // Filtrar por cliente si se especifica
        if (request.CustomerId.HasValue)
        {
            soldProductsQuery = soldProductsQuery.Where(p => p.Sale.BzaCustomerId == request.CustomerId.Value);
        }

        var soldProducts = await soldProductsQuery
            .OrderBy(p => p.Sale.Customer.Name)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new SoldProductItemDto
            {
                Id = p.Id,
                CustomerId = p.Sale.BzaCustomerId,
                CustomerName = p.Sale.Customer.Name,
                CustomerPhone = p.Sale.Customer.Phone ?? string.Empty,
                Description = p.Description,
                Price = p.Price,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new SoldProductsBySaleDto
        {
            BzaSaleId = saleEvent.Id,
            EventDescription = saleEvent.Description,
            Items = soldProducts
        };
    }
}
