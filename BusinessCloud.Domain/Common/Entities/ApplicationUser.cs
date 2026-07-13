using Microsoft.AspNetCore.Identity;

namespace BusinessCloud.Domain.Common.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string TenantId { get; set; } = null!; // El claim que viajará en el JWT
        public string Role { get; set; } = "SuperAdmin"; // SuperAdmin | Commissionist | BazarUser
        public int? SellerId { get; set; } // Vinculación con Seller (solo para Commissionist)
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indica que el usuario debe cambiar su contraseña en el próximo inicio de sesión
        /// (contraseña temporal asignada por el SuperAdmin).
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>
        /// Fecha (UTC) en la que el usuario cambió su contraseña por última vez.
        /// Queda registrado que ya realizó el cambio de la contraseña temporal.
        /// </summary>
        public DateTime? PasswordChangedAt { get; set; }

        /// <summary>
        /// Módulos/secciones del bazar que el usuario puede ver, separados por coma.
        /// Solo aplica a usuarios de rol "BazarUser". Un SuperAdmin ve todo.
        /// </summary>
        public string? AllowedModules { get; set; }

        /// <summary>
        /// Permiso para visualizar los totales de venta (montos agregados).
        /// Si es false, se ocultan los totales para proteger la información de ventas.
        /// </summary>
        public bool CanViewTotals { get; set; } = true;
    }
}
