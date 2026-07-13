using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.DeletePaymentCard;

public record DeletePaymentCardCommand(int Id) : IRequest;

public class DeletePaymentCardHandler(IBazaresDbContext context)
    : IRequestHandler<DeletePaymentCardCommand>
{
    public async Task Handle(DeletePaymentCardCommand request, CancellationToken ct)
    {
        var entity = await context.PaymentCards
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Tarjeta con Id {request.Id} no encontrada");

        // Una tarjeta que ya fue enviada para pago no se puede eliminar (solo desactivar).
        if (entity.WasSentForPayment)
        {
            throw new InvalidOperationException(
                "No se puede eliminar una tarjeta que ya fue enviada para pago. Solo puedes desactivarla.");
        }

        context.PaymentCards.Remove(entity);
        await context.SaveChangesAsync(ct);
    }
}
