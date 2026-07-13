using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSalePayments;

/// <summary>
/// Query para obtener los pagos/abonos registrados en un Evento de Venta.
/// Opcionalmente filtra por estado (1=Preautorizado, 2=Aprobado, 3=Rechazado).
/// </summary>
public record GetBzaSalePaymentsQuery(int SaleId, int? Status = null) : IRequest<List<BzaSalePaymentItemDto>>;

/// <summary>
/// DTO de un pago/abono dentro de un Evento de Venta.
/// </summary>
public class BzaSalePaymentItemDto
{
    public int Id { get; set; }
    public int BzaSaleId { get; set; }
    public int BzaCustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? ProofImageUrl { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
