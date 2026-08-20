using FluentValidation;
namespace BusinessCloud.Application.Bazares.Queries.GetDeliveryLogisticsBatch;
public class GetDeliveryLogisticsBatchValidator : AbstractValidator<GetDeliveryLogisticsBatchQuery>
{
 public GetDeliveryLogisticsBatchValidator() { RuleFor(x => x.ClosureEventIds).NotEmpty().Must(x => x.Count <= 100); RuleForEach(x => x.ClosureEventIds).GreaterThan(0); }
}