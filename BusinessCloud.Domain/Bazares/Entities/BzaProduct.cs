using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Representa una compra/producto adquirido por un cliente dentro de un Evento de Venta.
/// Cada producto pertenece a UN cliente y se vincula a UN evento de venta (BzaSale).
/// </summary>
public class BzaProduct : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Descripción del producto comprado.
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
    /// FK al Evento de Venta (Corte/En Vivo/Catálogo) donde se registró esta compra.
    /// </summary>
    public int BzaSaleId { get; set; }
    public BzaSale Sale { get; set; } = null!;

    /// <summary>
    /// FK al Cliente que realizó esta compra.
    /// </summary>
    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;
}