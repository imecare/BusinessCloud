using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;

public record GetBzaDashboardQuery : IRequest<BzaDashboardDto>;

public class BzaDashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalCollectors { get; set; }
    public decimal WeeklySales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public int PendingSales { get; set; }
    public int PaidSales { get; set; }
    public int DeliveredSales { get; set; }
    public int DelinquentsCount { get; set; }
    public int MessagesAvailable { get; set; }
    public List<CollectorVolumeDto> CollectorVolumes { get; set; } = new();
    public List<DelinquentCustomerDto> Delinquents { get; set; } = new();
}

public class CollectorVolumeDto
{
    public int CollectorId { get; set; }
    public string CollectorName { get; set; } = string.Empty;
    public int? BzaCollectorGroupId { get; set; }
    public string? GroupDescription { get; set; }
    public int CustomerCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCollected { get; set; }
}

public class DelinquentCustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public int OverdueSales { get; set; }
}
