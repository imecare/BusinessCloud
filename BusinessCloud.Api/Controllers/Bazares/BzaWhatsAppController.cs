using BusinessCloud.Application.Bazares.Queries.GetWhatsAppMessages;
using BusinessCloud.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

/// <summary>Consulta de estatus de mensajes de WhatsApp (entrega/errores) del bazar.</summary>
[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaWhatsAppController(ISender mediator) : ControllerBase
{
    /// <summary>Lista los mensajes de WhatsApp enviados y su estatus de entrega.</summary>
    [HttpGet("messages")]
    public async Task<ActionResult<List<WhatsAppMessageDto>>> GetMessages([FromQuery] string? purpose = null, [FromQuery] int take = 200)
        => await mediator.Send(new GetWhatsAppMessagesQuery(purpose, take));
}
