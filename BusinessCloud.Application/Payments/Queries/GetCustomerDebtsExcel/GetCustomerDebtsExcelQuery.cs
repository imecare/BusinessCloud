using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerDebtsExcel;

/// <summary>Descarga las deudas activas de la empresa con sus compras y abonos.</summary>
public sealed record GetCustomerDebtsExcelQuery : IRequest<CustomerDebtsExcelResult>;

/// <summary>Archivo Excel del reporte de deudas activas.</summary>
public sealed record CustomerDebtsExcelResult(byte[] Content, string FileName, string ContentType);
