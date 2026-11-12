// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for EventColumnInfoUnitTest
    /// </summary>
    [TestClass]
    public class EventColumnInfoUnitTest : DbScopedXEventTestBase
    {
        
        

        /// <summary>
        /// Tests the properties.
        /// </summary>
        [TestMethod]
        public void TestProperties()
        {

            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                EventColumnInfo info =
                    store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting").EventColumnInfoSet["collect_statement"];
                Assert.IsNotNull(info);
                Assert.AreEqual(0, info.ID);
                Assert.AreEqual("boolean", info.TypeName);
                Assert.AreEqual("package0", info.TypePackageName);
                Assert.AreEqual(new Guid("60AA9FBF-673B-4553-B7ED-71DCA7F5E972"), info.TypePackageID);
                Assert.AreEqual("true", info.Value);
                Assert.AreEqual(65536, info.Capabilities);
                Assert.IsNotNull(info.CapabilitiesDesc);
                Assert.IsNotNull(info.Description);
            }));

        }



    }

}
