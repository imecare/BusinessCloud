using FluentValidation;
namespace BusinessCloud.Application.Bazares.Commands.ProcessDeliveryBatch;
public class ProcessDeliveryBatchValidator : AbstractValidator<ProcessDeliveryBatchCommand>
{
 public ProcessDeliveryBatchValidator()
 {
  RuleFor(x => x.ClosureEventIds).NotEmpty().Must(x => x.Count <= 100); RuleForEach(x => x.ClosureEventIds).GreaterThan(0);
  When(x => x.PendingAction == DeliveryPendingAction.MoveToExisting, () => RuleFor(x => x.TargetClosureEventId).NotNull().GreaterThan(0));
  When(x => x.PendingAction == DeliveryPendingAction.MoveToNew, () => { RuleFor(x => x.NewDeliveryDate).NotNull(); RuleFor(x => x.NewPaymentDeadline).NotNull(); });
 }
}