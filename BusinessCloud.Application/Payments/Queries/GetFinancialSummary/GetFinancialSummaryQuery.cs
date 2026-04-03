using MediatR;
namespace BusinessCloud.Application.Dashboard.Queries.GetFinancialSummary;

public record GetFinancialSummaryQuery(DateTime StartDate, DateTime EndDate) : IRequest<FinancialSummaryDto>;