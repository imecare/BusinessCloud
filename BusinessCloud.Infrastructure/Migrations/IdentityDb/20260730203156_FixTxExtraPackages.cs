using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.IdentityDb
{
    /// <inheritdoc />
    public partial class FixTxExtraPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los extras se identifican por nombre (no por IncludedMessages) para evitar
            // colisiones con paquetes mensuales que también incluyen 600 (p. ej. "Básico").

            // 1) Los paquetes extra son solo estos dos por nombre.
            migrationBuilder.Sql(
                "UPDATE [Packages] SET [IsExtra] = 0 WHERE [Module] = 'Bazares' " +
                "AND [Name] NOT IN (N'280 transacciones', N'600 transacciones');");

            // 2) Inserta el paquete de 600 transacciones si aún no existe (antes se omitió por colisión).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Packages] WHERE [Module] = 'Bazares' AND [Name] = N'600 transacciones')
INSERT INTO [Packages] ([Name], [Module], [Price], [Currency], [IncludedMessages], [IsActive], [Description], [CreatedAt], [IsExtra])
VALUES (N'600 transacciones', N'Bazares', 200.00, N'MXN', 600, 1, N'600 transacciones (envío de ventas) por $200.', SYSUTCDATETIME(), 1);
");

            // 3) Asegura que ambos extras queden marcados como IsExtra.
            migrationBuilder.Sql(
                "UPDATE [Packages] SET [IsExtra] = 1 WHERE [Module] = 'Bazares' " +
                "AND [Name] IN (N'280 transacciones', N'600 transacciones');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se revierte: la corrección de datos no debe deshacerse automáticamente.
        }
    }
}