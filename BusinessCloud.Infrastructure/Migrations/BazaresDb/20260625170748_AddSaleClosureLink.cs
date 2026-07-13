using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddSaleClosureLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BzaClosureEventId",
                table: "Bza_Sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Sales_BzaClosureEventId",
                table: "Bza_Sales",
                column: "BzaClosureEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Sales_Bza_ClosureEvents_BzaClosureEventId",
                table: "Bza_Sales",
                column: "BzaClosureEventId",
                principalTable: "Bza_ClosureEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Sales_Bza_ClosureEvents_BzaClosureEventId",
                table: "Bza_Sales");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Sales_BzaClosureEventId",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "BzaClosureEventId",
                table: "Bza_Sales");
        }
    }
}
