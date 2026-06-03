using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteBzaSale;

public class DeleteBzaSaleHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<DeleteBzaSaleCommand, DeleteBzaSaleResult>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<DeleteBzaSaleResult> Handle(DeleteBzaSaleCommand request, CancellationToken cancellationToken)
    {
        var saleEvent = await _context.Sales
            .Include(s => s.SoldProducts)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (saleEvent is null)
            return new DeleteBzaSaleResult(false, "Evento de Venta no encontrado.");

        // Solo se puede eliminar si ningún cliente tiene productos registrados
        if (saleEvent.SoldProducts.Count != 0)
            return new DeleteBzaSaleResult(false, "No se puede eliminar un Evento de Venta que tiene productos vendidos registrados.");

        _context.Sales.Remove(saleEvent);
        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SaleEventDeleted",
            SaleEventId = request.Id,
            Description = saleEvent.Description,
            Reason = request.Reason,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return new DeleteBzaSaleResult(true, "Evento de Venta eliminado correctamente.");
    }
}
