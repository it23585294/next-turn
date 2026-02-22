-- =============================================
-- NextTurn - Initial Database Schema
-- Version: 001
-- =============================================

-- Create Organizations table
IF NOT EXISTS (
    SELECT *
    FROM sys.tables
    WHERE name = 'Organizations'
)
BEGIN
    CREATE TABLE Organizations (
        OrganizationId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO