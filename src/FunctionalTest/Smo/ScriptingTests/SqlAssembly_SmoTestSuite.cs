// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing SqlAssembly properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class SqlAssembly_SmoTestSuite : SmoObjectTestBase
    {
        #region Static Vars

        /// <summary>
        /// List of setup scripts to run
        /// </summary>
        private static readonly IList<string> setupScripts = new List<string>()
        {
            {
                "SqlAssembly_SmoTestSuite_Sql2016_Setup.sql"
            }
        };

        #endregion // Static Vars

        #region Database Test Helpers

        /// <summary>
        /// Runs the setup scripts for the specified targetServer
        /// </summary>
        /// <param name="db"></param>
        internal static void SetupDb(_SMO.Database db)
        {
            TraceHelper.TraceInformation(String.Format("Setting up database {0}", db.Name));

            foreach (string scriptName in setupScripts)
            {
                new ScriptHelpers().LoadAndRunScriptResource(scriptName, db, Assembly.GetExecutingAssembly());
            }
        }

        #endregion // Database Test Helpers

        #region Scripting Tests

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected override void CreateSmoObject(_SMO.SqlSmoObject obj)
        {
            _SMO.SqlAssembly asm = (_SMO.SqlAssembly)obj;
            _SMO.Database database = asm.Parent;

            asm = database.Assemblies["Geometry"];
            Assert.IsNotNull(asm, "Assembly not created in setup.");
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.SqlAssembly asm = (_SMO.SqlAssembly)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Assemblies.Refresh();
            Assert.IsNull(database.Assemblies[asm.Name],
                          "Current assembly not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an assembly with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SqlRequiredFeature(SqlFeature.SqlClr)]
        public void SmoDropIfExists_SqlAssembly_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);

                    _SMO.SqlAssembly asm = new _SMO.SqlAssembly(database,
                        GenerateSmoObjectName("asm"));

                    string asmScriptDropIfExistsTemplate = "DROP ASSEMBLY IF EXISTS [{0}]";
                    string asmScriptDropIfExists = string.Format(asmScriptDropIfExistsTemplate, asm.Name);

                    VerifySmoObjectDropIfExists(asm, database, asmScriptDropIfExists);
                });
        }

        #endregion
    }
}
