using MediatR;

namespace BusinessCloud.Application.Payments.Commands.MarkCommissionPaid;

public record MarkCommissionPaidCommand(
    int SaleId,
    bool Paid,
    string? Note
) : IRequest<MarkCommissionPaidResult>;

public class MarkCommissionPaidResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}