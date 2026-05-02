BEGIN TRANSACTION;
ALTER TABLE [Penalties] ADD [AppealDate] datetime2 NULL;

ALTER TABLE [Penalties] ADD [AppealReason] nvarchar(max) NULL;

ALTER TABLE [Penalties] ADD [AppealResponse] nvarchar(max) NULL;

ALTER TABLE [Penalties] ADD [AppealResponseDate] datetime2 NULL;

ALTER TABLE [Penalties] ADD [HasAppealed] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Penalties] ADD [IssuedByAdminId] int NULL;

ALTER TABLE [Penalties] ADD [MessageId] int NULL;

ALTER TABLE [Penalties] ADD [MessageId1] int NULL;

ALTER TABLE [Penalties] ADD [Status] int NOT NULL DEFAULT 0;

ALTER TABLE [Penalties] ADD [UserType] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Messages] ADD [BlockedAt] datetime2 NULL;

ALTER TABLE [Messages] ADD [BlockedReason] nvarchar(max) NULL;

ALTER TABLE [Messages] ADD [IsBlocked] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Messages] ADD [PenaltyId] int NULL;

ALTER TABLE [Messages] ADD [TriggeredPenalty] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE INDEX [IX_Penalties_IssuedByAdminId] ON [Penalties] ([IssuedByAdminId]);

CREATE INDEX [IX_Penalties_MessageId1] ON [Penalties] ([MessageId1]);

ALTER TABLE [Penalties] ADD CONSTRAINT [FK_Penalties_Messages_MessageId1] FOREIGN KEY ([MessageId1]) REFERENCES [Messages] ([Id]);

ALTER TABLE [Penalties] ADD CONSTRAINT [FK_Penalties_Users_IssuedByAdminId] FOREIGN KEY ([IssuedByAdminId]) REFERENCES [Users] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260403141133_SyncMessageModel', N'9.0.2');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Type');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var + '];');
UPDATE [Notifications] SET [Type] = N'' WHERE [Type] IS NULL;
ALTER TABLE [Notifications] ALTER COLUMN [Type] nvarchar(20) NOT NULL;
ALTER TABLE [Notifications] ADD DEFAULT N'' FOR [Type];

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Message');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Notifications] ALTER COLUMN [Message] nvarchar(500) NOT NULL;

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'ActionUrl');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Notifications] ALTER COLUMN [ActionUrl] nvarchar(200) NULL;

ALTER TABLE [Notifications] ADD [ReadAt] datetime2 NULL;

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MessageViolations]') AND [c].[name] = N'ViolationType');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MessageViolations] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [MessageViolations] ALTER COLUMN [ViolationType] nvarchar(900) NOT NULL;

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commissions]') AND [c].[name] = N'PaymentVerificationData');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Commissions] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Commissions] ALTER COLUMN [PaymentVerificationData] nvarchar(max) NULL;

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commissions]') AND [c].[name] = N'PaymentRequestData');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Commissions] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Commissions] ALTER COLUMN [PaymentRequestData] nvarchar(max) NULL;

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commissions]') AND [c].[name] = N'ChapaTransactionId');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Commissions] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Commissions] ALTER COLUMN [ChapaTransactionId] nvarchar(100) NULL;

ALTER TABLE [Commissions] ADD [ChapaPaymentUrl] nvarchar(200) NULL;

ALTER TABLE [Commissions] ADD [CommissionRate] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Commissions] ADD [DueDate] datetime2 NULL;

ALTER TABLE [Commissions] ADD [Notes] nvarchar(500) NULL;

ALTER TABLE [Commissions] ADD [OrderAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Commissions] ADD [OrderId] int NOT NULL DEFAULT 0;

ALTER TABLE [Commissions] ADD [PaidAt] datetime2 NULL;

CREATE INDEX [IX_Commissions_OrderId] ON [Commissions] ([OrderId]);

ALTER TABLE [Commissions] ADD CONSTRAINT [FK_Commissions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260403163042_AddPaymentDataToCommission', N'9.0.2');

ALTER TABLE [Users] ADD [DateOfBirth] datetime2 NULL;

ALTER TABLE [Users] ADD [FAN] nvarchar(16) NULL;

ALTER TABLE [Users] ADD [FaydaStatus] nvarchar(20) NOT NULL DEFAULT N'';

ALTER TABLE [Users] ADD [FaydaVerifiedAt] datetime2 NULL;

ALTER TABLE [Users] ADD [IsFaydaVerified] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE UNIQUE INDEX [IX_Users_FAN] ON [Users] ([FAN]) WHERE [FAN] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260407125949_AddFaydaIdentityFields', N'9.0.2');

CREATE TABLE [FaydaRegistries] (
    [Id] int NOT NULL IDENTITY,
    [FAN] nvarchar(16) NOT NULL,
    [FullName] nvarchar(150) NOT NULL,
    [DateOfBirth] datetime2 NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [Region] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_FaydaRegistries] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_FaydaRegistries_FAN] ON [FaydaRegistries] ([FAN]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260407133418_AddFaydaRegistryMock', N'9.0.2');

ALTER TABLE [Users] ADD [ApprovedAt] datetime2 NULL;

ALTER TABLE [Users] ADD [RejectionReason] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260407181819_AddRejectionAndApprovalProperties', N'9.0.2');

ALTER TABLE [Users] ADD [ApprovalStatus] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Users] ADD [VerifiedFullName] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [VerifiedPhoneNumber] nvarchar(max) NULL;

ALTER TABLE [FaydaRegistries] ADD [PhoneNumber] nvarchar(20) NOT NULL DEFAULT N'';

CREATE TABLE [FaydaVerifications] (
    [Id] int NOT NULL IDENTITY,
    [FaydaId] nvarchar(16) NOT NULL,
    [OTP] nvarchar(6) NULL,
    [OTPExpiry] datetime2 NULL,
    [AttemptCount] int NOT NULL,
    [IsLocked] bit NOT NULL,
    [IsVerified] bit NOT NULL,
    [LastOtpRequestTime] datetime2 NOT NULL,
    CONSTRAINT [PK_FaydaVerifications] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260409063146_AddFaydaVerification', N'9.0.2');

EXEC sp_rename N'[FaydaVerifications].[OTPExpiry]', N'OtpExpiry', 'COLUMN';

EXEC sp_rename N'[FaydaVerifications].[FaydaId]', N'FAN', 'COLUMN';

EXEC sp_rename N'[FaydaVerifications].[AttemptCount]', N'ResendCount', 'COLUMN';

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FaydaVerifications]') AND [c].[name] = N'OTP');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [FaydaVerifications] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [FaydaVerifications] ALTER COLUMN [OTP] nvarchar(max) NULL;

ALTER TABLE [FaydaVerifications] ADD [Attempts] int NOT NULL DEFAULT 0;

ALTER TABLE [FaydaVerifications] ADD [ExpiryTime] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [FaydaVerifications] ADD [TransactionId] nvarchar(max) NOT NULL DEFAULT N'';

CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [PerformedBy] int NOT NULL,
    [Reason] nvarchar(max) NULL,
    [Timestamp] datetime2 NOT NULL,
    [IpAddress] nvarchar(max) NULL,
    [UserAgent] nvarchar(max) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260410063700_AddAuditLogsAndRefineFayda', N'9.0.2');

ALTER TABLE [FaydaVerifications] ADD [UserEmail] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260410071108_AddUserEmailToVerification', N'9.0.2');

CREATE INDEX [IX_AuditLogs_PerformedBy] ON [AuditLogs] ([PerformedBy]);

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);

ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users_PerformedBy] FOREIGN KEY ([PerformedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260410074358_AddAuditLogNavPropertiesFixed', N'9.0.2');

CREATE TABLE [EmailLogs] (
    [Id] int NOT NULL IDENTITY,
    [To] nvarchar(max) NOT NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [ReferenceId] nvarchar(max) NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [SentAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260411041034_AddEmailLog', N'9.0.2');

ALTER TABLE [FaydaVerifications] DROP CONSTRAINT [PK_FaydaVerifications];

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FaydaVerifications]') AND [c].[name] = N'Id');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [FaydaVerifications] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [FaydaVerifications] DROP COLUMN [Id];

ALTER TABLE [FaydaVerifications] ADD [VerifiedDob] datetime2 NULL;

ALTER TABLE [FaydaVerifications] ADD [VerifiedName] nvarchar(max) NULL;

ALTER TABLE [FaydaVerifications] ADD [VerifiedPhone] nvarchar(max) NULL;

ALTER TABLE [FaydaVerifications] ADD CONSTRAINT [PK_FaydaVerifications] PRIMARY KEY ([FAN]);

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_FaydaVerifications_FAN] FOREIGN KEY ([FAN]) REFERENCES [FaydaVerifications] ([FAN]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260411050546_FixFaydaVerificationPK', N'9.0.2');

ALTER TABLE [Users] ADD [ApprovalStatusMessage] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [ApprovalStatusType] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260411084326_AddInAppNotificationFields', N'9.0.2');

CREATE TABLE [SupplierCategories] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [AssociatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierCategories_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SupplierCategories_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_SupplierCategories_CategoryId] ON [SupplierCategories] ([CategoryId]);

CREATE INDEX [IX_SupplierCategories_SupplierId] ON [SupplierCategories] ([SupplierId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260411090907_AddSupplierCategoryMapping', N'9.0.2');

EXEC sp_rename N'[Ratings].[RatingScore]', N'RatingValue', 'COLUMN';

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ratings]') AND [c].[name] = N'Comment');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Ratings] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Ratings] ALTER COLUMN [Comment] nvarchar(1000) NULL;

ALTER TABLE [Ratings] ADD [Category] nvarchar(50) NULL;

ALTER TABLE [Ratings] ADD [HelpfulCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Ratings] ADD [IsVerifiedPurchase] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Ratings] ADD [NotHelpfulCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Ratings] ADD [OrderId] int NOT NULL DEFAULT 0;

ALTER TABLE [Ratings] ADD [UpdatedAt] datetime2 NULL;

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderStatusHistories]') AND [c].[name] = N'Status');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [OrderStatusHistories] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [OrderStatusHistories] ALTER COLUMN [Status] nvarchar(50) NOT NULL;

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderStatusHistories]') AND [c].[name] = N'Comments');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [OrderStatusHistories] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [OrderStatusHistories] ALTER COLUMN [Comments] nvarchar(500) NULL;

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderStatusHistories]') AND [c].[name] = N'ChangedByUserId');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [OrderStatusHistories] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [OrderStatusHistories] ALTER COLUMN [ChangedByUserId] int NULL;

ALTER TABLE [Orders] ADD [QRCodeValue] nvarchar(200) NULL;

ALTER TABLE [Deliveries] ADD [CustomerQRCode] nvarchar(max) NULL;

ALTER TABLE [Deliveries] ADD [IsQRVerified] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Deliveries] ADD [QRVerificationMethod] nvarchar(max) NULL;

ALTER TABLE [Deliveries] ADD [QRVerifiedAt] datetime2 NULL;

ALTER TABLE [Commissions] ADD [PaymentType] nvarchar(30) NOT NULL DEFAULT N'';

ALTER TABLE [Commissions] ADD [RetailerId] int NULL;

CREATE TABLE [ReturnRequests] (
    [Id] int NOT NULL IDENTITY,
    [ReturnNumber] nvarchar(50) NOT NULL,
    [OrderId] int NOT NULL,
    [PurchaseOrderId] int NOT NULL,
    [RetailerId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [Reason] nvarchar(20) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Images] nvarchar(500) NULL,
    [RefundAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [RefundMethod] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ApprovedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [AdminNotes] nvarchar(500) NULL,
    [RejectionReason] nvarchar(500) NULL,
    [IsReturnLabelGenerated] bit NOT NULL,
    [TrackingNumber] nvarchar(max) NULL,
    [ItemsShippedAt] datetime2 NULL,
    [ItemsReceivedAt] datetime2 NULL,
    CONSTRAINT [PK_ReturnRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReturnRequests_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReturnRequests_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReturnRequests_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReturnRequests_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Ratings_OrderId] ON [Ratings] ([OrderId]);

CREATE INDEX [IX_Commissions_RetailerId] ON [Commissions] ([RetailerId]);

CREATE INDEX [IX_ReturnRequests_OrderId] ON [ReturnRequests] ([OrderId]);

CREATE INDEX [IX_ReturnRequests_PurchaseOrderId] ON [ReturnRequests] ([PurchaseOrderId]);

CREATE INDEX [IX_ReturnRequests_RetailerId] ON [ReturnRequests] ([RetailerId]);

CREATE INDEX [IX_ReturnRequests_SupplierId] ON [ReturnRequests] ([SupplierId]);

ALTER TABLE [Commissions] ADD CONSTRAINT [FK_Commissions_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]);

ALTER TABLE [Ratings] ADD CONSTRAINT [FK_Ratings_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260411091758_AddReturnNavigationProperties', N'9.0.2');

ALTER TABLE [ProductCategories] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE TABLE [RetailerCategories] (
    [Id] int NOT NULL IDENTITY,
    [RetailerId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [AssociatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RetailerCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetailerCategories_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RetailerCategories_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RetailerCategories_CategoryId] ON [RetailerCategories] ([CategoryId]);

CREATE INDEX [IX_RetailerCategories_RetailerId] ON [RetailerCategories] ([RetailerId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260411110420_AddRetailerCategories', N'9.0.2');

ALTER TABLE [ProductCategories] ADD [Level] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260411162128_AddCategoryLevel', N'9.0.2');

ALTER TABLE [TenderBids] ADD [AfterSalesSupport] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [InsuranceCoverage] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [ProductSpecifications] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [QualityCertifications] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [References] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260416063426_AddMoreBidFields', N'9.0.2');

ALTER TABLE [Suppliers] ADD [CommissionTier] nvarchar(20) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418071401_PaymentFlowUpdate', N'9.0.2');

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CartItems]') AND [c].[name] = N'ProductId');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [CartItems] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [CartItems] ALTER COLUMN [ProductId] int NULL;

ALTER TABLE [CartItems] ADD [Description] nvarchar(max) NULL;

ALTER TABLE [CartItems] ADD [ProductName] nvarchar(100) NULL;

ALTER TABLE [CartItems] ADD [UnitPrice] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418073032_UpdateCartItemForProductName', N'9.0.2');

ALTER TABLE [Warehouses] ADD [AssignedManagerId] int NULL;

ALTER TABLE [Warehouses] ADD [CapacityUsed] int NULL;

ALTER TABLE [Warehouses] ADD [ContactPersonName] nvarchar(100) NULL;

ALTER TABLE [Warehouses] ADD [ContactPhone] nvarchar(20) NULL;

ALTER TABLE [Warehouses] ADD [EmergencyContact] nvarchar(20) NULL;

ALTER TABLE [Warehouses] ADD [LastInventoryCount] datetime2 NULL;

ALTER TABLE [Warehouses] ADD [OperatingHoursFrom] time NULL;

ALTER TABLE [Warehouses] ADD [OperatingHoursTo] time NULL;

ALTER TABLE [Vehicles] ADD [AssignedDriverId] int NULL;

ALTER TABLE [Vehicles] ADD [FuelEfficiency] decimal(18,2) NULL;

ALTER TABLE [Vehicles] ADD [FuelType] nvarchar(20) NULL;

ALTER TABLE [Vehicles] ADD [LastServiceDate] datetime2 NULL;

ALTER TABLE [Vehicles] ADD [Mileage] decimal(18,2) NULL;

ALTER TABLE [Vehicles] ADD [NextServiceDueDate] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418203846_AddLogisticsTrackingFields', N'9.0.2');

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SupplierEmployees]') AND [c].[name] = N'IsLicenseVerified');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [SupplierEmployees] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [SupplierEmployees] DROP COLUMN [IsLicenseVerified];

EXEC sp_rename N'[Warehouses].[StorageType]', N'StorageArchitecture', 'COLUMN';

EXEC sp_rename N'[Warehouses].[HandlingTimeHours]', N'HubType', 'COLUMN';

EXEC sp_rename N'[Warehouses].[AssignedManagerId]', N'LoadingBays', 'COLUMN';

EXEC sp_rename N'[Vehicles].[VolumeCapacity]', N'PurchaseCost', 'COLUMN';

EXEC sp_rename N'[Vehicles].[RoadworthinessStatus]', N'Model', 'COLUMN';

EXEC sp_rename N'[Vehicles].[RegistrationNumber]', N'Color', 'COLUMN';

EXEC sp_rename N'[Vehicles].[LastMaintenanceDate]', N'TireChangeDue', 'COLUMN';

EXEC sp_rename N'[Vehicles].[InsuranceStatus]', N'AssetCode', 'COLUMN';

EXEC sp_rename N'[Vehicles].[HasTemperatureControl]', N'TemperatureControlled', 'COLUMN';

EXEC sp_rename N'[Vehicles].[AssignedDriverId]', N'ManufactureYear', 'COLUMN';

EXEC sp_rename N'[SupplierEmployees].[LicenseExpiryDate]', N'UpdatedAt', 'COLUMN';

EXEC sp_rename N'[SupplierEmployees].[DrivingLicenseNumber]', N'EmergencyContact', 'COLUMN';

ALTER TABLE [Warehouses] ADD [AvgProcessingTimeHours] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [CCTVEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [CreatedBy] nvarchar(max) NULL;

ALTER TABLE [Warehouses] ADD [Email] nvarchar(100) NULL;

ALTER TABLE [Warehouses] ADD [FireSafetyInstalled] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [ForkliftsAvailable] int NULL;

ALTER TABLE [Warehouses] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [Landmark] nvarchar(200) NULL;

ALTER TABLE [Warehouses] ADD [Latitude] decimal(10,8) NULL;

ALTER TABLE [Warehouses] ADD [Longitude] decimal(11,8) NULL;

ALTER TABLE [Warehouses] ADD [SubCityZone] nvarchar(100) NULL;

ALTER TABLE [Warehouses] ADD [UpdatedBy] nvarchar(max) NULL;

ALTER TABLE [Warehouses] ADD [WorkingDays] nvarchar(100) NULL;

ALTER TABLE [Vehicles] ADD [Brand] nvarchar(100) NULL;

ALTER TABLE [Vehicles] ADD [CreatedBy] nvarchar(max) NULL;

ALTER TABLE [Vehicles] ADD [CurrentEstimatedValue] decimal(18,2) NULL;

ALTER TABLE [Vehicles] ADD [FuelTankCapacity] decimal(18,2) NULL;

ALTER TABLE [Vehicles] ADD [GPSInstalled] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Vehicles] ADD [InternalVolumeM3] decimal(18,2) NULL;

ALTER TABLE [Vehicles] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Vehicles] ADD [PurchaseDate] datetime2 NULL;

ALTER TABLE [Vehicles] ADD [RegistrationExpiryDate] datetime2 NULL;

ALTER TABLE [Vehicles] ADD [UpdatedBy] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [CreatedBy] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [DateOfBirth] datetime2 NULL;

ALTER TABLE [SupplierEmployees] ADD [EmploymentType] int NOT NULL DEFAULT 0;

ALTER TABLE [SupplierEmployees] ADD [Gender] nvarchar(20) NULL;

ALTER TABLE [SupplierEmployees] ADD [JoinDate] datetime2 NULL;

ALTER TABLE [SupplierEmployees] ADD [NationalID] nvarchar(50) NULL;

ALTER TABLE [SupplierEmployees] ADD [Shift] int NOT NULL DEFAULT 0;

ALTER TABLE [SupplierEmployees] ADD [UpdatedBy] nvarchar(max) NULL;

CREATE TABLE [DriverProfiles] (
    [Id] int NOT NULL IDENTITY,
    [SupplierEmployeeId] int NOT NULL,
    [DrivingLicenseNumber] nvarchar(100) NULL,
    [LicenseType] nvarchar(50) NULL,
    [LicenseIssueDate] datetime2 NULL,
    [LicenseExpiryDate] datetime2 NULL,
    [MedicalFitnessExpiry] datetime2 NULL,
    [DeliveryRegion] nvarchar(100) NULL,
    [CityCoverage] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_DriverProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DriverProfiles_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VehicleAssignments] (
    [Id] int NOT NULL IDENTITY,
    [VehicleId] int NOT NULL,
    [SupplierEmployeeId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [IsPrimary] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_VehicleAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VehicleAssignments_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_VehicleAssignments_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [WarehouseAssignments] (
    [Id] int NOT NULL IDENTITY,
    [WarehouseId] int NOT NULL,
    [SupplierEmployeeId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [IsPrimary] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_WarehouseAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WarehouseAssignments_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WarehouseAssignments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [WarehouseProfiles] (
    [Id] int NOT NULL IDENTITY,
    [SupplierEmployeeId] int NOT NULL,
    [CanApproveTransfers] bit NOT NULL,
    [CanManageInventory] bit NOT NULL,
    [CanViewReports] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_WarehouseProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WarehouseProfiles_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_DriverProfiles_SupplierEmployeeId] ON [DriverProfiles] ([SupplierEmployeeId]);

CREATE INDEX [IX_VehicleAssignments_SupplierEmployeeId] ON [VehicleAssignments] ([SupplierEmployeeId]);

CREATE INDEX [IX_VehicleAssignments_VehicleId] ON [VehicleAssignments] ([VehicleId]);

CREATE INDEX [IX_WarehouseAssignments_SupplierEmployeeId] ON [WarehouseAssignments] ([SupplierEmployeeId]);

CREATE INDEX [IX_WarehouseAssignments_WarehouseId] ON [WarehouseAssignments] ([WarehouseId]);

CREATE UNIQUE INDEX [IX_WarehouseProfiles_SupplierEmployeeId] ON [WarehouseProfiles] ([SupplierEmployeeId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260419164051_ProfessionalSCMArchitecture', N'9.0.2');

ALTER TABLE [Warehouses] ADD [Timezone] nvarchar(50) NULL;

ALTER TABLE [Warehouses] ADD [WeekendDays] nvarchar(100) NULL;

ALTER TABLE [Vehicles] ADD [InsuranceCertificateUrl] nvarchar(max) NULL;

ALTER TABLE [Vehicles] ADD [RegistrationCertificateUrl] nvarchar(max) NULL;

ALTER TABLE [Vehicles] ADD [VehiclePhotosUrls] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [ContractDocumentUrl] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [IdDocumentUrl] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [PhotoUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260419165542_AddOperationalAttachments', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260419171033_Phase5_FileUploadsAndLogistics', N'9.0.2');

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'ContactPersonName');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [ContactPersonName];

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'ContactPhone');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [ContactPhone];

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'Email');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [Email];

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'EmergencyContact');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var18 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [EmergencyContact];

ALTER TABLE [Warehouses] ADD [PrimaryManagerId] int NULL;

ALTER TABLE [Vehicles] ADD [PrimaryDriverId] int NULL;

CREATE INDEX [IX_Warehouses_PrimaryManagerId] ON [Warehouses] ([PrimaryManagerId]);

CREATE INDEX [IX_Vehicles_PrimaryDriverId] ON [Vehicles] ([PrimaryDriverId]);

ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_SupplierEmployees_PrimaryDriverId] FOREIGN KEY ([PrimaryDriverId]) REFERENCES [SupplierEmployees] ([Id]);

ALTER TABLE [Warehouses] ADD CONSTRAINT [FK_Warehouses_SupplierEmployees_PrimaryManagerId] FOREIGN KEY ([PrimaryManagerId]) REFERENCES [SupplierEmployees] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260420185245_AddPrimaryAssignmentsToAssets', N'9.0.2');

ALTER TABLE [Warehouses] ADD [CoverageRegions] nvarchar(500) NULL;

ALTER TABLE [Warehouses] ADD [CurrentWorkload] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [MaxDeliveryDistanceKM] int NOT NULL DEFAULT 0;

ALTER TABLE [SupplierEmployees] ADD [Department] nvarchar(100) NULL;

ALTER TABLE [SupplierEmployees] ADD [EmployeeDisplayId] nvarchar(50) NULL;

ALTER TABLE [SupplierEmployees] ADD [ForcePasswordChange] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [Status] int NOT NULL DEFAULT 0;

ALTER TABLE [Orders] ADD [DeliveryCity] nvarchar(100) NULL;

ALTER TABLE [Orders] ADD [DeliveryRegion] nvarchar(100) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421052248_AddDeliveryCityAndRegionToOrder', N'9.0.2');

ALTER TABLE [Vehicles] ADD [CurrentMileage] decimal(18,2) NULL;

ALTER TABLE [Vehicles] ADD [WarehouseId] int NULL;

ALTER TABLE [DriverProfiles] ADD [CoverageArea] nvarchar(500) NULL;

CREATE INDEX [IX_Vehicles_WarehouseId] ON [Vehicles] ([WarehouseId]);

ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421074003_EnterpriseLogisticsOverhaul', N'9.0.2');

EXEC sp_rename N'[SupplierEmployees].[PhotoUrl]', N'ProfilePhotoPath', 'COLUMN';

ALTER TABLE [Warehouses] ADD [PhotoPath] nvarchar(max) NULL;

ALTER TABLE [Vehicles] ADD [PhotoPath] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421092341_FinalLogisticsSync', N'9.0.2');

EXEC sp_rename N'[SupplierEmployees].[EmergencyContact]', N'EmergencyContactName', 'COLUMN';

ALTER TABLE [Warehouses] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Warehouses] ADD [HasBackupPower] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [HasInternet] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [HazardStorageAllowed] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [OccupancyStatus] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [OverflowWarningThreshold] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [PackingStationsCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [ReceivingAreaSizeM2] decimal(18,2) NULL;

ALTER TABLE [Warehouses] ADD [ReservedSpace] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [TemperatureZoneTypes] nvarchar(max) NULL;

ALTER TABLE [Vehicles] ADD [AccidentHistoryNote] nvarchar(max) NULL;

ALTER TABLE [Vehicles] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Vehicles] ADD [DriverEligibilityType] nvarchar(max) NULL;

ALTER TABLE [Vehicles] ADD [FuelCardNumber] nvarchar(50) NULL;

ALTER TABLE [Vehicles] ADD [InsuranceProvider] nvarchar(100) NULL;

ALTER TABLE [Vehicles] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Vehicles] ADD [ServiceIntervalMonths] int NOT NULL DEFAULT 0;

ALTER TABLE [Vehicles] ADD [TireChangeDueMileage] decimal(18,2) NULL;

ALTER TABLE [SupplierEmployees] ADD [AllowedLoginZones] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [BloodGroup] nvarchar(10) NULL;

ALTER TABLE [SupplierEmployees] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [SupplierEmployees] ADD [DeviceAccessRestriction] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [EmergencyContactPhone] nvarchar(20) NULL;

ALTER TABLE [SupplierEmployees] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [RequireMFA] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [RolePermissions] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [SalaryGrade] nvarchar(20) NULL;

ALTER TABLE [SupplierEmployees] ADD [SupervisorId] int NULL;

CREATE TABLE [DispatchTasks] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [VehicleId] int NULL,
    [DeliveryAgentId] int NULL,
    [HubId] int NULL,
    [RouteName] nvarchar(200) NULL,
    [PlannedDeparture] datetime2 NULL,
    [ActualDeparture] datetime2 NULL,
    [EstimatedArrival] datetime2 NULL,
    [ActualArrival] datetime2 NULL,
    [Status] nvarchar(50) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [RecipientName] nvarchar(max) NULL,
    [SignaturePath] nvarchar(max) NULL,
    [DeliveryPhotoPath] nvarchar(max) NULL,
    [DeliveryLat] decimal(18,2) NULL,
    [DeliveryLong] decimal(18,2) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_DispatchTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DispatchTasks_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DispatchTasks_SupplierEmployees_DeliveryAgentId] FOREIGN KEY ([DeliveryAgentId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DispatchTasks_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]),
    CONSTRAINT [FK_DispatchTasks_Warehouses_HubId] FOREIGN KEY ([HubId]) REFERENCES [Warehouses] ([Id])
);

CREATE TABLE [EmployeeDocuments] (
    [Id] int NOT NULL IDENTITY,
    [SupplierEmployeeId] int NOT NULL,
    [DocumentType] nvarchar(100) NOT NULL,
    [DocumentName] nvarchar(100) NOT NULL,
    [DocumentUrl] nvarchar(max) NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [IssueDate] datetime2 NULL,
    [DocumentNumber] nvarchar(100) NULL,
    [IsVerified] bit NOT NULL,
    [VerifiedAt] datetime2 NULL,
    [VerifiedBy] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeDocuments_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EmployeeWarehouseAccesses] (
    [Id] int NOT NULL IDENTITY,
    [SupplierEmployeeId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [PermissionLevel] nvarchar(50) NOT NULL,
    [CanApproveDispatch] bit NOT NULL,
    [CanManageStock] bit NOT NULL,
    [GrantedAt] datetime2 NOT NULL,
    [GrantedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeWarehouseAccesses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeWarehouseAccesses_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmployeeWarehouseAccesses_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [GPSLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [VehicleId] int NOT NULL,
    [Latitude] decimal(10,8) NOT NULL,
    [Longitude] decimal(11,8) NOT NULL,
    [SpeedKph] decimal(18,2) NULL,
    [NearestAddress] nvarchar(100) NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_GPSLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GPSLogs_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [InventoryTransfers] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [SourceWarehouseId] int NOT NULL,
    [DestinationWarehouseId] int NOT NULL,
    [ProductId] int NULL,
    [Quantity] int NOT NULL,
    [Status] int NOT NULL,
    [RequestedById] int NULL,
    [ApprovedById] int NULL,
    [RequestedDate] datetime2 NULL,
    [CompletionDate] datetime2 NULL,
    [Remarks] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryTransfers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransfers_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
    CONSTRAINT [FK_InventoryTransfers_SupplierEmployees_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_SupplierEmployees_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InventoryTransfers_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_Warehouses_SourceWarehouseId] FOREIGN KEY ([SourceWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MaintenanceRecords] (
    [Id] int NOT NULL IDENTITY,
    [VehicleId] int NOT NULL,
    [ServiceDate] datetime2 NOT NULL,
    [OdometerAtService] decimal(18,2) NOT NULL,
    [ServiceType] nvarchar(100) NOT NULL,
    [TotalCost] decimal(18,2) NOT NULL,
    [ServiceProvider] nvarchar(200) NULL,
    [Description] nvarchar(500) NULL,
    [NextServiceDue] datetime2 NOT NULL,
    [NextServiceMileage] decimal(18,2) NOT NULL,
    [InvoiceDocumentUrl] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaintenanceRecords_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VehicleDocuments] (
    [Id] int NOT NULL IDENTITY,
    [VehicleId] int NOT NULL,
    [DocumentType] nvarchar(100) NOT NULL,
    [DocumentName] nvarchar(100) NOT NULL,
    [DocumentUrl] nvarchar(max) NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [IssueDate] datetime2 NULL,
    [IssuingAuthority] nvarchar(100) NULL,
    [IsVerified] bit NOT NULL,
    [VerifiedAt] datetime2 NULL,
    [VerifiedBy] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_VehicleDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VehicleDocuments_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VehicleDriverHistories] (
    [Id] int NOT NULL IDENTITY,
    [VehicleId] int NOT NULL,
    [SupplierEmployeeId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [ChangeReason] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_VehicleDriverHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VehicleDriverHistories_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_VehicleDriverHistories_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [WarehouseManagerHistories] (
    [Id] int NOT NULL IDENTITY,
    [WarehouseId] int NOT NULL,
    [SupplierEmployeeId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [ChangeReason] nvarchar(200) NULL,
    [IsPrimary] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WarehouseManagerHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WarehouseManagerHistories_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WarehouseManagerHistories_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [IncidentReports] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [ReportedById] int NOT NULL,
    [VehicleId] int NULL,
    [WarehouseId] int NULL,
    [DispatchTaskId] int NULL,
    [Type] int NOT NULL,
    [Severity] int NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [PhotoUrl] nvarchar(max) NULL,
    [Lat] decimal(18,2) NULL,
    [Long] decimal(18,2) NULL,
    [Status] nvarchar(50) NOT NULL,
    [ResolutionNotes] nvarchar(max) NULL,
    [ObservedAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_IncidentReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IncidentReports_DispatchTasks_DispatchTaskId] FOREIGN KEY ([DispatchTaskId]) REFERENCES [DispatchTasks] ([Id]),
    CONSTRAINT [FK_IncidentReports_SupplierEmployees_ReportedById] FOREIGN KEY ([ReportedById]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_IncidentReports_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_IncidentReports_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]),
    CONSTRAINT [FK_IncidentReports_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id])
);

CREATE INDEX [IX_SupplierEmployees_SupervisorId] ON [SupplierEmployees] ([SupervisorId]);

CREATE INDEX [IX_DispatchTasks_DeliveryAgentId] ON [DispatchTasks] ([DeliveryAgentId]);

CREATE INDEX [IX_DispatchTasks_HubId] ON [DispatchTasks] ([HubId]);

CREATE INDEX [IX_DispatchTasks_OrderId] ON [DispatchTasks] ([OrderId]);

CREATE INDEX [IX_DispatchTasks_VehicleId] ON [DispatchTasks] ([VehicleId]);

CREATE INDEX [IX_EmployeeDocuments_SupplierEmployeeId] ON [EmployeeDocuments] ([SupplierEmployeeId]);

CREATE INDEX [IX_EmployeeWarehouseAccesses_SupplierEmployeeId] ON [EmployeeWarehouseAccesses] ([SupplierEmployeeId]);

CREATE INDEX [IX_EmployeeWarehouseAccesses_WarehouseId] ON [EmployeeWarehouseAccesses] ([WarehouseId]);

CREATE INDEX [IX_GPSLogs_VehicleId] ON [GPSLogs] ([VehicleId]);

CREATE INDEX [IX_IncidentReports_DispatchTaskId] ON [IncidentReports] ([DispatchTaskId]);

CREATE INDEX [IX_IncidentReports_ReportedById] ON [IncidentReports] ([ReportedById]);

CREATE INDEX [IX_IncidentReports_SupplierId] ON [IncidentReports] ([SupplierId]);

CREATE INDEX [IX_IncidentReports_VehicleId] ON [IncidentReports] ([VehicleId]);

CREATE INDEX [IX_IncidentReports_WarehouseId] ON [IncidentReports] ([WarehouseId]);

CREATE INDEX [IX_InventoryTransfers_ApprovedById] ON [InventoryTransfers] ([ApprovedById]);

CREATE INDEX [IX_InventoryTransfers_DestinationWarehouseId] ON [InventoryTransfers] ([DestinationWarehouseId]);

CREATE INDEX [IX_InventoryTransfers_ProductId] ON [InventoryTransfers] ([ProductId]);

CREATE INDEX [IX_InventoryTransfers_RequestedById] ON [InventoryTransfers] ([RequestedById]);

CREATE INDEX [IX_InventoryTransfers_SourceWarehouseId] ON [InventoryTransfers] ([SourceWarehouseId]);

CREATE INDEX [IX_InventoryTransfers_SupplierId] ON [InventoryTransfers] ([SupplierId]);

CREATE INDEX [IX_MaintenanceRecords_VehicleId] ON [MaintenanceRecords] ([VehicleId]);

CREATE INDEX [IX_VehicleDocuments_VehicleId] ON [VehicleDocuments] ([VehicleId]);

CREATE INDEX [IX_VehicleDriverHistories_SupplierEmployeeId] ON [VehicleDriverHistories] ([SupplierEmployeeId]);

CREATE INDEX [IX_VehicleDriverHistories_VehicleId] ON [VehicleDriverHistories] ([VehicleId]);

CREATE INDEX [IX_WarehouseManagerHistories_SupplierEmployeeId] ON [WarehouseManagerHistories] ([SupplierEmployeeId]);

CREATE INDEX [IX_WarehouseManagerHistories_WarehouseId] ON [WarehouseManagerHistories] ([WarehouseId]);

ALTER TABLE [SupplierEmployees] ADD CONSTRAINT [FK_SupplierEmployees_SupplierEmployees_SupervisorId] FOREIGN KEY ([SupervisorId]) REFERENCES [SupplierEmployees] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421132048_Logistics2FullERP', N'9.0.2');

ALTER TABLE [AuditLogs] DROP CONSTRAINT [FK_AuditLogs_Users_PerformedBy];

ALTER TABLE [AuditLogs] DROP CONSTRAINT [FK_AuditLogs_Users_UserId];

DROP INDEX [IX_AuditLogs_PerformedBy] ON [AuditLogs];

DROP INDEX [IX_AuditLogs_UserId] ON [AuditLogs];

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'PerformedBy');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [AuditLogs] DROP COLUMN [PerformedBy];

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'UserId');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT [' + @var20 + '];');
ALTER TABLE [AuditLogs] DROP COLUMN [UserId];

EXEC sp_rename N'[AuditLogs].[Timestamp]', N'PerformedAtUtc', 'COLUMN';

EXEC sp_rename N'[AuditLogs].[Reason]', N'OldValueJson', 'COLUMN';

EXEC sp_rename N'[AuditLogs].[Action]', N'EntityId', 'COLUMN';

ALTER TABLE [SupplierEmployees] ADD [MonthlySalary] decimal(18,2) NULL;

ALTER TABLE [Notifications] ADD [TargetRole] nvarchar(50) NULL;

ALTER TABLE [Notifications] ADD [TargetWarehouseId] int NULL;

ALTER TABLE [AuditLogs] ADD [ActionType] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [AuditLogs] ADD [EntityType] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [AuditLogs] ADD [NewValueJson] nvarchar(max) NULL;

ALTER TABLE [AuditLogs] ADD [Notes] nvarchar(max) NULL;

ALTER TABLE [AuditLogs] ADD [PerformedByUserId] int NULL;

CREATE INDEX [IX_AuditLogs_PerformedByUserId] ON [AuditLogs] ([PerformedByUserId]);

ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421161826_EnterpriseSchemaSync', N'9.0.2');

ALTER TABLE [InventoryTransfers] ADD [ApprovedDate] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421185355_AddApprovedDateToInventoryTransfer', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260422202019_AddApprovalStatusFields', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424101322_AddItemDetailsToOrders', N'9.0.2');

DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'TireChangeDue');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var21 + '];');
ALTER TABLE [Vehicles] DROP COLUMN [TireChangeDue];

DECLARE @var22 sysname;
SELECT @var22 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'TireChangeDueMileage');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var22 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [TireChangeDueMileage] int NULL;

DECLARE @var23 sysname;
SELECT @var23 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'Mileage');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var23 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [Mileage] int NULL;

DECLARE @var24 sysname;
SELECT @var24 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'CurrentMileage');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var24 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [CurrentMileage] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424181332_FixVehiclePropertyTypes', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424201928_SyncAllModelsToDatabase_Fix', N'9.0.2');

COMMIT;
GO

