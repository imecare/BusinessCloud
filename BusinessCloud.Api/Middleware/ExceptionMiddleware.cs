using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text.Json;

namespace BusinessCloud.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

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
                // Usamos 'ex' para que Serilog registre el error completo
                Log.Fatal(ex, "Fallo grave durante el arranque de la aplicación");
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, string message, HttpStatusCode status)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            var result = JsonSerializer.Serialize(new
            {
                success = false,
                message = message
            });

            await context.Response.WriteAsync(result);
        }
    }
}
