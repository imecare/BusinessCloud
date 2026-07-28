using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;

public record CreateBzaCustomerCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? FacebookName { get; init; }
    public string Phone { get; init; } = string.Empty;
    public int BzaCollectorId { get; init; }

    /// <summary>Desafío OTP (SuperAdmin) para forzar el alta de un cliente bloqueado. Alternativa al PIN.</summary>
    public string? ChallengeId { get; init; }
    public string? VerificationCode { get; init; }

    /// <summary>PIN de seguridad del SuperAdmin. Alternativa al código OTP para forzar el alta.</summary>
    public string? AdminPin { get; init; }
}