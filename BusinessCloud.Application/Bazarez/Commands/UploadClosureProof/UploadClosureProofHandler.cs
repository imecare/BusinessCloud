using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UploadClosureProof;

public class UploadClosureProofHandler(IBazaresDbContext context, IBlobStorageService blobStorage)
    : IRequestHandler<UploadClosureProofCommand, UploadClosureProofResultDto>
{
    private const string ContainerName = "bazarez";
    private const string DirectoryName = "comprobantes";
    private readonly IBazaresDbContext _context = context;
    private readonly IBlobStorageService _blobStorage = blobStorage;

    public async Task<UploadClosureProofResultDto> Handle(UploadClosureProofCommand request, CancellationToken cancellationToken)
    {
        // Acceso público por token: ignorar filtros de tenant y resolver manualmente.
        var total = await _context.ClosureCustomerTotals
            .IgnoreQueryFilters()
            .Include(t => t.Proofs)
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(t => t.UploadToken == request.UploadToken, cancellationToken)
            ?? throw new KeyNotFoundException("El enlace no es válido o ha expirado.");

        // Un comprobante ya aprobado no puede modificarse.
        if (total.Status == BzaClosureCustomerTotalStatus.Validated)
            throw new InvalidOperationException("El comprobante ya fue aprobado y no puede modificarse.");

        var wasRejected = total.Status == BzaClosureCustomerTotalStatus.Rejected;
        var tenantId = total.TenantId;
        var eventIds = total.ClosureEvent.Items.Select(i => i.BzaEventId).ToList();

        // Si el envío anterior fue rechazado, los comprobantes previos ya no son válidos:
        // se descartan para empezar de cero con el nuevo conjunto.
        if (wasRejected && total.Proofs.Count > 0)
        {
            _context.ClosureProofs.RemoveRange(total.Proofs);
            total.Proofs.Clear();
        }

        // Subir cada comprobante a Blob Storage y registrarlo (acumulativo).
        var now = DateTime.UtcNow;
        string? lastUrl = null;
        foreach (var file in request.Files)
        {
            var extension = GetExtension(file.FileName, file.ContentType);
            var blobName = $"{DirectoryName}/{request.UploadToken}-{Guid.NewGuid():N}{extension}";
            var proofUrl = await _blobStorage.UploadAsync(
                ContainerName, blobName, file.Content, file.ContentType, cancellationToken);

            total.Proofs.Add(new BzaClosureProof
            {
                TenantId = tenantId, // Pre-asignado: el override de SaveChanges no lo sobrescribe.
                ImageUrl = proofUrl,
                UploadedAt = now
            });
            lastUrl = proofUrl;
        }

        // Conservar la última URL para compatibilidad con vistas que muestran una sola.
        var url = lastUrl ?? total.ProofImageUrl ?? string.Empty;
        total.ProofImageUrl = url;
        total.ProofUploadedAt = now;
        total.Status = BzaClosureCustomerTotalStatus.ProofReceived;

        // Método de pago declarado por el cliente (1=Transferencia, 2=Depósito, 3=Retiro sin tarjeta).
        if (request.PaymentMethod is >= 1 and <= 3)
        {
            total.PaymentMethod = request.PaymentMethod.Value;
        }

        // Referencia o aclaración opcional del cliente (se conserva si no se envía una nueva).
        if (!string.IsNullOrWhiteSpace(request.Reference))
        {
            total.CustomerReference = request.Reference.Trim();
        }

        // Si el comprobante venía de un rechazo, marcar el reenvío y guardar la
        // justificación opcional del cliente (visible para el bazar al reautorizar).
        if (wasRejected)
        {
            total.Resubmitted = true;
            total.CustomerJustification = string.IsNullOrWhiteSpace(request.Justification)
                ? null
                : request.Justification.Trim();
        }

        // El evento de pago pasa a "Comprobante recibido (pendiente de validar)"
        // en cuanto llega el primer comprobante.
        if (total.ClosureEvent.Status == BzaClosureEventStatus.PendingPayment)
        {
            total.ClosureEvent.Status = BzaClosureEventStatus.ProofReceived;
        }

        // Calcular pendiente por evento del cliente y crear pagos preautorizados.
        var sales = await _context.Sales
            .IgnoreQueryFilters()
            .Include(s => s.Products)
            .Where(s => s.TenantId == tenantId
                        && s.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(s.BzaEventId))
            .ToListAsync(cancellationToken);

        var verifiedPayments = await _context.Payments
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId
                        && p.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(p.BzaEventId)
                        && p.IsVerified)
            .ToListAsync(cancellationToken);

        // Eliminar pagos preautorizados (por comprobante, sin verificar) de un envío
        // anterior para evitar duplicados al sustituir o reenviar el comprobante.
        var stalePreauth = await _context.Payments
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId
                        && p.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(p.BzaEventId)
                        && p.PaymentMethod == "Comprobante"
                        && !p.IsVerified)
            .ToListAsync(cancellationToken);

        if (stalePreauth.Count > 0)
            _context.Payments.RemoveRange(stalePreauth);

        foreach (var sale in sales)
        {
            // Cerrar la venta: ya no se podrán agregar más productos.
            sale.IsClosed = true;

            var subtotal = sale.Products.Sum(p => p.Price);
            var paid = verifiedPayments.Where(p => p.BzaEventId == sale.BzaEventId).Sum(p => p.Amount);
            var pending = Math.Max(0m, subtotal - paid);
            if (pending <= 0m)
                continue;

            _context.Payments.Add(new BzaPayment
            {
                TenantId = tenantId, // Pre-asignado: el override de SaveChanges no lo sobrescribe.
                Amount = pending,
                Date = DateTime.UtcNow,
                PaymentMethod = "Comprobante",
                ProofImageUrl = url,
                PaymentStatus = 1, // Preautorizado
                IsVerified = false,
                BzaEventId = sale.BzaEventId,
                BzaCustomerId = total.BzaCustomerId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UploadClosureProofResultDto
        {
            Success = true,
            ProofImageUrl = url,
            ProofImageUrls = total.Proofs
                .OrderBy(p => p.UploadedAt)
                .Select(p => p.ImageUrl)
                .ToList()
        };
    }

    private static string GetExtension(string fileName, string contentType)
    {
        var ext = System.IO.Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(ext))
            return ext.ToLowerInvariant();

        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => ".jpg"
        };
    }
}
