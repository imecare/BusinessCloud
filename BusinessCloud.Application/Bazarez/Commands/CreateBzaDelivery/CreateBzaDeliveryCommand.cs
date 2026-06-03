using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaDelivery;

public record CreateBzaDeliveryCommand : IRequest<int>
{
    public int BzaCollectorGroupId { get; init; }
    public DateTime DeliveryDate { get; init; }
    public string? Notes { get; init; }
}
