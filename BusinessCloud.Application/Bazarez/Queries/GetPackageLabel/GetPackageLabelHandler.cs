using BusinessCloud.Application.Common.Interfaces;
using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetPackageLabel;

/// <summary>
/// DEPRECATED: GetPackageLabel ya no aplica porque una Venta (Sale) es ahora un Evento
/// que agrupa productos de múltiples clientes, no de uno solo.
/// Usar GetCustomerPackageLabel con customerId y saleId específicos.
/// </summary>
#pragma warning disable CS9113 // Parameter is unread
public class GetPackageLabelHandler(IBazaresDbContext _)
    : IRequestHandler<GetPackageLabelQuery, PackageLabelDto>
#pragma warning restore CS9113
{
    public Task<PackageLabelDto> Handle(GetPackageLabelQuery request, CancellationToken ct)
    {
        throw new NotImplementedException(
            "GetPackageLabel deprecado. Una Venta (BzaSale) es un Evento que agrupa productos de múltiples clientes. " +
            "Use el endpoint GET /api/bazares/sales/customer/{customerId}/label/{saleId} para obtener " +
            "la etiqueta de paquete de un cliente específico dentro de un evento.");
    }
}
