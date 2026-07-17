using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.NotifyExpiringCompanies;

public class NotifyExpiringCompaniesHandler(
    IIdentityDbContext context,
    IWhatsAppSender whatsApp)
    : IRequestHandler<NotifyExpiringCompaniesCommand, NotifyExpiringCompaniesResult>
{
    private readonly IIdentityDbContext _context = context;
    private readonly IWhatsAppSender _whatsApp = whatsApp;

    public async Task<NotifyExpiringCompaniesResult> Handle(
        NotifyExpiringCompaniesCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Empresas con suscripción, teléfono del dueño y que no fueron avisadas hoy.
        var subscriptions = await _context.TenantSubscriptions
            .Where(s => s.OwnerPhone != null && s.OwnerPhone != ""
                && (s.LastExpirationNotifiedOn == null || s.LastExpirationNotifiedOn < today))
            .ToListAsync(cancellationToken);

        var tenantNames = await _context.Tenants
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var details = new List<NotifyDetail>();
        int notified = 0, failed = 0, skipped = 0;

        foreach (var sub in subscriptions)
        {
            var status = sub.EvaluateStatus(now, request.ExpiringSoonDays);

            // Solo se avisa cuando requiere atención (por vencer, en prórroga o suspendida).
            if (status == SubscriptionStatus.Active)
            {
                skipped++;
                continue;
            }

            var companyName = tenantNames.TryGetValue(sub.TenantId, out var name) ? name : sub.TenantId;
            var message = BuildMessage(companyName, status, sub, now);

            var sent = await _whatsApp.SendTextAsync(sub.OwnerPhone!, message, cancellationToken);

            if (sent)
            {
                sub.LastExpirationNotifiedOn = today;
                notified++;
                details.Add(new NotifyDetail(sub.TenantId, companyName, status.ToString(), true, null));
            }
            else
            {
                failed++;
                details.Add(new NotifyDetail(sub.TenantId, companyName, status.ToString(), false, "No se pudo enviar el WhatsApp."));
            }
        }

        if (notified > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return new NotifyExpiringCompaniesResult(notified, failed, skipped, details);
    }

    private static string BuildMessage(
        string companyName,
        SubscriptionStatus status,
        TenantSubscription sub,
        DateTime now)
    {
        var paidUntil = sub.PaidUntil.ToString("dd/MM/yyyy");
        var graceEnds = sub.GraceEndsOn.ToString("dd/MM/yyyy");

        return status switch
        {
            SubscriptionStatus.ExpiringSoon =>
                $"Hola {companyName}. Tu suscripción a BusinessCloud vence el {paidUntil} " +
                $"({sub.DaysUntilExpiration(now)} día(s)). Renueva a tiempo para no perder el servicio.",

            SubscriptionStatus.Grace =>
                $"Hola {companyName}. Tu suscripción venció el {paidUntil}. " +
                $"Tienes hasta el {graceEnds} para renovar antes de que se suspenda el servicio.",

            SubscriptionStatus.Suspended =>
                $"Hola {companyName}. Tu suscripción venció el {paidUntil} y el periodo de prórroga terminó. " +
                "El servicio ha sido suspendido. Realiza tu pago para reactivarlo.",

            _ => $"Hola {companyName}. Estado de tu suscripción a BusinessCloud actualizado.",
        };
    }
}
