using System.Text.Json;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Api.Controllers.Shared;

/// <summary>
/// Webhooks de WhatsApp Cloud API (Meta). Endpoint público:
/// - GET: verificación del webhook (hub.challenge / hub.verify_token).
/// - POST: recepción de eventos (estatus de entrega de mensajes: sent/delivered/read/failed).
/// El estatus se guarda en Bza_WhatsAppMessages para mostrar al bazar si el mensaje llegó
/// al cliente o el motivo por el que no se entregó.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/whatsapp/webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IBazaresDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IBazaresDbContext context,
        IConfiguration config,
        ILogger<WhatsAppWebhookController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    /// <summary>Verificación del webhook por parte de Meta al suscribirlo.</summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expected = _config["WhatsApp:WebhookVerifyToken"];

        if (mode == "subscribe" && !string.IsNullOrEmpty(expected) && verifyToken == expected)
        {
            // Meta espera que se devuelva el challenge tal cual, con 200.
            return Content(challenge ?? string.Empty, "text/plain");
        }

        _logger.LogWarning("Verificación de webhook de WhatsApp rechazada (mode={Mode}).", mode);
        return StatusCode(403, "Verificación fallida.");
    }

    /// <summary>Recepción de eventos del webhook (estatus de mensajes e inbound).</summary>
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement body, CancellationToken ct)
    {
        try
        {
            await ProcessAsync(body, ct);
        }
        catch (Exception ex)
        {
            // Nunca fallar hacia Meta: se registra y se responde 200 para evitar reintentos agresivos.
            _logger.LogError(ex, "Error procesando webhook de WhatsApp.");
        }

        return Ok();
    }

    private async Task ProcessAsync(JsonElement root, CancellationToken ct)
    {
        if (!root.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return;

        var changed = false;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                    continue;

                if (!value.TryGetProperty("statuses", out var statuses) || statuses.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var status in statuses.EnumerateArray())
                {
                    changed |= await ApplyStatusAsync(status, ct);
                }
            }
        }

        if (changed)
            await _context.SaveChangesAsync(ct);
    }

    private async Task<bool> ApplyStatusAsync(JsonElement status, CancellationToken ct)
    {
        var wamid = status.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        var statusText = status.TryGetProperty("status", out var stProp) ? stProp.GetString() : null;
        var recipient = status.TryGetProperty("recipient_id", out var rProp) ? rProp.GetString() : null;

        if (string.IsNullOrEmpty(wamid) || string.IsNullOrEmpty(statusText))
            return false;

        int? errorCode = null;
        string? errorTitle = null;
        string? errorMessage = null;

        if (status.TryGetProperty("errors", out var errors)
            && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
        {
            var err = errors[0];
            if (err.TryGetProperty("code", out var codeProp) && codeProp.TryGetInt32(out var c))
                errorCode = c;
            if (err.TryGetProperty("title", out var titleProp))
                errorTitle = titleProp.GetString();
            if (err.TryGetProperty("message", out var msgProp))
                errorMessage = msgProp.GetString();
            if (errorMessage is null
                && err.TryGetProperty("error_data", out var edata)
                && edata.TryGetProperty("details", out var detailsProp))
                errorMessage = detailsProp.GetString();
        }

        var now = DateTime.UtcNow;

        var existing = await _context.WhatsAppMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.WaMessageId == wamid, ct);

        if (existing is null)
        {
            // Estatus recibido sin registro de envío previo (por ejemplo, mensajes enviados
            // antes de habilitar el registro): se crea una fila solo con el estatus.
            _context.WhatsAppMessages.Add(new BzaWhatsAppMessage
            {
                WaMessageId = wamid,
                ToPhone = recipient ?? string.Empty,
                Purpose = "unknown",
                Status = statusText,
                ErrorCode = errorCode,
                ErrorTitle = errorTitle,
                ErrorMessage = errorMessage,
                SentAt = now,
                StatusUpdatedAt = now,
            });
            return true;
        }

        // No degradar el estatus (read > delivered > sent). Solo actualizar si avanza o es failed.
        existing.Status = statusText;
        existing.StatusUpdatedAt = now;
        if (statusText == "failed")
        {
            existing.ErrorCode = errorCode;
            existing.ErrorTitle = errorTitle;
            existing.ErrorMessage = errorMessage;
        }
        return true;
    }
}
