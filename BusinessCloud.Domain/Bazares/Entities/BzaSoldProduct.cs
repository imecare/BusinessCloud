using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Representa un producto vendido a un cliente dentro de un Evento de Venta.
/// NO es un catálogo de productos, es un registro de venta individual.
/// Cada registro pertenece a UN cliente y se vincula a UN evento de venta (BzaSale).
/// </summary>
public class BzaSoldProduct : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Descripción del producto vendido (texto libre, no referencia a catálogo).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Precio de venta al cliente.
    /// </summary>
    public decimal Price { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Relaciones
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// FK al Evento de Venta (Corte/En Vivo/Catálogo) donde se registró esta venta.
    /// </summary>
    public int BzaSaleId { get; set; }
    public BzaSale Sale { get; set; } = null!;

    /// <summary>
    /// FK al Cliente al que se le vendió el producto.
    /// </summary>
    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;
}