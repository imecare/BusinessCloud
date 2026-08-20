using MediatR;
namespace BusinessCloud.Application.Bazares.Commands.UnifyDeliveryDates;
public record UnifyDeliveryDatesCommand(List<int> ClosureEventIds, DateTime DeliveryDate) : IRequest<UnifyDeliveryDatesResultDto>;
public record UnifyDeliveryDatesResultDto(List<int> ClosureEventIds, DateTime DeliveryDate, int UpdatedGroupDates);