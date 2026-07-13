using BusinessCloud.Application.Bazares.Commands.UploadClosureProof;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.ManualValidateClosureTotal;

/// <summary>
/// Validación manual de un total por parte del bazar (backoffice), sin depender de que
/// el cliente suba su comprobante. Dos modos:
/// - El bazar adjunta el/los comprobante(s) (recibidos por otro medio) y valida.
/// - El bazar valida SIN comprobante (confirmó el pago por otro canal): la nota es obligatoria.
/// En ambos casos queda marcado en el total para trazabilidad.
/// </summary>
public record ManualValidateClosureTotalCommand(
    int ClosureCustomerTotalId,
    IReadOnlyList<ClosureProofFileInput> Files,
    string? Note = null) : IRequest<ManualValidateClosureTotalResultDto>;

public class ManualValidateClosureTotalResultDto
{
    public int ClosureEventId { get; set; }
    public int ClosureCustomerTotalId { get; set; }
    public int TotalStatus { get; set; }
    public int ClosureStatus { get; set; }
    public bool ProofUploadedByBazar { get; set; }
    public bool ValidatedWithoutProof { get; set; }
    public string? ValidationNote { get; set; }
    public List<string> ProofImageUrls { get; set; } = new();
}

public class ManualValidateClosureTotalHandler(IBazaresDbContext context, IBlobStorageService blobStorage)
    : IRequestHandler<ManualValidateClosureTotalCommand, ManualValidateClosureTotalResultDto>
{
    private const string ContainerName = "bazarez";
    private const string DirectoryName = "comprobantes";
    private readonly IBazaresDbContext _context = context;
    private readonly IBlobStorageService _blobStorage = blobStorage;

    public async Task<ManualValidateClosureTotalResultDto> Handle(ManualValidateClosureTotalCommand request, CancellationToken ct)
    {
        var total = await _context.ClosureCustomerTotals
            .Include(t => t.Proofs)
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.Items)
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.CustomerTotals)
            .FirstOrDefaultAsync(t => t.Id == request.ClosureCustomerTotalId, ct)
            ?? throw new KeyNotFoundException("El total del cliente no existe.");

        if (total.Status == BzaClosureCustomerTotalStatus.Validated)
        {
            return Build(total);
        }

        if (total.Status == BzaClosureCustomerTotalStatus.Cancelled)
            throw new InvalidOperationException("La venta está cancelada. Reactívala antes de validar.");

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var tenantId = total.TenantId;
        var now = DateTime.UtcNow;

        // Si el bazar adjunta comprobante(s), se suben y se marca que los subió el bazar.
        string? lastUrl = null;
        if (request.Files is { Count: > 0 })
        {
            foreach (var file in request.Files)
            {
                var extension = GetExtension(file.FileName, file.ContentType);
                var blobName = $"{DirectoryName}/manual-{total.Id}-{Guid.NewGuid():N}{extension}";
                var proofUrl = await _blobStorage.UploadAsync(
                    ContainerName, blobName, file.Content, file.ContentType, ct);

                total.Proofs.Add(new BzaClosureProof
                {
                    TenantId = tenantId,
                    ImageUrl = proofUrl,
                    UploadedAt = now
                });
                lastUrl = proofUrl;
            }

            total.ProofUploadedByBazar = true;
            total.ProofImageUrl = lastUrl ?? total.ProofImageUrl;
            total.ProofUploadedAt = now;
        }

        var hasProof = total.Proofs.Count > 0 || !string.IsNullOrEmpty(total.ProofImageUrl);

        if (!hasProof)
        {
            // Validación sin comprobante: la nota es obligatoria.
            if (note is null)
                throw new InvalidOperationException("Para validar sin comprobante debes capturar una nota obligatoria.");
            total.ValidatedWithoutProof = true;
        }

        if (note is not null)
            total.ValidationNote = note;

        // Aprobar/crear los pagos que cubren el total del cliente en los eventos del cierre.
        var eventIds = total.ClosureEvent.Items.Select(i => i.BzaEventId).ToList();

        var sales = await _context.Sales
            .Include(s => s.Products)
            .Where(s => s.TenantId == tenantId
                        && s.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(s.BzaEventId))
            .ToListAsync(ct);

        // Aprobar pagos preautorizados por comprobante (si el cliente había subido antes).
        var preauth = await _context.Payments
            .Where(p => p.TenantId == tenantId
                        && p.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(p.BzaEventId)
                        && p.PaymentMethod == "Comprobante"
                        && !p.IsVerified)
            .ToListAsync(ct);

        foreach (var payment in preauth)
        {
            payment.IsVerified = true;
            payment.PaymentStatus = 2;
            payment.VerifiedAt = now;
            payment.VerificationNotes = "Validado manualmente por el bazar.";
        }

        var verifiedPayments = await _context.Payments
            .Where(p => p.TenantId == tenantId
                        && p.BzaCustomerId == total.BzaCustomerId
                        && eventIds.Contains(p.BzaEventId)
                        && p.IsVerified)
            .ToListAsync(ct);

        foreach (var sale in sales)
        {
            sale.IsClosed = true;

            var subtotal = sale.Products.Sum(p => p.Price);
            var paid = verifiedPayments.Where(p => p.BzaEventId == sale.BzaEventId).Sum(p => p.Amount)
                       + preauth.Where(p => p.BzaEventId == sale.BzaEventId).Sum(p => p.Amount);
            var pending = Math.Max(0m, subtotal - paid);
            if (pending <= 0m)
                continue;

            _context.Payments.Add(new BzaPayment
            {
                TenantId = tenantId,
                Amount = pending,
                Date = now,
                PaymentMethod = "Comprobante",
                ProofImageUrl = total.ProofImageUrl ?? string.Empty,
                PaymentStatus = 2,
                IsVerified = true,
                VerifiedAt = now,
                VerificationNotes = hasProof
                    ? "Validado manualmente por el bazar (comprobante subido por el bazar)."
                    : "Validado por el bazar sin comprobante.",
                BzaEventId = sale.BzaEventId,
                BzaCustomerId = total.BzaCustomerId
            });
        }

        total.Status = BzaClosureCustomerTotalStatus.Validated;
        total.RejectionReason = null;

        if (total.ClosureEvent.Status == BzaClosureEventStatus.PendingPayment)
            total.ClosureEvent.Status = BzaClosureEventStatus.ProofReceived;

        var allValidated = total.ClosureEvent.CustomerTotals
            .All(t => t.Id == total.Id || t.Status == BzaClosureCustomerTotalStatus.Validated);
        if (allValidated)
            total.ClosureEvent.Status = BzaClosureEventStatus.Validated;

        await _context.SaveChangesAsync(ct);

        return Build(total);
    }

    private static ManualValidateClosureTotalResultDto Build(BzaClosureCustomerTotal total) => new()
    {
        ClosureEventId = total.BzaClosureEventId,
        ClosureCustomerTotalId = total.Id,
        TotalStatus = total.Status,
        ClosureStatus = total.ClosureEvent.Status,
        ProofUploadedByBazar = total.ProofUploadedByBazar,
        ValidatedWithoutProof = total.ValidatedWithoutProof,
        ValidationNote = total.ValidationNote,
        ProofImageUrls = total.Proofs.OrderBy(p => p.UploadedAt).Select(p => p.ImageUrl).ToList()
    };

    private static string GetExtension(string fileName, string contentType)
    {
        var ext = System.IO.Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(ext))
            return ext.ToLowerInvariant();

        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => ".bin"
        };
    }
}
