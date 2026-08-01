using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureWhatsAppStatus;

public record GetClosureWhatsAppStatusQuery(int ClosureEventId) : IRequest<ClosureWhatsAppStatusDto>;

public class ClosureWhatsAppStatusDto
{
    public int ClosureEventId { get; set; }
    public int Total { get; set; }
    public int Processing { get; set; }
    public int Delivered { get; set; }
    public int Read { get; set; }
    public int Failed { get; set; }
    public int NoWhatsApp { get; set; }
    public int Unconfirmed { get; set; }
    public int InboxUnread { get; set; }
    public int InboxRead { get; set; }
    public List<ClosureWhatsAppStatusItemDto> Items { get; set; } = [];
}

public record ClosureWhatsAppStatusItemDto(
    int ClosureCustomerTotalId,
    int CustomerId,
    string CustomerName,
    string DeliveryStatus,
    DateTime? SentAt,
    DateTime? StatusUpdatedAt,
    string? Error,
    bool InboxNotificationCreated,
    DateTime? InboxReadAt);
