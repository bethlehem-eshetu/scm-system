-- Fix for Retailer Dashboard Missing Columns
-- This script safely adds SupplierId and UpdatedAt columns to relevant tables

-- 1. Fix PurchaseOrders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrders]') AND name = N'SupplierId')
BEGIN
    ALTER TABLE [dbo].[PurchaseOrders] ADD [SupplierId] int NULL;
    PRINT 'Added SupplierId to PurchaseOrders';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrders]') AND name = N'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[PurchaseOrders] ADD [UpdatedAt] datetime2 NULL;
    PRINT 'Added UpdatedAt to PurchaseOrders';
END
GO

-- 2. Fix Tenders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenders]') AND name = N'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Tenders] ADD [UpdatedAt] datetime2 NULL;
    PRINT 'Added UpdatedAt to Tenders';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenders]') AND name = N'SupplierId')
BEGIN
    ALTER TABLE [dbo].[Tenders] ADD [SupplierId] int NULL;
    PRINT 'Added SupplierId to Tenders';
END
GO

-- 3. Fix Orders (referenced later in Dashboard)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [UpdatedAt] datetime2 NULL;
    PRINT 'Added UpdatedAt to Orders';
END
GO

-- 4. Fix Retailers (just in case)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Retailers]') AND name = N'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Retailers] ADD [UpdatedAt] datetime2 NULL;
    PRINT 'Added UpdatedAt to Retailers';
END
GO

-- Update existing NULLs to a default date to avoid issues if EF expects values
UPDATE [dbo].[PurchaseOrders] SET [UpdatedAt] = [OrderDate] WHERE [UpdatedAt] IS NULL;
UPDATE [dbo].[Tenders] SET [UpdatedAt] = [CreatedAt] WHERE [UpdatedAt] IS NULL;
UPDATE [dbo].[Orders] SET [UpdatedAt] = [CreatedAt] WHERE [UpdatedAt] IS NULL;
UPDATE [dbo].[Retailers] SET [UpdatedAt] = [CreatedAt] WHERE [UpdatedAt] IS NULL;
GO
