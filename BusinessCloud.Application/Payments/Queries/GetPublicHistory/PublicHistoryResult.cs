using BusinessCloud.Application.Payments.Dtos;

namespace BusinessCloud.Application.Payments.Queries.GetPublicHistory;

public class PublicHistoryResult
{
    public bool CustomerFound { get; set; }
    public PublicHistoryLookupResponse? Data { get; set; }
}