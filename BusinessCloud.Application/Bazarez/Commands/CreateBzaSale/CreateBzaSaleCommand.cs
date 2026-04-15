using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSale;

public record CreateBzaSaleProductDto(string Description, decimal Price);

public record CreateBzaSaleCommand : IRequest<int>
{
    public int BzaCustomerId { get; init; }
    public string? Description { get; init; }
    public List<CreateBzaSaleProductDto> Products { get; init; } = new();
}