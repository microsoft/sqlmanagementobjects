// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _NU = NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing WorkloadManagementWorkloadGroup properties and scripting
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
    public class WorkloadManagementWorkloadClassifier_SmoTestSuite : SmoObjectTestBase
    {
        /// <summary>
        /// Alter workload classifier test
        /// </summary>
        [TestMethod]
        public void SmoAlter_WorkloadManagementWorkloadGroup()
        {
            this.ExecuteWithDbDrop(
                db =>
                {
                    string wgname = "testworkloadgroup";
                    var wlg = new _SMO.WorkloadManagementWorkloadGroup(db, wgname)
                    {
                        CapPercentageResource = 75,
                        MinPercentageResource = 50,
                        RequestMinResourceGrantPercent = 25.0,
                        RequestMaxResourceGrantPercent = 25.0,
                        Importance = _SMO.WorkloadManagementImportance.Normal,
                        QueryExecutionTimeoutSec = 0
                    };
                    wlg.Create();

                    string wcname = "testworkloadclassifier";
                    // create
                    _SMO.WorkloadManagementWorkloadClassifier wlc = new _SMO.WorkloadManagementWorkloadClassifier(db, wcname);
                    wlc.GroupName = wgname;
                    wlc.MemberName = "dbo";
                    wlc.WlmContext = "new_context";
                    db.WorkloadManagementWorkloadClassifiers.Add(wlc);
                    wlc.Create();

                    //validate
                    Assert.That(db.WorkloadManagementWorkloadClassifiers[wcname].WlmContext, _NU.Is.EqualTo("new_context"),
                                "WorkloadManagemengWorkloadClassifier Alter failed");
                }
            );
        }

        [TestMethod]
        public void SmoDropIfExists_WorkloadManagementWorkloadGroup()
        {
            this.ExecuteWithDbDrop(
                db =>
                {
                    string wgname = "testworkloadgroup";
                    var wlg = new _SMO.WorkloadManagementWorkloadGroup(db, wgname)
                    {
                        CapPercentageResource = 75,
                        MinPercentageResource = 50,
                        RequestMinResourceGrantPercent = 25.0,
                        RequestMaxResourceGrantPercent = 25.0,
                        Importance = _SMO.WorkloadManagementImportance.Normal,
                        QueryExecutionTimeoutSec = 0
                    };
                    wlg.Create();

                    string wcname = "testworkloadclassifier";
                    //validate does not exist
                    Assert.That(db.WorkloadManagementWorkloadClassifiers[wcname], _NU.Is.Null);

                    // create
                    _SMO.WorkloadManagementWorkloadClassifier wlc = new _SMO.WorkloadManagementWorkloadClassifier(db, wcname);
                    wlc.GroupName = wgname;
                    wlc.MemberName = "dbo";
                    db.WorkloadManagementWorkloadClassifiers.Add(wlc);
                    wlc.Create();
                    Assert.That(db.WorkloadManagementWorkloadClassifiers[wcname], _NU.Is.Not.Null);

                    // drop and validate
                    wlc.DropIfExists();
                    Assert.That(db.WorkloadManagementWorkloadClassifiers[wcname], _NU.Is.Null,
                        "WorkloadManagemengWorkloadClassifiers DropIfExists failed");
                }
            );
        }

        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
        }
    }
}
