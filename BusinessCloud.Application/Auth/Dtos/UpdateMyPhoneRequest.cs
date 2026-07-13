namespace BusinessCloud.Application.Auth.Dtos;

/// <summary>
/// Solicitud del usuario autenticado para configurar su propio número de WhatsApp
/// (usado para recibir códigos de verificación). Formato internacional con código de país.
/// </summary>
public class UpdateMyPhoneRequest
{
    public string? PhoneNumber { get; set; }
}
