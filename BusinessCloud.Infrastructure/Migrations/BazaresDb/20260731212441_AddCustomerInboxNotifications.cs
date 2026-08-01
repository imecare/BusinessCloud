using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddCustomerInboxNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bza_CustomerInboxNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaCustomerId = table.Column<int>(type: "int", nullable: false),
                    BzaClosureCustomerTotalId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_CustomerInboxNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_CustomerInboxNotifications_Bza_ClosureCustomerTotals_BzaClosureCustomerTotalId",
                        column: x => x.BzaClosureCustomerTotalId,
                        principalTable: "Bza_ClosureCustomerTotals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_CustomerInboxNotifications_Bza_Customers_BzaCustomerId",
                        column: x => x.BzaCustomerId,
                        principalTable: "Bza_Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_CustomerInboxNotifications_BzaClosureCustomerTotalId",
                table: "Bza_CustomerInboxNotifications",
                column: "BzaClosureCustomerTotalId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_CustomerInboxNotifications_BzaCustomerId",
                table: "Bza_CustomerInboxNotifications",
                column: "BzaCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_CustomerInboxNotifications_TenantId_BzaClosureCustomerTotalId",
                table: "Bza_CustomerInboxNotifications",
                columns: new[] { "TenantId", "BzaClosureCustomerTotalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_CustomerInboxNotifications_TenantId_BzaCustomerId_ReadAt",
                table: "Bza_CustomerInboxNotifications",
                columns: new[] { "TenantId", "BzaCustomerId", "ReadAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_CustomerInboxNotifications");
        }
    }
}
