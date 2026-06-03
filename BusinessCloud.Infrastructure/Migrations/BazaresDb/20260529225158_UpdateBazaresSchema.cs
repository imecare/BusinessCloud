using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class UpdateBazaresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Bza_Collectors");

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

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Collectors_BzaCollectorGroupId",
                table: "Bza_Collectors",
                column: "BzaCollectorGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Collectors_Bza_CollectorGroups_BzaCollectorGroupId",
                table: "Bza_Collectors",
                column: "BzaCollectorGroupId",
                principalTable: "Bza_CollectorGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Collectors_Bza_CollectorGroups_BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropTable(
                name: "Bza_CollectorGroups");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Collectors_BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropColumn(
                name: "BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Bza_Collectors");

            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Bza_Collectors",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
