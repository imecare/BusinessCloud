using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddBazarWhatsAppAndWithdrawal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneralWhatsApp",
                table: "Bza_BazarSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesWhatsApp",
                table: "Bza_BazarSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryWhatsApp",
                table: "Bza_BazarSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryWhatsAppDescription",
                table: "Bza_BazarSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SecondaryWhatsAppShowInProof",
                table: "Bza_BazarSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithdrawalWithoutCardEnabled",
                table: "Bza_BazarSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalWithoutCardMessage",
                table: "Bza_BazarSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneralWhatsApp",
                table: "Bza_BazarSettings");

            migrationBuilder.DropColumn(
                name: "SalesWhatsApp",
                table: "Bza_BazarSettings");

            migrationBuilder.DropColumn(
                name: "SecondaryWhatsApp",
                table: "Bza_BazarSettings");

            migrationBuilder.DropColumn(
                name: "SecondaryWhatsAppDescription",
                table: "Bza_BazarSettings");

            migrationBuilder.DropColumn(
                name: "SecondaryWhatsAppShowInProof",
                table: "Bza_BazarSettings");

            migrationBuilder.DropColumn(
                name: "WithdrawalWithoutCardEnabled",
                table: "Bza_BazarSettings");

            migrationBuilder.DropColumn(
                name: "WithdrawalWithoutCardMessage",
                table: "Bza_BazarSettings");
        }
    }
}
