using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.MarkClosureMessageManualSent;

public class MarkClosureMessageManualSentHandler(IBazaresDbContext context)
    : IRequestHandler<MarkClosureMessageManualSentCommand, MarkClosureMessageManualSentResultDto>
{
    public async Task<MarkClosureMessageManualSentResultDto> Handle(
        MarkClosureMessageManualSentCommand request,
        CancellationToken cancellationToken)
    {
        var totalExists = await context.ClosureCustomerTotals
            .AnyAsync(t => t.Id == request.ClosureCustomerTotalId, cancellationToken);

        if (!totalExists)
            throw new KeyNotFoundException("El total del cliente no existe.");

        var message = await context.WhatsAppMessages
            .Where(m => m.Purpose == "totals"
                && m.BzaClosureCustomerTotalId == request.ClosureCustomerTotalId)
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe un registro de mensaje manual para este cliente.");

        if (string.Equals(message.Status, "manual_sent", StringComparison.OrdinalIgnoreCase))
        {
            var markedAt = message.StatusUpdatedAt ?? message.SentAt;
            return new(request.ClosureCustomerTotalId, "manual_sent", markedAt);
        }

        if (!string.Equals(message.Status, "sin_whatsapp", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo los clientes reportados sin WhatsApp pueden marcarse como enviados manualmente.");

        var now = DateTime.UtcNow;
        message.Status = "manual_sent";
        message.StatusUpdatedAt = now;
        message.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);

        return new(request.ClosureCustomerTotalId, "manual_sent", now);
    }
}
