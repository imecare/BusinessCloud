using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;

public record BzaSaleProductDto(string Description, decimal Price);
public record BzaSaleAuditDto(string Event, DateTime Timestamp, string Details);

public record BzaSaleDetailDto(
    int Id,
    string? Description,
    decimal Total,
    int Status,
    string CustomerName,
    List<BzaSaleProductDto> Products,
    List<BzaSaleAuditDto> AuditHistory // Datos de MongoDB
);

public record GetBzaSaleDetailQuery(int Id) : IRequest<BzaSaleDetailDto>;