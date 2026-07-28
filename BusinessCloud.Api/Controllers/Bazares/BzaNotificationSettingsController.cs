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
    ICurrentUserService currentUser,
    IAdminPinService adminPin) : ControllerBase
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

    /// <summary>Crea una nueva tarjeta. Solo SuperAdmin, con verificacion por PIN o WhatsApp.</summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("cards")]
    public async Task<ActionResult<int>> CreateCard(CreatePaymentCardCommand command)
    {
        var invalid = await ValidateChallengeAsync("payment.card.add", command.ChallengeId, command.VerificationCode, command.AdminPin);
        if (invalid is not null)
            return invalid;

        return await mediator.Send(command);
    }

    /// <summary>Actualiza una tarjeta existente. Solo SuperAdmin, con verificacion por PIN o WhatsApp.</summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("cards/{id}")]
    public async Task<ActionResult> UpdateCard(int id, UpdatePaymentCardCommand command)
    {
        if (id != command.Id)
            return BadRequest("El ID de la tarjeta no coincide.");

        var invalid = await ValidateChallengeAsync("payment.card.update", command.ChallengeId, command.VerificationCode, command.AdminPin);
        if (invalid is not null)
            return invalid;

        await mediator.Send(command);
        return NoContent();
    }

    /// <summary>Elimina una tarjeta. Solo SuperAdmin, con verificacion por PIN o WhatsApp.</summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpDelete("cards/{id}")]
    public async Task<ActionResult> DeleteCard(int id, [FromQuery] string? challengeId, [FromQuery] string? verificationCode, [FromQuery] string? adminPin)
    {
        var invalid = await ValidateChallengeAsync("payment.card.delete", challengeId, verificationCode, adminPin);
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
    /// Valida PIN o codigo OTP del SuperAdmin segun lo que se proporcione.
    /// Si se envia adminPin, verifica el hash. Si se envia challengeId+code, verifica OTP.
    /// </summary>
    private async Task<ActionResult?> ValidateChallengeAsync(string purpose, string? challengeId, string? code, string? pin)
    {
        var userId = currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { success = false, message = "Sesion no valida." });

        if (!string.IsNullOrWhiteSpace(pin))
        {
            var pinOk = await adminPin.VerifyPinAsync(userId, pin);
            if (!pinOk)
                return StatusCode(403, new { success = false, message = "PIN incorrecto.", code = "PIN_INVALID" });
            return null;
        }

        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "Esta operacion requiere verificacion (PIN o codigo WhatsApp).",
                code = "VERIFICATION_REQUIRED"
            });
        }

        if (!verification.Validate(challengeId, code, purpose, userId))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "El codigo de verificacion es invalido o expiro.",
                code = "VERIFICATION_INVALID"
            });
        }

        return null;
    }
}

public record SetCardActiveRequest(bool IsActive);
