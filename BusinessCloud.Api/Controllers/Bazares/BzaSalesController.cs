using BusinessCloud.Application.Bazares.Commands.CreateBzaSale;
using BusinessCloud.Application.Bazares.Commands.DeleteBzaSale;
using BusinessCloud.Application.Bazares.Commands.ImportProductsToSale;
using BusinessCloud.Application.Bazares.Commands.RegisterBzaPayment;
using BusinessCloud.Application.Bazares.Commands.RegisterBzaPaymentWithFile;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaSale;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaSaleStatus;
using BusinessCloud.Application.Bazares.Commands.VerifyBzaPayment;
using BusinessCloud.Application.Bazares.Queries.GetAllBzaSales;
using BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;
using BusinessCloud.Application.Bazares.Queries.GetCustomerEventTicket;
using BusinessCloud.Application.Bazares.Queries.GetCustomerPackageLabel;
using BusinessCloud.Application.Bazares.Queries.GetCustomerSalesHistory;
using BusinessCloud.Application.Bazares.Queries.GetSalesTemplate;
using BusinessCloud.Application.Bazares.Queries.GetWeeklyTicket;
using BusinessCloud.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

/// <summary>
/// Controlador para gestionar Eventos de Venta (Cortes/En Vivos/Catálogos) del módulo Bazares.
/// Un Evento de Venta agrupa productos comprados por múltiples clientes.
/// </summary>
[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaSalesController(ISender mediator) : ControllerBase
{
    private readonly ISender _mediator = mediator;

    #region CRUD Eventos de Venta

    /// <summary>
    /// Crear un nuevo Evento de Venta (Corte/Catálogo/En Vivo).
    /// </summary>
    /// <returns>ID del Evento de Venta creado.</returns>
    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBzaSaleCommand command)
    {
        return await _mediator.Send(command);
    }

    /// <summary>
    /// Obtener todos los Eventos de Venta del tenant.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BzaSaleListDto>>> GetAll()
    {
        return await _mediator.Send(new GetAllBzaSalesQuery());
    }

    /// <summary>
    /// Obtener detalle de un Evento de Venta (métricas globales e historial).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BzaSaleDetailDto>> GetById(int id)
    {
        return await _mediator.Send(new GetBzaSaleDetailQuery(id));
    }

    /// <summary>
    /// Actualizar datos del Evento de Venta (Descripción, fechas de corte y entrega, estatus).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateBzaSaleCommand command)
    {
        if (id != command.Id) return BadRequest("El ID no coincide.");
        var result = await _mediator.Send(command);
        return result ? NoContent() : NotFound(new { message = "Evento de Venta no encontrado." });
    }

    /// <summary>
    /// Eliminar Evento de Venta (solo si ningún cliente tiene productos registrados en él).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, [FromQuery] string? reason)
    {
        var result = await _mediator.Send(new DeleteBzaSaleCommand(id, reason));
        return result.Success ? NoContent() : BadRequest(new { message = result.Message });
    }

    /// <summary>
    /// Cambiar estatus general del Evento de Venta.
    /// </summary>
    [HttpPatch("status")]
    public async Task<ActionResult> UpdateStatus(UpdateBzaSaleStatusCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    #endregion

    #region Importación Masiva en el Evento

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
    /// Importar compras masivas desde Excel para ESTE Evento de Venta.
    /// Lee filas con [Cliente, Teléfono, Producto, Precio, Costo]. 
    /// Crea los clientes si no existen y les asigna los productos bajo este Evento ID.
    /// </summary>
    [HttpPost("{id}/import")]
    public async Task<ActionResult<ImportProductsResult>> ImportProductsToSale(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Archivo vacío o no proporcionado." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var command = new ImportProductsToSaleCommand(id, ms.ToArray());
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    #endregion

    #region Pagos de Clientes

    /// <summary>
    /// Registrar abono/pago de un cliente para sus compras de un Evento de Venta (BzaSaleId).
    /// </summary>
    [HttpPost("payments")]
    public async Task<ActionResult<BzaPaymentResultDto>> RegisterPayment(RegisterBzaPaymentCommand command)
    {
        return await _mediator.Send(command);
    }

    /// <summary>
    /// Registrar pago con comprobante físico a BlobStorage (Status Preautorizado).
    /// </summary>
    [HttpPost("payments/with-proof")]
    public async Task<ActionResult<BzaPaymentWithFileResultDto>> RegisterPaymentWithProof(
        [FromForm] int bzaSaleId,
        [FromForm] int bzaCustomerId,
        [FromForm] decimal amount,
        [FromForm] string paymentMethod,
        [FromForm] string? reference,
        IFormFile? proofFile)
    {
        byte[]? fileContent = null;
        string? fileName = null;
        string? contentType = null;

        if (proofFile is not null && proofFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await proofFile.CopyToAsync(ms);
            fileContent = ms.ToArray();
            fileName = proofFile.FileName;
            contentType = proofFile.ContentType;
        }

        var command = new RegisterBzaPaymentWithFileCommand
        {
            BzaSaleId = bzaSaleId,
            BzaCustomerId = bzaCustomerId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Reference = reference,
            ProofFileContent = fileContent,
            ProofFileName = fileName,
            ProofContentType = contentType
        };

        return await _mediator.Send(command);
    }

    /// <summary>
    /// Aprobar o rechazar el pago preautorizado del cliente.
    /// </summary>
    [HttpPatch("payments/{paymentId}/verify")]
    public async Task<ActionResult> VerifyPayment(int paymentId, [FromBody] VerifyPaymentRequest request)
    {
        var command = new VerifyBzaPaymentCommand
        {
            PaymentId = paymentId,
            Approved = request.Approved,
            Notes = request.Notes
        };
        var result = await _mediator.Send(command);
        return result.Success
            ? Ok(new { message = result.Message, newCustomerStatus = result.NewCustomerStatus })
            : BadRequest(new { message = result.Message });
    }

    #endregion

    #region Tickets e Historial de Clientes

    /// <summary>
    /// Ticket del cliente de un Evento de Venta específico.
    /// Detalla qué productos compró en ESE en vivo/catálogo, sus abonos y saldo restante.
    /// </summary>
    [HttpGet("customer/{customerId}/ticket/{saleId}")]
    public async Task<ActionResult<CustomerEventTicketDto>> GetCustomerEventTicket(int customerId, int saleId)
    {
        return await _mediator.Send(new GetCustomerEventTicketQuery(customerId, saleId));
    }

    /// <summary>
    /// Ticket consolidado de la semana para el cliente.
    /// Agrupa todos los Eventos de Venta activos en la semana con sus desgloses y un Gran Total Combinado.
    /// </summary>
    [HttpGet("customer/{customerId}/weekly-ticket")]
    public async Task<ActionResult<WeeklyTicketDto>> GetWeeklyTicket(int customerId)
    {
        return await _mediator.Send(new GetWeeklyTicketQuery(customerId));
    }

    /// <summary>
    /// Historial completo del cliente agrupado por Evento de Venta (BzaSaleId).
    /// </summary>
    [HttpGet("customer/{customerId}/history")]
    public async Task<ActionResult<CustomerSalesHistoryDto>> GetCustomerHistory(int customerId)
    {
        return await _mediator.Send(new GetCustomerSalesHistoryQuery(customerId));
    }

    /// <summary>
    /// Datos de la etiqueta de paquete del cliente para un Evento de Venta pagado (Para impresión con QR).
    /// </summary>
    [HttpGet("customer/{customerId}/label/{saleId}")]
    public async Task<ActionResult<CustomerPackageLabelDto>> GetCustomerLabel(int customerId, int saleId)
    {
        return await _mediator.Send(new GetCustomerPackageLabelQuery(customerId, saleId));
    }

    #endregion
}

/// <summary>
/// Request para verificar/aprobar o rechazar un pago preautorizado.
/// </summary>
public class VerifyPaymentRequest
{
    public bool Approved { get; set; }
    public string? Notes { get; set; }
}