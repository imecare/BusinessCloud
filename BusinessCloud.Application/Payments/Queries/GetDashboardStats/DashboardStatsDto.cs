namespace BusinessCloud.Application.Payments.Queries.GetDashboardStats;

public class DashboardStatsDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal PendingCollection { get; set; }
    public decimal PendingCommissions { get; set; }
    public decimal PaidCommissions { get; set; }
    public int ActiveCustomers { get; set; }
    public int ActiveSellers { get; set; }
    public decimal TotalProfit { get; set; }
    /// <summary>Total de gastos/compras registrados en el mes actual.</summary>
    public decimal MonthlyExpenses { get; set; }
}
