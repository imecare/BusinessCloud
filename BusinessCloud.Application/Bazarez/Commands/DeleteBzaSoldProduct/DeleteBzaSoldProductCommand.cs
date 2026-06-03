using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeleteBzaSoldProduct;

/// <summary>
/// Comando para eliminar un producto vendido de un Evento de Venta.
/// </summary>
public record DeleteBzaSoldProductCommand(int Id) : IRequest<bool>;
