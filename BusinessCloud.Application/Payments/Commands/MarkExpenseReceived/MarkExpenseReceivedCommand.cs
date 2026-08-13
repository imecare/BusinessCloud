using MediatR;

namespace BusinessCloud.Application.Payments.Commands.MarkExpenseReceived;

/// <summary>Marca (o desmarca) una compra como recibida.</summary>
public record MarkExpenseReceivedCommand(int Id, bool Received) : IRequest<bool>;
