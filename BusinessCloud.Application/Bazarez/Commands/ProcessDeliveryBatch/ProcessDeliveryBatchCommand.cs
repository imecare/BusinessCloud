using MediatR;
namespace BusinessCloud.Application.Bazares.Commands.ProcessDeliveryBatch;
public enum DeliveryPendingAction { Cancel = 0, MoveToExisting = 1, MoveToNew = 2 }
public record ProcessDeliveryBatchCommand(List<int> ClosureEventIds, DeliveryPendingAction PendingAction, int? TargetClosureEventId = null, DateTime? NewDeliveryDate = null, DateTime? NewPaymentDeadline = null) : IRequest<ProcessDeliveryBatchResultDto>;
public record ProcessDeliveryBatchResultDto(List<int> ClosureEventIds, int PendingAffected, int? TargetClosureEventId);