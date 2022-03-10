// Copyright (c) Microsoft.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for MapValueInfoUnitTest
    /// </summary>
    [TestClass]
    public class MapValueInfoUnitTest : DbScopedXEventTestBase
    {

        [TestMethod]
        public void DbScoped_MapValueInfo_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestProperties();
                TestQueryMappedValue();
            }));
        }

        /// <summary>
        /// Tests the properties.
        /// </summary>
        public void TestProperties()
        {            
            MapInfo mapInfo = store.Package0Package.MapInfoSet["etw_channel"];
            MapValueInfo mapValueInfo = mapInfo.MapValueInfoSet["1"];
            Assert.AreEqual("Admin", mapValueInfo.Value);
            Assert.AreEqual(mapInfo, mapValueInfo.Parent);

            mapInfo = store.Packages["sqlos"].MapInfoSet["assert_type"];
            mapValueInfo = mapInfo.MapValueInfoSet["2"];
            Assert.AreEqual("Soft", mapValueInfo.Value);
            Assert.AreEqual(mapInfo, mapValueInfo.Parent);
        }

        /// <summary>
        /// Tests query the mapped value of a specified event column info.
        /// </summary>
        public void TestQueryMappedValue()
        {
            ReadOnlyEventColumnInfo columnInfo = store.ObjectInfoSet.Get<EventInfo>("sqlserver.sp_statement_starting").ReadOnlyEventColumnInfoSet["channel"];
            string columnValue = columnInfo.Value;
            MapInfo mapInfo = store.Packages[columnInfo.TypePackageName].MapInfoSet[columnInfo.TypeName];
            Assert.AreEqual("Analytic", mapInfo.MapValueInfoSet[columnValue].Value);
        }
    }
}
