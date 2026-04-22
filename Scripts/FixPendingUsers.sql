SET QUOTED_IDENTIFIER ON;
GO
UPDATE Users 
SET IsApproved = 1, 
    IsFaydaVerified = 1, 
    AccountStatus = 'Active', 
    ApprovalStatus = 'Approved',
    FaydaStatus = 'Verified'
WHERE IsApproved = 1 OR AccountStatus = 'Active';
GO
