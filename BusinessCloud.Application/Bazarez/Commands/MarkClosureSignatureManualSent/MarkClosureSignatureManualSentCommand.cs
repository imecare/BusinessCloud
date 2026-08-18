using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.MarkClosureSignatureManualSent;

public record MarkClosureSignatureManualSentCommand(int ClosureCustomerTotalId, bool Sent)
    : IRequest<MarkClosureSignatureManualSentResultDto>;

public record MarkClosureSignatureManualSentResultDto(int ClosureCustomerTotalId, bool Sent, DateTime? SentAt);
