using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddNotificationSubscriptionsAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bza_CustomerNotificationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaCustomerId = table.Column<int>(type: "int", nullable: false),
                    BzaClosureCustomerTotalId = table.Column<int>(type: "int", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSuccessfulPushAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailedPushAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_CustomerNotificationSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_CustomerNotificationSubscriptions_Bza_ClosureCustomerTotals_BzaClosureCustomerTotalId",
                        column: x => x.BzaClosureCustomerTotalId,
                        principalTable: "Bza_ClosureCustomerTotals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bza_CustomerNotificationSubscriptions_Bza_Customers_BzaCustomerId",
                        column: x => x.BzaCustomerId,
                        principalTable: "Bza_Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bza_NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaClosureEventId = table.Column<int>(type: "int", nullable: false),
                    BzaClosureCustomerTotalId = table.Column<int>(type: "int", nullable: false),
                    BzaCustomerId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_NotificationLogs_Bza_ClosureCustomerTotals_BzaClosureCustomerTotalId",
                        column: x => x.BzaClosureCustomerTotalId,
                        principalTable: "Bza_ClosureCustomerTotals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bza_NotificationLogs_Bza_ClosureEvents_BzaClosureEventId",
                        column: x => x.BzaClosureEventId,
                        principalTable: "Bza_ClosureEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_NotificationLogs_Bza_Customers_BzaCustomerId",
                        column: x => x.BzaCustomerId,
                        principalTable: "Bza_Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_CustomerNotificationSubscriptions_BzaClosureCustomerTotalId",
                table: "Bza_CustomerNotificationSubscriptions",
                column: "BzaClosureCustomerTotalId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_CustomerNotificationSubscriptions_BzaCustomerId",
                table: "Bza_CustomerNotificationSubscriptions",
                column: "BzaCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_CustomerNotificationSubscriptions_TenantId_BzaCustomerId_Endpoint",
                table: "Bza_CustomerNotificationSubscriptions",
                columns: new[] { "TenantId", "BzaCustomerId", "Endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_NotificationLogs_BzaClosureCustomerTotalId",
                table: "Bza_NotificationLogs",
                column: "BzaClosureCustomerTotalId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_NotificationLogs_BzaClosureEventId",
                table: "Bza_NotificationLogs",
                column: "BzaClosureEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_NotificationLogs_BzaCustomerId",
                table: "Bza_NotificationLogs",
                column: "BzaCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_NotificationLogs_TenantId_BzaClosureEventId_BzaClosureCustomerTotalId_SentAt",
                table: "Bza_NotificationLogs",
                columns: new[] { "TenantId", "BzaClosureEventId", "BzaClosureCustomerTotalId", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_CustomerNotificationSubscriptions");

            migrationBuilder.DropTable(
                name: "Bza_NotificationLogs");
        }
    }
}
