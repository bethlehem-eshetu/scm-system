IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [ProductCategories] (
    [Id] int NOT NULL IDENTITY,
    [CategoryName] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ParentCategoryId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductCategories_ProductCategories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [AccountStatus] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [IsApproved] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [LastLoginAt] datetime2 NULL,
    [LoginAttempts] int NULL DEFAULT 0,
    [EmailVerified] bit NOT NULL,
    [PhoneVerified] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [ProductAttributeDefinitions] (
    [Id] int NOT NULL IDENTITY,
    [CategoryId] int NOT NULL,
    [AttributeName] nvarchar(100) NOT NULL,
    [DataType] nvarchar(20) NOT NULL,
    [Unit] nvarchar(20) NOT NULL,
    [IsRequired] bit NOT NULL,
    CONSTRAINT [PK_ProductAttributeDefinitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductAttributeDefinitions_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Title] nvarchar(100) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [ActionUrl] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Penalties] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [PenaltyType] nvarchar(20) NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Penalties] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Penalties_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Retailers] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [BusinessName] nvarchar(150) NOT NULL,
    [BusinessType] nvarchar(50) NOT NULL,
    [TaxIdentificationNumber] nvarchar(50) NOT NULL,
    [BusinessLicenseNumber] nvarchar(100) NOT NULL,
    [BusinessAddress] nvarchar(200) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Country] nvarchar(100) NOT NULL,
    [StoreSize] nvarchar(20) NOT NULL,
    [BusinessLogo] nvarchar(255) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsVerified] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Retailers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Retailers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [CompanyName] nvarchar(150) NOT NULL,
    [BusinessType] nvarchar(50) NOT NULL,
    [LicenseNumber] nvarchar(100) NOT NULL,
    [LicenseFilePath] nvarchar(255) NOT NULL,
    [TaxIdentificationNumber] nvarchar(50) NOT NULL,
    [CompanyAddress] nvarchar(200) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Country] nvarchar(100) NOT NULL,
    [Website] nvarchar(255) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [VerificationStatus] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Suppliers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Conversations] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [RetailerId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastMessageAt] datetime2 NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Conversations_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Conversations_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [ProductName] nvarchar(150) NOT NULL,
    [BasePrice] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [SKU] nvarchar(50) NOT NULL,
    [ImageUrl] nvarchar(255) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SupplierEmployees] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [EmployeeRole] nvarchar(50) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SupplierEmployees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierEmployees_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupplierEmployees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Tenders] (
    [Id] int NOT NULL IDENTITY,
    [RetailerId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ClosingDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SupplierId] int NULL,
    CONSTRAINT [PK_Tenders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tenders_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenders_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
);

CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [City] nvarchar(50) NOT NULL,
    [Capacity] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Warehouses_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Messages] (
    [Id] int NOT NULL IDENTITY,
    [ConversationId] int NOT NULL,
    [SenderId] int NOT NULL,
    [MessageText] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProductAttributeValues] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [AttributeId] int NOT NULL,
    [Value] nvarchar(255) NOT NULL,
    CONSTRAINT [PK_ProductAttributeValues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductAttributeValues_ProductAttributeDefinitions_AttributeId] FOREIGN KEY ([AttributeId]) REFERENCES [ProductAttributeDefinitions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProductAttributeValues_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TenderBids] (
    [Id] int NOT NULL IDENTITY,
    [TenderId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [BidAmount] decimal(18,2) NOT NULL,
    [DeliveryTimeline] nvarchar(100) NOT NULL,
    [BidNotes] nvarchar(max) NOT NULL,
    [SubmittedDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_TenderBids] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenderBids_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenderBids_Tenders_TenderId] FOREIGN KEY ([TenderId]) REFERENCES [Tenders] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TenderItems] (
    [Id] int NOT NULL IDENTITY,
    [TenderId] int NOT NULL,
    [ProductName] nvarchar(150) NOT NULL,
    [Quantity] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_TenderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenderItems_Tenders_TenderId] FOREIGN KEY ([TenderId]) REFERENCES [Tenders] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Inventories] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [QuantityOnHand] int NOT NULL,
    [QuantityReserved] int NOT NULL,
    [QuantityAvailable] int NOT NULL,
    [WarehouseLocation] nvarchar(100) NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    [WarehouseId] int NULL,
    CONSTRAINT [PK_Inventories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Inventories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Inventories_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [MessageViolations] (
    [Id] int NOT NULL IDENTITY,
    [MessageId] int NOT NULL,
    [ViolationType] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsResolved] bit NOT NULL,
    CONSTRAINT [PK_MessageViolations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageViolations_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PurchaseOrders] (
    [Id] int NOT NULL IDENTITY,
    [PONumber] nvarchar(50) NOT NULL,
    [RetailerId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [TenderBidId] int NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_TenderBids_TenderBidId] FOREIGN KEY ([TenderBidId]) REFERENCES [TenderBids] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [Commissions] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [CommissionAmount] decimal(18,2) NOT NULL,
    [ChapaTransactionId] nvarchar(100) NOT NULL,
    [PaymentRequestData] nvarchar(max) NOT NULL,
    [PaymentVerificationData] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Commissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Commissions_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Commissions_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [OrderStatus] nvarchar(20) NOT NULL DEFAULT N'Processing',
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [RetailerId] int NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]),
    CONSTRAINT [FK_Orders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PurchaseOrderItems] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Ratings] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderId] int NOT NULL,
    [RetailerId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [RatingScore] int NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Ratings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Ratings_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Ratings_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Ratings_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Deliveries] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [DeliveryEmployeeId] int NULL,
    [TrackingNumber] nvarchar(50) NOT NULL,
    [Carrier] nvarchar(50) NOT NULL,
    [DeliveryStatus] nvarchar(20) NOT NULL DEFAULT N'Preparing',
    [DepartureTime] datetime2 NULL,
    [ArrivalTime] datetime2 NULL,
    [DeliveredDate] datetime2 NULL,
    [ProofOfDelivery] nvarchar(255) NOT NULL,
    CONSTRAINT [PK_Deliveries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Deliveries_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Deliveries_SupplierEmployees_DeliveryEmployeeId] FOREIGN KEY ([DeliveryEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrderStatusHistories] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [ChangedBy] int NOT NULL,
    [ChangedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DeliveryTrackings] (
    [Id] int NOT NULL IDENTITY,
    [DeliveryId] int NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [StatusNote] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_DeliveryTrackings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeliveryTrackings_Deliveries_DeliveryId] FOREIGN KEY ([DeliveryId]) REFERENCES [Deliveries] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Commissions_PurchaseOrderId] ON [Commissions] ([PurchaseOrderId]);

CREATE INDEX [IX_Commissions_SupplierId] ON [Commissions] ([SupplierId]);

CREATE INDEX [IX_Conversations_RetailerId] ON [Conversations] ([RetailerId]);

CREATE UNIQUE INDEX [IX_Conversations_SupplierId_RetailerId] ON [Conversations] ([SupplierId], [RetailerId]);

CREATE INDEX [IX_Deliveries_DeliveryEmployeeId] ON [Deliveries] ([DeliveryEmployeeId]);

CREATE UNIQUE INDEX [IX_Deliveries_OrderId] ON [Deliveries] ([OrderId]);

CREATE INDEX [IX_DeliveryTrackings_DeliveryId] ON [DeliveryTrackings] ([DeliveryId]);

CREATE UNIQUE INDEX [IX_Inventories_ProductId] ON [Inventories] ([ProductId]);

CREATE INDEX [IX_Inventories_WarehouseId] ON [Inventories] ([WarehouseId]);

CREATE INDEX [IX_Messages_ConversationId] ON [Messages] ([ConversationId]);

CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);

CREATE UNIQUE INDEX [IX_MessageViolations_MessageId] ON [MessageViolations] ([MessageId]);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);

CREATE UNIQUE INDEX [IX_Orders_PurchaseOrderId] ON [Orders] ([PurchaseOrderId]);

CREATE INDEX [IX_Orders_RetailerId] ON [Orders] ([RetailerId]);

CREATE INDEX [IX_Orders_SupplierId] ON [Orders] ([SupplierId]);

CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);

CREATE INDEX [IX_Penalties_UserId] ON [Penalties] ([UserId]);

CREATE INDEX [IX_ProductAttributeDefinitions_CategoryId] ON [ProductAttributeDefinitions] ([CategoryId]);

CREATE INDEX [IX_ProductAttributeValues_AttributeId] ON [ProductAttributeValues] ([AttributeId]);

CREATE INDEX [IX_ProductAttributeValues_ProductId] ON [ProductAttributeValues] ([ProductId]);

CREATE INDEX [IX_ProductCategories_ParentCategoryId] ON [ProductCategories] ([ParentCategoryId]);

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);

CREATE UNIQUE INDEX [IX_Products_SKU] ON [Products] ([SKU]);

CREATE UNIQUE INDEX [IX_Products_SupplierId_ProductName] ON [Products] ([SupplierId], [ProductName]);

CREATE INDEX [IX_PurchaseOrderItems_ProductId] ON [PurchaseOrderItems] ([ProductId]);

CREATE INDEX [IX_PurchaseOrderItems_PurchaseOrderId] ON [PurchaseOrderItems] ([PurchaseOrderId]);

CREATE UNIQUE INDEX [IX_PurchaseOrders_PONumber] ON [PurchaseOrders] ([PONumber]);

CREATE INDEX [IX_PurchaseOrders_RetailerId] ON [PurchaseOrders] ([RetailerId]);

CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);

CREATE UNIQUE INDEX [IX_PurchaseOrders_TenderBidId] ON [PurchaseOrders] ([TenderBidId]) WHERE [TenderBidId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Ratings_PurchaseOrderId] ON [Ratings] ([PurchaseOrderId]);

CREATE INDEX [IX_Ratings_RetailerId] ON [Ratings] ([RetailerId]);

CREATE INDEX [IX_Ratings_SupplierId] ON [Ratings] ([SupplierId]);

CREATE UNIQUE INDEX [IX_Retailers_UserId] ON [Retailers] ([UserId]);

CREATE INDEX [IX_SupplierEmployees_SupplierId] ON [SupplierEmployees] ([SupplierId]);

CREATE UNIQUE INDEX [IX_SupplierEmployees_UserId] ON [SupplierEmployees] ([UserId]);

CREATE UNIQUE INDEX [IX_Suppliers_LicenseNumber] ON [Suppliers] ([LicenseNumber]);

CREATE UNIQUE INDEX [IX_Suppliers_TaxIdentificationNumber] ON [Suppliers] ([TaxIdentificationNumber]);

CREATE UNIQUE INDEX [IX_Suppliers_UserId] ON [Suppliers] ([UserId]);

CREATE INDEX [IX_TenderBids_SupplierId] ON [TenderBids] ([SupplierId]);

CREATE INDEX [IX_TenderBids_TenderId] ON [TenderBids] ([TenderId]);

CREATE INDEX [IX_TenderItems_TenderId] ON [TenderItems] ([TenderId]);

CREATE INDEX [IX_Tenders_CategoryId] ON [Tenders] ([CategoryId]);

CREATE INDEX [IX_Tenders_RetailerId] ON [Tenders] ([RetailerId]);

CREATE INDEX [IX_Tenders_SupplierId] ON [Tenders] ([SupplierId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE INDEX [IX_Warehouses_SupplierId] ON [Warehouses] ([SupplierId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310184119_InitialCompleteSchema', N'9.0.2');

DROP INDEX [IX_Suppliers_TaxIdentificationNumber] ON [Suppliers];

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Suppliers]') AND [c].[name] = N'Website');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Suppliers] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Suppliers] ALTER COLUMN [Website] nvarchar(255) NULL;

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Suppliers]') AND [c].[name] = N'TaxIdentificationNumber');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Suppliers] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Suppliers] ALTER COLUMN [TaxIdentificationNumber] nvarchar(50) NULL;

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Suppliers]') AND [c].[name] = N'LicenseFilePath');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Suppliers] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Suppliers] ALTER COLUMN [LicenseFilePath] nvarchar(255) NULL;

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Suppliers]') AND [c].[name] = N'Description');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Suppliers] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Suppliers] ALTER COLUMN [Description] nvarchar(max) NULL;

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Suppliers]') AND [c].[name] = N'BusinessType');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Suppliers] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Suppliers] ALTER COLUMN [BusinessType] nvarchar(50) NULL;

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retailers]') AND [c].[name] = N'TaxIdentificationNumber');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Retailers] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Retailers] ALTER COLUMN [TaxIdentificationNumber] nvarchar(50) NULL;

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retailers]') AND [c].[name] = N'StoreSize');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Retailers] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Retailers] ALTER COLUMN [StoreSize] nvarchar(20) NULL;

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retailers]') AND [c].[name] = N'Description');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Retailers] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Retailers] ALTER COLUMN [Description] nvarchar(max) NULL;

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retailers]') AND [c].[name] = N'BusinessType');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Retailers] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Retailers] ALTER COLUMN [BusinessType] nvarchar(50) NULL;

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retailers]') AND [c].[name] = N'BusinessLogo');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Retailers] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Retailers] ALTER COLUMN [BusinessLogo] nvarchar(255) NULL;

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retailers]') AND [c].[name] = N'BusinessLicenseNumber');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Retailers] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Retailers] ALTER COLUMN [BusinessLicenseNumber] nvarchar(100) NULL;

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Type');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [Notifications] ALTER COLUMN [Type] nvarchar(50) NULL;

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'ActionUrl');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Notifications] ALTER COLUMN [ActionUrl] nvarchar(max) NULL;

CREATE UNIQUE INDEX [IX_Suppliers_TaxIdentificationNumber] ON [Suppliers] ([TaxIdentificationNumber]) WHERE [TaxIdentificationNumber] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260311060148_MakeOptionalFieldsNullable', N'9.0.2');

ALTER TABLE [Tenders] ADD [Quantity] int NOT NULL DEFAULT 0;

ALTER TABLE [PurchaseOrders] ADD [ProductId] int NOT NULL DEFAULT 0;

ALTER TABLE [PurchaseOrders] ADD [ProductName] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [PurchaseOrders] ADD [Quantity] int NOT NULL DEFAULT 0;

ALTER TABLE [PurchaseOrders] ADD [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

CREATE INDEX [IX_PurchaseOrders_ProductId] ON [PurchaseOrders] ([ProductId]);

ALTER TABLE [PurchaseOrders] ADD CONSTRAINT [FK_PurchaseOrders_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260311162155_Module3ProcurementUpdates', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260311164031_AddSupplierEmployee', N'9.0.2');

ALTER TABLE [Products] ADD [IsAvailable] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Products] ADD [Quantity] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [Unit] nvarchar(50) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312122231_AddProductCatalog_Module2_Fix', N'9.0.2');

ALTER TABLE [Commissions] DROP CONSTRAINT [FK_Commissions_PurchaseOrders_PurchaseOrderId];

ALTER TABLE [Deliveries] DROP CONSTRAINT [FK_Deliveries_Orders_OrderId];

ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_Retailers_RetailerId];

ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [FK_PurchaseOrders_TenderBids_TenderBidId];

ALTER TABLE [Ratings] DROP CONSTRAINT [FK_Ratings_PurchaseOrders_PurchaseOrderId];

ALTER TABLE [TenderBids] DROP CONSTRAINT [FK_TenderBids_Tenders_TenderId];

ALTER TABLE [Tenders] DROP CONSTRAINT [FK_Tenders_ProductCategories_CategoryId];

ALTER TABLE [Tenders] DROP CONSTRAINT [FK_Tenders_Retailers_RetailerId];

DROP INDEX [IX_PurchaseOrders_PONumber] ON [PurchaseOrders];

DROP INDEX [IX_PurchaseOrders_TenderBidId] ON [PurchaseOrders];

DROP INDEX [IX_Orders_PurchaseOrderId] ON [Orders];

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TenderBids]') AND [c].[name] = N'DeliveryTimeline');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [TenderBids] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [TenderBids] DROP COLUMN [DeliveryTimeline];

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrderItems]') AND [c].[name] = N'TotalPrice');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrderItems] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [PurchaseOrderItems] DROP COLUMN [TotalPrice];

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'TotalPrice');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [OrderItems] DROP COLUMN [TotalPrice];

EXEC sp_rename N'[Tenders].[ClosingDate]', N'SubmissionDeadline', 'COLUMN';

EXEC sp_rename N'[TenderBids].[SubmittedDate]', N'SubmittedAt', 'COLUMN';

EXEC sp_rename N'[TenderBids].[BidNotes]', N'Notes', 'COLUMN';

EXEC sp_rename N'[TenderBids].[BidAmount]', N'ProposedTotalAmount', 'COLUMN';

EXEC sp_rename N'[OrderStatusHistories].[Notes]', N'Comments', 'COLUMN';

EXEC sp_rename N'[OrderStatusHistories].[ChangedBy]', N'ChangedByUserId', 'COLUMN';

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tenders]') AND [c].[name] = N'Title');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Tenders] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [Tenders] ALTER COLUMN [Title] nvarchar(150) NOT NULL;

ALTER TABLE [Tenders] ADD [ExpectedDeliveryDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Tenders] ADD [ReferenceNumber] nvarchar(50) NOT NULL DEFAULT N'';

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TenderItems]') AND [c].[name] = N'ProductName');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [TenderItems] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [TenderItems] ALTER COLUMN [ProductName] nvarchar(100) NOT NULL;

ALTER TABLE [TenderItems] ADD [EstimatedUnitPrice] decimal(18,2) NULL;

ALTER TABLE [TenderItems] ADD [Unit] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [TenderBids] ADD [DeliveryLeadTimeDays] int NOT NULL DEFAULT 0;

ALTER TABLE [TenderBids] ADD [ValidityPeriodDays] int NOT NULL DEFAULT 0;

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'ExpectedDeliveryDate');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var18 + '];');
UPDATE [PurchaseOrders] SET [ExpectedDeliveryDate] = '0001-01-01T00:00:00.0000000' WHERE [ExpectedDeliveryDate] IS NULL;
ALTER TABLE [PurchaseOrders] ALTER COLUMN [ExpectedDeliveryDate] datetime2 NOT NULL;
ALTER TABLE [PurchaseOrders] ADD DEFAULT '0001-01-01T00:00:00.0000000' FOR [ExpectedDeliveryDate];

ALTER TABLE [PurchaseOrders] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [PurchaseOrders] ADD [DeliveryAddress] nvarchar(255) NOT NULL DEFAULT N'';

ALTER TABLE [PurchaseOrderItems] ADD [ProductId1] int NULL;

DROP INDEX [IX_Orders_RetailerId] ON [Orders];
DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'RetailerId');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var19 + '];');
UPDATE [Orders] SET [RetailerId] = 0 WHERE [RetailerId] IS NULL;
ALTER TABLE [Orders] ALTER COLUMN [RetailerId] int NOT NULL;
ALTER TABLE [Orders] ADD DEFAULT 0 FOR [RetailerId];
CREATE INDEX [IX_Orders_RetailerId] ON [Orders] ([RetailerId]);

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'OrderStatus');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var20 + '];');

DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'CreatedAt');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var21 + '];');

ALTER TABLE [Orders] ADD [OrderNumber] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Orders] ADD [PaymentStatus] nvarchar(20) NOT NULL DEFAULT N'';

ALTER TABLE [Orders] ADD [PurchaseOrderId1] int NULL;

ALTER TABLE [Orders] ADD [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [OrderItems] ADD [ProductId1] int NULL;

CREATE INDEX [IX_PurchaseOrders_TenderBidId] ON [PurchaseOrders] ([TenderBidId]);

CREATE INDEX [IX_PurchaseOrderItems_ProductId1] ON [PurchaseOrderItems] ([ProductId1]);

CREATE INDEX [IX_OrderStatusHistories_ChangedByUserId] ON [OrderStatusHistories] ([ChangedByUserId]);

CREATE INDEX [IX_Orders_PurchaseOrderId] ON [Orders] ([PurchaseOrderId]);

CREATE UNIQUE INDEX [IX_Orders_PurchaseOrderId1] ON [Orders] ([PurchaseOrderId1]) WHERE [PurchaseOrderId1] IS NOT NULL;

CREATE INDEX [IX_OrderItems_ProductId1] ON [OrderItems] ([ProductId1]);

ALTER TABLE [Commissions] ADD CONSTRAINT [FK_Commissions_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE;

ALTER TABLE [Deliveries] ADD CONSTRAINT [FK_Deliveries_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE;

ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_Products_ProductId1] FOREIGN KEY ([ProductId1]) REFERENCES [Products] ([Id]);

ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_PurchaseOrders_PurchaseOrderId1] FOREIGN KEY ([PurchaseOrderId1]) REFERENCES [PurchaseOrders] ([Id]);

ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [OrderStatusHistories] ADD CONSTRAINT [FK_OrderStatusHistories_Users_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [PurchaseOrderItems] ADD CONSTRAINT [FK_PurchaseOrderItems_Products_ProductId1] FOREIGN KEY ([ProductId1]) REFERENCES [Products] ([Id]);

ALTER TABLE [PurchaseOrders] ADD CONSTRAINT [FK_PurchaseOrders_TenderBids_TenderBidId] FOREIGN KEY ([TenderBidId]) REFERENCES [TenderBids] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Ratings] ADD CONSTRAINT [FK_Ratings_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE;

ALTER TABLE [TenderBids] ADD CONSTRAINT [FK_TenderBids_Tenders_TenderId] FOREIGN KEY ([TenderId]) REFERENCES [Tenders] ([Id]) ON DELETE CASCADE;

ALTER TABLE [Tenders] ADD CONSTRAINT [FK_Tenders_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE;

ALTER TABLE [Tenders] ADD CONSTRAINT [FK_Tenders_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260324232836_RebuildModule3', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260325114529_AddProductQuantityUnitIsAvailable', N'9.0.2');

CREATE TABLE [Carts] (
    [Id] int NOT NULL IDENTITY,
    [RetailerId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Carts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Carts_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CartItems] (
    [Id] int NOT NULL IDENTITY,
    [CartId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [AddedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CartItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_CartItems_CartId] ON [CartItems] ([CartId]);

CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);

CREATE UNIQUE INDEX [IX_Carts_RetailerId] ON [Carts] ([RetailerId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260325123108_AddMarketplaceCart', N'9.0.2');

DROP INDEX [IX_Products_SKU] ON [Products];

DECLARE @var22 sysname;
SELECT @var22 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'SKU');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var22 + '];');
ALTER TABLE [Products] ALTER COLUMN [SKU] nvarchar(50) NULL;

DECLARE @var23 sysname;
SELECT @var23 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ImageUrl');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var23 + '];');
ALTER TABLE [Products] ALTER COLUMN [ImageUrl] nvarchar(255) NULL;

DECLARE @var24 sysname;
SELECT @var24 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Description');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var24 + '];');
ALTER TABLE [Products] ALTER COLUMN [Description] nvarchar(max) NULL;

ALTER TABLE [Products] ADD [Barcode] nvarchar(100) NULL;

ALTER TABLE [Products] ADD [Brand] nvarchar(100) NULL;

ALTER TABLE [Products] ADD [CostPrice] decimal(18,2) NULL;

ALTER TABLE [Products] ADD [CountryOfOrigin] nvarchar(100) NULL;

ALTER TABLE [Products] ADD [Dimensions] nvarchar(100) NULL;

ALTER TABLE [Products] ADD [DiscountPercentage] decimal(5,2) NULL;

ALTER TABLE [Products] ADD [HSCode] nvarchar(50) NULL;

ALTER TABLE [Products] ADD [IsFeatured] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Products] ADD [IsHazardous] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Products] ADD [LeadTimeDays] int NULL;

ALTER TABLE [Products] ADD [Manufacturer] nvarchar(100) NULL;

ALTER TABLE [Products] ADD [MaximumStockLevel] int NULL;

ALTER TABLE [Products] ADD [MetaDescription] nvarchar(1000) NULL;

ALTER TABLE [Products] ADD [MetaKeywords] nvarchar(500) NULL;

ALTER TABLE [Products] ADD [MetaTitle] nvarchar(255) NULL;

ALTER TABLE [Products] ADD [MinimumOrderQuantity] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [ReorderLevel] int NULL;

ALTER TABLE [Products] ADD [ReorderQuantity] int NULL;

ALTER TABLE [Products] ADD [ShippingHeight] decimal(18,2) NULL;

ALTER TABLE [Products] ADD [ShippingLength] decimal(18,2) NULL;

ALTER TABLE [Products] ADD [ShippingWeight] decimal(18,2) NULL;

ALTER TABLE [Products] ADD [ShippingWidth] decimal(18,2) NULL;

ALTER TABLE [Products] ADD [ShortDescription] nvarchar(500) NULL;

ALTER TABLE [Products] ADD [Slug] nvarchar(255) NULL;

ALTER TABLE [Products] ADD [Specifications] nvarchar(max) NULL;

ALTER TABLE [Products] ADD [SubCategoryId] int NULL;

ALTER TABLE [Products] ADD [Tags] nvarchar(255) NULL;

ALTER TABLE [Products] ADD [TaxRate] decimal(5,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Products] ADD [WarrantyPeriod] nvarchar(100) NULL;

ALTER TABLE [Products] ADD [Weight] decimal(18,2) NULL;

ALTER TABLE [Products] ADD [WeightUnit] nvarchar(20) NULL;

ALTER TABLE [Products] ADD [WholesalePrice] decimal(18,2) NULL;

CREATE UNIQUE INDEX [IX_Products_SKU] ON [Products] ([SKU]) WHERE [SKU] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327175811_AddProfessionalProductFields', N'9.0.2');

DROP INDEX [IX_Products_SKU] ON [Products];

ALTER TABLE [Tenders] ADD [DeliveryLocation] nvarchar(max) NULL;

ALTER TABLE [Tenders] ADD [DeliveryWeight] int NOT NULL DEFAULT 0;

ALTER TABLE [Tenders] ADD [InspectionRequirement] nvarchar(max) NULL;

ALTER TABLE [Tenders] ADD [Language] nvarchar(max) NULL;

ALTER TABLE [Tenders] ADD [PackagingRequirements] nvarchar(max) NULL;

ALTER TABLE [Tenders] ADD [PaymentTerms] nvarchar(max) NULL;

ALTER TABLE [Tenders] ADD [PriceWeight] int NOT NULL DEFAULT 0;

ALTER TABLE [Tenders] ADD [TechnicalWeight] int NOT NULL DEFAULT 0;

ALTER TABLE [TenderBids] ADD [DeliveryPlan] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [FinancialProposal] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [IsWinningBid] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [TenderBids] ADD [QualityGuarantee] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [TechnicalProposal] nvarchar(max) NULL;

DECLARE @var25 sysname;
SELECT @var25 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'SKU');
IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var25 + '];');
ALTER TABLE [Products] ALTER COLUMN [SKU] nvarchar(50) NULL;

DECLARE @var26 sysname;
SELECT @var26 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ImageUrl');
IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var26 + '];');
ALTER TABLE [Products] ALTER COLUMN [ImageUrl] nvarchar(255) NULL;

DECLARE @var27 sysname;
SELECT @var27 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Description');
IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var27 + '];');
ALTER TABLE [Products] ALTER COLUMN [Description] nvarchar(max) NULL;

ALTER TABLE [Products] ADD [ReservedQuantity] int NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX [IX_Products_SKU] ON [Products] ([SKU]) WHERE [SKU] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327193022_AddScmEnhancements', N'9.0.2');

DECLARE @var28 sysname;
SELECT @var28 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'CountryOfOrigin');
IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var28 + '];');
ALTER TABLE [Products] DROP COLUMN [CountryOfOrigin];

DECLARE @var29 sysname;
SELECT @var29 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Dimensions');
IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var29 + '];');
ALTER TABLE [Products] DROP COLUMN [Dimensions];

DECLARE @var30 sysname;
SELECT @var30 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Manufacturer');
IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var30 + '];');
ALTER TABLE [Products] DROP COLUMN [Manufacturer];

DECLARE @var31 sysname;
SELECT @var31 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Specifications');
IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var31 + '];');
ALTER TABLE [Products] DROP COLUMN [Specifications];

DECLARE @var32 sysname;
SELECT @var32 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'WarrantyPeriod');
IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var32 + '];');
ALTER TABLE [Products] DROP COLUMN [WarrantyPeriod];

DECLARE @var33 sysname;
SELECT @var33 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Weight');
IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var33 + '];');
ALTER TABLE [Products] DROP COLUMN [Weight];

DECLARE @var34 sysname;
SELECT @var34 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'WeightUnit');
IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var34 + '];');
ALTER TABLE [Products] DROP COLUMN [WeightUnit];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327193039_AddHybridEavSupport', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327200200_FixPurchaseOrderFK', N'9.0.2');

ALTER TABLE [OrderItems] DROP CONSTRAINT [FK_OrderItems_Products_ProductId1];

ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_PurchaseOrders_PurchaseOrderId1];

ALTER TABLE [PurchaseOrderItems] DROP CONSTRAINT [FK_PurchaseOrderItems_Products_ProductId1];

DROP INDEX [IX_PurchaseOrderItems_ProductId1] ON [PurchaseOrderItems];

DROP INDEX [IX_Orders_PurchaseOrderId] ON [Orders];

DROP INDEX [IX_Orders_PurchaseOrderId1] ON [Orders];

DROP INDEX [IX_OrderItems_ProductId1] ON [OrderItems];

DECLARE @var35 sysname;
SELECT @var35 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrderItems]') AND [c].[name] = N'ProductId1');
IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrderItems] DROP CONSTRAINT [' + @var35 + '];');
ALTER TABLE [PurchaseOrderItems] DROP COLUMN [ProductId1];

DECLARE @var36 sysname;
SELECT @var36 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'PurchaseOrderId1');
IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var36 + '];');
ALTER TABLE [Orders] DROP COLUMN [PurchaseOrderId1];

DECLARE @var37 sysname;
SELECT @var37 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ProductId1');
IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT [' + @var37 + '];');
ALTER TABLE [OrderItems] DROP COLUMN [ProductId1];

CREATE UNIQUE INDEX [IX_Orders_PurchaseOrderId] ON [Orders] ([PurchaseOrderId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327200414_RemoveShadowProperties', N'9.0.2');

DROP INDEX [IX_Orders_PurchaseOrderId] ON [Orders];

DECLARE @var38 sysname;
SELECT @var38 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'PurchaseOrderId');
IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var38 + '];');
ALTER TABLE [Orders] ALTER COLUMN [PurchaseOrderId] int NULL;

ALTER TABLE [Orders] ADD [DeliveryAddress] nvarchar(255) NOT NULL DEFAULT N'';

ALTER TABLE [Orders] ADD [ExpectedDeliveryDate] datetime2 NULL;

CREATE UNIQUE INDEX [IX_Orders_PurchaseOrderId] ON [Orders] ([PurchaseOrderId]) WHERE [PurchaseOrderId] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328084448_AddDeliveryToOrder', N'9.0.2');

ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [FK_PurchaseOrders_Products_ProductId];

DROP INDEX [IX_PurchaseOrders_ProductId] ON [PurchaseOrders];

DECLARE @var39 sysname;
SELECT @var39 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'ProductId');
IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var39 + '];');
ALTER TABLE [PurchaseOrders] DROP COLUMN [ProductId];

DECLARE @var40 sysname;
SELECT @var40 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'ProductName');
IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var40 + '];');
ALTER TABLE [PurchaseOrders] DROP COLUMN [ProductName];

DECLARE @var41 sysname;
SELECT @var41 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'Quantity');
IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var41 + '];');
ALTER TABLE [PurchaseOrders] DROP COLUMN [Quantity];

DECLARE @var42 sysname;
SELECT @var42 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'UnitPrice');
IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var42 + '];');
ALTER TABLE [PurchaseOrders] DROP COLUMN [UnitPrice];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328090856_RemoveLegacyPOColumns', N'9.0.2');

ALTER TABLE [SupplierEmployees] ADD [VehicleId] int NULL;

ALTER TABLE [SupplierEmployees] ADD [WarehouseId] int NULL;

ALTER TABLE [PurchaseOrders] ADD [DeliveryAgentId] int NULL;

ALTER TABLE [PurchaseOrders] ADD [DeliveryMethod] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [PurchaseOrders] ADD [Discount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [PurchaseOrders] ADD [PaymentStatus] nvarchar(20) NOT NULL DEFAULT N'';

ALTER TABLE [PurchaseOrders] ADD [ProofOfDelivery] nvarchar(255) NOT NULL DEFAULT N'';

ALTER TABLE [PurchaseOrders] ADD [Subtotal] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [PurchaseOrders] ADD [VAT] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [PurchaseOrders] ADD [WarehouseId] int NULL;

CREATE TABLE [Vehicles] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [LicensePlate] nvarchar(50) NOT NULL,
    [VehicleType] nvarchar(50) NOT NULL,
    [Capacity] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Vehicles_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_SupplierEmployees_VehicleId] ON [SupplierEmployees] ([VehicleId]);

CREATE INDEX [IX_SupplierEmployees_WarehouseId] ON [SupplierEmployees] ([WarehouseId]);

CREATE INDEX [IX_PurchaseOrders_DeliveryAgentId] ON [PurchaseOrders] ([DeliveryAgentId]);

CREATE INDEX [IX_PurchaseOrders_WarehouseId] ON [PurchaseOrders] ([WarehouseId]);

CREATE INDEX [IX_Vehicles_SupplierId] ON [Vehicles] ([SupplierId]);

ALTER TABLE [PurchaseOrders] ADD CONSTRAINT [FK_PurchaseOrders_SupplierEmployees_DeliveryAgentId] FOREIGN KEY ([DeliveryAgentId]) REFERENCES [SupplierEmployees] ([Id]);

ALTER TABLE [PurchaseOrders] ADD CONSTRAINT [FK_PurchaseOrders_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]);

ALTER TABLE [SupplierEmployees] ADD CONSTRAINT [FK_SupplierEmployees_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE SET NULL;

ALTER TABLE [SupplierEmployees] ADD CONSTRAINT [FK_SupplierEmployees_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328094214_AddEmployeeRolesAndVehicles', N'9.0.2');

ALTER TABLE [SupplierEmployees] ADD [DrivingLicenseNumber] nvarchar(100) NULL;

ALTER TABLE [SupplierEmployees] ADD [IsLicenseVerified] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [LicenseExpiryDate] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328120629_AddLicenseAndVehicleStatus', N'9.0.2');

ALTER TABLE [Vehicles] ADD [Status] nvarchar(30) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328120809_AddVehicleStatus', N'9.0.2');

DECLARE @var43 sysname;
SELECT @var43 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'Location');
IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var43 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [Location];

DECLARE @var44 sysname;
SELECT @var44 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'Capacity');
IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var44 + '];');
ALTER TABLE [Vehicles] DROP COLUMN [Capacity];

EXEC sp_rename N'[Warehouses].[Capacity]', N'StorageType', 'COLUMN';

EXEC sp_rename N'[Vehicles].[IsActive]', N'HasTemperatureControl', 'COLUMN';

DECLARE @var45 sysname;
SELECT @var45 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'City');
IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var45 + '];');
ALTER TABLE [Warehouses] ALTER COLUMN [City] nvarchar(100) NOT NULL;

ALTER TABLE [Warehouses] ADD [Address] nvarchar(300) NOT NULL DEFAULT N'';

ALTER TABLE [Warehouses] ADD [Country] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Warehouses] ADD [HandlingTimeHours] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [MaxCapacity] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [Region] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Warehouses] ADD [Status] int NOT NULL DEFAULT 0;

ALTER TABLE [Warehouses] ADD [SupportsDelivery] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Warehouses] ADD [UpdatedAt] datetime2 NULL;

ALTER TABLE [Warehouses] ADD [WarehouseCode] nvarchar(50) NULL;

DECLARE @var46 sysname;
SELECT @var46 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'VehicleType');
IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var46 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [VehicleType] int NOT NULL;

DECLARE @var47 sysname;
SELECT @var47 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'Status');
IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT [' + @var47 + '];');
ALTER TABLE [Vehicles] ALTER COLUMN [Status] int NOT NULL;

ALTER TABLE [Vehicles] ADD [InsuranceExpiryDate] datetime2 NULL;

ALTER TABLE [Vehicles] ADD [InsuranceStatus] nvarchar(50) NULL;

ALTER TABLE [Vehicles] ADD [LastMaintenanceDate] datetime2 NULL;

ALTER TABLE [Vehicles] ADD [MaxLoadCapacity] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Vehicles] ADD [RegistrationNumber] nvarchar(50) NULL;

ALTER TABLE [Vehicles] ADD [RoadworthinessStatus] nvarchar(100) NULL;

ALTER TABLE [Vehicles] ADD [UpdatedAt] datetime2 NULL;

ALTER TABLE [Vehicles] ADD [VolumeCapacity] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328125230_StandardizeLogisticsModels', N'9.0.2');

DECLARE @var48 sysname;
SELECT @var48 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Barcode');
IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var48 + '];');
ALTER TABLE [Products] DROP COLUMN [Barcode];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328133018_RemoveBarcode', N'9.0.2');

DELETE FROM PurchaseOrders;

ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_PurchaseOrders_PurchaseOrderId];

ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [FK_PurchaseOrders_Warehouses_WarehouseId];

DROP INDEX [IX_Orders_PurchaseOrderId] ON [Orders];

DECLARE @var49 sysname;
SELECT @var49 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'PurchaseOrderId');
IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var49 + '];');
ALTER TABLE [Orders] DROP COLUMN [PurchaseOrderId];

DROP INDEX [IX_PurchaseOrders_WarehouseId] ON [PurchaseOrders];
DECLARE @var50 sysname;
SELECT @var50 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'WarehouseId');
IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var50 + '];');
UPDATE [PurchaseOrders] SET [WarehouseId] = 0 WHERE [WarehouseId] IS NULL;
ALTER TABLE [PurchaseOrders] ALTER COLUMN [WarehouseId] int NOT NULL;
ALTER TABLE [PurchaseOrders] ADD DEFAULT 0 FOR [WarehouseId];
CREATE INDEX [IX_PurchaseOrders_WarehouseId] ON [PurchaseOrders] ([WarehouseId]);

ALTER TABLE [PurchaseOrders] ADD [DeliveredAt] datetime2 NULL;

ALTER TABLE [PurchaseOrders] ADD [InvoiceNumber] nvarchar(50) NULL;

ALTER TABLE [PurchaseOrders] ADD [Notes] nvarchar(1000) NULL;

ALTER TABLE [PurchaseOrders] ADD [OrderId] int NOT NULL DEFAULT 0;

CREATE INDEX [IX_PurchaseOrders_OrderId] ON [PurchaseOrders] ([OrderId]);

ALTER TABLE [PurchaseOrders] ADD CONSTRAINT [FK_PurchaseOrders_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [PurchaseOrders] ADD CONSTRAINT [FK_PurchaseOrders_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328152137_FinalizeMultiWarehouseSchema3', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328170320_AddMissingPurchaseOrderColumns', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328183322_SyncProductModelFields', N'9.0.2');

ALTER TABLE [Orders] ADD [RejectedAt] datetime2 NULL;

ALTER TABLE [Orders] ADD [RejectionReason] nvarchar(500) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328185429_AddOrderRejectionFields', N'9.0.2');

DROP INDEX [IX_Inventories_ProductId] ON [Inventories];

DECLARE @var51 sysname;
SELECT @var51 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Quantity');
IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var51 + '];');
ALTER TABLE [Products] DROP COLUMN [Quantity];

DECLARE @var52 sysname;
SELECT @var52 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ReservedQuantity');
IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var52 + '];');
ALTER TABLE [Products] DROP COLUMN [ReservedQuantity];

DECLARE @var53 sysname;
SELECT @var53 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventories]') AND [c].[name] = N'QuantityAvailable');
IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [Inventories] DROP CONSTRAINT [' + @var53 + '];');
ALTER TABLE [Inventories] DROP COLUMN [QuantityAvailable];

CREATE INDEX [IX_Inventories_ProductId] ON [Inventories] ([ProductId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328194924_RemoveLegacyStockFields', N'9.0.2');

DECLARE @var54 sysname;
SELECT @var54 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductAttributeDefinitions]') AND [c].[name] = N'Unit');
IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [ProductAttributeDefinitions] DROP CONSTRAINT [' + @var54 + '];');
ALTER TABLE [ProductAttributeDefinitions] ALTER COLUMN [Unit] nvarchar(20) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328201621_MakeAttributeUnitNullable', N'9.0.2');

DECLARE @var55 sysname;
SELECT @var55 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'ProofOfDelivery');
IF @var55 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var55 + '];');
ALTER TABLE [PurchaseOrders] ALTER COLUMN [ProofOfDelivery] nvarchar(255) NULL;

DECLARE @var56 sysname;
SELECT @var56 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseOrders]') AND [c].[name] = N'DeliveryMethod');
IF @var56 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseOrders] DROP CONSTRAINT [' + @var56 + '];');
ALTER TABLE [PurchaseOrders] ALTER COLUMN [DeliveryMethod] nvarchar(100) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328203830_MakePurchaseOrderFieldsNullable', N'9.0.2');

EXEC sp_rename N'[TenderBids].[FinancialProposal]', N'PackagingPlan', 'COLUMN';

EXEC sp_rename N'[TenderBids].[DeliveryPlan]', N'InspectionCompliance', 'COLUMN';

ALTER TABLE [TenderItems] ADD [ProductId] int NOT NULL DEFAULT 0;

DECLARE @var57 sysname;
SELECT @var57 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TenderBids]') AND [c].[name] = N'Notes');
IF @var57 IS NOT NULL EXEC(N'ALTER TABLE [TenderBids] DROP CONSTRAINT [' + @var57 + '];');
ALTER TABLE [TenderBids] ALTER COLUMN [Notes] nvarchar(max) NULL;

ALTER TABLE [TenderBids] ADD [DeliveryCapacity] nvarchar(255) NULL;

ALTER TABLE [TenderBids] ADD [DeliveryMethod] nvarchar(100) NULL;

ALTER TABLE [TenderBids] ADD [DiscountPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TenderBids] ADD [PenaltyAcceptance] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [TenderBids] ADD [ProposedDeliveryDate] datetime2 NULL;

ALTER TABLE [TenderBids] ADD [Quantity] int NOT NULL DEFAULT 0;

ALTER TABLE [TenderBids] ADD [Score] decimal(5,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TenderBids] ADD [Subtotal] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TenderBids] ADD [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TenderBids] ADD [VATPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;

CREATE INDEX [IX_TenderItems_ProductId] ON [TenderItems] ([ProductId]);

ALTER TABLE [TenderItems] ADD CONSTRAINT [FK_TenderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260328211531_ExpandBidSchema', N'9.0.2');

ALTER TABLE [PurchaseOrders] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260329075452_AddUpdatedAtToPurchaseOrder', N'9.0.2');

ALTER TABLE [PurchaseOrders] ADD [VehicleId] int NULL;

CREATE INDEX [IX_PurchaseOrders_VehicleId] ON [PurchaseOrders] ([VehicleId]);

ALTER TABLE [PurchaseOrders] ADD CONSTRAINT [FK_PurchaseOrders_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260329121805_AddVehicleToPurchaseOrder', N'9.0.2');

ALTER TABLE [PurchaseOrders] ADD [PackedAt] datetime2 NULL;

ALTER TABLE [PurchaseOrders] ADD [PickedAt] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260329125114_AddPickingPackingTimestamps', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260403130245_AddIsActiveToPenalty', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260403130538_AddIsActiveToPenalt', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260403132310_AddMissingPenaltyColumns', N'9.0.2');

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

DECLARE @var58 sysname;
SELECT @var58 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Type');
IF @var58 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var58 + '];');
UPDATE [Notifications] SET [Type] = N'' WHERE [Type] IS NULL;
ALTER TABLE [Notifications] ALTER COLUMN [Type] nvarchar(20) NOT NULL;
ALTER TABLE [Notifications] ADD DEFAULT N'' FOR [Type];

DECLARE @var59 sysname;
SELECT @var59 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Message');
IF @var59 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var59 + '];');
ALTER TABLE [Notifications] ALTER COLUMN [Message] nvarchar(500) NOT NULL;

DECLARE @var60 sysname;
SELECT @var60 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'ActionUrl');
IF @var60 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var60 + '];');
ALTER TABLE [Notifications] ALTER COLUMN [ActionUrl] nvarchar(200) NULL;

ALTER TABLE [Notifications] ADD [ReadAt] datetime2 NULL;

DECLARE @var61 sysname;
SELECT @var61 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MessageViolations]') AND [c].[name] = N'ViolationType');
IF @var61 IS NOT NULL EXEC(N'ALTER TABLE [MessageViolations] DROP CONSTRAINT [' + @var61 + '];');
ALTER TABLE [MessageViolations] ALTER COLUMN [ViolationType] nvarchar(900) NOT NULL;

DECLARE @var62 sysname;
SELECT @var62 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commissions]') AND [c].[name] = N'PaymentVerificationData');
IF @var62 IS NOT NULL EXEC(N'ALTER TABLE [Commissions] DROP CONSTRAINT [' + @var62 + '];');
ALTER TABLE [Commissions] ALTER COLUMN [PaymentVerificationData] nvarchar(max) NULL;

DECLARE @var63 sysname;
SELECT @var63 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commissions]') AND [c].[name] = N'PaymentRequestData');
IF @var63 IS NOT NULL EXEC(N'ALTER TABLE [Commissions] DROP CONSTRAINT [' + @var63 + '];');
ALTER TABLE [Commissions] ALTER COLUMN [PaymentRequestData] nvarchar(max) NULL;

DECLARE @var64 sysname;
SELECT @var64 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Commissions]') AND [c].[name] = N'ChapaTransactionId');
IF @var64 IS NOT NULL EXEC(N'ALTER TABLE [Commissions] DROP CONSTRAINT [' + @var64 + '];');
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

DECLARE @var65 sysname;
SELECT @var65 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FaydaVerifications]') AND [c].[name] = N'OTP');
IF @var65 IS NOT NULL EXEC(N'ALTER TABLE [FaydaVerifications] DROP CONSTRAINT [' + @var65 + '];');
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

DECLARE @var66 sysname;
SELECT @var66 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FaydaVerifications]') AND [c].[name] = N'Id');
IF @var66 IS NOT NULL EXEC(N'ALTER TABLE [FaydaVerifications] DROP CONSTRAINT [' + @var66 + '];');
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

DECLARE @var67 sysname;
SELECT @var67 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ratings]') AND [c].[name] = N'Comment');
IF @var67 IS NOT NULL EXEC(N'ALTER TABLE [Ratings] DROP CONSTRAINT [' + @var67 + '];');
ALTER TABLE [Ratings] ALTER COLUMN [Comment] nvarchar(1000) NULL;

ALTER TABLE [Ratings] ADD [Category] nvarchar(50) NULL;

ALTER TABLE [Ratings] ADD [HelpfulCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Ratings] ADD [IsVerifiedPurchase] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Ratings] ADD [NotHelpfulCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Ratings] ADD [OrderId] int NOT NULL DEFAULT 0;

ALTER TABLE [Ratings] ADD [UpdatedAt] datetime2 NULL;

DECLARE @var68 sysname;
SELECT @var68 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderStatusHistories]') AND [c].[name] = N'Status');
IF @var68 IS NOT NULL EXEC(N'ALTER TABLE [OrderStatusHistories] DROP CONSTRAINT [' + @var68 + '];');
ALTER TABLE [OrderStatusHistories] ALTER COLUMN [Status] nvarchar(50) NOT NULL;

DECLARE @var69 sysname;
SELECT @var69 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderStatusHistories]') AND [c].[name] = N'Comments');
IF @var69 IS NOT NULL EXEC(N'ALTER TABLE [OrderStatusHistories] DROP CONSTRAINT [' + @var69 + '];');
ALTER TABLE [OrderStatusHistories] ALTER COLUMN [Comments] nvarchar(500) NULL;

DECLARE @var70 sysname;
SELECT @var70 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderStatusHistories]') AND [c].[name] = N'ChangedByUserId');
IF @var70 IS NOT NULL EXEC(N'ALTER TABLE [OrderStatusHistories] DROP CONSTRAINT [' + @var70 + '];');
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418072942_AddMissingColumnsToTenderAndProduct', N'9.0.2');

DECLARE @var71 sysname;
SELECT @var71 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CartItems]') AND [c].[name] = N'ProductId');
IF @var71 IS NOT NULL EXEC(N'ALTER TABLE [CartItems] DROP CONSTRAINT [' + @var71 + '];');
ALTER TABLE [CartItems] ALTER COLUMN [ProductId] int NULL;

ALTER TABLE [CartItems] ADD [Description] nvarchar(max) NULL;

ALTER TABLE [CartItems] ADD [ProductName] nvarchar(100) NULL;

ALTER TABLE [CartItems] ADD [UnitPrice] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418073032_UpdateCartItemForProductName', N'9.0.2');

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'ProductName') ALTER TABLE Tenders ADD ProductName nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'AllowPartialBids') ALTER TABLE Tenders ADD AllowPartialBids bit NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'AttachmentPath') ALTER TABLE Tenders ADD AttachmentPath nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'BudgetMax') ALTER TABLE Tenders ADD BudgetMax decimal(18,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'BudgetMin') ALTER TABLE Tenders ADD BudgetMin decimal(18,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'PreferredSuppliers') ALTER TABLE Tenders ADD PreferredSuppliers nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418183746_AddMissingTenderColumnsManual2', N'9.0.2');

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'ProductName') ALTER TABLE PurchaseOrderItems ADD ProductName nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'Description') ALTER TABLE PurchaseOrderItems ADD Description nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderItems') AND name = 'ProductName') ALTER TABLE OrderItems ADD ProductName nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderItems') AND name = 'Description') ALTER TABLE OrderItems ADD Description nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418191845_AddMissingCommonColumnsManual', N'9.0.2');

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

ALTER TABLE [SupplierEmployees] ADD [AutoAcceptPickTasks] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [DefaultWarehouseLocation] nvarchar(200) NULL;

ALTER TABLE [SupplierEmployees] ADD [LowStockThreshold] int NOT NULL DEFAULT 0;

ALTER TABLE [SupplierEmployees] ADD [NotifyLowStock] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [PicklistFormat] nvarchar(50) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260419083226_SyncDatabase', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260419083722_AddWarehouseManagerSettingsColumns', N'9.0.2');

CREATE TABLE [InventoryHistories] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [SupplierEmployeeId] int NOT NULL,
    [Quantity] int NOT NULL,
    [BatchNumber] nvarchar(50) NULL,
    [ExpiryDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryHistories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryHistories_SupplierEmployees_SupplierEmployeeId] FOREIGN KEY ([SupplierEmployeeId]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryHistories_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_InventoryHistories_ProductId] ON [InventoryHistories] ([ProductId]);

CREATE INDEX [IX_InventoryHistories_SupplierEmployeeId] ON [InventoryHistories] ([SupplierEmployeeId]);

CREATE INDEX [IX_InventoryHistories_WarehouseId] ON [InventoryHistories] ([WarehouseId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260419091452_AddInventoryHistory', N'9.0.2');

DECLARE @var72 sysname;
SELECT @var72 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SupplierEmployees]') AND [c].[name] = N'IsLicenseVerified');
IF @var72 IS NOT NULL EXEC(N'ALTER TABLE [SupplierEmployees] DROP CONSTRAINT [' + @var72 + '];');
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

DECLARE @var73 sysname;
SELECT @var73 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'ContactPersonName');
IF @var73 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var73 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [ContactPersonName];

DECLARE @var74 sysname;
SELECT @var74 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'ContactPhone');
IF @var74 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var74 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [ContactPhone];

DECLARE @var75 sysname;
SELECT @var75 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'Email');
IF @var75 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var75 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [Email];

DECLARE @var76 sysname;
SELECT @var76 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouses]') AND [c].[name] = N'EmergencyContact');
IF @var76 IS NOT NULL EXEC(N'ALTER TABLE [Warehouses] DROP CONSTRAINT [' + @var76 + '];');
ALTER TABLE [Warehouses] DROP COLUMN [EmergencyContact];

ALTER TABLE [Warehouses] ADD [PrimaryManagerId] int NULL;

ALTER TABLE [Vehicles] ADD [PrimaryDriverId] int NULL;

CREATE INDEX [IX_Warehouses_PrimaryManagerId] ON [Warehouses] ([PrimaryManagerId]);

CREATE INDEX [IX_Vehicles_PrimaryDriverId] ON [Vehicles] ([PrimaryDriverId]);

ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_SupplierEmployees_PrimaryDriverId] FOREIGN KEY ([PrimaryDriverId]) REFERENCES [SupplierEmployees] ([Id]);

ALTER TABLE [Warehouses] ADD CONSTRAINT [FK_Warehouses_SupplierEmployees_PrimaryManagerId] FOREIGN KEY ([PrimaryManagerId]) REFERENCES [SupplierEmployees] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260420185245_AddPrimaryAssignmentsToAssets', N'9.0.2');

ALTER TABLE [Users] ADD [ProfileImage] nvarchar(255) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260420185839_AddProfileImageToUsers', N'9.0.2');

ALTER TABLE [Users] ADD [DefaultDashboardView] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [NotificationEmail] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260420192115_AddAdminSettingsToUser', N'9.0.2');

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

EXEC sp_rename N'[Users].[NotificationEmail]', N'SecondaryNotificationEmail', 'COLUMN';

ALTER TABLE [Users] ADD [ReceiveSystemAlerts] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421062449_AddAdminPreferences', N'9.0.2');

ALTER TABLE [Vehicles] ADD [CurrentMileage] decimal(18,2) NULL;

ALTER TABLE [Vehicles] ADD [WarehouseId] int NULL;

ALTER TABLE [DriverProfiles] ADD [CoverageArea] nvarchar(500) NULL;

CREATE INDEX [IX_Vehicles_WarehouseId] ON [Vehicles] ([WarehouseId]);

ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421074003_EnterpriseLogisticsOverhaul', N'9.0.2');

ALTER TABLE [Users] ADD [AlertDailySummary] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Users] ADD [AlertNewRegistration] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Users] ADD [AlertSystemError] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Users] ADD [LanguagePreference] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [ThemePreference] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [TwoFactorEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421082023_AddAdminSettingsFields', N'9.0.2');

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

DECLARE @var77 sysname;
SELECT @var77 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'PerformedBy');
IF @var77 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT [' + @var77 + '];');
ALTER TABLE [AuditLogs] DROP COLUMN [PerformedBy];

DECLARE @var78 sysname;
SELECT @var78 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'UserId');
IF @var78 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT [' + @var78 + '];');
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

ALTER TABLE [Users] ADD [TwoFactorSecret] nvarchar(100) NULL;

CREATE TABLE [UserSessions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [SessionToken] nvarchar(100) NOT NULL,
    [IpAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    [LoginTime] datetime2 NOT NULL,
    [LastActivityTime] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_UserSessions_UserId] ON [UserSessions] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260422073449_AddUserSessionsAnd2FASecret', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260422202019_AddApprovalStatusFields', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260423121455_AddTenderFields', N'9.0.2');

ALTER TABLE [Products] ADD [AvailableStock] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [DamagedStock] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [DispatchedStock] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [InTransitStock] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [LastStockUpdate] datetime2 NULL;

ALTER TABLE [Products] ADD [ReservedStock] int NOT NULL DEFAULT 0;

CREATE TABLE [InventoryMovements] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [WarehouseId] int NULL,
    [MovementType] nvarchar(50) NOT NULL,
    [Quantity] int NOT NULL,
    [BeforeAvailableStock] int NOT NULL,
    [BeforeReservedStock] int NOT NULL,
    [AfterAvailableStock] int NOT NULL,
    [AfterReservedStock] int NOT NULL,
    [ReferenceNumber] nvarchar(100) NOT NULL,
    [ReferenceType] nvarchar(50) NOT NULL,
    [ReferenceId] int NULL,
    [PerformedBy] int NULL,
    [Reason] nvarchar(500) NOT NULL,
    [DocumentReference] nvarchar(255) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryMovements_Users_PerformedBy] FOREIGN KEY ([PerformedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryMovements_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [InventoryReservations] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [PurchaseOrderId] int NULL,
    [OrderId] int NULL,
    [SupplierId] int NOT NULL,
    [WarehouseId] int NULL,
    [Quantity] int NOT NULL,
    [ReservedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [Status] nvarchar(30) NOT NULL DEFAULT N'Pending',
    [ReleasedAt] datetime2 NULL,
    [PickedBy] int NULL,
    [PickedAt] datetime2 NULL,
    [PackedBy] int NULL,
    [PackedAt] datetime2 NULL,
    [ShippedBy] int NULL,
    [ShippedAt] datetime2 NULL,
    [Priority] int NOT NULL DEFAULT 1,
    [Notes] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_InventoryReservations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryReservations_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryReservations_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryReservations_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryReservations_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryReservations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE SET NULL
);

CREATE INDEX [IX_InventoryMovement_Product_Date] ON [InventoryMovements] ([ProductId], [CreatedAt]);

CREATE INDEX [IX_InventoryMovement_ReferenceNumber] ON [InventoryMovements] ([ReferenceNumber]);

CREATE INDEX [IX_InventoryMovement_Type] ON [InventoryMovements] ([MovementType]);

CREATE INDEX [IX_InventoryMovements_PerformedBy] ON [InventoryMovements] ([PerformedBy]);

CREATE INDEX [IX_InventoryMovements_WarehouseId] ON [InventoryMovements] ([WarehouseId]);

CREATE INDEX [IX_InventoryReservation_ExpiresAt] ON [InventoryReservations] ([ExpiresAt]);

CREATE INDEX [IX_InventoryReservation_OrderId] ON [InventoryReservations] ([OrderId]);

CREATE INDEX [IX_InventoryReservation_Product_Status] ON [InventoryReservations] ([ProductId], [Status]);

CREATE INDEX [IX_InventoryReservation_PurchaseOrderId] ON [InventoryReservations] ([PurchaseOrderId]);

CREATE INDEX [IX_InventoryReservations_SupplierId] ON [InventoryReservations] ([SupplierId]);

CREATE INDEX [IX_InventoryReservations_WarehouseId] ON [InventoryReservations] ([WarehouseId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260423151654_AddInventoryManagementTables', N'9.0.2');

CREATE TABLE [InventoryAdjustments] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [WarehouseId] int NULL,
    [QuantityChange] int NOT NULL,
    [AdjustmentType] nvarchar(50) NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [ApprovedById] int NULL,
    [ApprovedAt] datetime2 NULL,
    [PerformedById] int NULL,
    [DocumentReference] nvarchar(255) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryAdjustments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryAdjustments_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryAdjustments_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryAdjustments_Users_PerformedById] FOREIGN KEY ([PerformedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryAdjustments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [InventorySnapshots] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [WarehouseId] int NULL,
    [AvailableStock] int NOT NULL,
    [ReservedStock] int NOT NULL,
    [DispatchedStock] int NOT NULL,
    [DamagedStock] int NOT NULL,
    [InTransitStock] int NOT NULL,
    [SnapshotDate] date NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventorySnapshots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventorySnapshots_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventorySnapshots_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StockTransfers] (
    [Id] int NOT NULL IDENTITY,
    [SourceWarehouseId] int NOT NULL,
    [DestinationWarehouseId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [RequestedById] int NULL,
    [ApprovedById] int NULL,
    [RequestedAt] datetime2 NULL,
    [ApprovedAt] datetime2 NULL,
    [ShippedAt] datetime2 NULL,
    [ReceivedAt] datetime2 NULL,
    [Notes] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StockTransfers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransfers_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockTransfers_SupplierEmployees_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockTransfers_SupplierEmployees_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [SupplierEmployees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockTransfers_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockTransfers_Warehouses_SourceWarehouseId] FOREIGN KEY ([SourceWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_InventoryAdjustments_ApprovedById] ON [InventoryAdjustments] ([ApprovedById]);

CREATE INDEX [IX_InventoryAdjustments_PerformedById] ON [InventoryAdjustments] ([PerformedById]);

CREATE INDEX [IX_InventoryAdjustments_ProductId] ON [InventoryAdjustments] ([ProductId]);

CREATE INDEX [IX_InventoryAdjustments_WarehouseId] ON [InventoryAdjustments] ([WarehouseId]);

CREATE INDEX [IX_InventorySnapshots_ProductId] ON [InventorySnapshots] ([ProductId]);

CREATE INDEX [IX_InventorySnapshots_WarehouseId] ON [InventorySnapshots] ([WarehouseId]);

CREATE INDEX [IX_StockTransfers_ApprovedById] ON [StockTransfers] ([ApprovedById]);

CREATE INDEX [IX_StockTransfers_DestinationWarehouseId] ON [StockTransfers] ([DestinationWarehouseId]);

CREATE INDEX [IX_StockTransfers_ProductId] ON [StockTransfers] ([ProductId]);

CREATE INDEX [IX_StockTransfers_RequestedById] ON [StockTransfers] ([RequestedById]);

CREATE INDEX [IX_StockTransfers_SourceWarehouseId] ON [StockTransfers] ([SourceWarehouseId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260423173517_AddCompleteInventoryLogicV3', N'9.0.2');

ALTER TABLE [SupplierEmployees] ADD [IsOnDuty] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [SupplierEmployees] ADD [WorkingHoursStart] time NULL;

ALTER TABLE [SupplierEmployees] ADD [WorkingHoursEnd] time NULL;

ALTER TABLE [SupplierEmployees] ADD [MaxDailyDeliveries] int NOT NULL DEFAULT 10;

ALTER TABLE [SupplierEmployees] ADD [RequireProofPhoto] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [SupplierEmployees] ADD [RequireSignature] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [SupplierEmployees] ADD [AutoAcceptAssignments] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [AllowNightDeliveries] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [NotifyNewAssignment] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [SupplierEmployees] ADD [SmsNotificationNumber] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260423185827_AddDeliveryAgentSettings', N'9.0.2');

ALTER TABLE [Suppliers] ADD [CommissionRate] decimal(5,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Commissions] ADD [AmountPaid] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Commissions] ADD [IsFullyPaid] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Commissions] ADD [RemainingBalance] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Commissions] ADD [SupplierPayoutAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Commissions] ADD [SupplierPayoutDate] datetime2 NULL;

ALTER TABLE [Commissions] ADD [SupplierPayoutStatus] nvarchar(20) NULL;

CREATE TABLE [DeadLetterWebhooks] (
    [Id] int NOT NULL IDENTITY,
    [Payload] nvarchar(max) NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [RetryCount] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DeadLetterWebhooks] PRIMARY KEY ([Id])
);

CREATE TABLE [Refunds] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [ReturnId] int NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Refunds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Refunds_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Refunds_OrderId] ON [Refunds] ([OrderId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260423200418_AddAdvancedPaymentSchema', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260423202955_MergeFix_CombinedFeatures', N'9.0.2');

ALTER TABLE [PurchaseOrders] ADD [CancellationReason] nvarchar(500) NULL;

ALTER TABLE [Orders] ADD [CancellationReason] nvarchar(500) NULL;

ALTER TABLE [Orders] ADD [CancelledAt] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424071623_AddCancellationReasonToOrders', N'9.0.2');

ALTER TABLE [SupplierEmployees] DROP CONSTRAINT [FK_SupplierEmployees_Users_UserId];

CREATE TABLE [SupportTickets] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Subject] nvarchar(100) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ResolvedAt] datetime2 NULL,
    CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupportTickets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_SupportTickets_UserId] ON [SupportTickets] ([UserId]);

ALTER TABLE [SupplierEmployees] ADD CONSTRAINT [FK_SupplierEmployees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424083305_AddSupportTickets', N'9.0.2');

ALTER TABLE [InventoryAdjustments] DROP CONSTRAINT [FK_InventoryAdjustments_Products_ProductId];

ALTER TABLE [InventoryAdjustments] DROP CONSTRAINT [FK_InventoryAdjustments_Users_ApprovedById];

ALTER TABLE [InventoryAdjustments] DROP CONSTRAINT [FK_InventoryAdjustments_Warehouses_WarehouseId];

ALTER TABLE [Products] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Products] ADD [RowVersion] rowversion NOT NULL;

ALTER TABLE [InventoryMovements] ADD [Remarks] nvarchar(max) NULL;

ALTER TABLE [Inventories] ADD [RowVersion] rowversion NOT NULL;

CREATE TABLE [InboundShipments] (
    [Id] int NOT NULL IDENTITY,
    [ShipmentNumber] nvarchar(50) NOT NULL,
    [SupplierId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ExpectedArrival] datetime2 NULL,
    [ReceivedDate] datetime2 NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InboundShipments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InboundShipments_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InboundShipments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [InboundShipmentItems] (
    [Id] int NOT NULL IDENTITY,
    [InboundShipmentId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ExpectedQuantity] int NOT NULL,
    [ReceivedQuantity] int NOT NULL,
    [DamagedQuantity] int NOT NULL,
    [BatchNumber] nvarchar(200) NULL,
    [ExpiryDate] datetime2 NULL,
    CONSTRAINT [PK_InboundShipmentItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InboundShipmentItems_InboundShipments_InboundShipmentId] FOREIGN KEY ([InboundShipmentId]) REFERENCES [InboundShipments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InboundShipmentItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_Warehouses_WarehouseCode] ON [Warehouses] ([WarehouseCode]) WHERE [WarehouseCode] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Vehicles_LicensePlate] ON [Vehicles] ([LicensePlate]);

CREATE UNIQUE INDEX [IX_SupplierEmployees_Email] ON [SupplierEmployees] ([Email]);

CREATE UNIQUE INDEX [IX_SupplierEmployees_Phone] ON [SupplierEmployees] ([Phone]);

CREATE INDEX [IX_InboundShipmentItems_InboundShipmentId] ON [InboundShipmentItems] ([InboundShipmentId]);

CREATE INDEX [IX_InboundShipmentItems_ProductId] ON [InboundShipmentItems] ([ProductId]);

CREATE INDEX [IX_InboundShipments_SupplierId] ON [InboundShipments] ([SupplierId]);

CREATE INDEX [IX_InboundShipments_WarehouseId] ON [InboundShipments] ([WarehouseId]);

ALTER TABLE [InventoryAdjustments] ADD CONSTRAINT [FK_InventoryAdjustments_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE;

ALTER TABLE [InventoryAdjustments] ADD CONSTRAINT [FK_InventoryAdjustments_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]);

ALTER TABLE [InventoryAdjustments] ADD CONSTRAINT [FK_InventoryAdjustments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424113043_AddInboundLogisticsAndProductHardening', N'9.0.2');

ALTER TABLE [Suppliers] ADD [CompanyDescription] nvarchar(max) NULL;

ALTER TABLE [Suppliers] ADD [PickupAddress] nvarchar(max) NULL;

ALTER TABLE [Suppliers] ADD [WebsiteUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424115222_AddSupplierBasicInfo', N'9.0.2');

ALTER TABLE [Suppliers] ADD [CompanyLogo] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424121151_AddSupplierLogo', N'9.0.2');

CREATE TABLE [BankAccounts] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [BankName] nvarchar(100) NOT NULL,
    [AccountHolderName] nvarchar(100) NOT NULL,
    [AccountNumber] nvarchar(50) NOT NULL,
    [Branch] nvarchar(100) NULL,
    [SwiftCode] nvarchar(20) NULL,
    [IsPrimary] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankAccounts_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_BankAccounts_SupplierId] ON [BankAccounts] ([SupplierId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424122427_AddBankAccountsTable', N'9.0.2');

ALTER TABLE [Suppliers] ADD [NotifyBidAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Suppliers] ADD [NotifyChannel] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Suppliers] ADD [NotifyDisputeAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Suppliers] ADD [NotifyLowStockAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Suppliers] ADD [NotifyOrderAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Suppliers] ADD [NotifyPaymentAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424124224_AddNotificationAnd2FA', N'9.0.2');

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Vehicles') AND name = 'Make') ALTER TABLE Vehicles ADD Make nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Vehicles') AND name = 'Brand') ALTER TABLE Vehicles ADD Brand nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Vehicles') AND name = 'Model') ALTER TABLE Vehicles ADD Model nvarchar(100) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424142039_AddVehicleMakeModel', N'9.0.2');

ALTER TABLE [SupplierEmployees] ADD [AssignedZones] nvarchar(max) NULL;

ALTER TABLE [SupplierEmployees] ADD [DailyCutoffTime] time NULL;

ALTER TABLE [SupplierEmployees] ADD [DefaultPackingPriority] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [SupplierEmployees] ADD [EnableVoicePicking] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [PrintLabelFormat] nvarchar(50) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424193608_AddWarehouseAdvancedSettings', N'9.0.2');

ALTER TABLE [SupplierEmployees] ADD [EnableReminders] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [SupplierEmployees] ADD [EnableTaskAlerts] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424195818_AddWarehouseNotificationSettings', N'9.0.2');

ALTER TABLE [Retailers] ADD [AutoAcceptPreferredBids] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [AutoNotifyNewTenders] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [BlockedSuppliers] nvarchar(max) NULL;

ALTER TABLE [Retailers] ADD [BudgetMax] decimal(18,2) NULL;

ALTER TABLE [Retailers] ADD [BudgetMin] decimal(18,2) NULL;

ALTER TABLE [Retailers] ADD [ContactPersonEmail] nvarchar(100) NULL;

ALTER TABLE [Retailers] ADD [ContactPersonName] nvarchar(100) NULL;

ALTER TABLE [Retailers] ADD [ContactPersonPhone] nvarchar(20) NULL;

ALTER TABLE [Retailers] ADD [DefaultBillingAddress] nvarchar(max) NULL;

ALTER TABLE [Retailers] ADD [DefaultShippingAddress] nvarchar(max) NULL;

ALTER TABLE [Retailers] ADD [DefaultShippingMethod] nvarchar(100) NULL;

ALTER TABLE [Retailers] ADD [DefaultTenderClosingDays] int NOT NULL DEFAULT 0;

ALTER TABLE [Retailers] ADD [FavoriteSuppliers] nvarchar(max) NULL;

ALTER TABLE [Retailers] ADD [PreferredCategories] nvarchar(max) NULL;

ALTER TABLE [Retailers] ADD [PreferredDeliveryTimeline] int NOT NULL DEFAULT 0;

ALTER TABLE [Retailers] ADD [PreferredPaymentMethod] nvarchar(max) NULL;

ALTER TABLE [Retailers] ADD [ProofOfDeliveryRequired] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [SupplierRatingThreshold] int NOT NULL DEFAULT 0;

ALTER TABLE [Retailers] ADD [WebsiteUrl] nvarchar(200) NULL;

ALTER TABLE [Retailers] ADD [YearsInBusiness] int NOT NULL DEFAULT 0;

CREATE TABLE [RetailerAddresses] (
    [Id] int NOT NULL IDENTITY,
    [RetailerId] int NOT NULL,
    [AddressType] nvarchar(50) NOT NULL,
    [AddressLine] nvarchar(200) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Region] nvarchar(100) NULL,
    [Country] nvarchar(100) NOT NULL,
    [PostalCode] nvarchar(20) NULL,
    [IsDefault] bit NOT NULL,
    CONSTRAINT [PK_RetailerAddresses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetailerAddresses_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RetailerPaymentMethods] (
    [Id] int NOT NULL IDENTITY,
    [RetailerId] int NOT NULL,
    [MethodType] nvarchar(50) NOT NULL,
    [Details] nvarchar(200) NOT NULL,
    [IsDefault] bit NOT NULL,
    [Provider] nvarchar(max) NULL,
    [ExpiryDate] datetime2 NULL,
    CONSTRAINT [PK_RetailerPaymentMethods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetailerPaymentMethods_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RetailerAddresses_RetailerId] ON [RetailerAddresses] ([RetailerId]);

CREATE INDEX [IX_RetailerPaymentMethods_RetailerId] ON [RetailerPaymentMethods] ([RetailerId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424204356_AddRetailerSettings', N'9.0.2');

ALTER TABLE [Retailers] ADD [BidAcceptedAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [DeliveryNotifications] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [LowStockAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [NewTenderMatchAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [OrderDeliveredAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [OrderShippedAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Retailers] ADD [PriceDropAlert] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE TABLE [RetailerPreferences] (
    [Id] int NOT NULL IDENTITY,
    [RetailerId] int NOT NULL,
    [NewTenderMatchAlert] bit NOT NULL,
    [BidAcceptedAlert] bit NOT NULL,
    [OrderShippedAlert] bit NOT NULL,
    [OrderDeliveredAlert] bit NOT NULL,
    [LowStockAlert] bit NOT NULL,
    [PriceDropAlert] bit NOT NULL,
    [Theme] nvarchar(max) NULL,
    [Language] nvarchar(max) NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RetailerPreferences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetailerPreferences_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_RetailerPreferences_RetailerId] ON [RetailerPreferences] ([RetailerId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425064434_AddFullRetailerSettings', N'9.0.2');

CREATE TABLE [EmailTemplates] (
    [Id] int NOT NULL IDENTITY,
    [EventType] nvarchar(100) NOT NULL,
    [Subject] nvarchar(255) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id])
);

CREATE TABLE [SystemConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(max) NULL,
    [Description] nvarchar(255) NULL,
    [DataType] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_SystemConfigurations] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425131207_AddAdminSystemConfig', N'9.0.2');

ALTER TABLE [InventoryAdjustments] DROP CONSTRAINT [FK_InventoryAdjustments_Products_ProductId];

DROP INDEX [IX_Products_SupplierId_ProductName] ON [Products];
DECLARE @var79 sysname;
SELECT @var79 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ProductName');
IF @var79 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var79 + '];');
ALTER TABLE [Products] ALTER COLUMN [ProductName] nvarchar(450) NOT NULL;
CREATE UNIQUE INDEX [IX_Products_SupplierId_ProductName] ON [Products] ([SupplierId], [ProductName]);

DECLARE @var80 sysname;
SELECT @var80 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryReservations]') AND [c].[name] = N'Priority');
IF @var80 IS NOT NULL EXEC(N'ALTER TABLE [InventoryReservations] DROP CONSTRAINT [' + @var80 + '];');
ALTER TABLE [InventoryReservations] ADD DEFAULT 1 FOR [Priority];

DECLARE @var81 sysname;
SELECT @var81 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryReservations]') AND [c].[name] = N'CreatedAt');
IF @var81 IS NOT NULL EXEC(N'ALTER TABLE [InventoryReservations] DROP CONSTRAINT [' + @var81 + '];');
ALTER TABLE [InventoryReservations] ADD DEFAULT (GETDATE()) FOR [CreatedAt];

DECLARE @var82 sysname;
SELECT @var82 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryMovements]') AND [c].[name] = N'Reason');
IF @var82 IS NOT NULL EXEC(N'ALTER TABLE [InventoryMovements] DROP CONSTRAINT [' + @var82 + '];');
ALTER TABLE [InventoryMovements] ALTER COLUMN [Reason] nvarchar(500) NULL;

DECLARE @var83 sysname;
SELECT @var83 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventoryMovements]') AND [c].[name] = N'DocumentReference');
IF @var83 IS NOT NULL EXEC(N'ALTER TABLE [InventoryMovements] DROP CONSTRAINT [' + @var83 + '];');
ALTER TABLE [InventoryMovements] ALTER COLUMN [DocumentReference] nvarchar(255) NULL;

DECLARE @var84 sysname;
SELECT @var84 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Deliveries]') AND [c].[name] = N'ProofOfDelivery');
IF @var84 IS NOT NULL EXEC(N'ALTER TABLE [Deliveries] DROP CONSTRAINT [' + @var84 + '];');
ALTER TABLE [Deliveries] ALTER COLUMN [ProofOfDelivery] nvarchar(255) NULL;

ALTER TABLE [InventoryAdjustments] ADD CONSTRAINT [FK_InventoryAdjustments_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425194610_MergeReconciliationFix', N'9.0.2');

ALTER TABLE [Suppliers] ADD [Region] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Retailers] ADD [Region] nvarchar(100) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260427184336_AddRegionToRetailerAndSupplier', N'9.0.2');

COMMIT;
GO

