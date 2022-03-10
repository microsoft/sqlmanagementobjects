// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Unit Test for PredCompareInfo.
    /// </summary>
    [TestClass]
    public class PredCompareUnitTest : DbScopedXEventTestBase
    {
        private PredCompareInfo predCompare = null;

        [TestMethod]
        public void DbScoped_PredCompare_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestPredCompareCollectionContains();
                TestPredCompareInfoSet();
                TestPredCompareObjectQuery();
                TestPredCompareProperties();
                TestPredCompareToString();
            }));
        }

        /// <summary>
        /// Tests the PredCompareInfo properties.
        /// </summary>
        public void TestPredCompareProperties()
        {
            predCompare = store.ObjectInfoSet.Get<PredCompareInfo>("sqlserver.equal_i_sql_unicode_string");
            Assert.IsNotNull(predCompare);
            Assert.IsInstanceOfType(predCompare, typeof(PredCompareInfo));
            Assert.AreEqual("equal_i_sql_unicode_string", predCompare.Name);
            Assert.AreEqual("Equality operator between two SQL UNICODE string values", predCompare.Description);
            Assert.AreEqual("unicode_string", predCompare.TypeName);
            Assert.AreEqual("package0", predCompare.TypePackageName);
            Assert.AreEqual(new Guid("60AA9FBF-673B-4553-B7ED-71DCA7F5E972"), predCompare.TypePackageID);
            Assert.IsNotNull(predCompare.IdentityKey);
            Assert.AreEqual(predCompare.Name, predCompare.IdentityKey.Name);
        }

        /// <summary>
        /// Tests the object query for Pred_compare.
        /// </summary>
        public void TestPredCompareObjectQuery()
        {
            
            

            SfcObjectQuery query = new SfcObjectQuery(store);
            //construct the query
            foreach (object obj in query.ExecuteIterator(new SfcQueryExpression("DatabaseXEStore[@Name='msdb']/Package[@Name='package0']/PredCompareInfo[@Name='equal_uint64']"), null, null))
            {
                predCompare = obj as PredCompareInfo;
            }

            Assert.IsNotNull(predCompare);
            Assert.IsInstanceOfType(predCompare, typeof(PredCompareInfo));
            Assert.AreEqual("uint64", predCompare.TypeName);
            Assert.AreEqual("package0", predCompare.TypePackageName);
        }

        /// <summary>
        /// Tests the ToString.
        /// </summary>
        public void TestPredCompareToString()
        {
            
            

            predCompare = store.ObjectInfoSet.Get<PredCompareInfo>("sqlserver.equal_i_sql_unicode_string");
            Assert.IsNotNull(predCompare.ToString());
            Assert.IsTrue(predCompare.ToString().Length > 0);
        }

        /// <summary>
        /// Tests the Contains() for PredCompareCollection.
        /// </summary>
        public void TestPredCompareCollectionContains()
        {
            
            Assert.IsTrue(store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"].PredCompareInfoSet.Contains("equal_i_sql_unicode_string"));
            Assert.IsFalse(store.Packages["sqlos"].PredCompareInfoSet.Contains("equal_i_sql_unicode_string"));
        }

        /// <summary>
        /// Tests the PredCompareInfoSet for each package has the correct number of elements.
        /// </summary>
        public void TestPredCompareInfoSet()
        {
            Assert.IsTrue(VerifyObjectCount(XEUtil.Package0PackageName, XEUtil.PredCompareTypeName, store.Package0Package.PredCompareInfoSet.Count));
        }
    }
}
