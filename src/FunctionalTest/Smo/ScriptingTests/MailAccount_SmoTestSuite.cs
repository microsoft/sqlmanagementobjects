// Copyright (c) Microsoft.
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
    /// Test suite for testing MailAccount properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class MailAccount_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            MailAccount mailAcc = (MailAccount)obj;
            SqlMail sm = (SqlMail)objVerify;

            sm.Accounts.Refresh();
            Assert.IsNull(sm.Accounts[mailAcc.Name],
                            "Current mail account not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a mail account with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_MailAccount_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SqlMail sm = database.Parent.Mail;
                    MailAccount mailAcc = new MailAccount(sm,
                        GenerateUniqueSmoObjectName("mailAcc"));

                    mailAcc.EmailAddress = mailAcc.Name + "@test.com";

                    try
                    {
                        VerifySmoObjectDropIfExists(mailAcc, sm);
                    }
                    catch (Exception)
                    {
                        if (sm.Accounts[mailAcc.Name] != null)
                        {
                            mailAcc.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

