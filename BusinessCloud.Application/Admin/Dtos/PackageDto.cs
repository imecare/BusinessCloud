namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>Paquete del catálogo (por sistema).</summary>
public class PackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Module { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MXN";
    public int IncludedMessages { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

/// <summary>Compra de paquete / mensajes de una empresa.</summary>
public class PackagePurchaseDto
{
    public int Id { get; set; }
    public string TenantId { get; set; } = null!;
    public int? PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public int MessagesAdded { get; set; }
    public decimal Price { get; set; }
    public DateTime PurchasedAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>Saldo de mensajes de WhatsApp de una empresa.</summary>
public class MessageBalanceDto
{
    public string TenantId { get; set; } = null!;
    public int Available { get; set; }
    public int TotalPurchased { get; set; }
    public int TotalUsed { get; set; }
}
