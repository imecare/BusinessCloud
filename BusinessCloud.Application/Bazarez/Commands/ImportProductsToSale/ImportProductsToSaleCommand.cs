using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.ImportProductsToSale;

/// <summary>
/// Comando para importar compras masivas desde Excel para un Evento de Venta específico.
/// Lee filas con [Cliente, Teléfono, Producto, Precio, Costo].
/// Crea los clientes si no existen y les asigna los productos bajo este Evento ID.
/// </summary>
public record ImportProductsToSaleCommand(int BzaSaleId, byte[] FileContent) : IRequest<ImportProductsResult>;

/// <summary>
/// Resultado de la importación masiva de productos.
/// </summary>
public class ImportProductsResult
{
    public int ImportedProducts { get; set; }
    public int NewCustomersCreated { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = [];
}
