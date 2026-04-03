namespace BusinessCloud.Application.Dashboard.Queries.GetFinancialSummary;

public record FinancialSummaryDto(
    decimal TotalSales,
    decimal TotalCosts,
    decimal TotalCommissions,
    decimal NetProfit // Utilidad Neta [cite: 26]
);