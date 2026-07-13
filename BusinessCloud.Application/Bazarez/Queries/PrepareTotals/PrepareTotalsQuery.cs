using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.PrepareTotals;

/// <summary>
/// Prepara el envío de totales: a partir de los eventos seleccionados calcula los
/// grupos de recolección participantes (con fecha de entrega sugerida según su
/// día configurado), los clientes y los montos pendientes.
/// </summary>
public record PrepareTotalsQuery(List<int> EventIds) : IRequest<PrepareTotalsResultDto>;

public class PrepareTotalsResultDto
{
    public List<TotalsEventDto> Events { get; set; } = new();
    public List<TotalsGroupDto> Groups { get; set; } = new();
    public int CustomerCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime SuggestedPaymentDeadline { get; set; }
    /// <summary>Hora límite de pago por defecto (HH:mm) configurada por el bazar.</summary>
    public string? PaymentCutoffTime { get; set; }
}

public record TotalsEventDto(int EventId, string Description, decimal Pending, int CustomerCount);

public record TotalsGroupDto(
    int GroupId,
    string GroupName,
    int? DeliveryDay,
    DateTime SuggestedDeliveryDate,
    int CustomerCount,
    decimal Pending);
