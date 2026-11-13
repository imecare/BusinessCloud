using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Catalogs.Entities;

public enum CatOrderStatus
{
    New = 1,
    Confirmed = 2,
    Cancelled = 3,
}

/// <summary>Pedido enviado por un cliente desde el catálogo público.</summary>
public class CatOrder : BaseAuditableEntity
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? InternalNote { get; set; }
    public CatOrderStatus Status { get; set; } = CatOrderStatus.New;

    /// <summary>Pre-total que vio el cliente (cantidad solicitada x precio).</summary>
    public decimal EstimatedTotal { get; set; }

    /// <summary>Total ajustado por la empresa (cantidad final x precio).</summary>
    public decimal FinalTotal { get; set; }

    public bool HasOutOfStockItems { get; set; }

    /// <summary>La existencia descontada sigue vigente (false si el pedido se canceló y se devolvió).</summary>
    public bool StockDiscounted { get; set; } = true;

    public ICollection<CatOrderItem> Items { get; set; } = new List<CatOrderItem>();
}

public class CatOrderItem : BaseAuditableEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    // Los identificadores de producto/variante son referencias informativas (sin FK):
    // el pedido conserva una copia de nombre/precio aunque el producto cambie o se elimine.
    public int ProductId { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantLabel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }

    public int RequestedQuantity { get; set; }
    public int QuantityDiscounted { get; set; }
    public int FinalQuantity { get; set; }
    public bool OutOfStockAtSubmit { get; set; }
    public string? LineNote { get; set; }

    public CatOrder? Order { get; set; }
}
