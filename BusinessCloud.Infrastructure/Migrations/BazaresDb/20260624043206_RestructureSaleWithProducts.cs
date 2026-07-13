using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class RestructureSaleWithProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Events_BzaSaleId",
                table: "Bza_DeliveryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Events_BzaSaleId",
                table: "Bza_DispatchItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Payments_Bza_Events_BzaSaleId",
                table: "Bza_Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Customers_BzaCustomerId",
                table: "Bza_SoldProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Events_BzaSaleId",
                table: "Bza_SoldProducts");

            migrationBuilder.DropIndex(
                name: "IX_Bza_SoldProducts_BzaCustomerId",
                table: "Bza_SoldProducts");

            migrationBuilder.DropColumn(
                name: "BzaCustomerId",
                table: "Bza_SoldProducts");

            migrationBuilder.RenameColumn(
                name: "BzaSaleId",
                table: "Bza_Payments",
                newName: "BzaEventId");

            migrationBuilder.RenameIndex(
                name: "IX_Bza_Payments_BzaSaleId",
                table: "Bza_Payments",
                newName: "IX_Bza_Payments_BzaEventId");

            migrationBuilder.RenameColumn(
                name: "BzaSaleId",
                table: "Bza_DispatchItems",
                newName: "BzaEventId");

            migrationBuilder.RenameIndex(
                name: "IX_Bza_DispatchItems_BzaSaleId",
                table: "Bza_DispatchItems",
                newName: "IX_Bza_DispatchItems_BzaEventId");

            migrationBuilder.RenameColumn(
                name: "BzaSaleId",
                table: "Bza_DeliveryItems",
                newName: "BzaEventId");

            migrationBuilder.RenameIndex(
                name: "IX_Bza_DeliveryItems_BzaSaleId",
                table: "Bza_DeliveryItems",
                newName: "IX_Bza_DeliveryItems_BzaEventId");

            migrationBuilder.CreateTable(
                name: "Bza_Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaEventId = table.Column<int>(type: "int", nullable: false),
                    BzaCustomerId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_Sales_Bza_Customers_BzaCustomerId",
                        column: x => x.BzaCustomerId,
                        principalTable: "Bza_Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bza_Sales_Bza_Events_BzaEventId",
                        column: x => x.BzaEventId,
                        principalTable: "Bza_Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Sales_BzaCustomerId",
                table: "Bza_Sales",
                column: "BzaCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Sales_BzaEventId",
                table: "Bza_Sales",
                column: "BzaEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Events_BzaEventId",
                table: "Bza_DeliveryItems",
                column: "BzaEventId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Events_BzaEventId",
                table: "Bza_DispatchItems",
                column: "BzaEventId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Payments_Bza_Events_BzaEventId",
                table: "Bza_Payments",
                column: "BzaEventId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Sales_BzaSaleId",
                table: "Bza_SoldProducts",
                column: "BzaSaleId",
                principalTable: "Bza_Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Events_BzaEventId",
                table: "Bza_DeliveryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Events_BzaEventId",
                table: "Bza_DispatchItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Payments_Bza_Events_BzaEventId",
                table: "Bza_Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Sales_BzaSaleId",
                table: "Bza_SoldProducts");

            migrationBuilder.DropTable(
                name: "Bza_Sales");

            migrationBuilder.RenameColumn(
                name: "BzaEventId",
                table: "Bza_Payments",
                newName: "BzaSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_Bza_Payments_BzaEventId",
                table: "Bza_Payments",
                newName: "IX_Bza_Payments_BzaSaleId");

            migrationBuilder.RenameColumn(
                name: "BzaEventId",
                table: "Bza_DispatchItems",
                newName: "BzaSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_Bza_DispatchItems_BzaEventId",
                table: "Bza_DispatchItems",
                newName: "IX_Bza_DispatchItems_BzaSaleId");

            migrationBuilder.RenameColumn(
                name: "BzaEventId",
                table: "Bza_DeliveryItems",
                newName: "BzaSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_Bza_DeliveryItems_BzaEventId",
                table: "Bza_DeliveryItems",
                newName: "IX_Bza_DeliveryItems_BzaSaleId");

            migrationBuilder.AddColumn<int>(
                name: "BzaCustomerId",
                table: "Bza_SoldProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_SoldProducts_BzaCustomerId",
                table: "Bza_SoldProducts",
                column: "BzaCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Events_BzaSaleId",
                table: "Bza_DeliveryItems",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Events_BzaSaleId",
                table: "Bza_DispatchItems",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Payments_Bza_Events_BzaSaleId",
                table: "Bza_Payments",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Customers_BzaCustomerId",
                table: "Bza_SoldProducts",
                column: "BzaCustomerId",
                principalTable: "Bza_Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Events_BzaSaleId",
                table: "Bza_SoldProducts",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
