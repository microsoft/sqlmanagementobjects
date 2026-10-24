--=======================================================
--This file contains statements to run that are valid
--for any On-Prem SQL Server version 13.0 (2016) and above
--=======================================================

--=======================
--= DATABASE MASTER KEY =
--=======================

USE [master]
GO

--First create master key for server (on master db)
IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE symmetric_key_id = 101)
BEGIN
	CREATE MASTER KEY ENCRYPTION BY $(RandomTSqlPassword)
END
GO

--Then create server-scoped certificate
IF NOT EXISTS (SELECT * FROM sys.certificates where name = 'DEK_SmoTestSuite_ServerCertificate')
BEGIN
	CREATE CERTIFICATE DEK_SmoTestSuite_ServerCertificate
		WITH SUBJECT = 'Database Encryption Key Test Server Certificate',
		EXPIRY_DATE = '30001031',
		START_DATE = '20121031'
END
GO

USE [$(BracketEscapedDatabaseName)]
GO

--And finally create the Database Encryption Key in the target database, using the certificate we opened earlier
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE DEK_SmoTestSuite_ServerCertificate