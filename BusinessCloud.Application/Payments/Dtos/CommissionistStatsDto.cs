namespace BusinessCloud.Application.Payments.Dtos;

public class CommissionistStatsDto
{
    public int TotalCustomers { get; set; }
    public int TotalSales { get; set; }
    public int PaidSales { get; set; }
    public decimal PendingCommissionsAmount { get; set; }
    public decimal PaidCommissionsAmount { get; set; }
    public int PendingCommissionsCount { get; set; }
    public int PaidCommissionsCount { get; set; }
}