// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// UnitTest for Target.
    /// </summary>
    [TestClass]
    public class TargetUnitTest : DbScopedXEventTestBase
    {
        #region help T-SQL
        //create a dummy session, add one dummy event and target.
        private string createSessionStmt = "CREATE EVENT SESSION targetUnitTest ON DATABASE ADD EVENT sqlserver.rpc_starting " +
            "ADD TARGET package0.event_file (SET filename='https://account.blob.core.windows.net/container/filename.xel')";
        private string dropSessionStmt = "DROP EVENT SESSION targetUnitTest ON DATABASE";

        #endregion

        [TestMethod]
        public void DbScoped_Target_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestGetTargetData();
                TestTargetCtorEmpty();
                TestTargetCtorName();
                TestTargetCtorNameModule();
                TestTargetCtorNameModuleWrongName();
                TestTargetCtorNameWrongFormat();
                TestTargetCtorNameWrongPackageName();
                TestTargetCtorNameWrongTargetName();
                TestTargetCtorNullXEStore();
                TestTargetCtorTargetInfo();
                TestTargetCtorTargetInfoNull();
                TestTargetGetTargetFields();
                TestTargetProperties();
                TestTargetSetTargetFieldPending();
            }));
        }

        /// <summary>
        /// Test the Target constructor with Empty Name.
        /// </summary>
        public void TestTargetCtorName()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["targetUnitTest"];
                Target target = new Target(session, "package0.event_file");
                Assert.IsNotNull(target);
                Assert.AreEqual("package0.event_file", target.Name);
                Assert.AreEqual("package0", target.PackageName);
                Assert.AreEqual(-1, target.ID);
                Assert.IsNotNull(target.Description);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Test the Target constructor with full qulified Name.
        /// </summary>
        public void TestTargetCtorNameModule()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["targetUnitTest"];
                Target target = new Target(session, "package0.event_file");
                Assert.IsNotNull(target);
                Assert.AreEqual("package0.event_file", target.Name);
                Assert.AreEqual("package0", target.PackageName);
                Assert.AreEqual(-1, target.ID);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Test the Target constructor with wrong module id.
        /// </summary>
        public void TestTargetCtorNameModuleWrongName()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["targetUnitTest"];
                Assert.Throws<XEventException>(
                    () => new Target(session, "[CE79811F-1A80-40E1-8F5D-111111111111].package0.event_file"),
                    "Target constructor should throw");
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }

        }

        /// <summary>
        /// Test the Target constructor with TargetInfo.
        /// </summary>
        public void TestTargetCtorTargetInfo()
        {
            
            Session session = new Session();
            TargetInfo info = store.Package0Package.TargetInfoSet["event_file"];
            Target target = new Target(session, info);

            Assert.IsNotNull(target);
            Assert.AreEqual("package0.event_file", target.Name);
            Assert.AreEqual("package0", target.PackageName);
            Assert.AreEqual(-1, target.ID);
            Assert.AreEqual(session, target.Parent);
        }

        /// <summary>
        /// Test the Empty Target constructor.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestTargetCtorEmpty()
        {
            Target target = new Target();
            Assert.IsNull(target.Name);
            Assert.AreEqual(-1, target.ID);
            Assert.IsNull(target.Description);
        }

        /// <summary>
        /// Test the Target constructor with Null DatabaseXEStore.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestTargetCtorNullXEStore()
        {
            Session session = new Session();
            Assert.Throws<NullReferenceException>(() => new Target(session, "package0.event_file"));
        }

        /// <summary>
        /// Test the Target constructor with malformed Name.
        /// </summary>
        public void TestTargetCtorNameWrongFormat()
        {
            Assert.Throws<XEventException>(() =>
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["targetUnitTest"];
                Target target = new Target(session, "a.b.c.d");
            }, "Target constructor should throw");
            ExecNonQuery(dropSessionStmt);            
        }

        /// <summary>
        /// Test the Target constructor with wrong package Name.
        /// </summary>
        public void TestTargetCtorNameWrongPackageName()
        {
            Assert.Throws<XEventException>(() =>
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["targetUnitTest"];
                Target target = new Target(session, "wrongpackage.allocation_ring_buffer_recorded");
            }, "Target constructor should throw");
            ExecNonQuery(dropSessionStmt);
        }

        /// <summary>
        /// Test the Target constructor with wrong Target Name.
        /// </summary>
        public void TestTargetCtorNameWrongTargetName()
        {
            Assert.Throws<XEventException>(() =>
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["targetUnitTest"];
                Target target = new Target(session, "sqlserver.nosuchtarget");
            }, "Target constructor should throw");
            ExecNonQuery(dropSessionStmt);
        }

        /// <summary>
        /// Test the Target constructor with TargetInfo.
        /// </summary>
        public void TestTargetCtorTargetInfoNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                Session session = new Session();
                //the event name is not correct, return value is null
                TargetInfo info = store.Packages["package0"].TargetInfoSet["nosuchtarget"];
                Target target = new Target(session, info);
            }, "Target constructor should throw");
        }

        /// <summary>
        /// Test the Target Properties.
        /// </summary>
        public void TestTargetProperties()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Target target = store.Sessions["targetUnitTest"].Targets["package0.event_file"];
                Assert.IsNotNull(target);
                Assert.AreEqual("package0.event_file", target.Name);
                Assert.AreEqual("package0", target.PackageName);
                //only one event and one target are in the session, so the ID should be 2.
                Assert.AreEqual(2, target.ID);
                Assert.IsNotNull(target.IdentityKey);
                Assert.IsNotNull(target.Description);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Test the get method for Property TargetFields.
        /// </summary>
        public void TestTargetGetTargetFields()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Target target = store.Sessions["targetUnitTest"].Targets["package0.event_file"];
                Assert.IsNotNull(target);
                Assert.IsNotNull(target.TargetFields);
                //package0.event_file has 5 customizable fields
                //filename is set to "file" in this session
                Assert.That(target.TargetFields.Count, Is.EqualTo(9), "TestTargetGetTargetFields event_file TargetFields.Count" ); 
                Assert.AreEqual("https://account.blob.core.windows.net/container/filename.xel", target.TargetFields["filename"].Value);

                //other fields are all in default value
                Assert.AreEqual(null, target.TargetFields["max_file_size"].Value);
                Assert.AreEqual(null, target.TargetFields["max_rollover_files"].Value);
                Assert.AreEqual(null, target.TargetFields["increment"].Value);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Test set target field for a pending Target.
        /// </summary>
        public void TestTargetSetTargetFieldPending()
        {
            try
            {
                ExecNonQuery(createSessionStmt);
                Session session = store.Sessions["targetUnitTest"];
                //the target is in pending state
                Target target = new Target(session, "package0.event_file");
                Assert.IsNotNull(target);
                Assert.IsNull(target.TargetFields["max_file_size"].Value);
                //fields can be set when target is in pending state
                target.TargetFields["max_file_size"].Value = 4;
                Assert.AreEqual(4, target.TargetFields["max_file_size"].Value);
                //other fields are still null
                Assert.IsNull(target.TargetFields["increment"].Value);
            }
            finally
            {
                ExecNonQuery(dropSessionStmt);
            }
        }

        /// <summary>
        /// Query excecutor.
        /// </summary>
        /// <param name="stmt"></param>
        private void ExecNonQuery(string stmt)
        {
            (store.SfcConnection as ServerConnection).ExecuteNonQuery(stmt);
        }


        /// <summary>
        /// Tests reading target data.
        /// </summary>
        public void TestGetTargetData()
        {
            Session session = store.Sessions["targetUnitTest"];
            if (session != null)
                session.Drop();

            session = store.CreateSession("targetUnitTest");
            session.AddEvent("sqlserver.rpc_starting");
            session.AddEvent("sqlserver.rpc_completed");
            Target target = session.AddTarget(store.RingBufferTargetInfo);
            session.Create();
            try
            {
                session.Start();

                Assert.IsNotNull(target.GetTargetData());

                target = new Target(session, store.EventFileTargetInfo);
                Assert.Throws<XEventException>(() => target.GetTargetData(), "GetTargetData should throw");
            
                session.Stop();
                Assert.Throws<XEventException>(() => target.GetTargetData(), "GetTargetData should throw after session stopped");
            }
            finally
            {
                session.Drop();
            }
        }
    }
}
