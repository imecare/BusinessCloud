using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessCloud.Application.Payments.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentsDbContext _context;
        private readonly ILogger<PaymentService> _logger; // Inyectamos el logger

        public PaymentService(PaymentsDbContext context, ILogger<PaymentService> logger, IValidator<CreateSaleRequest> validator)
        {
            _context = context;
        }

        public async Task<PaymentResponse> RegisterPaymentAsync(RegisterPaymentRequest request)
        {
            // LOG ESTRUCTURADO: No usamos $, usamos {Propiedad}
            _logger.LogInformation("Procesando abono para Venta: {SaleId}. Monto: {Amount}",
                request.SaleId, request.Amount);

            try
            {
                // 1. Obtener la venta con sus pagos actuales
                var sale = await _context.Sales
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == request.SaleId);

            if (sale == null) throw new KeyNotFoundException("Venta no encontrada.");

            // 2. Calcular saldo actual
            decimal currentBalance = sale.TotalAmount - sale.Payments.Sum(p => p.Amount);

            // 3. Validación de Negocio
            if (request.Amount > currentBalance)
                throw new InvalidOperationException($"El abono (${request.Amount}) no puede ser mayor al saldo (${currentBalance}).");

            // 4. Registrar el abono
            var payment = new Payment
            {
                SaleId = request.SaleId,
                Amount = request.Amount,
                Date = DateTime.UtcNow,
                PaymentMethod = request.PaymentMethod ?? "Cash"
            };

            _context.Payments.Add(payment);

            // 5. ¿Se liquidó la venta?
            if (currentBalance - request.Amount <= 0)
            {
                sale.IsPaid = true;
            }

            await _context.SaveChangesAsync();

            return new PaymentResponse
            {
                PaymentId = payment.Id,
                NewBalance = currentBalance - request.Amount
            };
            }
            catch (Exception ex)
            {
                // Captura el error completo incluyendo el StackTrace
                _logger.LogError(ex, "Error fatal al registrar abono para la Venta: {SaleId}", request.SaleId);
                throw;
            }
        }
    }
}