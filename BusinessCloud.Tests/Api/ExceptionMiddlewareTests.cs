using System.Net;
using System.Text.Json;
using BusinessCloud.Api.Middleware;
using BusinessCloud.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessCloud.Tests.Api;

/// <summary>
/// Pruebas del middleware global de errores: mapeo de excepciones de dominio/aplicación a
/// códigos HTTP y formato del sobre de respuesta (ApiResponse).
/// </summary>
public class ExceptionMiddlewareTests
{
    private static async Task<(int status, string body)> Run(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        using var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;

        var middleware = new ExceptionMiddleware(_ => throw toThrow);
        await middleware.InvokeAsync(context);

        bodyStream.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(bodyStream).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    public static IEnumerable<object[]> Mappings() => new List<object[]>
    {
        new object[] { new TenantResolutionException(), (int)HttpStatusCode.Forbidden },
        new object[] { new UnauthorizedAccessException("no"), (int)HttpStatusCode.Unauthorized },
        new object[] { new ArgumentException("dato inválido"), (int)HttpStatusCode.BadRequest },
        new object[] { new InvalidOperationException("conflicto"), (int)HttpStatusCode.Conflict },
        new object[] { new Exception("boom"), (int)HttpStatusCode.InternalServerError },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public async Task Mapea_ExcepcionAlCodigoHttpCorrecto(Exception ex, int expectedStatus)
    {
        var (status, _) = await Run(ex);
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public async Task Respuesta_UsaSobreConSuccessFalseYMensaje()
    {
        var (_, body) = await Run(new ArgumentException("dato inválido"));

        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("dato inválido", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SaleCollectorInactive_Devuelve409ConDatosParaReactivar()
    {
        var ex = new SaleCollectorInactiveException(
            message: "Recolector inactivo",
            code: "COLLECTOR_INACTIVE",
            collectorId: 7,
            collectorName: "Juan",
            collectorInactive: true,
            groupId: null,
            groupDescription: null,
            groupInactive: false);

        var (status, body) = await Run(ex);

        Assert.Equal((int)HttpStatusCode.Conflict, status);
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("COLLECTOR_INACTIVE", data.GetProperty("code").GetString());
        Assert.Equal(7, data.GetProperty("collectorId").GetInt32());
    }
}
