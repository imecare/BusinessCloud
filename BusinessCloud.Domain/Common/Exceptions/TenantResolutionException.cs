namespace BusinessCloud.Domain.Common.Exceptions;

/// <summary>
/// Exception thrown when TenantId cannot be resolved from the request context.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public sealed class TenantResolutionException : Exception
{
    public TenantResolutionException()
        : base("No se pudo resolver el TenantId del contexto de la solicitud. Acceso denegado.")
    {
    }

    public TenantResolutionException(string message)
        : base(message)
    {
    }

    public TenantResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
