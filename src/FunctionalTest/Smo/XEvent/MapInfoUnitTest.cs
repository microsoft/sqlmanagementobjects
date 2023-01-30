// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Unit Test for MapInfo.
    /// </summary>
    [TestClass]
    public class MapInfoUnitTest : DbScopedXEventTestBase
    {

        private MapInfo mapInfo = null;


        [TestMethod]
        public void DbScoped_MapInfo_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestMapInfoCollectionContains();
                TestMapInfoObjectQuery();
                TestMapInfoProperties();
                TestMapInfoPropertiesNullCap();
                TestMapInfoSet();
                TestMapInfoToString();
            }));
        }

        /// <summary>
        /// Tests the MapInfo properties.
        /// </summary>
        public void TestMapInfoProperties()
        {
            
            

            mapInfo = store.Packages["package0"].MapInfoSet["etw_channel"];
            Assert.AreEqual("ETW channels", mapInfo.Description);
            Assert.AreEqual(0, mapInfo.Capabilities);
            Assert.AreEqual(null, mapInfo.CapabilitiesDesc);
            Assert.IsNotNull(mapInfo.IdentityKey);
            Assert.AreEqual(mapInfo.Name, mapInfo.IdentityKey.Name);
        }

        /// <summary>
        /// Tests the MapInfo properties when CapabilitiesDesc is Null.
        /// </summary>
        public void TestMapInfoPropertiesNullCap()
        {        
            mapInfo = store.Packages["sqlos"].MapInfoSet["keyword_map"];
            Assert.AreEqual("Event grouping keywords", mapInfo.Description);
            Assert.AreEqual(0, mapInfo.Capabilities);
            Assert.AreEqual(null, mapInfo.CapabilitiesDesc);
        }

        /// <summary>
        /// Tests the object query for MapInfo.
        /// </summary>
        public void TestMapInfoObjectQuery()
        {
            SfcObjectQuery query = new SfcObjectQuery(store);
            //construct the query
            foreach (object obj in query.ExecuteIterator(new SfcQueryExpression("DatabaseXEStore[@Name='msdb']/Package[@Name='package0']/MapInfo[@Name='etw_channel']"), null, null))
            {
                mapInfo = obj as MapInfo;
            }
            Assert.IsNotNull(mapInfo);
            Assert.IsInstanceOfType(mapInfo, typeof(MapInfo));
            Assert.AreEqual("ETW channels", mapInfo.Description);
            Assert.AreEqual(0, mapInfo.Capabilities);
            Assert.AreEqual(null, mapInfo.CapabilitiesDesc);
        }

        /// <summary>
        /// Tests ToString.
        /// </summary>
        public void TestMapInfoToString()
        {
            mapInfo = store.Packages["package0"].MapInfoSet["etw_channel"];
            Assert.IsNotNull(mapInfo.ToString());
            Assert.IsTrue(mapInfo.ToString().Length > 0);
        }


        /// <summary>
        /// Tests the Contains() for MapInfoCollection.
        /// </summary>
        public void TestMapInfoCollectionContains()
        {
            Assert.IsTrue(store.Package0Package.MapInfoSet.Contains("etw_channel"));
            Assert.IsTrue(store.Package0Package.MapInfoSet.Contains("etw_opcodes"));
            Assert.IsFalse(store.Packages["sqlos"].MapInfoSet.Contains("etw_opcodes"));
        }

        /// <summary>
        /// Tests the MapInfoSet for each package has the correct number of elements.
        /// </summary>
        public void TestMapInfoSet()
        {
            Assert.IsTrue(VerifyObjectCount(XEUtil.SqlosPackageName, XEUtil.MapTypeName, store.Packages["sqlos"].MapInfoSet.Count));
            Assert.IsTrue(VerifyObjectCount(XEUtil.Package0PackageName, XEUtil.MapTypeName, store.Package0Package.MapInfoSet.Count));
        }

    }
}
