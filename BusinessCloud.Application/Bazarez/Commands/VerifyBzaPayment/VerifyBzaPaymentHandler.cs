using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.VerifyBzaPayment;

public class VerifyBzaPaymentHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<VerifyBzaPaymentCommand, VerifyBzaPaymentResult>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<VerifyBzaPaymentResult> Handle(VerifyBzaPaymentCommand request, CancellationToken ct)
    {
        var payment = await _context.Payments
            .Include(p => p.Sale)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);

        if (payment is null)
            return new VerifyBzaPaymentResult(false, "Pago no encontrado.", "");

        if (payment.PaymentStatus != 1)
            return new VerifyBzaPaymentResult(false, "Este pago ya fue verificado.", "");

        payment.PaymentStatus = request.Approved ? 2 : 3; // 2=Aprobado, 3=Rechazado
        payment.IsVerified = request.Approved;
        payment.VerificationNotes = request.Notes;
        payment.VerifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Calcular nuevo estado del cliente en este evento
        string newCustomerStatus = "Pendiente";
        if (request.Approved)
        {
            var customerProductsTotal = await _context.SoldProducts
                .Where(p => p.BzaSaleId == payment.BzaSaleId && p.BzaCustomerId == payment.BzaCustomerId)
                .SumAsync(p => p.Price, ct);

            var customerPaidAmount = await _context.Payments
                .Where(p => p.BzaSaleId == payment.BzaSaleId && p.BzaCustomerId == payment.BzaCustomerId && p.IsVerified)
                .SumAsync(p => p.Amount, ct);

            newCustomerStatus = customerPaidAmount >= customerProductsTotal ? "Pagado" : "Pendiente";
        }

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = request.Approved ? "Bza_PaymentApproved" : "Bza_PaymentRejected",
            PaymentId = payment.Id,
            SaleEventId = payment.BzaSaleId,
            CustomerId = payment.BzaCustomerId,
            CustomerName = payment.Customer.Name,
            Amount = payment.Amount,
            Notes = request.Notes,
            NewCustomerStatus = newCustomerStatus,
            Timestamp = DateTime.UtcNow
        }, ct);

        var message = request.Approved ? "Pago aprobado correctamente." : "Pago rechazado.";
        return new VerifyBzaPaymentResult(true, message, newCustomerStatus);
    }
}
