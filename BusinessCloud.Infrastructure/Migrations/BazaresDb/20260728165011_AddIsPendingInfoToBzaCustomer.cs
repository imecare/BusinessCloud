using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddIsPendingInfoToBzaCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPendingInfo",
                table: "Bza_Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPendingInfo",
                table: "Bza_Customers");
        }
    }
}
