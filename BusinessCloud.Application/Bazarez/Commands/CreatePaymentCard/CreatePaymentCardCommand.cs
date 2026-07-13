using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreatePaymentCard;

public record CreatePaymentCardCommand(string CardNumber, string CardHolderName, string? Bank, string? Notes, bool IsActive = true)
    : IRequest<int>
{
    /// <summary>Identificador del desafío OTP (verificación por WhatsApp del SuperAdmin).</summary>
    public string? ChallengeId { get; init; }

    /// <summary>Código de verificación ingresado por el SuperAdmin.</summary>
    public string? VerificationCode { get; init; }
}

public class CreatePaymentCardHandler(IBazaresDbContext context)
    : IRequestHandler<CreatePaymentCardCommand, int>
{
    public async Task<int> Handle(CreatePaymentCardCommand request, CancellationToken ct)
    {
        if (!CardNumberValidator.IsValid(request.CardNumber))
        {
            throw new ArgumentException("El número de tarjeta no tiene un formato válido.");
        }

        var entity = new BzaPaymentCard
        {
            CardNumber = request.CardNumber.Trim(),
            CardHolderName = request.CardHolderName.Trim(),
            Bank = string.IsNullOrWhiteSpace(request.Bank) ? null : request.Bank.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsActive = request.IsActive
        };

        context.PaymentCards.Add(entity);
        await context.SaveChangesAsync(ct);

        return entity.Id;
    }
}
