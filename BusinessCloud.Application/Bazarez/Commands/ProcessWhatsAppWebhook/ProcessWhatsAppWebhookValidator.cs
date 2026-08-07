using FluentValidation;

namespace BusinessCloud.Application.Bazarez.Commands.ProcessWhatsAppWebhook;

public sealed class ProcessWhatsAppWebhookValidator : AbstractValidator<ProcessWhatsAppWebhookCommand>
{
    public ProcessWhatsAppWebhookValidator()
    {
        RuleFor(x => x.Statuses).NotNull();
        RuleFor(x => x.Messages).NotNull();
    }
}