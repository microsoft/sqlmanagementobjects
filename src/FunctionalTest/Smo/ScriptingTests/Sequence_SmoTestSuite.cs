// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Sequence properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Sequence_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Sequence seq = (_SMO.Sequence)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Sequences.Refresh();
            Assert.IsNull(database.Sequences[seq.Name],
                          "Current sequence not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a sequence with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Sequence_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Sequence seq = new _SMO.Sequence(database, GenerateSmoObjectName("seq"));

                    seq.DataType = _SMO.DataType.Int;
                    seq.StartValue = 1;
                    seq.IncrementValue = 1;

                    string sequenceScriptDropIfExistsTemplate = "DROP SEQUENCE IF EXISTS [{0}].[{1}]";
                    string sequenceScriptDropIfExists = string.Format(sequenceScriptDropIfExistsTemplate, seq.Schema, seq.Name);

                    VerifySmoObjectDropIfExists(seq, database, sequenceScriptDropIfExists);
                });
        }

        #endregion
    }
}
