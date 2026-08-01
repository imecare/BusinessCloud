using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddLiveSaleDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bza_LiveSaleDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BzaEventId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_LiveSaleDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_LiveSaleDrafts_Bza_Events_BzaEventId",
                        column: x => x.BzaEventId,
                        principalTable: "Bza_Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_LiveSaleDrafts_BzaEventId",
                table: "Bza_LiveSaleDrafts",
                column: "BzaEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bza_LiveSaleDrafts_TenantId_BzaEventId",
                table: "Bza_LiveSaleDrafts",
                columns: new[] { "TenantId", "BzaEventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_LiveSaleDrafts");
        }
    }
}
