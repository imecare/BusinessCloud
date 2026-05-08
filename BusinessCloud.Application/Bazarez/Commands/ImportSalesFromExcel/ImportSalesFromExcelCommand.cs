using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.ImportSalesFromExcel;

public record ImportSalesFromExcelCommand(byte[] FileContent) : IRequest<ImportSalesResult>;

public class ImportSalesResult
{
    public int TotalRows { get; set; }
    public int SalesCreated { get; set; }
    public int CustomersCreated { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
