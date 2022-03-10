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
    /// Test suite for testing Alert properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class Alert_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            Alert alert = (Alert)obj;
            JobServer jobSvr = (JobServer)objVerify;

            jobSvr.Alerts.Refresh();
            Assert.IsNull(jobSvr.Alerts[alert.Name],
                            "Current alert not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an alert with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDropIfExists_Alert_Sql16AndAfterOnPrem()
        {
            this.ExecuteTest(() =>
                {
                    string alertName = GenerateUniqueSmoObjectName("alert");
                    JobServer jobSvr = ServerContext.JobServer;
                    var alert = new Alert(jobSvr, alertName)
                    {
                        PerformanceCondition = "SQLServer:General Statistics|User Connections||>|3"
                    };

                    try
                    {
                        VerifySmoObjectDropIfExists(alert, jobSvr);
                    }
                    catch (Exception)
                    {
                        jobSvr.Alerts.Refresh();
                        if (jobSvr.Alerts[alert.Name] != null)
                        {
                            alert.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.Enterprise)]
        public void Alert_occurrence_count_can_be_altered_and_reset()
        {
            this.ExecuteFromDbPool((db) =>
            {
                string alertName = GenerateUniqueSmoObjectName("alert");
                JobServer jobSvr = ServerContext.JobServer;
                var alert = new Alert(jobSvr, alertName)
                {
                    Severity = 10,
                    IsEnabled = true,
                };
                alert.Create();
                try
                {
                    db.ExecuteNonQuery("EXEC xp_logevent 50001, 'testing', INFORMATIONAL");
                    var dateToSet = DateTime.UtcNow.AddDays(-1).Date;
                    var initialCount = alert.OccurrenceCount;
                    alert.IsEnabled = false;
                    // last occurrencedate can only be reset, not set to a real time
                    alert.LastOccurrenceDate = DateTime.MinValue;
                    alert.CountResetDate = dateToSet;
                    alert.Alter();
                    alert.Refresh();
                    Assert.Multiple(() =>
                    {
                        Assert.That(alert.IsEnabled, Is.False, "IsEnabled after Alter");
                        Assert.That(alert.CountResetDate, Is.EqualTo(dateToSet), "CountResetDate after alter");
                        Assert.That(alert.OccurrenceCount, Is.AtLeast(initialCount), "OccurrenceCount after alter");
                    });
                    alert.ResetOccurrenceCount();
                    alert.Refresh();
                    Assert.That(alert.OccurrenceCount, Is.Zero, "OccurrenceCount after ResetOccurrenceCount");
                }
                finally
                {
                    alert.Drop();
                }
            });
        }
    }
}

