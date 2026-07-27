using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CloseClosureDelivery;

public class CloseClosureDeliveryHandler(IBazaresDbContext context)
    : IRequestHandler<CloseClosureDeliveryCommand, CloseClosureDeliveryResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<CloseClosureDeliveryResultDto> Handle(CloseClosureDeliveryCommand request, CancellationToken cancellationToken)
    {
        var ev = await _context.ClosureEvents
            .Include(c => c.DeliveryProofs)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, cancellationToken)
            ?? throw new KeyNotFoundException("El evento de cierre no existe.");

        if (!ev.InDeliveryProcess)
            throw new InvalidOperationException("El evento aún no está en proceso de entrega.");

        if (ev.DeliveryProofs.Count == 0)
            throw new InvalidOperationException("Debes subir al menos un comprobante de entrega antes de cerrar.");

        ev.Delivered = true;
        ev.DeliveredAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new CloseClosureDeliveryResultDto
        {
            Success = true,
            Delivered = ev.Delivered,
            DeliveredAt = ev.DeliveredAt
        };
    }
}