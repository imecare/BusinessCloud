using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CreateMessagePackageRequest;

public class CreateMessagePackageRequestHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser,
    IWhatsAppSender whatsApp)
    : IRequestHandler<CreateMessagePackageRequestCommand, MessagePackageRequestResult>
{
    private const string DefaultSuperAdminPhone = "3121232192";

    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IWhatsAppSender _whatsApp = whatsApp;

    public async Task<MessagePackageRequestResult> Handle(
        CreateMessagePackageRequestCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.GetRequiredTenantId();

        var package = await _context.Packages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == request.PackageId && p.IsActive && p.Module == SystemModules.Bazares,
                cancellationToken)
            ?? throw new KeyNotFoundException("El paquete no existe o no está disponible.");

        var companyName = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? tenantId;

        var entity = new MessagePackageRequest
        {
            TenantId = tenantId,
            CompanyName = companyName,
            PackageId = package.Id,
            PackageName = package.Name,
            RequestedMessages = package.IncludedMessages,
            Price = package.Price,
            Status = RequestStatus.Pending,
            RequestedByUserId = _currentUser.UserId,
            RequestedByName = _currentUser.Username,
            Note = request.Note?.Trim(),
            RequestedAt = DateTime.UtcNow,
        };

        _context.MessagePackageRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Aviso al super administrador (no descuenta del saldo de la empresa).
        var phone = await _context.PlatformSettings
            .AsNoTracking()
            .Select(s => s.SuperAdminPhone)
            .FirstOrDefaultAsync(cancellationToken) ?? DefaultSuperAdminPhone;

        var message =
            $"📦 Solicitud de paquete de mensajes\n" +
            $"Empresa: {companyName}\n" +
            $"Paquete: {package.Name} ({package.IncludedMessages} mensajes)\n" +
            $"Precio: {package.Currency} {package.Price:0.00}\n" +
            "Revisa las solicitudes pendientes en el panel de administración.";

        try
        {
            await _whatsApp.SendTextAsync(phone, message, cancellationToken);
        }
        catch
        {
            // El aviso es best-effort; la solicitud ya quedó registrada.
        }

        return new MessagePackageRequestResult(entity.Id, package.Name, package.IncludedMessages);
    }
}
