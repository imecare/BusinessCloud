using MediatR;

namespace BusinessCloud.Application.Payments.Commands.DeleteSale;

public record DeleteSaleCommand(int Id, string? Reason = null) : IRequest<bool>;
