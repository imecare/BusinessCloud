using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.SetCompanyStatus;

public class SetCompanyStatusHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser,
    IValidator<SetCompanyStatusCommand> validator)
    : IRequestHandler<SetCompanyStatusCommand, SetCompanyStatusResult>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IValidator<SetCompanyStatusCommand> _validator = validator;

    public async Task<SetCompanyStatusResult> Handle(
        SetCompanyStatusCommand request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            throw new KeyNotFoundException($"La empresa '{request.TenantId}' no existe.");

        tenant.IsActive = request.IsActive;

        var subscription = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId, cancellationToken);

        if (subscription is not null)
        {
            subscription.IsManuallySuspended = !request.IsActive;
            subscription.UpdatedAt = DateTime.UtcNow;
            subscription.UpdatedBy = _currentUser.UserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var status = subscription is not null
            ? subscription.EvaluateStatus(DateTime.UtcNow).ToString()
            : (request.IsActive ? SubscriptionStatus.Active : SubscriptionStatus.Suspended).ToString();

        return new SetCompanyStatusResult(tenant.Id, tenant.IsActive, status);
    }
}
