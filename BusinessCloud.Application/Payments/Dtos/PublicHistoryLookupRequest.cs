namespace BusinessCloud.Application.Payments.Dtos;

public class PublicHistoryLookupRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
}