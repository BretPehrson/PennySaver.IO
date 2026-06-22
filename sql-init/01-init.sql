-- Starter initialization for PennySaver DB (SQL Server)
-- Creates database and tables

SET NOCOUNT ON;

IF NOT EXISTS(SELECT name FROM sys.databases WHERE name = N'PennySaverDb')
BEGIN
    CREATE DATABASE PennySaverDb;
END
GO

USE PennySaverDb;
GO

IF OBJECT_ID('dbo.Account','U') IS NULL
BEGIN
    CREATE TABLE dbo.Account (
        AccountId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Type] NVARCHAR(MAX) NULL,
        CurrentBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID('dbo.Category','U') IS NULL
BEGIN
    CREATE TABLE dbo.Category (
        CategoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        ColorCode NVARCHAR(7) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID('dbo.Transaction','U') IS NULL
BEGIN
    CREATE TABLE dbo.[Transaction] (
        TransactionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AccountId INT NOT NULL FOREIGN KEY REFERENCES dbo.Account(AccountId),
        Amount DECIMAL(18,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [Description] NVARCHAR(MAX) NULL,
        CategoryId INT NULL FOREIGN KEY REFERENCES dbo.Category(CategoryId)
    );
END
GO