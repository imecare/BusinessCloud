using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CancelPendingSales;

public class CancelPendingSalesHandler(IBazaresDbContext context)
    : IRequestHandler<CancelPendingSalesCommand, CancelPendingSalesResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<CancelPendingSalesResultDto> Handle(CancelPendingSalesCommand request, CancellationToken cancellationToken)
    {
        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "No se subió el comprobante antes de marcar el evento en proceso de entrega."
            : request.Reason!.Trim();

        var totals = await _context.ClosureCustomerTotals
            .Include(t => t.ClosureEvent)
            .Include(t => t.Customer)
            .Include(t => t.Proofs)
            .Where(t => t.BzaClosureEventId == request.ClosureEventId
                        && t.Status == BzaClosureCustomerTotalStatus.Pending)
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
            return new CancelPendingSalesResultDto { ClosureEventId = request.ClosureEventId, CancelledCount = 0 };

        var eventIds = await _context.ClosureEventItems
            .Where(i => i.BzaClosureEventId == request.ClosureEventId)
            .Select(i => i.BzaEventId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var total in totals)
        {
            var customerIds = new[] { total.BzaCustomerId };
            var preauth = await _context.Payments
                .Where(p => p.BzaCustomerId == total.BzaCustomerId
                            && eventIds.Contains(p.BzaEventId)
                            && p.PaymentMethod == "Comprobante"
                            && !p.IsVerified)
                .ToListAsync(cancellationToken);

            if (preauth.Count > 0)
                _context.Payments.RemoveRange(preauth);

            var proofUrls = total.Proofs
                .OrderBy(p => p.UploadedAt)
                .Select(p => p.ImageUrl)
                .ToList();
            if (proofUrls.Count == 0 && !string.IsNullOrWhiteSpace(total.ProofImageUrl))
                proofUrls.Add(total.ProofImageUrl!);

            _context.SaleCancellations.Add(new BzaSaleCancellation
            {
                TenantId = total.TenantId,
                BzaClosureCustomerTotalId = total.Id,
                BzaClosureEventId = total.BzaClosureEventId,
                BzaCustomerId = total.BzaCustomerId,
                CustomerName = total.Customer?.Name ?? "Cliente",
                CustomerPhone = total.Customer?.Phone,
                EventDescription = total.ClosureEvent.Description,
                TotalAmount = total.TotalAmount,
                Reason = reason,
                IsCustomerFault = true,
                CancelledAt = now,
                ProofUrls = proofUrls.Count > 0 ? string.Join('\n', proofUrls) : null
            });

            total.Status = BzaClosureCustomerTotalStatus.Cancelled;
            total.CancellationReason = reason;
            total.CancelledIsCustomerFault = true;
            total.CancelledAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CancelPendingSalesResultDto
        {
            ClosureEventId = request.ClosureEventId,
            CancelledCount = totals.Count
        };
    }
}