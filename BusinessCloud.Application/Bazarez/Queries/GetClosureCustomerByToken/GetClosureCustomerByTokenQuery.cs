using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureCustomerByToken;

/// <summary>
/// Consulta PÚBLICA (sin autenticación) del total a pagar de un cliente por su token de subida.
/// </summary>
public record GetClosureCustomerByTokenQuery(string UploadToken) : IRequest<ClosureCustomerPublicDto>;

/// <summary>Tarjeta/cuenta activa mostrada al cliente para depósitos o transferencias.</summary>
public record PublicPaymentCardDto(string CardNumber, string CardHolderName, string? Bank, string? Notes);

/// <summary>Producto comprado por el cliente en este cierre.</summary>
public record PublicProductDto(string Description, decimal Price);

public record OtherPendingAccountDto(string BazarName, string? BazarLogoUrl, string UploadToken);

public class ClosureCustomerPublicDto
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime PaymentDeadline { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool ProofUploaded { get; set; }
    public string? ProofImageUrl { get; set; }
    /// <summary>Todos los comprobantes subidos por el cliente (varios depósitos).</summary>
    public List<ClosureProofDto> Proofs { get; set; } = new();
    public string ClosureDescription { get; set; } = string.Empty;
    /// <summary>Estado del total: 1=Pendiente, 2=ComprobanteRecibido, 3=Validado, 4=Rechazado, 5=Cancelada.</summary>
    public int Status { get; set; }
    /// <summary>Motivo del rechazo (cuando Status = 4), para que el cliente lo consulte.</summary>
    public string? RejectionReason { get; set; }
    /// <summary>Motivo de la cancelación (cuando Status = 5).</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Referencia o aclaración que el cliente agregó previamente.</summary>
    public string? CustomerReference { get; set; }

    /// <summary>Banco del retiro sin tarjeta capturado previamente por el cliente (si aplica).</summary>
    public string? WithdrawalBank { get; set; }

    /// <summary>Método de pago declarado: 0=No especificado, 1=Transferencia, 2=Depósito, 3=Retiro sin tarjeta.</summary>
    public int PaymentMethod { get; set; }
    /// <summary>Nombre del bazar (para saludos y enlaces).</summary>
    public string? BazarName { get; set; }
    /// <summary>URL del logo del bazar.</summary>
    public string? BazarLogoUrl { get; set; }
    /// <summary>Tarjetas/cuentas activas para que el cliente realice el pago.</summary>
    public List<PublicPaymentCardDto> ActiveCards { get; set; } = new();
    /// <summary>Detalle de productos del cliente en este cierre.</summary>
    public List<PublicProductDto> Products { get; set; } = new();
    /// <summary>Indica si el bazar habilitó la opción de retiro sin tarjeta.</summary>
    public bool WithdrawalWithoutCardEnabled { get; set; }
    /// <summary>Mensaje del bazar sobre el retiro sin tarjeta.</summary>
    public string? WithdrawalWithoutCardMessage { get; set; }
    /// <summary>WhatsApp de atención a ventas (para el link directo).</summary>
    public string? SalesWhatsApp { get; set; }
    /// <summary>WhatsApp adicional (solo si el bazar decidió mostrarlo aquí).</summary>
    public string? SecondaryWhatsApp { get; set; }
    /// <summary>Descripción del WhatsApp adicional.</summary>
    public string? SecondaryWhatsAppDescription { get; set; }

    /// <summary>Hora límite de pago general del bazar (HH:mm), usada si el cierre no trae hora propia.</summary>
    public string? PaymentCutoffTime { get; set; }

    /// <summary>Mensaje adicional en el cobro (configurado por el bazar), a mostrar al pie del comprobante.</summary>
    public string? ChargeMessage { get; set; }

    /// <summary>Indica si Web Push esta habilitado para suscripcion desde el navegador.</summary>
    public bool WebPushEnabled { get; set; }

    /// <summary>Llave publica VAPID para registrar la suscripcion Web Push en el navegador.</summary>
    public string? WebPushPublicKey { get; set; }

    /// <summary>Otras cuentas del mismo telefono en bazares distintos con saldo pendiente o rechazado.</summary>
    public List<OtherPendingAccountDto> OtherPendingAccounts { get; set; } = new();
}

public class GetClosureCustomerByTokenHandler(IBazaresDbContext context, IConfiguration configuration)
    : IRequestHandler<GetClosureCustomerByTokenQuery, ClosureCustomerPublicDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;

    public async Task<ClosureCustomerPublicDto> Handle(GetClosureCustomerByTokenQuery request, CancellationToken cancellationToken)
    {
        var total = await _context.ClosureCustomerTotals
            .IgnoreQueryFilters()
            .Include(t => t.Customer)
            .Include(t => t.Proofs)
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.GroupDeliveries)
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(t => t.UploadToken == request.UploadToken, cancellationToken)
            ?? throw new KeyNotFoundException("El enlace no es válido o ha expirado.");

        DateTime? deliveryDate = total.BzaCollectorGroupId.HasValue
            ? total.ClosureEvent.GroupDeliveries
                .FirstOrDefault(g => g.BzaCollectorGroupId == total.BzaCollectorGroupId.Value)?.DeliveryDate
                ?? total.ClosureEvent.OfficialDeliveryDate
            : total.ClosureEvent.OfficialDeliveryDate;

        var phone = new string((total.Customer?.Phone ?? string.Empty).Where(char.IsDigit).ToArray());

        // Configuración del bazar (para WhatsApp, retiro sin tarjeta, nombre).
        var settings = await _context.BazarSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == total.TenantId, cancellationToken);

        // Mensaje adicional de cobro configurado por el bazar.
        var notif = await _context.NotificationSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.TenantId == total.TenantId, cancellationToken);

        // Tarjetas/cuentas activas para que el cliente pague.
        var activeCards = await _context.PaymentCards
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == total.TenantId && c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => new PublicPaymentCardDto(c.CardNumber, c.CardHolderName, c.Bank, c.Notes))
            .ToListAsync(cancellationToken);

        // Detalle de productos comprados por el cliente en los eventos del cierre.
        var eventIds = total.ClosureEvent.Items.Select(i => i.BzaEventId).ToList();
        var products = await _context.Sales
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == total.TenantId
                        && s.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(s.BzaEventId))
            .SelectMany(s => s.Products)
            .OrderBy(p => p.Description)
            .Select(p => new PublicProductDto(p.Description, p.Price))
            .ToListAsync(cancellationToken);

        var otherPendingAccounts = new List<OtherPendingAccountDto>();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var crossTenantTotals = await _context.ClosureCustomerTotals
                .IgnoreQueryFilters()
                .Include(t => t.Customer)
                .Where(t => t.BzaCustomerId != total.BzaCustomerId
                            && t.Customer.Phone == phone
                            && t.TenantId != total.TenantId
                            && (t.Status == Domain.Bazares.Entities.BzaClosureCustomerTotalStatus.Pending
                                || t.Status == Domain.Bazares.Entities.BzaClosureCustomerTotalStatus.Rejected)
                            && t.ClosureEvent.Status != Domain.Bazares.Entities.BzaClosureEventStatus.Cancelled)
                .Select(t => new
                {
                    t.UploadToken,
                    TenantId = t.TenantId
                })
                .ToListAsync(cancellationToken);

            var tenantIds = crossTenantTotals.Select(x => x.TenantId).Distinct().ToList();
            var bazarInfo = await _context.BazarSettings
                .IgnoreQueryFilters()
                .Where(s => tenantIds.Contains(s.TenantId))
                .Select(s => new { s.TenantId, s.BazarName, s.LogoUrl })
                .ToListAsync(cancellationToken);

            var bazarInfoByTenant = bazarInfo.ToDictionary(x => x.TenantId, x => x);

            otherPendingAccounts = crossTenantTotals
                .Select(x =>
                {
                    bazarInfoByTenant.TryGetValue(x.TenantId, out var info);
                    return new OtherPendingAccountDto(
                        info?.BazarName ?? "Bazar",
                        info?.LogoUrl,
                        x.UploadToken);
                })
                .DistinctBy(x => x.UploadToken)
                .ToList();
        }

        return new ClosureCustomerPublicDto
        {
            CustomerName = total.Customer?.Name ?? "Cliente",
            TotalAmount = total.TotalAmount,
            PaymentDeadline = total.ClosureEvent.PaymentDeadline,
            DeliveryDate = deliveryDate,
            ProofUploaded = total.Status == Domain.Bazares.Entities.BzaClosureCustomerTotalStatus.ProofReceived
                            || total.Status == Domain.Bazares.Entities.BzaClosureCustomerTotalStatus.Validated,
            ProofImageUrl = total.ProofImageUrl,
            Proofs = total.Proofs
                .OrderBy(p => p.UploadedAt)
                .Select(p => new ClosureProofDto(p.Id, p.ImageUrl, p.UploadedAt))
                .ToList(),
            ClosureDescription = total.ClosureEvent.Description,
            Status = total.Status,
            RejectionReason = total.RejectionReason,
            CancellationReason = total.CancellationReason,
            CustomerReference = total.CustomerReference,
            WithdrawalBank = total.WithdrawalBank,
            PaymentMethod = total.PaymentMethod,
            BazarName = settings?.BazarName,
            BazarLogoUrl = settings?.LogoUrl,
            ActiveCards = activeCards,
            Products = products,
            WithdrawalWithoutCardEnabled = settings?.WithdrawalWithoutCardEnabled ?? false,
            WithdrawalWithoutCardMessage = settings?.WithdrawalWithoutCardMessage,
            SalesWhatsApp = settings?.SalesWhatsApp,
            SecondaryWhatsApp = settings?.SecondaryWhatsAppShowInProof == true ? settings?.SecondaryWhatsApp : null,
            SecondaryWhatsAppDescription = settings?.SecondaryWhatsAppShowInProof == true ? settings?.SecondaryWhatsAppDescription : null,
            PaymentCutoffTime = settings?.PaymentCutoffTime,
            ChargeMessage = notif?.ChargeMessage,
            WebPushPublicKey = _configuration["WebPush:PublicKey"],
            WebPushEnabled = !string.IsNullOrWhiteSpace(_configuration["WebPush:PublicKey"]),
            OtherPendingAccounts = otherPendingAccounts
        };
    }
}
