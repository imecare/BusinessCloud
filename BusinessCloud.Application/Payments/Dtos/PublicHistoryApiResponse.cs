namespace BusinessCloud.Application.Payments.Dtos;

public class PublicHistoryApiResponse
{
    public string StatusCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public PublicHistoryLookupResponse? Data { get; set; }
}