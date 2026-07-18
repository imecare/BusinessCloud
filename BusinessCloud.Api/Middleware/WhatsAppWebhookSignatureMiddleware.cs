using System.Security.Cryptography;
using System.Text;

namespace BusinessCloud.Api.Middleware;

public class WhatsAppWebhookSignatureMiddleware
{
    private static readonly PathString WebhookPath = new("/api/whatsapp/webhook");

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppWebhookSignatureMiddleware> _logger;

    public WhatsAppWebhookSignatureMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<WhatsAppWebhookSignatureMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Path.Equals(WebhookPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var appSecret = _configuration["WhatsApp:AppSecret"];
        var providedSignature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(appSecret) || string.IsNullOrWhiteSpace(providedSignature))
        {
            _logger.LogWarning("Webhook de WhatsApp rechazado por firma faltante o AppSecret no configurado. IP={Ip}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Firma inválida.");
            return;
        }

        context.Request.EnableBuffering();
        using var buffer = new MemoryStream();
        await context.Request.Body.CopyToAsync(buffer, context.RequestAborted);
        context.Request.Body.Position = 0;

        var expectedSignature = "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), buffer.ToArray()));

        if (!FixedTimeEquals(providedSignature.Trim(), expectedSignature))
        {
            _logger.LogWarning("Webhook de WhatsApp rechazado por firma inválida. IP={Ip}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Firma inválida.");
            return;
        }

        await _next(context);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.ASCII.GetBytes(left);
        var rightBytes = Encoding.ASCII.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}