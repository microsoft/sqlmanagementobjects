// Copyright (c) Microsoft.
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
    /// Unit Test for Event class.
    /// </summary>
    [TestClass]
    public class EventUnitTest : DbScopedXEventTestBase
    {

        #region help T-SQL
        //create a dummy session, add one dummy event.
        private string createSessionStmt = "CREATE EVENT SESSION eventUnitTest ON DATABASE ADD EVENT sqlserver.rpc_starting";
        private string dropSessionStmt = "DROP EVENT SESSION eventUnitTest ON DATABASE";

        //create a dummy session, add one event with a customizable field and a predicate.
        private string createSessionWithFieldStmt = "CREATE EVENT SESSION eventUnitTest ON DATABASE ADD EVENT" +
            " sqlserver.rpc_starting(SET collect_statement = 1 action (package0.event_sequence) where package0.current_thread_id = 4)";
        private string dropSessionWithFieldStmt = "DROP EVENT SESSION eventUnitTest ON DATABASE";
        
        #endregion

        [TestMethod]
        public void DbScoped_Event_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestEventRemoveAction();
                TestEventAddAction();
                TestEventAddActionInfo();
                TestEventAddActionInfoNull();
                TestEventAddActionName();
                TestEventAddActionNameEmpty();
                TestEventAddActionNameNull();
                TestEventCtorEmpty();
                TestEventCtorEventInfo();
                TestEventCtorEventInfoNull();
                TestEventCtorName();
                TestEventCtorNameModule();
                TestEventCtorNameModuleWrongName();
                TestEventCtorNameWrongEventName();
                TestEventCtorNameWrongFormat();
                TestEventCtorNameWrongPackageName();
                TestEventCtorNullXEStore();
                TestEventGetActions();
                TestEventGetEventFields();
                TestEventGetPredicateExisting();
                TestEventGetPredicateExpression();
                TestEventGetPredicatePending();
                TestEventProperties();
                TestEventRemoveActionNull();
                TestEventSetEventFieldPending();
                TestEventSetInfo();
                TestEventSetInfoNull();
                TestEventSetInfoExisting();
                TestEventSetNameForExistingEvent();
                TestEventSetPredicateExisting();
                TestEventSetPredicateExpressionExisting();
                TestEventSetPredicateExpressionPending();
                TestEventSetPredicatePending();
                TestEventRemoveActionFromCollection();                
            }));
        }

        /// <summary>
        /// Test the constructor with Name.
        /// </summary>
        public void TestEventCtorName()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Assert.IsNotNull(ev);
                Assert.AreEqual("sqlserver.sp_statement_starting", ev.Name);
                Assert.AreEqual(0, ev.Actions.Count);
                Assert.IsNull(ev.PredicateExpression);
                Assert.AreEqual("sqlserver", ev.PackageName);
                Assert.AreEqual(-1, ev.ID);                
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Test the constructor with full qulified Name.
        /// </summary>
        public void TestEventCtorNameModule()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Assert.IsNotNull(ev);
                Assert.AreEqual("sqlserver.sp_statement_starting", ev.Name);
                Assert.AreEqual(0, ev.Actions.Count);
                Assert.IsNull(ev.PredicateExpression);
                Assert.AreEqual("sqlserver", ev.PackageName);
                Assert.AreEqual(-1, ev.ID);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Test the constructor with wrong module name.
        /// </summary>
        public void TestEventCtorNameModuleWrongName()
        {
            ExecNonQuery(createSessionStmt);
            try
            {
                Assert.Throws<XEventException>(() =>
                {
                    Session session = store.Sessions["eventUnitTest"];
                    Event ev = new Event(session,
                        "[CE79811F-1A80-40E1-8F5D-111111111111].sqlserver.sp_statement_starting");
                    Assert.IsNotNull(ev);
                    Assert.AreEqual("sqlserver.sp_statement_starting", ev.Name);
                    Assert.AreEqual(0, ev.Actions.Count);
                    Assert.IsNull(ev.PredicateExpression);
                    Assert.AreEqual("sqlserver", ev.PackageName);
                    Assert.AreEqual(-1, ev.ID);
                }, "Event constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Test the constructor with EventInfo
        /// </summary>
        public void TestEventCtorEventInfo()
        {
            Session session = new Session();
            EventInfo info = store.ObjectInfoSet.Get<EventInfo>("sqlserver.sp_statement_starting");
            Event ev = new Event(session, info);

            Assert.IsNotNull(ev);
            Assert.AreEqual("sqlserver.sp_statement_starting", ev.Name);
            Assert.AreEqual("sqlserver", ev.PackageName);
            Assert.IsNull(ev.PredicateExpression);
            Assert.AreEqual(-1, ev.ID);
            Assert.AreEqual(session, ev.Parent);
            Assert.AreEqual("Occurs when a statement inside a stored procedure has started.", ev.Description);
        }

        /// <summary>
        /// Test the empty constructor.
        /// </summary>
        public void TestEventCtorEmpty()
        {
            Event ev = new Event();
            Assert.IsNull(ev.Name);
            Assert.IsNull(ev.PredicateExpression);
            Assert.IsNull(ev.Description);
            ev.PredicateExpression = "a = 5";
            Assert.AreEqual("a = 5", ev.PredicateExpression);
            Assert.AreEqual(-1, ev.ID);
        }

        /// <summary>
        /// Test the SetEventInfo.
        /// </summary>
        public void TestEventSetInfo()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Event ev = new Event();
                Assert.IsNull(ev.Name);

                EventInfo info = store.ObjectInfoSet.Get<EventInfo>("sqlserver.sp_statement_starting");
                ev.SetEventInfo(info);
                Assert.IsNotNull(ev);
                Assert.AreEqual("sqlserver.sp_statement_starting", ev.Name);
                Assert.AreEqual("sqlserver", ev.PackageName);
                Assert.IsNull(ev.PredicateExpression);

                //ctor with name, then change the eventinfo
                ev = new Event(store.Sessions["eventUnitTest"], "sqlserver.rpc_starting");
                Assert.AreEqual("sqlserver.rpc_starting", ev.Name);

                ev.SetEventInfo(info);
                Assert.AreEqual("sqlserver.sp_statement_starting", ev.Name);
                Assert.AreEqual("sqlserver", ev.PackageName);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Test SetEventInfo when input is null.
        /// </summary>
        public void TestEventSetInfoNull()
        {
            Event ev = new Event();
            Assert.Throws<ArgumentNullException>(() =>
                ev.SetEventInfo(null), "SetEventInfo should throw");
        }

        /// <summary>
        /// Test SetEventInfo for an existing Event.
        /// </summary>
        public void TestEventSetInfoExisting()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = session.Events["sqlserver.rpc_starting"];

                Assert.AreEqual("sqlserver.rpc_starting", ev.Name);
                EventInfo info = store.ObjectInfoSet.Get<EventInfo>("sqlserver.sp_statement_starting");
                Assert.Throws<XEventException>(() =>
                    ev.SetEventInfo(info), "SetEventInfo should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Test the constructor with null DatabaseXEStore.
        /// </summary>
        public void TestEventCtorNullXEStore()
        {
            Session session = new Session();
            Assert.Throws<NullReferenceException>(() => new Event(session, "sqlserver.rpc_starting"),
                "Event constructor should throw");
        }


        /// <summary>
        /// Test the constructor with malformed name.
        /// </summary>
        public void TestEventCtorNameWrongFormat()
        {
            ExecNonQuery(createSessionStmt);
 
            try
            {
                Session session = store.Sessions["eventUnitTest"];
                Assert.Throws<XEventException>(() => new Event(session, "a.b.c.d"), "Event constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }            
        }

        /// <summary>
        /// Test the constructor with wrong package name.
        /// </summary>
        public void TestEventCtorNameWrongPackageName()
        {
            ExecNonQuery(createSessionStmt);
 
            try
            {
                Session session = store.Sessions["eventUnitTest"];
                Assert.Throws<XEventException>(
                    () => new Event(session, "wrongpackage.allocation_ring_buffer_recorded"),
                    "Event constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Test the constructor when Event Name is wrong.
        /// </summary>
        public void TestEventCtorNameWrongEventName()
        {
            ExecNonQuery(createSessionStmt);
            try
            {                
                Session session = store.Sessions["eventUnitTest"];
                Assert.Throws<XEventException>(
                    () => new Event(session, "sqlserver.nosuchevent"), "Event constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Test the constructor when EventInfo is null.
        /// </summary>
        public void TestEventCtorEventInfoNull()
        {
            Session session = new Session();
            //the event name is not correct, return value is null
            EventInfo info = store.Packages["package0"].EventInfoSet["nosuchevent"];
            Assert.Throws<ArgumentNullException>(() => new Event(session, info), "Event should throw");
        }

        /// <summary>
        /// Test the set name for existing Event.
        /// </summary>
        public void TestEventSetNameForExistingEvent()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.Throws<XEventException>(() => ev.Name = "newName", "Set Name should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }            
        }

        /// <summary>
        /// Test get the Predicate Expression.
        /// </summary>
        public void TestEventGetPredicateExpression()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.AreEqual("([package0].[current_thread_id]=(4))", ev.PredicateExpression);
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }         
        }

        /// <summary>
        /// Test set predicate expression for pending object.
        /// </summary>
        public void TestEventSetPredicateExpressionPending()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Assert.IsNull(ev.PredicateExpression);

                ev.PredicateExpression = "([package0].[current_thread_id]=(4))";
                Assert.AreEqual("([package0].[current_thread_id]=(4))", ev.PredicateExpression);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }                          
        }

        /// <summary>
        /// Test set predicate for existing object.
        /// </summary>
        public void TestEventSetPredicateExpressionExisting()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.AreEqual("([package0].[current_thread_id]=(4))", ev.PredicateExpression);
                ev.PredicateExpression = "new predicate";
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }           
        }

        /// <summary>
        /// Test set Predicate object for pending event.
        /// </summary>
        public void TestEventSetPredicatePending()
        {

            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Assert.IsNull(ev.PredicateExpression);

                PredValue value = new PredValue(4);
                PredOperand operand = new PredOperand(store.Package0Package.PredSourceInfoSet["current_thread_id"]);
                PredCompareExpr expr = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
                ev.Predicate = expr;
                Assert.AreEqual("([package0].[current_thread_id]=(4))", ev.PredicateExpression);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }  

        }


        /// <summary>
        /// Test get the Predicate Expression for an existing event.
        /// </summary>
        public void TestEventGetPredicateExisting()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                PredExpr predicate = ev.Predicate;
                Assert.IsNotNull(predicate);

               
                PredFunctionExpr funcExpre = predicate as PredFunctionExpr;
                Assert.IsNotNull(funcExpre);

                PredCompareInfo compareInfo = funcExpre.Operator;
                Assert.IsNotNull(compareInfo);
                Assert.AreEqual("equal_uint64", compareInfo.Name);
                Assert.AreEqual("package0", compareInfo.Parent.Name);

                Assert.AreEqual("4", funcExpre.Value.ToString());

                PredOperand operand = funcExpre.Operand as PredOperand;
                Assert.IsNotNull(operand);
                PredSourceInfo predsource = operand.OperandObject as PredSourceInfo;
                Assert.IsNotNull(predsource);
                Assert.AreEqual("current_thread_id", predsource.Name);
                Assert.AreEqual("package0", predsource.Parent.Name);
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }
        }

        /// <summary>
        /// Test get the Predicate Expression for a pending event.
        /// </summary>
        public void TestEventGetPredicatePending()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Session session = store.Sessions["eventUnitTest"];

                Event ev = new Event(session, store.ObjectInfoSet.Get<EventInfo>("sqlserver.sp_statement_starting"));
                Assert.IsNotNull(ev);
                Assert.IsNull(ev.Predicate);

                PredValue value = new PredValue(4);
                PredOperand operand = new PredOperand(store.Package0Package.PredSourceInfoSet["current_thread_id"]);
                PredCompareExpr expr = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
                ev.Predicate = expr;

                Assert.IsNotNull(ev.Predicate);
                Assert.AreEqual(expr, ev.Predicate);

                //Test set the predicate to null
                ev.Predicate = null;
                Assert.IsNull(ev.Predicate);
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }  
        }

        /// <summary>
        /// Test set Predicate object for Existing Event.
        /// </summary>
        public void TestEventSetPredicateExisting()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.AreEqual("([package0].[current_thread_id]=(4))", ev.PredicateExpression);
                PredValue value = new PredValue(4);
                PredOperand operand = new PredOperand(store.Package0Package.PredSourceInfoSet["current_thread_id"]);
                PredCompareExpr expr = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
                ev.Predicate = expr;
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }          
        }

        
        /// <summary>
        /// Test the Event Properties.
        /// </summary>
        public void TestEventProperties()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.AreEqual("sqlserver.rpc_starting", ev.Name);
                Assert.AreEqual("sqlserver", ev.PackageName);
                //only one object in the session, so the ID should be 1.
                Assert.AreEqual(1, ev.ID);
                Assert.IsNotNull(ev.IdentityKey);
                Assert.AreEqual("([package0].[current_thread_id]=(4))", ev.PredicateExpression);
                Assert.AreEqual("Occurs when a remote procedure call has started.", ev.Description);

            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }            
        }

        /// <summary>
        /// Test Get EventFiled.
        /// </summary>
        public void TestEventGetEventFields()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.IsNotNull(ev.EventFields);
                //sqlserver.rpc_starting has two customizable fields:collect_statement & collect_data_stream
                //collect_statement is set to 1 in this session
                //collect_data_stream will be null
                Assert.AreEqual(2, ev.EventFields.Count);
                Assert.AreEqual(1, ev.EventFields["collect_statement"].Value);
                Assert.AreEqual(null, ev.EventFields["collect_data_stream"].Value);                
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }          
        }

        /// <summary>
        /// Test Set EventField.
        /// </summary>
        public void TestEventSetEventFieldPending()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Session session = store.Sessions["eventUnitTest"];
                //the event is in pending state
                Event ev = new Event(session, "sqlserver.rpc_starting");
                Assert.IsNotNull(ev);
                //fields can be set when event is in pending state
                ev.EventFields["collect_data_stream"].Value = 1;
                Assert.AreEqual(1, ev.EventFields["collect_data_stream"].Value);
                //the un-set event fields should be null.
                Assert.IsNull(ev.EventFields["collect_statement"].Value);
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }          
        }

        /// <summary>
        /// Test Get Action.
        /// </summary>
        public void TestEventGetActions()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.IsNotNull(ev.Actions);
                Assert.AreEqual(1, ev.Actions.Count);
                XEvent.Action action = ev.Actions["package0.event_sequence"];
                Assert.IsNotNull(action);
            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            } 
        }

        /// <summary>
        /// Tests add action through Event.Actions.Add().
        /// </summary>
        public void TestEventAddAction()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.rpc_starting");
                XEvent.Action action = new XEvent.Action(ev, "package0.event_sequence");
                ev.Actions.Add(action);
                Assert.IsNotNull(ev.Actions);
                Assert.IsTrue(ev.Actions.Contains(action));
                Assert.AreEqual(1, ev.Actions.Count);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }         
        }

        /// <summary>
        /// Test remove Action from action collection.
        /// </summary>
        public void TestEventRemoveActionFromCollection()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.IsNotNull(ev.Actions);
                Assert.AreEqual(1, ev.Actions.Count);
                XEvent.Action action = ev.Actions["package0.event_sequence"];
                Assert.IsNotNull(action);
                //remove the existing action from the event
                ev.Actions.Remove(action);
                //the action is not really removed until Alter action on Session is called.
                //the remove action only mark the state of action, the state is not public
                Assert.AreEqual(1, ev.Actions.Count);

            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }         
        }


        /// <summary>
        /// Test Add Action through Name.
        /// </summary>
        public void TestEventAddActionName()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                //Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                ev.AddAction("package0.event_sequence");
                
                Assert.IsNotNull(ev.Actions);
                Assert.AreEqual(1, ev.Actions.Count);
                Assert.IsNotNull(ev.Actions["package0.event_sequence"]);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }           
        }

        /// <summary>
        /// Test AddAction when Name is null.
        /// </summary>
        public void TestEventAddActionNameNull()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Assert.Throws<XEventException>(() => ev.AddAction((string)null), "AddAction should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }          
        }

        /// <summary>
        /// Test Add Action when Name is empty.
        /// </summary>
        public void TestEventAddActionNameEmpty()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Assert.Throws<XEventException>(() => ev.AddAction(""), "AddAction should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }          
        }

        /// <summary>
        /// Test Add ActionInfo
        /// </summary>
        public void TestEventAddActionInfo()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");

                ActionInfo info = store.Package0Package.ActionInfoSet["event_sequence"];
                ev.AddAction(info);

                Assert.IsNotNull(ev.Actions);
                Assert.AreEqual(1, ev.Actions.Count);
                Assert.IsNotNull(ev.Actions["package0.event_sequence"]);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }          
        }

        /// <summary>
        /// Test AddActionInfo when input is null.
        /// </summary>
        public void TestEventAddActionInfoNull()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["eventUnitTest"];
                Event ev = new Event(session, "sqlserver.sp_statement_starting");
                Assert.Throws<ArgumentNullException>(() => ev.AddAction((ActionInfo)null), "AddAction should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }   
        }

        /// <summary>
        /// Test Remove Action.
        /// </summary>
        public void TestEventRemoveAction()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.IsNotNull(ev.Actions);
                Assert.AreEqual(1, ev.Actions.Count);
                XEvent.Action action = ev.Actions["package0.event_sequence"];
                Assert.IsNotNull(action);
                Assert.That(action.State, Is.EqualTo(SfcObjectState.Existing));                
                //remove the existing action from the event
                ev.RemoveAction(action);
                Assert.AreEqual(SfcObjectState.ToBeDropped, action.State);
                //the action is not really removed until Alter action on Session is called.
                //the remove action only mark the state of action, the state is not public
                Assert.AreEqual(1, ev.Actions.Count);

            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }          
        }

        /// <summary>
        /// Test RemoveAction when input is null.
        /// </summary>
        public void TestEventRemoveActionNull()
        {
            try
            {
                ExecNonQuery(createSessionWithFieldStmt);
                Event ev = store.Sessions["eventUnitTest"].Events["sqlserver.rpc_starting"];
                Assert.IsNotNull(ev);
                Assert.IsNotNull(ev.Actions);
                Assert.AreEqual(1, ev.Actions.Count);
                XEvent.Action action = ev.Actions["package0.event_sequence"];
                Assert.IsNotNull(action);
                //remove the existing action from the event
                Assert.Throws<ArgumentNullException>(() => ev.RemoveAction(null), "RemoveAction should throw");
                //the action is not really removed until Alter action on Session is called.
                //the remove action only mark the state of action, the state is not public
                Assert.AreEqual(1, ev.Actions.Count);

            }
            finally
            {
                ExecNonQuery(dropSessionWithFieldStmt);
            }          
        }

        /// <summary>
        /// Query executor.
        /// </summary>
        /// <param name="stmt"></param>
        private void ExecNonQuery(string stmt)
        { 
            (store.SfcConnection as ServerConnection).ExecuteNonQuery(stmt);
        }
    }
}
