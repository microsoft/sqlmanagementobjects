// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;
using Microsoft.SqlServer.Management.Smo;
using System.Data;
using System.IO;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DatabaseSmoTests : SqlTestBase
    {

        /// <summary>
        /// In a nutshell, we validate that Database.Alter() on a database in "restoring" state does not throw.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = HostPlatformNames.Windows, MaxMajor = 15, MinMajor = 15)]
        public void Database_Scripting_Alter_When_Database_Is_Restoring_succeeds()
        {
            ExecuteFromDbPool((db) =>
            {
                string backupfile = null;
                Database restoringDb = null;

                try
                {
                    // Take full backup of the database
                    db.TakeFullBackup();

                    // Get path to the backup file
                    backupfile = Path.Combine(db.Parent.BackupDirectory, $"{db.Name}.bak");
                    
                    // Restore database with NORECOVERY (in OE, it shos as "Restoring..."
                    restoringDb = DatabaseObjectHelpers.RestoreDatabaseFromBackup(db.Parent, backupfile, db.Name + "_new", withNoRecovery: true);

                    // Trying to Alter() the DB should NOT throw
                    Assert.DoesNotThrow(restoringDb.Alter, "Alter of a restoring DB failed!");
                }
                finally
                {
                    // Cleanup after ourselves
                    if (backupfile != null)
                    {
                        // Best effort... can be removed once I create the scheduled task that deletes
                        // old .bak files on the servers.
                        var guessedRemotePath = @"\\" + db.Parent.Name + @"\" + backupfile.Replace(':', '$');
                        try
                        {
                            if (File.Exists(guessedRemotePath))
                            {
                                File.Delete(guessedRemotePath);
                                Trace.TraceInformation($"Best effort cleanup succeeded: {guessedRemotePath}");
                            }
                        }
                        catch
                        {
                            Trace.TraceInformation($"Best effort cleanup failed: {guessedRemotePath}");
                        }
                    }

                    if (restoringDb != null)
                    {
                        restoringDb.Drop();
                    }

                }
            });
        }


        /// <summary>
        /// Tests that 'schema' is reported correctly (i.e. not blank all the time)
        /// by the EnumObjects() method. Also, we validate the other 3 properties.
        ///
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void EnumObjects_Sets_Synonym_Schema_And_Other_Properties()
        {
            ExecuteWithDbDrop("Synonym",
               database =>
               {
                   // 
                   var expectedSynSchema = database.Schemas[0].Name;                   // any existing schema is good
                   var expectedSynName = SmoObjectHelpers.GenerateUniqueObjectName();  // a given random name

                   // Create a new synonym
                   _SMO.Synonym syn = new _SMO.Synonym(database, expectedSynName)
                   {
                       BaseDatabase = database.Name,
                       BaseSchema = "dummySchema",          // dummy value - does not matter
                       BaseObject = "dummyObj",             // dummy value - does not matter
                       Schema = expectedSynSchema
                   };

                   syn.Create();
                   var script = database.ExecutionManager.RecordQueryText(() =>
                   {
                       // Now, we retrieve the same object we just created by calling EnumObjects()
                       var objs = database.EnumObjects(DatabaseObjectTypes.Synonym, _SMO.SortOrder.Schema);
                       var synobj = objs.Rows.Cast<System.Data.DataRow>().Where(r => (string)r["Name"] == expectedSynName).Single();

                       // The original bug was that Schema was coming back as blank, because there was an assumption
                       // that synonyms did not have a schema (which was incorrect)
                       Assert.That(synobj["Schema"], Is.EqualTo(expectedSynSchema), "Unexpected value for Schema");

                       // While we are at it, let's also check the other properties...
                       Assert.That(synobj["DatabaseObjectTypes"], Is.EqualTo("Synonym"), "Unexpected value for DatabaseObjectTypes");
                       Assert.That(synobj["Name"], Is.EqualTo(expectedSynName), "Unexpected value for Name");
                       Assert.That(synobj["Urn"], Is.EqualTo(syn.Urn.ToString()), "Unexpected value for Urn");
                   }, alsoExecute: true);
                   Assert.That(script.ToSingleString(), Contains.Substring("ORDER BY [Schema]"), "EnumObjects (SortOrder.Schema)");
                   if (database.DatabaseEngineType == DatabaseEngineType.Standalone)
                   {
                       script = database.ExecutionManager.RecordQueryText(() => database.EnumObjects(), alsoExecute: true);
                       Assert.That(script.ToSingleString(), Contains.Substring("ORDER BY [DatabaseObjectTypes]"), "EnumObjects()");
                   }
                   script = database.ExecutionManager.RecordQueryText(() => database.EnumObjects(DatabaseObjectTypes.Table, _SMO.SortOrder.Name), alsoExecute: true);
                   Assert.That(script.ToSingleString(), Contains.Substring("ORDER BY [Name]"), "EnumObjects()");
                   script = database.ExecutionManager.RecordQueryText(() => database.EnumObjects(DatabaseObjectTypes.View, _SMO.SortOrder.Urn), alsoExecute: true);
                   Assert.That(script.ToSingleString(), Contains.Substring("ORDER BY [Urn]"), "EnumObjects()");
               });
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        public void Database_Drop_does_not_throw_when_Parent_is_user_database()
        {
            ExecuteWithDbDrop(db =>
            {
                var connString = new SqlConnectionStringBuilder(this.SqlConnectionStringBuilder.ConnectionString)
                {
                    InitialCatalog = db.Name
                };
                var server = new _SMO.Server(new ServerConnection(new SqlConnection(connString.ConnectionString)));
                var dbToDrop = server.Databases[db.Name];
                var droppedEventRaised = false;
                SmoApplication.EventsSingleton.ObjectDropped += (s, e) =>
                {
                    if (e.Urn == dbToDrop.Urn) droppedEventRaised = true;
                };
                Assert.DoesNotThrow(() => dbToDrop.Drop(), "Drop when initial catalog is database name");
                Assert.Throws<_SMO.FailedOperationException>(() => db.Refresh(), "Refresh should fail after db is dropped");
                Assert.That(droppedEventRaised, Is.EqualTo(true), "Dropped event should be raised");
            });
        }

        /// <summary>
        /// DW does not support Drop calls on the user database directly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void Database_Drop_DW_throws_when_Parent_is_user_database()
        {
            ExecuteWithDbDrop(db =>
            {
                var connString = new SqlConnectionStringBuilder(this.SqlConnectionStringBuilder.ConnectionString)
                {
                    InitialCatalog = db.Name
                };
                var server = new _SMO.Server(new ServerConnection(new SqlConnection(connString.ConnectionString)));
                var dbToDrop = server.Databases[db.Name];

                var ex = Assert.Throws<_SMO.FailedOperationException>(() => dbToDrop.Drop(), "Drop when initial catalog is DW database name");
                Assert.That(ex.InnerException, Is.InstanceOf<ExecutionFailureException>(),
                    "InnerException should be ExecutionFailureException");
                Assert.That(ex.InnerException.InnerException, Is.InstanceOf<SqlException>(), "InnerException.InnerException should be SqlException");
                Assert.That(ex.InnerException.InnerException.Message, Contains.Substring("'Drop Database' is not supported"),
                    "InnerException message");
            });
        }

        /// <summary>
        /// Verify that property SpaceAvailable is reported as 0 (n/a) for SQL DW.
        /// </summary>
        [TestMethod]
        [TestCategory("Legacy")]    /* slow test, not for PR validation */
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void Database_SpaceAvailable_Is_Zero_For_DW()
        {
            ExecuteTest(
                srv => {
                    Database_SpaceAvailable_Is_Zero(AzureDatabaseEdition.DataWarehouse);
                });
        }

        /// <summary>
        /// Verify that property SpaceAvailable is reported as 0 (n/a) for SQL DB (Hyperscale edition).
        /// </summary>
        [TestMethod]
        [TestCategory("Legacy")]    /* slow test, not for PR validation */
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void Database_SpaceAvailable_Is_Zero_For_Hyperscale()
        {
            ExecuteTest(
                srv => {
                    Database_SpaceAvailable_Is_Zero(AzureDatabaseEdition.Hyperscale);
                });
        }

        /// <summary>
        /// Helped method for the 2 above tests to assert that the SpaceAvailable property is 0 for the given Azure Database Edition.
        /// </summary>
        private void Database_SpaceAvailable_Is_Zero(AzureDatabaseEdition ade)
        {
            ExecuteWithDbDrop(db =>
            {
                Assert.That(db.SpaceAvailable, Is.EqualTo(0), $"Unexpected SpaceAvailable for azure edition '{db.AzureEdition}'");
            }, ade);
        }

        [TestMethod]
        [TestCategory("Legacy")]    /* test prone to race condition (so it seems), not for PR validation */
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.Express)]
        public void Database_enabling_encryption_creates_encryption_key()
        {
            ExecuteWithDbDrop(db =>
            {
                var encryptionEnabled = !db.EncryptionEnabled;
                db.EncryptionEnabled = encryptionEnabled;
                // on premises servers post-2008 r2 require an encryption key
                if (db.IsSupportedProperty(nameof(db.HasDatabaseEncryptionKey)) && db.DatabaseEncryptionKey.State != SqlSmoState.Existing)
                {
                    // Script snippet copied from databaseencryptionkey_smotestsuite to use the same cert name
                    db.Parent.ConnectionContext.ExecuteNonQuery($@"--First create master key for server (on master db)
IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE symmetric_key_id = 101)
BEGIN
	CREATE MASTER KEY ENCRYPTION BY PASSWORD = '{SqlTestRandom.GeneratePassword()}'
END
GO

--Then create server-scoped certificate
IF NOT EXISTS (SELECT * FROM sys.certificates where name = 'DEK_SmoTestSuite_ServerCertificate')
BEGIN
	CREATE CERTIFICATE DEK_SmoTestSuite_ServerCertificate
		WITH SUBJECT = 'Database Encryption Key Test Server Certificate',
		EXPIRY_DATE = '30001031',
		START_DATE = '20121031'
END");
                    db.DatabaseEncryptionKey.EncryptorName = "DEK_SmoTestSuite_ServerCertificate";
                    db.DatabaseEncryptionKey.EncryptionAlgorithm = DatabaseEncryptionAlgorithm.Aes256;
                    db.DatabaseEncryptionKey.EncryptionType = DatabaseEncryptionType.ServerCertificate;
                    Assert.That(db.DatabaseEncryptionKey.State, Is.EqualTo(SqlSmoState.Creating), "DatabaseEncryptionKey.State pre-Alter");
                }

                db.Alter();
                db.Refresh();
                Assert.That(db.EncryptionEnabled, Is.EqualTo(encryptionEnabled), "EncryptionEnabled after Alter");
                if (db.IsSupportedProperty(nameof(db.HasDatabaseEncryptionKey)))
                {
                    Assert.That(db.DatabaseEncryptionKey.State, Is.EqualTo(SqlSmoState.Existing),
                        "DatabaseEncryptionKey.State post-Alter");
                    // We can't immediately toggle encryption until the first change is done
                    var maxWaits = 300;
                    while (maxWaits-- > 0 && 
                    (db.DatabaseEncryptionKey.EncryptionState == DatabaseEncryptionState.EncryptionInProgress || db.DatabaseEncryptionKey.EncryptionState == DatabaseEncryptionState.DecryptionInProgress))
                    {
                        Thread.Sleep(100);
                        db.DatabaseEncryptionKey.Refresh();
                    }
                    db.EnableEncryption(!encryptionEnabled);
                    db.Refresh();
                    Assert.That(db.EncryptionEnabled, Is.EqualTo(!encryptionEnabled), $"EncryptionEnabled after EnableEncryption({!encryptionEnabled})");
                }
            });
        }
        /// <summary>
        /// This is a catch-all method for covering various database methods without wasting
        /// code on a separate test-per-method. 
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void Database_miscellaneous_methods_produce_correct_sql()
        {
            ExecuteWithDbDrop(db =>
            {
                // dbcc checkdb requires single user mode
                if (db.DatabaseEngineEdition != DatabaseEngineEdition.SqlDatabase)
                {
                    db.UserAccess = DatabaseUserAccess.Single;
                }
                db.AutoUpdateStatisticsEnabled = false;
                db.AutoCreateStatisticsEnabled = true;
                db.Alter();
                var table = db.CreateTable("t1", new ColumnProperties("c1", DataType.Int));
                table.InsertDataToTable(100);
                var view = db.CreateView("v1", "dbo", $"select c1 from [dbo].{table.Name.SqlBracketQuoteString()}", isSchemaBound: true);
                var index = new _SMO.Index(view, "i1") { IsClustered = true, IsUnique = true };
                index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
                index.Create();
                Assert.Multiple(() =>
                {
                    var commands = db.ExecutionManager.RecordQueryText(db.UpdateIndexStatistics, alsoExecute: true).Cast<string>();
                    Assert.That(commands, Has.Member($"UPDATE STATISTICS [dbo].{table.Name.SqlBracketQuoteString()}"), "UpdateIndexStatistics should include table t1");
                    Assert.That(commands, Has.Member($"UPDATE STATISTICS [dbo].{view.Name.SqlBracketQuoteString()}"), "UpdateIndexStatistics should include view v1");
                    PrefetchAllChildTypes(db);
                    GetArchiveReports(db);
                    if (db.DatabaseEngineEdition != DatabaseEngineEdition.SqlDatabase && db.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                    {
                        CheckTablesDataOnlyAllRepairTypes(db);
                        CheckTablesAllRepairTypes(db);
                        CheckAllocationsAllRepairTypes(db);
                    }
                });
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void Database_shrink()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = db.CreateTable("t1", new ColumnProperties("c1", DataType.Int));
                table.InsertDataToTable(100);
                Assert.Multiple(() =>
                {
                    var commands = db.ExecutionManager.RecordQueryText(() => db.Shrink(101, ShrinkMethod.Default), alsoExecute: true).Cast<string>();
                    Assert.That(commands, Has.Member($"DBCC SHRINKDATABASE(N{db.Name.SqlSingleQuoteString()}, 100 )"), "shrink 101 default");
                    commands = db.ExecutionManager.RecordQueryText(() => db.Shrink(0, ShrinkMethod.NoTruncate), alsoExecute: true).Cast<string>();
                    Assert.That(commands, Has.Member($"DBCC SHRINKDATABASE(N{db.Name.SqlSingleQuoteString()}, NOTRUNCATE)"), "shrink 0 notruncate");
                    commands = db.ExecutionManager.RecordQueryText(() => db.Shrink(0, ShrinkMethod.TruncateOnly), alsoExecute: true).Cast<string>();
                    Assert.That(commands, Has.Member($"DBCC SHRINKDATABASE(N{db.Name.SqlSingleQuoteString()}, TRUNCATEONLY)"), "shrink 0 truncateonly");
                    Assert.Throws<FailedOperationException>(() => db.Shrink(0, ShrinkMethod.EmptyFile));
                });
            });
        }


        /// <summary>
        /// This is the set of child object types that don't have a Database.PrefetchObjectsImpl implementation.
        /// As of Dec 2019 it's not clear what criteria to use to choose which child collections should
        /// be part of this API. It might be intended for objects that themselves have specific child object types
        /// that the user would want to include in the fetch in order to create the script.
        /// </summary>
        private static readonly HashSet<System.Type> nonPrefetchedTypes = new HashSet<Type>
        {
            typeof(DatabaseScopedCredential),
            typeof(Synonym),
            typeof(FullTextCatalog),
            typeof(FullTextStopList),
            typeof(SearchPropertyList),
            typeof(SecurityPolicy),
            typeof(Certificate),
            typeof(SymmetricKey),
            typeof(AsymmetricKey),
            typeof(ApplicationRole),
            typeof(ExternalLanguage)
        };

        private static readonly IEnumerable<Type> extraPrefetchedTypes = new[]
        {
            typeof(Default),
            typeof(_SMO.Rule),
            typeof(PartitionScheme),
            typeof(PartitionFunction),
        };

        private static void PrefetchAllChildTypes(_SMO.Database db)
        {
            var disc = new SfcMetadataDiscovery(typeof(_SMO.Database));
            var tObjectPermission = typeof(_SMO.IObjectPermission);
            var objectTypes = disc.Objects.Where(r =>
                    r.ContainerType != null && tObjectPermission.IsAssignableFrom(r.Type)).Select(r => r.Type)
                .Except(nonPrefetchedTypes).ToList();
            Assert.That(objectTypes, Is.Not.Empty, "Database should have collections of child objects that implement IObjectPermission");
            Assert.Multiple(() =>
            {
                foreach (var relation in objectTypes.Union(extraPrefetchedTypes))
                {
                    Assert.DoesNotThrow(() => db.PrefetchObjects(relation), $"Database.PrefetchObjects should support type {relation.Name}. If Prefetch support is not needed, update nonPrefetchedTypes.");
                }

                Assert.That(() => db.PrefetchObjects(typeof(SqlSmoObject)), Throws.InstanceOf<FailedOperationException>(), "PrefetchObjects(typeof(SqlSmoObject))");
                Assert.That(() => db.PrefetchObjects(null), Throws.InstanceOf<ArgumentNullException>(), "PrefetchObjects(null)");
                Assert.That(() => db.PrefetchObjects(typeof(Table), (ScriptingPreferences)null), Throws.InstanceOf<ArgumentNullException>(), "PrefetchObjects(Table, (ScriptingPreferences)null)");
                Assert.That(() => db.PrefetchObjects(typeof(Table), (ScriptingOptions)null), Throws.InstanceOf<ArgumentNullException>(), "PrefetchObjects(Table, (ScriptingOptions)null)");
            });
        }

        private static void GetArchiveReports(_SMO.Database db)
        {
            if (db.IsSupportedProperty(nameof(db.RemoteDataArchiveEnabled)))
            {

                IEnumerable<RemoteDataArchiveMigrationStatusReport> statusReports = Enumerable.Empty<RemoteDataArchiveMigrationStatusReport>();
                var commands = db.ExecutionManager.RecordQueryText(() => statusReports =
                    db.GetRemoteDataArchiveMigrationStatusReports(
                        DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)), 2).ToList(), alsoExecute: true).Cast<string>();

                Assert.That(statusReports, Is.Empty, "GetRemoteDataArchiveMigrationStatusReports should return empty list for non-archived DB");
                var select = commands.Single(c => c.Contains("INNER JOIN"));
                Assert.That(select, Contains.Substring("SELECT\nTOP (2) dbs.name as database_name,"), "GetRemoteDataArchiveMigrationStatusReports should select 2 rows");
                Assert.That(select, Contains.Substring("FROM\nsys.dm_db_rda_migration_status rdams"), "GetRemoteDataArchiveMigrationStatusReports should query ys.dm_db_rda_migration_status");
            }
        }

        private static void CheckTablesDataOnlyAllRepairTypes(_SMO.Database db)
        {
            IEnumerable<string> messages = Enumerable.Empty<string>();
            var commands = db.ExecutionManager.RecordQueryText(() => messages = db.CheckTablesDataOnly().Cast<string>().ToList(), alsoExecute: true).Cast<string>();
            Assert.That(messages.Take(2),
                Is.EqualTo(new object[]
                {
                    $"DBCC results for '{db.Name}'.",
                    "Warning: NO_INDEX option of checkdb being used. Checks on non-system indexes will be skipped."
                }), "CheckTablesDataOnly() messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, NOINDEX)" }),
                "CheckTablesDataOnly");
            commands = db.ExecutionManager.RecordQueryText(() => 
                messages = db.CheckTablesDataOnly(RepairOptions.AllErrorMessages |
                                              RepairOptions.ExtendedLogicalChecks |
                                              RepairOptions.NoInformationMessages | RepairOptions.TableLock |
                                              RepairOptions.EstimateOnly).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>().ToList();
            Assert.That(messages.Count,
                Is.AtMost(1),
                "CheckTablesDataOnly(<all repair options>) messages.Count");
            Assert.That(commands,
                Is.EqualTo(new[]
                {
                    $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, NOINDEX) WITH  ALL_ERRORMSGS , EXTENDED_LOGICAL_CHECKS , NO_INFOMSGS , TABLOCK , ESTIMATEONLY  "
                }), "CheckTablesDataOnly(<all repair options>)");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckTablesDataOnly(RepairStructure.None).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>().ToList();
            Assert.That(messages, Is.Empty, "CheckTablesDataOnly(RepairOptions.None) messages");
            Assert.That(commands,
                Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, NOINDEX) WITH NO_INFOMSGS" }),
                "CheckTablesDataOnly(RepairOptions.None)");
            if (db.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || db.Parent.VersionMajor > 12)
            {
                commands = db.ExecutionManager.RecordQueryText(() =>
                    messages = db.CheckTablesDataOnly(RepairOptions.None, RepairStructure.None, maxDOP: 2)
                        .Cast<string>().ToList(), alsoExecute: true)
                    .Cast<string>().ToList();

                Assert.That(messages.Take(2),
                    Is.EqualTo(new object[]
                    {
                        $"DBCC results for '{db.Name}'.",
                        "Warning: NO_INDEX option of checkdb being used. Checks on non-system indexes will be skipped."
                    }), "CheckTablesDataOnly() messages");
                Assert.That(commands,
                    Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, NOINDEX) WITH  MAXDOP = 2  " }),
                    "CheckTablesDataOnly(maxDOP:2)");
            }
            else
            {
                Assert.Throws<_SMO.FailedOperationException>(
                    () => db.CheckTablesDataOnly(RepairOptions.None, RepairStructure.None, maxDOP: 2),
                    "CheckTablesDataOnly(maxDop:2) pre-sql2016");
            }
        }

        private static void CheckTablesAllRepairTypes(_SMO.Database db)
        {
            var messages = Enumerable.Empty<string>();
            var commands = db.ExecutionManager.RecordQueryText(() =>
                 messages = db.CheckTables(RepairType.None).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>().ToList();
            Assert.That(messages, Is.Empty, "CheckTables(RepairType.None) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()})  WITH NO_INFOMSGS" }),
                "CheckTables(RepairType.None");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckTables(RepairType.Rebuild).Cast<string>().ToList(), alsoExecute: true)
            .Cast<string>().ToList();
            Assert.That(messages, Is.Empty, "CheckTables(RepairType.Rebuild) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, REPAIR_REBUILD)  WITH NO_INFOMSGS" }),
                "CheckTables(RepairType.Rebuild");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckTables(RepairType.Fast).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>().ToList();
            Assert.That(messages, Is.Empty, "CheckTables(RepairType.Fast) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, REPAIR_FAST)  WITH NO_INFOMSGS" }),
                "CheckTables(RepairType.Fast");
            commands = db.ExecutionManager.RecordQueryText(() => 
                messages = db.CheckTables(RepairType.AllowDataLoss).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>().ToList();
            Assert.That(messages, Is.Empty, "CheckTables(RepairType.AllowDataLoss) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, REPAIR_ALLOW_DATA_LOSS)  WITH NO_INFOMSGS" }),
                "CheckTables(RepairType.AllowDataLoss");
            commands = db.ExecutionManager.RecordQueryText(() => 
                messages = db.CheckTables(RepairType.AllowDataLoss, RepairStructure.DataPurity).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>().ToList();
            Assert.That(messages.Take(1), Is.EqualTo(new object[]
            {
                $"DBCC results for '{db.Name}'."
            }), "CheckTables(RepairType.AllowDataLoss, RepairStructure.DataPurity) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, REPAIR_ALLOW_DATA_LOSS)  WITH  DATA_PURITY  " }),
                "CheckTables(RepairType.AllowDataLoss, RepairStructure.DataPurity");
            commands = db.ExecutionManager.RecordQueryText(() => 
                messages = db.CheckTables(RepairType.AllowDataLoss, RepairOptions.EstimateOnly).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>().ToList();
            Assert.That(messages, Has.Member("DBCC execution completed. If DBCC printed error messages, contact your system administrator."), "CheckTables(RepairType.AllowDataLoss, RepairOptions.EstimateOnly) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()}, REPAIR_ALLOW_DATA_LOSS)  WITH  ESTIMATEONLY  " }),
                "CheckTables(RepairType.AllowDataLoss, RepairOptions.EstimateOnly");
            if (db.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || db.Parent.VersionMajor > 12)
            {
                commands = db.ExecutionManager.RecordQueryText( () => 
                    messages = db.CheckTables(RepairType.None, RepairOptions.None,
                                              RepairStructure.PhysicalOnly, maxDOP: 2).Cast<string>().ToList(), alsoExecute: true)
                    .Cast<string>().ToList();
                Assert.That(messages.Take(1), Is.EqualTo(new object[]
                    {
                        $"DBCC results for '{db.Name}'."
                    }),
                    "CheckTables(RepairType.None, RepairOptions.None, RepairStructure.PhysicalOnly, maxdop:2) messages");
                Assert.That(commands,
                    Is.EqualTo(new[]
                        {$"DBCC CHECKDB(N{db.Name.SqlSingleQuoteString()})  WITH  MAXDOP = 2 , PHYSICAL_ONLY  "}),
                    "CheckTables(RepairType.None, RepairOptions.None, RepairStructure.PhysicalOnly, maxdop:2)");
            }
            else
            {
                Assert.Throws<_SMO.FailedOperationException>(() => db.CheckTables(RepairType.None,
                    RepairOptions.None,
                    RepairStructure.PhysicalOnly, maxDOP: 2), "CheckTables throws pre-sql2016 when using maxdop");
            }

        }

        private static void CheckAllocationsAllRepairTypes(Database db)
        {
            var messages = Enumerable.Empty<string>();
            var commands = db.ExecutionManager.RecordQueryText(() => 
                messages = db.CheckAllocations(RepairType.None).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>();
            Assert.That(messages, Is.Empty, "CheckAllocations(RepairType.None) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKALLOC(N{db.Name.SqlSingleQuoteString()})  WITH NO_INFOMSGS" }),
                "CheckAllocations(RepairType.None)");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckAllocations(RepairType.Rebuild).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>();
            Assert.That(messages, Is.Empty, "CheckAllocations(RepairType.Rebuild) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKALLOC(N{db.Name.SqlSingleQuoteString()}, REPAIR_REBUILD)  WITH NO_INFOMSGS" }),
                "CheckAllocations(RepairType.Rebuild");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckAllocations(RepairType.Fast).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>();
            Assert.That(messages, Is.Empty, "CheckAllocations(RepairType.Fast) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKALLOC(N{db.Name.SqlSingleQuoteString()}, REPAIR_FAST)  WITH NO_INFOMSGS" }),
                "CheckAllocations(RepairType.Fast");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckAllocations(RepairType.AllowDataLoss).Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>();
            Assert.That(messages, Is.Empty, "CheckAllocations(RepairType.AllowDataLoss) messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKALLOC(N{db.Name.SqlSingleQuoteString()}, REPAIR_ALLOW_DATA_LOSS)  WITH NO_INFOMSGS" }),
                "CheckAllocations(RepairType.AllowDataLoss");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckAllocationsDataOnly().Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>();
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKALLOC(N{db.Name.SqlSingleQuoteString()}, NOINDEX)" }),
                "CheckAllocationsDataOnly");
            commands = db.ExecutionManager.RecordQueryText(() =>
                messages = db.CheckCatalog().Cast<string>().ToList(), alsoExecute: true)
                .Cast<string>();
            Assert.That(messages, Is.EqualTo(new[] { "DBCC execution completed. If DBCC printed error messages, contact your system administrator." }), "CheckCatalog messages");
            Assert.That(commands, Is.EqualTo(new[] { $"DBCC CHECKCATALOG({db.Name.SqlBracketQuoteString()})" }),
                "CheckCatalog");
        }


        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse)]
        public void Database_transaction_counts()
        {
            ExecuteWithDbDrop((db) =>
            {
                var tranCount = db.GetTransactionCount();
                Assert.That(tranCount, Is.Zero, "new database should have 0 transactions");
                db.ExecuteNonQuery("BEGIN TRAN");
                tranCount = db.GetTransactionCount(TransactionTypes.UnVersioned);
                Assert.That(tranCount, Is.EqualTo(1), "database should have 1 unversioned transaction after begin tran");
                tranCount = db.GetTransactionCount(TransactionTypes.Versioned);
                Assert.That(tranCount, Is.Zero, "database should have 0 versioned transactions after begin tran");
                var transactionDataTable = db.EnumTransactions(TransactionTypes.UnVersioned);
                Assert.That(transactionDataTable.Rows.Cast<DataRow>(), Is.Not.Empty, "EnumTransactions returns non-empty table after begin tran");
                db.ExecuteNonQuery("ROLLBACK TRAN");
                transactionDataTable = db.EnumTransactions();
                Assert.That(transactionDataTable.Rows.Cast<DataRow>(), Is.Empty, "EnumTransactions returns empty table after end tran");

            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDatabaseEdge)]
        public void Database_FullTextCatalogs()
        {
            ExecuteWithDbDrop((db) =>
            {
                var ftc1 = new FullTextCatalog(db, "ftc1");
                ftc1.Create();
                var ftc2 = new FullTextCatalog(db, "ftc2");
                ftc2.Create();
                Assert.Throws<FailedOperationException>(() => db.SetDefaultFullTextCatalog("nosuchcatalog"),
                    "SetDefaultFullTextCatalog should throw for a non-existent catalog name");
                Assert.DoesNotThrow(() => db.SetDefaultFullTextCatalog(ftc2.Name), "SetDefaultFullTextCatalog with valid catalog name");
                Assert.That(db.DefaultFullTextCatalog, Is.EqualTo(ftc2.Name), "SetDefaultFullTextCatalog(ftc2.Name)");
                db.RemoveFullTextCatalogs();
                db.FullTextCatalogs.Refresh();
                Assert.That(db.FullTextCatalogs.Cast<FullTextCatalog>(), Is.Empty, "RemoveFullTextCatalogs should remove all catalogs");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Database_EnumMatchingSPs_returns_correct_URNs()
        {
            ExecuteTest(() =>
            {
                var db = ServerContext.Databases["master"];
                Assert.Throws<ArgumentNullException>(() => db.EnumMatchingSPs(null), "EnumMatchingSPs(null)");
                db.StoredProcedures.ClearAndInitialize("[@IsSystemObject = true()]", new string[0]);
                var systemProc = db.StoredProcedures[0];
                var textToSearch = systemProc.TextBody.Substring(1, 20);
                var urnCollection = db.EnumMatchingSPs(textToSearch, includeSystem: true);
                Assert.That(urnCollection.Cast<Urn>(), Has.Member(systemProc.Urn), "EnumMatchSPs should find system proc");
                urnCollection = db.EnumMatchingSPs(textToSearch, includeSystem: false);
                Assert.That(urnCollection.Cast<Urn>(), Is.Empty, "EnumMatchSPs shouldn't find any non-system sproc to match");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse)]
        public void Database_EnumLocks()
        {
            ExecuteFromDbPool(db =>
            {
                var allLocks = db.EnumLocks();
                var colNames = allLocks.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                Assert.That(colNames, Is.EquivalentTo(new[] { "RequestorSpid", "LockType", "Database", "Schema", "Table", "Index", "Status", }), "Columns from EnumLocks");
                var allRows = allLocks.Rows.Cast<DataRow>().ToList();
                if (allRows.Any())
                {
                    Assert.That(allRows.Select(r => r["Database"]), Has.All.EqualTo(db.Name), "db.EnumLocks() returns locks for only the current database");
                    Trace.TraceInformation("Calling EnumLocks(processId)");
                    var spid = (int)allRows[0]["RequestorSpid"];
                    var spidLocks = db.EnumLocks(spid);
                    var spidRows = spidLocks.Rows.Cast<DataRow>().ToList();
                    Assert.That(spidRows.Select(r => r["Database"]), Has.All.EqualTo(db.Name), "db.EnumLocks(requestorSpid) returns locks for only the current database");
                    Assert.That(spidRows.Select(r => r["RequestorSpid"]), Has.All.EqualTo(spid), "EnumLocks(requestorSpid) should filter to the matching spid");
                }
                else
                {
                    Trace.TraceInformation("No locks returned by EnumLocks()");
                }

            });
        }

        /// <summary>
        /// This API is misleadingly named, it works for any file group, not just filestream
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Express)]
        public void Database_SetDefaultFileStreamFileGroup_succeeds()
        {
            ExecuteWithDbDrop(db =>
            {                
                var fileGroup = new FileGroup(db, "filegroup1", isFileStream:false);
                fileGroup.Files.Add(new DataFile(fileGroup, "datafile1", System.IO.Path.Combine(db.PrimaryFilePath, "filename1.mdf")));
                db.FileGroups.Add(fileGroup);
                db.Alter();
                Assert.That(() => db.SetDefaultFileStreamFileGroup(fileGroup.Name), Throws.Nothing, $"SetDefaultFileStreamFileGroup({fileGroup.Name})");
                db.FileGroups[fileGroup.Name].Refresh();
                Assert.That(db.FileGroups[fileGroup.Name].IsDefault, Is.True, $"{fileGroup.Name} is not the default file group SetDefaultFileStreamFileGroup");
                Assert.That(() => db.SetDefaultFileStreamFileGroup("PRIMARY"), Throws.Nothing, $"SetDefaultFileStreamFileGroup(PRIMARY)");
                db.FileGroups[fileGroup.Name].Refresh();
                Assert.That(db.FileGroups[fileGroup.Name].IsDefault, Is.False, $"{fileGroup.Name} should not be the default file group after setting it to PRIMARY SetDefaultFileStreamFileGroup");
                Assert.That(() => db.SetDefaultFileGroup(fileGroup.Name), Throws.Nothing, $"SetDefaultFileGroup({fileGroup.Name})");
                db.FileGroups[fileGroup.Name].Refresh();
                Assert.That(db.FileGroups[fileGroup.Name].IsDefault, Is.True, $"{fileGroup.Name} is not the default file group  SetDefaultFileGroup");
                Assert.That(() => db.SetDefaultFileGroup("PRIMARY"), Throws.Nothing, $"SetDefaultFileGroup(PRIMARY)");
                db.FileGroups[fileGroup.Name].Refresh();
                Assert.That(db.FileGroups[fileGroup.Name].IsDefault, Is.False, $"{fileGroup.Name} should not be the default file group after setting it to PRIMARY SetDefaultFileGroup");
                Assert.Multiple(() =>
                {
                    Assert.That(() => db.SetDefaultFileStreamFileGroup(null), Throws.ArgumentNullException, "SetDefaultFileStreamFileGroup(null)");
                    Assert.That(() => db.SetDefaultFileStreamFileGroup(""), Throws.ArgumentException, "SetDefaultFileStreamFileGroup(string.empty)");
                    Assert.That(() => db.SetDefaultFileGroup(null), Throws.ArgumentNullException, "SetDefaultFileGroup(null)");
                    Assert.That(() => db.SetDefaultFileGroup(""), Throws.ArgumentException, "SetDefaultFileGroup(string.empty)");
                });
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 11)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        public void Database_IsLocalPrimaryReplica_returns_false_appropriately()
        {
            ExecuteFromDbPool(db =>
            {
                var expected = db.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase && db.IsSupportedProperty(nameof(db.AvailabilityGroupName)) && !string.IsNullOrEmpty(db.AvailabilityGroupName);
                Assert.That(db.IsLocalPrimaryReplica, Is.EqualTo(expected), "IsLocalPrimaryReplica");                
            });
        }

        [TestMethod]
        public void Database_IsMember_returns_correct_value()
        {
            ExecuteFromDbPool(db =>
            {
                Assert.That(db.IsMember("db_owner"), Is.True, "db_owner");
                Assert.That(db.IsMember("bogus'role"), Is.False, "bogus'role");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 13)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        public void Database_PlanGuide_methods()
        {
            ExecuteFromDbPool(db =>
            {
                // make sure we have at least 1 plan guide that we can break
                var table = db.CreateTable("pg");
                var index = table.CreateIndex("pg");
                var statement = $"SELECT * from {table.Name.SqlBracketQuoteString()}";
                var planGuide = new PlanGuide(db, "planGuideTest")
                {
                    Statement = statement,
                    ScopeType = PlanGuideType.Sql,
                    ScopeBatch = statement,
                    Hints = $"OPTION(TABLE HINT ({table.Name.SqlBracketQuoteString()}, INDEX({index.Name.SqlBracketQuoteString()})))"
                };
                planGuide.Create();
                try
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(db.ValidateAllPlanGuides(), Is.True, "ValidateAllPlanGuides()");
                        index.Drop();
                        Assert.That(db.ValidateAllPlanGuides(out DataTable dataTable), Is.False, "ValidateAllPlanGuides(out dataTable) after dropping index");
                        var errorList = dataTable.Rows.Cast<DataRow>().Select(row => row.Field<string>("name")).ToList();
                        Assert.That(errorList, Is.EquivalentTo(new[] { planGuide.Name }), "ValidateAllPlanGuides should have a failure for the missing index");
                        db.DisableAllPlanGuides();
                        Assert.That(db.PlanGuides.Cast<PlanGuide>().Select(pg => pg.IsDisabled), Has.All.True, "DisableAllPlanGuides");
                        db.EnableAllPlanGuides();
                        db.PlanGuides.Refresh();
                        // Since EnableAllPlanGuides doesn't Alter PlanGuide objects, SMO uses cached data for individual objects that we've already referenced.
                        // So we have to Refresh the individual collection members in addition to the collection itself
                        Assert.That(db.PlanGuides.Cast<PlanGuide>().Select(pg => { pg.Refresh(); return pg.IsDisabled; }), Has.All.False, "EnableAllPlanGuides");
                        db.DropAllPlanGuides();
                        db.PlanGuides.Refresh();
                        Assert.That(db.PlanGuides.Cast<PlanGuide>().Select(pg => pg.Name), Is.Empty, "DropAllPlanGuides");
                    });
                }
                finally
                {
                    planGuide.DropIfExists();
                    index.DropIfExists();
                    table.DropIfExists();
                }
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 13)]
        public void Database_CreateForAttach_attaches_database()
        {
            ExecuteWithDbDrop(db =>
            {
                var dbName = db.Name;
                var server = db.Parent;
                var primaryFile = db.FileGroups[db.DefaultFileGroup].Files[0];
                var primaryName = primaryFile.Name;
                var primaryFileName = primaryFile.FileName;
                server.DetachDatabase(db.Name, updateStatistics: false);
                server.Databases.Refresh();
                Assert.That(server.Databases.Cast<Database>().Select(d => d.Name), Has.No.Member(dbName), "DetachDatabase didn't remove the database");
                var databaseAttach = new Database(server, dbName);
                databaseAttach.FileGroups.Add(new FileGroup(databaseAttach, "PRIMARY"));
                databaseAttach.FileGroups["PRIMARY"].Files.Add(new DataFile(databaseAttach.FileGroups["PRIMARY"], primaryName, primaryFileName));
                Assert.That(() => databaseAttach.Create(forAttach: true), Throws.Nothing, "Create(forAttach:true)");
                server.Databases.Refresh();
                Assert.That(server.Databases.Cast<Database>().Select(d => d.Name), Has.Member(dbName), "Create didn't attach the database");
                // with the same name the files should be the same as before
                databaseAttach.FileGroups.Refresh();
                var newPrimaryFile = databaseAttach.FileGroups[databaseAttach.DefaultFileGroup].Files[0];
                Assert.That(newPrimaryFile.Name, Is.EqualTo(primaryName), "primary file name");
                Assert.That(newPrimaryFile.FileName, Is.EqualTo(primaryFileName), "primary file filename");
            });
        }

        /// <summary>
        /// Method to test Creating a new Azure DB when specifying MaxSizeInBytes.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void Script_HyperScale_With_MaxSizeInBytes()
        {
            this.ExecuteTest(
                 server =>
                 {
                     //Adding parameters to create Hyperscale db
                     Database db = new Database(server, "HyperscaleDB1", DatabaseEngineEdition.SqlDatabase)
                     {
                         AzureEdition = SqlTestBase.AzureDatabaseEdition.Hyperscale.ToString(),
                         AzureServiceObjective = "HS_Gen5_4",
                         MaxSizeInBytes = 1073741824,
                         ReadOnly = false
                     };
                     var commands = server.ExecutionManager.RecordQueryText(db.Create).Cast<string>();
                     string expected = $"CREATE DATABASE [HyperscaleDB1]  (EDITION = 'Hyperscale', SERVICE_OBJECTIVE = 'HS_Gen5_4', MAXSIZE = 1 GB);{Environment.NewLine}";
                     Assert.That(commands, Has.Exactly(1).AtLeast(expected), "Invalid Query to Create Hyperscale database");
                 });
        }

        /// <summary>
        /// Method to test Creating a new Azure DB when NOT specifying MaxSizeInBytes.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void Script_HyperScale_Without_MaxSizeInBytes()
        {
            this.ExecuteTest(
                 server =>
                 {
                     try
                     {
                         //Adding parameters to create Hyperscale db
                         Database db = new Database(server, "HyperscaleDB1", DatabaseEngineEdition.SqlDatabase)
                         {
                             AzureEdition = SqlTestBase.AzureDatabaseEdition.Hyperscale.ToString(),
                             AzureServiceObjective = "HS_Gen5_4",
                             ReadOnly = false
                         };
                         server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                         db.Create();
                         var commands = server.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                         string expected = $"CREATE DATABASE [HyperscaleDB1]  (EDITION = 'Hyperscale', SERVICE_OBJECTIVE = 'HS_Gen5_4');{Environment.NewLine}";
                         Assert.That(commands, Has.Exactly(1).EqualTo(expected), "Invalid Query to Create Hyperscale database");
                     }
                     finally
                     {
                         server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                     }
                 });
        }

        /// <summary>
        /// Method to test scripting an existing Azure Hyperscale db.  
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void Script_HyperScale_DataBase()
        {
            string backupFile = null;
            var script = new System.Collections.Specialized.StringCollection();
            ExecuteWithDbDrop("HyperscaleDB", AzureDatabaseEdition.Hyperscale, backupFile, db =>
            {
                script = db.Script();
                var commands = script;
                string expected = $"CREATE DATABASE {db.FullQualifiedName} COLLATE SQL_Latin1_General_CP1_CS_AS  (EDITION = 'Hyperscale', SERVICE_OBJECTIVE = 'HS_Gen5_2') WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF;{Environment.NewLine}";
                Assert.That(commands, Has.Exactly(1).EqualTo(expected), "Invalid Query to Create Hyperscale database");
            });

        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        public void Database_EnumGroups_returns_windows_group_users()
        {
            ExecuteFromDbPool(db =>
            {
                var serverLogins = db.Parent.Logins.Cast<Login>().Where(l => l.LoginType == LoginType.WindowsGroup).ToList();
                if (serverLogins.Count == 0)
                {
                    Trace.TraceWarning($"Server {db.Parent.Name} has no windows group logins!");
                    return;
                }
                foreach (var serverLogin in serverLogins)
                {
                    db.CreateUser(serverLogin.Name, serverLogin.Name);
                }
                var dataTable = db.EnumWindowsGroups(groupName: null);
                var actualGroups = dataTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("Name"));
                var expectedGroups = serverLogins.Select(l => l.Name);
                var actualMappings = db.EnumLoginMappings().Rows.Cast<DataRow>().Select(r => r.Field<string>("UserName"));
                Assert.Multiple(() =>
                {
                    // note that no other test in this class can add group users!
                    Assert.That(actualGroups, Is.EquivalentTo(expectedGroups), "Database.EnumWindowsGroups(null)");
                    dataTable = db.EnumWindowsGroups(serverLogins[0].Name);
                    actualGroups = dataTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("Name"));
                    Assert.That(actualGroups, Is.EquivalentTo(new[] { serverLogins[0].Name }), $"Database.EnumWindowsGroups({serverLogins[0].Name})");
                    Assert.That(actualMappings, Is.EquivalentTo(expectedGroups.Union(new[] { "dbo" })), "EnumLoginMappings");
                });
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        public void Database_Alter_toggles_accelerated_recovery()
        {
            ExecuteFromDbPool(db =>
            {
                db.AcceleratedRecoveryEnabled = true;
                db.Alter();
                db.Refresh();
                var table = db.CreateTable("adr");
                table.InsertDataToTable(200);
                Assert.Multiple(() =>
                {
                    Assert.That(db.AcceleratedRecoveryEnabled, Is.True, "AcceleratedRecoveryEnabled set to true by Alter");
                    Assert.That(db.PersistentVersionStoreFileGroup, Is.EqualTo("PRIMARY"), "Default persistent store file group");
                    Assert.That(db.PersistentVersionStoreSizeKB, Is.GreaterThan(0), "PersistentVersionStoreSizeKB");
                });
                db.AcceleratedRecoveryEnabled = false;
                db.Alter();
                db.Refresh();
                Assert.That(db.AcceleratedRecoveryEnabled, Is.False, "AcceleratedRecoveryEnabled set to false by Alter");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 12)]
        public void Database_ChangeMirroringState_creates_correct_script()
        {
            ExecuteFromDbPool(db =>
            {
                var script = db.ExecutionManager.RecordQueryText(() => db.ChangeMirroringState(MirroringOption.Off)).ToSingleString();
                Assert.That(script, Is.EqualTo($"ALTER DATABASE {db.Name.SqlBracketQuoteString()} SET PARTNER OFF{Environment.NewLine}"));
                script = db.ExecutionManager.RecordQueryText(() => db.ChangeMirroringState(MirroringOption.Suspend)).ToSingleString();
                Assert.That(script, Is.EqualTo($"ALTER DATABASE {db.Name.SqlBracketQuoteString()} SET PARTNER SUSPEND{Environment.NewLine}"));
                script = db.ExecutionManager.RecordQueryText(() => db.ChangeMirroringState(MirroringOption.Resume)).ToSingleString();
                Assert.That(script, Is.EqualTo($"ALTER DATABASE {db.Name.SqlBracketQuoteString()} SET PARTNER RESUME{Environment.NewLine}"));
                script = db.ExecutionManager.RecordQueryText(() => db.ChangeMirroringState(MirroringOption.RemoveWitness)).ToSingleString();
                Assert.That(script, Is.EqualTo($"ALTER DATABASE {db.Name.SqlBracketQuoteString()} SET WITNESS OFF{Environment.NewLine}"));
                script = db.ExecutionManager.RecordQueryText(() => db.ChangeMirroringState(MirroringOption.Failover)).ToSingleString();
                Assert.That(script, Is.EqualTo($"USE [master];ALTER DATABASE {db.Name.SqlBracketQuoteString()} SET PARTNER FAILOVER{Environment.NewLine}"));
                script = db.ExecutionManager.RecordQueryText(() => db.ChangeMirroringState(MirroringOption.ForceFailoverAndAllowDataLoss)).ToSingleString();
                Assert.That(script, Is.EqualTo($"ALTER DATABASE {db.Name.SqlBracketQuoteString()} SET PARTNER FORCE_SERVICE_ALLOW_DATA_LOSS {Environment.NewLine}"));
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 12)]
        public void Database_SetSnapshotIsolation_toggles_snapshot_isolation()
        {
            ExecuteFromDbPool(db =>
            {
                var snapshotIsolation = db.SnapshotIsolationState;
                var enableSnapshot = snapshotIsolation == SnapshotIsolationState.Disabled;
                db.SetSnapshotIsolation(enableSnapshot);
                try
                {
                    db.Refresh();
                    if (enableSnapshot)
                    {
                        Assert.That(db.SnapshotIsolationState, Is.AnyOf(SnapshotIsolationState.Enabled, SnapshotIsolationState.PendingOn), $"SetSnapshotIsolation({enableSnapshot})");
                    }
                    else
                    {
                        Assert.That(db.SnapshotIsolationState, Is.AnyOf(SnapshotIsolationState.Disabled, SnapshotIsolationState.PendingOff), $"SetSnapshotIsolation({enableSnapshot})");
                    }
                }
                finally
                {
                    db.SetSnapshotIsolation(!enableSnapshot);
                }
            });
        }

        /// <summary>
        /// Regression test for https://github.com/microsoft/sqlmanagementobjects/issues/32
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void Database_Checkpoint_checkpoints_the_correct_database()
        {
            ExecuteFromDbPool(db =>
            {
                var expected = db.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase ? new[] { "CHECKPOINT" } : new[] { $"USE {db.Name.SqlBracketQuoteString()}", "CHECKPOINT" };
                // There are 2 ways we could verify on the server that the checkpoint runs against the right database but both are unwieldy and subject to external race conditions:
                // 1. Set trace flag 3502 on and look for error log entries
                // 2. Create an XEvent session to monitor for checkpoint_starting events on the current database
                // We'll keep it simple and just look for the USING statement in the outgoing query
                var queries = db.ExecutionManager.RecordQueryText(db.Checkpoint, alsoExecute: true).Cast<string>().ToArray();
                Assert.That(queries, Is.EqualTo(expected), "Checkpoint TSQL");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Database_Size_succeeds_with_CaptureSql_on()
        {
            ExecuteFromDbPool(db =>
            {
                var expectedSize = db.Size;
                db.Refresh();
                db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                try
                {
                    var actualSize = db.Size;
                    Assert.Multiple(() =>
                   {
                       Assert.That(actualSize, Is.EqualTo(expectedSize), "Size with CaptureSql on should match it with CaptureSql off");
                       Assert.That(db.ExecutionManager.ConnectionContext.SqlExecutionModes, Is.EqualTo(SqlExecutionModes.CaptureSql), "Capture mode should be preserved");
                   });
                }
                finally
                {
                    db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                }
            });
        }

#if MICROSOFTDATA
        [TestMethod]
        [SupportedServerVersionRange(Edition=DatabaseEngineEdition.SqlDatabase)]
        public void Database_enumerating_databases_does_not_login_to_each_database()
        {
            ExecuteTest(() =>
            {
                // Get the server's DatabaseEngineType first so that query doesn't get made during the collection enumeration
                Manageability.Utils.Helpers.TraceHelper.TraceInformation($"Main connection engine type: {ServerContext.DatabaseEngineType}");
                using (var eventRecorder = new SqlClientEventRecorder(Environment.CurrentManagedThreadId))
                {
                    eventRecorder.Start();
                    ServerContext.Databases.ClearAndInitialize("[@IsSystemObject = false()]", new[] {nameof(Database.Status)});
                    eventRecorder.Stop();
                    var messages = eventRecorder.Events.SelectMany(e => e.Payload).Select(p => p.ToString());
                    Assert.That(messages, Has.None.Contains("sc.TdsParser.SendPreLoginHandshake"), "No logins should have occurred - Status only");
                    Assert.That(messages, Has.None.Contains("SERVERPROPERTY('EngineEdition') AS DatabaseEngineEdition,"), "No query for DatabaseEngineEdition should have been made - Status only");
                }
                using (var eventRecorder = new SqlClientEventRecorder(Environment.CurrentManagedThreadId))
                {
                    eventRecorder.Start();
                    ServerContext.Databases.ClearAndInitialize(string.Empty, Enumerable.Empty<string>());
                    eventRecorder.Stop();
                    var messages = eventRecorder.Events.SelectMany(e => e.Payload).Select(p => p.ToString());
                    Assert.That(messages, Has.None.Contains("sc.TdsParser.SendPreLoginHandshake"), "No logins should have occurred - No properties");
                    Assert.That(messages, Has.None.Contains("SERVERPROPERTY('EngineEdition') AS DatabaseEngineEdition,"), "No query for DatabaseEngineEdition should have been made - No properties");
                    Assert.That(ServerContext.Databases.Cast<Database>().Select( d=> d.Name), Has.Member("master"), "Should have at least master");
                }
                // Querying for Status and other properties goes through a different path. Don't use any PostProcess properties for the test, as they require a login to the user db.
                using (var eventRecorder = new SqlClientEventRecorder(Environment.CurrentManagedThreadId))
                {
                    eventRecorder.Start();
                    ServerContext.Databases.ClearAndInitialize("[@IsSystemObject = false()]", new[] { nameof(Database.Status), nameof(Database.ChangeTrackingEnabled) });
                    eventRecorder.Stop();
                    var messages = eventRecorder.Events.SelectMany(e => e.Payload).Select(p => p.ToString());
                    Assert.That(messages, Has.None.Contains("sc.TdsParser.SendPreLoginHandshake"), "No logins should have occurred - Status and other properties");
                    Assert.That(messages, Has.None.Contains("SERVERPROPERTY('EngineEdition') AS DatabaseEngineEdition,"), "No query for DatabaseEngineEdition should have been made - Status and other properties");
                }
            });
        }

#endif
    }

}
