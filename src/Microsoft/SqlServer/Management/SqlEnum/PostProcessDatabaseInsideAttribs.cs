// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Globalization;
#if STRACE
    using Microsoft.SqlServer.Management.Diagnostics;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    internal class PostProcessDatabaseInsideAttribs : PostProcessWithRowCaching
    {
        string databaseName = string.Empty;
        String sqlQuery = null;
        string defaultdataPath = string.Empty;

        protected override bool SupportDataReader
        {
            get { return false;}
        }

        /// <summary>
        /// Number of Kbytes per page
        /// </summary>
        // DEVNOTE(MatteoT)
        //   Up until 6/19/2020 this code was fetching the value from SQL itself by running
        //      select convert(float,low/1024.) from master.dbo.spt_values where number = 1 and type = 'E'
        //   This would be "ok" for SQL on-prem, but such SP is not available in Azure
        //   There is also evidence that this is a value that is unlikely to change (it has not changed ever)
        //   so, saving a trip to the server is always beneficial + we would be able to use this against Azure.
        double BytesPerPage => 8;

        string DefaultDataPath
        {
            get
            {
                if (string.IsNullOrEmpty(defaultdataPath))
                {
                    DataTable dt = ExecuteSql.ExecuteWithResults("select SERVERPROPERTY('instancedefaultdatapath')",
                                                                 this.ConnectionInfo);
                    defaultdataPath = (string)dt.Rows[0][0];
                    defaultdataPath =defaultdataPath.Remove(defaultdataPath.Length - 1, 1);
                }
                return defaultdataPath;
            }
        }


        protected override string SqlQuery
        {
            get
            {
                if ( null == sqlQuery )
                {
                    sqlQuery = ExecuteSql.GetServerVersion(this.ConnectionInfo).Major < 9 ?
                        BuildSqlStatementLess90() :
                        BuildSqlStatementMoreEqual90();
                    sqlQuery = String.Format(CultureInfo.InvariantCulture, sqlQuery, Util.EscapeString(this.databaseName, '\''));
                }
                return sqlQuery;
            }
        }

        /// <summary>
        /// Clean row collection per individual database property query.
        /// In batch query for HasMemoryOptimizedObjects, the initial query results are reused for the next database properties.
        /// </summary>
        public override void CleanRowData()
        {
            // Gather all the database list once for the first time for optimization in expanding database list
            // to check HasMemoryOptimizedObjects through master.sys.master_files instead of sys.filegroups.
            // In Azure, master.sys.master_files is not exposed to client applications.
            //
            bool hkDatabaseList = GetIsFieldHit("HasMemoryOptimizedObjects") && HitFieldsCount() == 1 &&
                                ((DatabaseEngineType)ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo)) != DatabaseEngineType.SqlAzureDatabase;
            if (hkDatabaseList)
            {
                return;
            }

            base.CleanRowData();
        }

        private void BuildCommonSql(StatementBuilder sb)
        {
            if (GetIsFieldHit("IsDbAccessAdmin"))
            {
                sb.AddProperty("IsDbAccessAdmin", "is_member(N'db_accessadmin')");
            }
            if (GetIsFieldHit("IsDbBackupOperator"))
            {
                sb.AddProperty("IsDbBackupOperator", "is_member(N'db_backupoperator')");
            }
            if (GetIsFieldHit("IsDbDatareader"))
            {
                sb.AddProperty("IsDbDatareader", "is_member(N'db_datareader')");
            }
            if (GetIsFieldHit("IsDbDatawriter"))
            {
                sb.AddProperty("IsDbDatawriter", "is_member(N'db_datawriter')");
            }
            if (GetIsFieldHit("IsDbOwner"))
            {
                sb.AddProperty("IsDbOwner", "is_member(N'db_owner')");
            }
            if (GetIsFieldHit("IsDbSecurityAdmin"))
            {
                sb.AddProperty("IsDbSecurityAdmin", "is_member(N'db_securityadmin')");
            }
            if (GetIsFieldHit("IsDbDdlAdmin"))
            {
                sb.AddProperty("IsDbDdlAdmin", "is_member(N'db_ddladmin')");
            }
            if (GetIsFieldHit("IsDbDenyDatareader"))
            {
                sb.AddProperty("IsDbDenyDatareader", "is_member(N'db_denydatareader')");
            }
            if (GetIsFieldHit("IsDbDenyDatawriter"))
            {
                sb.AddProperty("IsDbDenyDatawriter", "is_member(N'db_denydatawriter')");
            }
            if ( GetIsFieldHit("DboLogin") )
            {
                sb.AddProperty("DboLogin", "is_member(N'db_owner')");
            }
            if ( GetIsFieldHit("UserName") )
            {
                sb.AddProperty("UserName", "user_name()");
            }
        }

        private string BuildSqlStatementMoreEqual90()
        {
            var sb = new StatementBuilder();

            BuildCommonSql(sb);
            
            var isFabricDW = ExecuteSql.IsFabricConnection(ConnectionInfo) && ExecuteSql.GetDatabaseEngineEdition(ConnectionInfo) == DatabaseEngineEdition.SqlOnDemand;

            // In Fabric DW, sys.database_files is not accessible.
            if ( GetIsFieldHit("SpaceAvailable") || GetIsFieldHit("Size") )
            {
                sb.AddProperty("DbSize", isFabricDW ? "CAST(0 as float)" : "(SELECT SUM(CAST(df.size as float)) FROM sys.database_files AS df WHERE df.type in ( 0, 2, 4 ) )");
            }
            if ( GetIsFieldHit("SpaceAvailable") )
            {
                sb.AddProperty("SpaceUsed", isFabricDW ? "0" : "(SUM(a.total_pages) + (SELECT ISNULL(SUM(CAST(df.size as bigint)), 0) FROM sys.database_files AS df WHERE df.type = 2 ))");
            }
            if ( GetIsFieldHit("Size") )
            {
                sb.AddProperty("LogSize", isFabricDW ? "CAST(0 as float)" : "(SELECT SUM(CAST(df.size as float)) FROM sys.database_files AS df WHERE df.type in (1, 3))");
                // Determine whether this is a Hyperscale edition and save it for later use.
                // DEVNOTE(MatteoT): we could have compute the property without checking the database engine type,
                //                   this making the code a little cleaner. However, that would have been at  the
                //                   expense of unnecessary T-SQL.
                if (ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo) == DatabaseEngineType.SqlAzureDatabase)
                {
                    sb.AddProperty("IsHyperscaleEdition", "(SELECT IIF(databasepropertyex(db_name(),'Edition') = 'Hyperscale', 1, 0))");
                }
            }
            if (GetIsFieldHit("HasMemoryOptimizedObjects"))
            {
                DatabaseEngineType dbEngineType =
                    (DatabaseEngineType)ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo);
                //If we're fetching other properties or the server is an azure server then don't pre-fetch
                //data for all the DBs.
                //If the post process has other properties it's unlikely we're populating data for more than one DB at a time
                //For Azure we can't query the master DB from a user db (plus the master_files DMV doesn't exist)
                if (HitFieldsCount() > 1 || dbEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    if (dbEngineType == DatabaseEngineType.SqlAzureDatabase)
                    {
                        //Azure doesn't have the FX filegroup so we use the databasepropertyex instead as a temporary solution,
                        //note that this isn't the correct way to do this as it doesn't actually tell us whether there's hekaton
                        //objects in the db. TFS#6298872 has been created to follow up on this
                        sb.AddProperty("HasMemoryOptimizedObjects", "(SELECT databasepropertyex(db_name(),'IsXTPSupported'))");
                    }
                    else
                    {
                        sb.AddProperty("HasMemoryOptimizedObjects", "ISNULL((select top 1 1 from sys.filegroups FG where FG.[type] = 'FX'), 0)");
                    }
                }
                else
                {
                    //This fetches a list of ALL the dbs with a master_file of type = 2 (FILESTREAM) and then filters
                    //it out later. This is done as a performance increase when fetching this property alone for specific cases
                    //(such as the DB dropdown in SSMS)
                    sb.AddProperty(null, "db.name as HasMemoryOptimizedObjects from master.sys.master_files mf join master.sys.databases db on mf.database_id = db.database_id where mf.[type] = 2");
                }
            }
            if ( GetIsFieldHit("MemoryAllocatedToMemoryOptimizedObjectsInKB"))
            {
                sb.AddProperty("MemoryAllocatedToMemoryOptimizedObjectsInKB", isFabricDW ? "0.00" :  @"isnull((select convert(decimal(18,2),(sum(tms.memory_allocated_for_table_kb) + sum(tms.memory_allocated_for_indexes_kb))) 
                                                                                from [sys].[dm_db_xtp_table_memory_stats] tms), 0.00)");
            }
            if ( GetIsFieldHit("MemoryUsedByMemoryOptimizedObjectsInKB"))
            {
                sb.AddProperty("MemoryUsedByMemoryOptimizedObjectsInKB",  isFabricDW ? "0.00" : @"isnull((select convert(decimal(18,2),(sum(tms.memory_used_by_table_kb) + sum(tms.memory_used_by_indexes_kb))) 
                                                                           from [sys].[dm_db_xtp_table_memory_stats] tms), 0.00)");
            }
            if (GetIsFieldHit("HasFileInCloud"))
            {
                sb.AddProperty("HasFileInCloud", isFabricDW ? "0" : @"ISNULL ((select top 1 1 from sys.database_files
                                                    where state = 0 and physical_name like 'https%' collate SQL_Latin1_General_CP1_CI_AS), 0)");
            }
            if (GetIsFieldHit("DataSpaceUsage") || GetIsFieldHit("IndexSpaceUsage"))
            {
                sb.AddProperty("DataSpaceUsage", "SUM(CASE When it.internal_type IN (202,204,207,211,212,213,214,215,216) Then 0 When a.type <> 1 Then a.used_pages	When p.index_id < 2 Then a.data_pages	Else 0	END)");
            }
            if ( GetIsFieldHit("IndexSpaceUsage") )
            {
                sb.AddProperty("IndexSpaceTotal", "SUM(a.used_pages)");
            }
            if (GetIsFieldHit("IsMailHost"))
            {
                sb.AddProperty("IsMailHost", "(select count(1) from sys.services where name ='InternalMailService')");
            }
            if ( GetIsFieldHit("DefaultSchema") )
            {
                sb.AddProperty("DefaultSchema", "(select schema_name())");
            }
            if (GetIsFieldHit("DataSpaceUsage") || GetIsFieldHit("IndexSpaceUsage") || GetIsFieldHit("SpaceAvailable"))
            {
                sb.AddFrom("sys.partitions p join sys.allocation_units a on p.partition_id = a.container_id left join sys.internal_tables it on p.object_id = it.object_id");
            }
            if (GetIsFieldHit("DefaultFileGroup"))
            {
                sb.AddProperty("DefaultFileGroup", "(select top 1 ds.name from sys.data_spaces as ds where ds.is_default = 1 and ds.type = 'FG' )");
            }
            if (GetIsFieldHit("IsManagementDataWarehouse"))
            {
                sb.AddProperty("IsManagementDataWarehouse", "(select count(1) from sys.extended_properties where name = 'Microsoft_DataCollector_MDW_Version')");
            }
            if (GetIsFieldHit("DefaultFileStreamFileGroup"))
            {
                sb.AddProperty("DefaultFileStreamFileGroup", "(select case when t1.c1 > 0 then t1.c2 else N'' end from (select top 1 count(*) c1, min(ds.name) c2 from sys.data_spaces as ds where ds.is_default = 1 and ds.type = 'FD') t1)");
            }
            if (GetIsFieldHit("PrimaryFilePath"))
            {
                //sys.master_files comes empty for a contained user.
                if (ExecuteSql.IsContainedAuthentication(this.ConnectionInfo))
                {
                    sb.AddProperty("PrimaryFilePath", "(select ISNULL(df.physical_name, N'') from sys.database_files as df where df.data_space_id = 1 and df.file_id = 1)");
                }
            }
            if (GetIsFieldHit("MaxDop"))
            {
                sb.AddProperty("MaxDop", "(select value from sys.database_scoped_configurations as dsc where dsc.name = 'MAXDOP')");
            }
            if (GetIsFieldHit("MaxDopForSecondary"))
            {
                sb.AddProperty("MaxDopForSecondary", "(select value_for_secondary from sys.database_scoped_configurations as dsc where dsc.name = 'MAXDOP')");
            }
            if (GetIsFieldHit("LegacyCardinalityEstimation"))
            {
                sb.AddProperty("LegacyCardinalityEstimation", "(select value from sys.database_scoped_configurations as dsc where dsc.name = 'LEGACY_CARDINALITY_ESTIMATION')");
            }
            if (GetIsFieldHit("LegacyCardinalityEstimationForSecondary"))
            {
                sb.AddProperty("LegacyCardinalityEstimationForSecondary", "(select ISNULL(value_for_secondary, 2) from sys.database_scoped_configurations as dsc where dsc.name = 'LEGACY_CARDINALITY_ESTIMATION')");
            }
            if (GetIsFieldHit("ParameterSniffing"))
            {
                sb.AddProperty("ParameterSniffing", "(select value from sys.database_scoped_configurations as dsc where dsc.name = 'PARAMETER_SNIFFING')");
            }
            if (GetIsFieldHit("ParameterSniffingForSecondary"))
            {
                sb.AddProperty("ParameterSniffingForSecondary", "(select ISNULL(value_for_secondary, 2) from sys.database_scoped_configurations as dsc where dsc.name = 'PARAMETER_SNIFFING')");
            }
            if (GetIsFieldHit("QueryOptimizerHotfixes"))
            {
                sb.AddProperty("QueryOptimizerHotfixes", "(select value from sys.database_scoped_configurations as dsc where dsc.name = 'QUERY_OPTIMIZER_HOTFIXES')");
            }
            if (GetIsFieldHit("QueryOptimizerHotfixesForSecondary"))
            {
                sb.AddProperty("QueryOptimizerHotfixesForSecondary", "(select ISNULL(value_for_secondary, 2) from sys.database_scoped_configurations as dsc where dsc.name = 'QUERY_OPTIMIZER_HOTFIXES')");
            }
            if (GetIsFieldHit("IsLedger"))
            {
                sb.AddProperty("IsLedger", "(select is_ledger_on from sys.databases WHERE name=N'{0}')");
            }

            AddDbChaining(sb);

            return sb.SqlStatement;
        }

        private string BuildSqlStatementLess90()
        {
            var sb = new StatementBuilder();

            BuildCommonSql(sb);

            if ( GetIsFieldHit("SpaceAvailable") || GetIsFieldHit("Size") )
            {
                sb.AddProperty("DbSize", "(select sum(convert(float,size)) from dbo.sysfiles where (status & 64 = 0))");
            }
            if ( GetIsFieldHit("SpaceAvailable") )
            {
                sb.AddProperty("SpaceUsed", "(select sum(convert(float,reserved)) from dbo.sysindexes where indid in (0, 1, 255))");
            }
            if ( GetIsFieldHit("Size") )
            {
                sb.AddProperty("LogSize", "(select sum(convert(float,size)) from dbo.sysfiles where (status & 64 <> 0))");
            }
            if ( GetIsFieldHit("DataSpaceUsage") || GetIsFieldHit("IndexSpaceUsage") )
            {
                sb.AddProperty("DataSpaceUsage", "((select sum(convert(float,dpages)) from dbo.sysindexes where indid < 2) + (select isnull(sum(convert(float,used)), 0) from dbo.sysindexes where indid = 255))");
            }
            if ( GetIsFieldHit("IndexSpaceUsage") )
            {
                sb.AddProperty("IndexSpaceTotal", "(select sum(convert(float,used)) from dbo.sysindexes where indid in (0, 1, 255))");
            }
            if ( GetIsFieldHit("DefaultSchema") )
            {
                sb.AddProperty("DefaultSchema", "user_name()");
            }
            if (GetIsFieldHit("DefaultFileGroup"))
            {
                sb.AddProperty("DefaultFileGroup", "(select top 1 fg.groupname from dbo.sysfilegroups as fg where fg.status & 0x10 <> 0)");
            }
            ServerVersion sv = ExecuteSql.GetServerVersion(this.ConnectionInfo);
            if ( sv.Major >= 8 && sv.BuildNumber >= 760 ) //db chaining implmented starting with build 760
            {
                AddDbChaining(sb);
            }
            
            return sb.SqlStatement;
        }

        private void AddDbChaining(StatementBuilder sb)
        {
            if ( GetIsFieldHit("DatabaseOwnershipChaining"))
            {
                sb.AddPrefix("create table #tmpdbchaining( name sysname , dbc sysname )");
                ServerVersion sv = ExecuteSql.GetServerVersion(this.ConnectionInfo);
                if (sv.Major < 9)
                {
                    sb.AddPrefix("insert into #tmpdbchaining exec dbo.sp_dboption N'{0}', 'db chaining'\n");
                }
                else
                {
                    sb.AddPrefix("insert into #tmpdbchaining SELECT 'db chaining' AS 'OptionName', CASE WHEN (SELECT is_db_chaining_on FROM sys.databases WHERE name=N'{0}') = 1 THEN 'ON' ELSE 'OFF' END AS 'CurrentSetting'\n");
                }
                sb.AddPrefix("declare @DBChaining bit\nset @DBChaining = null\nselect @DBChaining = case LOWER(dbc) when 'off' then 0 else 1 end from #tmpdbchaining");
                sb.AddProperty("DatabaseOwnershipChaining", "@DBChaining");
                sb.AddPostfix("drop table #tmpdbchaining");
            }
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            ServerVersion sv = ExecuteSql.GetServerVersion(this.ConnectionInfo);
            this.databaseName = GetTriggeredString(dp, 0);
            this.GetCachedRowResultsForDatabase(dp, this.databaseName);
            if (this.rowResults == null
                && string.Compare(name, "PrimaryFilePath", StringComparison.OrdinalIgnoreCase ) != 0)  
            {
                data = DBNull.Value;
            }
            else
            {
                switch (name)
                {

                    case "SpaceAvailable":
                        double logSpaceAvailable = 0;
                        if (!IsNull(data))
                        {
                            logSpaceAvailable = (double)data;
                        }
                        if (sv.Major < 9)
                        {
                            data = ((double)this.rowResults[0]["DbSize"] - (double)this.rowResults[0]["SpaceUsed"]) * this.BytesPerPage + logSpaceAvailable;
                        }
                        else
                        {
                            data = ((double)this.rowResults[0]["DbSize"] - (Int64)this.rowResults[0]["SpaceUsed"]) * this.BytesPerPage + logSpaceAvailable;
                        }
                        if ((double)data < 0)
                        {
                            data = (double)0;
                        }
                        break;
                    case "Size":
                        double logSize = 0;
                        //will be DBNull for view database
                        if (rowResults[0]["LogSize"] is double @double)
                        {
                            logSize = @double;
                        }

                        // When dealing with a Hyperscale edition (SQL Azure DB), ignore the size of the logs
                        // as it is always returned as 1TB, and it would make the Size property meaningless.
                        var isHyperScale =
                            rowResults[0].Table.Columns.Contains("IsHyperscaleEdition") &&
                            Convert.ToBoolean(rowResults[0]["IsHyperscaleEdition"]); 

                        data = ((double)this.rowResults[0]["DbSize"] + (isHyperScale ? 0 : logSize)) * BytesPerPage / 1024; // reporting units are MB
                        break;
                    //when modify check table.xml DataSpaceUsed for consistency
                    case "DataSpaceUsage":
                        if (sv.Major < 9)
                        {
                            data = ((double)this.rowResults[0]["DataSpaceUsage"]) * this.BytesPerPage;
                        }
                        else
                        {
                            data = ((Int64)this.rowResults[0]["DataSpaceUsage"]) * this.BytesPerPage;
                        }
                        break;

                    // Property indicates whether database has Hekaton objects or not by checking for existence of memory optimized filegroup
                    case "HasMemoryOptimizedObjects":
                        if (HitFieldsCount() > 1 || ((DatabaseEngineType)ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo)) == DatabaseEngineType.SqlAzureDatabase)
                        {
                            data = Convert.ToBoolean((this.rowResults[0]["HasMemoryOptimizedObjects"]));
                        }
                        else
                        {
                            bool foundMatched = false;
                            for (int i = 0; i < this.rowResults.Count; i++)
                            {
                                if (this.rowResults[i]["HasMemoryOptimizedObjects"].ToString() == this.databaseName)
                                {
                                    foundMatched = true;
                                    break;
                                }
                            }
                            data = Convert.ToBoolean(foundMatched);
                        }
                        break;
                    // Property indicated amount of memory allocated to memory optimized objects and 0 if Hekaton is not in use
                    case "MemoryAllocatedToMemoryOptimizedObjectsInKB":
                        data = ((Decimal)this.rowResults[0]["MemoryAllocatedToMemoryOptimizedObjectsInKB"]);
                        break;
                    // Property indicated amount of memory used by memory optimized objects and 0 if Hekaton is not in use
                    case "MemoryUsedByMemoryOptimizedObjectsInKB":
                        data = ((Decimal)this.rowResults[0]["MemoryUsedByMemoryOptimizedObjectsInKB"]);
                        break;
                    // Property indicates whether database has any file in cloud path (in XStore)
                    case "HasFileInCloud":
                        data = Convert.ToBoolean((this.rowResults[0]["HasFileInCloud"]));
                        break;
                    // Property indicates whether database has any Isledger ON in cloud path
                    case "IsLedger":
                        data = Convert.ToBoolean((this.rowResults[0]["IsLedger"]));
                        break;
                    //when modify check table.xml IndexSpaceUsed and index.xml IndexSpaceUsed for consistency
                    case "IndexSpaceUsage":
                        if (sv.Major < 9)
                        {
                            data = ((double)this.rowResults[0]["IndexSpaceTotal"] - (double)this.rowResults[0]["DataSpaceUsage"]) * this.BytesPerPage;
                        }
                        else
                        {
                            data = ((Int64)this.rowResults[0]["IndexSpaceTotal"] - (Int64)this.rowResults[0]["DataSpaceUsage"]) * this.BytesPerPage;
                        }
                        break;
                    case "UserName":
                    case "IsMailHost":
                    case "DboLogin":
                    case "DefaultSchema":
                    case "IsDbOwner":
                    case "IsDbDdlAdmin":
                    case "IsDbDatareader":
                    case "IsDbDatawriter":
                    case "IsDbAccessAdmin":
                    case "IsDbSecurityAdmin":
                    case "IsDbBackupOperator":
                    case "IsDbDenyDatareader":
                    case "IsDbDenyDatawriter":
                    case "DefaultFileGroup":
                    case "MaxDop":
                    case "MaxDopForSecondary":
                    case "LegacyCardinalityEstimation":
                    case "LegacyCardinalityEstimationForSecondary":
                    case "ParameterSniffing":
                    case "ParameterSniffingForSecondary":
                    case "QueryOptimizerHotfixes":
                    case "QueryOptimizerHotfixesForSecondary":
                        data = this.rowResults[0][name];
                        break;
                    case "DefaultFileStreamFileGroup":
                        if (sv.Major >= 10)
                        {
                            data = this.rowResults[0]["DefaultFileStreamFileGroup"];
                        }
                        break;
                    case "DatabaseOwnershipChaining":
                        if (sv.Major >= 8 && sv.BuildNumber >= 760) //db chaining implmented starting with build 760
                        {
                            data = this.rowResults[0]["DatabaseOwnershipChaining"];
                        }
                        break;
                    case "IsManagementDataWarehouse":
                        // Data Collector is a Katmai feature
                        if (sv.Major >= 10)
                        {
                            data = this.rowResults[0]["IsManagementDataWarehouse"];
                        }
                        break;
                    case "PrimaryFilePath":
                        if (sv.Major < 9)
                        {
                            if (IsNull(data))
                            {
                                return data;
                            }
                            else
                            {
                                //cut the file name
                                data = GetPath((string)data);
                            }
                        }
                        else
                        {
                            if (ExecuteSql.IsContainedAuthentication(this.ConnectionInfo))
                            {
                                data = this.rowResults[0]["PrimaryFilePath"];
                            }

                            if (IsNull(data))
                            {
                                return String.Empty;
                            }
                            else
                            {
                                data = GetPath((string)data);
                            }
                        }
                        break;
                }
            }
            return data;
        }

        String GetPath(String sFullName)
        {
            if ( null == sFullName || 0 == sFullName.Length )
            {

                return String.Empty;
            }
            String s = String.Empty;
            try
            {
                s = PathWrapper.GetDirectoryName(sFullName);
            }
            catch (ArgumentException exp)
            {
                TraceHelper.LogExCatch(exp);
            }
            return s;
        }
    }

    internal class PostProcessAutoCloseProperties : PostProcessWithRowCaching
    {
        protected override bool SupportDataReader
        {
            get { return false;}
        }

        protected override string SqlQuery
        {
            get
            {
                String queryInDb = null;

                ServerVersion sv = ExecuteSql.GetServerVersion(this.ConnectionInfo);

                if (sv.Major >= 12)
                {
                    queryInDb = "SELECT dtb.collation_name AS [Collation], CAST(DATABASEPROPERTYEX(dtb.name, 'Version') AS int) AS [Version], dtb.compatibility_level AS [CompatibilityLevel], CAST(CHARINDEX(N'_CS_', dtb.collation_name) AS bit) AS [CaseSensitive], dtb.target_recovery_time_in_seconds AS [TargetRecoveryTime], dtb.delayed_durability AS [DelayedDurability] FROM master.sys.databases AS dtb where name = db_name()";
                }
                else if (sv.Major >= 11)
                {
                    queryInDb = "SELECT dtb.collation_name AS [Collation], CAST(DATABASEPROPERTYEX(dtb.name, 'Version') AS int) AS [Version], dtb.compatibility_level AS [CompatibilityLevel], CAST(CHARINDEX(N'_CS_', dtb.collation_name) AS bit) AS [CaseSensitive], dtb.target_recovery_time_in_seconds AS [TargetRecoveryTime] FROM master.sys.databases AS dtb where name = db_name()";
                }
                else if (sv.Major >= 9)
                {
                    queryInDb = "SELECT dtb.collation_name AS [Collation], CAST(DATABASEPROPERTYEX(dtb.name, 'Version') AS int) AS [Version], dtb.compatibility_level AS [CompatibilityLevel], CAST(CHARINDEX(N'_CS_', dtb.collation_name) AS bit) AS [CaseSensitive] FROM master.sys.databases AS dtb where name = db_name()";
                }
                else
                {
                    queryInDb = "SELECT CAST(DATABASEPROPERTYEX(dtb.name, 'Collation') AS sysname) AS [Collation], CAST(DATABASEPROPERTYEX(dtb.name, 'Version') AS int) AS [Version], dtb.cmptlevel AS [CompatibilityLevel], CAST(CHARINDEX(N'_CS_', CAST(DATABASEPROPERTYEX(dtb.name, 'Collation') AS nvarchar(255))) AS bit) AS [CaseSensitive] FROM master.dbo.sysdatabases AS dtb where name = db_name()";
                }

                return queryInDb;
            }
        }

        //if the column is null, get the properties 
        //that we know can be null if AUTO_CLOSE is ON
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if ( IsNull(data) )
            {
                this.GetCachedRowResultsForDatabase(dp, databaseName : GetTriggeredString(dp, 0));
                data = this.rowResults == null ? data : this.rowResults[0][name];
            }
            return data;
        }
    }

    internal class PostProcessContainedDbProperties : PostProcess
    {
        DataTable dt;

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            ContainmentType ct = (ContainmentType)GetTriggeredInt32(dp, 0);
            if (ct != ContainmentType.None)
            {
                return data;
            }

            if (dt == null)
            {
                //configuration_id 115:  nested triggers
                //configuration_id 124:  default language
                //configuration_id 1126: default full-text language
                //configuration_id 1127: two digit year cutoff
                //configuration_id 1555: transform noise words
                string query = @"select 
case 
    when cfg.configuration_id = 124 -- configuration id for default language
    then (select lcid from sys.syslanguages as sl where sl.langid = cfg.value_in_use) -- getting default language LCID from default language langid
    else cfg.value_in_use
end as value,
case 
    when cfg.configuration_id = 124 -- configuration id for default language
    then (select name collate catalog_default from sys.syslanguages as sl where sl.langid = cfg.value_in_use) -- getting default language name from default language langid
    when cfg.configuration_id = 1126 -- configuration id for default fulltext language
    then ISNULL((select name collate catalog_default from sys.fulltext_languages as fl where fl.lcid = cfg.value_in_use), N'') -- getting default fulltext language name from default fulltext language lcid
    else null
end as name,
cfg.configuration_id as configuration_id
from sys.configurations as cfg
where cfg.configuration_id in (115, 124, 1126, 1127, 1555) 
order by cfg.configuration_id asc";                
                
                //Don't need to set the context to master as sys.configurations work
                //in all legacy databases(ContainmentType = None).
                dt = ExecuteSql.ExecuteWithResults(query, this.ConnectionInfo);
            }

            switch (name)
            {
                case "NestedTriggersEnabled":                
                    data = dt.Rows[0][0];
                    break;
                case "DefaultLanguageLcid":                
                    data = dt.Rows[1][0];
                    break;
                case "DefaultLanguageName":
                    data = dt.Rows[1][1];
                    break;
                case "DefaultFullTextLanguageLcid":
                    data = dt.Rows[2][0];
                    break;
                case "DefaultFullTextLanguageName":
                    data = dt.Rows[2][1];
                    break;
                case "TwoDigitYearCutoff":                
                    data = dt.Rows[3][0];
                    break;
                case "TransformNoiseWords":
                    data = dt.Rows[4][0];
                    break;
            }

            return data;
        }
    }

    internal class PostProcessFileProperties : PostProcess
    {
        
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            DataTable dt = null;

            string databaseName = GetTriggeredString(dp, 0);
            int fileId = GetTriggeredInt32(dp, 1);
            if (dt == null)
            {
                dt = ExecuteSql.ExecuteWithResults(String.Format(CultureInfo.InvariantCulture,
                    @"  CREATE TABLE #tempspace (value BIGINT) 
IF (SERVERPROPERTY('EngineEdition') = 11) 
INSERT INTO #tempspace VALUES(-1) 
ELSE IF (OBJECT_ID(N'msdb.sys.sp_getVolumeFreeSpace',  N'P')) is null 
INSERT INTO #tempspace VALUES(-1)
ELSE IF (SERVERPROPERTY('EngineEdition') = 9)
begin
INSERT INTO #tempspace
SELECT available_bytes/1024
FROM sys.master_files AS f
CROSS APPLY sys.dm_os_volume_stats(f.database_id, f.file_id)
WHERE f.database_id = DB_ID('master') and f.file_id = 2
end
ELSE 
INSERT INTO #tempspace EXEC msdb.sys.sp_getVolumeFreeSpace {0},{1} 
SELECT TOP 1 value  as [freebytes] from #tempspace   as [freebytes] 
DROP TABLE #tempspace", Util.MakeSqlString(databaseName), fileId), this.ConnectionInfo);

            }

            if (dt.Rows.Count > 0)
            {
                return ((long)dt.Rows[0]["freebytes"]);
            }
            return data;
        }

    }

    internal class PostProcessServerProperties : PostProcess
    {

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if (name == "ServerType")
            {
                return ((DatabaseEngineType)ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo));
            }
            else if (name == "IsContainedAuthentication")
            {
                return ExecuteSql.IsContainedAuthentication(this.ConnectionInfo);
            }

            return data;
        }

    }

}
