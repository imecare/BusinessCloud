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

        /// <summary>
        /// true = paquete "extra" de transacciones (recarga puntual). Solo se ofrece cuando a la
        /// empresa le quedan pocas transacciones. false = paquete mensual del plan normal.
        /// </summary>
        public bool IsExtra { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
