using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Bazares.Commands.CancelClosureSale;
using BusinessCloud.Application.Bazares.Commands.ManualValidateClosureTotal;
using BusinessCloud.Application.Bazares.Commands.Notifications;
using BusinessCloud.Application.Bazares.Commands.ReactivateClosureSale;
using BusinessCloud.Application.Bazares.Commands.RejectClosureProof;
using BusinessCloud.Application.Bazares.Commands.ResyncClosureGroups;
using BusinessCloud.Application.Bazares.Commands.SendClosureWhatsApp;
using BusinessCloud.Application.Bazares.Commands.SendTotals;
using BusinessCloud.Application.Bazares.Commands.StartClosureDelivery;
using BusinessCloud.Application.Bazares.Commands.UploadClosureProof;
using BusinessCloud.Application.Bazares.Commands.ValidateClosureProof;
using BusinessCloud.Application.Bazares.Queries.GetClosureEventDetail;
using BusinessCloud.Application.Bazares.Queries.GetClosureEvents;
using BusinessCloud.Application.Bazares.Queries.GetDeliveryLabelData;
using BusinessCloud.Application.Bazares.Queries.GetReactivationOptions;
using BusinessCloud.Application.Bazares.Queries.PrepareTotals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaTotalsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Prepara el envío de totales para los eventos seleccionados:
    /// grupos participantes, fechas de entrega sugeridas, clientes y montos.
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<PrepareTotalsResultDto>> Preview(PrepareTotalsQuery query)
        => await mediator.Send(query);

    /// <summary>
    /// Envía los totales: cierra los eventos, crea el cierre de venta,
    /// registra fechas de entrega por grupo y genera los mensajes por cliente.
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<SendTotalsResultDto>> Send(SendTotalsCommand command)
        => await mediator.Send(command);

    /// <summary>
    /// Envía por WhatsApp (Cloud API) el mensaje de cobro a todos los clientes del cierre
    /// y registra cada envío para dar seguimiento a su entrega.
    /// </summary>
    [HttpPost("{id:int}/send-whatsapp")]
    public async Task<ActionResult<SendClosureWhatsAppResultDto>> SendWhatsApp(int id, [FromBody] SendWhatsAppRequest body)
        => await mediator.Send(new SendClosureWhatsAppCommand(id, body?.PortalBaseUrl ?? string.Empty));

    /// <summary>
    /// Envia notificaciones masivas para clientes seleccionados usando el canal elegido.
    /// </summary>
    [HttpPost("notifications/bulk")]
    public async Task<ActionResult<SendBulkNotificationsResultDto>> SendBulkNotifications([FromBody] SendBulkNotificationsRequest body)
        => await mediator.Send(new SendBulkNotificationsCommand(
            body.CustomerTotalIds ?? new List<int>(),
            body.NotificationType,
            body.ChannelStrategy,
            body.PortalBaseUrl));

    /// <summary>
    /// Historial de cierres de venta (envíos de totales).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ClosureEventListItemDto>>> GetAll()
        => await mediator.Send(new GetClosureEventsQuery());

    /// <summary>
    /// Detalle de un evento de pago: totales por cliente y sus comprobantes para revisar.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClosureEventDetailDto>> GetDetail(int id)
        => await mediator.Send(new GetClosureEventDetailQuery(id));

    /// <summary>
    /// Valida el comprobante de un cliente: marca la venta como pagada y, si todos
    /// los comprobantes del evento de pago quedan validados, cierra el evento.
    /// </summary>
    [HttpPost("totals/{totalId:int}/validate")]
    public async Task<ActionResult<ValidateClosureProofResultDto>> ValidateProof(int totalId)
        => await mediator.Send(new ValidateClosureProofCommand(totalId));

    /// <summary>
    /// Validación manual por el bazar: valida el total adjuntando el/los comprobante(s)
    /// (recibidos por otro medio) o sin comprobante con una nota obligatoria.
    /// </summary>
    [HttpPost("totals/{totalId:int}/manual-validate")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<ManualValidateClosureTotalResultDto>> ManualValidate(
        int totalId,
        [FromForm] List<IFormFile>? files = null,
        [FromForm] string? note = null)
    {
        var incoming = (files ?? new List<IFormFile>())
            .Where(f => f is not null && f.Length > 0)
            .ToList();

        var streams = new List<Stream>();
        try
        {
            var inputs = new List<ClosureProofFileInput>();
            foreach (var f in incoming)
            {
                var stream = f.OpenReadStream();
                streams.Add(stream);
                inputs.Add(new ClosureProofFileInput(stream, f.FileName, f.ContentType));
            }

            var result = await mediator.Send(new ManualValidateClosureTotalCommand(totalId, inputs, note));
            return Ok(result);
        }
        finally
        {
            foreach (var s in streams)
            {
                await s.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Rechaza el comprobante de un cliente con un motivo. El cliente podrá
    /// consultarlo en su enlace y volver a subir un comprobante.
    /// </summary>
    [HttpPost("totals/{totalId:int}/reject")]
    public async Task<ActionResult<RejectClosureProofResultDto>> RejectProof(int totalId, [FromBody] RejectProofRequest body)
        => await mediator.Send(new RejectClosureProofCommand(totalId, body?.Reason ?? string.Empty));

    /// <summary>
    /// Cancela la venta de un cliente (p. ej. porque no se recibió el pago). El bazar
    /// captura un motivo e indica si la cancelación es responsabilidad del cliente.
    /// </summary>
    [HttpPost("totals/{totalId:int}/cancel")]
    public async Task<ActionResult<CancelClosureSaleResultDto>> CancelSale(int totalId, [FromBody] CancelSaleRequest body)
        => await mediator.Send(new CancelClosureSaleCommand(totalId, body?.Reason ?? string.Empty, body?.IsCustomerFault ?? false));

    /// <summary>
    /// Reactiva una venta cancelada: vuelve a Pendiente para que el cliente suba comprobante.
    /// Permite mantenerla en el mismo evento, moverla a uno existente o crear uno nuevo.
    /// </summary>
    [HttpPost("totals/{totalId:int}/reactivate")]
    public async Task<ActionResult<ReactivateClosureSaleResultDto>> ReactivateSale(int totalId, [FromBody] ReactivateSaleRequest? body)
        => await mediator.Send(new ReactivateClosureSaleCommand(
            totalId,
            body?.Mode ?? ReactivateMode.Same,
            body?.TargetClosureEventId,
            body?.NewDeliveryDate,
            body?.NewPaymentDeadline));

    /// <summary>
    /// Opciones para reactivar una venta cancelada (si requiere reasignar evento y candidatos).
    /// </summary>
    [HttpGet("totals/{totalId:int}/reactivation-options")]
    public async Task<ActionResult<ReactivationOptionsDto>> ReactivationOptions(int totalId)
        => await mediator.Send(new GetReactivationOptionsQuery(totalId));

    /// <summary>
    /// Datos para generar etiquetas y hoja de despacho de un evento de entrega:
    /// identidad del bazar, grupos participantes y clientes con sus productos.
    /// </summary>
    [HttpGet("{id:int}/delivery-labels")]
    public async Task<ActionResult<DeliveryLabelDataDto>> GetDeliveryLabels(int id)
        => await mediator.Send(new GetDeliveryLabelDataQuery(id));

    /// <summary>
    /// Re-sincroniza el grupo de recolección de los clientes del cierre con el grupo
    /// actual de su recolector (útil tras reasignar recolectores después del envío de totales).
    /// </summary>
    [HttpPost("{id:int}/resync-groups")]
    public async Task<ActionResult<ResyncClosureGroupsResultDto>> ResyncGroups(int id)
        => await mediator.Send(new ResyncClosureGroupsCommand(id));

    /// <summary>
    /// Marca el evento de entrega como "en proceso de entrega" (tras imprimir etiquetas).
    /// </summary>
    [HttpPost("{id:int}/start-delivery")]
    public async Task<ActionResult<StartClosureDeliveryResultDto>> StartDelivery(int id)
        => await mediator.Send(new StartClosureDeliveryCommand(id));
}

/// <summary>Cuerpo de la petición de rechazo de comprobante.</summary>
public class RejectProofRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Cuerpo de la petición para enviar los mensajes del cierre por WhatsApp.</summary>
public class SendWhatsAppRequest
{
    public string? PortalBaseUrl { get; set; }
}

public class SendBulkNotificationsRequest
{
    public List<int>? CustomerTotalIds { get; set; }
    public int NotificationType { get; set; }
    public int ChannelStrategy { get; set; }
    public string? PortalBaseUrl { get; set; }
}

/// <summary>Cuerpo de la petición de cancelación de venta.</summary>
public class CancelSaleRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool IsCustomerFault { get; set; }
}

/// <summary>Cuerpo de la petición de reactivación de venta.</summary>
public class ReactivateSaleRequest
{
    public ReactivateMode Mode { get; set; } = ReactivateMode.Same;
    public int? TargetClosureEventId { get; set; }
    public DateTime? NewDeliveryDate { get; set; }
    public DateTime? NewPaymentDeadline { get; set; }
}
