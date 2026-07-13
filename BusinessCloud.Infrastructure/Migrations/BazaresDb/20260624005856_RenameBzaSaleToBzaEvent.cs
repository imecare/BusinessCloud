using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class RenameBzaSaleToBzaEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Eliminar las FK que apuntan a Bza_Sales (los datos de las tablas hijas se conservan).
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Sales_BzaSaleId",
                table: "Bza_DeliveryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Sales_BzaSaleId",
                table: "Bza_DispatchItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Payments_Bza_Sales_BzaSaleId",
                table: "Bza_Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Sales_BzaSaleId",
                table: "Bza_SoldProducts");

            // 2. Renombrar la tabla conservando todos los datos.
            migrationBuilder.RenameTable(
                name: "Bza_Sales",
                newName: "Bza_Events");

            // 3. Renombrar la PK para que coincida con el nuevo nombre de la tabla.
            migrationBuilder.DropPrimaryKey(
                name: "PK_Bza_Sales",
                table: "Bza_Events");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bza_Events",
                table: "Bza_Events",
                column: "Id");

            // 4. Recrear las FK apuntando a Bza_Events.
            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Events_BzaSaleId",
                table: "Bza_DeliveryItems",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Events_BzaSaleId",
                table: "Bza_DispatchItems",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Payments_Bza_Events_BzaSaleId",
                table: "Bza_Payments",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Events_BzaSaleId",
                table: "Bza_SoldProducts",
                column: "BzaSaleId",
                principalTable: "Bza_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Events_BzaSaleId",
                table: "Bza_DeliveryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Events_BzaSaleId",
                table: "Bza_DispatchItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Payments_Bza_Events_BzaSaleId",
                table: "Bza_Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Events_BzaSaleId",
                table: "Bza_SoldProducts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bza_Events",
                table: "Bza_Events");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bza_Sales",
                table: "Bza_Events",
                column: "Id");

            migrationBuilder.RenameTable(
                name: "Bza_Events",
                newName: "Bza_Sales");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DeliveryItems_Bza_Sales_BzaSaleId",
                table: "Bza_DeliveryItems",
                column: "BzaSaleId",
                principalTable: "Bza_Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_DispatchItems_Bza_Sales_BzaSaleId",
                table: "Bza_DispatchItems",
                column: "BzaSaleId",
                principalTable: "Bza_Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Payments_Bza_Sales_BzaSaleId",
                table: "Bza_Payments",
                column: "BzaSaleId",
                principalTable: "Bza_Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_SoldProducts_Bza_Sales_BzaSaleId",
                table: "Bza_SoldProducts",
                column: "BzaSaleId",
                principalTable: "Bza_Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
