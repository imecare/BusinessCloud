using System.Text;
using System.Text.Json;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Infrastructure.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessCloud.Infrastructure.Common.Services;

/// <summary>
/// Envío de mensajes por WhatsApp usando la Cloud API de Meta (Graph API).
/// </summary>
public class WhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppSender> _logger;

    public WhatsAppSender(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<WhatsAppSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public Task<bool> SendOtpAsync(string toPhone, string code, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.OtpTemplateName))
        {
            return SendTemplateOtpAsync(toPhone, code, cancellationToken)
                .ContinueWith(t => t.Result.Success, cancellationToken);
        }

        var message =
            $"Tu código de verificación de Bazar-Enlace es: {code}\n" +
            "Vence en 10 minutos. No lo compartas con nadie.";
        return SendTextAsync(toPhone, message, cancellationToken);
    }

    public async Task<WhatsAppSendResult> SendOtpWithResultAsync(string toPhone, string code, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.OtpTemplateName))
        {
            return await SendTemplateOtpAsync(toPhone, code, cancellationToken);
        }

        var to = NormalizePhone(toPhone, _options.DefaultCountryCode);
        if (!IsConfigured || string.IsNullOrWhiteSpace(to))
            return new WhatsAppSendResult(false, null, null, "WhatsApp no configurado o número inválido.");

        var message =
            $"Tu código de verificación de Bazar-Enlace es: {code}\n" +
            "Vence en 10 minutos. No lo compartas con nadie.";
        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = message },
        };
        return await PostAsync(payload, cancellationToken);
    }

    public async Task<bool> SendTextAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        var to = NormalizePhone(toPhone, _options.DefaultCountryCode);
        if (!IsConfigured || string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("WhatsApp no configurado o número inválido. No se envió el mensaje.");
            return false;
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = message },
        };

        var result = await PostAsync(payload, cancellationToken);
        return result.Success;
    }

    public async Task<WhatsAppSendResult> SendTextWithResultAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        var to = NormalizePhone(toPhone, _options.DefaultCountryCode);
        if (!IsConfigured || string.IsNullOrWhiteSpace(to))
            return new WhatsAppSendResult(false, null, null, "WhatsApp no configurado o número inválido.");

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = message },
        };

        return await PostAsync(payload, cancellationToken);
    }

    private async Task<WhatsAppSendResult> SendTemplateOtpAsync(string toPhone, string code, CancellationToken cancellationToken)
    {
        var to = NormalizePhone(toPhone, _options.DefaultCountryCode);
        if (!IsConfigured || string.IsNullOrWhiteSpace(to))
            return new WhatsAppSendResult(false, null, null, "WhatsApp no configurado o número inválido.");

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new
            {
                name = _options.OtpTemplateName,
                language = new { code = _options.OtpTemplateLang },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[] { new { type = "text", text = code } },
                    },
                },
            },
        };

        return await PostAsync(payload, cancellationToken);
    }

    private async Task<WhatsAppSendResult> PostAsync(object payload, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";
            var json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Extraer el id del mensaje (wamid) de la respuesta: { "messages": [ { "id": "wamid..." } ] }
                string? messageId = null;
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("messages", out var msgs)
                        && msgs.ValueKind == JsonValueKind.Array && msgs.GetArrayLength() > 0
                        && msgs[0].TryGetProperty("id", out var idProp))
                    {
                        messageId = idProp.GetString();
                    }
                }
                catch { /* respuesta sin el formato esperado */ }

                return new WhatsAppSendResult(true, messageId, null, null);
            }

            _logger.LogError("Error al enviar WhatsApp ({Status}): {Body}", (int)response.StatusCode, body);

            // Extraer detalle del error de Meta: { "error": { "code": 131026, "message": "..." } }
            string? errorCode = null, errorMessage = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var err))
                {
                    if (err.TryGetProperty("code", out var codeProp))
                        errorCode = codeProp.ToString();
                    if (err.TryGetProperty("message", out var msgProp))
                        errorMessage = msgProp.GetString();
                }
            }
            catch { /* ignore */ }

            return new WhatsAppSendResult(false, null, errorCode, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar mensaje de WhatsApp.");
            return new WhatsAppSendResult(false, null, null, ex.Message);
        }
    }

    /// <summary>
    /// Deja solo dígitos (formato requerido por la Cloud API, sin '+') y antepone el
    /// código de país por defecto cuando el número llega sin él (10 dígitos nacionales).
    /// </summary>
    private static string NormalizePhone(string phone, string defaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return string.Empty;

        var cc = new string((defaultCountryCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(cc) && digits.Length == 10 && !digits.StartsWith(cc))
            digits = cc + digits;

        return digits;
    }
}
