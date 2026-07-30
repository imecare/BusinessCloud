using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.IdentityDb
{
    /// <inheritdoc />
    public partial class AddPackageIsExtra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExtra",
                table: "Packages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Los paquetes de 280 y 600 transacciones son recargas extra: solo se ofrecen
            // cuando a la empresa le quedan pocas transacciones (banner / modal de bloqueo).
            migrationBuilder.Sql(
                "UPDATE [Packages] SET [IsExtra] = 1 WHERE [Module] = 'Bazares' AND [IncludedMessages] IN (280, 600);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExtra",
                table: "Packages");
        }
    }
}
