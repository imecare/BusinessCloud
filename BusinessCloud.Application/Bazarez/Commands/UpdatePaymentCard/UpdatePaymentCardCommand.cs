using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;

namespace BusinessCloud.Application.Bazares.Commands.UpdatePaymentCard;

public record UpdatePaymentCardCommand(int Id, string CardNumber, string CardHolderName, string? Bank, string? Notes, bool IsActive)
    : IRequest
{
    /// <summary>Identificador del desafío OTP (verificación por WhatsApp del SuperAdmin).</summary>
    public string? ChallengeId { get; init; }

    /// <summary>Código de verificación ingresado por el SuperAdmin.</summary>
    public string? VerificationCode { get; init; }
}

public class UpdatePaymentCardHandler(IBazaresDbContext context)
    : IRequestHandler<UpdatePaymentCardCommand>
{
    public async Task Handle(UpdatePaymentCardCommand request, CancellationToken ct)
    {
        if (!CardNumberValidator.IsValid(request.CardNumber))
        {
            throw new ArgumentException("El número de tarjeta no tiene un formato válido.");
        }

        var entity = await context.PaymentCards
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Tarjeta con Id {request.Id} no encontrada");

        // Una tarjeta que ya fue enviada para pago no se puede modificar (solo activar/desactivar).
        if (entity.WasSentForPayment)
        {
            throw new InvalidOperationException(
                "No se puede modificar una tarjeta que ya fue enviada para pago. Solo puedes activarla o desactivarla.");
        }

        entity.CardNumber = request.CardNumber.Trim();
        entity.CardHolderName = request.CardHolderName.Trim();
        entity.Bank = string.IsNullOrWhiteSpace(request.Bank) ? null : request.Bank.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entity.IsActive = request.IsActive;

        await context.SaveChangesAsync(ct);
    }
}
