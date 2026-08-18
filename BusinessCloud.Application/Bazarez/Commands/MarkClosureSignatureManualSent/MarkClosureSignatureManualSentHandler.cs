using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.MarkClosureSignatureManualSent;

public class MarkClosureSignatureManualSentHandler(IBazaresDbContext context)
    : IRequestHandler<MarkClosureSignatureManualSentCommand, MarkClosureSignatureManualSentResultDto>
{
    public async Task<MarkClosureSignatureManualSentResultDto> Handle(
        MarkClosureSignatureManualSentCommand request,
        CancellationToken cancellationToken)
    {
        var total = await context.ClosureCustomerTotals
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.Id == request.ClosureCustomerTotalId, cancellationToken)
            ?? throw new KeyNotFoundException("El total del cliente no existe.");

        var existing = await context.WhatsAppMessages
            .Where(message => message.Purpose == "signatures"
                && message.BzaClosureCustomerTotalId == request.ClosureCustomerTotalId)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!request.Sent)
        {
            if (existing is not null)
            {
                context.WhatsAppMessages.Remove(existing);
                await context.SaveChangesAsync(cancellationToken);
            }
            return new(request.ClosureCustomerTotalId, false, null);
        }

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            existing = new BzaWhatsAppMessage
            {
                TenantId = total.TenantId,
                ToPhone = total.Customer?.Phone ?? string.Empty,
                Purpose = "signatures",
                BzaCustomerId = total.BzaCustomerId,
                BzaClosureCustomerTotalId = total.Id,
                Status = "manual_sent",
                SentAt = now,
                StatusUpdatedAt = now,
            };
            context.WhatsAppMessages.Add(existing);
        }
        else
        {
            existing.Status = "manual_sent";
            existing.StatusUpdatedAt = now;
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new(request.ClosureCustomerTotalId, true, now);
    }
}
