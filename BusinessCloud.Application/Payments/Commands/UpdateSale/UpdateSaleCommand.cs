using MediatR;

namespace BusinessCloud.Application.Payments.Commands.UpdateSale;

public record UpdateSaleCommand(
    int Id,
    int CustomerId,
    int? SellerId,
    decimal TotalAmount,
    decimal CostPrice,
    decimal CommissionAmount,
    string ProductDescription,
    DateTime? Date
) : IRequest<bool>;
