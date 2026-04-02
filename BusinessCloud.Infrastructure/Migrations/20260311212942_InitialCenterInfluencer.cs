using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCenterInfluencer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RFC",
                table: "InfluenceCenters",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InfluenceCenters",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "InfluenceCenters",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InfluenceCenters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "InfluenceCenters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InfluenceCenters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "InfluenceCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfluenceCenters_RFC",
                table: "InfluenceCenters",
                column: "RFC",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfluenceCenters_Username",
                table: "InfluenceCenters",
                column: "Username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InfluenceCenters_RFC",
                table: "InfluenceCenters");

            migrationBuilder.DropIndex(
                name: "IX_InfluenceCenters_Username",
                table: "InfluenceCenters");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InfluenceCenters");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "InfluenceCenters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InfluenceCenters");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "InfluenceCenters");

            migrationBuilder.AlterColumn<string>(
                name: "RFC",
                table: "InfluenceCenters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InfluenceCenters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "InfluenceCenters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
