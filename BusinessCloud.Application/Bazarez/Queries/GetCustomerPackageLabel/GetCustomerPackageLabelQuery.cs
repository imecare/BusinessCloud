using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerPackageLabel;

/// <summary>
/// Query para obtener datos de la etiqueta de paquete de un cliente para un Evento de Venta pagado.
/// Para impresión con QR.
/// </summary>
public record GetCustomerPackageLabelQuery(int CustomerId, int SaleId) : IRequest<CustomerPackageLabelDto>;

/// <summary>
/// DTO con datos para la etiqueta de paquete del cliente.
/// </summary>
public class CustomerPackageLabelDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public int SaleEventId { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }
    public string CollectorName { get; set; } = string.Empty;
    public string CollectorGroupName { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public string LabelCode { get; set; } = string.Empty;
    public string QrData { get; set; } = string.Empty;
}
