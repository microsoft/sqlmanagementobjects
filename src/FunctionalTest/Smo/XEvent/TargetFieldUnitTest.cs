// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;


namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for TargetFieldUnitTest
    /// </summary>
    [TestClass]
    public class TargetFieldUnitTest : DbScopedXEventTestBase
    {
        private static Target ringBufferTarget;

        [TestMethod]
        public void DbScoped_TargetField_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                ringBufferTarget = store.CreateSession("ut1").AddTarget(store.RingBufferTargetInfo);            
                TestConstructors();
                TestProperties();
                TestFields();
                TestInvalidParameter();
            }));
        }
                    
        /// <summary>
        /// Tests the constructors.
        /// </summary>
        public void TestConstructors()
        {
            
            TargetField field = new TargetField();
            Assert.IsNotNull(field);
            Assert.AreEqual(-1, field.ID);

            field = new TargetField(ringBufferTarget, store.RingBufferTargetInfo.TargetColumnInfoSet["max_memory"]);
            Assert.IsNotNull(field);
            Assert.AreEqual(ringBufferTarget, field.Parent);
            Assert.AreEqual("max_memory", field.Name);
        }

        /// <summary>
        /// Tests the properties.
        /// </summary>
        public void TestProperties()
        {
            string name = "TestProperties_62C4CD89D74A4dceB066CB304959BFC5";
            string sqlCreate = "CREATE EVENT SESSION " + name + " ON DATABASE ADD EVENT sqlserver.rpc_starting ADD TARGET package0.ring_buffer(SET max_memory=8096)";
            string sqlDrop = "DROP EVENT SESSION " + name + " ON DATABASE";
            if (store.Sessions[name] != null)
            {
                (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlDrop);
            }
            (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlCreate);
            try
            {
                
                Target target = store.Sessions[name].Targets["package0.ring_buffer"];
                //target field number change due to the modification in TargetFields property.
                Assert.AreEqual(3, target.TargetFields.Count);

                TargetField field = target.TargetFields["max_memory"];
                Assert.IsNotNull(field);
                Assert.AreEqual("max_memory", field.Name);
                Assert.AreEqual(8096, field.Value);
                Assert.IsTrue(field.Description.Contains("Maximum amount of memory in KB to use"));

                //occurrence_number will be null.
                field = target.TargetFields["occurrence_number"];
                Assert.AreEqual(null, field.Value);
            }
            finally
            {
                (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlDrop);
            }
        }

        [TestMethod]
        [VisualStudio.TestTools.UnitTesting.Ignore]
        [SqlTestCategory(SqlTestCategory.Staging)]
        // azure doesn't support histogram target yet
        public void TestStringField()
        {
            string name = "TargetFieldUnitTest_TestStringField";
            Session session = null;
            session = store.Sessions[name];
            if (session != null)
            {
                session.Drop();
            }
            session = store.CreateSession(name);                         
            session.AddEvent("sqlserver.sp_statement_starting");
            session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.sql_statement_completed"));
            Target target = session.AddTarget(store.HistogramTargetInfo);
            target.TargetFields["filtering_event_name"].Value = "sqlserver.sp_statement_starting";
            target.TargetFields["slots"].Value = 32;
            target.TargetFields["source"].Value = "source_database_id";
            target.TargetFields["source_type"].Value = 0;
            session.Create();
            try
            {
                target = store.Sessions[name].Targets["package0.histogram"];
                Assert.AreEqual("sqlserver.sp_statement_starting", target.TargetFields["filtering_event_name"].Value);
                Assert.AreEqual(32, target.TargetFields["slots"].Value);
                Assert.AreEqual("source_database_id", target.TargetFields["source"].Value);
                Assert.AreEqual(0, target.TargetFields["source_type"].Value);

            }
            finally
            {
                if (session != null)
                {
                    session.Drop();
                }
            }
        }

        /// <summary>
        /// Tests all customized columns have been created as fields.
        /// </summary>
        public void TestFields()
        {
            
            TargetFieldCollection coll = ringBufferTarget.TargetFields;
            Assert.That(coll.OfType<TargetField>().Select(field => field.Name),
                Is.EquivalentTo(new string[] {"max_events_limit", "max_memory", "occurrence_number"}),
                "Unexpected TargetFieldCollection for ring buffer target");

        }


        /// <summary>
        /// Tests invalid parameter for constructor
        /// </summary>
        public void TestInvalidParameter()
        {            
            Assert.Throws<ArgumentNullException>(() => 
                new TargetField(ringBufferTarget, null),
            "TargetField constructor should throw");
        }
    }
}
