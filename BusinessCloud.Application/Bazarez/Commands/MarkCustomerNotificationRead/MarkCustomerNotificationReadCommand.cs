using BusinessCloud.Application.Bazares.Common;
using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.MarkCustomerNotificationRead;

public enum CustomerNotificationAccessKind
{
    Portal = 1,
    ClosureTotal = 2,
}

public record MarkCustomerNotificationReadCommand(
    string AccessToken,
    int NotificationId,
    CustomerNotificationAccessKind AccessKind) : IRequest<CustomerInboxNotificationDto>;
