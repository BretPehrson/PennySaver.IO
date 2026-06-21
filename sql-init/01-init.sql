-- Starter initialization for PennySaver DB (SQL Server)
-- Creates database, a sample table and seed data

SET NOCOUNT ON;

IF NOT EXISTS(SELECT name FROM sys.databases WHERE name = N'PennySaverDb')
BEGIN
    CREATE DATABASE PennySaverDb;
END
GO

USE PennySaverDb;
GO

IF OBJECT_ID('dbo.Items','U') IS NULL
BEGIN
    CREATE TABLE dbo.Items (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items)
BEGIN
    INSERT INTO dbo.Items (Name, Description) VALUES
    (N'Sample Item', N'This is a starter row created by sql-init/01-init.sql');
END
GO
