// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for ReadOnlyEventColumnInfoUnitTest
    /// </summary>
    [TestClass]
    public class ReadOnlyEventColumnInfoUnitTest : DbScopedXEventTestBase
    {


        [TestMethod]
        public void DbScoped_ReadOnlyEventColumnInfo_Tests()
        {
            ExecuteWithDbDrop((db) => ExecuteTest(db, TestProperties));        
        }

        /// <summary>
        /// Tests the properties.
        /// </summary>
        public void TestProperties()
        {
            ReadOnlyEventColumnInfo info = store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_completed").ReadOnlyEventColumnInfoSet["UUID"];
            Assert.IsNotNull(info);
            Assert.AreEqual(0, info.ID);
            Assert.AreEqual("guid_ptr", info.TypeName);
            Assert.AreEqual("package0", info.TypePackageName);
            Assert.AreEqual(new Guid("60AA9FBF-673B-4553-B7ED-71DCA7F5E972"), info.TypePackageID);
            Assert.AreEqual("F1574C94-2B93-4716-A3C1-90F1E1FE2DB8", info.Value);
            Assert.AreEqual(0, info.Capabilities);
            Assert.IsNull(info.CapabilitiesDesc);
            Assert.AreEqual("Globally Unique ID", info.Description);
        }

        
    }

}
