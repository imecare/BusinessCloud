using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddManualValidationAndReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerReference",
                table: "Bza_ClosureCustomerTotals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProofUploadedByBazar",
                table: "Bza_ClosureCustomerTotals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ValidatedWithoutProof",
                table: "Bza_ClosureCustomerTotals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ValidationNote",
                table: "Bza_ClosureCustomerTotals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerReference",
                table: "Bza_ClosureCustomerTotals");

            migrationBuilder.DropColumn(
                name: "ProofUploadedByBazar",
                table: "Bza_ClosureCustomerTotals");

            migrationBuilder.DropColumn(
                name: "ValidatedWithoutProof",
                table: "Bza_ClosureCustomerTotals");

            migrationBuilder.DropColumn(
                name: "ValidationNote",
                table: "Bza_ClosureCustomerTotals");
        }
    }
}
