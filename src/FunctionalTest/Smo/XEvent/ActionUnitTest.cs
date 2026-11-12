// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Unit Test for the Action class.
    /// </summary>
    [TestClass]
    public class ActionUnitTest : DbScopedXEventTestBase
    {          
        #region help T-SQL
        //create a dummy session, add one event used as parent for actions.
        private string createSessionStmt = "CREATE EVENT SESSION actionUnitTest ON DATABASE ADD EVENT sqlserver.rpc_starting";
        private string dropSessionStmt = "DROP EVENT SESSION actionUnitTest ON DATABASE";

        //create a dummy session, add one event with an action.
        private string createSessionWithActionStmt = "CREATE EVENT SESSION actionUnitTest ON DATABASE ADD EVENT sqlserver.rpc_starting(ACTION (package0.event_sequence))";
        private string dropSessionWithActionStmt = "DROP EVENT SESSION actionUnitTest ON DATABASE";
        #endregion


        /// <summary>
        /// Test the empty constructor.
        /// </summary>
        [TestMethod]
        public void TestActionCtorEmpty()
        {
            XEvent.Action action = new XEvent.Action();
            Assert.IsNotNull(action);
            Assert.IsNull(action.Name);
            Assert.IsNull(action.Parent);
            Assert.IsNull(action.Description);
        }

        /// <summary>
        /// Invokes all the Action tests
        /// </summary>
        [TestMethod]
        public void DbScoped_Action_Tests()
        {
            ExecuteFromDbPool(PoolName, (db) => ExecuteTest(db, () =>
            {
                TestActionCtorActionInfo();
                TestActionCtorName();
                TestActionCtorNameModule();
                TestActionCtorNameModuleWrongModule();
                TestActionCtorNameNullSession();
                TestActionCtorNameNullXEStore();
                TestActionCtorNullActionInfo();
                TestActionCtorWrongActionName();
                TestActionCtorWrongNameFormatLess();
                TestActionCtorWrongNameFormatMore();
                TestActionCtorWrongPackageName();
                TestActionProperties();
                TestActionSetNameForExistingAction();
            }));
        }
        /// <summary>
        /// Test the constructor with Name parameter.
        /// </summary>
        public void TestActionCtorName()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Event ev = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"];
                XEvent.Action action = new XEvent.Action(ev, "package0.event_sequence");
                Assert.IsNotNull(action);
                Assert.AreEqual(ev, action.Parent);
                Assert.AreEqual("package0.event_sequence", action.Name);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Tests when parent.Parent(Event.Parent) is not set. 
        /// </summary>
        [TestMethod]
        public void TestActionCtorNameNullSession()
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                Event ev = new Event();
                XEvent.Action action = new XEvent.Action(ev, "package0.calllstack");
            }, "Action constructor should throw");
        }

        /// <summary>
        /// Test the constructor when DatabaseXEStore is null in Event.
        /// </summary>
        public void TestActionCtorNameNullXEStore()
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                //DatabaseXEStore is null for the session
                Session session = new Session();
                Event ev = new Event(session, store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting"));
                XEvent.Action action = new XEvent.Action(ev, "package0.event_sequence");
            }, "Constructor should throw");
        }


        /// <summary>
        /// Test the constructor with a full qulified name.
        /// </summary>
        public void TestActionCtorNameModule()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Event ev = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"];
                XEvent.Action action = new XEvent.Action(ev, "[CE79811F-1A80-40E1-8F5D-7445A3F375E7].sqlserver.client_app_name");
                Assert.IsNotNull(action);
                Assert.AreEqual(ev, action.Parent);
                Assert.AreEqual("sqlserver.client_app_name", action.Name);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Test the constructor with wrong module id. Since we do not check module id by now, this will be ok.
        /// </summary>
        public void TestActionCtorNameModuleWrongModule()
        {
            try
            {
                Assert.Throws<XEventException>(() =>
                {
                    ExecNonQuery(createSessionStmt);
                    Event ev = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"];                    
                    XEvent.Action action = new XEvent.Action(ev, "40E1-8F5D-7445A3F375E7.sqlserver.client_app_name");                    
                }, "Action constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            } 
        }

        /// <summary>
        /// Test the constructor with adding just by the object name without the package name.
        /// </summary>
        public void TestActionCtorWrongNameFormatLess()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Event ev = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"];
                XEvent.Action action = new XEvent.Action(ev, "event_sequence");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
            
        }

        /// <summary>
        /// Test the constructor with malformed name.
        /// </summary>
        public void TestActionCtorWrongNameFormatMore()
        {
            try
            {
                Assert.Throws<XEventException>(() =>
                {
                    ExecNonQuery(createSessionStmt);
                    Event ev = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"];
                    XEvent.Action action = new XEvent.Action(ev, "a.b.c.d");
                }, "Action constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }                       
        }

        /// <summary>
        /// Test the constructor when the package name is wrong.
        /// </summary>
        
        public void TestActionCtorWrongPackageName()
        {
            try
            {
                Assert.Throws<XEventException>(() =>
                {
                    ExecNonQuery(createSessionStmt);
                    Event ev = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"];
                    //the package name here is not correct.
                    XEvent.Action action = new XEvent.Action(ev, "nosuchserver.client_app_name");
                }, "Action constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Test the constructor when the action Name is wrong.
        /// </summary>
        
        public void TestActionCtorWrongActionName()
        {
            try
            {
                Assert.Throws<XEventException>(() =>
                {
                    ExecNonQuery(createSessionStmt);
                    Event ev = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"];
                    //the action name here is not correct.
                    XEvent.Action action = new XEvent.Action(ev, "sqlserver.nosuchevent");
                }, "Action constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }        
        }
        
        /// <summary>
        /// Test the constructor with ActionInfo.
        /// </summary>
        public void TestActionCtorActionInfo()
        {
            ActionInfo info = store.Package0Package.ActionInfoSet["event_sequence"];
            Event ev = new Event();
            XEvent.Action action = new XEvent.Action(ev, info);
            Assert.IsNotNull(action);
            Assert.AreEqual(ev, action.Parent);
            Assert.AreEqual("package0.event_sequence", action.Name);
            Assert.AreEqual(info.Description, action.Description);
        }


        /// <summary>
        /// Test the constructor with Null ActionInfo.
        /// </summary>
        public void TestActionCtorNullActionInfo()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                //the name for action is wrong, the ActionInfo is null.
                ActionInfo info = store.Package0Package.ActionInfoSet["no such action"];
                Event ev = new Event();
                XEvent.Action action = new XEvent.Action(ev, info);
            }, "Action constructor should throw");
        }

        /// <summary>
        /// Test the properties for Action class.
        /// </summary>
        public void TestActionProperties()
        {
            try
            {
                ExecNonQuery(createSessionWithActionStmt);
                XEvent.Action action = store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"].Actions["package0.event_sequence"];
                Assert.IsNotNull(action);
                Assert.AreEqual("package0.event_sequence", action.Name);
                Assert.IsNotNull(action.IdentityKey);
                Assert.IsNotNull(action.Parent);
                Assert.AreEqual("sqlserver.rpc_starting", action.Parent.Name);
            }
            finally
            {
                ExecNonQuery(dropSessionWithActionStmt);
            }                            
        }

        /// <summary>
        /// Test settng name for an existing Action object.
        /// </summary>
        public void TestActionSetNameForExistingAction()
        {
            try
            {
                Assert.Throws<XEventException>(() =>
                {
                    ExecNonQuery(createSessionWithActionStmt);
                    XEvent.Action action =
                        store.Sessions["actionUnitTest"].Events["sqlserver.rpc_starting"].Actions[
                            "package0.event_sequence"];
                    action.Name = "newName";
                }, "Name setter should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionWithActionStmt);
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
