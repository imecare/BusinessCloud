using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AlignBzaSaleProductSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Sales_Bza_Customers_BzaCustomerId",
                table: "Bza_Sales");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Sales_BzaCustomerId",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "BzaCustomerId",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "LabelCode",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "PortalToken",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "ProofOfPaymentUrl",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Bza_Collectors");

            migrationBuilder.RenameColumn(
                name: "DeliveredToCollectorAt",
                table: "Bza_Sales",
                newName: "DeliveryDate");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BzaCustomerId",
                table: "Bza_Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BzaCustomerId",
                table: "Bza_Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Bza_Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "Bza_Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Bza_Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Bza_Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BzaCollectorGroupId",
                table: "Bza_Collectors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Bza_Collectors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Bza_CollectorGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_CollectorGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bza_Deliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaCollectorGroupId = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_Deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_Deliveries_Bza_CollectorGroups_BzaCollectorGroupId",
                        column: x => x.BzaCollectorGroupId,
                        principalTable: "Bza_CollectorGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bza_DeliveryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaDeliveryId = table.Column<int>(type: "int", nullable: false),
                    BzaSaleId = table.Column<int>(type: "int", nullable: false),
                    Delivered = table.Column<bool>(type: "bit", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_DeliveryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_DeliveryItems_Bza_Deliveries_BzaDeliveryId",
                        column: x => x.BzaDeliveryId,
                        principalTable: "Bza_Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_DeliveryItems_Bza_Sales_BzaSaleId",
                        column: x => x.BzaSaleId,
                        principalTable: "Bza_Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Products_BzaCustomerId",
                table: "Bza_Products",
                column: "BzaCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Payments_BzaCustomerId",
                table: "Bza_Payments",
                column: "BzaCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Collectors_BzaCollectorGroupId",
                table: "Bza_Collectors",
                column: "BzaCollectorGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Deliveries_BzaCollectorGroupId",
                table: "Bza_Deliveries",
                column: "BzaCollectorGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_DeliveryItems_BzaDeliveryId",
                table: "Bza_DeliveryItems",
                column: "BzaDeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_DeliveryItems_BzaSaleId",
                table: "Bza_DeliveryItems",
                column: "BzaSaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Collectors_Bza_CollectorGroups_BzaCollectorGroupId",
                table: "Bza_Collectors",
                column: "BzaCollectorGroupId",
                principalTable: "Bza_CollectorGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Payments_Bza_Customers_BzaCustomerId",
                table: "Bza_Payments",
                column: "BzaCustomerId",
                principalTable: "Bza_Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Products_Bza_Customers_BzaCustomerId",
                table: "Bza_Products",
                column: "BzaCustomerId",
                principalTable: "Bza_Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Collectors_Bza_CollectorGroups_BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Payments_Bza_Customers_BzaCustomerId",
                table: "Bza_Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Products_Bza_Customers_BzaCustomerId",
                table: "Bza_Products");

            migrationBuilder.DropTable(
                name: "Bza_DeliveryItems");

            migrationBuilder.DropTable(
                name: "Bza_Deliveries");

            migrationBuilder.DropTable(
                name: "Bza_CollectorGroups");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Products_BzaCustomerId",
                table: "Bza_Products");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Payments_BzaCustomerId",
                table: "Bza_Payments");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Collectors_BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropColumn(
                name: "BzaCustomerId",
                table: "Bza_Products");

            migrationBuilder.DropColumn(
                name: "BzaCustomerId",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Bza_Customers");

            migrationBuilder.DropColumn(
                name: "BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Bza_Collectors");

            migrationBuilder.RenameColumn(
                name: "DeliveryDate",
                table: "Bza_Sales",
                newName: "DeliveredToCollectorAt");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BzaCustomerId",
                table: "Bza_Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LabelCode",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalToken",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofOfPaymentUrl",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Bza_Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Bza_Collectors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Sales_BzaCustomerId",
                table: "Bza_Sales",
                column: "BzaCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Sales_Bza_Customers_BzaCustomerId",
                table: "Bza_Sales",
                column: "BzaCustomerId",
                principalTable: "Bza_Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
