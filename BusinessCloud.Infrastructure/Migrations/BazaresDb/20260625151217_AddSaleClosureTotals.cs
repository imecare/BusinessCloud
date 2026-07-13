using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddSaleClosureTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Bza_Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Bza_ClosureEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OfficialDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_ClosureEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bza_ClosureCustomerTotals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaClosureEventId = table.Column<int>(type: "int", nullable: false),
                    BzaCustomerId = table.Column<int>(type: "int", nullable: false),
                    BzaCollectorGroupId = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UploadToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProofImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofUploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_ClosureCustomerTotals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureCustomerTotals_Bza_ClosureEvents_BzaClosureEventId",
                        column: x => x.BzaClosureEventId,
                        principalTable: "Bza_ClosureEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureCustomerTotals_Bza_Customers_BzaCustomerId",
                        column: x => x.BzaCustomerId,
                        principalTable: "Bza_Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bza_ClosureEventItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaClosureEventId = table.Column<int>(type: "int", nullable: false),
                    BzaEventId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_ClosureEventItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureEventItems_Bza_ClosureEvents_BzaClosureEventId",
                        column: x => x.BzaClosureEventId,
                        principalTable: "Bza_ClosureEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureEventItems_Bza_Events_BzaEventId",
                        column: x => x.BzaEventId,
                        principalTable: "Bza_Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bza_ClosureGroupDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaClosureEventId = table.Column<int>(type: "int", nullable: false),
                    BzaCollectorGroupId = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_ClosureGroupDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureGroupDeliveries_Bza_ClosureEvents_BzaClosureEventId",
                        column: x => x.BzaClosureEventId,
                        principalTable: "Bza_ClosureEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureGroupDeliveries_Bza_CollectorGroups_BzaCollectorGroupId",
                        column: x => x.BzaCollectorGroupId,
                        principalTable: "Bza_CollectorGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureCustomerTotals_BzaClosureEventId",
                table: "Bza_ClosureCustomerTotals",
                column: "BzaClosureEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureCustomerTotals_BzaCustomerId",
                table: "Bza_ClosureCustomerTotals",
                column: "BzaCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureCustomerTotals_UploadToken",
                table: "Bza_ClosureCustomerTotals",
                column: "UploadToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureEventItems_BzaClosureEventId",
                table: "Bza_ClosureEventItems",
                column: "BzaClosureEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureEventItems_BzaEventId",
                table: "Bza_ClosureEventItems",
                column: "BzaEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureGroupDeliveries_BzaClosureEventId",
                table: "Bza_ClosureGroupDeliveries",
                column: "BzaClosureEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureGroupDeliveries_BzaCollectorGroupId",
                table: "Bza_ClosureGroupDeliveries",
                column: "BzaCollectorGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_ClosureCustomerTotals");

            migrationBuilder.DropTable(
                name: "Bza_ClosureEventItems");

            migrationBuilder.DropTable(
                name: "Bza_ClosureGroupDeliveries");

            migrationBuilder.DropTable(
                name: "Bza_ClosureEvents");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Bza_Sales");
        }
    }
}
