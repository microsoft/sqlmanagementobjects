// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NUnit.Framework;
using Assert = NUnit.Framework.Assert;


namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for EventFieldUnitTest
    /// </summary>
    [TestClass]
    public class EventFieldUnitTest : DbScopedXEventTestBase
    {
        private Event fileReadEvent;

        [TestMethod]
        public void DBScoped_EventField_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                fileReadEvent =
                    store.CreateSession("ut1").AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting"));
                TestConstructors();
                TestFields();
                TestInvalidParameter();
                TestNonSettingEventField();
                TestProperties();                
            }));
        }

        /// <summary>
        /// Tests the constructors.
        /// </summary>
        public void TestConstructors()
        {            
            EventField field = new EventField();
            Assert.IsNotNull(field);
            Assert.AreEqual(-1, field.ID);
            var eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting");
            var columnInfoSet = eventInfo.EventColumnInfoSet["collect_statement"];
            field = new EventField(fileReadEvent, columnInfoSet );
            Assert.IsNotNull(field);
            Assert.AreEqual(fileReadEvent, field.Parent);
            Assert.AreEqual("collect_statement", field.Name);
        }

        /// <summary>
        /// Tests the properties.
        /// </summary>
        public void TestProperties()
        {
            string name = "TestProperties_CFAC840D606541aaBA48635A80CDB5B5";
            string sqlCreate = "CREATE EVENT SESSION " + name + " ON DATABASE ADD EVENT sqlserver.rpc_starting(SET collect_statement=1)";
            string sqlDrop = "DROP EVENT SESSION " + name + " ON DATABASE";
            try
            {
                if (store.Sessions[name] != null)
                {
                    (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlDrop);
                }
                (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlCreate);
                Event evt = store.Sessions[name].Events["sqlserver.rpc_starting"];
                EventFieldCollection coll = evt.EventFields;
                Assert.That(coll.OfType<EventField>().Select(f => f.Name),
                    Is.EquivalentTo(new string[] {"collect_data_stream", "collect_statement"}), "Unexpected EventFieldCollection");
                EventField field = coll["collect_statement"];
                Assert.IsNotNull(field);
                Assert.AreEqual("collect_statement", field.Name);
                Assert.AreEqual(1, field.Value);
                Assert.IsNotNull(field.Description);
            }
            finally
            {
                (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlDrop);
            }
        }



        /// <summary>
        /// Tests getting the non-setting event field when multiple files exist.
        /// </summary>
        public void TestNonSettingEventField()
        {
            string name = "TestNonSettingEventField";
            string sqlCreate = "CREATE EVENT SESSION " + name + " ON DATABASE ADD EVENT sqlserver.sp_statement_starting(SET collect_statement=1)";
            string sqlDrop = "DROP EVENT SESSION " + name + " ON DATABASE";
            try
            {
                if (store.Sessions[name] != null)
                {
                    (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlDrop);
                }
                (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlCreate);
                Event evt = store.Sessions[name].Events["sqlserver.sp_statement_starting"];
                EventFieldCollection coll = evt.EventFields;
                Assert.AreEqual(2, coll.Count);
                EventField field = coll["collect_statement"];
                Assert.IsNotNull(field);
                Assert.AreEqual(1, field.Value);

                //value for the non-setting field are null
                field = coll["collect_object_name"];
                Assert.IsNotNull(field);
                Assert.AreEqual(null, field.Value);
            }
            finally
            {
                (store.SfcConnection as ServerConnection).ExecuteNonQuery(sqlDrop);
            }
        }

        /// <summary>
        /// Tests all customized columns have been created as fields.
        /// </summary>
        public void TestFields()
        {            
            EventFieldCollection coll = fileReadEvent.EventFields;
            Assert.AreEqual(2, coll.Count);
            Assert.IsTrue(coll.Contains("collect_statement"));
        }

        /// <summary>
        /// Tests invalid parameter for constructor
        /// </summary>
        public void TestInvalidParameter()
        {

            Assert.Throws<ArgumentNullException>(() =>
                new EventField(fileReadEvent, null), "EventField constructor should throw");

            Assert.Throws<ArgumentNullException>(() =>
                new EventField(null,
                    store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting").EventColumnInfoSet[
                        "collect_data_stream"]), "EventField constructor should throw");
        }

    }

}
