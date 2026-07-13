using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetWhatsAppMessages;

/// <summary>
/// Estatus de entrega de los mensajes de WhatsApp enviados por el bazar (según los
/// webhooks de Meta). Base para el reporte de comprobantes.
/// </summary>
public record GetWhatsAppMessagesQuery(string? Purpose = null, int Take = 200) : IRequest<List<WhatsAppMessageDto>>;

public record WhatsAppMessageDto(
    int Id,
    string? WaMessageId,
    string ToPhone,
    string Purpose,
    int? BzaCustomerId,
    int? BzaClosureCustomerTotalId,
    string Status,
    int? ErrorCode,
    string? ErrorTitle,
    string? ErrorMessage,
    DateTime SentAt,
    DateTime? StatusUpdatedAt);

public class GetWhatsAppMessagesHandler(IBazaresDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetWhatsAppMessagesQuery, List<WhatsAppMessageDto>>
{
    public async Task<List<WhatsAppMessageDto>> Handle(GetWhatsAppMessagesQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.TenantId ?? string.Empty;

        // La entidad no tiene filtro global (los webhooks escriben sin contexto de tenant);
        // se filtra manualmente por el tenant del usuario actual.
        var query = context.WhatsAppMessages
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Purpose))
            query = query.Where(m => m.Purpose == request.Purpose);

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(request.Take <= 0 ? 200 : request.Take)
            .Select(m => new WhatsAppMessageDto(
                m.Id, m.WaMessageId, m.ToPhone, m.Purpose, m.BzaCustomerId, m.BzaClosureCustomerTotalId,
                m.Status, m.ErrorCode, m.ErrorTitle, m.ErrorMessage, m.SentAt, m.StatusUpdatedAt))
            .ToListAsync(ct);
    }
}
