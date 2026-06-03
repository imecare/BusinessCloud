using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaDeliveryDetail;

public record GetBzaDeliveryDetailQuery(int Id) : IRequest<BzaDeliveryDetailDto>;

public class BzaDeliveryDetailDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupDescription { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BzaDeliveryItemDto> Items { get; set; } = new();
}

public class BzaDeliveryItemDto
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string? SaleDescription { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public bool Delivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }
}
