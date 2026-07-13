using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.RejectClosureProof;

public class RejectClosureProofHandler(IBazaresDbContext context)
    : IRequestHandler<RejectClosureProofCommand, RejectClosureProofResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<RejectClosureProofResultDto> Handle(RejectClosureProofCommand request, CancellationToken cancellationToken)
    {
        var reason = (request.Reason ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Debes indicar el motivo del rechazo.");

        var total = await _context.ClosureCustomerTotals
            .Include(t => t.ClosureEvent)
            .Include(t => t.Customer)
            .Include(t => t.Proofs)
            .FirstOrDefaultAsync(t => t.Id == request.ClosureCustomerTotalId, cancellationToken)
            ?? throw new KeyNotFoundException("El total del cliente no existe.");

        if (total.Status == BzaClosureCustomerTotalStatus.Pending)
            throw new InvalidOperationException("El cliente aún no ha subido su comprobante.");

        if (total.Status == BzaClosureCustomerTotalStatus.Validated)
            throw new InvalidOperationException("El comprobante ya fue aprobado y no puede rechazarse.");

        // Anular los pagos preautorizados (sin verificar) generados por este comprobante:
        // así la venta no se cuenta como pagada mientras el cliente sube uno nuevo.
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

        // Conservar un registro histórico del rechazo (auditoría para reportes).
        // Se guarda un snapshot del cliente/evento y las URLs de los comprobantes
        // rechazados, aunque el cliente vuelva a subir otro comprobante después.
        var proofUrls = total.Proofs
            .OrderBy(p => p.UploadedAt)
            .Select(p => p.ImageUrl)
            .ToList();
        if (proofUrls.Count == 0 && !string.IsNullOrWhiteSpace(total.ProofImageUrl))
            proofUrls.Add(total.ProofImageUrl!);

        _context.ProofRejections.Add(new BzaProofRejection
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
            RejectedAt = DateTime.UtcNow,
            ProofUrls = proofUrls.Count > 0 ? string.Join('\n', proofUrls) : null
        });

        total.Status = BzaClosureCustomerTotalStatus.Rejected;
        total.RejectionReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        return new RejectClosureProofResultDto
        {
            ClosureEventId = total.BzaClosureEventId,
            ClosureCustomerTotalId = total.Id,
            TotalStatus = total.Status,
            ClosureStatus = total.ClosureEvent.Status,
            RejectionReason = reason
        };
    }
}
