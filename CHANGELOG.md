# Change log

Update this document for externally visible changes. Put most recent changes first.
Once we push a new version to nuget.org add a double hash header for that version.

## 172.76.0

- Fix scripting performance #165
- Add new audit action type INFORMATION_PROTECTION_OPERATION_GROUP
- Add new database level permissions ALTER ANY EXTERNAL MODEL, CREATE EXTERNAL MODEL and ALTER ANY INFORMATION PROTECTION

## 172.64.0

- Add DesignMode support to `QueryStoreOptions` class
- Add Vector data type support

## 172.61.0

- Remove major version restriction on Microsoft.Data.SqlClient dependency Fixes [Issue 188](https://github.com/microsoft/sqlmanagementobjects/issues/188)
- Remove net6 binaries
- Allow login information to be updated for an existing user
- Enabled AutoCreateStatisticsIncremental property configuration for Azure SQL database
- Change `Database.PrefetchObjects` to omit `ExtendedProperties` when passed a `ScriptingOptions` object that omits them. Fixes [Issue 177](https://github.com/microsoft/sqlmanagementobjects/issues/177)

## 172.52.0

- Add `ServerRole` support for Azure SQL database
- Add 170 compat support
- Update NetFx binaries to net472
- Remove obsolete formatter-based serialization constructors from non-Netfx Exceptions
- Add ConnectionOptions and Pushdown to `CREATE` and `ALTER` scripts for external data sources
- Added new `DateLastModified` property on Error Logs returned by `EnumErrorLogs()`. Its value is the same as the existing `CreateDate`,
  (which has been incorrectly namd for years) only properly stampted with the date/time offset information to allow easier 
  interoperability between client and servers in different time zones. `CreateDate` is essentially deprecated, and new applications
  should start using `DateLastModified` instead.
- Fixed an issue in the `EnumErrorLogs()` where `CreateDate` field could be `NULL` depending on the configuration of the machine 
  on which SQL Server was running on.
- Regex for the EXECUTE statement was updated to also match the shortened form 'EXEC'

## 171.30.0

- <b>BREAKING</b>: Move Transfer interfaces to Smo.Extended and remove unused/non-implemented interfaces. This is a breaking change that requires recompilation of apps that use Transfer.
- Change base class of `ConnectionException` to `Exception`
- Update major package version to 171
- add new database permission alter any external mirror for azure sql database

## 170.23.0

- Fix bug where creating Microsoft Entra ID logins for Azure SQL database and On Prem databases was disabled
- Upgraded SqlClient to 5.1.2 and removed direct Azure SDK dependencies from the nuget package
- Fix createdrop script error for versioned table in ledger database
- Fix database scoped extended events enumeration on Azure SQL database instances having DATABASE_DEFAULT catalog collation
- Improve scripting of dependency objects in Azure SQL database
- Added `ObjectId` parameter in User and Login create options
- Fix `Database.PrefetchObjects` not to throw for SQL version earlier than 2016
- Add ledger support in Database create options for MI in SSMS
- Add `OwnerLoginName` property to `JobSchedule` per [issue 120](https://github.com/microsoft/sqlmanagementobjects/issues/120)
- Fixed the `Database.AvailabilityDatabaseSynchronizationState` property to reflect the correct synchronization state of MI databases in Managed Instance Link

## 170.18.0

- Add `SearchPropertyList` support for Azure SQL Database

## 170.17.0, 161.48044.0

- Fix issue where `Table.Create` and `View.Create` were querying the server for indexes
- Add option to generate scripts exclusively for Data Classification, Create a new SMO object `SensitivityClassification` under `Database`
- Add support for creating Certificate objects using binary-encoded certificate bytes (https://github.com/microsoft/sqlmanagementobjects/issues/132)
- Fix for incorrect scripting of Database objects targeting SQL Managed Instances

## 170.13.0, 161.48036.0

- Fix [issue](https://github.com/microsoft/sqlmanagementobjects/issues/123) with `Table.Alter` for Synapse
- Add initial replication of contained AG system databases to AG creation
- Upgrade VSTest to 17.4.1 to remove workaround for unit test builds
- Fix Databases collection not to login to each database when app asks for `Status` property
- Enable datetime masked columns
- Update product display names
- Add database, server, and object permissions for SQL Server 2019 and SQL Server 2022
- Add support for strict encryption and HostNameInCertificate 


## 170.12.0, 161.48028.0

- Add certificate and asymmetric key user support for Azure DB
- Change the name of the XML file used by SSMS 19 to RegSrvr16.xml
- Change `SetDefaultInitFields` to [allow inclusion of properties unsupported](https://github.com/microsoft/sqlmanagementobjects/issues/84) by the connected SQL edition. 

## 170.11.0, 161.47027.0

- Fix distribution columns on scripting for taking into consideration more than one distribution column
- Add new EXTGOV_OPERATION_GROUP audit action type
- Force [QUOTED_IDENTIFIER ON](https://github.com/microsoft/sqlmanagementobjects/issues/96) for all tables 
- Change Databases enumeration on Azure DB to ignore `sys.databases` entries that don't have an entry in `sys.database_service_objectives`. Prevents attempted logins to user databases when enumerating databases on the logical master
- Update permissions enumeration for SQL Server 2022

## 170.6.0-preview

- Add SmoMetadataProvider preview package
- Replace netcoreapp3.1 with net6

## 170.5.0-preview

- First public 170 build on Nuget.org
- Upgrade Microsoft.Data.SqlClient to version 5.0
- Upgrade build tools to VS2022

## 161.47021.0

- Add `LedgerViewSchema` property to table objects
- Fix an issue that caused ledger tables with views with different schemas to be scripted improperly
- Added support for `Contained Availability Groups`: new AvailabilityGroup.IsContained and AvailabilityGroup.ReuseSystemDatabases properties and updated Create() method.
- Fixed generate scripts test for SQL 2012
- Added automated tests for `JobServer` methods
- Marked several `JobServer` methods supporting SQL 2005 and earlier as Obsolete
- Marked unused property `JobServerFilter.OldestFirst` as Obsolete
- Add `IsDroppedLedgerTable` and `IsDroppedLedgerView` properties to table and view objects, respectively
- Add `IsDroppedLedgerColumn` properties to column, and updated scripting to not include dropped ledger columns in script creation
- Fixed heuristic in [Wmi.ManagedComputer](https://github.com/microsoft/sqlmanagementobjects/issues/83) to determine the correct WMI namespace to connect to,
  to workaround a bug where SQL Setup setup does not fully uninstall the SQL WMI Provider.
- Update `ConnectionManager.InternalConnect` to retry connection in response to error 42109 (serverless instance is waking up)

## 161.47008.0

- Fix an issue that caused `ServerConnection.SqlExecutionModes` property to be set to `ExecuteSql` during lazy property fetches of SMO objects despite being set to `CaptureSql` by the calling application.
- Add `LoginType` property to `ILoginOptions` interface.
- `Login.PasswordPolicyEnforced` now returns `false` for Windows logins instead of throwing an exception
- Remove net461 binaries from nuget packages
- Added Scripting Support for Ledger tables for SQL 2022+
- Change the `Size` property on `Server/Drive` objects to `System.Int64`. These objects don't have a C# wrapper class so it's not breaking any compilation.
- Add support for SQL Server version 16
- Add new permissions for SQL 2019+ to SMO enumerations
- Added External Stream object and External Streaming Jobs object for scripting
- Add support for XML compression

## 161.46521.71

- Handle Dedicated SQL Pool login error 110003 during enumerate of Databases
- Enable asymmetric and symmetric key objects for dedicated SQL Pool database
- Fix Tables enumeration on Azure SQL Database instances using a case sensitive catalog collation
- Fix scripting of [hidden columns](https://github.com/microsoft/sqlmanagementobjects/issues/65)
- Enable Generate Scripts to script temporal tables when the destination is a pre-2016 version of SQL Server. System versioning DDL will be omitted from the generated script.

## 161.46437.65

- Update Microsoft.Data.SqlClient dependency to version 3.0.0
- Added Scripting Support for Ledger table in Azure SQLDB
- Change `Server.MasterDBPath` and `Server.MasterDBLogPath` properties to use `file_id` instead of `name` from `sys.database_files`
- Enable Index creation for memory optimized tables in Azure
- Fix Server/Logins to show external Logins for Azure SQLDB as they are now supported
- Split SmoMetadataProvider into its own nuget packages
- Adding support for External Languages

## 161.46347.54

- Add Microsoft.SqlServer.SqlWmiManagement and Microsoft.SqlServer.Management.Smo.Wmi to lib\netcoreapp3.1
- Add missing resource files for netcoreapp3.1 and netstandard2.0
- Fix an [issue](https://github.com/microsoft/sqlmanagementobjects/issues/50) with scripting Azure Synapse Analytics databases
- Add missing values to AuditActionType enum
- Fixed an issue where AffinityInfo.Alter() may throw an error like `An item with the same key has already been added` when
  trying to update the AffinityMask of a specific CPU, particularly on machines with Soft-NUMA.
- Updated formatting logic of Predicate values in XEvent scripts
- Fix for scripting distributed Availability Groups
- Add support for resumable option on create constraints and low priority wait

## 161.46041.41

- Add descriptions to more Facet properties
- Add net461 binaries due to customer demand. Only core scripting functionality is included in lib\net461
- Make RegisteredServersStore.InitializeLocalRegisteredServersStore public to enable loading and saving registered servers in a custom location
- Fixed an [issue](https://github.com/microsoft/sqlmanagementobjects/issues/34)
  where the creation of a DataFile may fail when targeting a SQL Azure Managed Instance
- Fix Database.Checkpoint to always checkpoint the correct database. [Issue 32](https://github.com/microsoft/sqlmanagementobjects/issues/32)

## 161.44091.28

- Make ISmoScriptWriter interface public
- Enable apps to provide custom ISmoScriptWriter implementation to SqlScriptPublishModel and ScriptMaker
- Enabled Security Policy while GenerateScript/Transfer database.
- Expose EXTERNAL_MONITOR server audit destination for SQL Managed Instance
- Expose OPERATOR_AUDIT server audit option for SQL Managed Instance
- Change association of DatabaseEngineEdition.SqlOnDemand to DatabaseEngineType.SqlAzureDatabase
- Fix implementation of Microsoft.SqlServer.Management.HadrModel.FailoverTask.Perform to handle AvailabilityGroupClusterType.None correctly

## 161.42121.15

- Add netcoreapp3.1 build output
- Fix [logins using impersonation](https://github.com/microsoft/sqlmanagementobjects/issues/24)
- Expose OlapConnectionInfo class in non-netfx ConnectionInfo
- Expose WmiMgmtScopeConnection in non-netfx ConnectionInfo

## 161.41981.14

- Add Accelerated Database Recovery support - <https://github.com/microsoft/sqlmanagementobjects/issues/22>
- Enable Column.BindDefault on Azure SQL Database
- Add DestinationServerConnection property to Transfer
  - [Github issue 16](https://github.com/microsoft/sqlmanagementobjects/issues/16)
  - Allows for use of Azure SQL Database as a destination server
  - Enables full customization of the destination connection
- [Script User objects for Azure SQL Database correctly](https://github.com/microsoft/sqlmanagementobjects/issues/18)
- [Enable CreateOrAlter behavior for Scripter](https://github.com/microsoft/sqlmanagementobjects/issues/11)
- Fixed issue where MaxSize value was reported as negative for Hyperscale Azure SQL Databases - Added new property "IsMaxSizeApplicable" and disabled negative values for Hyperscale Azure SQL Databases.

## 161.41011.9

- Put begin try/begin catch around TSQL querying sys.database_service_objectives in Azure SQL Database. This view may throw if Azure control plane has an outage and block expansion of the Databases node in SSMS.
- Add support for Workload Management Workload Classifiers.
- Add support for Workload Management Workload Groups.
- Handle SQL error code 4060 during fetch of Database.DatabaseEngineEdition and use default value of Unknown
- Update Microsoft.Data.SqlClient dependency to version 2.0.0
- Update the Nuget package major version to 161 to reflect the shift to Microsoft.Data.SqlClient for NetFx
- Fixed Database.Size property to report the accurate size of the database when
  DatabaseEngineType is SqlAzureDatabase
- Fixed issue where Database.SpaceAvailable was reported as negative for Hyperscale Azure SQL Databases
  (the value is reported as 0, meaning *Not applicable*)
- Implement IObjectPermission on DatabaseScopedCredential. <https://github.com/microsoft/sqlmanagementobjects/issues/14>
- Enabled Server.EnumServerAttributes API on Azure SQL Database
- Enabled Lock enumeration APIs on Azure SQL Database
- Deleted the Database.CheckIdentityValues API
- Added new property "RequestMaximumMemoryGrantPercentageAsDouble" in WorkloadGroup to accept decimal values in Resource Governor (SQL 2019+).
- Changed the netfx binaries in Microsoft.SqlServer.SqlManagementObjects package to use Microsoft.Data.SqlClient
- Added a new package, Microsoft.SqlServer.SqlManagementObjects.SSMS, which only has netfx binaries and that uses System.Data.SqlClient
- Fixed a scripting issue with statistics on filtered indexes where the filter from the index would be scripted with the UPDATE STATISTICS TSQL.

## 160.2004021.0

- First non-preview 160 release, aligned with [SQL Server Management Studio](https://aka.ms/ssmsfullsetup) 18.5
- Script extended properties for Azure SQL Database objects
- Enable Jupyter Notebook output for SqlScriptPublishModel. SSMS 18.5 can output a Notebook for Azure Data Studio in Generate Scripts now.
- Fix issue where Table.EnableAllIndexes(Recreate) did nothing
- Fix Database.EnumObjectPermissions usage in NetStandard binaries

- Enabled Security Policy and Security Predicate objects on Azure SQL DataWarehouse

- Enabled Text property for StoredProcedure on Azure SQL Database
- Enabled Database.GetTransactionCount and Database.EnumTransactions on Azure SQL Database
- Added CMK and CEK scripts to "Generating scripts for all database objects" option in SSMS.
- Changed the order of the scripts in SmoUrnFilter.cs to script out the CMK and CEK Scripts before Tables.
- Transferdata unit test cases were failing due to the "USE" statement in the Create Query for CMK and CEK Scripts. Removed "USE" statement
- Updated Transfer/ScriptingBaselines Xml's with the CMK and CEK Scripts for all the versions which supports CMK's and CEK's (2016 and later)
- Updated the ColumnMasterkey.baseline.xml's and ColumnEncryptionKey.baseline.xml's for the versions which supports these keys (Removed USE statement in the create query).
- Enabled support for Column.IsMasked and Column.MaskingFunction for DataWarehouse
- Remove FORCE ORDER hint from table enumeration that was causing major performance issues
- Fix Transfer with PrefetchAllObjects == false for pre-SQL 2014 versions so it doesn't throw an exception
- Added BLOB_STORAGE scripting support for external data sources
- Fixed [error scripting external tables](<https://feedback.azure.com/forums/908035-sql-server/suggestions/38267746-cannot-script-external-table-in-ssms-18-2>) for Azure SQL Database
- Replace Microsoft.SqlServer.Management.SqlParser.dll with a dependency to its Nuget package
- Fixed SMO Column's sensitivity attribute drop failed when attribute is empty
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
