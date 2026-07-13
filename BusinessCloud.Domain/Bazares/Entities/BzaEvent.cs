using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Representa un Evento de Venta (Corte / En Vivo / Catálogo).
/// NO pertenece a un cliente único; agrupa productos comprados por múltiples clientes.
/// </summary>
public class BzaEvent : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Descripción del evento (ej: "En vivo 5 de Junio", "Catálogo Primavera 2026").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Fecha límite de pago para los clientes que participan en este evento.
    /// </summary>
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>
    /// Estado del evento de venta:
    /// 1=Abierto (Activo), 2=Cerrado (No acepta más compras), 3=EnEntrega, 4=Finalizado, 5=Cancelado
    /// </summary>
    public int Status { get; set; } = 1;

    // ─────────────────────────────────────────────────────────────────────────
    // Navegación
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Ventas registradas en este evento (una por cada cliente participante).
    /// Cada venta agrupa los productos comprados por ese cliente.
    /// </summary>
    public ICollection<BzaSale> Sales { get; set; } = [];

    /// <summary>
    /// Pagos recibidos de clientes para este evento de venta.
    /// </summary>
    public ICollection<BzaPayment> Payments { get; set; } = [];
}