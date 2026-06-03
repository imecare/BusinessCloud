using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSoldProduct;

/// <summary>
/// Comando para registrar un producto vendido a un cliente en un Evento de Venta específico.
/// </summary>
public record CreateBzaSoldProductCommand : IRequest<int>
{
    /// <summary>
    /// FK al Evento de Venta donde se registra la venta.
    /// </summary>
    public int BzaSaleId { get; init; }

    /// <summary>
    /// FK al Cliente al que se le vendió el producto.
    /// </summary>
    public int BzaCustomerId { get; init; }

    /// <summary>
    /// Descripción del producto vendido (texto libre, no catálogo).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Precio de venta al cliente.
    /// </summary>
    public decimal Price { get; init; }
}
