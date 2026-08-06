using BusinessCloud.Api.Common;
using BusinessCloud.Application.Bazarez.Commands.ProcessWhatsAppWebhook;
using BusinessCloud.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Shared;

/// <summary>
/// Webhooks de WhatsApp Cloud API (Meta). Endpoint publico:
/// - GET: verificacion del webhook (hub.challenge / hub.verify_token).
/// - POST: recepcion de eventos (estatus de entrega de mensajes: sent/delivered/read/failed).
/// El estatus se guarda en Bza_WhatsAppMessages para mostrar al bazar si el mensaje llego
/// al cliente o el motivo por el que no se entrego.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/whatsapp/webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppWebhookCommandQueue _queue;
    private readonly IConfiguration _config;
    private readonly IPasswordRecoverySessionStore _passwordRecoverySessions;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IWhatsAppWebhookCommandQueue queue,
        IConfiguration config,
        IPasswordRecoverySessionStore passwordRecoverySessions,
        ILogger<WhatsAppWebhookController> logger)
    {
        _queue = queue;
        _config = config;
        _passwordRecoverySessions = passwordRecoverySessions;
        _logger = logger;
    }

    /// <summary>Verificacion del webhook por parte de Meta al suscribirlo.</summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expected = _config["WhatsApp:WebhookVerifyToken"];

        if (mode == "subscribe" && !string.IsNullOrEmpty(expected) && verifyToken == expected)
        {
            return Content(challenge ?? string.Empty, "text/plain");
        }

        _logger.LogWarning("Verificacion de webhook de WhatsApp rechazada (mode={Mode}).", mode);
        return StatusCode(403, "Verificacion fallida.");
    }

    /// <summary>Recepcion de eventos del webhook (estatus de mensajes e inbound).</summary>
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] WhatsAppWebhookPayload body, CancellationToken ct)
    {
        var command = new ProcessWhatsAppWebhookCommand(
            body.Entry
                .SelectMany(e => e.Changes)
                .SelectMany(c => c.Value?.Statuses ?? Enumerable.Empty<WhatsAppWebhookStatusPayload>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Id) && !string.IsNullOrWhiteSpace(s.Status))
                .Select(s => new WhatsAppWebhookStatusInput(
                    s.Id!,
                    s.Status!,
                    s.RecipientId,
                    s.Errors.FirstOrDefault()?.Code,
                    s.Errors.FirstOrDefault()?.Title,
                    s.Errors.FirstOrDefault()?.Message ?? s.Errors.FirstOrDefault()?.ErrorData?.Details))
                .ToList(),
            body.Entry
                .SelectMany(e => e.Changes)
                .SelectMany(c => c.Value?.Messages ?? Enumerable.Empty<WhatsAppWebhookMessagePayload>())
                .Where(m => string.Equals(m.Type, "text", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(m.Id)
                            && !string.IsNullOrWhiteSpace(m.From)
                            && !string.IsNullOrWhiteSpace(m.Text?.Body))
                .Select(m => new WhatsAppWebhookTextInput(m.Id!, m.From!, m.Type!, m.Text!.Body!))
                .ToList());

        await _queue.EnqueueAsync(command, ct);

        return Ok();
    }
}
