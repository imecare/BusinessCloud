using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Registro de la lista de bloqueo de clientes ("clientes vetados").
/// Permite marcar a un cliente (por nombre y/o nombre de Facebook, con un motivo) para
/// que al intentar dar de alta a otro cliente con el mismo nombre o Facebook el sistema
/// alerte al bazar. El teléfono es único, por lo que no se usa como criterio de coincidencia.
/// </summary>
public class BzaBlockedCustomer : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Nombre del cliente bloqueado.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Nombre de Facebook del cliente bloqueado (opcional).</summary>
    public string? FacebookName { get; set; }

    /// <summary>Teléfono de referencia del cliente bloqueado (opcional, solo informativo).</summary>
    public string? Phone { get; set; }

    /// <summary>Motivo / descripción del bloqueo (obligatorio).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>FK opcional al cliente existente que originó el bloqueo.</summary>
    public int? BzaCustomerId { get; set; }

    /// <summary>Indica si el bloqueo está activo.</summary>
    public bool IsActive { get; set; } = true;
}
