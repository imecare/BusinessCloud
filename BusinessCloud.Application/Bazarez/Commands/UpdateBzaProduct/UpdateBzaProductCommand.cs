using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaProduct;

/// <summary>
/// Comando para modificar datos de una compra/producto.
/// </summary>
public record UpdateBzaProductCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
