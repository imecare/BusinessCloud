using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteClosureProof;

public class DeleteClosureProofHandler(IBazaresDbContext context)
    : IRequestHandler<DeleteClosureProofCommand, DeleteClosureProofResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<DeleteClosureProofResultDto> Handle(DeleteClosureProofCommand request, CancellationToken cancellationToken)
    {
        // Acceso público por token: ignorar filtros de tenant y resolver manualmente.
        var total = await _context.ClosureCustomerTotals
            .IgnoreQueryFilters()
            .Include(t => t.Proofs)
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(t => t.UploadToken == request.UploadToken, cancellationToken)
            ?? throw new KeyNotFoundException("El enlace no es válido o ha expirado.");

        // Un comprobante ya aprobado no puede eliminarse.
        if (total.Status == BzaClosureCustomerTotalStatus.Validated)
            throw new InvalidOperationException("El comprobante ya fue aprobado y no puede modificarse.");

        var proof = total.Proofs.FirstOrDefault(p => p.Id == request.ProofId)
            ?? throw new KeyNotFoundException("El comprobante no existe.");

        _context.ClosureProofs.Remove(proof);
        total.Proofs.Remove(proof);

        if (total.Proofs.Count > 0)
        {
            // Aún quedan comprobantes: mantener "en espera" y apuntar al más reciente.
            var last = total.Proofs.OrderBy(p => p.UploadedAt).Last();
            total.ProofImageUrl = last.ImageUrl;
            total.ProofUploadedAt = last.UploadedAt;
        }
        else
        {
            // Sin comprobantes: volver a "pendiente" y limpiar los pagos preautorizados.
            total.ProofImageUrl = null;
            total.ProofUploadedAt = null;
            total.Status = BzaClosureCustomerTotalStatus.Pending;

            var tenantId = total.TenantId;
            var eventIds = total.ClosureEvent.Items.Select(i => i.BzaEventId).ToList();

            var stalePreauth = await _context.Payments
                .IgnoreQueryFilters()
                .Where(p => p.TenantId == tenantId
                            && p.BzaCustomerId == total.BzaCustomerId
                            && eventIds.Contains(p.BzaEventId)
                            && p.PaymentMethod == "Comprobante"
                            && !p.IsVerified)
                .ToListAsync(cancellationToken);

            if (stalePreauth.Count > 0)
                _context.Payments.RemoveRange(stalePreauth);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new DeleteClosureProofResultDto
        {
            Success = true,
            RemainingProofs = total.Proofs.Count,
            Status = total.Status
        };
    }
}
