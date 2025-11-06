// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for SessionUnitTest
    /// </summary>
    [TestClass]
    public class SessionUnitTest : DbScopedXEventTestBase
    {
                
        /// <summary>
        /// Tests the default value of a newly create session.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestDefaultValue()
        {
            Session session = new Session();
            Assert.AreEqual(-1, session.ID);
            Assert.IsNull(session.Parent);
            Assert.IsNull(session.Name);
            Assert.IsFalse(session.IsRunning);
            Assert.AreEqual(Session.EventRetentionModeEnum.AllowSingleEventLoss, session.EventRetentionMode);
            Assert.AreEqual(Session.DefaultDispatchLatency, session.MaxDispatchLatency);
            Assert.AreEqual(Session.DefaultMaxMemory, session.MaxMemory);
            Assert.AreEqual(0, session.MaxEventSize);
            Assert.AreEqual(Session.MemoryPartitionModeEnum.None, session.MemoryPartitionMode);
            Assert.IsFalse(session.TrackCausality);
            Assert.IsFalse(session.AutoStart);
            Assert.AreEqual(Session.NotStarted, session.StartTime);

            session = new Session(store, "ut1");
            Assert.AreEqual(-1, session.ID);
            Assert.AreEqual(store, session.Parent);
            Assert.AreEqual("ut1", session.Name);
            Assert.IsFalse(session.IsRunning);
            Assert.AreEqual(Session.EventRetentionModeEnum.AllowSingleEventLoss, session.EventRetentionMode);
            Assert.AreEqual(Session.DefaultDispatchLatency, session.MaxDispatchLatency);
            Assert.AreEqual(Session.DefaultMaxMemory, session.MaxMemory);
            Assert.AreEqual(0, session.MaxEventSize);
            Assert.AreEqual(Session.MemoryPartitionModeEnum.None, session.MemoryPartitionMode);
            Assert.IsFalse(session.TrackCausality);
            Assert.IsFalse(session.AutoStart);
            Assert.AreEqual(Session.NotStarted, session.StartTime);
        }

        [TestMethod]
        public void DbScoped_Session_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestAddRemoveEvent();
                TestCollectionInitialization();
                TestCreateAlterDropSession();
                TestCreateAndAlter();
                TestDummySessionAlter();
                TestModifyPredicateOnExistingSession();
                TestNoBlockEvent();
                TestRemoveActionFromExistingSession();
                TestSQLInjection();
                TestScriptCreateDrop();
                TestStartStopExistingSession();
                TestValidateByWrongMethodName();
                TestValidateEvent();
                TestValidateName();
                TestSessionNameUpdate();
            }));
        }

        /// <summary>
        /// Tests create, alter & drop session.
        /// </summary>
        public void TestCreateAlterDropSession()
        {
            string name = "TestCreateAlterDropSession-A6D975E5-017B-434f-9C99-E500451F27B7";
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }

            Session session = store.CreateSession(name);
            Event evt = session.AddEvent("sqlserver.sp_statement_starting");
            evt.AddAction("sqlserver.sql_text");
            Target target = session.AddTarget("package0.ring_buffer");
            target.TargetFields["max_memory"].Value = 8192;
            session.Create();


            string sqlCount = "SELECT COUNT(*) FROM sys.database_event_sessions where name='" + name + "'";
            Assert.AreEqual(1, (store.SfcConnection as ServerConnection).ExecuteScalar(sqlCount));
            session = store.Sessions[name];
            Assert.AreEqual(1, session.Events.Count);
            Assert.AreEqual(1, session.Events["sqlserver.sp_statement_starting"].Actions.Count);
            Assert.IsTrue(session.Events["sqlserver.sp_statement_starting"].Actions.Contains("sqlserver.sql_text"));
            Assert.AreEqual(1, session.Targets.Count);
            Assert.IsTrue(session.Targets.Contains("package0.ring_buffer"));

            evt = session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.sql_statement_completed"));
            evt.AddAction(store.ObjectInfoSet.Get<ActionInfo>("sqlserver.database_name"));
            session.Alter();


            session = store.Sessions[name];
            Assert.AreEqual(2, session.Events.Count);
            Assert.AreEqual(1, session.Events["sqlserver.sql_statement_completed"].Actions.Count);
            Assert.IsTrue(session.Events["sqlserver.sql_statement_completed"].Actions.Contains("sqlserver.database_name"));
            Assert.AreEqual(1, session.Targets.Count);

            session.Drop();
            Assert.IsNull(store.Sessions[name]);
            Assert.AreEqual(0, (store.SfcConnection as ServerConnection).ExecuteScalar(sqlCount));
        }

        public void TestStartStopExistingSession()
        {
            if (store.Sessions.Count > 0)
            {
                foreach (Session session in store.Sessions)
                {
                    bool running = session.IsRunning;
                    RevertSessionState(session);
                    Assert.AreEqual(!running, session.IsRunning);

                    RevertSessionState(session);
                    Assert.AreEqual(running, session.IsRunning);
                }
            }
        }

        private bool RevertSessionState (Session session)
        {
            if (session.IsRunning)
            {
                session.Stop();
            }
            else
            {
                session.Start();
            }

            return session.IsRunning;
        }

        /// <summary>
        /// Tests start & stop session.
        /// </summary>
        public void TestStartStopSession()
        {
            string name = "TestStartStopSession-A6D975E5-017B-434f-9C99-E500451F27B7";
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }

            Session session = store.CreateSession(name);
            Event evt = session.AddEvent("sqlserver.sp_statement_starting");
            evt.AddAction("sqlserver.sql_text");
            Target target = session.AddTarget("package0.ring_buffer");
            target.TargetFields["max_memory"].Value = 8192;
            session.Create();
            Assert.IsFalse(session.IsRunning);

            session.Start();
            Assert.AreEqual(true, session.IsRunning);
            session.Refresh();
            string sqlCount = "SELECT COUNT(*) FROM sys.dm_xe_database_sessions where name='" + name + "'";
            Assert.AreEqual(1, (store.SfcConnection as ServerConnection).ExecuteScalar(sqlCount));
            Assert.IsTrue(session.IsRunning);

            session.Stop();
            Assert.AreEqual(false, session.IsRunning);
            Assert.AreEqual(0, (store.SfcConnection as ServerConnection).ExecuteScalar(sqlCount));
            session.Refresh();
            Assert.IsFalse(session.IsRunning);

            session.Drop();
        }


        [TestMethod]
        [TestCategory("Unit")]
        public void TestValidateByWrongMethodName()
        {
            Session session = new Session();
            Assert.Throws<XEventException>(() => session.Validate("invalid"), "Validate should throw");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TestValidateName()
        {
            Session session = new Session();
            Assert.Throws<XEventException>(() => session.Validate(ValidationMethod.Create), "Validate should throw");
        }

        public void TestValidateEvent()
        {
            Session session = store.CreateSession("ut1");
            Assert.Throws<XEventException>(() => session.Validate(ValidationMethod.Create), "Validate should throw");
        }       

        public void TestScriptCreateDrop()
        {
            string name = "ut1";
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }

            Session session = store.CreateSession(name);
            bool hasException = false;
            try
            {
                session.ScriptCreate();
            }
            catch (XEventException)
            {
                hasException = true;
            }
            Assert.IsTrue(hasException);

            Event evt = session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting"));
            evt.AddAction(store.ObjectInfoSet.Get <ActionInfo>("sqlserver.sql_text"));
            Target target = session.AddTarget(store.RingBufferTargetInfo);
            target.TargetFields["max_memory"].Value = 8192;

            string sql = session.ScriptCreate().ToString();
            Assert.AreEqual(@"CREATE EVENT SESSION [ut1] ON DATABASE 
ADD EVENT sqlserver.rpc_starting(
    ACTION(sqlserver.sql_text))
ADD TARGET package0.ring_buffer(SET max_memory=(8192))
", sql);
            sql = session.ScriptDrop().ToString();
            sql = sql.Remove(sql.LastIndexOf("\r\n", StringComparison.Ordinal)).TrimEnd();
            Assert.AreEqual("DROP EVENT SESSION [" + name + "] ON DATABASE", sql);
        }

        public void When_session_has_multiple_targets_ScriptCreate_properly_delimits_the_list()
        {
            var sessionName = "testSession" + Guid.NewGuid();
            var session = store.CreateSession(sessionName );
            session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting"));
            session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.sp_statement_starting"));
            session.AddTarget(store.RingBufferTargetInfo);
            session.AddTarget(store.EventFileTargetInfo);
            var sqlScript = session.ScriptCreate().ToString();
            Assert.That(sqlScript, Is.EqualTo(@"CREATE EVENT SESSION [" + sessionName + @"] ON DATABASE 
ADD EVENT sqlserver.rpc_starting,
ADD EVENT sqlserver.sp_statement_starting
ADD TARGET package0.event_file,
ADD TARGET package0.ring_buffer
"), sqlScript, "Unexpected create script");

        }

        public void TestAddRemoveEvent()
        {
            Session session = store.CreateSession("ut1");
            Event evt1 = session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting"));
            Event evt2 = session.AddEvent("sqlserver.rpc_completed");
            Assert.IsTrue(session.RemoveEvent(evt1));
            Assert.IsTrue(session.RemoveEvent(evt2));
            Assert.AreEqual(0, session.Events.Count);
        }

        /// <summary>
        /// bug 306447
        /// Tests whether all children's state are correct after Session.Create().
        /// </summary>
        public void TestCreateAndAlter()
        {
            Session session = store.CreateSession("SessionUnitTest_TestCreateAndAlter_1");
            Event evt = session.AddEvent("sqlserver.rpc_starting");
            EventField eventField = evt.EventFields["collect_statement"];
            eventField.Value = 1;
            XEvent.Action action = evt.AddAction("sqlserver.sql_text");
            PredOperand op = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.database_id"));
            PredValue value = new PredValue(5);
            evt.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, op, value);
            Target target = session.AddTarget(store.RingBufferTargetInfo);
            TargetField targetField = target.TargetFields["max_memory"];
            targetField.Value = 8192;
            Assert.AreEqual(SfcObjectState.Pending, session.State);
            Assert.AreEqual(SfcObjectState.Pending, evt.State);
            Assert.AreEqual(SfcObjectState.Pending, eventField.State);
            Assert.AreEqual(SfcObjectState.Pending, action.State);
            Assert.AreEqual(SfcObjectState.Pending, target.State);
            Assert.AreEqual(SfcObjectState.Pending, targetField.State);
            session.Create();
            Assert.AreEqual(SfcObjectState.Existing, session.State);
            Assert.AreEqual(SfcObjectState.Existing, evt.State);
            Assert.AreEqual(SfcObjectState.Existing, eventField.State);
            Assert.AreEqual(SfcObjectState.Existing, action.State);
            Assert.AreEqual(SfcObjectState.Existing, target.State);
            Assert.AreEqual(SfcObjectState.Existing, targetField.State);
            session.AddTarget(store.EventCounterTargetInfo);
            session.Alter();
            session.Drop();
        }


        /// <summary>
        /// Tests the dummy session logic when altering.
        /// </summary>
        public void TestDummySessionAlter()
        {
            string name = "SessionUnitTest_TestDummySessionAlter_1";
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }

            Session session = store.CreateSession(name);
            Event evt = session.AddEvent("sqlserver.rpc_starting");
            EventField eventField = evt.EventFields["collect_statement"];
            eventField.Value = 1;
            XEvent.Action action = evt.AddAction("sqlserver.sql_text");
            PredOperand op = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.database_id"));
            PredValue value = new PredValue(5);
            evt.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, op, value);
            Target target = session.AddTarget(store.RingBufferTargetInfo);
            TargetField targetField = target.TargetFields["max_memory"];
            targetField.Value = 8192;
            
            session.Create();

            bool hasException = false;
            try
            {
                evt.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, op, new PredValue("abcd"));
                session.Alter();
            }
            catch (XEventException)
            {
                hasException = true;
                //expect exception here
            }
            session.Drop();
            Assert.IsTrue(hasException);
        }

        /// <summary>
        /// Tests the bug actions can't be removed from existing session.
        /// </summary>
        public void TestRemoveActionFromExistingSession()
        {
            string name = "[SessionUnitTest_TestRemoveActionFromExistingSession_1]";
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }

            string acquired = "sqlserver.rpc_starting";
            string released = "sqlserver.rpc_completed";
            string event_sequence = "package0.event_sequence";
            string username = "sqlserver.username";
            string ring = "package0.ring_buffer";

            Session session = store.CreateSession(name);
            Event evtAcquired = session.AddEvent(acquired);
            XEvent.Action action1 = evtAcquired.AddAction(event_sequence);
            XEvent.Action action2 = evtAcquired.AddAction(username);
            Event evtReleased = session.AddEvent(released);
            Target target = session.AddTarget(ring);
            session.Create();

            evtAcquired.RemoveAction(action1);
            session.Validate(ValidationMethod.Alter);
            evtAcquired.AddAction(event_sequence);    //Recreate
            evtAcquired.RemoveAction(action2);  //ToBeDropped
            session.RemoveEvent(evtReleased);

            string script = session.ScriptAlter().ToString();
            Assert.IsTrue(script.Contains(event_sequence));
            Assert.IsFalse(script.Contains(username));
            Assert.IsTrue(script.Contains(released));
            Assert.IsFalse(script.Contains(ring));

            session.Alter();
            session.Drop();
        }


        /// <summary>
        /// to verify bug 309036 has been fixed
        /// Tests a pending session can add the same event with an existing session who has the same name
        /// </summary>
        public void TestCollectionInitialization()
        {
            Session session = store.Sessions["SessionUnitTest_TestCollectionInitialization"];
            if (session != null)
            {
                session.Drop();
            }

            session = store.CreateSession("SessionUnitTest_TestCollectionInitialization");
            EventInfo eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting");
            session.AddEvent(eventInfo);
            session.Create();
            Session pendingSession = store.CreateSession("SessionUnitTest_TestCollectionInitialization");
            pendingSession.AddEvent(eventInfo);
            
            session.Drop();
        }

        /// <summary>
        /// Tests the SQL injection.
        /// </summary>
        public void TestSQLInjection()
        {
            string sessionName = "ut_a'; create event session [ut_b] on database add event sqlserver.error_reported";
            Session session = new Session(store, sessionName);
            session.AddEvent("sqlserver.lock_deadlock");
            session.Create();

            Assert.IsTrue(store.Sessions.Contains("ut_a'; create event session [ut_b] on database add event sqlserver.error_reported"));
            Assert.IsFalse(store.Sessions.Contains("ut_b"));

            session.Drop();
        }

        /// <summary>
        /// Events with "NO_BLOCKING"  capability cannot be added to a session with the EventRetentionMode property set to "NO_EVENT_LOSS"
        /// This test validates the same
        /// </summary>
        
        public void TestNoBlockEvent()
        {
            string sessionName = "sessionunittest_testnoblockevents-" + System.Guid.NewGuid();

            if (store.Sessions[sessionName] != null)
            {
                store.Sessions[sessionName].Drop();
            }

            // create session object
            Session session = store.CreateSession(sessionName);

            // set options
            session.EventRetentionMode = Session.EventRetentionModeEnum.NoEventLoss;


            // add an event with no_block capability
            session.AddEvent("wait_info_external");

            // try creating the session => should throw a validation exception
            Assert.Throws<XEventException> (() =>
            session.Create()
            , "Create should throw");
        }                    

        public void TestModifyPredicateOnExistingSession()
        {
            string name = "SessionUnitTest_Predicate_1";
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }
            Session session = store.CreateSession(name);
            Event evt = session.AddEvent("sqlserver.rpc_starting");
            PredValue value = new PredValue(1756);
            PredOperand operand = new PredOperand(store.Packages["package0"].PredSourceInfoSet["current_thread_id"]);
            PredCompareExpr expr = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
            evt.Predicate = expr;
            session.Create();

            evt.Predicate = null;
            Assert.IsNull(evt.Predicate);
            session.Alter();
        }

        public void TestSessionNameUpdate()
        {
            var initialCount = store.Sessions.Count;
            string guid = Guid.NewGuid().ToString();
            string sessionName = "XEventSessionTest" + guid;

            Session session = store.CreateSession(sessionName);
            Session session2 = null;
            Assert.AreEqual("XEventSessionTest" + guid, session.Name);
            session.AddEvent("sqlserver.lock_deadlock");
            try
            {
                session.Create();

                session2 = store.CreateSession(sessionName);
                Assert.AreEqual("XEventSessionTest" + guid, session.Name);
                session2.AddEvent("sqlserver.lock_deadlock");

                // Try to create another session with same name and fail
                var ex = Assert.Throws<SfcCRUDOperationFailedException>(() => session2.Create(), "Creating XEvent session with same name should throw an exception");
                Assert.That(ex.InnerException.InnerException.Message, Contains.Substring("  Choose a unique name for the event session."),
                            "Unique name for XEvent exception should be thrown");

                guid = Guid.NewGuid().ToString();
                session2.Name = "XEventSessionTest2" + guid;
                var expectedCreateString = $@"CREATE EVENT SESSION [{session2.Name}]";

                session2.Create();

                Assert.AreEqual(initialCount + 2, store.Sessions.Count, "Number of sessions created should be 2");
                Assert.IsTrue(store.Sessions[session2.Name] != null, "New session not present in Sessions collection");
                Assert.That(session2.ScriptCreate().ToString().Contains(expectedCreateString), "Generated Create script is not same as expected string!");
            }
            finally
            {
                session.Drop();
                session2?.Drop();
            }
        }
    }
}
