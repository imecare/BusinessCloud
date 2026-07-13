using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaCustomersTemplate;

/// <summary>
/// Genera un archivo Excel para importar clientes. Incluye listas desplegables de
/// clientes existentes (para elegir o escribir nuevos) y de recolectores.
/// </summary>
public record GetBzaCustomersTemplateQuery : IRequest<BzaCustomersTemplateResult>;

public record BzaCustomersTemplateResult(byte[] FileContent, string FileName, string ContentType);
