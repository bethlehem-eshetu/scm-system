CREATE TABLE [ProductCategories] (
    [Id] int NOT NULL IDENTITY,
    [CategoryName] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ParentCategoryId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductCategories_ProductCategories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SystemSettings] (
    [Id] int NOT NULL IDENTITY,
    [SettingKey] nvarchar(100) NOT NULL,
    [SettingValue] nvarchar(max) NOT NULL,
    [Description] nvarchar(255) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
);
GO


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
GO


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
GO


CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Title] nvarchar(100) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Type] nvarchar(50) NULL,
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [ActionUrl] nvarchar(max) NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
GO


CREATE TABLE [Retailers] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [BusinessName] nvarchar(150) NOT NULL,
    [BusinessType] nvarchar(50) NULL,
    [TaxIdentificationNumber] nvarchar(50) NULL,
    [BusinessLicenseNumber] nvarchar(100) NULL,
    [BusinessAddress] nvarchar(200) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Country] nvarchar(100) NOT NULL,
    [StoreSize] nvarchar(20) NULL,
    [BusinessLogo] nvarchar(255) NULL,
    [Description] nvarchar(max) NULL,
    [IsVerified] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Retailers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Retailers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [CompanyName] nvarchar(150) NOT NULL,
    [BusinessType] nvarchar(50) NULL,
    [LicenseNumber] nvarchar(100) NOT NULL,
    [LicenseFilePath] nvarchar(255) NULL,
    [TaxIdentificationNumber] nvarchar(50) NULL,
    [CompanyAddress] nvarchar(200) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Country] nvarchar(100) NOT NULL,
    [Website] nvarchar(255) NULL,
    [Description] nvarchar(max) NULL,
    [VerificationStatus] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Suppliers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [ProductName] nvarchar(150) NOT NULL,
    [BasePrice] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [SKU] nvarchar(50) NOT NULL,
    [ImageUrl] nvarchar(255) NOT NULL,
    [Quantity] int NOT NULL,
    [Unit] nvarchar(50) NOT NULL,
    [IsAvailable] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


CREATE TABLE [Tenders] (
    [Id] int NOT NULL IDENTITY,
    [RetailerId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Quantity] int NOT NULL,
    [ClosingDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SupplierId] int NULL,
    CONSTRAINT [PK_Tenders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tenders_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenders_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
);
GO


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
GO


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
GO


CREATE TABLE [ProductAttributeValues] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [AttributeId] int NOT NULL,
    [Value] nvarchar(255) NOT NULL,
    CONSTRAINT [PK_ProductAttributeValues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductAttributeValues_ProductAttributeDefinitions_AttributeId] FOREIGN KEY ([AttributeId]) REFERENCES [ProductAttributeDefinitions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProductAttributeValues_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


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
GO


CREATE TABLE [TenderItems] (
    [Id] int NOT NULL IDENTITY,
    [TenderId] int NOT NULL,
    [ProductName] nvarchar(150) NOT NULL,
    [Quantity] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_TenderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenderItems_Tenders_TenderId] FOREIGN KEY ([TenderId]) REFERENCES [Tenders] ([Id]) ON DELETE CASCADE
);
GO


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
GO


CREATE TABLE [MessageViolations] (
    [Id] int NOT NULL IDENTITY,
    [MessageId] int NOT NULL,
    [ViolationType] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsResolved] bit NOT NULL,
    CONSTRAINT [PK_MessageViolations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageViolations_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseOrders] (
    [Id] int NOT NULL IDENTITY,
    [PONumber] nvarchar(50) NOT NULL,
    [RetailerId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [TenderBidId] int NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(100) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PurchaseOrders_Retailers_RetailerId] FOREIGN KEY ([RetailerId]) REFERENCES [Retailers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_TenderBids_TenderBidId] FOREIGN KEY ([TenderBidId]) REFERENCES [TenderBids] ([Id]) ON DELETE SET NULL
);
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


CREATE TABLE [DeliveryTrackings] (
    [Id] int NOT NULL IDENTITY,
    [DeliveryId] int NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [StatusNote] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_DeliveryTrackings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeliveryTrackings_Deliveries_DeliveryId] FOREIGN KEY ([DeliveryId]) REFERENCES [Deliveries] ([Id]) ON DELETE CASCADE
);
GO


CREATE UNIQUE INDEX [IX_Commissions_PurchaseOrderId] ON [Commissions] ([PurchaseOrderId]);
GO


CREATE INDEX [IX_Commissions_SupplierId] ON [Commissions] ([SupplierId]);
GO


CREATE INDEX [IX_Conversations_RetailerId] ON [Conversations] ([RetailerId]);
GO


CREATE UNIQUE INDEX [IX_Conversations_SupplierId_RetailerId] ON [Conversations] ([SupplierId], [RetailerId]);
GO


CREATE INDEX [IX_Deliveries_DeliveryEmployeeId] ON [Deliveries] ([DeliveryEmployeeId]);
GO


CREATE UNIQUE INDEX [IX_Deliveries_OrderId] ON [Deliveries] ([OrderId]);
GO


CREATE INDEX [IX_DeliveryTrackings_DeliveryId] ON [DeliveryTrackings] ([DeliveryId]);
GO


CREATE UNIQUE INDEX [IX_Inventories_ProductId] ON [Inventories] ([ProductId]);
GO


CREATE INDEX [IX_Inventories_WarehouseId] ON [Inventories] ([WarehouseId]);
GO


CREATE INDEX [IX_Messages_ConversationId] ON [Messages] ([ConversationId]);
GO


CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
GO


CREATE UNIQUE INDEX [IX_MessageViolations_MessageId] ON [MessageViolations] ([MessageId]);
GO


CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
GO


CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
GO


CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
GO


CREATE UNIQUE INDEX [IX_Orders_PurchaseOrderId] ON [Orders] ([PurchaseOrderId]);
GO


CREATE INDEX [IX_Orders_RetailerId] ON [Orders] ([RetailerId]);
GO


CREATE INDEX [IX_Orders_SupplierId] ON [Orders] ([SupplierId]);
GO


CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);
GO


CREATE INDEX [IX_Penalties_UserId] ON [Penalties] ([UserId]);
GO


CREATE INDEX [IX_ProductAttributeDefinitions_CategoryId] ON [ProductAttributeDefinitions] ([CategoryId]);
GO


CREATE INDEX [IX_ProductAttributeValues_AttributeId] ON [ProductAttributeValues] ([AttributeId]);
GO


CREATE INDEX [IX_ProductAttributeValues_ProductId] ON [ProductAttributeValues] ([ProductId]);
GO


CREATE INDEX [IX_ProductCategories_ParentCategoryId] ON [ProductCategories] ([ParentCategoryId]);
GO


CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO


CREATE UNIQUE INDEX [IX_Products_SKU] ON [Products] ([SKU]);
GO


CREATE UNIQUE INDEX [IX_Products_SupplierId_ProductName] ON [Products] ([SupplierId], [ProductName]);
GO


CREATE INDEX [IX_PurchaseOrderItems_ProductId] ON [PurchaseOrderItems] ([ProductId]);
GO


CREATE INDEX [IX_PurchaseOrderItems_PurchaseOrderId] ON [PurchaseOrderItems] ([PurchaseOrderId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseOrders_PONumber] ON [PurchaseOrders] ([PONumber]);
GO


CREATE INDEX [IX_PurchaseOrders_ProductId] ON [PurchaseOrders] ([ProductId]);
GO


CREATE INDEX [IX_PurchaseOrders_RetailerId] ON [PurchaseOrders] ([RetailerId]);
GO


CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseOrders_TenderBidId] ON [PurchaseOrders] ([TenderBidId]) WHERE [TenderBidId] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_Ratings_PurchaseOrderId] ON [Ratings] ([PurchaseOrderId]);
GO


CREATE INDEX [IX_Ratings_RetailerId] ON [Ratings] ([RetailerId]);
GO


CREATE INDEX [IX_Ratings_SupplierId] ON [Ratings] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_Retailers_UserId] ON [Retailers] ([UserId]);
GO


CREATE INDEX [IX_SupplierEmployees_SupplierId] ON [SupplierEmployees] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_SupplierEmployees_UserId] ON [SupplierEmployees] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Suppliers_LicenseNumber] ON [Suppliers] ([LicenseNumber]);
GO


CREATE UNIQUE INDEX [IX_Suppliers_TaxIdentificationNumber] ON [Suppliers] ([TaxIdentificationNumber]) WHERE [TaxIdentificationNumber] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_Suppliers_UserId] ON [Suppliers] ([UserId]);
GO


CREATE INDEX [IX_TenderBids_SupplierId] ON [TenderBids] ([SupplierId]);
GO


CREATE INDEX [IX_TenderBids_TenderId] ON [TenderBids] ([TenderId]);
GO


CREATE INDEX [IX_TenderItems_TenderId] ON [TenderItems] ([TenderId]);
GO


CREATE INDEX [IX_Tenders_CategoryId] ON [Tenders] ([CategoryId]);
GO


CREATE INDEX [IX_Tenders_RetailerId] ON [Tenders] ([RetailerId]);
GO


CREATE INDEX [IX_Tenders_SupplierId] ON [Tenders] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE INDEX [IX_Warehouses_SupplierId] ON [Warehouses] ([SupplierId]);
GO


