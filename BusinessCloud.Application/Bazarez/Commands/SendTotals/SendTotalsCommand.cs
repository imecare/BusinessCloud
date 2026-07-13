using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.SendTotals;

/// <summary>
/// Fecha de entrega asignada a un grupo de recolección dentro del cierre.
/// </summary>
public record GroupDeliveryInput(int GroupId, DateTime DeliveryDate);

/// <summary>
/// Envía los totales: cierra los eventos seleccionados, crea el Evento de Cierre de Venta,
/// fija la fecha límite de pago, registra las fechas de entrega por grupo y genera los
/// totales por cliente con su token público para subir el comprobante.
/// </summary>
public record SendTotalsCommand(
    List<int> EventIds,
    DateTime PaymentDeadline,
    DateTime? OfficialDeliveryDate,
    List<GroupDeliveryInput> GroupDeliveries) : IRequest<SendTotalsResultDto>;

public class SendTotalsResultDto
{
    public int ClosureEventId { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<CustomerTotalMessageDto> Messages { get; set; } = new();
}

public class CustomerTotalMessageDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string UploadToken { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Mensaje de WhatsApp listo. Contiene el marcador __UPLOAD_LINK__ que el frontend
    /// reemplaza por la URL pública del portal de comprobantes.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
