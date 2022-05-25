
# Introduction

This file will log substantial changes made to SMO between public releases to nuget.org.

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
- Added new property "RequestMaximumMemoryGrantPercentageAsDouble" in WorkloadGroup to accept decimal values in Resource Governor (SQL 2019+).
- Changed the netfx binaries in Microsoft.SqlServer.SqlManagementObjects package to use Microsoft.Data.SqlClient
- Added a new package, Microsoft.SqlServer.SqlManagementObjects.SSMS, which only has netfx binaries and that uses System.Data.SqlClient

