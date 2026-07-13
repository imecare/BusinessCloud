using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.ValidateBzaCustomersImport;

/// <summary>
/// Valida (sin guardar) un archivo Excel de clientes. Analiza cada nombre
/// (existente / nuevo), detecta conflictos de teléfono y devuelve una vista
/// previa para que el usuario complete la información faltante antes de confirmar.
/// </summary>
public record ValidateBzaCustomersImportQuery(byte[] FileContent)
    : IRequest<ValidateBzaCustomersImportResult>;

public class ValidateBzaCustomersImportResult
{
    public bool HasRows { get; set; }
    public int TotalRows { get; set; }
    public List<ImportCustomerRowDto> Customers { get; set; } = [];
    public List<ImportCollectorDto> Collectors { get; set; } = [];
    public List<ImportCollectorGroupDto> CollectorGroups { get; set; } = [];

    /// <summary>Nombres de recolectores presentes en el archivo que NO existen en BD.
    /// Deben darse de alta (eligiendo grupo) antes de confirmar.</summary>
    public List<string> NewCollectors { get; set; } = [];

    public List<string> Errors { get; set; } = [];
}

/// <summary>Fila de cliente detectada en el archivo.</summary>
public class ImportCustomerRowDto
{
    public string Name { get; set; } = string.Empty;
    public string PhoneFromFile { get; set; } = string.Empty;
    public string CollectorNameFromFile { get; set; } = string.Empty;
    public string FacebookNameFromFile { get; set; } = string.Empty;

    /// <summary>Recolector sugerido (resuelto por nombre desde el archivo), si aplica.</summary>
    public int? SuggestedCollectorId { get; set; }

    /// <summary>El recolector del archivo existe en BD.</summary>
    public bool CollectorExists { get; set; }

    /// <summary>"existing" | "new".</summary>
    public string MatchStatus { get; set; } = "new";

    /// <summary>Cliente coincidente por nombre (solo existentes).</summary>
    public int? MatchedCustomerId { get; set; }

    /// <summary>El teléfono del archivo ya pertenece a OTRO cliente.</summary>
    public bool PhoneConflict { get; set; }

    /// <summary>Nombre del cliente dueño del teléfono en conflicto (si aplica).</summary>
    public string? PhoneConflictCustomerName { get; set; }
}

public class ImportCollectorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ImportCollectorGroupDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
}
