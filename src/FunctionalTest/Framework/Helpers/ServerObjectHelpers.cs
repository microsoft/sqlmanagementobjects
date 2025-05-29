// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TraceHelper = Microsoft.SqlServer.Test.Manageability.Utils.Helpers.TraceHelper;
using SMO = Microsoft.SqlServer.Management.Smo;
using System.Threading.Tasks;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods, constants, etc dealing with the SMO Server object
    /// </summary>
    public static class ServerObjectHelpers
    {
        private static readonly Semaphore azureDbCreateLock = new Semaphore(3, 3);

        /// <summary>
        /// Restores a database from the specified backup file. It's the callers responsibility to ensure the server
        /// has read access for the file.
        /// The files in the backup will be moved to the default data/log locations for the server
        /// </summary>
        /// <param name="server"></param>
        /// <param name="dbBackupFile">Path of backup file for restoring database</param>
        /// <param name="dbName">Name of restored database</param>
        /// <param name="azureStorageHelper"></param>
        /// <returns>Restored database</returns>
        internal static Database RestoreDatabaseFromBackup(this SMO.Server server, string dbBackupFile, string dbName, AzureStorageHelper azureStorageHelper)
        {

            var isUrl = dbBackupFile.StartsWith("http", StringComparison.OrdinalIgnoreCase);
            if (isUrl && azureStorageHelper == null)
            {
                throw new ArgumentNullException("AzureStorageHelper is required to restore from a URL");
            }
            // Check if the backup file exists
            if (!isUrl && !File.Exists(dbBackupFile))
            {
                Trace.TraceWarning($"DB Backup File '{dbBackupFile}' is not visible to the test");
            }

            if (dbBackupFile.IsPackageFileName())
            {
                return RestoreDatabaseFromPackageFile(server, dbBackupFile, dbName, azureStorageHelper);
            }

            // Handle traditional backup files
            var dataFilePath = string.IsNullOrEmpty(server.Settings.DefaultFile) ? server.MasterDBPath : server.Settings.DefaultFile;
            if (string.IsNullOrWhiteSpace(dataFilePath))
            {
                throw new InvalidOperationException("Could not get database file path for restoring from backup");
            }

            var logFilePath = string.IsNullOrEmpty(server.Settings.DefaultLog) ? server.MasterDBLogPath : server.Settings.DefaultLog;
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                throw new InvalidOperationException("Could not get database log file path for restoring from backup");
            }

            Credential azureCredential = null;
            var restore = new Restore
            {
                Database = dbName,
                Action = RestoreActionType.Database,
            };
            if (isUrl)
            {
                azureCredential = azureStorageHelper.EnsureCredential(server);
                restore.CredentialName = azureCredential.Name;
                restore.BlockSize = 512;
            }
            restore.Devices.AddDevice(dbBackupFile, isUrl ? DeviceType.Url : DeviceType.File);
            DataTable dt = restore.ReadFileList(server);

            //The files need to be moved to avoid collisions
            int index = 0;
            foreach (DataRow row in dt.Rows)
            {
                //Type == L means it's a log file so put it in the log file location, all others go to data file location
                var filePath = "L".Equals(row["Type"] as string, StringComparison.OrdinalIgnoreCase)
                    ? logFilePath
                    : dataFilePath;
                //Unique filename so new files don't collide either
                var fileName = dbName + "_" + index + Path.GetExtension(row["PhysicalName"] as string);
                _ = restore.RelocateFiles.Add(new RelocateFile(row["LogicalName"] as string, Path.Combine(filePath, fileName)));
                ++index;
            }

            TraceHelper.TraceInformation($"Restoring database '{dbName}' from backup file '{dbBackupFile}'");
            try
            {
                restore.SqlRestore(server);
            }
            finally
            {
                azureCredential?.Drop();
            }

            server.Databases.Refresh();
            return server.Databases[dbName];
        }

        private static Database RestoreDatabaseFromPackageFile(SMO.Server server, string dbBackupFile, string dbName, AzureStorageHelper azureStorageHelper)
        {
            TraceHelper.TraceInformation($"Restoring database '{dbName}' from package file '{dbBackupFile}' using sqlpackage.exe with a response file");

            // Construct the sqlpackage.exe command
            var action = dbBackupFile.EndsWith(".bacpac", StringComparison.OrdinalIgnoreCase) ? "Import" : "Publish";
            var connStr = new SqlConnectionStringBuilder(server.ConnectionContext.ConnectionString) { InitialCatalog = dbName };
            var sqlPackagePath = "sqlpackage.exe"; // Ensure sqlpackage.exe is in the PATH
            var isUrl = dbBackupFile.StartsWith("http", StringComparison.OrdinalIgnoreCase);
            if (isUrl)
            {
                dbBackupFile = azureStorageHelper.DownloadBlob(dbBackupFile);
            }
            // Create a temporary response file
            var responseFilePath = Path.GetTempFileName();
            try
            {
                // Write parameters to the response file
                File.WriteAllLines(responseFilePath, new[]
                {
                    $"/Action:{action}",
                    $"/SourceFile:\"{dbBackupFile}\"",
                    $"/TargetConnectionString:\"{connStr.ConnectionString}\""
                });

                // Execute the command with the response file
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = sqlPackagePath,
                    Arguments = $"@\"{responseFilePath}\"",
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start sqlpackage.exe process.");
                    }
                    // For some reason, trying to read StandardOut from sqlpackage causes it to hang when there's an error.
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Trace.TraceError($"*** sqlpackage.exe failed with exit code {process.ExitCode}.{Environment.NewLine}\t{error}");
                        throw new InvalidOperationException($"Failed to restore database from package file '{dbBackupFile}': {error}");
                    }
                }
            }
            finally
            {
                if (isUrl)
                {
                    File.Delete(dbBackupFile);
                }
                File.Delete(responseFilePath);
            }

            server.Databases.Refresh();
            return server.Databases[dbName];
        }

        /// <summary>
        /// Check if certain database specified by databaseName exists on the server
        /// </summary>
        /// <param name="server"></param>
        /// <param name="databaseName">Name of database</param>
        /// <returns>True if database exists, false otherwise</returns>
        internal static bool CheckDatabaseExistence(this SMO.Server server, string databaseName)
        {
            // Start another SqlConneciton using same ConnectionString as the current server object,
            // use T-Sql script exectution instead of SMO way to avoid unsolved SMO issues
            using (var conn = new SqlConnection(server.ConnectionContext.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format(
                        CultureInfo.InvariantCulture,
                        "SELECT count(*) FROM sys.databases WHERE name = {0}",
                        SmoObjectHelpers.SqlSingleQuoteString(databaseName));
                    int rowCount = (int)cmd.ExecuteScalar();
                    if (rowCount > 0)
                    {
                        return true;
                    };
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the Drives available on the server. Windows drives will have letters like C:, while
        /// Linux may just have the root /
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static IEnumerable<string> EnumerateDrives(this SMO.Server server)
        {
            var req = new Request()
            {
                Urn = "Server/Drive",
                Fields = new[] { "Name" }
            };
            return ((DataSet)(new Enumerator().Process(server.ConnectionContext, req)))
                .Tables[0]
                .Rows.OfType<DataRow>()
                .Select(r => r[0] as string);

        }

        public const string NameColumn = "Name";
        public const string IsFileColumn = "IsFile";
        public const string FullNameColumn = "FullName";

        /// <summary>
        /// Returns a DataTable whose rows have Name,IsFile,FullName columns for each file or folder
        /// in the given folder
        /// </summary>
        /// <param name="server"></param>
        /// <param name="path">Directory path on the server</param>
        /// <returns></returns>
        public static DataTable EnumerateFilesAndFolders(this SMO.Server server, string path)
        {
            var orderByIsfile = new OrderBy { Field = "IsFile" };
            var orderByName = new OrderBy { Field = "Name" };
            var req = new Request
            {
                Urn = "Server/File[@Path='" + Urn.EscapeString(path) + "']",
                Fields = new[] { NameColumn, IsFileColumn, FullNameColumn },
                OrderByList = new[] { orderByIsfile, orderByName }
            };

            DataSet ds = new Enumerator().Process(server.ConnectionContext, req);
            return ds.Tables[0];
        }

        /// <summary>
        /// Creates a database definition with the specified parameters on the specified server. This creates the local
        /// definition only, it does not call Create() on the Database.
        /// </summary>
        /// <param name="server">The server to create the database definition for</param>
        /// <param name="dbNamePrefix">The prefix to give the database name</param>
        /// <param name="dbAzureDatabaseEdition">The Azure edition to use when creating an Azure database</param>
        /// <returns>The Database object representing the definition</returns>
        public static Database CreateDatabaseDefinition(
            this SMO.Server server,
            string dbNamePrefix = "",
            SqlTestBase.AzureDatabaseEdition dbAzureDatabaseEdition = SqlTestBase.AzureDatabaseEdition.NotApplicable
        )
        {
            Database db = null;
            string databaseName = SmoObjectHelpers.GenerateUniqueObjectName(dbNamePrefix);

            try
            {
                TraceHelper.TraceInformation("Creating new database '{0}' on server '{1}'", databaseName,
                    server.Name);
                // No valid backup file location so default to creating our own
                switch (dbAzureDatabaseEdition)
                {
                    // VBUMP update compat level on Azure
                    // We set ReadOnly to make sure we exercise code paths in Database.ScriptCreate based
                    // on the property being non-null
                    case SqlTestBase.AzureDatabaseEdition.NotApplicable:
                        {
                            db = new Database(server, databaseName) { ReadOnly = false };
                            break;
                        }
                    case SqlTestBase.AzureDatabaseEdition.DataWarehouse:
                        {
                            db = new Database(server, databaseName,
                                DatabaseEngineEdition.SqlDataWarehouse)
                            {
                                AzureEdition = dbAzureDatabaseEdition.ToString(),
                                AzureServiceObjective = "DW100c",
                                MaxSizeInBytes = 1024.0 * 1024.0 * 1024.0 * 500,
                                ReadOnly = false
                            };
                            //500GB
                            break;
                        }
                    case SqlTestBase.AzureDatabaseEdition.Hyperscale:
                        {
                            db = new Database(server, databaseName,
                                DatabaseEngineEdition.SqlDatabase)
                            {
                                AzureEdition = dbAzureDatabaseEdition.ToString(),
                                AzureServiceObjective = "HS_Gen5_2",
                                // Shake out issues that only arise in case sensitive collations
                                Collation = "SQL_Latin1_General_CP1_CS_AS",
                                CatalogCollation = CatalogCollationType.DatabaseDefault,
                                MaxSizeInBytes = 0,
                                CompatibilityLevel = CompatibilityLevel.Version170,
                                ReadOnly = false
                            };
                            break;
                        }
                    case SqlTestBase.AzureDatabaseEdition.Basic:
                    case SqlTestBase.AzureDatabaseEdition.Standard:
                    case SqlTestBase.AzureDatabaseEdition.Premium:
                        {
                            db = new Database(server, databaseName,
                                DatabaseEngineEdition.SqlDatabase)
                            {
                                AzureEdition = dbAzureDatabaseEdition.ToString(),
                                MaxSizeInBytes = 1024.0 * 1024.0 * 1024.0,
                                CompatibilityLevel = CompatibilityLevel.Version170,
                                ReadOnly = false
                            };
                            //1GB
                            break;
                        }
                    default:
                        throw new InvalidOperationException(
                            string.Format(
                                "Can't recognize Azure SQL database edition '{0}' specified for current database",
                                dbAzureDatabaseEdition));
                }
            }
            catch (Exception e)
            {
                // Add in some more information
                string message = string.Format(
                    "CreateDatabaseDefinition failed when targeting server {0}. Message:\n{1}\nStack Trace:\n{2}",
                    server.Name,
                    e.BuildRecursiveExceptionMessage(),
                    e.StackTrace);
                Trace.TraceError(message);
                throw new InternalTestFailureException(message, e);
            }

            return db;
        }

        /// <summary>
        /// Creates a database with the specified parameters on the specified server.
        /// </summary>
        /// <param name="server">The server to create the database on</param>
        /// <param name="dbNamePrefix">The prefix to give the database name</param>
        /// <param name="dbAzureDatabaseEdition">The Azure edition to use when creating an Azure database</param>
        /// <param name="dbBackupFile">If specified the database backup file to use to create the server</param>
        /// <param name="useEscapedCharacters"></param>
        /// <returns>The Database object representing the database on the server</returns>
        public static Database CreateDatabaseWithRetry(
            this SMO.Server server,
            string dbNamePrefix = "",
            SqlTestBase.AzureDatabaseEdition dbAzureDatabaseEdition = SqlTestBase.AzureDatabaseEdition.NotApplicable,
            string dbBackupFile = "",
            bool useEscapedCharacters = true
        ) => CreateDatabaseWithRetry(server, new DatabaseParameters
        {
            NamePrefix = dbNamePrefix,
            AzureDatabaseEdition = dbAzureDatabaseEdition,
            BackupFile = dbBackupFile,
            UseEscapedCharacters = useEscapedCharacters
        });

        /// <summary>
        /// Creates a database with the specified parameters on the specified server.
        /// </summary>
        /// <param name="server">The server to create the database on</param>
        /// <param name="dbParameters"></param>
        /// <returns>The Database object representing the database on the server</returns>
        public static Database CreateDatabaseWithRetry(
            this SMO.Server server,
            DatabaseParameters dbParameters
        )
        {
            Database db = null;

            RetryHelper.RetryWhenExceptionThrown(
            () =>
            {
                var databaseName = SmoObjectHelpers.GenerateUniqueObjectName(dbParameters.NamePrefix, 
                    includeClosingBracket: dbParameters.UseEscapedCharacters, 
                    includeDoubleClosingBracket: dbParameters.UseEscapedCharacters, 
                    includeSingleQuote: dbParameters.UseEscapedCharacters, 
                    includeDoubleSingleQuote: dbParameters.UseEscapedCharacters);
                try
                {
                    // Bacpac files have to be applied after the database is created
                    if (string.IsNullOrEmpty(dbParameters.BackupFile) || dbParameters.BackupFile.IsPackageFileName())
                    {
                        if (server.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
                        {
                            db = new Database(server, databaseName) { ReadOnly = false };
                        }
                        else
                        {

                            TraceHelper.TraceInformation("Creating new database '{0}' on server '{1}'", databaseName,
                                server.Name);
                            switch (dbParameters.AzureDatabaseEdition)
                            {
                                // We set ReadOnly to make sure we exercise code paths in Database.ScriptCreate based
                                // on the property being non-null
                                case SqlTestBase.AzureDatabaseEdition.NotApplicable:
                                    {
                                        // VBUMP
                                        db = server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand
                                            ? new Database(server, databaseName)
                                            : new Database(server, databaseName) { ReadOnly = false, CompatibilityLevel = CompatibilityLevel.Version170 };
                                        break;
                                    }
                                case SqlTestBase.AzureDatabaseEdition.DataWarehouse:
                                    {
                                        db = new Database(server, databaseName,
                                            DatabaseEngineEdition.SqlDataWarehouse)
                                        {
                                            AzureEdition = dbParameters.AzureDatabaseEdition.ToString(),
                                            // newer regions don't support DW100c but dw1000c times out too often
                                            AzureServiceObjective = "DW100c",
                                            MaxSizeInBytes = 1024.0 * 1024.0 * 1024.0 * 500,
                                            ReadOnly = false
                                        };
                                        break;
                                    }
                                case SqlTestBase.AzureDatabaseEdition.Hyperscale:
                                    {
                                        db = new Database(server, databaseName,
                                            DatabaseEngineEdition.SqlDatabase)
                                        {
                                            AzureEdition = dbParameters.AzureDatabaseEdition.ToString(),
                                            AzureServiceObjective = "HS_Gen5_2",
                                            // Shake out issues that only arise in case sensitive collations
                                            Collation = "SQL_Latin1_General_CP1_CS_AS",
                                            CatalogCollation = CatalogCollationType.DatabaseDefault,
                                            MaxSizeInBytes = 0,
                                            CompatibilityLevel = CompatibilityLevel.Version160,
                                            ReadOnly = false
                                        };
                                        break;
                                    }
                                case SqlTestBase.AzureDatabaseEdition.Basic:
                                case SqlTestBase.AzureDatabaseEdition.Standard:
                                case SqlTestBase.AzureDatabaseEdition.Premium:
                                    {
                                        db = new Database(server, databaseName,
                                            DatabaseEngineEdition.SqlDatabase)
                                        {
                                            AzureEdition = dbParameters.AzureDatabaseEdition.ToString(),
                                            MaxSizeInBytes = 1024.0 * 1024.0 * 1024.0,
                                            CompatibilityLevel = CompatibilityLevel.Version160,
                                            ReadOnly = false
                                        };
                                        break;
                                    }
                                case SqlTestBase.AzureDatabaseEdition.GeneralPurpose:
                                case SqlTestBase.AzureDatabaseEdition.BusinessCritical:
                                    {
                                        db = new Database(server, databaseName,
                                            DatabaseEngineEdition.SqlDatabase)
                                        {
                                            AzureEdition = dbParameters.AzureDatabaseEdition.ToString(),
                                            CompatibilityLevel = CompatibilityLevel.Version160,
                                            ReadOnly = false
                                        };
                                        break;
                                    }
                                default:
                                    throw new InvalidOperationException(
                                        string.Format(
                                            "Can't recognize Azure SQL database edition '{0}' specified for current database",
                                            dbParameters.AzureDatabaseEdition));
                            }
                        }
                        if (!string.IsNullOrEmpty(dbParameters.Collation))
                        {
                            db.Collation = dbParameters.Collation;
                        }
                        // Reduce contention for Azure resources by limiting the number of simultaneous creates
                        if (server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                        {
                            try
                            {
                                _ = azureDbCreateLock.WaitOne();
                                db.Create();
                                // Give the db a few seconds to be ready for action
                                Thread.Sleep(5000);
                            }
                            finally
                            {
                                _ = azureDbCreateLock.Release();
                            }
                        }
                        else
                        {
                            try
                            {
                                db.Create();
                            }
                            catch (SmoException se) when (se.BuildRecursiveExceptionMessage().Contains("Could not obtain exclusive lock on database 'model'"))
                            {
                                server.HandleModelLock();
                                throw;
                            }
                            if (server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                            {
                                db.FileGroups["PRIMARY"].Files[0].Size = 32768;
                                db.Alter();
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(dbParameters.BackupFile))
                    {
                        // Restore DB from the specified backup file
                        TraceHelper.TraceInformation("Restoring database '{0}' from backup file '{1}'",
                             databaseName,
                             dbParameters.BackupFile);
                        db = server.RestoreDatabaseFromBackup(dbParameters.BackupFile, databaseName, ConnectionHelpers.GetAzureKeyVaultHelper().StorageHelper);
                    }
                }
                catch (Exception e)
                {
                    // Add in some more information
                    string message = string.Format(
                        "CreateDatabaseWithRetry failed when targeting server {0}. Message:\n{1}\nStack Trace:\n{2}",
                        server.Name,
                        e.BuildRecursiveExceptionMessage(),
                        e.StackTrace);
                    Trace.TraceError(message);
                    throw new InternalTestFailureException(message, e);
                }
            }, whenFunc: RetryWhenExceptionThrown, retries: 3, retryDelayMs: 30000,
            retryMessage: "Creating Initial DB failed");
            return db;
        }

        // We already have a 10 minute timeout for commands, so something is really wrong on the server if it takes this long.
        // A retry would likely just make things worse.
        private static bool RetryWhenExceptionThrown(Exception e)
        {
            var sqlException = e.FirstInstanceOf<SqlException>();
            return sqlException != null && !sqlException.BuildRecursiveExceptionMessage().Contains("Execution Timeout");
        }

        /// <summary>
        /// Creates a snapshot of the specified DB
        /// </summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static Database CreateDbSnapshotWithRetry(this SMO.Server server, Database db)
        {
            Database dbSnapshot = null;
            RetryHelper.RetryWhenExceptionThrown(
            () =>
            {
                string databaseSnapshotName = db.Name + "_ss";
                TraceHelper.TraceInformation("Creating database snapshot '{0}' on server '{1}'",
                     databaseSnapshotName,
                     server.Name);
                try
                {
                    dbSnapshot = new Database(server, databaseSnapshotName)
                    {
                        DatabaseSnapshotBaseName = db.Name
                    };

                    foreach (FileGroup fg in db.FileGroups)
                    {
                        dbSnapshot.FileGroups.Add(new FileGroup(dbSnapshot, fg.Name));
                        foreach (DataFile df in fg.Files)
                        {
                            dbSnapshot.FileGroups[fg.Name].Files.Add(
                                new DataFile(dbSnapshot.FileGroups[fg.Name], df.Name,
                                    Path.Combine(db.PrimaryFilePath, df.Name + Guid.NewGuid() + ".ss")));
                        }

                    }

                    dbSnapshot.Create();
                }
                catch (Exception e)
                {
                    // Add in some more information
                    string message = string.Format(
                        "CreateDbSnapshotWithRetry failed when targeting server {0}. Message:\n{1}\nStack Trace:\n{2}",
                        server.Name,
                        e.BuildRecursiveExceptionMessage(),
                        e.StackTrace);
                    Trace.TraceError(message);
                    throw new InternalTestFailureException(message, e);
                }
            }, retries: 3, retryDelayMs: 30000,
            retryMessage: "Creating Snapshot of initial DB failed");

            return dbSnapshot;
        }

        /// <summary>
        /// Attempts to drop the specified database, but does not throw if an error occurs. Will kill
        /// all connections to the DB for on-prem servers first (this functionality is not supported on Azure)
        /// </summary>
        /// <param name="server">The server containing the DB we want to drop</param>
        /// <param name="dbName">The name of the DB to drop</param>
        /// <returns>TRUE if the DB was successfully dropped, FALSE otherwise (either error occurred or it didn't exist)</returns>
        public static bool DropKillDatabaseNoThrow(this SMO.Server server, string dbName)
        {
            bool dbDropped = false;
            try
            {
                bool dbExists = server.CheckDatabaseExistence(dbName);
                if (dbExists)
                {
                    if (server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                    {
                        TraceHelper.TraceInformation("Dropping database [{0}] on server [{1}]", dbName, server.Name);
                        // Calling Database.Drop doesn't work for Azure SQL DW so drop at the server level, which in our case is always master
                        server.ExecutionManager.ExecuteNonQuery($"DROP DATABASE {dbName.SqlBracketQuoteString()}");
                    }
                    else
                    {
                        TraceHelper.TraceInformation("Dropping database [{0}] and closing all open connections on server [{1}]",
                             dbName,
                             server.Name);
                        // DROP DATABASE can take 5 minutes on MI, so delegate it to a background worker and ignore any errors.
                        if (server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                        {
                            var newServer = new SMO.Server(server.ConnectionContext.Copy());
                            Task.Run(() => newServer.KillDatabase(dbName));
                        }
                        else
                        {
                            server.KillDatabase(dbName);
                        }
                    }
                    dbDropped = true;
                }
            }
            catch (Exception e)
            {
                // Log this but don't re-throw since we won't consider this a test failure
                Trace.TraceWarning(
                    "Got exception trying to drop test db [{0}] on server [{1}]\nMessage: {2}\nStack Trace:\n{3}",
                    dbName,
                    server.Name,
                    e.BuildRecursiveExceptionMessage(),
                    e.StackTrace);
            }
            return dbDropped;
        }

        /// <summary>
        /// Returns the Instance-qualified net name of the specified server
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static string NetNameWithInstance(this SMO.Server server)
        {
            return !server.IsSupportedProperty("NetName") ?
                server.Name :
                server.NetName +
                   (string.IsNullOrWhiteSpace(server.InstanceName) ? string.Empty : @"\" + server.InstanceName);
        }

        /// <summary>
        /// Create a database and take full backup
        /// </summary>
        /// <param name="server">The target server</param>
        /// <returns>The newly created dataase</returns>
        public static Database CreateDatabaseAndTakeFullBackup(this SMO.Server server)
        {
            // HADR cannot handle the database with special characters in the name
            // defect 11008593 is tracking this issue
            SMO.Database db = server.CreateDatabaseWithRetry();
            db.TakeFullBackup();
            return db;
        }

        /// <summary>
        /// Creates a new Login on this server
        /// </summary>
        /// <param name="server">The server to create the login for</param>
        /// <param name="name">The name to give the login</param>
        /// <param name="loginType">The type of login</param>
        /// <param name="password">Optional - the password to give the login</param>
        /// <param name="loginCreateOptions">Optional - the options when creating the login</param>
        /// <param name="isDisabled">Whether to create the login and then immediately disable it</param>
        /// <returns>The Login object</returns>
        public static Login CreateLogin(this SMO.Server server, string name, LoginType loginType, string password = null, LoginCreateOptions? loginCreateOptions = null, bool isDisabled = false)
        {
            var login = CreateLoginDefinition(server, name, loginType);
            TraceHelper.TraceInformation("Creating login on server '{0}' of type '{1}' with name '{2}'{3}{4}",
                 server.NetNameWithInstance(),
                 loginType,
                 name,
                 loginCreateOptions.HasValue ? " and LoginCreateOptions " + loginCreateOptions.Value : string.Empty,
                 isDisabled ? " and disabled" : string.Empty);
            if (string.IsNullOrEmpty(password))
            {
                login.Create();
            }
            else
            {
                if (loginCreateOptions.HasValue)
                {
                    login.Create(password, loginCreateOptions.Value);
                }
                else
                {
                    login.Create(password);
                }
            }

            if (isDisabled)
            {
                login.Disable();
            }

            return login;
        }

        /// <summary>
        /// Creates a new Login definition for this server. This does not actually
        /// create the Login - just the local definition.
        /// </summary>
        /// <param name="server">The server to create the login for</param>
        /// <param name="name">The name to give the login</param>
        /// <param name="loginType">The type of login</param>
        /// <returns>The Login object</returns>
        public static Login CreateLoginDefinition(this SMO.Server server, string name, LoginType loginType)
        {
            TraceHelper.TraceInformation("Creating login definition of type '{0}' with name '{1}'", loginType, name);
            var login = new Login(server, name)
            {
                LoginType = loginType,
            };
            return login;
        }

        /// <summary>
        /// Checks for spids holding locks on the model database
        /// </summary>
        /// <param name="server"></param>
        public static void HandleModelLock(this SMO.Server server)
        {
            try
            {
                var dataSet = server.ExecutionManager.ExecuteWithResults(
                    "select dtl.request_session_id, isnull(des.host_name,  N''), isnull(des.program_name, N''), isnull(des.login_name, N''), isnull(des.status, N''), isnull(des.last_request_start_time, '2023-08-22')  from sys.dm_tran_locks dtl join sys.dm_exec_sessions des on des.session_id = dtl.request_session_id  where resource_database_id = 3");
                foreach (var row in dataSet.Tables[0].Rows.Cast<DataRow>())
                {
                    var spid = (int)row[0];
                    var hostname = (string)row[1];
                    var programname = (string)row[2];
                    var loginname = (string)row[3];
                    var status = (string)row[4];
                    var requesttime = (DateTime)row[5];
                    Trace.TraceWarning($"Model is locked by spid {spid}: {loginname}@{hostname} using {programname}. Status: {status} Last query time:{requesttime}");
                    var dbcc = server.ExecutionManager.ExecuteWithResults($"DBCC INPUTBUFFER({spid})");
                    foreach (var r in dbcc.Tables[0].Rows.Cast<DataRow>())
                    {
                        if (r["EventInfo"] is string eventInfo)
                        {
                            Trace.TraceWarning($"\tQuery text: {eventInfo}");
                        }
                    }

                    if (status == "sleeping")
                    {
                        var closeSpid = Environment.GetEnvironmentVariable("SMOTEST_BREAKMODELLOCK");
                        if (closeSpid == "1")
                        {
                            Trace.TraceWarning($"Force closing spid {spid}");
                            server.KillProcess(spid);
                        }
                    }

                }
            }
            catch { }
        }
    }
}
