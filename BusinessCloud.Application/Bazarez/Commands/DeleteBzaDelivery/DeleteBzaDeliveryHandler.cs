using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteBzaDelivery;

public class DeleteBzaDeliveryHandler : IRequestHandler<DeleteBzaDeliveryCommand, DeleteBzaDeliveryResult>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public DeleteBzaDeliveryHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<DeleteBzaDeliveryResult> Handle(DeleteBzaDeliveryCommand request, CancellationToken cancellationToken)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (delivery is null)
            return new DeleteBzaDeliveryResult(false, "Entrega no encontrada.");

        if (delivery.Items.Any())
            return new DeleteBzaDeliveryResult(false, "No se puede eliminar una entrega que tiene items asociados.");

        _context.Deliveries.Remove(delivery);
        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_DeliveryDeleted",
            DeliveryId = request.Id,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return new DeleteBzaDeliveryResult(true, "Entrega eliminada correctamente.");
    }
}
