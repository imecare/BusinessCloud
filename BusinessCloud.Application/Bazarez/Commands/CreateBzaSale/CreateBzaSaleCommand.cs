using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSale;

/// <summary>
/// Comando para crear un nuevo Evento de Venta (Corte/Catálogo/En Vivo).
/// El evento agrupa productos comprados por múltiples clientes.
/// </summary>
public record CreateBzaSaleCommand : IRequest<int>
{
    /// <summary>
    /// Descripción del evento (ej: "En vivo 5 de Junio", "Catálogo Primavera 2026").
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Fecha límite de pago para los clientes que participan en este evento.
    /// </summary>
    public DateTime? PaymentDeadline { get; init; }
}