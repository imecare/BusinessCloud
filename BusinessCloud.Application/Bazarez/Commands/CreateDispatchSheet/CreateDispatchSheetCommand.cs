using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateDispatchSheet;

public record CreateDispatchSheetCommand(int BzaCollectorId, DateTime DispatchDate) : IRequest<DispatchSheetResultDto>;

public class DispatchSheetResultDto
{
    public int DispatchSheetId { get; set; }
    public string CollectorName { get; set; } = string.Empty;
    public DateTime DispatchDate { get; set; }
    public int TotalPackages { get; set; }
    public List<DispatchItemDto> Items { get; set; } = new();
}

public class DispatchItemDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int PieceCount { get; set; }
    public string LabelCode { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
