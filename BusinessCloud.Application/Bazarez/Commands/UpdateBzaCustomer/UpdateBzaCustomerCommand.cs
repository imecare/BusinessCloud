using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;

public record UpdateBzaCustomerCommand : IRequest
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? FacebookName { get; init; }
    public string Phone { get; init; } = string.Empty;
    public int Status { get; init; } // Para manejar el (1, 0) de tu libreta
    public int BzaCollectorId { get; init; }
}