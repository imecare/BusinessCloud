using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.RegisterCompanyPayment;

public class RegisterCompanyPaymentHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser,
    IValidator<RegisterCompanyPaymentCommand> validator)
    : IRequestHandler<RegisterCompanyPaymentCommand, RegisterCompanyPaymentResult>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IValidator<RegisterCompanyPaymentCommand> _validator = validator;

    public async Task<RegisterCompanyPaymentResult> Handle(
        RegisterCompanyPaymentCommand request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var subscription = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId, cancellationToken);

        if (subscription is null)
            throw new KeyNotFoundException(
                $"La empresa '{request.TenantId}' no tiene una suscripción configurada. Crea la suscripción antes de registrar pagos.");

        var paymentDate = (request.PaymentDate ?? DateTime.UtcNow).Date;

        // Si la suscripción sigue vigente, se acumula desde la fecha pagada;
        // si ya venció, la extensión parte de la fecha del pago.
        var baseDate = subscription.PaidUntil.Date >= paymentDate
            ? subscription.PaidUntil.Date
            : paymentDate;

        var monthsToAdd = (int)subscription.Period * request.Periods;
        subscription.PaidUntil = baseDate.AddMonths(monthsToAdd);

        // Un pago reactiva automáticamente una suspensión manual previa.
        subscription.IsManuallySuspended = false;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.UpdatedBy = _currentUser.UserId;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            subscription.Notes = request.Notes.Trim();

        // Comisión mensual del comisionista: porcentaje sobre el pago de la empresa.
        if (subscription.SellerId.HasValue && subscription.CommissionMonthlyPercent > 0)
        {
            var sellerExists = await _context.SystemSellers
                .AnyAsync(s => s.Id == subscription.SellerId.Value, cancellationToken);

            if (sellerExists)
            {
                var baseAmount = request.Amount ?? subscription.Price * request.Periods;
                var commissionAmount = Math.Round(
                    baseAmount * subscription.CommissionMonthlyPercent / 100m, 2);

                _context.SellerCommissions.Add(new SellerCommission
                {
                    SystemSellerId = subscription.SellerId.Value,
                    TenantId = subscription.TenantId,
                    Type = CommissionType.Monthly,
                    BaseAmount = baseAmount,
                    Percent = subscription.CommissionMonthlyPercent,
                    Amount = commissionAmount,
                    PeriodDate = paymentDate,
                    Notes = $"Comisión mensual por pago de {request.Periods} periodo(s).",
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var status = subscription.EvaluateStatus(DateTime.UtcNow).ToString();

        return new RegisterCompanyPaymentResult(
            subscription.TenantId,
            subscription.PaidUntil,
            subscription.GraceEndsOn,
            status);
    }
}
