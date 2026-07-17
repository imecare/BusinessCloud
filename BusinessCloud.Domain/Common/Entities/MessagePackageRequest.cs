namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>Estados de una solicitud (paquete de mensajes o contacto).</summary>
    public static class RequestStatus
    {
        public const string Pending = "Pending";
        public const string Attended = "Attended";
        public const string Rejected = "Rejected";
    }

    /// <summary>
    /// Solicitud de una empresa (bazar) para contratar un paquete de mensajes adicionales.
    /// Se guarda como pendiente y se avisa por WhatsApp al super administrador. No descuenta
    /// del saldo de mensajes de la empresa.
    /// </summary>
    public class MessagePackageRequest
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = null!;
        public string CompanyName { get; set; } = string.Empty;

        public int? PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public int RequestedMessages { get; set; }
        public decimal Price { get; set; }

        public string Status { get; set; } = RequestStatus.Pending;

        public string? RequestedByUserId { get; set; }
        public string? RequestedByName { get; set; }
        public string? Note { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AttendedAt { get; set; }
        public string? AttendedBy { get; set; }
    }

    /// <summary>
    /// Solicitud de contacto desde el login del bazar (contratar o reactivar cuenta).
    /// Se avisa por WhatsApp al super administrador.
    /// </summary>
    public class ContactRequest
    {
        public int Id { get; set; }

        public string Phone { get; set; } = null!;

        /// <summary>"Contract" (contratar) o "Reactivate" (reactivar).</summary>
        public string Type { get; set; } = "Contract";

        public string? Message { get; set; }
        public string Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AttendedAt { get; set; }
        public string? AttendedBy { get; set; }
    }
}
