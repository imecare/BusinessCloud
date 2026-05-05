using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.PaymentsDb
{
    /// <inheritdoc />
    public partial class AddCommissionAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionPaidAt",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommissionPaidByUserId",
                table: "Sales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommissionPaymentNote",
                table: "Sales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SellerId",
                table: "Sales",
                column: "SellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Sellers_SellerId",
                table: "Sales",
                column: "SellerId",
                principalTable: "Sellers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Sellers_SellerId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SellerId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CommissionPaidAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CommissionPaidByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CommissionPaymentNote",
                table: "Sales");
        }
    }
}
