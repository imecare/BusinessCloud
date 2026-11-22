using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.CatalogsDb
{
    /// <inheritdoc />
    public partial class AddCatalogProductDiscountPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "Cat_Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "Cat_Products");
        }
    }
}
