using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Bazares.Queries.GetCancelledSalesReport;
using BusinessCloud.Application.Bazares.Queries.GetPendingWithdrawalsReport;
using BusinessCloud.Application.Bazares.Queries.GetRejectedProofsReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

/// <summary>
/// Reportes del módulo Bazares.
/// </summary>
[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaReportsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Reporte de comprobantes rechazados: clientes, motivos y referencias.
    /// </summary>
    [HttpGet("rejected-proofs")]
    public async Task<ActionResult<RejectedProofsReportDto>> RejectedProofs(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => await mediator.Send(new GetRejectedProofsReportQuery(from, to));

    /// <summary>
    /// Reporte de ventas canceladas: motivos y clasificación de responsabilidad del cliente.
    /// </summary>
    [HttpGet("cancelled-sales")]
    public async Task<ActionResult<CancelledSalesReportDto>> CancelledSales(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => await mediator.Send(new GetCancelledSalesReportQuery(from, to));

    /// <summary>
    /// Reporte de retiros sin tarjeta pendientes de validar: cliente, monto, banco,
    /// venta asociada y enlaces a las imágenes de los comprobantes.
    /// </summary>
    [HttpGet("pending-withdrawals")]
    public async Task<ActionResult<PendingWithdrawalsReportDto>> PendingWithdrawals(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => await mediator.Send(new GetPendingWithdrawalsReportQuery(from, to));
}
