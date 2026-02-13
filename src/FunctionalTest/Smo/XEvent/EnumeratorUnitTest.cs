// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using System.Linq;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Unit test for the Enumerator in XEvent object model. This Enumerator is based
    /// on SFC Enumerator. The main implementation work is to provide the model definition
    /// in XML resource file. So the test use SFC Enumerator framework to verify if each type
    /// in our model definition can be correctly enumerated from backend DB.
    /// </summary>
    [TestClass]
    public class EnumeratorUnitTest : DbScopedXEventTestBase
    {
        /// <summary>
        /// Query request used by SFC Enumerator. The expected Urn for each type
        /// is provided in each test case.
        /// </summary>
        private Request request;

        /// <summary>
        /// SFC Enumerator to execute the query
        /// </summary>
        private Enumerator enumerator;

        /// <summary>
        /// The result for each enumeration. The verify is done on this result.
        /// </summary>
        private EnumResult result;

        private string storeUrn;
        private string dbName;

        [TestMethod]
        public void DbScoped_Enumerator_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                InitTestSession();
                storeUrn = store.Urn;
                dbName = db.Name;
                enumerator = new Enumerator();
                request = new Request();
                result = new EnumResult();
                TestXEStore();
                TestAction();
                TestActionInfo();
                TestEvent();
                TestEventColumnInfo();
                TestEventInfo();
                TestMapInfo();
                TestMapValueInfo();
                TestPackage();
                TestPredCompareInfo();
                TestPredSourceInfo();
                TestTarget();
                TestTargetColumnInfo();
                TestTargetField();
                TestTargetInfo();
                TestTypeInfo();
            }));
        }

        private void InitTestSession()
        {
            var session = store.CreateSession("azure_xe_test");
            session.AddEvent("sqlserver.error_reported").AddAction("event_sequence");
            session.AddEvent("wait_info");            
            session.AddTarget(store.RingBufferTargetInfo).TargetFields["max_memory"].Value = 4096;
            session.Create();
        }

        /// <summary>
        /// Tests the XE store. Validate the running session count property.
        /// </summary>
        public void TestXEStore()
        {
            request.Urn = this.storeUrn;
            result = enumerator.Process(connection, request);
            DataTable dt = result;

            Assert.AreEqual(1, dt.Rows.Count);
            DataRow row = dt.Rows[0];

            Assert.AreEqual(dbName, row["Name"]);

            int running = (int)connection.ExecuteScalar("SELECT count(*) FROM sys.dm_xe_database_sessions");
            Assert.AreEqual(running, row["RunningSessionCount"]);
        }

        /// <summary>
        /// Tests the XE store with no filter specified. Validate the running session count property.
        /// </summary>
        public void TestXEStoreUnfiltered()
        {
            request.Urn = "DatabaseXEStore";

            // Try default first (master) - should fail 

            var ex = Assert.Throws<EnumeratorException>(() =>
                result = enumerator.Process(this.ServerContext.ConnectionContext, request), "Process should throw");
            
            var sqlex = ex.InnerException as SqlException;
            Assert.That(sqlex, Is.Not.Null, "InnerException should be SqlException");
            Assert.That(sqlex.Number, Is.EqualTo(297), "Unexpected SqlException Number");

            // Try direct connection
            result = enumerator.Process(connection, request);
            DataTable dt = result;

            Assert.AreEqual(1, dt.Rows.Count);
            DataRow row = dt.Rows[0];

            Assert.AreEqual(dbName, row["Name"]);

            int running = (int)connection.ExecuteScalar("SELECT count(*) FROM sys.dm_xe_database_sessions");
            Assert.AreEqual(running, row["RunningSessionCount"]);
        }



        /// <summary>
        /// Tests the event.Validate all of the meaningful properties.
        /// </summary>

        public void TestEvent()
        {
            request.Urn = this.storeUrn + "/Session[@Name='azure_xe_test']/Event[@Name='sqlos.wait_info']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual("sqlos", row["PackageName"]);
                Assert.That(row["ID"], Is.EqualTo(1), $"ID for {request.Urn}");
            }
        }

        /// <summary>
        /// Tests the target.Validate all of the meaningful properties.
        /// </summary>
        public void TestTarget()
        {
            request.Urn = this.storeUrn + "/Session[@Name='azure_xe_test']/Target[@Name='package0.ring_buffer']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.That(row["ID"], Is.EqualTo(4), $"ID for {request.Urn}");
                Assert.AreEqual("package0", row["PackageName"]);
            }
        }

        /// <summary>
        /// Tests the action.Validate all of the meaningful properties.
        /// </summary>
        public void TestAction()
        {
            request.Urn = this.storeUrn + "/Session[@Name='azure_xe_test']/Event[@Name='sqlserver.error_reported']/Action[@Name='package0.event_sequence']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual("package0", row["PackageName"]);
            }
        }
        /// <summary>
        /// Tests the TargetField.Validate all of the meaningful properties.
        /// </summary>
        public void TestTargetField()
        {
            request.Urn = this.storeUrn + "/Session[@Name='azure_xe_test']/Target[@Name='package0.ring_buffer']/TargetField[@Name='max_memory']";
            result = enumerator.Process(connection, request);

            // sys.dm_xe_object_columns 

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual(4096, row["value"]);
            }

        }

        /// <summary>
        /// Tests the package.Validate all of the meaningful properties.
        /// </summary>
        public void TestPackage()
        {
            request.Urn = this.storeUrn + "/Package[@Name='package0']";
            result = enumerator.Process(connection, request);

            
            // sys.dm_xe_packages 
            if (connection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                DataTable dataTable = result;
                Assert.AreEqual(1, dataTable.Rows.Count);
            }
            else
            {
                DataSet directSet = connection.ExecuteWithResults("Select * From sys.dm_xe_packages Where name = 'package0'");
                DataRow directRow = directSet.Tables[0].Rows[0];

                DataTable dataTable = result;
                Assert.AreEqual(dataTable.Rows.Count, 1);

                DataRow row = dataTable.Rows[0];

                Assert.AreEqual(directRow["capabilities"], row["Capabilities"]);
                Assert.AreEqual(directRow["capabilities_desc"], row["CapabilitiesDesc"]);
                Assert.AreEqual(directRow["description"], row["Description"]);
            }
        }

        /// <summary>
        /// Tests the EventInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestEventInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='sqlserver']/EventInfo[@Name='lock_acquired']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.IsNotNull(row["Description"]);
                Assert.AreEqual(2, row["Capabilities"]);
                Assert.AreEqual("sds_visible", row["CapabilitiesDesc"]);
            }

        }

        /// <summary>
        /// Tests the event column info.
        /// </summary>
        public void TestEventColumnInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='sqlserver']/EventInfo[@Name='allocation_ring_buffer_recorded']/EventColumnInfo[@Name='collect_call_stack']";
            result = enumerator.Process(connection, request);

            
            // sys.dm_xe_packages 
            if (connection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                DataTable dataTable = result;
                Assert.AreEqual(dataTable.Rows.Count, 0);
            }
            else
            {
                DataTable dataTable = result;
                Assert.AreEqual(dataTable.Rows.Count, 1);
                foreach (DataRow row in dataTable.Rows)
                {
                    Assert.IsNotNull(row);
                    Assert.AreEqual(0, row["ID"]);
                    Assert.AreEqual("boolean", row["TypeName"]);
                    Assert.AreEqual("package0", row["TypePackageName"]);
                    Assert.AreEqual("false", row["Value"]);
                    Assert.AreNotEqual(DBNull.Value, row["Description"]);
                    Assert.AreEqual(65536, row["Capabilities"]);
                    Assert.IsNotNull(row["CapabilitiesDesc"]);
                }
            }
        }

        /// <summary>
        /// Tests the TargetInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestTargetInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='package0']/TargetInfo[@Name='pair_matching']";
            result = enumerator.Process(connection, request);

            
            // sys.dm_xe_packages 
            if (connection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                DataTable dataTable = result;
                Assert.AreEqual(0, dataTable.Rows.Count, "Expected 0 rows for pair_matching target in azure");
            }
            else
            {
                DataTable dataTable = result;
                Assert.AreEqual(dataTable.Rows.Count, 1);
                foreach (DataRow row in dataTable.Rows)
                {
                    Assert.IsNotNull(row);
                    Assert.AreEqual("Pairing target", row["Description"]);
                    Assert.AreEqual(256, row["Capabilities"]);
                    Assert.AreEqual("process_whole_buffers", row["CapabilitiesDesc"]);
                }
            }
        }

        /// <summary>
        /// Tests the target column info.
        /// </summary>        
        public void TestTargetColumnInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='Package0']/TargetInfo[@Name='pair_matching']/TargetColumnInfo[@Name='end_event']";
            result = enumerator.Process(connection, request);

            
            // sys.dm_xe_packages 
            if (connection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                DataTable dataTable = result;
                Assert.AreEqual(0, dataTable.Rows.Count, "Expected no data for pair_matching target in azure");
            }
            else
            {
                DataTable dataTable = result;
                Assert.AreEqual(dataTable.Rows.Count, 1);
                foreach (DataRow row in dataTable.Rows)
                {
                    Assert.IsNotNull(row);
                    Assert.AreEqual(3, row["ID"]);
                    Assert.AreEqual("unicode_string_ptr", row["TypeName"]);
                    Assert.AreEqual("package0", row["TypePackageName"]);
                    //Assert.AreEqual("customizable", row["ColumnType"]);
                    Assert.AreEqual(System.DBNull.Value, row["Value"]);
                    Assert.AreEqual("Event name specifying the ending event in a paired sequence", row["Description"]);
                    Assert.AreEqual(1, row["Capabilities"]);
                    Assert.AreEqual("mandatory", row["CapabilitiesDesc"]);
                }
            }
        }

        /// <summary>
        /// Tests the ActionInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestActionInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='package0']/ActionInfo[@Name='event_sequence']";
            result = enumerator.Process(connection, request);

            
            // sys.dm_xe_packages 
            if (connection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                DataTable dataTable = result;
                Assert.AreEqual(1, dataTable.Rows.Count);
            }
            else
            {
                DataSet directSet = connection.ExecuteWithResults(@"select a.description, ISNULL(a.capabilities,0) 'capabilities', a.type_name, tp.name 'pname' 
from sys.dm_xe_packages p, sys.dm_xe_objects a, sys.dm_xe_packages tp
where a.object_type = N'action' and a.package_guid = p.guid and p.name = 'package0' and a.name='event_sequence' and tp.guid = a.type_package_guid");
                DataRow directRow = directSet.Tables[0].Rows[0];

                DataTable dataTable = result;
                Assert.AreEqual(1, dataTable.Rows.Count);

                DataRow row = dataTable.Rows[0];

                Assert.AreEqual(directRow["description"], row["Description"]);
                Assert.AreEqual(directRow["capabilities"], row["Capabilities"]);
                Assert.AreEqual(directRow["type_name"], row["TypeName"]);
                Assert.AreEqual(directRow["pname"], row["TypePackageName"]);
            }
        }

        /// <summary>
        /// Tests the PredSourceInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestPredSourceInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='package0']/PredSourceInfo[@Name='current_thread_id']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual("Get the current Windows thread ID", row["Description"]);
                Assert.AreEqual("package0", row["TypePackageName"]);
                Assert.AreEqual("uint32", row["TypeName"]);
            }

        }

        /// <summary>
        /// Tests the PredCompareInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestPredCompareInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='package0']/PredCompareInfo[@Name='equal_uint64']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual("Equality operator between two 64-bit unsigned int values", row["Description"]);
                Assert.AreEqual("package0", row["TypePackageName"]);
                Assert.AreEqual("uint64", row["TypeName"]);
            }
        }

        /// <summary>
        /// Tests the MapInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestMapInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='package0']/MapInfo[@Name='etw_level']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual("ETW levels", row["Description"]);
                Assert.AreEqual(0, row["Capabilities"]);
            }
        }

        /// <summary>
        /// Tests the TypeInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestTypeInfo()
        {
            request.Urn = this.storeUrn + "/Package[@Name='package0']/TypeInfo[@Name='int8']";
            result = enumerator.Process(connection, request);

            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual("Signed 8-bit integer", row["Description"]);
                Assert.AreEqual(256, row["Capabilities"]);
            }
        }

        /// <summary>
        /// Tests the MapValueInfo.Validate all of the meaningful properties.
        /// </summary>
        public void TestMapValueInfo()
        {
            request.Urn = this.storeUrn +
                          "/Package[@Name='package0']/MapInfo[@Name='etw_level']/MapValueInfo[@Name='1']";
            result = enumerator.Process(connection, request);
            DataTable dataTable = result;
            Assert.That(dataTable.Rows.Cast<DataRow>().ToList(), Has.Count.EqualTo(1), $"Incorrect row count fetched for {request.Urn}");
            foreach (DataRow row in dataTable.Rows)
            {
                Assert.IsNotNull(row);
                Assert.AreEqual("Abnormal exit or termination", row["Value"]);
                Assert.AreEqual("1", row["Name"]);
            }
        }
    }
}
