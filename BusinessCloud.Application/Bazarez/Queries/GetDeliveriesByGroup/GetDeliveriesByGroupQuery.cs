using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetDeliveriesByGroup;

public record GetDeliveriesByGroupQuery(int BzaCollectorGroupId) : IRequest<List<BzaDeliveryByGroupDto>>;

public class BzaDeliveryByGroupDto
{
    public int Id { get; set; }
    public DateTime DeliveryDate { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
