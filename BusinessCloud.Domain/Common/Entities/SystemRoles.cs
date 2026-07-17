namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>
    /// Roles de usuario del sistema.
    /// </summary>
    public static class SystemRoles
    {
        /// <summary>Administrador de una empresa (por tenant).</summary>
        public const string SuperAdmin = "SuperAdmin";

        /// <summary>Comisionista/vendedor vinculado a un Seller.</summary>
        public const string Commissionist = "Commissionist";

        /// <summary>Usuario operativo del bazar.</summary>
        public const string BazarUser = "BazarUser";

        /// <summary>
        /// Administrador global del SaaS (cross-tenant). No pertenece a ninguna empresa;
        /// gestiona empresas, suscripciones, comisiones y paquetes desde el panel admin.
        /// </summary>
        public const string PlatformAdmin = "PlatformAdmin";
    }

    /// <summary>
    /// Módulo lógico usado por el panel de administración (no es un TenantModule real,
    /// se emplea para distinguir el login del panel admin).
    /// </summary>
    public static class AdminModule
    {
        public const string Name = "Admin";
    }
}
