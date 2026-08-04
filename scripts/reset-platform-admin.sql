/*
    reset-platform-admin.sql
    -------------------------------------------------------------------------
    Borra el usuario PlatformAdmin (super admin del FrontAdmin) y sus filas
    dependientes de Identity. Tras ejecutarlo, REINICIA la API: el bloque de
    seeding en Program.cs volverá a crear el usuario con el valor actual de
    los user-secrets (PlatformAdmin:Email / PlatformAdmin:Password).

    Base de datos: bcloudMain   (conexión PaymentsConnection)
    Email objetivo: contacto@bcloud.com.mx
    -------------------------------------------------------------------------
*/

USE [bcloudMain];
GO

DECLARE @Email NVARCHAR(256) = N'contacto@bcloud.com.mx';

DECLARE @UserId NVARCHAR(450) =
    (SELECT [Id] FROM [dbo].[AspNetUsers] WHERE [Email] = @Email);

IF @UserId IS NULL
BEGIN
    PRINT 'No existe ningún usuario con ese email. Nada que borrar.';
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE FROM [dbo].[AspNetUserRoles]  WHERE [UserId] = @UserId;
    DELETE FROM [dbo].[AspNetUserClaims] WHERE [UserId] = @UserId;
    DELETE FROM [dbo].[AspNetUserLogins] WHERE [UserId] = @UserId;
    DELETE FROM [dbo].[AspNetUserTokens] WHERE [UserId] = @UserId;
    DELETE FROM [dbo].[AspNetUsers]      WHERE [Id]     = @UserId;

    COMMIT TRANSACTION;
    PRINT 'PlatformAdmin borrado. Reinicia la API para que el seeding lo vuelva a crear.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Error al borrar el PlatformAdmin: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
