using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Tarjeta activa para incluir en los mensajes de cobro a los clientes.
/// Almacena el número de tarjeta y el nombre del titular.
/// </summary>
public class BzaPaymentCard : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Número de la tarjeta (o CLABE / cuenta) a mostrar al cliente.</summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>Nombre del titular a nombre de quien está la tarjeta.</summary>
    public string CardHolderName { get; set; } = string.Empty;

    /// <summary>Banco emisor (opcional).</summary>
    public string? Bank { get; set; }

    /// <summary>Especificación o nota del titular (ej: "solo para transferencias", "solo depósitos en efectivo").</summary>
    public string? Notes { get; set; }

    /// <summary>Indica si la tarjeta está activa para enviarse en los mensajes.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indica que la tarjeta ya fue enviada al menos una vez a los clientes en un envío
    /// de totales (cobro). Una tarjeta enviada NO se puede eliminar ni modificar: solo
    /// activarse o desactivarse, para preservar la trazabilidad de los datos de cobro.
    /// </summary>
    public bool WasSentForPayment { get; set; }
}
