// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Linq;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;
using TraceHelper = Microsoft.SqlServer.Test.Manageability.Utils.Helpers.TraceHelper;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing SMO Server object functionality
    /// </summary>
    [TestClass]
    public class Server_SmoTestSuite : SqlTestBase
    {

        #region Server Functionality Tests


        /// <summary>
        /// Tests that Server.CompareUrn works correctly with both case-sensitive and case-insensitive collation. 
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        [SqlTestArea(SqlTestArea.SMO)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlManagedInstance)]
        public void Server_CompareUrnWorksCorrectly_WithDifferentCollations()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    var server = database.Parent;
                    var table = DatabaseObjectHelpers.CreateTable(database, "tbl");
                    Urn modifiedUrn = new Urn(table.Urn.ToString().Replace(SmoObjectHelpers.SqlEscapeSingleQuote(table.Name), SmoObjectHelpers.SqlEscapeSingleQuote(table.Name.ToUpper())));

                    //Case-Sensitive Collation Compare
                    TraceHelper.TraceInformation("Setting collation of DB to SQL_Latin1_General_CP1_CS_AS");
                    database.Collation = "SQL_Latin1_General_CP1_CS_AS";
                    database.Alter();
                    Assert.That(server.CompareUrn(table.Urn, modifiedUrn), Is.Not.EqualTo(0),
                        "URN comparison failed: Both Should not be equal when a case-insensitive collation is used.\nOriginal URN '{0}'\nModified URN '{1}",
                        table.Urn,
                        modifiedUrn);

                    //Case-Insensitive Collation Compare
                    TraceHelper.TraceInformation("Setting collation of DB to SQL_Latin1_General_CP1_CI_AS");
                    database.Collation = "SQL_Latin1_General_CP1_CI_AS";
                    database.Alter();
                    Assert.That(server.CompareUrn(table.Urn, modifiedUrn), Is.EqualTo(0),
                        "URN comparison failed: Both Should be equal when a case-insensitive collation is used.\nOriginal URN '{0}'\nModified URN '{1}",
                        table.Urn,
                        modifiedUrn);

                });
        }

        /// <summary>
        /// We can only verify scripting of server-level registry-based properties using capture
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11)]
        public void Server_alter_scripts_registry_properties_sorted_by_name()
        {
            var expectedScripts = new string[]
            {
                @"USE [master]",
                @"EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'AuditLevel', REG_DWORD, 3", 
                @"EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'DefaultData', REG_SZ, N'C:\DefaultFile'", 
                @"EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'NumErrorLogs', REG_DWORD, 100", 
                @"EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'ErrorLogSizeInKb', REG_DWORD, 5555"
            };
            ExecuteTest(() =>
            {
                Assert.DoesNotThrow(() => { int x = ServerContext.ErrorLogSizeKb; });
                this.ServerContext.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                this.ServerContext.NumberOfLogFiles = 100;
                // Server and Server.Settings both contribute to the script. Settings class is required to have the same 
                // registry properties as Server.
                this.ServerContext.Settings.ErrorLogSizeKb = 5555;
                this.ServerContext.DefaultFile = @"C:\DefaultFile";
                this.ServerContext.AuditLevel = _SMO.AuditLevel.All;
                this.ServerContext.Alter();
                StringCollection query = this.ServerContext.ConnectionContext.CapturedSql.Text;
                Assert.That(query.Cast<string>(), Is.EquivalentTo(expectedScripts), "Registry properties script");
            });
        }

        /// <summary>
        /// Make sure that Managed Instance's master db and log paths are not empty
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void Server_master_file_paths_not_empty_managed_instance()
        {
            this.ExecuteWithDbDrop(
               database =>
               {
                   _SMO.Server server = database.Parent;

                   Assert.IsFalse(string.IsNullOrEmpty(server.MasterDBPath), "MasterDB data path must not be empty!");
                   Assert.IsFalse(string.IsNullOrEmpty(server.MasterDBLogPath), "MasterDB log path must not be empty!");
               }
           );
        }

        /// <summary>
        /// Validating new Server properties specific for Managed Instances:
        /// HardwareGeneration, ServiceTier, ReservedStorageSizeMB, UsedStorageSizeMB
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void ServerPropertiesV15()
        {
            this.ExecuteWithMasterDb(
               database =>
               {
                   _SMO.Server server = database.Parent;

                   string hardwareGen, serviceTier;
                   int reservedStorage, usedStorage;

                   hardwareGen = server.HardwareGeneration;
                   serviceTier = server.ServiceTier;
                   reservedStorage = server.ReservedStorageSizeMB;
                   usedStorage = server.UsedStorageSizeMB;

                   if (server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                   {
                       Assert.That(hardwareGen, Is.Not.Empty, "HardwareGeneration has unexpected value NULL.");
                       Assert.That(serviceTier, Is.Not.Empty, "ServiceTier has unexpected value NULL.");
                       Assert.That(reservedStorage, Is.GreaterThan(0), "ReservedStorageSizeMB not greater than 0.");
                       Assert.That(usedStorage, Is.GreaterThanOrEqualTo(0), "UsedStorageSizeMB not greater or equal than 0.");
                   }
                   else
                   {
                       Assert.That(hardwareGen, Is.Empty, "'HardwareGeneration' property should be empty for Box edition.");
                       Assert.That(serviceTier, Is.Empty, "'ServiceTier' property should be empty for Box edition.");
                       Assert.That(reservedStorage, Is.EqualTo(0), "'ReservedStorageSizeMB' not 0 as expected.");
                       Assert.That(usedStorage, Is.EqualTo(0), "'UsedStorageSizeMB' not 0 as expected.");
                   }
               }
           );
        }

        /// <summary>
        /// Make sure that master db and log paths are not empty for SQL Standalone
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11)]
        public void Server_master_file_paths_not_empty()
        {
            this.ExecuteWithDbDrop(
               database =>
               {
                   _SMO.Server server = database.Parent;

                   Assert.That(server.MasterDBPath, Is.Not.Empty, "MasterDB data path must not be empty!");
                   Assert.That(server.MasterDBLogPath, Is.Not.Empty, "MasterDB log path must not be empty!");
               }
            );
        }

        /// <summary>
        /// RootDirectory is marked 'expensive' so it is excluded from the bulk fetch of Server.Information properties.
        /// This ensures that a user who lacks permission to execute xp_instance_regread (which RootDirectory relies on)
        /// can still read the other Information properties without the property fetch throwing.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, HostPlatform = "Windows")]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void Server_Information_properties_readable_without_xp_instance_regread_permission()
        {
            ExecuteTest(() =>
            {
                var loginName = "regread_login_" + Guid.NewGuid().ToString("N");
                var password = SqlTestRandom.GeneratePassword();
                ServerContext.CreateLogin(loginName, _SMO.LoginType.SqlLogin, password);
                var master = ServerContext.Databases["master"];
                try
                {
                    // Grant enough to read the SERVERPROPERTY/DMV-based Information properties, but deny the registry
                    // read used by RootDirectory so we can prove the other properties don't depend on it.
                    ServerContext.ConnectionContext.ExecuteNonQuery($"GRANT VIEW SERVER STATE TO [{loginName}]");
                    master.ExecutionManager.ExecuteNonQuery($"CREATE USER [{loginName}] FOR LOGIN [{loginName}]");
                    master.ExecutionManager.ExecuteNonQuery($"DENY EXECUTE ON sys.xp_instance_regread TO [{loginName}]");

                    var connectionString = new SqlConnectionStringBuilder(this.SqlConnectionStringBuilder.ConnectionString)
                    {
                        UserID = loginName,
                        Password = password,
                        IntegratedSecurity = false,
                        InitialCatalog = "master",
                        Pooling = false
                    };
                    var restrictedConnection = new ServerConnection(new SqlConnection(connectionString.ConnectionString));
                    try
                    {
                        var restrictedServer = new _SMO.Server(restrictedConnection);

                        // Reading the non-expensive Information properties must not trigger xp_instance_regread, so it must not throw.
                        Assert.DoesNotThrow(() =>
                        {
                            var info = restrictedServer.Information;
                            TraceHelper.TraceInformation(
                                "Edition='{0}' VersionString='{1}' Collation='{2}' EngineEdition='{3}' IsCaseSensitive='{4}'",
                                info.Edition, info.VersionString, info.Collation, info.EngineEdition, info.IsCaseSensitive);
                        }, "A user without xp_instance_regread permission should be able to read Information properties that do not depend on the registry.");

                        // RootDirectory still relies on xp_instance_regread, which is denied for this user, so requesting it explicitly should throw.
                        Assert.Throws<ExecutionFailureException>(
                            () => { var _ = restrictedServer.Information.RootDirectory; },
                            "RootDirectory relies on xp_instance_regread, which is denied for this user, so it should throw when requested.");
                    }
                    finally
                    {
                        restrictedConnection.Disconnect();
                    }
                }
                finally
                {
                    master.ExecutionManager.ExecuteNonQuery($"IF DATABASE_PRINCIPAL_ID(N'{loginName}') IS NOT NULL DROP USER [{loginName}]");
                    // DROP LOGIN IF EXISTS is not supported on SQL Server 2014, so use a conditional drop that works on all versions.
                    ServerContext.ConnectionContext.ExecuteNonQuery($"IF SUSER_ID(N'{loginName}') IS NOT NULL DROP LOGIN [{loginName}]");
                }
            });
        }

        #endregion Server Functionality Tests
            
        #region Helpers

        #endregion Helpers
    }
}
