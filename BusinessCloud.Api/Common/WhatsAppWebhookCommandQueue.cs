using System.Threading.Channels;
using BusinessCloud.Application.Bazares.Commands.ProcessWhatsAppWebhook;
using MediatR;

namespace BusinessCloud.Api.Common;

public interface IWhatsAppWebhookCommandQueue
{
    ValueTask EnqueueAsync(ProcessWhatsAppWebhookCommand command, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ProcessWhatsAppWebhookCommand> ReadAllAsync(CancellationToken cancellationToken);
}

public class WhatsAppWebhookCommandQueue : IWhatsAppWebhookCommandQueue
{
    private readonly Channel<ProcessWhatsAppWebhookCommand> _channel = Channel.CreateUnbounded<ProcessWhatsAppWebhookCommand>();

    public ValueTask EnqueueAsync(ProcessWhatsAppWebhookCommand command, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(command, cancellationToken);

    public IAsyncEnumerable<ProcessWhatsAppWebhookCommand> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}

public class WhatsAppWebhookBackgroundService(
    IWhatsAppWebhookCommandQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<WhatsAppWebhookBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var command in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
                await mediator.Send(command, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando evento en segundo plano del webhook de WhatsApp.");
            }
        }
    }
}