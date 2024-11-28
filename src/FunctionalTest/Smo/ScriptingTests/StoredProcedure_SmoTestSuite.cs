// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Diagnostics;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Stored Procedure properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class StoredProcedure_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.StoredProcedure proc = (_SMO.StoredProcedure)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.StoredProcedures.Refresh();
            Assert.IsNull(database.StoredProcedures[proc.Name],
                          "Current stored procedure not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a stored procedure with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Proc_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.StoredProcedure proc = new _SMO.StoredProcedure(database, GenerateSmoObjectName("proc"));

                    proc.TextHeader = String.Format("CREATE PROCEDURE {0} AS", SmoObjectHelpers.SqlBracketQuoteString(proc.Name));
                    proc.TextBody = "BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";

                    string procScriptDropIfExists = string.Format("DROP PROCEDURE IF EXISTS {0}.{1}", 
                        SmoObjectHelpers.SqlBracketQuoteString(proc.Schema), 
                        SmoObjectHelpers.SqlBracketQuoteString(proc.Name));

                    VerifySmoObjectDropIfExists(proc, database, procScriptDropIfExists);
                });
        }


        /// <summary>
        /// Regression test for bug 9868009
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void When_Edition_is_DW_Sproc_can_ScriptAlter()
        {
            ExecuteWithDbDrop("SprocScriptAlterDw", AzureDatabaseEdition.DataWarehouse, null, VerifyScriptAlter);
        }
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]

        public void Verify_ScriptAlter_for_sproc()
        {
            this.ExecuteWithDbDrop(VerifyScriptAlter);
        }

        private void VerifyScriptAlter(_SMO.Database database)
        {
            var name = GenerateSmoObjectName("procScriptAlter");
            _SMO.StoredProcedure proc = new _SMO.StoredProcedure(database, name);

            proc.TextHeader = String.Format("CREATE PROCEDURE {0} AS", SmoObjectHelpers.SqlBracketQuoteString(proc.Name));
            proc.TextBody =
                "BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            proc.Create();
            var script = new StringCollection();
            var prefs = new _SMO.ScriptingPreferences
            {
                Behavior = _SMO.ScriptBehavior.Create,
                ScriptForAlter = true
            };
            // These preferences emulate the defaults in SSMS 
            prefs.IncludeScripts.Header = true;
            prefs.IncludeScripts.DatabaseContext = true;
            prefs.IncludeScripts.SchemaQualify = true;
            prefs.TargetDatabaseEngineEdition = database.DatabaseEngineEdition;
            prefs.TargetDatabaseEngineType = database.DatabaseEngineType;
            prefs.TargetServerVersion = _SMO.SqlServerVersion.Version130;
            prefs.ForDirectExecution = false;
            prefs.OldOptions.DdlBodyOnly = false;
            prefs.OldOptions.DdlHeaderOnly = false;
            prefs.OldOptions.EnforceScriptingPreferences = true;
            prefs.OldOptions.PrimaryObject = true;
            prefs.DataType.UserDefinedDataTypesToBaseType = false;
            prefs.DataType.XmlNamespaces = true;
            proc.Touch();
            proc.ScriptAlter(script, prefs);
            // the constraints don't like carriage returns/line feeds. SMO seems to use both \r\n and \n 
            var actualScript = script.ToSingleString().Trim().Replace("\r\n", " ").Replace("\n", " ");
            // the header has the current timestamp, so just skip most of it
            Assert.That(actualScript,
                Does.EndWith(
                    String.Format(@"SET ANSI_NULLS ON SET QUOTED_IDENTIFIER ON ALTER PROCEDURE [dbo].{0} AS BEGIN SELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE' END", 
                    SmoObjectHelpers.SqlBracketQuoteString(name))),
                "Wrong ALTER script for stored procedure");
        }

        /// <summary>
        /// Tests create or alter a stored procedure through SMO on SQL16 and later.
        /// 1. Create the procedure, verify ScriptCreateOrAlter text and verify the object was created correctly
        /// 2. Alter the procedure, verify ScriptCreateOrAlter text and verify the object was updated correctly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoCreateOrAlter_Proc_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    // 1. Create the procedure, verify ScriptCreateOrAlter and check existence

                    _SMO.StoredProcedure proc = DatabaseObjectHelpers.CreateSPDefinition(
                                                    database, 
                                                    "sp",
                                                    schema: "dbo",
                                                    textBody: "BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND");
                    proc.TextHeader = string.Format("CREATE OR ALTER PROCEDURE {0}.{1} AS",
                                                    SmoObjectHelpers.SqlBracketQuoteString(proc.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(proc.Name));

                    VerifySmoObjectCreateOrAlterForCreate(
                        database,
                        proc,
                        string.Format(@"CREATE OR ALTER PROCEDURE {0} AS BEGIN SELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE' END", proc.FullQualifiedName));

                    // 2. Alter the procedure, verify ScriptCreateOrAlter and check existence

                    proc.TextHeader = string.Format("CREATE OR ALTER PROCEDURE {0}.{1} AS",
                                                    SmoObjectHelpers.SqlBracketQuoteString(proc.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(proc.Name));
                    proc.TextBody = "BEGIN\nSELECT * FROM sys.tables WHERE type_desc LIKE 'USER_TABLE'\nEND";

                    VerifySmoObjectCreateOrAlterForAlter(
                        database,
                        proc,
                        string.Format(@"SET ANSI_NULLS ON SET QUOTED_IDENTIFIER ON CREATE OR ALTER   PROCEDURE {0} AS BEGIN SELECT * FROM sys.tables WHERE type_desc LIKE 'USER_TABLE' END", proc.FullQualifiedName));
                });
        }

        /// <summary>
        /// Tests CreateOrAlter() is not supported for stored procedure through SMO on SQL14 and before.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MaxMajor = 12)]
        public void SmoCreateOrAlter_Proc_Sql14AndBeforeOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.StoredProcedure proc = DatabaseObjectHelpers.CreateSPDefinition(
                                                    database, 
                                                    "sp",
                                                    schema: "dbo",
                                                    textBody: "BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND");
                    proc.TextHeader = string.Format("CREATE OR ALTER PROCEDURE {0}.{1} AS",
                                                    SmoObjectHelpers.SqlBracketQuoteString(proc.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(proc.Name));

                    _SMO.FailedOperationException e = Assert.Throws<_SMO.FailedOperationException>(
                        () => proc.CreateOrAlter(),
                        string.Format(
                            "Expected FailedOperationException with message containing \"CreateOrAlter failed for StoredProcedure '{0}.{1}'.\" when calling CreateOrAlter against unsupported downlevel servers, but no such exception was thrown",
                            proc.Schema,
                            proc.Name));
                    Assert.That(e.Message, Does.Contain(string.Format("CreateOrAlter failed for StoredProcedure '{0}.{1}'.", proc.Schema, proc.Name)), "Unexpected error message.");
                });
        }

        /// <summary>
        /// Regression test for TFS 11825817
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void When_user_has_no_access_to_master_sproc_can_be_scripted()
        {
            ExecuteWithDbDrop(
                db =>
                {

                    var name = GenerateSmoObjectName("procNoMaster");
                    _SMO.StoredProcedure proc = new _SMO.StoredProcedure(db, name);

                    proc.TextHeader = String.Format("CREATE PROCEDURE {0} AS", SmoObjectHelpers.SqlBracketQuoteString(proc.Name));
                    proc.TextBody =
                        "BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
                    proc.Create();
                    var pwd = SqlTestRandom.GeneratePassword();
                    _SMO.Login login = null;
                    try
                    {
                        login = db.Parent.CreateLogin(GenerateUniqueSmoObjectName("login"), _SMO.LoginType.SqlLogin, pwd);
                        var user = db.CreateUser(login.Name, login.Name);

                        db.ExecutionManager.ExecuteNonQuery(
                            string.Format("grant view definition on database::{0} TO {1}",
                                SmoObjectHelpers.SqlBracketQuoteString(db.Name),
                                SmoObjectHelpers.SqlBracketQuoteString(user.Name)));
                        var connStr =
                            new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString)
                            {
                                InitialCatalog = db.Name,
                                UserID = user.Name,
                                // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="not secret")]
                                Password = pwd,
                                Authentication = SqlAuthenticationMethod.SqlPassword,
                            };
                        using (var sqlConn = new SqlConnection(connStr.ToString()))
                        {
                            var dbScopedConn = new ServerConnection(sqlConn);
                            var server = new _SMO.Server(dbScopedConn);
                            Assert.DoesNotThrow(() =>
                                server.GetSmoObject(proc.Urn), "GetSmoObject {0}", proc.Urn);
                        }
                    }
                    finally
                    {
                        if (login != null)
                        {
                            login.Drop();
                        }
                    }
                }, AzureDatabaseEdition.DataWarehouse);
        }

        /// <summary>
        /// Test added for task 11813773 - Load SP in DacFx throws 'unknown property XmlSchemaNamespace' error when getting parameters collection from SMO Procedure object.
        /// This test simulates the step that DacFx uses to get parameters count of a stored procedure.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void Check_DW_DB_returns_Store_Procedure_Parameters()
        {
            ExecuteWithDbDrop("SprocVerifyParameterDw", AzureDatabaseEdition.DataWarehouse, null, VerifyStoredProcedureParameterCount);
        }

        private void VerifyStoredProcedureParameterCount(_SMO.Database database)
        {
            var name = GenerateSmoObjectName("procVerifyParameter");
            //Create a stored procedure with a parameter
            _SMO.StoredProcedure proc = new _SMO.StoredProcedure(database, name);
            proc.TextHeader = String.Format("CREATE PROCEDURE {0} @p1 int AS", SmoObjectHelpers.SqlBracketQuoteString(proc.Name));
            proc.TextBody =
                "BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            proc.Create();

            //Get the Server object
            _SMO.Server server = database.GetServerObject();
          
            //Set default init fields for types as in DacFx test
            server.SetDefaultInitFields(typeof(_SMO.Table), true, proc.DatabaseEngineEdition);
            server.SetDefaultInitFields(typeof(_SMO.View), true, proc.DatabaseEngineEdition);
            server.SetDefaultInitFields(typeof(_SMO.StoredProcedure), true, proc.DatabaseEngineEdition);
            server.SetDefaultInitFields(typeof(_SMO.StoredProcedureParameter), true, proc.DatabaseEngineEdition);

            //Verifying other types that are applicable for SqlDataWarehouse
            server.SetDefaultInitFields(typeof(_SMO.UserDefinedFunction), true, proc.DatabaseEngineEdition);
            server.SetDefaultInitFields(typeof(_SMO.User), proc.DatabaseEngineEdition, "IsSystemObject");
            server.SetDefaultInitFields(typeof(_SMO.DatabaseRole), proc.DatabaseEngineEdition, "IsFixedRole");
            server.SetDefaultInitFields(typeof(_SMO.DatabaseRole), proc.DatabaseEngineEdition, "Name");
            server.SetDefaultInitFields(typeof(_SMO.FileGroup), proc.DatabaseEngineEdition, "Name");
            server.SetDefaultInitFields(typeof(_SMO.SqlAssembly), proc.DatabaseEngineEdition, "IsSystemObject");
            server.SetDefaultInitFields(typeof(_SMO.Login), proc.DatabaseEngineEdition, "Name");
            server.SetDefaultInitFields(typeof(_SMO.Certificate), proc.DatabaseEngineEdition, "Name");
            server.SetDefaultInitFields(typeof(_SMO.ExtendedProperty), true, proc.DatabaseEngineEdition);

            //Retrieve StoredProcedures, exclude system procedures
            var procedures = (from _SMO.StoredProcedure procedure in database.StoredProcedures
                              where !procedure.IsSystemObject
                              orderby procedure.Schema, procedure.Name
                              select procedure).ToList();
            //Get count of parameters if exists
            foreach (_SMO.StoredProcedure sProcedure in procedures)
            {
                var parameterCount = sProcedure.Parameters;
                if (parameterCount != null)
                {
                    Assert.That(parameterCount.Count, Is.GreaterThan(-1), "parameterCount.Count");
                }
            }
        }
    }
 #endregion
}
