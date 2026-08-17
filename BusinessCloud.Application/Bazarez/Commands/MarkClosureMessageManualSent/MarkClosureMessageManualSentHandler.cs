using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.MarkClosureMessageManualSent;

public class MarkClosureMessageManualSentHandler(IBazaresDbContext context)
    : IRequestHandler<MarkClosureMessageManualSentCommand, MarkClosureMessageManualSentResultDto>
{
    /// <summary>
    /// Ventana tras la cual un mensaje aceptado por Meta sin acuse de entrega/lectura se considera
    /// "sin confirmación de Meta" y puede marcarse como enviado manualmente (el bazar decide).
    /// </summary>
    private static readonly TimeSpan DeliveryConfirmationTimeout = TimeSpan.FromMinutes(15);

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

        var now = DateTime.UtcNow;

        var status = message.Status?.ToLowerInvariant();
        var isSinWhatsApp = status == "sin_whatsapp";
        var isFailed = status == "failed";
        // "Sin confirmación de Meta": Meta aceptó el mensaje (sent/accepted) pero pasaron >15 min
        // sin acuse de entrega/lectura. El bazar puede decidir enviarlo manualmente y marcarlo.
        var isUnconfirmed = (status == "sent" || status == "accepted")
            && now - message.SentAt >= DeliveryConfirmationTimeout;

        if (!isSinWhatsApp && !isFailed && !isUnconfirmed)
            throw new InvalidOperationException(
                "Solo los clientes sin WhatsApp, con envío fallido o sin confirmación de Meta pueden marcarse como enviados manualmente.");

        message.Status = "manual_sent";
        message.StatusUpdatedAt = now;
        message.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);

        return new(request.ClosureCustomerTotalId, "manual_sent", now);
    }
}
