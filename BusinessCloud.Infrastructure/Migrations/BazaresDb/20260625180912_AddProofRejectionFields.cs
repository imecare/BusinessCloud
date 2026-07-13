using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddProofRejectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerJustification",
                table: "Bza_ClosureCustomerTotals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Bza_ClosureCustomerTotals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Resubmitted",
                table: "Bza_ClosureCustomerTotals",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerJustification",
                table: "Bza_ClosureCustomerTotals");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Bza_ClosureCustomerTotals");

            migrationBuilder.DropColumn(
                name: "Resubmitted",
                table: "Bza_ClosureCustomerTotals");
        }
    }
}
