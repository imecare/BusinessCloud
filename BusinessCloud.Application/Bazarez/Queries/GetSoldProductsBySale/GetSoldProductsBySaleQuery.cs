using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetSoldProductsBySale;

/// <summary>
/// Query para obtener todos los productos vendidos de un Evento de Venta.
/// Opcionalmente filtra por customerId.
/// </summary>
public record GetSoldProductsBySaleQuery(int BzaSaleId, int? CustomerId = null) : IRequest<SoldProductsBySaleDto>;

/// <summary>
/// DTO con los productos vendidos de un Evento de Venta.
/// </summary>
public class SoldProductsBySaleDto
{
    public int BzaSaleId { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public List<SoldProductItemDto> Items { get; set; } = [];
}

/// <summary>
/// DTO de un producto vendido individual.
/// </summary>
public class SoldProductItemDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
