using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.UpsertSubscription;

public class UpsertSubscriptionHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser,
    IValidator<UpsertSubscriptionCommand> validator)
    : IRequestHandler<UpsertSubscriptionCommand, int>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IValidator<UpsertSubscriptionCommand> _validator = validator;

    public async Task<int> Handle(UpsertSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
            throw new KeyNotFoundException($"La empresa '{request.TenantId}' no existe.");

        var subscription = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId, cancellationToken);

        var normalizedPhone = string.IsNullOrWhiteSpace(request.OwnerPhone)
            ? null
            : new string(request.OwnerPhone.Where(char.IsDigit).ToArray());

        if (subscription is null)
        {
            subscription = new TenantSubscription
            {
                TenantId = request.TenantId,
                StartDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
            };
            _context.TenantSubscriptions.Add(subscription);
        }
        else
        {
            subscription.UpdatedAt = DateTime.UtcNow;
            subscription.UpdatedBy = _currentUser.UserId;
        }

        subscription.PlanName = request.PlanName.Trim();
        subscription.Period = request.Period;
        subscription.Price = request.Price;
        subscription.Currency = request.Currency.Trim().ToUpperInvariant();
        subscription.PaidUntil = request.PaidUntil.Date;
        subscription.GraceDays = request.GraceDays;
        subscription.OwnerName = request.OwnerName?.Trim();
        subscription.OwnerPhone = normalizedPhone;
        subscription.SellerId = request.SellerId;
        subscription.CommissionInitialAmount = request.CommissionInitialAmount;
        subscription.CommissionMonthlyPercent = request.CommissionMonthlyPercent;
        subscription.Notes = request.Notes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        // Genera el asiento de comisión inicial una sola vez por venta (tenant + comisionista).
        if (request.SellerId.HasValue && request.CommissionInitialAmount > 0)
        {
            var sellerExists = await _context.SystemSellers
                .AnyAsync(s => s.Id == request.SellerId.Value, cancellationToken);

            if (sellerExists)
            {
                var alreadyHasInitial = await _context.SellerCommissions.AnyAsync(
                    c => c.TenantId == request.TenantId
                        && c.SystemSellerId == request.SellerId.Value
                        && c.Type == CommissionType.Initial,
                    cancellationToken);

                if (!alreadyHasInitial)
                {
                    _context.SellerCommissions.Add(new SellerCommission
                    {
                        SystemSellerId = request.SellerId.Value,
                        TenantId = request.TenantId,
                        Type = CommissionType.Initial,
                        BaseAmount = 0,
                        Percent = 0,
                        Amount = request.CommissionInitialAmount,
                        PeriodDate = DateTime.UtcNow,
                        Notes = "Pago inicial por venta del sistema.",
                    });
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return subscription.Id;
    }
}
