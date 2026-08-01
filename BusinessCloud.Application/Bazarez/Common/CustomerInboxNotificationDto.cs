namespace BusinessCloud.Application.Bazares.Common;

public record CustomerInboxNotificationDto(
    int Id,
    string Title,
    string Message,
    string ActionUrl,
    DateTime CreatedAt,
    DateTime? ReadAt)
{
    public bool IsRead => ReadAt.HasValue;
}
