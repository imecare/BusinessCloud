using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSoldProduct;

/// <summary>
/// Comando para modificar datos de un producto vendido.
/// </summary>
public record UpdateBzaSoldProductCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
