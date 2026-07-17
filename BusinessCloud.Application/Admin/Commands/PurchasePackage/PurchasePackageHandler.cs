using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.PurchasePackage;

public class PurchasePackageHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<PurchasePackageCommand, PurchasePackageResult>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<PurchasePackageResult> Handle(
        PurchasePackageCommand request,
        CancellationToken cancellationToken)
    {
        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);
        if (!tenantExists)
            throw new KeyNotFoundException($"La empresa '{request.TenantId}' no existe.");

        int messages;
        decimal price;
        string packageName;

        if (request.PackageId.HasValue)
        {
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.Id == request.PackageId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"El paquete {request.PackageId} no existe.");

            messages = package.IncludedMessages;
            price = package.Price;
            packageName = package.Name;
        }
        else
        {
            messages = request.CustomMessages ?? 0;
            price = request.CustomPrice ?? 0m;
            packageName = "Mensajes adicionales";
        }

        if (messages <= 0)
            throw new ArgumentException("La compra debe agregar al menos un mensaje.");

        // Acumula el saldo (los mensajes restantes se conservan).
        var balance = await _context.TenantMessageBalances
            .FirstOrDefaultAsync(b => b.TenantId == request.TenantId, cancellationToken);

        if (balance is null)
        {
            balance = new TenantMessageBalance { TenantId = request.TenantId };
            _context.TenantMessageBalances.Add(balance);
        }

        balance.Available += messages;
        balance.TotalPurchased += messages;
        balance.UpdatedAt = DateTime.UtcNow;

        _context.PackagePurchases.Add(new PackagePurchase
        {
            TenantId = request.TenantId,
            PackageId = request.PackageId,
            PackageName = packageName,
            MessagesAdded = messages,
            Price = price,
            PurchasedAt = DateTime.UtcNow,
            Note = request.Note?.Trim(),
            CreatedBy = _currentUser.UserId,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new PurchasePackageResult(
            request.TenantId,
            messages,
            balance.Available,
            balance.TotalPurchased,
            balance.TotalUsed);
    }
}
