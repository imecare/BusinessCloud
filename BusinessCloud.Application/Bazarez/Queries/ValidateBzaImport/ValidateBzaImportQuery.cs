using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.ValidateBzaImport;

/// <summary>
/// Valida (sin guardar) un archivo Excel de compras para un Evento de Venta.
/// Analiza los clientes (existentes / ambiguos / nuevos) y detecta posibles
/// duplicados comparando los primeros productos contra eventos ABIERTOS.
/// </summary>
public record ValidateBzaImportQuery(int EventId, byte[] FileContent)
    : IRequest<ValidateBzaImportResult>;

/// <summary>Resultado del análisis previo del archivo.</summary>
public class ValidateBzaImportResult
{
    public bool HasRows { get; set; }
    public int TotalProducts { get; set; }
    public List<ImportCustomerGroupDto> Customers { get; set; } = [];
    public List<ImportCollectorDto> Collectors { get; set; } = [];
    public List<ImportCollectorGroupDto> CollectorGroups { get; set; } = [];

    /// <summary>Nombres de recolectores presentes en el archivo que NO existen en BD.
    /// Deben darse de alta (eligiendo grupo) antes de confirmar.</summary>
    public List<string> NewCollectors { get; set; } = [];

    public ImportDuplicateWarningDto? DuplicateWarning { get; set; }
    public List<string> Errors { get; set; } = [];
}

/// <summary>Grupo de productos de un cliente detectado en el archivo.</summary>
public class ImportCustomerGroupDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CollectorNameFromFile { get; set; } = string.Empty;

    /// <summary>Nombre de Facebook capturado en el archivo (opcional).</summary>
    public string FacebookNameFromFile { get; set; } = string.Empty;

    /// <summary>Teléfono capturado en el archivo (opcional).</summary>
    public string PhoneFromFile { get; set; } = string.Empty;

    /// <summary>Recolector sugerido (resuelto por nombre desde el archivo), si aplica.</summary>
    public int? SuggestedCollectorId { get; set; }

    /// <summary>El recolector del archivo existe en BD.</summary>
    public bool CollectorExists { get; set; }

    /// <summary>Para clientes existentes: el recolector del archivo difiere del registrado.</summary>
    public bool CollectorChanged { get; set; }

    /// <summary>Para clientes existentes: el nombre de Facebook del archivo difiere del registrado.</summary>
    public bool FacebookChanged { get; set; }

    /// <summary>Para clientes existentes: el teléfono del archivo difiere del registrado.</summary>
    public bool PhoneChanged { get; set; }

    /// <summary>Recolector actualmente registrado para el cliente (solo existentes).</summary>
    public int? CurrentCollectorId { get; set; }
    public string? CurrentCollectorName { get; set; }

    /// <summary>Nombre de Facebook actualmente registrado (solo existentes).</summary>
    public string? CurrentFacebookName { get; set; }

    /// <summary>Teléfono actualmente registrado (solo existentes).</summary>
    public string? CurrentPhone { get; set; }

    /// <summary>"existing" | "ambiguous" | "new".</summary>
    public string MatchStatus { get; set; } = "new";

    /// <summary>Cliente coincidente cuando MatchStatus = "existing".</summary>
    public int? MatchedCustomerId { get; set; }

    /// <summary>Candidatos cuando MatchStatus = "ambiguous" (mismo nombre, distinto teléfono).</summary>
    public List<ImportCandidateDto> Candidates { get; set; } = [];

    public List<ImportProductLineDto> Products { get; set; } = [];
    public decimal Total { get; set; }
}

public class ImportCandidateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CollectorName { get; set; } = string.Empty;
}

public class ImportProductLineDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>El precio del archivo es inválido o falta; el usuario debe capturarlo antes de confirmar.</summary>
    public bool PriceMissing { get; set; }

    /// <summary>Texto original del precio en el archivo (para mostrarlo como referencia).</summary>
    public string? RawPrice { get; set; }
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

/// <summary>Advertencia de posible archivo ya subido previamente.</summary>
public class ImportDuplicateWarningDto
{
    public bool PossibleDuplicate { get; set; }
    public List<string> MatchedDescriptions { get; set; } = [];
    public List<string> EventNames { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}
