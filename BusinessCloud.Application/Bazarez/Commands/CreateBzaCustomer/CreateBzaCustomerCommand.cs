using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;

public record CreateBzaCustomerCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? FacebookName { get; init; }
    public string Phone { get; init; } = string.Empty;
    public int BzaCollectorId { get; init; }
}