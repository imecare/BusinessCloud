using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Representa un producto individual perteneciente a una Venta (BzaSale).
/// NO es un catálogo de productos, es un renglón de la venta.
/// El cliente y el evento se obtienen a través de la Venta a la que pertenece.
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
    /// FK a la Venta (Cliente + Evento) a la que pertenece este producto.
    /// </summary>
    public int BzaSaleId { get; set; }
    public BzaSale Sale { get; set; } = null!;
}