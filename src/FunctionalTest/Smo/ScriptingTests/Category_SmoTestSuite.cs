// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing JobCategory properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class JobCategorySmoTest : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            JobCategory jobCategory = (JobCategory)obj;
            JobServer jobSvr = (JobServer)objVerify;

            jobSvr.Jobs.Refresh();
            Assert.IsNull(jobSvr.JobCategories[jobCategory.Name],
                          "Current job not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a job category with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_JobCategory_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string jobCategoryName = GenerateUniqueSmoObjectName("jobCategory");
                    JobServer jobSvr = database.Parent.JobServer;
                    JobCategory jobCategory = new JobCategory(jobSvr,
                        jobCategoryName);

                    try
                    {
                        VerifySmoObjectDropIfExists(jobCategory, jobSvr);
                    }
                    catch (Exception)
                    {
                        if (jobSvr.JobCategories[jobCategory.Name] != null)
                        {
                            jobCategory.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests

        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [TestMethod]
        public void JobCategoryCollection_Count_succeeds()
        {
            ExecuteTest(() =>
            {
                // TODO: Fix collection construction to ensure StringComparer initialization at the Parent and Server levels 
                // Then we can remove the m_comparer assignments in object constructors.
                // https://github.com/microsoft/sqlmanagementobjects/issues/31
                var jobServer = this.ServerContext.JobServer;
                var categories = jobServer.JobCategories;
                var count = categories.Count;
                Assert.That(count, Is.GreaterThan(0), "There should be at least one 1 JobCategory");
            });
        }
    }
}

