using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class Bza_V2_DispatchPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredToCollectorAt",
                table: "Bza_Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelCode",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                table: "Bza_Sales",
                type: "datetime2",
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

            migrationBuilder.AddColumn<string>(
                name: "PortalToken",
                table: "Bza_Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bza_DispatchSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaCollectorId = table.Column<int>(type: "int", nullable: false),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalPackages = table.Column<int>(type: "int", nullable: false),
                    CollectorSignatureUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_DispatchSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_DispatchSheets_Bza_Collectors_BzaCollectorId",
                        column: x => x.BzaCollectorId,
                        principalTable: "Bza_Collectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bza_Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaSaleId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProofImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_Payments_Bza_Sales_BzaSaleId",
                        column: x => x.BzaSaleId,
                        principalTable: "Bza_Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bza_DispatchItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaDispatchSheetId = table.Column<int>(type: "int", nullable: false),
                    BzaSaleId = table.Column<int>(type: "int", nullable: false),
                    PieceCount = table.Column<int>(type: "int", nullable: false),
                    LabelCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_DispatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_DispatchItems_Bza_DispatchSheets_BzaDispatchSheetId",
                        column: x => x.BzaDispatchSheetId,
                        principalTable: "Bza_DispatchSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_DispatchItems_Bza_Sales_BzaSaleId",
                        column: x => x.BzaSaleId,
                        principalTable: "Bza_Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_DispatchItems_BzaDispatchSheetId",
                table: "Bza_DispatchItems",
                column: "BzaDispatchSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_DispatchItems_BzaSaleId",
                table: "Bza_DispatchItems",
                column: "BzaSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_DispatchSheets_BzaCollectorId",
                table: "Bza_DispatchSheets",
                column: "BzaCollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Payments_BzaSaleId",
                table: "Bza_Payments",
                column: "BzaSaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_DispatchItems");

            migrationBuilder.DropTable(
                name: "Bza_Payments");

            migrationBuilder.DropTable(
                name: "Bza_DispatchSheets");

            migrationBuilder.DropColumn(
                name: "DeliveredToCollectorAt",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "LabelCode",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "PortalToken",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "ProofOfPaymentUrl",
                table: "Bza_Sales");

            migrationBuilder.DropColumn(
                name: "PortalToken",
                table: "Bza_Customers");
        }
    }
}
