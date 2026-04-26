using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSaleStatus;

public record UpdateBzaSaleStatusCommand : IRequest
{
    public int SaleId { get; init; }
    public int NewStatus { get; init; } // Ej: 2 para "Pagado"
    public string? Note { get; init; }  // "Pago recibido en efectivo", etc.
}