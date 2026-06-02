// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Mail;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing MailProfile properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class MailProfile_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            MailProfile mailProfile = (MailProfile)obj;
            SqlMail sm = (SqlMail)objVerify;

            sm.Profiles.Refresh();
            Assert.IsNull(sm.Profiles[mailProfile.Name],
                            "Current mail profile not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a mail profile with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_MailProfile_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SqlMail sm = database.Parent.Mail;
                    MailProfile mailProfile = new MailProfile(sm,
                        GenerateUniqueSmoObjectName("mailProfile"));

                    try
                    {
                        VerifySmoObjectDropIfExists(mailProfile, sm);
                    }
                    catch (Exception)
                    {
                        if (sm.Accounts[mailProfile.Name] != null)
                        {
                            mailProfile.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

