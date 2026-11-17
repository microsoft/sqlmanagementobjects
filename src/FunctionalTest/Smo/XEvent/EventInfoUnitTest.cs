// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Unit Test for EventInfo.
    /// </summary>
    [TestClass]
    public class EventInfoUnitTest : DbScopedXEventTestBase
    {
        private EventInfo eventInfo = null;

        [TestMethod]
        public void DbScoped_EventInfo_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestEventColumnInfoSet();
                TestEventInfoCollectionContains();
                TestEventInfoProperties();
                TestEventInfoSet();
                TestEventInfoToString();
            }));
        }
        /// <summary>
        /// Tests the EventInfo properties.
        /// </summary>
        public void TestEventInfoProperties()
        {

            eventInfo = store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"].EventInfoSet["rpc_starting"];
            Assert.IsNotNull(eventInfo);
            Assert.AreEqual("Occurs when a remote procedure call has started.", eventInfo.Description);
            Assert.AreEqual(2, eventInfo.Capabilities);
            Assert.AreEqual("sds_visible", eventInfo.CapabilitiesDesc);
            Assert.IsNotNull(eventInfo.IdentityKey);
            Assert.AreEqual(eventInfo.Name, eventInfo.IdentityKey.Name);
        }
        

        /// <summary>
        /// Tests the event column set.
        /// </summary>
        public void TestEventColumnInfoSet()
        {
            eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlserver.object_created");
            Assert.IsNotNull(eventInfo.EventColumnInfoSet);
            Assert.IsInstanceOfType(eventInfo.EventColumnInfoSet, typeof(EventColumnInfoCollection));
        }
        /// <summary>
        /// Tests ToString.
        /// </summary>
        public void TestEventInfoToString()
        {
            eventInfo = store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"].EventInfoSet["rpc_starting"];
            Assert.IsNotNull(eventInfo.ToString());
            Assert.IsTrue(eventInfo.ToString().Length > 0);
        }

        /// <summary>
        /// Tests the Contains() for EventInfoCollection.
        /// </summary>
        public void TestEventInfoCollectionContains()
        {
            Assert.IsTrue(store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"].EventInfoSet.Contains("rpc_starting"));
            Assert.IsFalse(store.Package0Package.EventInfoSet.Contains("rpc_starting"));
        }

        /// <summary>
        /// Tests the EventInfoSet for each package has the correct number of elements.
        /// </summary>
        public void TestEventInfoSet()
        {            
            Assert.IsTrue(VerifyObjectCount(XEUtil.SqlosPackageName, XEUtil.EventTypeName, store.Packages["sqlos"].EventInfoSet.Count));
        }
    }
}
