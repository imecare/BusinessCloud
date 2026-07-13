using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.SetPaymentCardActive;

/// <summary>
/// Activa o desactiva una tarjeta. Siempre permitido, incluso para tarjetas
/// que ya fueron enviadas para pago (que no se pueden eliminar ni modificar).
/// </summary>
public record SetPaymentCardActiveCommand(int Id, bool IsActive) : IRequest;

public class SetPaymentCardActiveHandler(IBazaresDbContext context)
    : IRequestHandler<SetPaymentCardActiveCommand>
{
    public async Task Handle(SetPaymentCardActiveCommand request, CancellationToken ct)
    {
        var entity = await context.PaymentCards
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Tarjeta con Id {request.Id} no encontrada");

        entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(ct);
    }
}
