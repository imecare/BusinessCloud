using BusinessCloud.Application.Bazares.Commands.MarkClosureSignatureManualSent;
using BusinessCloud.Application.Bazares.Queries.GetClosureSignatureMessages;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class MarkClosureSignatureManualSentHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    [Fact]
    public async Task Handle_MarkSentCreatesSignatureRecordAndUnmarkRemovesItFromReport()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente Firma",
            Phone = "5215512345678",
        };
        var total = new BzaClosureCustomerTotal
        {
            Id = 10,
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            Customer = customer,
            UploadToken = "signature-token",
        };
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre firma",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CustomerTotals = [total],
        });
        await context.SaveChangesAsync(default);
        var handler = new MarkClosureSignatureManualSentHandler(context);

        var marked = await handler.Handle(new MarkClosureSignatureManualSentCommand(total.Id, true), default);
        var reportAfterMark = await new GetClosureSignatureMessagesHandler(context)
            .Handle(new GetClosureSignatureMessagesQuery(20), default);
        var persisted = await context.WhatsAppMessages.SingleAsync();

        Assert.True(marked.Sent);
        Assert.NotNull(marked.SentAt);
        Assert.Equal("signatures", persisted.Purpose);
        Assert.Equal("manual_sent", persisted.Status);
        Assert.Equal(Tenant, persisted.TenantId);
        Assert.Equal(customer.Phone, persisted.ToPhone);
        Assert.True(Assert.Single(reportAfterMark.Items).Sent);

        var unmarked = await handler.Handle(new MarkClosureSignatureManualSentCommand(total.Id, false), default);
        var reportAfterUnmark = await new GetClosureSignatureMessagesHandler(context)
            .Handle(new GetClosureSignatureMessagesQuery(20), default);

        Assert.False(unmarked.Sent);
        Assert.Null(unmarked.SentAt);
        Assert.Empty(context.WhatsAppMessages);
        Assert.False(Assert.Single(reportAfterUnmark.Items).Sent);
    }
}
