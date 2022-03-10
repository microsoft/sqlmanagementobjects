// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for DataEventColumnInfoUnitTest
    /// </summary>
    [TestClass]
    public class DataEventColumnInfoUnitTest : DbScopedXEventTestBase
    {

        /// <summary>
        /// Invokes all the DataEventColumnInfo tests
        /// </summary>


        /// <summary>
        /// Tests null description.
        /// </summary>
        [TestMethod]
        public void TestNullDescription()
        {
            ExecuteFromDbPool(PoolName, (db) => ExecuteTest(db, () =>
            {
                DataEventColumnInfo info =
                    store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_info").DataEventColumnInfoSet["signal_duration"];
                Assert.IsNotNull(info);
                Assert.AreEqual(3, info.ID);
                Assert.AreEqual("uint64", info.TypeName);
                Assert.AreEqual(0, info.Capabilities);
                Assert.IsTrue(string.IsNullOrEmpty(info.CapabilitiesDesc));
                Assert.That(info.Description, Is.Null, "Description should be empty");
            }));

        }
    }
}
