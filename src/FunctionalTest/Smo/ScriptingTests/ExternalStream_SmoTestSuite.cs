// Copyright (c) Microsoft.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing External Language properties and scripting.
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = HostPlatformNames.Linux, Edition= DatabaseEngineEdition.SqlDatabaseEdge)]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.Enterprise, DatabaseEngineEdition.Personal, DatabaseEngineEdition.SqlDatabase, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.Standard, DatabaseEngineEdition.Unknown)]
    public class ExternalStream_SmoTestSuite : SmoObjectTestBase
    {
        [TestMethod]
        public void ExternalStream_TestCreateWithoutRequiredParameters()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    NUnit.Framework.Assert.Throws <Management.Smo.PropertyNotSetException> (
                        () =>
                        {
                            // Test the External Stream ICreateable interface
                            //
                            try
                            {

                                string externalStreamName = GenerateUniqueSmoObjectName("ExternalStream");
                                var obj = new ExternalStream(database, externalStreamName);
                                CreateSmoObject(obj);
                            }
                            catch (FailedOperationException e)
                            {
                                throw e.InnerException;
                            }
                        });
                });
        }

        [TestMethod]
        public void ExternalStream_TestCreateWithRequiredParametersOnly()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Test the External Stream ICreateable interface
                    //
                    string externalStreamName = GenerateUniqueSmoObjectName("ExternalStream");
                    var externalStream = new ExternalStream(database, externalStreamName);
                    externalStream.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName");

                    // The DataSource must exists in the database
                    //
                    ExternalDataSource testDataSource = new ExternalDataSource(database, externalStream.DataSourceName);
                    testDataSource.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource.Location = "edgehub://";
                    CreateSmoObject(testDataSource);

                    CreateSmoObject(externalStream);

                    // Verify that the object exists in the database
                    //
                    VerifyObjectExists(database, externalStreamName);
                });
        }

        [TestMethod]
        public void ExternalStream_TestCreateWithSomeParameters()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Test the External Stream ICreateable interface
                    //
                    string externalStreamName = GenerateUniqueSmoObjectName("ExternalStream");
                    var externalStream = new ExternalStream(database, externalStreamName);
                    externalStream.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName");
                    externalStream.Location = "]'locationName";

                    // The DataSource must exists in the database
                    //
                    ExternalDataSource testDataSource = new ExternalDataSource(database, externalStream.DataSourceName);
                    testDataSource.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource.Location = "edgehub://";
                    CreateSmoObject(testDataSource);

                    CreateSmoObject(externalStream);

                    // Verify that the object exists in the database
                    //
                    VerifyObjectExists(database, externalStreamName);
                });
        }

        /// <summary>
        /// Tests creating, altering, and dropping an external language through SMO from binary content.
        /// </summary>
        [TestMethod]
        public void ExternalStream_TestCreateWithStreamEngineParameters()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Test the External Stream ICreateable interface
                    //
                    string externalStreamName = GenerateUniqueSmoObjectName("ExternalStream");
                    var externalStream = new ExternalStream(database, externalStreamName);
                    externalStream.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName");
                    externalStream.Location = "]'locationName";
                    externalStream.InputOptions = "Partitions: 5";
                    externalStream.OutputOptions = "REJECT_POLICY: DROP, MINIMUM_ROWS: 10";

                    // The DataSource must exists in the database
                    //
                    ExternalDataSource testDataSource = new ExternalDataSource(database, externalStream.DataSourceName);
                    testDataSource.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource.Location = "edgehub://";
                    CreateSmoObject(testDataSource);

                    CreateSmoObject(externalStream);

                    // Verify that the object exists in the database
                    //
                    VerifyObjectExists(database, externalStreamName);
                });
        }

        [TestMethod]
        public void ExternalStream_TestDrop()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Test the External Stream ICreateable interface
                    //
                    string externalStreamName = GenerateUniqueSmoObjectName("ExternalStream");
                    var externalStream = new ExternalStream(database, externalStreamName);
                    externalStream.DataSourceName = GenerateUniqueSmoObjectName("ExternalStreamDataSourceName");
                    externalStream.Location = "]'locationName";
                    externalStream.InputOptions = "Partitions: 5";
                    externalStream.OutputOptions = "REJECT_POLICY: DROP, MINIMUM_ROWS: 10";

                    // The DataSource must exists in the database
                    //
                    ExternalDataSource testDataSource = new ExternalDataSource(database, externalStream.DataSourceName);
                    testDataSource.DataSourceType = ExternalDataSourceType.ExternalGenerics;
                    testDataSource.Location = "edgehub://";
                    CreateSmoObject(testDataSource);

                    CreateSmoObject(externalStream);

                    // Verify that the object exists in the database
                    //
                    VerifyObjectExists(database, externalStreamName);

                    // Verify the object exists in database collection
                    //
                    NUnit.Framework.Assert.That(() =>
                    {
                        return database.ExternalStreams.Contains(externalStream.Name);
                    }, "The ExternalStream does not exists in the database collection ExternalStreams");

                    // Drop the object
                    //
                    externalStream.Drop();

                    // Verify the object has been dropped
                    //
                    VerifyIsSmoObjectDropped(externalStream, database);
                });
        }

        protected override void VerifyIsSmoObjectDropped(SqlSmoObject obj, SqlSmoObject objVerify)
        {
            var database = (Database)objVerify;
            var extStream = (ExternalStream)obj;
            NUnit.Framework.Assert.That(() =>
            {
                return !database.ExternalStreams.Contains(extStream.Name);
            }, "Unable to Drop External Stream Object");
        }
    }
}