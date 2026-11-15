BEGIN TRANSACTION;
GO

ALTER TABLE [Staffs] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261112110423_AddStaffIsActive', N'8.0.28');
GO

COMMIT;
GO

