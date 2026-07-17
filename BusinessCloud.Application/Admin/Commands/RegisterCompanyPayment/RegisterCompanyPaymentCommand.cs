using MediatR;

namespace BusinessCloud.Application.Admin.Commands.RegisterCompanyPayment;

/// <summary>
/// Registra el pago de una empresa y extiende la fecha pagada (PaidUntil) por la
/// cantidad de periodos indicada. Si la suscripción ya venció, la extensión parte de hoy;
/// si sigue vigente, se acumula a partir de la fecha pagada vigente.
/// </summary>
public record RegisterCompanyPaymentCommand : IRequest<RegisterCompanyPaymentResult>
{
    public string TenantId { get; init; } = null!;

    /// <summary>Cantidad de periodos (según la periodicidad del plan) que cubre el pago.</summary>
    public int Periods { get; init; } = 1;

    /// <summary>Monto pagado (opcional, informativo).</summary>
    public decimal? Amount { get; init; }

    /// <summary>Fecha del pago (por defecto, ahora).</summary>
    public DateTime? PaymentDate { get; init; }

    public string? Notes { get; init; }
}

public record RegisterCompanyPaymentResult(
    string TenantId,
    DateTime PaidUntil,
    DateTime GraceEndsOn,
    string Status);
