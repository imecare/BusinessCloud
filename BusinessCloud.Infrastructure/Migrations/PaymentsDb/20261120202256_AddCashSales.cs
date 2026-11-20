using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.PaymentsDb
{
    /// <inheritdoc />
    public partial class AddCashSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Sales] WHERE DATALENGTH([TenantId]) > 900)
                   OR EXISTS (SELECT 1 FROM [Customers] WHERE DATALENGTH([TenantId]) > 900)
                    THROW 51000, 'TenantId supera 450 caracteres. Revise los datos antes de migrar.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Sellers_SellerId",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "Sales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Sales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCashSale",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "Customers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "SellerId",
                table: "Customers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsGeneralPublic",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_IdempotencyKey",
                table: "Sales",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId",
                unique: true,
                filter: "[IsGeneralPublic] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Sellers_SellerId",
                table: "Customers",
                column: "SellerId",
                principalTable: "Sellers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Customers] WHERE [SellerId] IS NULL)
                   OR EXISTS (SELECT 1 FROM [Sales] WHERE [IsCashSale] = 1)
                    THROW 51000, 'No se puede revertir contado mientras existan ventas de contado o clientes sin vendedor.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Sellers_SellerId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_IdempotencyKey",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsCashSale",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsGeneralPublic",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "SellerId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Sellers_SellerId",
                table: "Customers",
                column: "SellerId",
                principalTable: "Sellers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
