# Introduction

This file will log substantial changes made to SMO between public releases to nuget.org.

## 161.44091.28

- Make ISmoScriptWriter interface public
- Enable apps to provide custom ISmoScriptWriter implementation to SqlScriptPublishModel and ScriptMaker
- Enabled Security Policy objects to be included in Transfer
- Change association of DatabaseEngineEdition.SqlOnDemand to DatabaseEngineType.SqlAzureDatabase
- Fix implementation of Microsoft.SqlServer.Management.HadrModel.FailoverTask.Perform to handle AvailabilityGroupClusterType.None correctly

## 161.42121.15-msdata

- Add netcoreapp3.1 build output
- Fix [logins using impersonation](https://github.com/microsoft/sqlmanagementobjects/issues/24)
- Expose OlapConnectionInfo class in non-netfx ConnectionInfo
- Expose WmiMgmtScopeConnection in non-netfx ConnectionInfo
- Enable OPTIMIZE_FOR_SEQUENTIAL_KEY index option for Azure SQL Database

## 161.41981.14-msdata, 161.41981.14-preview

- Expose [accelerated database recovery](https://github.com/microsoft/sqlmanagementobjects/issues/22) settings for Database class
- Enable Column.BindDefault on Azure SQL Database
- Add DestinationServerConnection property to Transfer
  - [Github issue 16](https://github.com/microsoft/sqlmanagementobjects/issues/16)
  - Allows for use of Azure SQL Database as a destination server
  - Enables full customization of the destination connection
- [Script User objects for Azure SQL Database correctly](https://github.com/microsoft/sqlmanagementobjects/issues/18)
- [Enable CreateOrAlter behavior for Scripter](https://github.com/microsoft/sqlmanagementobjects/issues/11)
- Fixed issue where MaxSize value was reported as negative for Hyperscale Azure SQL Databases - Added new property "IsMaxSizeApplicable" and disabled negative values for Hyperscale Azure SQL Databases.

## 161.41011.9

- First non-preview release of major package version 161
- Microsoft.SqlServer.SqlManagementObjects.SSMS has binaries matching those shipping in SSMS 18.6
- Put `begin try/begin catch` around TSQL querying `sys.database_service_objectives` in Azure SQL Database. This view may throw if Azure control plane has an outage and was blocking expansion of the Databases node in SSMS.

## 161.40241.8-msdata and 161.40241.8-preview

- Increase package major version to 161. Assembly major version remains 16
- Change assembly minor version to 200 for Microsoft.SqlServer.SqlManagementObjects package
- Change NetFx binaries to use Microsoft.Data.SqlClient as their SQL client driver, replacing System.Data.SqlClient
- Created new package, Microsoft.SqlServer.SqlManagementObjects.SSMS, for use by SSMS and SSDT.
  This package has NetFx binaries still dependent on System.Data.SqlClient. It uses assembly minor version 100
- Update Microsoft.Data.SqlClient dependency to version 2.0.0
- Handle SQL error code 4060 during fetch of Database.DatabaseEngineEdition and use default value of Unknown
- Fixed Database.Size property to report the accurate size of the database when
  DatabaseEngineType is SqlAzureDatabase
- Fixed issue where Database.SpaceAvailable was reported as negative for Hyperscale Azure SQL Databases
  (the value is reported as 0, meaning *Unavailable*)
- Implement IObjectPermission on DatabaseScopedCredential. <https://github.com/microsoft/sqlmanagementobjects/issues/14>
- Enabled Server.EnumServerAttributes API on Azure SQL Database
- Enabled Lock enumeration APIs on Azure SQL Database
- Deleted the Database.CheckIdentityValues API
- Added new property "RequestMaximumMemoryGrantPercentageAsDouble" in WorkloadGroup to accept decimal values in Resource Governor (SQL2019 and above).
- Fixed a scripting issue with statistics on filtered indexes where the filter from the index would be scripted with the UPDATE STATISTICS TSQL.
- Enabled Security Policy and Security Predicate objects on Azure SQL DataWarehouse

## 160.2004021.0

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

## 160.1911221.0-preview

- Increase major version from 15 to 16
- Remove dependency on native batch parser from NetFx components
- Change NetStandard client driver to Microsoft.Data.SqlClient
- Add distribution property for DW materialized views
- Script FILLFACTOR for indexes on Azure SQL Database
