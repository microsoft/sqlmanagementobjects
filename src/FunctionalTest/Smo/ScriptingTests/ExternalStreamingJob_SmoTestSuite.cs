// Copyright (c) Microsoft.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing External Language properties and scripting.
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = HostPlatformNames.Linux, Edition= DatabaseEngineEdition.SqlDatabaseEdge)]
    public class ExternalStreamingJob_SmoTestSuite : SmoObjectTestBase
    {
        [TestMethod]
        public void ExternalStreamingJob_TestCreateWithoutRequiredParameters()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    Assert.Throws <Management.Smo.FailedOperationException> (
                        () =>
                        {
                            string ExternalStreamingJobName = GenerateUniqueSmoObjectName("ExternalStreamingJob");
                            var obj = new ExternalStreamingJob();
                            CreateSmoObject(obj);
                        });
                });
        }

        [TestMethod]
        public void ExternalStreamingJob_TestCreateWithRequiredParametersOnly()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Test the External Stream ICreateable interface
                    //
                    string externalStreamName = GenerateSmoObjectName("ExternalStream");
                    var externalStream = new ExternalStream(database, externalStreamName);
                    externalStream.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName");

                    string externalStreamName1 = GenerateSmoObjectName("ExternalStream1");
                    var externalStream1 = new ExternalStream(database, externalStreamName1);
                    externalStream1.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName1");

                    // The DataSource must exists in the database
                    //
                    ExternalDataSource testDataSource = new ExternalDataSource(database, externalStream.DataSourceName);
                    testDataSource.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource.Location = "edgehub://";

                    ExternalDataSource testDataSource1 = new ExternalDataSource(database, externalStream1.DataSourceName);
                    testDataSource1.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource1.Location = "edgehub://";

                    CreateSmoObject(testDataSource);
                    CreateSmoObject(testDataSource1);

                    CreateSmoObject(externalStream);
                    CreateSmoObject(externalStream1);

                    // Test the External Streaming Job ICreateable interface
                    //
                    string ExternalStreamingJobName = GenerateUniqueSmoObjectName("ExternalStreamingJob");
                    var ExternalStreamingJob = new ExternalStreamingJob(database, ExternalStreamingJobName);
                    ExternalStreamingJob.Name = GenerateUniqueSmoObjectName("ExternalStreamingJobDataSourceName");
                    ExternalStreamingJob.Statement = $"Select * INTO {externalStreamName1} from {externalStreamName}";
                    CreateSmoObject(ExternalStreamingJob);

                    // Verify that the object exists in the database
                    //
                    VerifyObjectExists(database, ExternalStreamingJobName);
                    Assert.That(ExternalStreamingJob.Status, 
                        Is.EqualTo(ExternalStreamingJobStatusType.Created), 
                        "The Stream Job status of created failed");

                    ExternalStreamingJob.Drop();
                    VerifyIsSmoObjectDropped(ExternalStreamingJob, database);
                });
        }

        [TestMethod]
        public void ExternalStreamingJob_TestStartAndStopJobWithStatus()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Test the External Stream ICreateable interface
                    //
                    string externalStreamName = GenerateSmoObjectName("ExternalStream");
                    var externalStream = new ExternalStream(database, externalStreamName);
                    externalStream.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName");

                    string externalStreamName1 = GenerateSmoObjectName("ExternalStream1");
                    var externalStream1 = new ExternalStream(database, externalStreamName1);
                    externalStream1.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName1");

                    // The DataSource must exists in the database
                    //
                    ExternalDataSource testDataSource = new ExternalDataSource(database, externalStream.DataSourceName);
                    testDataSource.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource.Location = "edgehub://";

                    ExternalDataSource testDataSource1 = new ExternalDataSource(database, externalStream1.DataSourceName);
                    testDataSource1.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource1.Location = "edgehub://";

                    CreateSmoObject(testDataSource);
                    CreateSmoObject(testDataSource1);

                    CreateSmoObject(externalStream);
                    CreateSmoObject(externalStream1);

                    // Test the External Streaming Job ICreateable interface
                    //
                    string ExternalStreamingJobName = GenerateUniqueSmoObjectName("ExternalStreamingJob");
                    var ExternalStreamingJob = new ExternalStreamingJob(database, ExternalStreamingJobName);
                    ExternalStreamingJob.Name = GenerateUniqueSmoObjectName("ExternalStreamingJobDataSourceName");
                    ExternalStreamingJob.Statement = $"Select * INTO {externalStreamName1} from {externalStreamName}";
                    CreateSmoObject(ExternalStreamingJob);

                    // Verify that the object exists in the database
                    //
                    VerifyObjectExists(database, ExternalStreamingJobName);
                    Assert.That(ExternalStreamingJob.Status,
                        Is.EqualTo(ExternalStreamingJobStatusType.Created),
                        "The Stream Job status of created failed");

                    try
                    {
                        ExternalStreamingJob.StartStreamingJob();
                        ExternalStreamingJob.Refresh();
                        Assert.That(ExternalStreamingJob.Status,
                            Is.EqualTo(ExternalStreamingJobStatusType.Starting),
                            "The Stream Job status of starting failed");
                    }
                    finally
                    {
                        ExternalStreamingJob.StopStreamingJob();
                        ExternalStreamingJob.Refresh();
                        Assert.That(ExternalStreamingJob.Status,
                            Is.EqualTo(ExternalStreamingJobStatusType.Stopping),
                            "The Stream Job status of stopping failed"
                            );
                    }
                });
        }

        protected override void VerifyIsSmoObjectDropped(SqlSmoObject obj, SqlSmoObject objVerify)
        {
            var database = (Database)objVerify;
            var extStream = (ExternalStreamingJob)obj;
            NUnit.Framework.Assert.That(() =>
            {
                return !database.ExternalStreamingJobs.Contains(extStream.Name);
            }, "Unable to Drop External Stream Object");
        }
    }
}