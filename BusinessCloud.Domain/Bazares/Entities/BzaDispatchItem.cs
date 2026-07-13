using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// L�nea de una hoja de despacho: un paquete/venta entregada al recolector.
/// </summary>
public class BzaDispatchItem : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaDispatchSheetId { get; set; }
    public int BzaEventId { get; set; }
    public int PieceCount { get; set; }
    public string? LabelCode { get; set; } // c�digo QR �nico

    public BzaDispatchSheet DispatchSheet { get; set; } = null!;
    public BzaEvent Event { get; set; } = null!;
}
