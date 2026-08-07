using BusinessCloud.Application.Bazares.Commands.MarkClosureMessageManualSent;
using BusinessCloud.Application.Bazares.Queries.GetClosureWhatsAppStatus;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class MarkClosureMessageManualSentHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    [Fact]
    public async Task Handle_NoWhatsAppMessage_MarksManualSentAndKeepsItInReport()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente Manual",
            Phone = "0000000001",
            FacebookName = "https://facebook.com/cliente.manual",
            HasNoWhatsApp = true,
        };
        var total = new BzaClosureCustomerTotal
        {
            Id = 10,
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            Customer = customer,
            UploadToken = "manual-upload-token",
        };
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre manual",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CustomerTotals = [total],
        });
        context.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            TenantId = Tenant,
            Purpose = "totals",
            BzaCustomerId = customer.Id,
            BzaClosureCustomerTotalId = total.Id,
            Status = "sin_whatsapp",
            SentAt = DateTime.UtcNow,
        });
        context.CustomerInboxNotifications.Add(new BzaCustomerInboxNotification
        {
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            BzaClosureCustomerTotalId = total.Id,
            Title = "Total",
            Message = "Mensaje para envio manual",
            ActionUrl = "/comprobante/manual-upload-token",
        });
        await context.SaveChangesAsync(default);

        var result = await new MarkClosureMessageManualSentHandler(context)
            .Handle(new MarkClosureMessageManualSentCommand(total.Id), default);
        var report = await new GetClosureWhatsAppStatusHandler(context)
            .Handle(new GetClosureWhatsAppStatusQuery(20), default);
        var item = Assert.Single(report.Items);

        Assert.Equal("manual_sent", result.DeliveryStatus);
        Assert.Equal(1, report.ManualSent);
        Assert.Equal(0, report.NoWhatsApp);
        Assert.Equal("manual_sent", item.DeliveryStatus);
        Assert.Equal(customer.FacebookName, item.FacebookName);
        Assert.Equal("Mensaje para envio manual", item.ManualMessage);
    }
}
