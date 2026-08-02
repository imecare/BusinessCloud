using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaImport;

/// <summary>
/// Confirma y guarda en BD la importación de compras previamente validada.
/// Crea clientes nuevos cuando corresponde y registra las ventas marcadas
/// con Source = Excel. No vuelve a leer el archivo: usa los datos resueltos
/// por el usuario en la pantalla de validación.
/// </summary>
public record CommitBzaImportCommand(
    int EventId,
    bool ConfirmDuplicate,
    List<CommitNewCollectorDto> NewCollectors,
    List<CommitImportCustomerDto> Customers) : IRequest<CommitBzaImportResult>;

/// <summary>Recolector nuevo a dar de alta (solo los que no existen en BD), con su grupo.</summary>
public class CommitNewCollectorDto
{
    public string Name { get; set; } = string.Empty;
    public int GroupId { get; set; }
}

public class CommitImportCustomerDto
{
    /// <summary>Cliente existente elegido (existing / ambiguous resuelto).</summary>
    public int? CustomerId { get; set; }

    /// <summary>Datos del cliente nuevo a crear (cuando no existe).</summary>
    public CommitImportNewCustomerDto? NewCustomer { get; set; }

    /// <summary>Para clientes existentes: si se confirma, cambia su recolector al indicado (por nombre).</summary>
    public string? ChangeCollectorToName { get; set; }

    /// <summary>Para clientes existentes: si se confirma, cambia su nombre de Facebook al indicado.</summary>
    public string? ChangeFacebookNameTo { get; set; }

    /// <summary>Para clientes existentes: si se confirma, cambia su teléfono al indicado.</summary>
    public string? ChangePhoneTo { get; set; }

    public List<CommitImportProductDto> Products { get; set; } = [];
}

public class CommitImportNewCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool HasNoWhatsApp { get; set; }

    /// <summary>Recolector del cliente, por nombre (existente o uno de los nuevos a crear).</summary>
    public string CollectorName { get; set; } = string.Empty;

    public string? FacebookName { get; set; }
}

public class CommitImportProductDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class CommitBzaImportResult
{
    public int ImportedProducts { get; set; }
    public int NewCustomersCreated { get; set; }
    public int NewCollectorsCreated { get; set; }
    public int CollectorsChanged { get; set; }
    public int CustomersUpdated { get; set; }
    public int SalesCreated { get; set; }

    /// <summary>Registros de cliente omitidos (p. ej. teléfono duplicado). Ver <see cref="Errors"/> para el detalle.</summary>
    public int IgnoredRecords { get; set; }

    /// <summary>
    /// Registros de cliente que no se pudieron guardar (p. ej. telefono duplicado) pero cuyos
    /// datos se conservan para que el usuario los corrija (telefono/nombre) y reintente sin
    /// tener que volver a subir el archivo.
    /// </summary>
    public List<CommitImportFailedRecord> FailedRecords { get; set; } = [];

    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Datos completos de un cliente nuevo cuyo alta fallo, con el motivo del conflicto,
/// para permitir corregirlo (telefono/nombre) y reintentar la importacion del registro.
/// </summary>
public class CommitImportFailedRecord
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool HasNoWhatsApp { get; set; }
    public string CollectorName { get; set; } = string.Empty;
    public string? FacebookName { get; set; }
    public List<CommitImportProductDto> Products { get; set; } = [];

    /// <summary>Tipo de conflicto que impidio guardar el registro (p. ej. "PhoneDuplicate").</summary>
    public string ConflictType { get; set; } = string.Empty;

    /// <summary>Mensaje legible que explica el motivo y como corregirlo.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Cliente existente con el que se produjo el conflicto (cuando aplica).</summary>
    public int? ConflictCustomerId { get; set; }
    public string? ConflictCustomerName { get; set; }
}
