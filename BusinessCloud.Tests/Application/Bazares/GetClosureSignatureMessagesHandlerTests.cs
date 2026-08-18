using BusinessCloud.Application.Bazares.Queries.GetClosureSignatureMessages;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class GetClosureSignatureMessagesHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    [Fact]
    public async Task Handle_UsesApplicableProofs_ReflectsManualSent_AndExcludesCancelledTotals()
    {
        using var context = BazaresContextFactory.Create();
        var ana = Customer(1, "Ana", "5215512345678", "ana.perfil");
        var beto = Customer(2, "Beto", "5215587654321", null);
        var cancelled = Customer(3, "Cancelado", "5215500000000", null);
        var sentAt = DateTime.UtcNow.AddMinutes(-5);
        var closure = new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre de agosto",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            Delivered = true,
            CustomerTotals =
            [
                Total(101, ana, 10, BzaClosureCustomerTotalStatus.Validated),
                Total(102, beto, 20, BzaClosureCustomerTotalStatus.Validated),
                Total(103, cancelled, 10, BzaClosureCustomerTotalStatus.Cancelled),
            ],
            DeliveryProofs =
            [
                Proof(201, null, "https://blob.test/general.jpg", 1),
                Proof(202, 10, "https://blob.test/group-10.jpg", 2),
                Proof(203, 20, "https://blob.test/group-20.jpg", 3),
            ],
        };
        context.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = Tenant, BazarName = "Bazar Test" });
        context.ClosureEvents.Add(closure);
        context.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            Id = 301,
            TenantId = Tenant,
            Purpose = "signatures",
            BzaCustomerId = ana.Id,
            BzaClosureCustomerTotalId = 101,
            ToPhone = ana.Phone,
            Status = "manual_sent",
            SentAt = sentAt.AddMinutes(-1),
            StatusUpdatedAt = sentAt,
        });
        await context.SaveChangesAsync(default);

        var result = await new GetClosureSignatureMessagesHandler(context)
            .Handle(new GetClosureSignatureMessagesQuery(closure.Id), default);

        Assert.True(result.Delivered);
        Assert.Equal("Cierre de agosto", result.Description);
        Assert.Equal(2, result.Items.Count);
        var anaItem = Assert.Single(result.Items, item => item.CustomerName == "Ana");
        // Cada cliente recibe TODAS las firmas del cierre (general + de todos los grupos).
        Assert.Equal(3, anaItem.ProofCount);
        Assert.Contains("https://blob.test/general.jpg\nhttps://blob.test/group-10.jpg\nhttps://blob.test/group-20.jpg", anaItem.Message);
        Assert.True(anaItem.Sent);
        Assert.Equal(sentAt, anaItem.SentAt);
        Assert.True(anaItem.HasWhatsApp);
        Assert.True(anaItem.HasMessenger);
        Assert.DoesNotContain(result.Items, item => item.CustomerName == "Cancelado");
    }

    private static BzaCustomer Customer(int id, string name, string phone, string? facebookName) => new()
    {
        Id = id,
        TenantId = Tenant,
        Name = name,
        Phone = phone,
        FacebookName = facebookName,
    };

    private static BzaClosureCustomerTotal Total(int id, BzaCustomer customer, int? groupId, int status) => new()
    {
        Id = id,
        TenantId = Tenant,
        BzaCustomerId = customer.Id,
        Customer = customer,
        BzaCollectorGroupId = groupId,
        UploadToken = $"token-{id}",
        Status = status,
    };

    private static BzaClosureDeliveryProof Proof(int id, int? groupId, string url, int minute) => new()
    {
        Id = id,
        TenantId = Tenant,
        BzaCollectorGroupId = groupId,
        ImageUrl = url,
        UploadedAt = new DateTime(2026, 8, 17, 12, minute, 0, DateTimeKind.Utc),
    };
}
