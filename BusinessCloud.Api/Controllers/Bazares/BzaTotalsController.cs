using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Bazares.Commands.CancelClosureSale;
using BusinessCloud.Application.Bazares.Commands.CancelPendingSales;
using BusinessCloud.Application.Bazares.Commands.CloseClosureDelivery;
using BusinessCloud.Application.Bazares.Commands.DeleteClosureDraft;
using BusinessCloud.Application.Bazares.Commands.DeleteDeliveryProof;
using BusinessCloud.Application.Bazares.Commands.ManualValidateClosureTotal;
using BusinessCloud.Application.Bazares.Commands.Notifications;
using BusinessCloud.Application.Bazares.Commands.MovePendingSales;
using BusinessCloud.Application.Bazares.Commands.ReactivateClosureSale;
using BusinessCloud.Application.Bazares.Commands.RejectClosureProof;
using BusinessCloud.Application.Bazares.Commands.ResyncClosureGroups;
using BusinessCloud.Application.Bazares.Commands.SendClosureWhatsApp;
using BusinessCloud.Application.Bazares.Commands.SendTotals;
using BusinessCloud.Application.Bazares.Commands.StartClosureDelivery;
using BusinessCloud.Application.Bazares.Commands.UploadClosureProof;
using BusinessCloud.Application.Bazares.Commands.UploadDeliveryProof;
using BusinessCloud.Application.Bazares.Commands.UploadPackedOrderPhotos;
using BusinessCloud.Application.Bazares.Commands.DeletePackedOrderPhoto;
using BusinessCloud.Application.Bazares.Commands.ValidateClosureProof;
using BusinessCloud.Application.Bazares.Queries.GetClosureDeliveryProofs;
using BusinessCloud.Application.Bazares.Queries.GetClosureEventDetail;
using BusinessCloud.Application.Bazares.Queries.GetClosureEvents;
using BusinessCloud.Application.Bazares.Queries.GetDeliveryLabelData;
using BusinessCloud.Application.Bazares.Queries.GetPendingMoveOptions;
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
    /// Cancela un cierre draft generado al previsualizar mensajes, para permitir
    /// corregir fechas y generar nuevamente el env�o de totales.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteDraft(int id)
    {
        await mediator.Send(new DeleteClosureDraftCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Envía por WhatsApp (Cloud API) el mensaje de cobro a todos los clientes del cierre
    /// y registra cada envío para dar seguimiento a su entrega.
    /// </summary>
    [HttpPost("{id:int}/send-whatsapp")]
    public async Task<ActionResult<SendClosureWhatsAppResultDto>> SendWhatsApp(int id, [FromBody] SendWhatsAppRequest body)
        => await mediator.Send(new SendClosureWhatsAppCommand(id, body?.PortalBaseUrl ?? string.Empty));

    /// <summary>
    /// Reintenta el envÃ­o por WhatsApp del mensaje de cobro solo para los clientes indicados
    /// (tÃ­picamente aquellos cuyo envÃ­o inicial fallÃ³).
    /// </summary>
    [HttpPost("{id:int}/send-whatsapp-retry")]
    public async Task<ActionResult<SendClosureWhatsAppResultDto>> RetryWhatsApp(int id, [FromBody] RetryWhatsAppRequest body)
        => await mediator.Send(new SendClosureWhatsAppCommand(id, body?.PortalBaseUrl ?? string.Empty, body?.CustomerIds));

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

    /// <summary>
    /// Opciones para mover las ventas pendientes (sin comprobante) de un evento de cierre
    /// antes de marcarlo "en proceso de entrega": cuántas hay y a qué otros eventos se pueden mover.
    /// </summary>
    [HttpGet("{id:int}/pending-move-options")]
    public async Task<ActionResult<PendingMoveOptionsDto>> PendingMoveOptions(int id)
        => await mediator.Send(new GetPendingMoveOptionsQuery(id));

    /// <summary>
    /// Mueve las ventas pendientes (sin comprobante) de un evento de cierre a otro
    /// existente o a uno nuevo, para que no queden "atrapadas" en un evento ya despachado.
    /// </summary>
    [HttpPost("{id:int}/move-pending")]
    public async Task<ActionResult<MovePendingSalesResultDto>> MovePendingSales(int id, [FromBody] MovePendingSalesRequest body)
        => await mediator.Send(new MovePendingSalesCommand(
            id,
            body.Mode,
            body.TargetClosureEventId,
            body.NewDeliveryDate,
            body.NewPaymentDeadline));

    /// <summary>
    /// Cancela por sistema todas las ventas pendientes (sin comprobante) de un evento
    /// de cierre. Se pueden reactivar después desde Validación de comprobantes.
    /// </summary>
    [HttpPost("{id:int}/cancel-pending")]
    public async Task<ActionResult<CancelPendingSalesResultDto>> CancelPendingSales(int id)
        => await mediator.Send(new CancelPendingSalesCommand(id));

    /// <summary>Sube una o varias fotos del pedido empacado para un cliente del cierre.</summary>
    [HttpPost("totals/{totalId:int}/packed-photos")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<UploadPackedOrderPhotosResultDto>> UploadPackedOrderPhotos(
        int totalId,
        [FromForm] List<IFormFile> files)
    {
        var incoming = (files ?? [])
            .Where(file => file is not null && file.Length > 0)
            .ToList();

        var streams = new List<Stream>();
        try
        {
            var inputs = incoming.Select(file =>
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                return new PackedOrderPhotoFileInput(stream, file.FileName, file.ContentType);
            }).ToList();

            return await mediator.Send(new UploadPackedOrderPhotosCommand(totalId, inputs));
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }
    }

    /// <summary>Elimina una foto del pedido empacado subida por error.</summary>
    [HttpDelete("packed-photos/{photoId:int}")]
    public async Task<ActionResult<DeletePackedOrderPhotoResultDto>> DeletePackedOrderPhoto(int photoId)
        => await mediator.Send(new DeletePackedOrderPhotoCommand(photoId));
    /// <summary>
    /// Detalle de entrega de un evento de cierre: grupos participantes y comprobantes
    /// de entrega (firmas/fotos de recibido) ya subidos.
    /// </summary>
    [HttpGet("{id:int}/delivery-proofs")]
    public async Task<ActionResult<ClosureDeliveryProofsDto>> GetDeliveryProofs(int id)
        => await mediator.Send(new GetClosureDeliveryProofsQuery(id));

    /// <summary>
    /// Sube uno o varios comprobantes de entrega (firma/foto de recibido) para un evento
    /// de cierre en proceso de entrega. Si no se indica grupo, el comprobante es general.
    /// </summary>
    [HttpPost("{id:int}/delivery-proofs")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<UploadDeliveryProofResultDto>> UploadDeliveryProofs(
        int id,
        [FromForm] List<IFormFile> files,
        [FromForm] int? collectorGroupId = null)
    {
        var incoming = (files ?? new List<IFormFile>())
            .Where(f => f is not null && f.Length > 0)
            .ToList();

        if (incoming.Count == 0)
            return BadRequest("Debes adjuntar al menos un archivo.");

        var streams = new List<Stream>();
        try
        {
            var inputs = new List<DeliveryProofFileInput>();
            foreach (var f in incoming)
            {
                var stream = f.OpenReadStream();
                streams.Add(stream);
                inputs.Add(new DeliveryProofFileInput(stream, f.FileName, f.ContentType));
            }

            var result = await mediator.Send(new UploadDeliveryProofCommand(id, collectorGroupId, inputs));
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

    /// <summary>Elimina un comprobante de entrega subido por error.</summary>
    [HttpDelete("delivery-proofs/{proofId:int}")]
    public async Task<ActionResult<DeleteDeliveryProofResultDto>> DeleteDeliveryProof(int proofId)
        => await mediator.Send(new DeleteDeliveryProofCommand(proofId));

    /// <summary>
    /// Cierra la entrega de un evento de cierre (requiere al menos un comprobante subido).
    /// A partir de aquí, los clientes ven su comprobante de entrega en su enlace.
    /// </summary>
    [HttpPost("{id:int}/close-delivery")]
    public async Task<ActionResult<CloseClosureDeliveryResultDto>> CloseDelivery(int id)
        => await mediator.Send(new CloseClosureDeliveryCommand(id));
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

/// <summary>Cuerpo de la peticiÃ³n de reintento de envÃ­o por WhatsApp a clientes especÃ­ficos.</summary>
public class RetryWhatsAppRequest
{
    public List<int>? CustomerIds { get; set; }
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

/// <summary>Cuerpo de la petición para mover ventas pendientes de un evento de cierre.</summary>
public class MovePendingSalesRequest
{
    public MovePendingSalesMode Mode { get; set; }
    public int? TargetClosureEventId { get; set; }
    public DateTime? NewDeliveryDate { get; set; }
    public DateTime? NewPaymentDeadline { get; set; }
}



