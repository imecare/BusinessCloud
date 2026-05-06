using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateSale;

// Define los datos necesarios para la venta según la tabla de Sales [cite: 23]
public record CreateSaleCommand(
    int CustomerId,
    int? SellerId,
    decimal TotalAmount,
    decimal CostPrice,
    decimal CommissionAmount,
    string ProductDescription
) : IRequest<int>;