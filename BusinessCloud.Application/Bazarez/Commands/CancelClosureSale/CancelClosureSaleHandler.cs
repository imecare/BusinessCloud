using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CancelClosureSale;

public class CancelClosureSaleHandler(IBazaresDbContext context)
    : IRequestHandler<CancelClosureSaleCommand, CancelClosureSaleResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<CancelClosureSaleResultDto> Handle(CancelClosureSaleCommand request, CancellationToken cancellationToken)
    {
        var reason = (request.Reason ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Debes indicar el motivo de la cancelación.");

        var total = await _context.ClosureCustomerTotals
            .Include(t => t.ClosureEvent)
            .Include(t => t.Customer)
            .Include(t => t.Proofs)
            .FirstOrDefaultAsync(t => t.Id == request.ClosureCustomerTotalId, cancellationToken)
            ?? throw new KeyNotFoundException("El total del cliente no existe.");

        if (total.Status == BzaClosureCustomerTotalStatus.Validated)
            throw new InvalidOperationException("La venta ya fue validada como pagada y no puede cancelarse aquí.");

        if (total.Status == BzaClosureCustomerTotalStatus.Cancelled)
            throw new InvalidOperationException("La venta ya está cancelada.");

        // Anular los pagos preautorizados (sin verificar) generados por comprobantes.
        var eventIds = await _context.ClosureEventItems
            .Where(i => i.BzaClosureEventId == total.BzaClosureEventId)
            .Select(i => i.BzaEventId)
            .ToListAsync(cancellationToken);

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

        var now = DateTime.UtcNow;

        // Registro histórico de la cancelación (auditoría para reportes).
        _context.SaleCancellations.Add(new BzaSaleCancellation
        {
            TenantId = total.TenantId, // Pre-asignado: el override de SaveChanges no lo sobrescribe.
            BzaClosureCustomerTotalId = total.Id,
            BzaClosureEventId = total.BzaClosureEventId,
            BzaCustomerId = total.BzaCustomerId,
            CustomerName = total.Customer?.Name ?? "Cliente",
            CustomerPhone = total.Customer?.Phone,
            EventDescription = total.ClosureEvent.Description,
            TotalAmount = total.TotalAmount,
            Reason = reason,
            IsCustomerFault = request.IsCustomerFault,
            CancelledAt = now,
            ProofUrls = proofUrls.Count > 0 ? string.Join('\n', proofUrls) : null
        });

        total.Status = BzaClosureCustomerTotalStatus.Cancelled;
        total.CancellationReason = reason;
        total.CancelledIsCustomerFault = request.IsCustomerFault;
        total.CancelledAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return new CancelClosureSaleResultDto
        {
            ClosureEventId = total.BzaClosureEventId,
            ClosureCustomerTotalId = total.Id,
            TotalStatus = total.Status,
            ClosureStatus = total.ClosureEvent.Status,
            Reason = reason,
            IsCustomerFault = request.IsCustomerFault
        };
    }
}
