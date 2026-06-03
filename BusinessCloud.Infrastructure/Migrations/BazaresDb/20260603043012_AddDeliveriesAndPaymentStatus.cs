using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddDeliveriesAndPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_DeliveryItems");

            migrationBuilder.DropTable(
                name: "Bza_Deliveries");

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
        }
    }
}
