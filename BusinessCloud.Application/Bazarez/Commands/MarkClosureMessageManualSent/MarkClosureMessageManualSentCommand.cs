using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.MarkClosureMessageManualSent;

public record MarkClosureMessageManualSentCommand(int ClosureCustomerTotalId)
    : IRequest<MarkClosureMessageManualSentResultDto>;

public record MarkClosureMessageManualSentResultDto(
    int ClosureCustomerTotalId,
    string DeliveryStatus,
    DateTime MarkedAt);
