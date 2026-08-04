using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AddGlobalCollectorCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryFrequency",
                table: "Bza_CollectorGroups",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bza_GlobalCollectorGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeliveryFrequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryDay = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_GlobalCollectorGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bza_GlobalCollectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BzaGlobalCollectorGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bza_GlobalCollectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bza_GlobalCollectors_Bza_GlobalCollectorGroups_BzaGlobalCollectorGroupId",
                        column: x => x.BzaGlobalCollectorGroupId,
                        principalTable: "Bza_GlobalCollectorGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bza_GlobalCollectorGroups_Description",
                table: "Bza_GlobalCollectorGroups",
                column: "Description",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_GlobalCollectors_BzaGlobalCollectorGroupId_Name",
                table: "Bza_GlobalCollectors",
                columns: new[] { "BzaGlobalCollectorGroupId", "Name" },
                unique: true);
            migrationBuilder.InsertData(
                table: "Bza_GlobalCollectorGroups",
                columns: new[] { "Id", "Description", "DeliveryFrequency", "DeliveryDay" },
                values: new object[,]
                {
                { 1, "..+ ENVIOS", "QUINCENAL", null },
                { 2, "INDEPENDIENTE", "QUINCENAL / SEMANAL", 6 },
                { 3, "RECOLECCIONES INDEPENDIENTES", "QUINCENAL / SEMANAL", 6 },
                { 4, "GRUPO AE", "QUINCENAL", null },
                { 5, "GRUPO ERA", "QUINCENAL / SEMANAL", 5 },
                { 6, "LEGACY", "QUINCENAL / SEMANAL", null },
                { 7, "ANAHI TOLEDO", "QUINCENAL", null },
                { 8, "GRUPO R.U.T.", "QUINCENAL", null },
                { 9, "GRUPO REB", "QUINCENAL / SEMANAL", null },
                { 10, "EQUIPO MERAKI", "QUINCENAL / SEMANAL", 6 },
                { 11, "CLAUDIA MORENO", "QUINCENAL", null },
                { 12, "YETZA ALVAREZ", "QUINCENAL", null },
                { 13, "COCO BZR ENVIOS", "QUINCENAL", null },
                { 14, "GRUPO SAE", "QUINCENAL", null },
                { 15, "CARRUSEL", "QUINCENAL", null },
                { 16, "CASA", "QUINCENAL", null },
                { 17, "CASAS BETA", "QUINCENAL / SEMANAL", null },
                { 18, "CLINICA 1", "QUINCENAL", null },
                { 19, "MUNDO", "QUINCENAL", null },
                { 20, "MACRO", "QUINCENAL", null },
                { 21, "PARQUE CUCAPAH", "QUINCENAL", null },
                { 22, "PLAZA 2000", "QUINCENAL / SEMANAL", null },
                { 23, "ROSARITO", "QUINCENAL", null },
                { 24, "CALIMAX ROMA", "QUINCENAL", null },
                { 25, "PLAZA GRAN FLORIDO", "QUINCENAL", null },
                { 26, "SALON MASERATI", "QUINCENAL", null },
                { 27, "SENDERO", "QUINCENAL / SEMANAL", null },
                { 28, "CASA BLANCA", "QUINCENAL", null },
                { 29, "ENVIOS CAMPOS", "QUINCENAL / SEMANAL", 6 },
                { 30, "YULIANA ENVIOS", "SEMANAL", 6 },
                { 31, "SLT", "QUINCENAL", null },
                { 32, "SOLER", "QUINCENAL", null },
                { 33, "COTSCO VIA RAPIDA", "QUINCENAL", null },
                { 34, "el punto", "QUINCENAL", null },
                { 35, "CALIMAX CALLE SEGUNDA", "QUINCENAL", null },
                { 36, "CALIMAX CALLE 2DA", "QUINCENAL / SEMANAL", 6 },
                { 37, "IMPERIAL", "QUINCENAL / SEMANAL", null },
                { 38, "VICKY ENVIOS", "QUINCENAL", null },
                { 39, "OXXO EL DORADO - CASA BLANCA", "QUINCENAL", null },
                { 40, "HOSPITAL DEL PRADO", "QUINCENAL", null },
                { 41, "LOCAL", "QUINCENAL", null },
                { 42, "ERE CASA BLANCA", "QUINCENAL", null },
                { 43, "SMART CASABLANCA", "QUINCENAL", null },
                { 44, "OXXO PASANDO SMART CASA BLANCA", "QUINCENAL", null },
                { 45, "CORREOS DEL CENTRO", "QUINCENAL", null },
                { 46, "SOLER GASOLINERA", "QUINCENAL", null },
                { 47, "SORIANA PLAYAS", "QUINCENAL", null },
                { 48, "NATURA", "QUINCENAL", null },
                { 49, "ELITE", "QUINCENAL / SEMANAL", 6 },
                { 50, "PUERTA PLATA", "SEMANAL", 6 },
                { 51, "TIJUANA FIU FIU", "QUINCENAL", null },
                { 52, "ENSENADA", "QUINCENAL", null },
                { 53, "FIU FIU", "QUINCENAL / SEMANAL", 6 },
                { 54, "COMEX", "QUINCENAL", null },
                { 55, "MORBOX", "QUINCENAL", null },
                { 56, "ALEXIS MEZA", "QUINCENAL", null },
                { 57, "GRECIA ZC", "QUINCENAL", null },
                { 58, "PLAZA EL PUNTO", "QUINCENAL", null },
                { 59, "playas", "QUINCENAL", null },
                { 60, "PAQUETERIA FIU FIU", "QUINCENAL", null },
                { 61, "MERAKI", "QUINCENAL", null },
                { 62, "JACKY REYES", "QUINCENAL", null },
                { 63, "NAT TORRES", "QUINCENAL", null },
                { 64, "RECEPCION ELITE", "QUINCENAL", null },
                { 65, "FIU FIU LOCAL", "QUINCENAL", null },
                { 66, "ROUSE GARCIA", "QUINCENAL", null },
                { 67, "EL PUNTO 5 Y 10", "SEMANAL", null },
                { 68, "COCO ENVIOS", "QUINCENAL", null },
                });

            migrationBuilder.InsertData(
                table: "Bza_GlobalCollectors",
                columns: new[] { "Id", "Name", "BzaGlobalCollectorGroupId" },
                values: new object[,]
                {
                { 1, "MAGUI GARCIA", 1 },
                { 2, "MOR BOX", 1 },
                { 3, "AMANDADHONIS", 1 },
                { 4, "OMAR HERRERA", 1 },
                { 5, "ADRIAN ZAMBRANO", 1 },
                { 6, "SALDOS GROOT", 2 },
                { 7, "ALEXIS VELAZQUEZ", 2 },
                { 8, "ALISSON CONTRERAS", 2 },
                { 9, "ANY ENVIOS BAZAR", 2 },
                { 10, "EFREN ENVIOS", 2 },
                { 11, "KARINA GUZMAN", 2 },
                { 12, "LA COSTA ENVIOS", 2 },
                { 13, "LILI DORANTES", 2 },
                { 14, "ROCIO RODRIGUEZ", 2 },
                { 15, "LYDIA DEETZ", 2 },
                { 16, "LIZZMAR SEV", 2 },
                { 17, "MARE CAMACHO", 2 },
                { 18, "NORMA GUZMAN", 2 },
                { 19, "ORLANDO ENVIOS", 2 },
                { 20, "PROYECTOS TIJUANA GT", 2 },
                { 21, "ROSSWELL SANTI", 2 },
                { 22, "KITTY CHANEL", 2 },
                { 23, "GRECIA CZ", 2 },
                { 24, "BAZARES VICKY", 2 },
                { 25, "KAROLINA FLORES", 2 },
                { 26, "LAU CHACHARAS", 2 },
                { 27, "CHARLOTTE FLORES", 2 },
                { 28, "ANY ENVIOS", 2 },
                { 29, "OUTLETS PINOS RECOLECCION ENVIOS", 2 },
                { 30, "ANDREA - ANDROMEDA", 2 },
                { 31, "ALEH RE", 2 },
                { 32, "SHANTAL", 3 },
                { 33, "ELENA", 3 },
                { 34, "BRUCE", 3 },
                { 35, "CHARAL ENVIOS", 3 },
                { 36, "WAL MAGG", 3 },
                { 37, "ASAMJA ENVIOS", 3 },
                { 38, "LIZ ESCALANTE", 3 },
                { 39, "JESUS ROMERO", 3 },
                { 40, "BAZAR RI", 3 },
                { 41, "JIREH VNTAS", 3 },
                { 42, "ANAPAO ESPINOZA", 4 },
                { 43, "LEO ROBLES", 5 },
                { 44, "LULU", 5 },
                { 45, "ISABEL CARDENAS", 5 },
                { 46, "ANYS LOPEZ BAZAR", 5 },
                { 47, "ALEXA CARLIN", 5 },
                { 48, "ENVIOS PANCHITA", 5 },
                { 49, "ENVIOS MIRIAM CONDE", 5 },
                { 50, "ENVIOS YUYOS", 5 },
                { 51, "MARISELA GODINEZ", 5 },
                { 52, "CIRIA PABLOS", 6 },
                { 53, "CYNTHIA ARTEAGA", 6 },
                { 54, "CINTHYA ARTEAGA", 6 },
                { 55, "ANAHI TOLEDO", 7 },
                { 56, "YMURAH JIMENEZ", 8 },
                { 57, "ESMERALDA LOPEZ", 8 },
                { 58, "MONSERRAT LAGUNAS", 8 },
                { 59, "ADRIANA LAZO", 8 },
                { 60, "DONFRITHO", 9 },
                { 61, "ATENEA", 9 },
                { 62, "TANIA SANDOVAL", 9 },
                { 63, "ATENEA ENVIOS", 10 },
                { 64, "SANTIAGO ENVIOS", 10 },
                { 65, "ANITA", 10 },
                { 66, "ROSSY", 10 },
                { 67, "YEYI", 10 },
                { 68, "ALEX MORALES", 10 },
                { 69, "ANA PACHECO", 10 },
                { 70, "YAYAS TOYS", 10 },
                { 71, "KARISSA BORBON", 10 },
                { 72, "ANITA CRUZ", 10 },
                { 73, "YANELI ENVIOS", 10 },
                { 74, "NEON VTS", 10 },
                { 75, "CLAUDIA MORENO", 11 },
                { 76, "YETZA ALVAREZ", 12 },
                { 77, "COCO BZR ENVIOS", 13 },
                { 78, "DANY SICARDO", 14 },
                { 79, "RENATA BERNABE", 14 },
                { 80, "CARRUSEL", 15 },
                { 81, "CASA", 16 },
                { 82, "CASAS BETA", 17 },
                { 83, "CLINICA 1", 18 },
                { 84, "MUNDO", 19 },
                { 85, "MACRO", 20 },
                { 86, "PARQUE CUCAPAH", 21 },
                { 87, "PLAZA 2000", 22 },
                { 88, "ROSARITO", 23 },
                { 89, "CALIMAX ROMA", 24 },
                { 90, "PLAZA GRAN FLORIDO", 25 },
                { 91, "SALON MASERATI", 26 },
                { 92, "SENDERO", 27 },
                { 93, "CASA BLANCA", 28 },
                { 94, "NIKIS CAMPOS", 29 },
                { 95, "NERELY CAMPOS", 29 },
                { 96, "PESCADITO", 29 },
                { 97, "YULIANA ENVIOS", 30 },
                { 98, "VNTAS CHIK", 31 },
                { 99, "BZZR CHACHARAS", 31 },
                { 100, "valentina hurtado", 31 },
                { 101, "LUNA ROSE", 31 },
                { 102, "PRISCILLA ALVAREZ", 31 },
                { 103, "MONAMOUR ENVIOS", 31 },
                { 104, "TACOS EL RUSO -SOLER", 32 },
                { 105, "COTSCO VIA RAPIDA", 33 },
                { 106, "EL PUNTO", 34 },
                { 107, "CALIMAX CALLE SEGUNDA", 35 },
                { 108, "CALIMAX CALLE 2DA", 36 },
                { 109, "AARON SILVA", 37 },
                { 110, "ZUZET LOCAL", 37 },
                { 111, "IMPERIAL PACK", 37 },
                { 112, "zuzet silva", 37 },
                { 113, "CRISTY", 37 },
                { 114, "TANIA ANTAN SHOPCSTRO", 37 },
                { 115, "DANIEL YERENAS", 38 },
                { 116, "VICKY ENVIOS", 38 },
                { 117, "OXXO EL DORADO - CASA BLANCA", 39 },
                { 118, "HOSPITAL DEL PRADO", 40 },
                { 119, "OXXO - 2000", 41 },
                { 120, "PUENTE DEL VERGEL", 41 },
                { 121, "ERE CASA BLANCA", 42 },
                { 122, "SMART CASABLANCA", 43 },
                { 123, "OXXO PASANDO SMART CASA BLANCA", 44 },
                { 124, "CORREOS DEL CENTRO", 45 },
                { 125, "SOLER GASOLINERA", 46 },
                { 126, "SORIANA PLAYAS", 47 },
                { 127, "NATURA", 48 },
                { 128, "ERIKA RUIZ", 49 },
                { 129, "ENVIOS VILLA", 49 },
                { 130, "DAYANA PEREZ", 49 },
                { 131, "DAYHANA GONZALEZ", 49 },
                { 132, "LEXA PULIDO", 49 },
                { 133, "DHAYANA GONZALEZ", 49 },
                { 134, "PERLA RAMOS", 49 },
                { 135, "SANDY ESTRADA", 49 },
                { 136, "PUERTA PLATA", 50 },
                { 137, "TIJUANA FIU FIU", 51 },
                { 138, "JOCELIN BADILLO", 51 },
                { 139, "ENSENADA", 52 },
                { 140, "FIU FIU", 53 },
                { 141, "joceline badillo", 53 },
                { 142, "PIEL CANELA", 54 },
                { 143, "MORBOX", 55 },
                { 144, "ALEXIS MEZA", 56 },
                { 145, "GRECIA ZC", 57 },
                { 146, "PLAZA EL PUNTO", 58 },
                { 147, "playas", 59 },
                { 148, "PAQUETERIA FIU FIU", 60 },
                { 149, "NEON VTS", 61 },
                { 150, "ELIZABETH CARRANZA", 61 },
                { 151, "JACKY REYES", 62 },
                { 152, "NAT TORRES", 63 },
                { 153, "RECEPCION ELITE", 64 },
                { 154, "FIU FIU LOCAL", 65 },
                { 155, "FIU FIU LOCAL", 66 },
                { 156, "ROUSE GARCIA", 66 },
                { 157, "EL PUNTO 5 Y 10", 67 },
                { 158, "COCO ENVIOS", 68 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bza_GlobalCollectors");

            migrationBuilder.DropTable(
                name: "Bza_GlobalCollectorGroups");

            migrationBuilder.DropColumn(
                name: "DeliveryFrequency",
                table: "Bza_CollectorGroups");
        }
    }
}
