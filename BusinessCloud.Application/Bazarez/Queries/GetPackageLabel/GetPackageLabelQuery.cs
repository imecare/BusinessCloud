using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetPackageLabel;

public record GetPackageLabelQuery(int BzaSaleId) : IRequest<PackageLabelDto>;

public class PackageLabelDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CollectorName { get; set; } = string.Empty;
    public string? CollectorGroup { get; set; }
    public string LabelCode { get; set; } = string.Empty;
    public int PieceCount { get; set; }
    public decimal Total { get; set; }
    public DateTime Date { get; set; }
    public List<string> Products { get; set; } = new();
}
