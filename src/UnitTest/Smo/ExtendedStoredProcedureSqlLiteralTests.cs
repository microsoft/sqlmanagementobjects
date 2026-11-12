// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests literal encoding of extended stored procedure arguments.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class ExtendedStoredProcedureSqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow(@"C:\Extensions\Test.dll", @"C:\Extensions\Test.dll")]
        [DataRow(@"C:\O'Brien\Test.dll", @"C:\O''Brien\Test.dll")]
        [DataRow("C:\\\u540d'Folder]\\Test.dll", "C:\\\u540d''Folder]\\Test.dll")]
        public void ExtendedStoredProcedure_ScriptCreate_EscapesDllLocation(string path, string expectedPath)
        {
            var database = new Database(ServerTests.GetDesignModeServer(17), "TestDb");
            var procedure = new ExtendedStoredProcedure(database, "Test'Procedure]", "dbo") { DllLocation = path };
            var scripts = new StringCollection();

            procedure.ScriptCreate(scripts, database.GetScriptingPreferencesForCreate());

            Assert.That(scripts, Is.EqualTo(new[]
                {
                    $"EXEC dbo.sp_addextendedproc N'[dbo].[Test''Procedure]]]', N'{expectedPath}'{System.Environment.NewLine}"
                }), "Procedure names and DLL locations must be escaped independently as SQL literals.");
            Assert.That(procedure.DllLocation, Is.EqualTo(path), "Scripting must not mutate the DLL location.");
        }

        [TestMethod]
        public void ExtendedStoredProcedure_ScriptCreate_PreservesAzureGuard()
        {
            var server = ServerTests.GetDesignModeServer(12,
                DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase);
            server.ConnectionContext.ServerInstance = "unit-test-server.invalid";
            server.ConnectionContext.ConnectTimeout = 1;
            var database = new Database(server, "TestDb", DatabaseEngineEdition.SqlDatabase);
            var procedure = new ExtendedStoredProcedure(database, "TestProcedure", "dbo") { DllLocation = "Test.dll" };
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.ScriptForCreateDrop = true;

            Assert.Throws<UnsupportedEngineTypeException>(
                () => procedure.ScriptCreate(new StringCollection(), preferences),
                "Extended stored procedure scripting must remain unsupported for Azure SQL Database.");
        }
    }
}
