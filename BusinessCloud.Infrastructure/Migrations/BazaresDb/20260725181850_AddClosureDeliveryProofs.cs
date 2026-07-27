using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddClosureDeliveryProofs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Delivered",
                table: "Bza_ClosureEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Bza_ClosureEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bza_ClosureDeliveryProofs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaClosureEventId = table.Column<int>(type: "int", nullable: false),
                    BzaCollectorGroupId = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_ClosureDeliveryProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureDeliveryProofs_Bza_ClosureEvents_BzaClosureEventId",
                        column: x => x.BzaClosureEventId,
                        principalTable: "Bza_ClosureEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bza_ClosureDeliveryProofs_Bza_CollectorGroups_BzaCollectorGroupId",
                        column: x => x.BzaCollectorGroupId,
                        principalTable: "Bza_CollectorGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureDeliveryProofs_BzaClosureEventId",
                table: "Bza_ClosureDeliveryProofs",
                column: "BzaClosureEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_ClosureDeliveryProofs_BzaCollectorGroupId",
                table: "Bza_ClosureDeliveryProofs",
                column: "BzaCollectorGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_ClosureDeliveryProofs");

            migrationBuilder.DropColumn(
                name: "Delivered",
                table: "Bza_ClosureEvents");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Bza_ClosureEvents");
        }
    }
}
