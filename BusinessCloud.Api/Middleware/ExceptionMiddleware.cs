using BusinessCloud.Domain.Common.Exceptions;
using BusinessCloud.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text.Json;

namespace BusinessCloud.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (TenantResolutionException ex)
            {
                // Multi-tenant security: TenantId not resolved → 403 Forbidden
                await HandleExceptionAsync(context, ex.Message, HttpStatusCode.Forbidden);
            }
            catch (DbUpdateException ex)
            {
                // Captura fallos de Base de Datos como FK incorrecta.
                await HandleExceptionAsync(context, "Error de base de datos: Conflicto de relación. Es posible que estés intentando usar un registro (ej. SellerId) que no existe. Error que regresa "  + ex.Message, HttpStatusCode.Conflict);
            }
            catch (InvalidOperationException ex)
            {
                await HandleExceptionAsync(context, ex.Message, HttpStatusCode.Conflict);
            }
            catch (ArgumentException ex)
            {
                await HandleExceptionAsync(context, ex.Message, HttpStatusCode.BadRequest);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleExceptionAsync(context, ex.Message, HttpStatusCode.Unauthorized);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error interno no controlado");
                await HandleExceptionAsync(context, "Error interno del servidor: " + ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, string message, HttpStatusCode status)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null
            };

            var result = JsonSerializer.Serialize(response, JsonOptions);
            await context.Response.WriteAsync(result);
        }
    }
}
