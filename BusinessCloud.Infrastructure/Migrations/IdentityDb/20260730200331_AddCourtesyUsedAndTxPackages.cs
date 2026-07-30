using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.IdentityDb
{
    /// <inheritdoc />
    public partial class AddCourtesyUsedAndTxPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourtesyUsed",
                table: "TenantMessageBalances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Paquetes de transacciones (envío de totales) disponibles en la modal de contratación.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Packages] WHERE [Module] = 'Bazares' AND [IncludedMessages] = 280)
INSERT INTO [Packages] ([Name], [Module], [Price], [Currency], [IncludedMessages], [IsActive], [Description], [CreatedAt])
VALUES (N'280 transacciones', N'Bazares', 100.00, N'MXN', 280, 1, N'280 transacciones (envío de ventas) por $100.', SYSUTCDATETIME());
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Packages] WHERE [Module] = 'Bazares' AND [IncludedMessages] = 600)
INSERT INTO [Packages] ([Name], [Module], [Price], [Currency], [IncludedMessages], [IsActive], [Description], [CreatedAt])
VALUES (N'600 transacciones', N'Bazares', 200.00, N'MXN', 600, 1, N'600 transacciones (envío de ventas) por $200.', SYSUTCDATETIME());
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Packages] WHERE [Module] = 'Bazares' AND [IncludedMessages] IN (280, 600);");

            migrationBuilder.DropColumn(
                name: "CourtesyUsed",
                table: "TenantMessageBalances");
        }
    }
}
