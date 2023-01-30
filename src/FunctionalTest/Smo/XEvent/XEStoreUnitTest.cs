// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Xml;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{

    /// <summary>
    /// Summary description for XEStoreUnitTest
    /// </summary>
    [TestClass]
    public class XEStoreUnitTest : DbScopedXEventTestBase
    {
        [TestMethod]
        public void DbScoped_XeStore_Tests()
        {
            ExecuteFromDbPool(PoolName, (db) => ExecuteTest(db, () =>
           {
               TestActionInTemplate();
               TestComplicatePredicateInTemplate();
               TestCreateSession();
               TestProperties();
               TestParameterInTemplate();
               TestProperties();
               TestPredicateInTemplate();
           }));
        }

        public void TestProperties()
        {
            Assert.AreNotEqual(0, store.Name.Length);
            Assert.IsTrue(store.Sessions.Count >= 0);
            Assert.IsTrue(store.Packages.Count > 0);
            Assert.IsTrue(store.RunningSessionCount >= 0);
        }


        public void TestCreateSession()
        {
            string name = "ut1";
            Session session = store.CreateSession(name);
            Assert.IsNotNull(session);
            Assert.AreEqual(store, session.Parent);
            Assert.AreEqual(name, session.Name);
        }


        public void TestPredicateInTemplate()
        {
            Session session = store.CreateSession("XEStoreUnitTest_TestPredicateInTemplate");
            EventInfo eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_info");
            Event evt = session.AddEvent(eventInfo);
            PredOperand operand = new PredOperand(eventInfo.DataEventColumnInfoSet["duration"]);
            PredValue value = new PredValue(7);
            PredCompareExpr pred = new PredCompareExpr(PredCompareExpr.ComparatorType.NE, operand, value);
            evt.Predicate = pred;
            session.Create();

            string path = "XEStoreUnitTest_TestPredicateInTemplate.xml";
            DatabaseXEStore.SaveSessionToTemplate(session, path, true);
            session.Drop();

            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList nodeList = doc.GetElementsByTagName("predicate");
            XmlElement elem = (XmlElement)nodeList[0];
            string content = elem.InnerXml;
            Assert.IsTrue(content.Contains("leaf"));
            Assert.IsTrue(content.Contains("comparator"));
            Assert.IsTrue(content.Contains("event"));
            Assert.IsTrue(content.Contains("value"));

            session = store.CreateSessionFromTemplate("XEStoreUnitTest_TestPredicateInTemplate2", path);
            Assert.AreEqual("([package0].[not_equal_uint64]([duration],(7)))", session.Events["sqlos.wait_info"].PredicateExpression);

            try
            {
                File.Delete(path);
            }
            catch
            { }
        }

        public void TestComplicatePredicateInTemplate()
        {
            Session session = store.CreateSession("XEStoreUnitTest_TestComplicatePredicateInTemplate");
            EventInfo eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlserver.lock_acquired");
            Event evt = session.AddEvent(eventInfo);
            PredOperand operand = new PredOperand(eventInfo.DataEventColumnInfoSet["mode"]);
            PredValue value = new PredValue(7);
            PredCompareExpr pred1 = new PredCompareExpr(PredCompareExpr.ComparatorType.NE, operand, value);
            operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.client_app_name"));
            value = new PredValue("profiler");
            PredFunctionExpr pred2 = new PredFunctionExpr(store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"].PredCompareInfoSet["equal_i_sql_unicode_string"], operand, value);
            PredLogicalExpr pred = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.And, pred1, pred2);
            evt.Predicate = pred;
            session.Create();

            string path = "XEStoreUnitTest_TestComplicatePredicateInTemplate.xml";
            DatabaseXEStore.SaveSessionToTemplate(session, path, true);
            session.Drop();

            session = store.CreateSessionFromTemplate("XEStoreUnitTest_TestComplicatePredicateInTemplate2", path);
            Assert.AreEqual("(([package0].[not_equal_uint64]([mode],(7))) AND ([sqlserver].[equal_i_sql_unicode_string]([sqlserver].[client_app_name],N'profiler')))",
                session.Events["sqlserver.lock_acquired"].PredicateExpression);

            try
            {
                File.Delete(path);
            }
            catch
            { }
        }

        public void TestActionInTemplate()
        {
            Session session = store.CreateSession("XEStoreUnitTest_TestActionInTemplate");
            EventInfo eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlserver.lock_deadlock");
            Event evt = session.AddEvent(eventInfo);
            evt.AddAction("sqlserver.sql_text");
            session.Create();

            string path = "XEStoreUnitTest_TestActionInTemplate.xml";
            DatabaseXEStore.SaveSessionToTemplate(session, path, true);
            try
            {
                session.Drop();
                session = store.CreateSessionFromTemplate("XEStoreUnitTest_TestActionInTemplate2", path);
                Assert.IsNotNull(session.Events["sqlserver.lock_deadlock"].Actions["sqlserver.sql_text"]);
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }

        public void TestParameterInTemplate()
        {
            Session session = store.CreateSession("XEStoreUnitTest_TestParameterInTemplate");
            EventInfo eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting");
            Event evt = session.AddEvent(eventInfo);
            Target target = session.AddTarget(store.RingBufferTargetInfo);
            target.TargetFields["max_memory"].Value = 8192;
            session.Create();

            string path = "XEStoreUnitTest_TestParameterInTemplate.xml";
            DatabaseXEStore.SaveSessionToTemplate(session, path, true);
            try
            {
                session.Drop();

                session = store.CreateSessionFromTemplate("XEStoreUnitTest_TestParameterInTemplate2", path);
                Assert.AreEqual("8192", session.Targets["package0.ring_buffer"].TargetFields["max_memory"].Value);
                session.Create();
                session.Drop();
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }
    }
}
