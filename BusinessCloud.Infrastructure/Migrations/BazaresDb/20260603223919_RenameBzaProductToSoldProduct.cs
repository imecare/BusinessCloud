using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class RenameBzaProductToSoldProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renombrar tabla si existe con el nombre viejo
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bza_Products')
                    EXEC sp_rename 'Bza_Products', 'Bza_SoldProducts';
            ");

            // Renombrar índices si existen con nombres viejos
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Products_BzaCustomerId' AND object_id = OBJECT_ID('Bza_SoldProducts'))
                    EXEC sp_rename N'Bza_SoldProducts.IX_Bza_Products_BzaCustomerId', N'IX_Bza_SoldProducts_BzaCustomerId', 'INDEX';
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Products_BzaSaleId' AND object_id = OBJECT_ID('Bza_SoldProducts'))
                    EXEC sp_rename N'Bza_SoldProducts.IX_Bza_Products_BzaSaleId', N'IX_Bza_SoldProducts_BzaSaleId', 'INDEX';
            ");

            // Renombrar PK si existe con nombre viejo
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Bza_Products')
                    EXEC sp_rename 'PK_Bza_Products', 'PK_Bza_SoldProducts';
            ");

            // Renombrar FKs si existen con nombres viejos
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Products_Bza_Customers_BzaCustomerId')
                    EXEC sp_rename 'FK_Bza_Products_Bza_Customers_BzaCustomerId', 'FK_Bza_SoldProducts_Bza_Customers_BzaCustomerId', 'OBJECT';
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Products_Bza_Sales_BzaSaleId')
                    EXEC sp_rename 'FK_Bza_Products_Bza_Sales_BzaSaleId', 'FK_Bza_SoldProducts_Bza_Sales_BzaSaleId', 'OBJECT';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir nombre de tabla
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bza_SoldProducts')
                    EXEC sp_rename 'Bza_SoldProducts', 'Bza_Products';
            ");

            // Revertir índices
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_SoldProducts_BzaCustomerId' AND object_id = OBJECT_ID('Bza_Products'))
                    EXEC sp_rename N'Bza_Products.IX_Bza_SoldProducts_BzaCustomerId', N'IX_Bza_Products_BzaCustomerId', 'INDEX';
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_SoldProducts_BzaSaleId' AND object_id = OBJECT_ID('Bza_Products'))
                    EXEC sp_rename N'Bza_Products.IX_Bza_SoldProducts_BzaSaleId', N'IX_Bza_Products_BzaSaleId', 'INDEX';
            ");

            // Revertir PK
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Bza_SoldProducts')
                    EXEC sp_rename 'PK_Bza_SoldProducts', 'PK_Bza_Products';
            ");

            // Revertir FKs
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_SoldProducts_Bza_Customers_BzaCustomerId')
                    EXEC sp_rename 'FK_Bza_SoldProducts_Bza_Customers_BzaCustomerId', 'FK_Bza_Products_Bza_Customers_BzaCustomerId', 'OBJECT';
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_SoldProducts_Bza_Sales_BzaSaleId')
                    EXEC sp_rename 'FK_Bza_SoldProducts_Bza_Sales_BzaSaleId', 'FK_Bza_Products_Bza_Sales_BzaSaleId', 'OBJECT';
            ");
        }
    }
}
