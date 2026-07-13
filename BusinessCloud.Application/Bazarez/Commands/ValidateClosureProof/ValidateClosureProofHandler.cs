using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.ValidateClosureProof;

public class ValidateClosureProofHandler(IBazaresDbContext context)
    : IRequestHandler<ValidateClosureProofCommand, ValidateClosureProofResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<ValidateClosureProofResultDto> Handle(ValidateClosureProofCommand request, CancellationToken cancellationToken)
    {
        var total = await _context.ClosureCustomerTotals
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.Items)
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.CustomerTotals)
            .FirstOrDefaultAsync(t => t.Id == request.ClosureCustomerTotalId, cancellationToken)
            ?? throw new KeyNotFoundException("El total del cliente no existe.");

        if (total.Status == BzaClosureCustomerTotalStatus.Pending)
            throw new InvalidOperationException("El cliente aún no ha subido su comprobante.");

        // Idempotente: si ya está validado, devolver el estado actual.
        if (total.Status == BzaClosureCustomerTotalStatus.Validated)
        {
            return new ValidateClosureProofResultDto
            {
                ClosureEventId = total.BzaClosureEventId,
                ClosureCustomerTotalId = total.Id,
                TotalStatus = total.Status,
                ClosureStatus = total.ClosureEvent.Status
            };
        }

        if (total.Status == BzaClosureCustomerTotalStatus.Rejected)
            throw new InvalidOperationException("El comprobante fue rechazado. Espera a que el cliente suba uno nuevo.");

        var eventIds = total.ClosureEvent.Items.Select(i => i.BzaEventId).ToList();

        // Aprobar los pagos por comprobante (preautorizados) del cliente en esos eventos:
        // así la venta queda pagada para el cliente y todo lo que abarca el cierre.
        var payments = await _context.Payments
            .Where(p => p.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(p.BzaEventId)
                        && p.PaymentMethod == "Comprobante"
                        && !p.IsVerified)
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
        {
            payment.IsVerified = true;
            payment.PaymentStatus = 2; // Aprobado
            payment.VerifiedAt = DateTime.UtcNow;
            payment.VerificationNotes = "Comprobante validado desde el envío de totales.";
        }

        total.Status = BzaClosureCustomerTotalStatus.Validated;
        total.RejectionReason = null;

        // Si todos los totales del cierre quedaron validados, cerrar el evento de pago.
        var allValidated = total.ClosureEvent.CustomerTotals
            .All(t => t.Id == total.Id || t.Status == BzaClosureCustomerTotalStatus.Validated);

        if (allValidated)
        {
            total.ClosureEvent.Status = BzaClosureEventStatus.Validated;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ValidateClosureProofResultDto
        {
            ClosureEventId = total.BzaClosureEventId,
            ClosureCustomerTotalId = total.Id,
            TotalStatus = total.Status,
            ClosureStatus = total.ClosureEvent.Status
        };
    }
}
