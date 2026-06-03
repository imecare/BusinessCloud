using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaDelivery;

public class UpdateBzaDeliveryHandler : IRequestHandler<UpdateBzaDeliveryCommand, bool>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public UpdateBzaDeliveryHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<bool> Handle(UpdateBzaDeliveryCommand request, CancellationToken cancellationToken)
    {
        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (delivery is null) return false;

        delivery.DeliveryDate = request.DeliveryDate;
        delivery.Status = request.Status;
        delivery.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_DeliveryUpdated",
            DeliveryId = delivery.Id,
            NewStatus = delivery.Status,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
