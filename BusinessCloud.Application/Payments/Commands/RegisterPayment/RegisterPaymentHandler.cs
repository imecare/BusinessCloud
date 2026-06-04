using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BusinessCloud.Application.Payments.Commands.RegisterPayment;

public class RegisterPaymentHandler : IRequestHandler<RegisterPaymentCommand, PaymentReceiptDto>
{
    private readonly IPaymentsDbContext _sqlContext;
    private readonly IMongoContext _mongoContext;
    private readonly ICacheService _cache;
    private readonly ILogger<RegisterPaymentHandler> _logger;
    private const string IdempotencyPrefix = "payment_idempotency_";

    public RegisterPaymentHandler(
        IPaymentsDbContext sqlContext,
        IMongoContext mongoContext,
        ICacheService cache,
        ILogger<RegisterPaymentHandler> logger)
    {
        _sqlContext = sqlContext;
        _mongoContext = mongoContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PaymentReceiptDto> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[TIMING] RegisterPayment - Inicio del handler");

        // Verificar idempotencia: si ya existe la clave, retornar el resultado previo
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var cacheKey = $"{IdempotencyPrefix}{request.IdempotencyKey}";
            var cachedResult = await _cache.GetAsync<PaymentReceiptDto>(cacheKey);
            _logger.LogInformation("[TIMING] RegisterPayment - Cache check: {Elapsed}ms", sw.ElapsedMilliseconds);
            if (cachedResult != null)
            {
                _logger.LogInformation("Pago duplicado detectado. IdempotencyKey: {Key}, Folio: {Folio}",
                    request.IdempotencyKey, cachedResult.Folio);
                return cachedResult;
            }
        }

        var sale = await _sqlContext.Sales
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken);
        _logger.LogInformation("[TIMING] RegisterPayment - Query Sale: {Elapsed}ms", sw.ElapsedMilliseconds);

        if (sale == null) throw new Exception("Venta no encontrada.");

        var payment = new Payment
        {
            SaleId = request.SaleId,
            Amount = request.Amount,
            PaymentTypeId = 2,
            PaymentDate = request.PaymentDate,
            Date = DateTime.UtcNow,
            Reference = request.Reference
        };

        // Recalcular IsPaid de la venta: sumar abonos existentes + el nuevo
        var previousPaid = await _sqlContext.Payments
            .Where(p => p.SaleId == request.SaleId && p.PaymentTypeId == 2)
            .SumAsync(p => p.Amount, cancellationToken);
        _logger.LogInformation("[TIMING] RegisterPayment - SumAsync Payments: {Elapsed}ms", sw.ElapsedMilliseconds);

        sale.IsPaid = (previousPaid + request.Amount) >= sale.TotalAmount;

        _sqlContext.Payments.Add(payment);
        await _sqlContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[TIMING] RegisterPayment - SaveChangesAsync: {Elapsed}ms", sw.ElapsedMilliseconds);

        // Preparar respuesta inmediata (sin esperar MongoDB)
        var result = new PaymentReceiptDto(
            Folio: $"PAY-{payment.Id}",
            CustomerName: sale.Customer?.Name ?? "Cliente",
            AmountPaid: request.Amount,
            NewBalance: sale.TotalAmount - previousPaid - request.Amount,
            PaymentDate: request.PaymentDate,
            Date: DateTime.UtcNow,
            LastMovements: new List<PaymentLineDto>()
        );

        // Guardar en cache para idempotencia (5 minutos)
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var cacheKey = $"{IdempotencyPrefix}{request.IdempotencyKey}";
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[TIMING] RegisterPayment - Cache set: {Elapsed}ms", sw.ElapsedMilliseconds);
        }

        // Fire-and-forget para MongoDB: no bloquea la respuesta al cliente
        _ = Task.Run(async () =>
        {
            try
            {
                await _mongoContext.InsertAuditLogAsync(new
                {
                    Event = "Payment",
                    PaymentId = payment.Id,
                    SaleId = sale.Id
                }, CancellationToken.None);

                await _mongoContext.UpdateCustomerReadModelAsync(
                    request.SaleId,
                    request.Amount,
                    request.Reference ?? "Abono",
                    CancellationToken.None);

                if (sale.Customer != null)
                {
                    await _cache.RemoveAsync($"history_{sale.TenantId}_{sale.Customer.Phone}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error en operaciones MongoDB para PaymentId: {PaymentId}. El pago está guardado en SQL.", payment.Id);
            }
        });

        _logger.LogInformation("[TIMING] RegisterPayment - Handler completado: {Elapsed}ms", sw.ElapsedMilliseconds);
        return result;
    }
}