using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Línea de una hoja de despacho: un paquete/venta entregada al recolector.
/// </summary>
public class BzaDispatchItem : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaDispatchSheetId { get; set; }
    public int BzaSaleId { get; set; }
    public int PieceCount { get; set; }
    public string? LabelCode { get; set; } // código QR único

    public BzaDispatchSheet DispatchSheet { get; set; } = null!;
    public BzaSale Sale { get; set; } = null!;
}
