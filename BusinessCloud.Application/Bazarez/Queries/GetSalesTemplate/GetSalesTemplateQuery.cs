using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetSalesTemplate;

/// <summary>
/// Genera un archivo Excel pre-cargado con clientes y recolectores del tenant.
/// </summary>
public record GetSalesTemplateQuery : IRequest<SalesTemplateResult>;

public record SalesTemplateResult(byte[] FileContent, string FileName, string ContentType);
