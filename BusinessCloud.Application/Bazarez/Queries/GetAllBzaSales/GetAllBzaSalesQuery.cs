using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetAllBzaSales;

/// <summary>
/// Query para listar Eventos de Venta con filtros opcionales por estado,
/// rango de fechas (sobre la fecha de creación del evento) y búsqueda por descripción.
/// </summary>
public record GetAllBzaSalesQuery(
    int? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null) : IRequest<List<BzaSaleListDto>>;

/// <summary>
/// DTO para listar Eventos de Venta (Cortes/En Vivos/Catálogos).
/// </summary>
public class BzaSaleListDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? PaymentDeadline { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// Total de ventas del evento (suma de todos los productos de todos los clientes).
    /// </summary>
    public decimal TotalEventSales { get; set; }

    /// <summary>
    /// Monto de ventas AÚN NO enviadas en un envío de totales (ventas sin cierre asignado).
    /// Si es 0, todas las ventas del evento ya están en proceso de pago y el evento
    /// no debe poder seleccionarse para un nuevo envío de totales.
    /// </summary>
    public decimal UnsentSalesAmount { get; set; }

    /// <summary>
    /// El evento ya tiene al menos una venta enviada a cobro (en proceso de pago).
    /// Cuando es true, NO se pueden agregar más ventas al evento (ni por el importador
    /// ni desde la página de Ventas).
    /// </summary>
    public bool HasSentSales { get; set; }

    /// <summary>
    /// Cantidad de clientes únicos con compras en este evento.
    /// </summary>
    public int UniqueCustomersCount { get; set; }

    /// <summary>
    /// Cantidad de clientes participando en el evento (alias de UniqueCustomersCount).
    /// </summary>
    public int TotalCustomers { get; set; }

    /// <summary>
    /// Monto total vendido en el evento (suma de productos).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Total pagado por los clientes (pagos verificados).
    /// </summary>
    public decimal TotalPaid { get; set; }

    /// <summary>
    /// Saldo pendiente del evento (TotalAmount - TotalPaid).
    /// </summary>
    public decimal TotalPending { get; set; }

    public DateTime CreatedAt { get; set; }
}
