using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateSale;

/// <summary>Registra una venta a credito o una venta de contado con su cobro completo.</summary>
public sealed record CreateSaleCommand(
    int CustomerId,
    int? SellerId,
    decimal TotalAmount,
    decimal CostPrice,
    decimal CommissionAmount,
    string ProductDescription,
    DateTime Date, // Fecha de la venta enviada desde el front
    string? IdempotencyKey = null,
    bool IsCashSale = false,
    string? PaymentMethod = null
) : IRequest<int>;