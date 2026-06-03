using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetDeliveriesByDateRange;

public record GetDeliveriesByDateRangeQuery : IRequest<List<BzaDeliveryByDateDto>>
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public int? BzaCollectorGroupId { get; init; }
}

public class BzaDeliveryByDateDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupDescription { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
