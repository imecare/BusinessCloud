using BusinessCloud.Application.Admin.Commands.AttendContactRequest;
using BusinessCloud.Application.Admin.Commands.AttendMessageRequest;
using BusinessCloud.Application.Admin.Commands.ChangeSuperAdminPhone;
using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Admin.Queries.GetContactRequests;
using BusinessCloud.Application.Admin.Queries.GetMessageRequests;
using BusinessCloud.Application.Admin.Queries.GetPlatformSettings;
using BusinessCloud.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Admin;

/// <summary>
/// Panel de administración: solicitudes (paquetes de mensajes y contacto) y ajustes de plataforma.
/// Requiere el rol global PlatformAdmin.
/// </summary>
[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin")]
public class AdminRequestsController(ISender mediator) : ControllerBase
{
    /// <summary>Solicitudes de paquetes de mensajes (por estado; sin estado = todas).</summary>
    [HttpGet("message-requests")]
    public async Task<IActionResult> GetMessageRequests([FromQuery] string? status)
    {
        var data = await mediator.Send(new GetMessageRequestsQuery(status));
        return Ok(new ApiResponse<IReadOnlyList<MessageRequestDto>> { Success = true, Data = data });
    }

    /// <summary>Marca una solicitud de paquete como atendida o rechazada.</summary>
    [HttpPost("message-requests/{id:int}/attend")]
    public async Task<IActionResult> AttendMessageRequest(int id, [FromQuery] bool reject = false)
    {
        await mediator.Send(new AttendMessageRequestCommand(id, reject));
        return Ok(new ApiResponse<object> { Success = true, Message = reject ? "Solicitud rechazada." : "Solicitud atendida." });
    }

    /// <summary>Solicitudes de contacto desde el login (contratar/reactivar).</summary>
    [HttpGet("contact-requests")]
    public async Task<IActionResult> GetContactRequests([FromQuery] string? status)
    {
        var data = await mediator.Send(new GetContactRequestsQuery(status));
        return Ok(new ApiResponse<IReadOnlyList<ContactRequestDto>> { Success = true, Data = data });
    }

    /// <summary>Marca una solicitud de contacto como atendida.</summary>
    [HttpPost("contact-requests/{id:int}/attend")]
    public async Task<IActionResult> AttendContactRequest(int id)
    {
        await mediator.Send(new AttendContactRequestCommand(id));
        return Ok(new ApiResponse<object> { Success = true, Message = "Solicitud atendida." });
    }

    /// <summary>Ajustes de la plataforma (teléfono del super administrador).</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var data = await mediator.Send(new GetPlatformSettingsQuery());
        return Ok(new ApiResponse<PlatformSettingsDto> { Success = true, Data = data });
    }

    /// <summary>Solicita un código para cambiar el teléfono del super administrador.</summary>
    [HttpPost("settings/phone/request-change")]
    public async Task<IActionResult> RequestPhoneChange()
    {
        var result = await mediator.Send(new RequestSuperAdminPhoneChangeCommand());
        return Ok(new ApiResponse<RequestPhoneChangeResult>
        {
            Success = true,
            Message = result.CodeSent
                ? "Se envió un código de confirmación por WhatsApp al teléfono actual."
                : "No se pudo enviar el código por WhatsApp. Verifica la configuración.",
            Data = result
        });
    }

    /// <summary>Confirma el cambio de teléfono con el código recibido.</summary>
    [HttpPost("settings/phone/confirm-change")]
    public async Task<IActionResult> ConfirmPhoneChange([FromBody] ConfirmPhoneChangeBody body)
    {
        var newPhone = await mediator.Send(
            new ConfirmSuperAdminPhoneChangeCommand(body.ChallengeId, body.Code, body.NewPhone));
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Teléfono actualizado.",
            Data = new { superAdminPhone = newPhone }
        });
    }

    public class ConfirmPhoneChangeBody
    {
        public string ChallengeId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string NewPhone { get; set; } = null!;
    }
}
