using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.SaveLiveSaleRow;

public record SaveLiveSaleRowCommand : IRequest<SaveLiveSaleRowResult>
{
    public int? DraftId { get; init; }
    public int BzaEventId { get; init; }
    public int? BzaCustomerId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

public record SaveLiveSaleRowResult
{
    public int? DraftId { get; init; }
    public int? SoldProductId { get; init; }
    public bool Assigned { get; init; }
}
