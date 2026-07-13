using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaCustomersImport;

/// <summary>
/// Confirma y guarda en BD la importación de clientes previamente validada.
/// Crea recolectores nuevos (con su grupo) y clientes nuevos. Omite los clientes
/// ya existentes por nombre y los que tengan un teléfono duplicado, informándolos.
/// </summary>
public record CommitBzaCustomersImportCommand(
    List<CommitNewCollectorDto> NewCollectors,
    List<CommitImportCustomerDto> Customers) : IRequest<CommitBzaCustomersImportResult>;

/// <summary>Recolector nuevo a dar de alta (solo los que no existen en BD), con su grupo.</summary>
public class CommitNewCollectorDto
{
    public string Name { get; set; } = string.Empty;
    public int GroupId { get; set; }
}

/// <summary>Cliente resuelto por el usuario a crear.</summary>
public class CommitImportCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>Recolector por nombre (existente o uno de los nuevos a crear).</summary>
    public string CollectorName { get; set; } = string.Empty;

    public string? FacebookName { get; set; }
}

public class CommitBzaCustomersImportResult
{
    public int CustomersCreated { get; set; }
    public int NewCollectorsCreated { get; set; }

    /// <summary>Clientes omitidos (ya existían o teléfono duplicado). Ver <see cref="Errors"/>.</summary>
    public int IgnoredRecords { get; set; }

    public List<string> Errors { get; set; } = [];
}
