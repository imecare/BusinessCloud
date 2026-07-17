namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>Solicitud de paquete de mensajes (para el panel admin).</summary>
public class MessageRequestDto
{
    public int Id { get; set; }
    public string TenantId { get; set; } = null!;
    public string CompanyName { get; set; } = string.Empty;
    public int? PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public int RequestedMessages { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = null!;
    public string? RequestedByName { get; set; }
    public string? Note { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? AttendedAt { get; set; }
}

/// <summary>Solicitud de contacto desde el login (contratar/reactivar).</summary>
public class ContactRequestDto
{
    public int Id { get; set; }
    public string Phone { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Message { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? AttendedAt { get; set; }
}

/// <summary>Ajustes de la plataforma.</summary>
public class PlatformSettingsDto
{
    public string SuperAdminPhone { get; set; } = string.Empty;
}
