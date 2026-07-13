using MediatR;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSaleStatus;

public class UpdateBzaSaleStatusHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<UpdateBzaSaleStatusCommand>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task Handle(UpdateBzaSaleStatusCommand request, CancellationToken cancellationToken)
    {
        // 1. Actualizar SQL
        var saleEvent = await _context.Events.FindAsync([request.SaleId], cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        saleEvent.Status = request.NewStatus;
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Auditoría en MongoDB (Histórico de transacciones)
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SaleEventStatusUpdated",
            SaleEventId = saleEvent.Id,
            Description = saleEvent.Description,
            NewStatus = request.NewStatus,
            Note = request.Note,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
    }
}