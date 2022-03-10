// Copyright (c) Microsoft.
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
    /// Test suite for testing DatabaseEncryptionKey properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class DatabaseEncryptionKey_SmoTestSuite : SmoObjectTestBase
    {
        #region Static Vars

        /// <summary>
        /// List of setup scripts to run
        /// </summary>
        private static readonly IList<string> setupScripts = new List<string>()
        {
            {
                "DatabaseEncryptionKey_SmoTestSuite_Sql2016_Setup.sql"
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
            var scriptHelpers = new ScriptHelpers();
            foreach (string scriptName in setupScripts)
            {
                scriptHelpers.LoadAndRunScriptResource(scriptName, db, Assembly.GetExecutingAssembly());
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
            _SMO.DatabaseEncryptionKey dek = (_SMO.DatabaseEncryptionKey)obj;
            _SMO.Database database = (_SMO.Database)dek.Parent;

            dek = database.DatabaseEncryptionKey;
            Assert.IsNotNull(dek, "Database encryption key not created in setup.");
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.DatabaseEncryptionKey dek = (_SMO.DatabaseEncryptionKey)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Refresh();
            Assert.IsNotNull(database.DatabaseEncryptionKey.State == _SMO.SqlSmoState.Dropped,
                          "Current database encryption key not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an database encryption key with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_DatabaseEncryptionKey_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);

                    _SMO.DatabaseEncryptionKey dek = new _SMO.DatabaseEncryptionKey();

                    dek.Parent = database;

                    VerifySmoObjectDropIfExists(dek, database);
                });
        }

        #endregion
    }
}
