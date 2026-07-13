/*
    reset-bazares-commissions-azure.sql
    ------------------------------------
    Objetivo: dejar limpio el esquema de BAZARES y COMMISSIONS en Azure (bcloudMain)
    para reconstruirlo desde cero con las migraciones de EF Core.

    ⚠️ SEGURIDAD:
      - Este script SOLO elimina tablas con prefijo 'Bza' y la tabla 'InfluenceCenters'.
      - NO toca las tablas del módulo Payments (Customer, Sale, Payment, Seller,
        DeletedPayment, DeletedSale) ni las de Identity.
      - Revisa la lista de tablas impresa antes de confirmar (corre primero el bloque
        de SELECT si quieres verificar qué se va a borrar).

    Ejecutar en SSMS conectado a: sql-server-bcloud.database.windows.net / bcloudMain
*/

SET NOCOUNT ON;

/* --- (Opcional) Verifica qué tablas se eliminarán antes de correr el resto ---
SELECT name AS TablasQueSeEliminaran
FROM sys.tables
WHERE name LIKE 'Bza%' OR name = 'InfluenceCenters'
ORDER BY name;
*/

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @sql NVARCHAR(MAX) = N'';

    /* 1) Eliminar TODAS las foreign keys que estén sobre, o que referencien,
          tablas 'Bza%' o 'InfluenceCenters'. */
    SELECT @sql += N'ALTER TABLE '
        + QUOTENAME(SCHEMA_NAME(fk.schema_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM sys.foreign_keys fk
    WHERE OBJECT_NAME(fk.parent_object_id) LIKE 'Bza%'
       OR OBJECT_NAME(fk.parent_object_id) = 'InfluenceCenters'
       OR OBJECT_NAME(fk.referenced_object_id) LIKE 'Bza%'
       OR OBJECT_NAME(fk.referenced_object_id) = 'InfluenceCenters';

    IF (@sql <> N'') EXEC sp_executesql @sql;

    /* 2) Eliminar las tablas 'Bza%' y 'InfluenceCenters'. */
    SET @sql = N'';
    SELECT @sql += N'DROP TABLE '
        + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
    FROM sys.tables
    WHERE name LIKE 'Bza%' OR name = 'InfluenceCenters';

    IF (@sql <> N'') EXEC sp_executesql @sql;

    /* 3) Borrar del historial de migraciones SOLO las de Bazares y Commissions. */
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] IN (
        -- Commissions
        '20260512160553_InitialCreate',
        -- Bazares
        '20260512160545_InitialCreate',
        '20260529225158_UpdateBazaresSchema',
        '20260603043012_AddDeliveriesAndPaymentStatus',
        '20260603220250_AlignBzaSaleProductSchema',
        '20260603223919_RenameBzaProductToSoldProduct',
        '20260624002940_RemoveBzaSaleDeliveryDate',
        '20260624005856_RenameBzaSaleToBzaEvent',
        '20260624043206_RestructureSaleWithProducts',
        '20260624205924_AddBzaSaleSource',
        '20260624215541_AddBzaNotificationSettings',
        '20260624222157_AddPaymentCardNotes',
        '20260624230331_AddCollectorGroupDeliveryDay',
        '20260625151217_AddSaleClosureTotals',
        '20260625170748_AddSaleClosureLink',
        '20260625180912_AddProofRejectionFields',
        '20260625183829_AddBzaBazarSettings',
        '20260625192708_AddClosureInDeliveryProcess',
        '20260625212914_AddCustomerPhoneUniqueIndex',
        '20260704014424_AddPaymentCardWasSentForPayment',
        '20260710004903_AddClosureProofs',
        '20260710013947_AddProofRejections',
        '20260710022431_AddSaleCancellations',
        '20260710173219_AddBazarColors',
        '20260710174105_AddBazarLabelTagline',
        '20260710181255_AddBazarWhatsAppAndWithdrawal',
        '20260710184806_AddClosureTotalPaymentMethod',
        '20260710194416_AddManualValidationAndReference',
        '20260710195649_AddPaymentCutoffTime',
        '20260710222832_AddBlockedCustomers',
        '20260711001919_AddWhatsAppMessages'
    );

    COMMIT TRANSACTION;
    PRINT 'OK: esquema de Bazares y Commissions limpiado. Payments intacto.';
END TRY
BEGIN CATCH
    IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
