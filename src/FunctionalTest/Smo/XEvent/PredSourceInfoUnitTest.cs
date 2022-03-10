// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for PredSourceInfoUnitTest
    /// </summary>
    [TestClass]
    public class PredSourceInfoUnitTest : DbScopedXEventTestBase
    {
        /// <summary>
        /// Tests the properties.
        /// </summary>
        [TestMethod]
        public void DbScoped_PredSourceInfo_TestProperties()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                PredSourceInfo info = store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.database_id");
                Assert.AreEqual("Get the current database ID", info.Description);
                Assert.AreEqual("uint16", info.TypeName);
                Assert.AreEqual("package0", info.TypePackageName);
                Assert.AreEqual(new Guid("60AA9FBF-673B-4553-B7ED-71DCA7F5E972"), info.TypePackageID);

                info = store.Packages["sqlos"].PredSourceInfoSet["cpu_id"];
                Assert.AreEqual("Get current CPU ID", info.Description);
                Assert.AreEqual("uint32", info.TypeName);
                Assert.AreEqual("package0", info.TypePackageName);
            }));
        }
    }
}
