namespace BusinessCloud.Infrastructure.Common.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string ConnectionString { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = "contacto@bcloud.com.mx";
    public string SenderName { get; set; } = "BazarHub";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) &&
        !string.IsNullOrWhiteSpace(SenderAddress);
}
