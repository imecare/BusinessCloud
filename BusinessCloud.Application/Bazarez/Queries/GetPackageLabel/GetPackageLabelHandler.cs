using BusinessCloud.Application.Common.Interfaces;
using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetPackageLabel;

/// <summary>
/// DEPRECATED: GetPackageLabel ya no aplica porque una Venta (Sale) es ahora un Evento
/// que agrupa productos de múltiples clientes, no de uno solo.
/// Usar GetCustomerPackageLabel con customerId y saleId específicos.
/// </summary>
public class GetPackageLabelHandler(IBazaresDbContext context)
    : IRequestHandler<GetPackageLabelQuery, PackageLabelDto>
{
    public Task<PackageLabelDto> Handle(GetPackageLabelQuery request, CancellationToken ct)
    {
        throw new NotImplementedException(
            "GetPackageLabel deprecado. Una Venta (BzaSale) es un Evento que agrupa productos de múltiples clientes. " +
            "Use el endpoint GET /api/bazares/sales/customer/{customerId}/label/{saleId} para obtener " +
            "la etiqueta de paquete de un cliente específico dentro de un evento.");
    }
}
