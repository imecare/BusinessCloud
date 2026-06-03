using BusinessCloud.Application.Common.Interfaces;
using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.ImportSalesFromExcel;

/// <summary>
/// DEPRECATED: ImportSalesFromExcel ya no aplica porque una Venta (Sale) es ahora un Evento
/// que agrupa productos de múltiples clientes, no crea ventas individuales.
/// Usar ImportProductsToSale con un BzaSaleId específico para importar productos a un evento.
/// </summary>
public class ImportSalesFromExcelHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<ImportSalesFromExcelCommand, ImportSalesResult>
{
    public Task<ImportSalesResult> Handle(ImportSalesFromExcelCommand request, CancellationToken ct)
    {
        throw new NotImplementedException(
            "ImportSalesFromExcel deprecado. Una Venta (BzaSale) es ahora un Evento que agrupa productos de múltiples clientes. " +
            "Use el endpoint POST /api/bazares/sales/{saleId}/import para importar productos desde Excel a un evento de venta existente.");
    }
}
