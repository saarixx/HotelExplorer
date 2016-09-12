--Delete the HotelDB database if it exists.  
IF EXISTS(SELECT * from sys.databases WHERE name='HotelDB')  
BEGIN  
    DROP DATABASE HotelDB;  
END

--Create a new database called HotelDB  
CREATE DATABASE HotelDB;

USE HotelDB;

IF OBJECT_ID('dbo.Hotels', 'U') IS NOT NULL 
  DROP TABLE dbo.Hotels; 

CREATE TABLE [dbo].[Hotels]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY, 
    [Name] VARCHAR(100) NOT NULL, 
	[WebSite] INT NOT NULL,
    [Data] TEXT NOT NULL    
);