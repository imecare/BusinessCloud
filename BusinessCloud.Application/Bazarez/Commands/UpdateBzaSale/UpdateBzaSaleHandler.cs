using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSale;

public class UpdateBzaSaleHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<UpdateBzaSaleCommand, bool>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<bool> Handle(UpdateBzaSaleCommand request, CancellationToken cancellationToken)
    {
        var saleEvent = await _context.Events.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (saleEvent is null) return false;

        saleEvent.Description = request.Description;
        saleEvent.PaymentDeadline = request.PaymentDeadline;
        saleEvent.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SaleEventUpdated",
            SaleEventId = saleEvent.Id,
            Description = saleEvent.Description,
            PaymentDeadline = saleEvent.PaymentDeadline,
            Status = saleEvent.Status,
            Timestamp = DateTime.UtcNow,
            Details = "Evento de Venta actualizado."
        }, cancellationToken);

        return true;
    }
}
