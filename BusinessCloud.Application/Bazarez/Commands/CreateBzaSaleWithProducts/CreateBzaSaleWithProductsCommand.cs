using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSaleWithProducts;

/// <summary>
/// Comando para registrar una Venta (par Cliente + Evento) con uno o varios productos
/// en una sola operación. Si ya existe una venta para ese par cliente-evento, se le
/// agregan los productos a la venta existente.
/// </summary>
public record CreateBzaSaleWithProductsCommand : IRequest<CreateBzaSaleWithProductsResult>
{
    /// <summary>
    /// FK al Evento de Venta.
    /// </summary>
    public int BzaEventId { get; init; }

    /// <summary>
    /// FK al Cliente dueño de la venta.
    /// </summary>
    public int BzaCustomerId { get; init; }

    /// <summary>
    /// Productos a registrar en la venta.
    /// </summary>
    public List<CreateBzaSaleProductItem> Products { get; init; } = [];
}

/// <summary>
/// Producto individual a registrar dentro de la venta.
/// </summary>
public record CreateBzaSaleProductItem
{
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

/// <summary>
/// Resultado de registrar una venta con productos. El total se calcula sobre todos
/// los productos de la venta (incluyendo los previamente existentes) y NO se persiste.
/// </summary>
public record CreateBzaSaleWithProductsResult
{
    public int SaleId { get; init; }
    public int ProductsAdded { get; init; }
    public decimal Total { get; init; }
}
