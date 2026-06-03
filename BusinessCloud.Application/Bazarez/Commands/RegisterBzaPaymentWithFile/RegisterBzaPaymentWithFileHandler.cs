using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.RegisterBzaPaymentWithFile;

public class RegisterBzaPaymentWithFileHandler(
    IBazaresDbContext context,
    IBlobStorageService blobStorage,
    IMongoContext mongoContext)
    : IRequestHandler<RegisterBzaPaymentWithFileCommand, BzaPaymentWithFileResultDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IBlobStorageService _blobStorage = blobStorage;
    private readonly IMongoContext _mongoContext = mongoContext;

    private static readonly Dictionary<int, string> PaymentStatusNames = new()
    {
        { 1, "Preautorizado" },
        { 2, "Aprobado" },
        { 3, "Rechazado" }
    };

    public async Task<BzaPaymentWithFileResultDto> Handle(RegisterBzaPaymentWithFileCommand request, CancellationToken ct)
    {
        // 1. Validar que el Evento de Venta exista
        var saleEvent = await _context.Sales.FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, ct)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        if (saleEvent.Status == 5)
            throw new InvalidOperationException("No se puede registrar pago en un evento cancelado.");

        // 2. Validar que el Cliente exista
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 3. Subir comprobante a BlobStorage si se proporciona
        string? proofUrl = null;
        if (request.ProofFileContent is not null && request.ProofFileContent.Length > 0)
        {
            var fileName = $"payments/{saleEvent.Id}/{customer.Id}/{Guid.NewGuid()}{Path.GetExtension(request.ProofFileName)}";
            using var stream = new MemoryStream(request.ProofFileContent);
            proofUrl = await _blobStorage.UploadAsync(
                "bazares-proofs",
                fileName,
                stream,
                request.ProofContentType ?? "application/octet-stream",
                ct);
        }

        // 4. Crear el pago (Preautorizado)
        var payment = new BzaPayment
        {
            BzaSaleId = request.BzaSaleId,
            BzaCustomerId = request.BzaCustomerId,
            Amount = request.Amount,
            Date = DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod,
            ProofImageUrl = proofUrl,
            Reference = request.Reference,
            PaymentStatus = 1, // Preautorizado - requiere verificación del responsable
            IsVerified = false
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        // 5. Calcular balance del cliente en este evento (solo pagos aprobados)
        var customerProductsTotal = await _context.SoldProducts
            .Where(p => p.BzaSaleId == request.BzaSaleId && p.BzaCustomerId == request.BzaCustomerId)
            .SumAsync(p => p.Price, ct);

        var customerPaidAmount = await _context.Payments
            .Where(p => p.BzaSaleId == request.BzaSaleId && p.BzaCustomerId == request.BzaCustomerId && p.IsVerified)
            .SumAsync(p => p.Amount, ct);

        var pendingBalance = Math.Max(0, customerProductsTotal - customerPaidAmount);

        // 6. Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_PaymentWithProofRegistered",
            SaleEventId = saleEvent.Id,
            SaleEventDescription = saleEvent.Description,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Status = "Preautorizado",
            HasProof = proofUrl is not null,
            Timestamp = DateTime.UtcNow
        }, ct);

        return new BzaPaymentWithFileResultDto
        {
            PaymentId = payment.Id,
            CustomerPendingBalanceInEvent = pendingBalance,
            IsFullyPaid = false, // No se marca como pagado hasta verificación
            ProofImageUrl = proofUrl,
            PaymentStatus = 1,
            PaymentStatusName = PaymentStatusNames[1]
        };
    }
}
