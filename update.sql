BEGIN TRANSACTION;
ALTER TABLE [InventoryTransfers] ADD [ApprovedDate] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421185355_AddApprovedDateToInventoryTransfer', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260422202019_AddApprovalStatusFields', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424101322_AddItemDetailsToOrders', N'9.0.2');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'TireChangeDue');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Vehicles] DROP COLUMN [TireChangeDue];

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'TireChangeDueMileage');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [TireChangeDueMileage] int NULL;

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'Mileage');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [Mileage] int NULL;

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'CurrentMileage');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [CurrentMileage] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424181332_FixVehiclePropertyTypes', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424201928_SyncAllModelsToDatabase_Fix', N'9.0.2');

COMMIT;
GO

