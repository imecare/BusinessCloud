using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaDelivery;

public record UpdateBzaDeliveryCommand : IRequest<bool>
{
    public int Id { get; init; }
    public DateTime DeliveryDate { get; init; }
    public int Status { get; init; }
    public string? Notes { get; init; }
}
