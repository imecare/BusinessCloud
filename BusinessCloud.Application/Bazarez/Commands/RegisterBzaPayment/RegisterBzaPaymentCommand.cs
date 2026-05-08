using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.RegisterBzaPayment;

public record RegisterBzaPaymentCommand : IRequest<BzaPaymentResultDto>
{
    public int BzaSaleId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string? ProofImageUrl { get; init; }
    public string? Reference { get; init; }
}

public class BzaPaymentResultDto
{
    public int PaymentId { get; set; }
    public decimal NewBalance { get; set; }
    public bool SaleFullyPaid { get; set; }
}
