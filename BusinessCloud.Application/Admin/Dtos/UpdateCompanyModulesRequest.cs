namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>Aplicaciones que deben quedar habilitadas para una empresa.</summary>
public sealed record UpdateCompanyModulesRequest
{
    public string[] Modules { get; init; } = [];
}
