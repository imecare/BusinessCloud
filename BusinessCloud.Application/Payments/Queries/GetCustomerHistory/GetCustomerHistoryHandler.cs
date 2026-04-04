using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace BusinessCloud.Application.Payments.Queries.GetCustomerHistory;

public class GetCustomerHistoryHandler : IRequestHandler<GetCustomerHistoryQuery, List<CustomerHistoryDto>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _userService;

    public GetCustomerHistoryHandler(IMongoContext mongoContext, ICacheService cache, ICurrentUserService userService)
    {
        _mongoContext = mongoContext;
        _cache = cache;
        _userService = userService;
    }

    public async Task<List<CustomerHistoryDto>> Handle(GetCustomerHistoryQuery request, CancellationToken cancellationToken)
    {
        // Prioridad 1: El TenantId del usuario logueado (Seguridad Interna)
        // Prioridad 2: El TenantId que viene en el request (Consulta Pública)
        string? effectiveTenantId = _userService.TenantId ?? request.TenantId;

        if (string.IsNullOrEmpty(effectiveTenantId))
        {
            return new List<CustomerHistoryDto>();
        }

        string cacheKey = $"history_{effectiveTenantId}_{request.CustomerPhone}";

        // 1. Intentar obtener de Redis
        var cachedData = await _cache.GetAsync<List<CustomerHistoryDto>>(cacheKey);
        if (cachedData != null) return cachedData;

                // 2. Si no está en Redis, ir a MongoDB a través de la abstracción del contexto
        var data = await _mongoContext.GetCustomerHistoryByPhoneAsync(effectiveTenantId, request.CustomerPhone, cancellationToken);

        // 3. Guardar en Redis para la próxima consulta
        if (data.Any())
        {
            await _cache.SetAsync(cacheKey, data, TimeSpan.FromMinutes(30));
        }

        return data;
    }
}