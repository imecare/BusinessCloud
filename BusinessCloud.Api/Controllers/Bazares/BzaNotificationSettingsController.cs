using Microsoft.AspNetCore.Mvc;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using BusinessCloud.Application.Bazares.Queries.GetNotificationSettings;
using BusinessCloud.Application.Bazares.Queries.GenerateChargeMessage;
using BusinessCloud.Application.Bazares.Commands.UpdateNotificationMessages;
using BusinessCloud.Application.Bazares.Commands.CreatePaymentCard;
using BusinessCloud.Application.Bazares.Commands.UpdatePaymentCard;
using BusinessCloud.Application.Bazares.Commands.DeletePaymentCard;
using BusinessCloud.Application.Bazares.Commands.SetPaymentCardActive;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaNotificationSettingsController(
    ISender mediator,
    IVerificationCodeService verification,
    ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Obtiene los mensajes personalizados y las tarjetas activas.</summary>
    [HttpGet]
    public async Task<ActionResult<NotificationSettingsDto>> Get([FromQuery] bool includeInactiveCards = true)
        => await mediator.Send(new GetNotificationSettingsQuery(includeInactiveCards));

    /// <summary>Genera el mensaje de cobro de un cliente (productos pendientes, totales y tarjetas activas).</summary>
    [HttpGet("charge-message/{customerId:int}")]
    public async Task<ActionResult<ChargeMessageResultDto>> GenerateChargeMessage(int customerId)
        => await mediator.Send(new GenerateChargeMessageQuery(customerId));

    /// <summary>Actualiza (upsert) los mensajes personalizados del tenant.</summary>
    [HttpPut("messages")]
    public async Task<ActionResult> UpdateMessages(UpdateNotificationMessagesCommand command)
    {
        await mediator.Send(command);
        return NoContent();
    }

    /// <summary>Crea una nueva tarjeta. Solo SuperAdmin, con verificación por WhatsApp.</summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("cards")]
    public async Task<ActionResult<int>> CreateCard(CreatePaymentCardCommand command)
    {
        var invalid = ValidateChallenge("payment.card.add", command.ChallengeId, command.VerificationCode);
        if (invalid is not null)
            return invalid;

        return await mediator.Send(command);
    }

    /// <summary>Actualiza una tarjeta existente. Solo SuperAdmin, con verificación por WhatsApp.</summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("cards/{id}")]
    public async Task<ActionResult> UpdateCard(int id, UpdatePaymentCardCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID de la tarjeta no coincide.");
        }

        var invalid = ValidateChallenge("payment.card.update", command.ChallengeId, command.VerificationCode);
        if (invalid is not null)
            return invalid;

        await mediator.Send(command);
        return NoContent();
    }

    /// <summary>Elimina una tarjeta. Solo SuperAdmin, con verificación por WhatsApp.</summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpDelete("cards/{id}")]
    public async Task<ActionResult> DeleteCard(int id, [FromQuery] string? challengeId, [FromQuery] string? verificationCode)
    {
        var invalid = ValidateChallenge("payment.card.delete", challengeId, verificationCode);
        if (invalid is not null)
            return invalid;

        await mediator.Send(new DeletePaymentCardCommand(id));
        return NoContent();
    }

    /// <summary>Activa o desactiva una tarjeta (permitido incluso si ya fue enviada para pago).</summary>
    [HttpPut("cards/{id}/active")]
    public async Task<ActionResult> SetCardActive(int id, [FromBody] SetCardActiveRequest body)
    {
        await mediator.Send(new SetPaymentCardActiveCommand(id, body.IsActive));
        return NoContent();
    }

    /// <summary>
    /// Valida el código OTP del desafío para el propósito indicado (verificación del SuperAdmin).
    /// Devuelve null si es válido, o un ActionResult de error 403 si no lo es.
    /// </summary>
    private ActionResult? ValidateChallenge(string purpose, string? challengeId, string? code)
    {
        var userId = currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { success = false, message = "Sesión no válida." });

        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "Esta operación requiere verificación por WhatsApp.",
                code = "VERIFICATION_REQUIRED"
            });
        }

        if (!verification.Validate(challengeId, code, purpose, userId))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "El código de verificación es inválido o expiró.",
                code = "VERIFICATION_INVALID"
            });
        }

        return null;
    }
}

public record SetCardActiveRequest(bool IsActive);
