using System.Text.Json.Serialization;

namespace BusinessCloud.Api.Controllers.Shared;

public class WhatsAppWebhookPayload
{
    [JsonPropertyName("entry")]
    public List<WhatsAppWebhookEntryPayload> Entry { get; set; } = new();
}

public class WhatsAppWebhookEntryPayload
{
    [JsonPropertyName("changes")]
    public List<WhatsAppWebhookChangePayload> Changes { get; set; } = new();
}

public class WhatsAppWebhookChangePayload
{
    [JsonPropertyName("value")]
    public WhatsAppWebhookValuePayload? Value { get; set; }
}

public class WhatsAppWebhookValuePayload
{
    [JsonPropertyName("statuses")]
    public List<WhatsAppWebhookStatusPayload> Statuses { get; set; } = new();

    [JsonPropertyName("messages")]
    public List<WhatsAppWebhookMessagePayload> Messages { get; set; } = new();
}

public class WhatsAppWebhookStatusPayload
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("recipient_id")]
    public string? RecipientId { get; set; }

    [JsonPropertyName("errors")]
    public List<WhatsAppWebhookErrorPayload> Errors { get; set; } = new();
}

public class WhatsAppWebhookMessagePayload
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public WhatsAppWebhookTextPayload? Text { get; set; }
}

public class WhatsAppWebhookTextPayload
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

public class WhatsAppWebhookErrorPayload
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error_data")]
    public WhatsAppWebhookErrorDataPayload? ErrorData { get; set; }
}

public class WhatsAppWebhookErrorDataPayload
{
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}