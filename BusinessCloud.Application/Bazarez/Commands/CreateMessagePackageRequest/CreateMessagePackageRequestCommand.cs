using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateMessagePackageRequest;

/// <summary>
/// Un bazar solicita contratar un paquete de mensajes adicionales. Se guarda como pendiente
/// y se avisa por WhatsApp al super administrador. No descuenta del saldo de la empresa.
/// </summary>
public record CreateMessagePackageRequestCommand(int PackageId, string? Note)
    : IRequest<MessagePackageRequestResult>;

public record MessagePackageRequestResult(int RequestId, string PackageName, int RequestedMessages);
