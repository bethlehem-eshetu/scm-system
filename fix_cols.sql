IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'ProductName' AND Object_ID = Object_ID(N'dbo.OrderItems'))
BEGIN
    ALTER TABLE [dbo].[OrderItems] ADD [ProductName] nvarchar(100) NULL;
    ALTER TABLE [dbo].[OrderItems] ADD [Description] nvarchar(max) NULL;
    ALTER TABLE [dbo].[OrderItems] ADD [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0;
END
GO
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'ProductName' AND Object_ID = Object_ID(N'dbo.PurchaseOrderItems'))
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderItems] ADD [ProductName] nvarchar(100) NULL;
    ALTER TABLE [dbo].[PurchaseOrderItems] ADD [Description] nvarchar(max) NULL;
    ALTER TABLE [dbo].[PurchaseOrderItems] ADD [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0;
END
GO
