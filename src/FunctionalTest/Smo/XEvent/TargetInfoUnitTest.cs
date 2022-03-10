// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for TargetInfoUnitTest
    /// </summary>
    [TestClass]
    public class TargetInfoUnitTest : DbScopedXEventTestBase
    {
       
        /// <summary>
        /// Tests the properties.
        /// </summary>
        [TestMethod]
        public void TestProperties()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TargetInfo info = store.RingBufferTargetInfo;
                Assert.AreEqual("ring_buffer", info.Name);
                Assert.AreEqual("Asynchronous ring buffer target.", info.Description);
                Assert.AreEqual(258, info.Capabilities);
                Assert.AreEqual("sds_visible process_whole_buffers", info.CapabilitiesDesc);
            }));
        }
    }
}
