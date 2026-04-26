using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;

public class GetBzaSaleDetailHandler : IRequestHandler<GetBzaSaleDetailQuery, BzaSaleDetailDto>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public GetBzaSaleDetailHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<BzaSaleDetailDto> Handle(GetBzaSaleDetailQuery request, CancellationToken cancellationToken)
    {
        // 1. Consultar SQL Server (Datos relacionales)
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (sale == null) throw new KeyNotFoundException("Venta no encontrada"); 

        // 2. Consultar MongoDB (Historial de auditoría)
        // Buscamos todos los logs que tengan el SaleId de esta venta
        var mongoLogs = await _mongoContext.GetAuditLogsBySaleIdAsync(sale.Id, cancellationToken);

        // 3. Mapear y Retornar
        return new BzaSaleDetailDto(
            sale.Id,
            sale.Description,
            sale.Total,
            sale.Status,
            sale.Customer.Name,
            sale.Products.Select(p => new BzaSaleProductDto(p.Description, p.Price)).ToList(),
            mongoLogs.Select(l => new BzaSaleAuditDto(l.Event, l.Timestamp, l.Details)).ToList()
        );
    }
}