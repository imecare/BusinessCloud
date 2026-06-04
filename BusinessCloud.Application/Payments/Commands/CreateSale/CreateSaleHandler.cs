using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;


namespace BusinessCloud.Application.Payments.Commands.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, int>
{
    private readonly IPaymentsDbContext _sqlContext;
    private readonly IMongoContext _mongoContext;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateSaleHandler> _logger;
    private const string IdempotencyPrefix = "sale_idempotency_";

    public CreateSaleHandler(
        IPaymentsDbContext sqlContext,
        IMongoContext mongoContext,
        ICacheService cache,
        ILogger<CreateSaleHandler> logger)
    {
        _sqlContext = sqlContext;
        _mongoContext = mongoContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<int> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[TIMING] CreateSale - Inicio del handler");

        // Verificar idempotencia: si ya existe la clave, retornar el resultado previo
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var cacheKey = $"{IdempotencyPrefix}{request.IdempotencyKey}";
            var cachedResult = await _cache.GetAsync<int?>(cacheKey);
            _logger.LogInformation("[TIMING] CreateSale - Cache check: {Elapsed}ms", sw.ElapsedMilliseconds);
            if (cachedResult.HasValue)
            {
                _logger.LogInformation("Solicitud duplicada detectada. IdempotencyKey: {Key}, SaleId: {SaleId}",
                    request.IdempotencyKey, cachedResult.Value);
                return cachedResult.Value;
            }
        }

        var sale = new Sale
        {
            CustomerId = request.CustomerId,
            SellerId = request.SellerId,
            TotalAmount = request.TotalAmount,
            CostPrice = request.CostPrice,
            CommissionAmount = request.CommissionAmount,
            ProductDescription = request.ProductDescription,
            IsCommissionPaid = false,
            Date = request.Date
        };

        var initialMovement = new Payment
        {
            Amount = request.TotalAmount,
            PaymentDate = request.Date,
            Date = DateTime.UtcNow,
            PaymentTypeId = 1,
            Reference = "Registro inicial de venta"
        };

        sale.Payment = new List<Payment> { initialMovement };
        _logger.LogInformation("[TIMING] CreateSale - Entidades creadas: {Elapsed}ms", sw.ElapsedMilliseconds);

        _sqlContext.Sales.Add(sale);
        _logger.LogInformation("[TIMING] CreateSale - Add al contexto: {Elapsed}ms", sw.ElapsedMilliseconds);

        await _sqlContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[TIMING] CreateSale - SaveChangesAsync completado: {Elapsed}ms, SaleId: {SaleId}", sw.ElapsedMilliseconds, sale.Id);

        // Guardar en cache para idempotencia (5 minutos)
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var cacheKey = $"{IdempotencyPrefix}{request.IdempotencyKey}";
            await _cache.SetAsync(cacheKey, sale.Id, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[TIMING] CreateSale - Cache set: {Elapsed}ms", sw.ElapsedMilliseconds);
        }

        // Fire-and-forget para MongoDB: no bloquea la respuesta al cliente
        _ = Task.Run(async () =>
        {
            try
            {
                await _mongoContext.InsertAuditLogAsync(new
                {
                    Event = "SaleAndInitialMovementCreated",
                    SaleId = sale.Id,
                    TenantId = sale.TenantId,
                    Details = new
                    {
                        request.TotalAmount,
                        request.ProductDescription,
                        InitialPayment = initialMovement.Amount
                    },
                    CreatedAt = DateTime.UtcNow
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al insertar audit log en MongoDB para SaleId: {SaleId}. El log se puede recuperar de SQL.", sale.Id);
            }
        });

        _logger.LogInformation("[TIMING] CreateSale - Handler completado: {Elapsed}ms", sw.ElapsedMilliseconds);
        return sale.Id;
    }
}
