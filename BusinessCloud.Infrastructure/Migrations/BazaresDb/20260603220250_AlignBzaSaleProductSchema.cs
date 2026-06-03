using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessCloud.Infrastructure.Migrations.BazaresDb
{
    /// <inheritdoc />
    public partial class AlignBzaSaleProductSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usar SQL condicional para evitar errores si los objetos no existen
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Sales_Bza_Customers_BzaCustomerId')
                    ALTER TABLE [Bza_Sales] DROP CONSTRAINT [FK_Bza_Sales_Bza_Customers_BzaCustomerId];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Sales_BzaCustomerId' AND object_id = OBJECT_ID('Bza_Sales'))
                    DROP INDEX [IX_Bza_Sales_BzaCustomerId] ON [Bza_Sales];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'BzaCustomerId' AND object_id = OBJECT_ID('Bza_Sales'))
                    ALTER TABLE [Bza_Sales] DROP COLUMN [BzaCustomerId];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'LabelCode' AND object_id = OBJECT_ID('Bza_Sales'))
                    ALTER TABLE [Bza_Sales] DROP COLUMN [LabelCode];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'PortalToken' AND object_id = OBJECT_ID('Bza_Sales'))
                    ALTER TABLE [Bza_Sales] DROP COLUMN [PortalToken];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ProofOfPaymentUrl' AND object_id = OBJECT_ID('Bza_Sales'))
                    ALTER TABLE [Bza_Sales] DROP COLUMN [ProofOfPaymentUrl];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'Total' AND object_id = OBJECT_ID('Bza_Sales'))
                    ALTER TABLE [Bza_Sales] DROP COLUMN [Total];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'GroupId' AND object_id = OBJECT_ID('Bza_Collectors'))
                    ALTER TABLE [Bza_Collectors] DROP COLUMN [GroupId];
            ");

            // Renombrar columna si existe
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'DeliveredToCollectorAt' AND object_id = OBJECT_ID('Bza_Sales'))
                    EXEC sp_rename 'Bza_Sales.DeliveredToCollectorAt', 'DeliveryDate', 'COLUMN';
            ");

            // Asegurar que Description no sea nullable
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'Description' AND object_id = OBJECT_ID('Bza_Sales') AND is_nullable = 1)
                    ALTER TABLE [Bza_Sales] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
            ");

            // Agregar columnas si no existen
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'BzaCustomerId' AND object_id = OBJECT_ID('Bza_Products'))
                    ALTER TABLE [Bza_Products] ADD [BzaCustomerId] int NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'BzaCustomerId' AND object_id = OBJECT_ID('Bza_Payments'))
                    ALTER TABLE [Bza_Payments] ADD [BzaCustomerId] int NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'PaymentStatus' AND object_id = OBJECT_ID('Bza_Payments'))
                    ALTER TABLE [Bza_Payments] ADD [PaymentStatus] int NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'VerificationNotes' AND object_id = OBJECT_ID('Bza_Payments'))
                    ALTER TABLE [Bza_Payments] ADD [VerificationNotes] nvarchar(max) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'VerifiedAt' AND object_id = OBJECT_ID('Bza_Payments'))
                    ALTER TABLE [Bza_Payments] ADD [VerifiedAt] datetime2 NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'Address' AND object_id = OBJECT_ID('Bza_Customers'))
                    ALTER TABLE [Bza_Customers] ADD [Address] nvarchar(max) NULL;
            ");

            // Agregar columnas a Bza_Collectors
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'BzaCollectorGroupId' AND object_id = OBJECT_ID('Bza_Collectors'))
                    ALTER TABLE [Bza_Collectors] ADD [BzaCollectorGroupId] int NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'IsActive' AND object_id = OBJECT_ID('Bza_Collectors'))
                    ALTER TABLE [Bza_Collectors] ADD [IsActive] bit NOT NULL DEFAULT 0;
            ");

            // Crear tablas si no existen
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bza_CollectorGroups')
                BEGIN
                    CREATE TABLE [Bza_CollectorGroups] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [Description] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [TenantId] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_Bza_CollectorGroups] PRIMARY KEY ([Id])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bza_Deliveries')
                BEGIN
                    CREATE TABLE [Bza_Deliveries] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [BzaCollectorGroupId] int NOT NULL,
                        [DeliveryDate] datetime2 NOT NULL,
                        [Status] int NOT NULL,
                        [Notes] nvarchar(max) NULL,
                        [TenantId] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_Bza_Deliveries] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Bza_Deliveries_Bza_CollectorGroups_BzaCollectorGroupId] FOREIGN KEY ([BzaCollectorGroupId]) REFERENCES [Bza_CollectorGroups]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_Bza_Deliveries_BzaCollectorGroupId] ON [Bza_Deliveries]([BzaCollectorGroupId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bza_DeliveryItems')
                BEGIN
                    CREATE TABLE [Bza_DeliveryItems] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [BzaDeliveryId] int NOT NULL,
                        [BzaSaleId] int NOT NULL,
                        [Delivered] bit NOT NULL,
                        [DeliveredAt] datetime2 NULL,
                        [Notes] nvarchar(max) NULL,
                        [TenantId] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_Bza_DeliveryItems] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Bza_DeliveryItems_Bza_Deliveries_BzaDeliveryId] FOREIGN KEY ([BzaDeliveryId]) REFERENCES [Bza_Deliveries]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Bza_DeliveryItems_Bza_Sales_BzaSaleId] FOREIGN KEY ([BzaSaleId]) REFERENCES [Bza_Sales]([Id])
                    );
                    CREATE INDEX [IX_Bza_DeliveryItems_BzaDeliveryId] ON [Bza_DeliveryItems]([BzaDeliveryId]);
                    CREATE INDEX [IX_Bza_DeliveryItems_BzaSaleId] ON [Bza_DeliveryItems]([BzaSaleId]);
                END
            ");

            // Crear índices si no existen
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Products_BzaCustomerId' AND object_id = OBJECT_ID('Bza_Products'))
                    CREATE INDEX [IX_Bza_Products_BzaCustomerId] ON [Bza_Products]([BzaCustomerId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Payments_BzaCustomerId' AND object_id = OBJECT_ID('Bza_Payments'))
                    CREATE INDEX [IX_Bza_Payments_BzaCustomerId] ON [Bza_Payments]([BzaCustomerId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Collectors_BzaCollectorGroupId' AND object_id = OBJECT_ID('Bza_Collectors'))
                    CREATE INDEX [IX_Bza_Collectors_BzaCollectorGroupId] ON [Bza_Collectors]([BzaCollectorGroupId]);
            ");

            // Crear FKs si no existen
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Collectors_Bza_CollectorGroups_BzaCollectorGroupId')
                    ALTER TABLE [Bza_Collectors] ADD CONSTRAINT [FK_Bza_Collectors_Bza_CollectorGroups_BzaCollectorGroupId] FOREIGN KEY ([BzaCollectorGroupId]) REFERENCES [Bza_CollectorGroups]([Id]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Payments_Bza_Customers_BzaCustomerId')
                    ALTER TABLE [Bza_Payments] ADD CONSTRAINT [FK_Bza_Payments_Bza_Customers_BzaCustomerId] FOREIGN KEY ([BzaCustomerId]) REFERENCES [Bza_Customers]([Id]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Products_Bza_Customers_BzaCustomerId')
                    ALTER TABLE [Bza_Products] ADD CONSTRAINT [FK_Bza_Products_Bza_Customers_BzaCustomerId] FOREIGN KEY ([BzaCustomerId]) REFERENCES [Bza_Customers]([Id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Collectors_Bza_CollectorGroups_BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Payments_Bza_Customers_BzaCustomerId",
                table: "Bza_Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Bza_Products_Bza_Customers_BzaCustomerId",
                table: "Bza_Products");

            migrationBuilder.DropTable(
                name: "Bza_DeliveryItems");

            migrationBuilder.DropTable(
                name: "Bza_Deliveries");

            migrationBuilder.DropTable(
                name: "Bza_CollectorGroups");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Products_BzaCustomerId",
                table: "Bza_Products");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Payments_BzaCustomerId",
                table: "Bza_Payments");

            migrationBuilder.DropIndex(
                name: "IX_Bza_Collectors_BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropColumn(
                name: "BzaCustomerId",
                table: "Bza_Products");

            migrationBuilder.DropColumn(
                name: "BzaCustomerId",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Bza_Payments");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Bza_Customers");

            migrationBuilder.DropColumn(
                name: "BzaCollectorGroupId",
                table: "Bza_Collectors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Bza_Collectors");

            migrationBuilder.RenameColumn(
                name: "DeliveryDate",
                table: "Bza_Sales",
                newName: "DeliveredToCollectorAt");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BzaCustomerId",
                table: "Bza_Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LabelCode",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalToken",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofOfPaymentUrl",
                table: "Bza_Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Bza_Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Bza_Collectors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bza_Sales_BzaCustomerId",
                table: "Bza_Sales",
                column: "BzaCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bza_Sales_Bza_Customers_BzaCustomerId",
                table: "Bza_Sales",
                column: "BzaCustomerId",
                principalTable: "Bza_Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
