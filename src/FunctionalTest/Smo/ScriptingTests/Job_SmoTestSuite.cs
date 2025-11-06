// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Job properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class Job_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            Job job = (Job)obj;
            JobServer jobSvr = (JobServer)objVerify;

            jobSvr.Jobs.Refresh();
            Assert.IsNull(jobSvr.Jobs[job.Name],
                          "Current job not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a job with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Job_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    JobServer jobSvr = database.Parent.JobServer;
                    Job job = new Job(jobSvr,
                        GenerateUniqueSmoObjectName("job"));

                    try
                    {
                        VerifySmoObjectDropIfExists(job, jobSvr);
                    }
                    catch (Exception)
                    {
                        if (jobSvr.Jobs[job.Name] != null)
                        {
                            job.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests

        // TODO: Fix collection construction to ensure StringComparer initialization at the Parent and Server levels 
        // Then we can remove the m_comparer assignments in object constructors.
        // https://github.com/microsoft/sqlmanagementobjects/issues/31
        //[TestMethod]
        //[SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        //public void JobServer_constructor_succeeds_without_msdb_database()
        //{
        //    ExecuteTest(() =>
        //    {
        //        var server = new Management.Smo.Server(new ServerConnection(new SqlConnection(SqlConnectionStringBuilder.ConnectionString)));
        //        Trace.TraceInformation("Removing msdb from server.Databases");
        //        server.Databases.ClearAndInitialize("[@Name = 'master']", Enumerable.Empty<string>());
        //        var jobServer = server.JobServer;
        //        Assert.That(jobServer.Name, Is.EqualTo(server.Name), "jobServer.Name");
        //        var jobServerComparer = jobServer.StringComparer;
        //        server.Databases.ClearAndInitialize("[@Name = 'msdb']", Enumerable.Empty<string>());
        //        var msdbComparer = server.Databases["msdb"].StringComparer;
        //        Assert.Multiple(() =>
        //        {
        //            Assert.That(jobServerComparer.CultureInfo, Is.EqualTo(msdbComparer.CultureInfo), "JobServer StringComparer.CultureInfo should match msdb StringComparer");
        //            Assert.That(jobServerComparer.CompareOptions, Is.EqualTo(msdbComparer.CompareOptions), "JobServer StringComparer.CompareOptions should match msdb StringComparer");
        //        });
        //    });
        //}
    }
}

