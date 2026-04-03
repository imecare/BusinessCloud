using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common; // Para ICurrentUserService
using BusinessCloud.Infrastructure.Persistence; // Tu MongoContext
using MediatR;
using MongoDB.Driver;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerHistory;

public class GetCustomerHistoryHandler : IRequestHandler<GetCustomerHistoryQuery, List<CustomerHistoryDto>>
{
    private readonly MongoContext _mongoContext;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _userService;

    public GetCustomerHistoryHandler(MongoContext mongoContext, ICacheService cache, ICurrentUserService userService)
    {
        _mongoContext = mongoContext;
        _cache = cache;
        _userService = userService;
    }

    public async Task<List<CustomerHistoryDto>> Handle(GetCustomerHistoryQuery request, CancellationToken cancellationToken)
    {
        // Prioridad 1: El TenantId del usuario logueado (Seguridad Interna) [cite: 43]
        // Prioridad 2: El TenantId que viene en el request (Consulta Pública) [cite: 51]
        // Cambia la línea del conflicto por esta:
        string? effectiveTenantId = _userService.TenantId.ToString() ?? request.TenantId;

        if (string.IsNullOrEmpty(effectiveTenantId))
        {
            return new List<CustomerHistoryDto>();
        }


        string cacheKey = $"history_{_userService.TenantId}_{request.CustomerPhone}";

        // 1. Intentar obtener de Redis
        var cachedData = await _cache.GetAsync<List<CustomerHistoryDto>>(cacheKey);
        if (cachedData != null) return cachedData;

        // 2. Si no está en Redis, ir a MongoDB
        var collection = _mongoContext.GetCollection<CustomerHistoryDto>("CustomerReadModel");
        var filter = Builders<CustomerHistoryDto>.Filter.And(
            Builders<CustomerHistoryDto>.Filter.Eq("TenantId", _userService.TenantId),
            Builders<CustomerHistoryDto>.Filter.Eq("CustomerPhone", request.CustomerPhone)
        );

        // Obtenemos el historial completo con sus movimientos incrustados
        var data = await collection.Find(filter)
                               .SortByDescending(x => x.Date)
                               .ToListAsync(cancellationToken);

        // 3. Guardar en Redis para la próxima consulta
        if (data.Any())
        {
            await _cache.SetAsync(cacheKey, data, TimeSpan.FromMinutes(30));
        }

        return data;
    }
}