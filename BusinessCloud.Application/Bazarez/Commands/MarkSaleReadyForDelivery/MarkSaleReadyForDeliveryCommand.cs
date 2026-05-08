using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.MarkSaleReadyForDelivery;

public record MarkSaleReadyForDeliveryCommand(int BzaSaleId) : IRequest;
