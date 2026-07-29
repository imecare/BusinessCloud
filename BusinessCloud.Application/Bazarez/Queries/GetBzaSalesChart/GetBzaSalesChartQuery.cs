using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSalesChart;

/// <summary>
/// Datos para las graficas de ventas del dashboard:
/// ventas por semana dentro de un mes y ventas por mes dentro de un anio.
/// El monto de una venta es la suma de los precios de sus productos y se ubica
/// en el periodo segun su fecha de captura (CreatedAt).
/// </summary>
public record GetBzaSalesChartQuery(int? Year, int? Month) : IRequest<BzaSalesChartDto>;

public record SalesBucketDto(string Label, decimal Amount, int Index);

public record BzaSalesChartDto(
    int Year,
    int Month,
    List<SalesBucketDto> Weekly,
    List<SalesBucketDto> Monthly,
    List<int> AvailableYears);
