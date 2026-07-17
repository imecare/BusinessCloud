namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>
    /// Paquete vendible por sistema. En Bazares incluye una cantidad de mensajes de WhatsApp
    /// que se suman (acumulables) al saldo de la empresa al contratarlo.
    /// </summary>
    public class Package
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        /// <summary>Sistema/módulo al que aplica (p. ej. "Bazares").</summary>
        public string Module { get; set; } = SystemModules.Bazares;

        public decimal Price { get; set; }
        public string Currency { get; set; } = "MXN";

        /// <summary>Mensajes de WhatsApp incluidos en el paquete.</summary>
        public int IncludedMessages { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
