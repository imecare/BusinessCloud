using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.ValidateBzaCustomersImport;

/// <summary>Valida un Excel de clientes sin guardar cambios.</summary>
public record ValidateBzaCustomersImportQuery(byte[] FileContent)
    : IRequest<ValidateBzaCustomersImportResult>;

public class ValidateBzaCustomersImportResult
{
    public bool HasRows { get; set; }
    public int TotalRows { get; set; }
    public int ExactDuplicateRows { get; set; }
    public int CollectorConflictCount { get; set; }
    public List<ImportCustomerRowDto> Customers { get; set; } = [];
    public List<ImportCollectorDto> Collectors { get; set; } = [];
    public List<ImportCollectorGroupDto> CollectorGroups { get; set; } = [];
    public List<string> NewCollectors { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public class ImportCustomerRowDto
{
    public string Name { get; set; } = string.Empty;
    public string PhoneFromFile { get; set; } = string.Empty;
    public string CollectorNameFromFile { get; set; } = string.Empty;
    public string FacebookNameFromFile { get; set; } = string.Empty;
    public int? SuggestedCollectorId { get; set; }
    public bool CollectorExists { get; set; }
    public bool CollectorAmbiguous { get; set; }
    public bool HasCollectorConflict { get; set; }
    public List<string> CollectorConflictNames { get; set; } = [];
    public string MatchStatus { get; set; } = "new";
    public int? MatchedCustomerId { get; set; }
    public bool PhoneConflict { get; set; }
    public string? PhoneConflictCustomerName { get; set; }
    public bool WillHaveNoWhatsApp { get; set; }
    public bool WillBePendingInfo { get; set; }
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
