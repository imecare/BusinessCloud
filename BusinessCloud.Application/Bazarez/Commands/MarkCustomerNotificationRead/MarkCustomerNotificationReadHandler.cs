using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.MarkCustomerNotificationRead;

public class MarkCustomerNotificationReadHandler(IBazaresDbContext context)
    : IRequestHandler<MarkCustomerNotificationReadCommand, CustomerInboxNotificationDto>
{
    public async Task<CustomerInboxNotificationDto> Handle(
        MarkCustomerNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await context.CustomerInboxNotifications
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.NotificationId, cancellationToken)
            ?? throw new KeyNotFoundException("La notificacion no existe.");

        var hasAccess = request.AccessKind switch
        {
            CustomerNotificationAccessKind.Portal => await context.Customers
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == notification.BzaCustomerId
                    && c.TenantId == notification.TenantId
                    && c.PortalToken == request.AccessToken, cancellationToken),
            CustomerNotificationAccessKind.ClosureTotal => await context.ClosureCustomerTotals
                .IgnoreQueryFilters()
                .AnyAsync(t => t.BzaCustomerId == notification.BzaCustomerId
                    && t.TenantId == notification.TenantId
                    && t.UploadToken == request.AccessToken, cancellationToken),
            _ => false,
        };

        if (!hasAccess)
            throw new KeyNotFoundException("La notificacion no existe.");

        if (!notification.ReadAt.HasValue)
        {
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = notification.ReadAt;
            await context.SaveChangesAsync(cancellationToken);
        }

        return new CustomerInboxNotificationDto(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.CreatedAt,
            notification.ReadAt);
    }
}
