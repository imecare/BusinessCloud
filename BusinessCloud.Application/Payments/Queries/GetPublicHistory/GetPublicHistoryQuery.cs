using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetPublicHistory;

public record GetPublicHistoryQuery(
    string Phone,
    string Rfc,
    string CompanyCode
) : IRequest<PublicHistoryResult>;