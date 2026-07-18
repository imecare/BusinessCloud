using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.Notifications;

public record SubscribeToWebPushCommand(
    string UploadToken,
    string Endpoint,
    string P256dh,
    string Auth) : IRequest<SubscribeToWebPushResultDto>;

public class SubscribeToWebPushResultDto
{
    public bool Success { get; set; }
    public int SubscriptionId { get; set; }
}

public class SubscribeToWebPushHandler(IBazaresDbContext context)
    : IRequestHandler<SubscribeToWebPushCommand, SubscribeToWebPushResultDto>
{
    public async Task<SubscribeToWebPushResultDto> Handle(SubscribeToWebPushCommand request, CancellationToken cancellationToken)
    {
        var total = await context.ClosureCustomerTotals
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UploadToken == request.UploadToken, cancellationToken)
            ?? throw new KeyNotFoundException("El enlace no es valido o ha expirado.");

        var endpoint = request.Endpoint.Trim();
        var p256dh = request.P256dh.Trim();
        var auth = request.Auth.Trim();

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth))
            throw new InvalidOperationException("La suscripcion Web Push no contiene las llaves requeridas.");

        var existing = await context.CustomerNotificationSubscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == total.TenantId
                                      && s.BzaCustomerId == total.BzaCustomerId
                                      && s.Endpoint == endpoint,
                cancellationToken);

        if (existing is null)
        {
            existing = new Domain.Bazares.Entities.BzaCustomerNotificationSubscription
            {
                TenantId = total.TenantId,
                BzaCustomerId = total.BzaCustomerId,
                BzaClosureCustomerTotalId = total.Id,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth,
                IsActive = true,
                LastFailureReason = null,
                LastFailedPushAt = null
            };
            context.CustomerNotificationSubscriptions.Add(existing);
        }
        else
        {
            existing.BzaClosureCustomerTotalId = total.Id;
            existing.P256dh = p256dh;
            existing.Auth = auth;
            existing.IsActive = true;
            existing.LastFailureReason = null;
            existing.LastFailedPushAt = null;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new SubscribeToWebPushResultDto
        {
            Success = true,
            SubscriptionId = existing.Id
        };
    }
}
