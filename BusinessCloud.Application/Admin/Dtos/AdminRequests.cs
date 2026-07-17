using BusinessCloud.Domain.Common.Entities;
using System.Text.Json.Serialization;

namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>Datos de la suscripción al crear/actualizar una empresa.</summary>
public class SubscriptionInput
{
    public string PlanName { get; set; } = "Mensual";
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BillingPeriod Period { get; set; } = BillingPeriod.Monthly;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MXN";
    public DateTime PaidUntil { get; set; }
    public int GraceDays { get; set; } = 5;
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public int? SellerId { get; set; }
    public decimal CommissionInitialAmount { get; set; }
    public decimal CommissionMonthlyPercent { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Alta de una empresa: crea el tenant, sus módulos, el usuario SuperAdmin y, opcionalmente, la suscripción.</summary>
public class CreateCompanyRequest
{
    public string CompanyName { get; set; } = null!;
    public string[]? Modules { get; set; }

    public string AdminEmail { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
    public string AdminFirstName { get; set; } = null!;
    public string AdminLastName { get; set; } = null!;

    public SubscriptionInput? Subscription { get; set; }
}

/// <summary>Registro de un pago que extiende la fecha pagada.</summary>
public class RegisterPaymentRequest
{
    public int Periods { get; set; } = 1;
    public decimal? Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Activación/suspensión de una empresa.</summary>
public class SetCompanyStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>Alta/edición de un comisionista del SaaS.</summary>
public class SystemSellerRequest
{
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal DefaultInitialAmount { get; set; }
    public decimal DefaultMonthlyPercent { get; set; }
}

/// <summary>Pago de comisiones de un comisionista.</summary>
public class PayCommissionsRequest
{
    public List<int>? CommissionIds { get; set; }
    public string? Note { get; set; }
}

/// <summary>Alta/edición de un paquete del catálogo.</summary>
public class PackageRequest
{
    public string Name { get; set; } = null!;
    public string Module { get; set; } = "Bazares";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MXN";
    public int IncludedMessages { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>Compra de un paquete o de mensajes adicionales para una empresa.</summary>
public class PurchasePackageRequest
{
    public int? PackageId { get; set; }
    public int? CustomMessages { get; set; }
    public decimal? CustomPrice { get; set; }
    public string? Note { get; set; }
}
