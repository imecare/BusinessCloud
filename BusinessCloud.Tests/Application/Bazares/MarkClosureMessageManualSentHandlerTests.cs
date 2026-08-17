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

    [Fact]
    public async Task Handle_FailedMessage_CanBeMarkedManualSent()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente Fallido",
            Phone = "0000000002",
            FacebookName = "https://facebook.com/cliente.fallido",
        };
        var total = new BzaClosureCustomerTotal
        {
            Id = 11,
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            Customer = customer,
            UploadToken = "failed-upload-token",
        };
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 21,
            TenantId = Tenant,
            Description = "Cierre fallido",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CustomerTotals = [total],
        });
        context.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            TenantId = Tenant,
            Purpose = "totals",
            BzaCustomerId = customer.Id,
            BzaClosureCustomerTotalId = total.Id,
            Status = "failed",
            SentAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync(default);

        var result = await new MarkClosureMessageManualSentHandler(context)
            .Handle(new MarkClosureMessageManualSentCommand(total.Id), default);
        var report = await new GetClosureWhatsAppStatusHandler(context)
            .Handle(new GetClosureWhatsAppStatusQuery(21), default);
        var item = Assert.Single(report.Items);

        Assert.Equal("manual_sent", result.DeliveryStatus);
        Assert.Equal(0, report.Failed);
        Assert.Equal(1, report.ManualSent);
        Assert.Equal("manual_sent", item.DeliveryStatus);
    }

    [Fact]
    public async Task Handle_UnconfirmedMessage_CanBeMarkedManualSent()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente Sin Acuse",
            Phone = "0000000003",
        };
        var total = new BzaClosureCustomerTotal
        {
            Id = 12,
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            Customer = customer,
            UploadToken = "unconfirmed-upload-token",
        };
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 22,
            TenantId = Tenant,
            Description = "Cierre sin acuse",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CustomerTotals = [total],
        });
        // Aceptado por Meta hace >15 min sin acuse => "sin confirmación de Meta".
        context.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            TenantId = Tenant,
            Purpose = "totals",
            BzaCustomerId = customer.Id,
            BzaClosureCustomerTotalId = total.Id,
            Status = "sent",
            SentAt = DateTime.UtcNow.AddMinutes(-20),
        });
        await context.SaveChangesAsync(default);

        var result = await new MarkClosureMessageManualSentHandler(context)
            .Handle(new MarkClosureMessageManualSentCommand(total.Id), default);

        Assert.Equal("manual_sent", result.DeliveryStatus);
    }

    [Fact]
    public async Task Handle_RecentlySentMessage_CannotBeMarkedManualSent()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Cliente Reciente", Phone = "0000000004" };
        var total = new BzaClosureCustomerTotal
        {
            Id = 13,
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            Customer = customer,
            UploadToken = "recent-upload-token",
        };
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 23,
            TenantId = Tenant,
            Description = "Cierre reciente",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CustomerTotals = [total],
        });
        // Enviado hace poco (aún en proceso): NO debe poder marcarse manual.
        context.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            TenantId = Tenant,
            Purpose = "totals",
            BzaCustomerId = customer.Id,
            BzaClosureCustomerTotalId = total.Id,
            Status = "sent",
            SentAt = DateTime.UtcNow.AddMinutes(-2),
        });
        await context.SaveChangesAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MarkClosureMessageManualSentHandler(context)
                .Handle(new MarkClosureMessageManualSentCommand(total.Id), default));
    }
}
