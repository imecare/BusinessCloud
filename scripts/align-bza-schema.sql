-- Sincronizar esquema BD con modelo: BzaSale como evento, BzaProduct con FK al cliente

-- 1) Bza_Sales: quitar FK + indice + columnas legacy
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Sales_Bza_Customers_BzaCustomerId')
    ALTER TABLE [Bza_Sales] DROP CONSTRAINT [FK_Bza_Sales_Bza_Customers_BzaCustomerId];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Sales_BzaCustomerId' AND object_id = OBJECT_ID('Bza_Sales'))
    DROP INDEX [IX_Bza_Sales_BzaCustomerId] ON [Bza_Sales];

IF COL_LENGTH('Bza_Sales','BzaCustomerId') IS NOT NULL ALTER TABLE [Bza_Sales] DROP COLUMN [BzaCustomerId];
IF COL_LENGTH('Bza_Sales','Total') IS NOT NULL ALTER TABLE [Bza_Sales] DROP COLUMN [Total];
IF COL_LENGTH('Bza_Sales','LabelCode') IS NOT NULL ALTER TABLE [Bza_Sales] DROP COLUMN [LabelCode];
IF COL_LENGTH('Bza_Sales','PortalToken') IS NOT NULL ALTER TABLE [Bza_Sales] DROP COLUMN [PortalToken];
IF COL_LENGTH('Bza_Sales','ProofOfPaymentUrl') IS NOT NULL ALTER TABLE [Bza_Sales] DROP COLUMN [ProofOfPaymentUrl];

-- 2) Bza_Sales: renombrar DeliveredToCollectorAt -> DeliveryDate
IF COL_LENGTH('Bza_Sales','DeliveredToCollectorAt') IS NOT NULL AND COL_LENGTH('Bza_Sales','DeliveryDate') IS NULL
    EXEC sp_rename 'Bza_Sales.DeliveredToCollectorAt', 'DeliveryDate', 'COLUMN';

-- 3) Bza_Sales: Description NOT NULL
UPDATE [Bza_Sales] SET [Description] = N'' WHERE [Description] IS NULL;
ALTER TABLE [Bza_Sales] ALTER COLUMN [Description] nvarchar(max) NOT NULL;

-- 4) Bza_Products: agregar BzaCustomerId + FK + indice
IF COL_LENGTH('Bza_Products','BzaCustomerId') IS NULL
    ALTER TABLE [Bza_Products] ADD [BzaCustomerId] int NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Products_BzaCustomerId' AND object_id = OBJECT_ID('Bza_Products'))
    CREATE INDEX [IX_Bza_Products_BzaCustomerId] ON [Bza_Products]([BzaCustomerId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Products_Bza_Customers_BzaCustomerId')
    ALTER TABLE [Bza_Products] ADD CONSTRAINT [FK_Bza_Products_Bza_Customers_BzaCustomerId]
        FOREIGN KEY ([BzaCustomerId]) REFERENCES [Bza_Customers]([Id]);

-- 5) Bza_Payments: agregar BzaCustomerId + FK + indice
IF COL_LENGTH('Bza_Payments','BzaCustomerId') IS NULL
    ALTER TABLE [Bza_Payments] ADD [BzaCustomerId] int NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bza_Payments_BzaCustomerId' AND object_id = OBJECT_ID('Bza_Payments'))
    CREATE INDEX [IX_Bza_Payments_BzaCustomerId] ON [Bza_Payments]([BzaCustomerId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bza_Payments_Bza_Customers_BzaCustomerId')
    ALTER TABLE [Bza_Payments] ADD CONSTRAINT [FK_Bza_Payments_Bza_Customers_BzaCustomerId]
        FOREIGN KEY ([BzaCustomerId]) REFERENCES [Bza_Customers]([Id]);

-- 6) Registrar migracion en __EFMigrationsHistory
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260603220250_AlignBzaSaleProductSchema')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES ('20260603220250_AlignBzaSaleProductSchema','10.0.0');
