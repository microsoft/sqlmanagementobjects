// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Linq;
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
        /// We're hard-coding max memory usage per core per hardware generation for Managed Servers
        /// in information.xml and inc_server.xml files. Since these numbers can change in production
        /// and new hardware generations will show up, this test will fail if things are to be updated.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void CheckMemoryLimitConstantsForManagedInstance()
        {
            this.ExecuteWithMasterDb(
               database =>
               {
                    const int memoryPerGbGen4 = 7168;
                    const int memoryPerGbGen5 = 5223;

                   _SMO.Server server = database.Parent;

                   // Check if this server belongs to known hardware generations
                   //
                   Assert.That(server.HardwareGeneration, Is.EqualTo("Gen4").Or.EqualTo("Gen5"), "Unknown hardware generation - update information.xml and inc_server.xml!");

                   // Check if memory per core ratio has changed
                   //
                   System.Data.DataSet ds = database.ExecuteWithResults("SELECT [process_memory_limit_mb] FROM sys.dm_os_job_object");
                   int actualServerMemory = System.Int32.Parse(ds.Tables[0].Rows[0][0].ToString());
                   int actualServerMemoryPerCore = actualServerMemory / server.Processors;

                   switch(server.HardwareGeneration)
                   {
                       case "Gen4":
                           Assert.That(memoryPerGbGen4, Is.EqualTo(actualServerMemoryPerCore), "Memory per core has changed for Gen4 - update information.xml and inc_server.xml!");
                           break;
                       case "Gen5":
                           Assert.That(memoryPerGbGen5, Is.EqualTo(actualServerMemoryPerCore), "Memory per core has changed for Gen5 - update information.xml and inc_server.xml!");
                           break;
                       default:
                           Assert.Fail("Unknown hardware generation - update information.xml and inc_server.xml!");
                           break;
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

        #endregion Server Functionality Tests
            
        #region Helpers

        #endregion Helpers
    }
}
