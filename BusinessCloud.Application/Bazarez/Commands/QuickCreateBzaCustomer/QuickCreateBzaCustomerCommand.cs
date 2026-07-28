using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.QuickCreateBzaCustomer;

/// <summary>
/// Alta rápida de cliente durante la captura en vivo de una venta: solo requiere el nombre.
/// El cliente queda marcado como "pendiente de completar información" (teléfono y recolector)
/// hasta que se edite con el comando de actualización de cliente.
/// </summary>
public record QuickCreateBzaCustomerCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
}
