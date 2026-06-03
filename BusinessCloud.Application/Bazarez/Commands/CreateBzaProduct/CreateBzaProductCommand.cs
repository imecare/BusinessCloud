using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaProduct;

/// <summary>
/// Comando para registrar la compra de un cliente en un Evento de Venta específico.
/// </summary>
public record CreateBzaProductCommand : IRequest<int>
{
    /// <summary>
    /// FK al Evento de Venta donde se registra la compra.
    /// </summary>
    public int BzaSaleId { get; init; }

    /// <summary>
    /// FK al Cliente que realiza la compra.
    /// </summary>
    public int BzaCustomerId { get; init; }

    /// <summary>
    /// Descripción del producto comprado.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Precio de venta al cliente.
    /// </summary>
    public decimal Price { get; init; }
}
