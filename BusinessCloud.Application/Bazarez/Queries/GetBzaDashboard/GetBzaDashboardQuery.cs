using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;

public record GetBzaDashboardQuery : IRequest<BzaDashboardDto>;

public class BzaDashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalCollectors { get; set; }
    public decimal WeeklySales { get; set; }
    public decimal WeeklyCollected { get; set; }
    public decimal PendingCollection { get; set; }
    public int PendingSales { get; set; }
    public int PaidSales { get; set; }
    public int DeliveredSales { get; set; }
    public List<CollectorVolumeDto> CollectorVolume { get; set; } = new();
    public List<DelinquentCustomerDto> Delinquents { get; set; } = new();
}

public class CollectorVolumeDto
{
    public string CollectorName { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public int PackageCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class DelinquentCustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal AmountOwed { get; set; }
    public DateTime? OldestDeadline { get; set; }
    public int OverdueSales { get; set; }
}
