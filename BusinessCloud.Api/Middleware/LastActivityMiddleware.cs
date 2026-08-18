using System.Security.Claims;
using BusinessCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BusinessCloud.Api.Middleware
{
    /// <summary>
    /// Registra la última actividad (UTC) de cada usuario autenticado para saber quién está
    /// usando el sistema. Para no escribir en la base en cada petición, aplica un "throttle":
    /// solo actualiza la marca si pasó al menos <see cref="ThrottleWindow"/> desde la última
    /// escritura de ese usuario (recordada en memoria). La escritura es un UPDATE directo
    /// (ExecuteUpdate) sin cargar la entidad, para que sea muy barato.
    /// </summary>
    public class LastActivityMiddleware
    {
        private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(2);
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public LastActivityMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, IdentityDbContext identity)
        {
            await _next(context);

            // Solo tras una petición autenticada correctamente.
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User?.FindFirstValue("sub");

            if (context.User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(userId))
                return;

            var cacheKey = $"last-activity:{userId}";
            if (_cache.TryGetValue(cacheKey, out _))
                return; // Dentro de la ventana de throttle: no se vuelve a escribir.

            var now = DateTime.UtcNow;
            _cache.Set(cacheKey, now, ThrottleWindow);

            try
            {
                await identity.Users
                    .Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.LastActivityAt, now));
            }
            catch
            {
                // La actividad es informativa; nunca debe romper la petición del usuario.
            }
        }
    }
}
