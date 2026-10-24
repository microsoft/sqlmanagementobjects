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
    /// Test suite for testing UserDefinedType properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class UserDefinedType_SmoTestSuite : SmoObjectTestBase
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
            TraceHelper.TraceInformation(string.Format("Setting up database {0}", db.Name));

            foreach (string scriptName in setupScripts)
            {
                new ScriptHelpers().LoadAndRunScriptResource(scriptName, db, Assembly.GetExecutingAssembly());
            }
        }

        #endregion // Database Test Helpers

        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.UserDefinedType udt = (_SMO.UserDefinedType)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.UserDefinedTypes.Refresh();
            Assert.IsNull(database.UserDefinedTypes[udt.Name],
                          "Current user-defined type not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an user-defined type with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SqlRequiredFeature(SqlFeature.SqlClr)]
        public void SmoDropIfExists_UDT_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);

                    _SMO.UserDefinedType udt = new _SMO.UserDefinedType(database,
                        GenerateSmoObjectName("udt"));
                    
                    _SMO.SqlAssembly asm = database.Assemblies["Geometry"];
                    Assert.IsNotNull(asm, "Assembly not created in setup.");
                    
                    udt.AssemblyName = asm.Name;
                    udt.ClassName = "Angle";

                    string udtScriptDropIfExistsTemplate = "DROP TYPE IF EXISTS [{0}].[{1}]";
                    string udtScriptDropIfExists = string.Format(udtScriptDropIfExistsTemplate, udt.Schema, udt.Name);

                    VerifySmoObjectDropIfExists(udt, database, udtScriptDropIfExists);
                });
        }

        #endregion
    }
}
