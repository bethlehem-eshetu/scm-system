-- Fix for Supplier Dashboard Missing Columns
-- This script safely adds CommissionRateAtTransaction to Commissions table

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Commissions]') AND name = N'CommissionRateAtTransaction')
BEGIN
    ALTER TABLE [dbo].[Commissions] ADD [CommissionRateAtTransaction] decimal(5,2) NOT NULL DEFAULT 0;
    PRINT 'Added CommissionRateAtTransaction to Commissions';
END
GO

-- Update existing records to match CommissionRate if applicable
UPDATE [dbo].[Commissions] SET [CommissionRateAtTransaction] = [CommissionRate] WHERE [CommissionRateAtTransaction] = 0;
GO
