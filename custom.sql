BEGIN TRANSACTION;
ALTER TABLE [SupplierEmployees] DROP CONSTRAINT [FK_SupplierEmployees_Users_UserId];
DROP INDEX [IX_SupplierEmployees_UserId] ON [SupplierEmployees];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SupplierEmployees]') AND [c].[name] = N'IsActive');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [SupplierEmployees] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [SupplierEmployees] DROP COLUMN [IsActive];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SupplierEmployees]') AND [c].[name] = N'UserId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [SupplierEmployees] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [SupplierEmployees] DROP COLUMN [UserId];

EXEC sp_rename N'[SupplierEmployees].[EmployeeRole]', N'Role', 'COLUMN';
ALTER TABLE [SupplierEmployees] ADD [FullName] nvarchar(100) NOT NULL DEFAULT N'';
ALTER TABLE [SupplierEmployees] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'';
COMMIT;
GO
