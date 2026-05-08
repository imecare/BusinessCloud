using BusinessCloud.Application.Bazares.Commands.CreateBzaSale;
using BusinessCloud.Application.Bazares.Commands.ImportSalesFromExcel;
using BusinessCloud.Application.Bazares.Commands.MarkSaleReadyForDelivery;
using BusinessCloud.Application.Bazares.Commands.RegisterBzaPayment;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaSaleStatus;
using BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;
using BusinessCloud.Application.Bazares.Queries.GetPackageLabel;
using BusinessCloud.Application.Bazares.Queries.GetSalesTemplate;
using BusinessCloud.Application.Bazares.Queries.GetWeeklyTicket;
using BusinessCloud.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaSalesController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaSalesController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBzaSaleCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BzaSaleDetailDto>> GetById(int id)
    {
        return await _mediator.Send(new GetBzaSaleDetailQuery(id));
    }

    [HttpPatch("status")]
    public async Task<ActionResult> UpdateStatus(UpdateBzaSaleStatusCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Descargar plantilla Excel pre-cargada con clientes y recolectores.
    /// </summary>
    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate()
    {
        var result = await _mediator.Send(new GetSalesTemplateQuery());
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Importar ventas masivas desde archivo Excel.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportSalesResult>> ImportFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo vacío o no proporcionado." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var result = await _mediator.Send(new ImportSalesFromExcelCommand(ms.ToArray()));
        return Ok(result);
    }

    /// <summary>
    /// Registrar pago en una venta.
    /// </summary>
    [HttpPost("payments")]
    public async Task<ActionResult<BzaPaymentResultDto>> RegisterPayment(RegisterBzaPaymentCommand command)
    {
        return await _mediator.Send(command);
    }

    /// <summary>
    /// Marcar venta como lista para entrega (requiere status=2 Pagado).
    /// </summary>
    [HttpPatch("{id}/ready-for-delivery")]
    public async Task<ActionResult> MarkReadyForDelivery(int id)
    {
        await _mediator.Send(new MarkSaleReadyForDeliveryCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Ticket semanal consolidado de un cliente.
    /// </summary>
    [HttpGet("weekly-ticket/{customerId}")]
    public async Task<ActionResult<WeeklyTicketDto>> GetWeeklyTicket(int customerId)
    {
        return await _mediator.Send(new GetWeeklyTicketQuery(customerId));
    }

    /// <summary>
    /// Datos de etiqueta de paquete (para impresión con QR).
    /// </summary>
    [HttpGet("{id}/label")]
    public async Task<ActionResult<PackageLabelDto>> GetLabel(int id)
    {
        return await _mediator.Send(new GetPackageLabelQuery(id));
    }
}