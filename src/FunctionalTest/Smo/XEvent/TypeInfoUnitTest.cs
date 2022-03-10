// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Management.XEventDbScoped;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for TypeInfoUnitTest
    /// </summary>
    [TestClass]
    
    public class TypeInfoUnitTest : DbScopedXEventTestBase
    {

        [TestMethod]
        public void DbScoped_TypeInfo_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestProperties();
                TestNullTypeProperties();
            }));
        }

        /// <summary>
        /// Tests the properties.
        /// </summary>
        public void TestProperties()
        {
            
            TypeInfo info = store.Package0Package.TypeInfoSet["int64"];
            Assert.AreEqual("Signed 64-bit integer", info.Description);
            Assert.AreEqual(8, info.Size);
            Assert.AreEqual(256, info.Capabilities);
            Assert.AreEqual("sign_extended", info.CapabilitiesDesc);
        }

        /// <summary>
        /// Tests the properties of null type.
        /// </summary>
        public void TestNullTypeProperties()
        {
           
            TypeInfo info = store.Package0Package.TypeInfoSet["null"];
            Assert.AreEqual("The NULL type", info.Description);
            Assert.AreEqual(0, info.Size);
            Assert.AreEqual(0, info.Capabilities);
            Assert.IsNull(info.CapabilitiesDesc);
        }

    }
}
