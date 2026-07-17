namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>
    /// Configuración global de la plataforma (fila única). Incluye el teléfono del
    /// super administrador del sistema, al que se envían avisos y solicitudes por WhatsApp.
    /// </summary>
    public class PlatformSettings
    {
        public int Id { get; set; } = 1;

        /// <summary>Teléfono (WhatsApp) del super administrador del sistema.</summary>
        public string SuperAdminPhone { get; set; } = "3121232192";

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
