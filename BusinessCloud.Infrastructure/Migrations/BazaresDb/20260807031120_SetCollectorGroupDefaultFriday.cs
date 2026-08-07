using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class SetCollectorGroupDefaultFriday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Bza_CollectorGroups] SET [DeliveryDay] = 5 WHERE [DeliveryDay] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
