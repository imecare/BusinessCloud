using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSale;

/// <summary>
/// Comando para actualizar datos de un Evento de Venta.
/// </summary>
public record UpdateBzaSaleCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime? PaymentDeadline { get; init; }
    public int Status { get; init; }
}
