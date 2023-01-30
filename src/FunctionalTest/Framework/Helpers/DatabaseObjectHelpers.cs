// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods, constants, etc dealing with the SMO Database object
    /// </summary>
    public static class DatabaseObjectHelpers
    {


        /// <summary>
        /// Restores a database from the specified backup file. It's the callers responsibility to ensure the server
        /// has read access for the file.
        ///
        /// The files in the backup will be moved to the default data/log locations for the server
        /// </summary>
        /// <param name="server">The server where the DB is going to be restored to</param>
        /// <param name="dbBackupFile">The full path to the backup file</param>
        /// <param name="dbName">The new name of the database</param>
        /// <param name="withNoRecovery">If true, the database will be restored with the NORECOVERY option. False, by default</param>
        /// <returns></returns>
        public static Database RestoreDatabaseFromBackup(SMO.Server server, string dbBackupFile, string dbName, bool withNoRecovery = false)
        {
            // In theory, we should ask SQL if the file exists or not... because the path could be
            // local to the server.
            if (dbBackupFile.StartsWith(@"\\") && File.Exists(dbBackupFile))
            {
                throw new InvalidArgumentException($"DB Backup File '{dbBackupFile}' does not exist");
            }

            // Get the default location where we should place the restored data files
            string dataFilePath = string.IsNullOrEmpty(server.Settings.DefaultFile) ? server.MasterDBPath : server.Settings.DefaultFile;
            if (string.IsNullOrWhiteSpace(dataFilePath))
            {
                // We failed to get the path
                throw new InvalidOperationException("Could not get database file path for restoring from backup");
            }

            // Get the default location where we should place the restored log files
            string logFilePath = string.IsNullOrEmpty(server.Settings.DefaultLog) ? server.MasterDBLogPath : server.Settings.DefaultLog;
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                // We failed to get the path
                throw new InvalidOperationException("Could not get database log file path for restoring from backup");
            }

            StringBuilder sb = new StringBuilder();

            sb.Append("RESTORE DATABASE " + dbName.SqlBracketQuoteString() + " FROM DISK = " + dbBackupFile.SqlSingleQuoteString() + " ");
            BackupInfo backupInfo = GetBackupInfo(server, dbBackupFile);
            //The files need to be moved to avoid collisions
            int index = 0;
            foreach (var file in backupInfo.Files)
            {
                //Type == L means it's a log file so put it in the log file location, all others go to data file location
                string filePath = "L".Equals(file.FileType, StringComparison.OrdinalIgnoreCase)
                    ? logFilePath
                    : dataFilePath;
                //Unique filename so new files don't collide either
                string fileName = dbName + "_" + index + Path.GetExtension(file.PhysicalName);
                string resultPath = Path.Combine(filePath, fileName);

                string delimiter = index == 0 ? "WITH " : ", ";
                sb.AppendLine(delimiter);
                sb.Append("MOVE " + file.LogicalName.SqlSingleQuoteString() + " TO " + resultPath.SqlSingleQuoteString() + " ");
                ++index;
            }

            if (withNoRecovery)
            {
                sb.Append(", NORECOVERY");
            }

            TraceHelper.TraceInformation(string.Format("Restoring database '{0}' from backup file '{1}'", dbName, dbBackupFile));
            server.ConnectionContext.ExecuteNonQuery(sb.ToString());

            server.Databases.Refresh();
            return server.Databases[dbName];
        }

        private class BackupInfo
        {
            public BackupInfo()
            {
                Files = new List<BackupFileInfo>();
            }

            public List<BackupFileInfo> Files;
        }

        private class BackupFileInfo
        {
            public string LogicalName;
            public string PhysicalName;
            public string FileType;
        }

        private static BackupInfo GetBackupInfo(SMO.Server server, string dbBackupFile)
        {
            BackupInfo info = new  BackupInfo();
            string escapedDbBackupFile = SmoObjectHelpers.SqlEscapeSingleQuote(dbBackupFile);
            string restoreFilelistCommand = $"RESTORE FILELISTONLY FROM DISK = '{escapedDbBackupFile}'";

            using (SqlDataReader reader = server.ConnectionContext.ExecuteReader(restoreFilelistCommand))
            {
                while (reader != null && reader.Read())
                {
                    BackupFileInfo fileInfo = new BackupFileInfo();
                    fileInfo.LogicalName = reader["LogicalName"] == null ? null : reader["LogicalName"].ToString();
                    fileInfo.PhysicalName = reader["PhysicalName"] == null ? null : reader["PhysicalName"].ToString();
                    fileInfo.FileType = reader["Type"] == null ? null : reader["Type"].ToString();
                    info.Files.Add(fileInfo);
                }
            }

            return info;
        }

        /// <summary>
        /// Creates a stored procedure definition with a uniquely generated name prefixed by the specified prefix and defined with the specified
        /// body and header. Optionally allows specifying the schema and whether the view is Schema Bound.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="spNamePrefix"></param>
        /// <param name="schema"></param>
        /// <param name="textBody"></param>
        /// <param name="isSchemaBound"></param>
        /// <returns></returns>
        public static StoredProcedure CreateSPDefinition(this Database database, string spNamePrefix, string schema, string textBody, bool isSchemaBound = false)
        {
            var proc = string.IsNullOrEmpty(schema) ?
                new StoredProcedure(database, SmoObjectHelpers.GenerateUniqueObjectName(spNamePrefix)) :
                new StoredProcedure(database, SmoObjectHelpers.GenerateUniqueObjectName(spNamePrefix), schema);
            proc.TextBody = textBody;
            proc.TextHeader = string.Format("CREATE PROCEDURE {0}.{1} {2} AS",
                SmoObjectHelpers.SqlBracketQuoteString(proc.Schema),
                SmoObjectHelpers.SqlBracketQuoteString(proc.Name),
                isSchemaBound ? "WITH SCHEMABINDING" : string.Empty);
           TraceHelper.TraceInformation("Creating new stored procedure definition \"{0}\"", proc.Name);
            return proc;
        }

        /// <summary>
        /// Creates a basic table with a uniquely generated name prefixed by the specified prefix.
        /// Optionally allows specifying the schema and columns to create. If no columns are given
        /// a single default one will be added.
        /// </summary>
        /// <param name="database">The database on which to create the table.</param>
        /// <param name="tableNamePrefix">The name prefix for the table.</param>
        /// <param name="schemaName">The optional schema.</param>
        /// <param name="tableProperties">The optional table properties to use.</param>
        /// <param name="columnProperties">The column properties to use.</param>
        /// <param name="indexProperties">The index properties to use.</param>
        /// <param name="includeNameUniqueifier">True to include a uniqueifier.</param>
        /// <returns>The SMO table.</returns>
        public static Table CreateTable(
            Database database,
            string tableNamePrefix,
            string schemaName = null,
            TableProperties tableProperties = null,
            ColumnProperties[] columnProperties = null,
            IndexProperties[] indexProperties = null,
            bool includeNameUniqueifier = true)
        {
            Table table = CreateTableDefinition(database, tableNamePrefix, schemaName, tableProperties, columnProperties, indexProperties, includeNameUniqueifier);

           TraceHelper.TraceInformation("Creating new table \"{0}\".", table.Name);

            table.Create();

            return table;
        }

        /// <summary>
        /// Creates a basic table with a uniquely generated name prefixed by the specified prefix.
        /// Optionally allows specifying the schema and columns to create. If no columns are given
        /// a single default one will be added.
        /// </summary>
        /// <param name="database">The database on which to create the table.</param>
        /// <param name="tableNamePrefix">The name prefix for the table.</param>
        /// <param name="schemaName">The optional schema.</param>
        /// <param name="tableProperties">The table properties to use.</param>
        /// <param name="columnProperties">The list of columns.</param>
        /// <param name="indexProperties">Indexes to add to the table.</param>
        /// <param name="includeNameUniqueifier">Include a name uniqueifier to the table name.</param>
        /// <returns>The table.</returns>
        public static Table CreateTableDefinition(
            this Database database,
            string tableNamePrefix,
            string schemaName = null,
            TableProperties tableProperties = null,
            ColumnProperties[] columnProperties = null,
            IndexProperties[] indexProperties = null,
            bool includeNameUniqueifier = true)
        {
            Table table = string.IsNullOrEmpty(schemaName) ?
                new Table(database, includeNameUniqueifier ? SmoObjectHelpers.GenerateUniqueObjectName(tableNamePrefix) : tableNamePrefix) :
                new Table(database, includeNameUniqueifier ? SmoObjectHelpers.GenerateUniqueObjectName(tableNamePrefix) : tableNamePrefix, schemaName);

            // Apply any table properties to the table.
            //
            if (tableProperties != null)
            {
                tableProperties.ApplyProperties(table);
            }

            if (columnProperties == null ||columnProperties.Any() == false)
            {
                if (tableProperties == null || !tableProperties.IsEdge)
                {
                    // User didn't specify any columns, just add in a default one. In the
                    // case of edge tables columns will only be added to this collection if
                    // they are provided.
                    //
                    table.Columns.Add(new Column(table, "col_1", new DataType(SqlDataType.Int)));
                }
            }
            else
            {
                foreach (ColumnProperties columnProps in columnProperties)
                {
                    table.Columns.Add(
                        new Column(
                            table,
                            columnProps.Name,
                            columnProps.SmoDataType)
                        {
                            Nullable = columnProps.Nullable,
                            Identity = columnProps.Identity
                        });
                }
            }

            // Append indexes during creation if there are any.
            //
            if (indexProperties != null && indexProperties.Any())
            {
                foreach (IndexProperties index in indexProperties)
                {
                    var idx = new SMO.Index(table, index.Name)
                    {
                        IndexType = index.IndexType,
                        IndexKeyType = index.KeyType,
                    };

                    if (index.Columns != null && index.Columns.Any())
                    {
                        foreach (Column column in index.Columns)
                        {
                            idx.IndexedColumns.Add(new IndexedColumn(idx, column.Name));
                        }
                    }

                    if (index.ColumnNames != null && index.ColumnNames.Any())
                    {
                        foreach (string columnName in index.ColumnNames)
                        {
                            idx.IndexedColumns.Add(new IndexedColumn(idx, columnName));
                        }
                    }

                    table.Indexes.Add(idx);
                }
            }

           TraceHelper.TraceInformation("Creating new table definition \"{0}\" with {1} columns and {2} indexes.", table.Name, table.Columns.Count, table.Indexes.Count);

            return table;
        }

        /// <summary>
        /// Creates a basic table with a uniquely generated name prefixed by the specified prefix.
        /// Optionally allows specifying the columns to create. If no columns are given a single
        /// default one will be added. Will be part of the "dbo" schema.
        /// </summary>
        /// <param name="database">The database this table will be created in.</param>
        /// <param name="tableNamePrefix">The table name prefix for the table.</param>
        /// <param name="columns">The list of columns.</param>
        /// <returns>The table object.</returns>
        public static Table CreateTable(this Database database, string tableNamePrefix, params ColumnProperties[] columns)
        {
            return CreateTable(database, tableNamePrefix, schemaName: "dbo", tableProperties: null, columnProperties: columns);
        }

        /// <summary>
        /// Create an external language.
        /// </summary>
        /// <param name="db">The target database</param>
        /// <param name="languageName">The language name</param>
        /// <returns>The newly created external language</returns>
        public static ExternalLanguage CreateExternalLanguageDefinition(this SMO.Database db, string languageName)
        {
            TraceHelper.TraceInformation($"Creating external language [{languageName}] for database [{db.Name}]");
            SMO.ExternalLanguage lang = new SMO.ExternalLanguage(db, languageName);
            return lang;
        }

        /// <summary>
        /// Call CreateExternalLanguageDefinition to create an external language on the server.
        /// </summary>
        /// <param name="db">The target database</param>
        /// <param name="languageName">The language name</param>
        /// <param name="externalLangFileName">Name of the extension .dll or .so file</param>
        /// <param name="externalLangFilePath">The full file path to the .zip or tar.gz file containing the extensions code</param>
        /// <param name="externalLangContentBits">The content of the language as a hex literal, similar to assemblies.</param>
        /// <param name="platform">The platform language was created for</param>
        /// <returns>The newly created external language</returns>
        /// <remarks>One and only one from externalLangFilePath or externalLangContentBits must be specified. </remarks>
        public static ExternalLanguage CreateExternalLanguage(
            this SMO.Database db,
            string languageName,
            string externalLangFileName,
            ExternalLanguageFilePlatform platform = ExternalLanguageFilePlatform.Default,
            string externalLangFilePath = null,
            byte[] externalLangContentBits = null)
        {
            // At least a language file path or language binary needs to be provided
            //
            if (string.IsNullOrEmpty(externalLangFilePath) && externalLangContentBits == null)
            {
                throw new InvalidOperationException("At least a language file path or language binary needs to be provided.");
            }

            var externalLanguage = CreateExternalLanguageDefinition(db, languageName);
            if (!string.IsNullOrEmpty(externalLangFilePath))
            {
                externalLanguage.AddFile(externalLangFileName, externalLangFilePath, platform);
            }
            else
            {
                externalLanguage.AddFile(externalLangFileName, externalLangContentBits, platform);
            }

            externalLanguage.Create();
            return externalLanguage;
        }

        /// <summary>
        /// Create an external library.
        /// </summary>
        /// <param name="db">The target database</param>
        /// <param name="libraryName">The library name</param>
        /// <returns>The newly created external library</returns>
        public static ExternalLibrary CreateExternalLibraryDefinition(this SMO.Database db, string libraryName)
        {
           TraceHelper.TraceInformation("Creating external library [{0}] for database [{1}]", libraryName, db.Name);
            SMO.ExternalLibrary lib = new SMO.ExternalLibrary(db, libraryName);
            return lib;
        }

        /// <summary>
        /// Call CreateExternalLibraryDefinition to create an external library on the server.
        /// </summary>
        /// <param name="db">The target database</param>
        /// <param name="libraryName">The library name</param>
        /// <param name="libraryContent"></param>
        /// <param name="contentType"></param>
        /// <returns>The newly created external library</returns>
        public static ExternalLibrary CreateExternalLibrary(this SMO.Database db, string libraryName, string libraryContent, ExternalLibraryContentType contentType)
        {
            var externalLibrary = CreateExternalLibraryDefinition(db, libraryName);
            externalLibrary.ExternalLibraryLanguage = "R";
            externalLibrary.Create(libraryContent, contentType);
            return externalLibrary;
        }

        /// <summary>
        /// Creates a user defined function definition with a uniquely generated name prefixed by the specified prefix and defined with the specified
        /// body and header. Optionally allows specifying the schema and whether the view is Schema Bound.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="udfNamePrefix"></param>
        /// <param name="schema"></param>
        /// <param name="textBody"></param>
        /// <param name="isSchemaBound"></param>
        /// <returns></returns>
        public static UserDefinedFunction CreateUdfDefinition(this Database database, string udfNamePrefix, string schema, string textBody, bool isSchemaBound = false)
        {
            var udf = string.IsNullOrEmpty(schema) ?
                new UserDefinedFunction(database, SmoObjectHelpers.GenerateUniqueObjectName(udfNamePrefix)) :
                new UserDefinedFunction(database, SmoObjectHelpers.GenerateUniqueObjectName(udfNamePrefix), schema);
            udf.TextBody = textBody;
            udf.TextHeader = string.Format("CREATE FUNCTION {0}.{1} {2} AS",
                SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                SmoObjectHelpers.SqlBracketQuoteString(udf.Name),
                isSchemaBound ? "WITH SCHEMABINDING" : string.Empty);
           TraceHelper.TraceInformation("Creating new user defined function definition \"{0}\"", udf.Name);
            return udf;
        }

        /// <summary>
        /// Creates a user defined function with a uniquely generated name prefixed by the specified prefix and defined with the specified
        /// body and header. Optionally allows specifying the schema and whether the view is Schema Bound.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="udfNamePrefix"></param>
        /// <param name="schema"></param>
        /// <param name="textBody"></param>
        /// <param name="isSchemaBound"></param>
        /// <returns></returns>
        public static UserDefinedFunction CreateUdf(this Database database, string udfNamePrefix, string schema, string textBody, bool isSchemaBound = false)
        {
            var udf = database.CreateUdfDefinition(udfNamePrefix, schema, textBody, isSchemaBound);
            udf.Create();
            return udf;
        }

        /// <summary>
        /// Creates a view definition with a uniquely generated name prefixed by the specified prefix and defined with the specified
        /// body and header. Optionally allows specifying the schema and whether the view is Schema Bound.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="viewNamePrefix"></param>
        /// <param name="schema"></param>
        /// <param name="textBody"></param>
        /// <param name="isSchemaBound"></param>
        /// <returns></returns>
        /// <remarks>You do not need to create new column objects (normally required when creating a view in SMO), this
        /// method will create the new columns for you based on the specified columns</remarks>
        public static View CreateViewDefinition(this Database database, string viewNamePrefix, string schema, string textBody, bool isSchemaBound = false)
        {
            var view = string.IsNullOrEmpty(schema) ?
                new View(database, SmoObjectHelpers.GenerateUniqueObjectName(viewNamePrefix)) :
                new View(database, SmoObjectHelpers.GenerateUniqueObjectName(viewNamePrefix), schema);
            view.TextBody = textBody;
            view.TextHeader = string.Format("CREATE VIEW {0}.{1} {2} AS",
                SmoObjectHelpers.SqlBracketQuoteString(view.Schema),
                SmoObjectHelpers.SqlBracketQuoteString(view.Name),
                isSchemaBound ? "WITH SCHEMABINDING" : string.Empty);
           TraceHelper.TraceInformation("Creating new view definition \"{0}\" with {1} columns", view.Name, view.Columns.Count);
            return view;
        }

        /// <summary>
        /// Creates a view with a uniquely generated name prefixed by the specified prefix and defined with the specified
        /// body and header. Optionally allows specifying the schema and whether the view is Schema Bound.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="viewNamePrefix"></param>
        /// <param name="schema"></param>
        /// <param name="textBody"></param>
        /// <param name="isSchemaBound"></param>
        /// <returns></returns>
        public static View CreateView(this Database database, string viewNamePrefix, string schema, string textBody, bool isSchemaBound = false)
        {
            var view = database.CreateViewDefinition(viewNamePrefix, schema, textBody, isSchemaBound);
            view.Create();
            return view;
        }

        /// <summary>
        /// Creates a memory-optimized-enabled filegroup (pre-requirement for creating Hekaton tables).
        /// New filegroup (and a file within it) are created alongside with default (primary) filegroup
        /// of a given database.
        /// </summary>
        /// <param name="database">The SMO database object.</param>
        /// <param name="filegroupName">The filegroup name.</param>
        /// <returns>Newly created filegroup.</returns>
        public static FileGroup CreateMemoryOptimizedFileGroup(this Database database, string filegroupName)
        {
            FileGroup memoryOptimizedFg = new FileGroup(database, filegroupName, FileGroupType.MemoryOptimizedDataFileGroup);
            memoryOptimizedFg.Create();

            DataFile dataFile = new DataFile(memoryOptimizedFg, filegroupName)
            {
                FileName = PathWrapper.Combine(PathWrapper.GetDirectoryName(database.FileGroups[0].Files[0].FileName), filegroupName)
            };

            dataFile.Create();

            return memoryOptimizedFg;
        }

        /// <summary>
        /// Create a DatabaseDdlTrigger with a uniquely generated name prefixed by the specified prefix. </summary>
        /// <param name="database"></param>
        /// <param name="triggerNamePrefix">The prefix name of the created trigger. </param>
        /// <param name="forOrAfterEvent">For/After ddl event, e.g. "FOR ALTER" or "AFTER CREATE". </param>
        /// <param name="textBody">The tsql body of the trigger. </param>
        /// <returns></returns>
        public static DatabaseDdlTrigger CreateDatabaseDdlTrigger(this Database database, string triggerNamePrefix, string forOrAfterEvent, string textBody)
        {
            var triggerDb = new DatabaseDdlTrigger(database, SmoObjectHelpers.GenerateUniqueObjectName(triggerNamePrefix));
            triggerDb.TextHeader = string.Format("CREATE TRIGGER {0} ON DATABASE {1} AS", SmoObjectHelpers.SqlBracketQuoteString(triggerDb.Name), forOrAfterEvent);
            triggerDb.TextBody = textBody;
            triggerDb.ImplementationType = ImplementationType.TransactSql;
            triggerDb.ExecutionContext = DatabaseDdlTriggerExecutionContext.Caller;
            triggerDb.Create();
            return triggerDb;
        }

        /// <summary>
        /// Creates a new FileGroup with the specific name, and then adds a new DataFile on the created FileGroup.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="fileGroupName"></param>
        /// <returns></returns>
        public static FileGroup CreateFileGroupWithDataFile(this Database database, string fileGroupName)
        {
            // Create a FileGroup object with the specific name
            FileGroup fileGroup = new FileGroup(database, fileGroupName);
            fileGroup.Create();

            // Create a DataFile on the FileGroup that is created before.
            DataFile dataFile = new DataFile(fileGroup, fileGroupName);
            dataFile.FileName = string.Format("{0}.mdf", PathWrapper.Combine(database.PrimaryFilePath, dataFile.Name));
            dataFile.Create();

            return fileGroup;
        }

        /// <summary>
        /// Creates the partition scheme and specifies the file group for each partiton.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="partitionPrefixName"></param>
        /// <param name="rangeValues">Specify the values that divide the data, e.g. {"3","6","9"} or {"1/1/2013","1/1/2014","1/1/2015"} </param>
        /// <param name="dataType">The column type that the partition function uses, e.g. Int or DateTime</param>
        /// <returns></returns>
        public static PartitionScheme CreatePartitionSchemeWithFileGroups(this Database database, string partitionPrefixName, object[] rangeValues, DataType dataType)
        {
            // The partition number should be one more than the size of the rangeValues.
            int partitionNumber = rangeValues.Count() + 1;

            // Create the fileGroups with the data file for each partition
            Collection<FileGroup> fileGroups = new Collection<FileGroup>();
            for (int id = 0; id < partitionNumber; id++)
            {
                // Azure SQL DB only supports PRIMARY filegroup
                if (database.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    fileGroups.Add(database.FileGroups["PRIMARY"]);
                }
                else
                {
                    fileGroups.Add(DatabaseObjectHelpers.CreateFileGroupWithDataFile(database,
                        String.Format("{0}_FG{1}", partitionPrefixName, id.ToString())));
                }
            }

            // Create a partition function with the specific range values and data type for the partition scheme.
            PartitionFunction partitionFunction = new PartitionFunction(database, partitionPrefixName + "_PF");
            partitionFunction.RangeValues = rangeValues;

            PartitionFunctionParameter partitionFunctionParameter = new PartitionFunctionParameter(partitionFunction, dataType);
            partitionFunction.PartitionFunctionParameters.Add(partitionFunctionParameter);

            partitionFunction.Create();

            // Create a partition scheme and specify the partition function and the file groups
            PartitionScheme partitionScheme = new PartitionScheme(database, partitionPrefixName + "_PS");

            partitionScheme.PartitionFunction = partitionFunction.Name;
            for (int id = 0; id < partitionNumber; id++)
            {
                partitionScheme.FileGroups.Add(fileGroups[id].Name);
            }

            partitionScheme.Create();

            return partitionScheme;
        }

        /// <summary>
        /// Take a full backup of the database
        /// </summary>
        /// <param name="database">The database</param>
        public static void TakeFullBackup(this Database database)
        {
            Backup backup = new Backup
            {
                Action = BackupActionType.Database,
                Database = database.Name,
                BackupSetDescription = string.Format("Full backup of {0}", database.Name),
                BackupSetName = database.Name,
                ExpirationDate = DateTime.Now.AddYears(1),
                LogTruncation = BackupTruncateLogType.Truncate,
                Incremental = false, //full backup
            };

            BackupDeviceItem bdi = new BackupDeviceItem(string.Format("{0}.bak", database.Name), DeviceType.File);
            backup.Devices.Add(bdi);

            backup.SqlBackup(database.Parent);
        }

        /// <summary>
        /// Creates a new User in this database mapped to the specified login
        /// </summary>
        /// <param name="database">The database to add the user to</param>
        /// <param name="name">The name to give the user</param>
        /// <param name="loginName">The name of the login this user maps to</param>
        /// <param name="password">Optional - the password for the user</param>
        /// <returns>The created User object</returns>
        public static User CreateUser(this Database database, string name, string loginName, string password = "")
        {
            var user = database.CreateUserDefinition(name, loginName);
           TraceHelper.TraceInformation("Creating new User definition '{0}' in database '{1}'{2}{3}",
                name,
                database.Name,
                string.IsNullOrEmpty(loginName) ? string.Empty : " for login " + loginName,
                string.IsNullOrEmpty(loginName) ? string.Empty : " with password '" + password + "'");
            if (string.IsNullOrEmpty(password))
            {
                user.Create();
            }
            else
            {
                user.Create(password);
            }
            return user;
        }

        /// <summary>
        /// Creates a User definition for this database mapped to the specified login.
        /// This does not actually create the User on the server, just the definition.
        /// </summary>
        /// <param name="database">The database to create the definition on</param>
        /// <param name="name">The name to give the user</param>
        /// <param name="loginName">The name of the login this user maps to</param>
        /// <returns>The User object definition</returns>
        public static User CreateUserDefinition(this Database database, string name, string loginName = "")
        {
           TraceHelper.TraceInformation("Creating new User definition '{0}'{1}",
                name,
                string.IsNullOrEmpty(loginName) ? string.Empty : " for login " + loginName);
            var user = new User(database, name);
            if (!string.IsNullOrEmpty(loginName))
            {
                user.Login = loginName;
            }
            return user;
        }

        /// <summary>
        /// Creates a UDDT with the given name and default content
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static UserDefinedDataType CreateUserDefinedDataType(this Database database, string name)
        {
            var uddt = new UserDefinedDataType(database, name);
            uddt.Create();
            return uddt;
        }

        /// <summary>
        /// Creates a user defined aggregate with the given name and default content
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static UserDefinedAggregate CreateUserDefinedAggregate(this Database database, string name)
        {
            var uda = new UserDefinedAggregate(database, name);

            uda.Parameters.Add(new UserDefinedAggregateParameter(uda, "udaParam", DataType.Int));
            uda.Create();
            return uda;
        }

        /// <summary>
        /// Creates a database scoped credential with the given name and default content
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DatabaseScopedCredential CreateDatabaseScopedCredential(this Database database, string name)
        {
            var dsc = new DatabaseScopedCredential(database, name);
            dsc.Create("userName", Guid.NewGuid().ToString());
            return dsc;
        }

        /// <summary>
        /// Creates an asymmetric key with the given name and default content
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AsymmetricKey CreateAsymmetricKey(this Database database, string name)
        {
            var key = new AsymmetricKey(database, name);
            key.Create(AsymmetricKeyEncryptionAlgorithm.Rsa1024, Guid.NewGuid().ToString());
            return key;
        }
    } 
}
