using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetAllBzaSales;

public record GetAllBzaSalesQuery : IRequest<List<BzaSaleListDto>>;

/// <summary>
/// DTO para listar Eventos de Venta (Cortes/En Vivos/Catálogos).
/// </summary>
public class BzaSaleListDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// Total de ventas del evento (suma de todos los productos de todos los clientes).
    /// </summary>
    public decimal TotalEventSales { get; set; }

    /// <summary>
    /// Cantidad de clientes únicos con compras en este evento.
    /// </summary>
    public int UniqueCustomersCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
