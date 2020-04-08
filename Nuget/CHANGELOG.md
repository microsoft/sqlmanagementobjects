# Introduction

This file will log substantial changes made to SMO between public releases to nuget.org.

## 160.1911221.0-preview

- Increase major version from 15 to 16
- Remove dependency on native batch parser from NetFx components
- Change NetStandard client driver to Microsoft.Data.SqlClient
- Add distribution property for DW materialized views
- Script FILLFACTOR for indexes on Azure SQL Database

## 160.201141.0-preview

- Remove unneeded "using" TSQL statements from Database.CheckTables method implementations
- Enable ColumnMasterKey properties Signature and AllowEnclaveComputations for Azure SQL DB
- Fix Database.EncryptionEnabled and Database.DatabaseEncryptionKey behavior during Database.Alter(). Now, this code will correctly create a new key using the server certificate named MyCertificate:

    ```C#
        db.EncryptionEnabled = true;
        db.DatabaseEncryptionKey.EncryptorName = "MyCertificate";
        db.DatabaseEncryptionKey.EncryptionAlgorithm = DatabaseEncryptionAlgorithm.Aes256;
        db.DatabaseEncryptionKey.EncryptionType = DatabaseEncryptionType.ServerCertificate;
        db.Alter()
    ```

- Fixed the "like" and "contains" URN filter functions to work with parameters containing single quotes. These operators can be used to optimally initialize collections:

    ```C#
    // populate the collection with databases that have Name starting with "RDA"
    var server = Server(new ServerConnection(sqlConnection));
    server.Databases.ClearAndInitialize("[like(@Name, 'RDA%')]", new string[] { });
    ```

- Make Table.Location property optional for creating or scripting external tables.
- Enable scripting of ANSI_PADDING settings for Azure SQL Database tables.
- Remove obsolete types ServerActiveDirectory and DatabaseActiveDirectory
- Added BLOB_STORAGE scripting support for external data sources
- Fixed [error scripting external tables](https://feedback.azure.com/forums/908035-sql-server/suggestions/38267746-cannot-script-external-table-in-ssms-18-2) for Azure SQL Database
- Replace Microsoft.SqlServer.Management.SqlParser.dll with a dependency to its Nuget package

## 160.2001141.0

- First non-preview 160 release, aligned with [SQL Server Management Studio](https://aka.ms/ssmsfullsetup) 18.5
- Script extended properties for Azure SQL Database objects
- Enable Jupyter Notebook output for SqlScriptPublishModel. SSMS 18.5 can output a Notebook for Azure Data Studio in Generate Scripts now.
- Fix issue where Table.EnableAllIndexes(Recreate) did nothing
- Fix Database.EnumObjectPermissions usage in NetStandard binaries
- Remove FORCE ORDER hint from table enumeration that was causing major performance issues
- Fix Transfer with PrefetchAllObjects == false for pre-Sql 2014 versions so it doesn't throw an exception
- Extend value range for platform, name, and engineEdition JSON properties of SQL Assessment targets with arrays of strings:
    
    ```JSON
        "target": {
            "platform": ["Windows", "Linux"],
            "name": ["master", "temp"]
        }
    ```
- Add 13 new [SQL Assessment rules](https://github.com/microsoft/sql-server-samples/blob/master/samples/manage/sql-assessment-api/release-notes.md)
- Fix help link in XTPHashAvgChainBuckets SQL Assessment rule
- Units for threshold parameter of FullBackup SQL Assessment rule changed from hours to days