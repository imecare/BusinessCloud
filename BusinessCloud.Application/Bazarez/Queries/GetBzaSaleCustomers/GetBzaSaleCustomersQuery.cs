using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleCustomers;

/// <summary>
/// Query para obtener los clientes que participan en un Evento de Venta,
/// con sus totales de compra, abonos aprobados y saldo pendiente.
/// </summary>
public record GetBzaSaleCustomersQuery(int SaleId) : IRequest<BzaSaleCustomersDto>;

/// <summary>
/// DTO con la lista de clientes de un Evento de Venta y su resumen de cobranza.
/// </summary>
public class BzaSaleCustomersDto
{
    public int SaleId { get; set; }
    public string SaleDescription { get; set; } = string.Empty;
    public List<BzaSaleCustomerItemDto> Customers { get; set; } = [];
    public int TotalCustomers { get; set; }
    public int FullyPaidCount { get; set; }
    public int PendingCount { get; set; }
}

/// <summary>
/// DTO de un cliente participante en el Evento de Venta.
/// </summary>
public class BzaSaleCustomerItemDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? FacebookName { get; set; }

    /// <summary>Total comprado por el cliente en este evento (suma de precios de productos).</summary>
    public decimal TotalPurchases { get; set; }

    /// <summary>Total de abonos aprobados del cliente en este evento.</summary>
    public decimal TotalPaid { get; set; }

    /// <summary>Saldo pendiente (TotalPurchases - TotalPaid).</summary>
    public decimal Balance { get; set; }

    /// <summary>Cantidad de productos comprados por el cliente en este evento.</summary>
    public int ProductCount { get; set; }

    /// <summary>true si el cliente saldó por completo su compra en este evento.</summary>
    public bool IsFullyPaid { get; set; }

    public int? CollectorGroupId { get; set; }
    public string? CollectorGroupName { get; set; }
}
