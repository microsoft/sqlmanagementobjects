// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Unit Test for TargetColumnInfo.
    /// </summary>
    [TestClass]
    public class TargetColumnInfoUnitTest : DbScopedXEventTestBase
    {
        private TargetColumnInfo targetColumn = null;

        [TestMethod]
        public void DbScoped_TargetColumnInfo_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestTargetColumnCollectionContains();
                TestTargetColumnInfoObjectQuery();
                TestTargetColumnInfoProperties();
                TestTargetColumnInfoSet();
                TestTargetColumnInfoValueNull();
                TestTargetColumnToString();
            }));
        }

        /// <summary>
        /// Tests the TargetColumnInfo properties.
        /// </summary>
        public void TestTargetColumnInfoProperties()
        {
            targetColumn = store.EventFileTargetInfo.TargetColumnInfoSet["max_file_size"];
            Assert.AreEqual("max_file_size", targetColumn.Name);
            Assert.AreEqual(1, targetColumn.ID);
            Assert.AreEqual("Maximum file size in MB", targetColumn.Description);
            Assert.AreEqual(0, targetColumn.Capabilities);
            Assert.AreEqual("", targetColumn.CapabilitiesDesc);
            //Assert.AreEqual("customizable", targetColumn.ColumnType);
            Assert.AreEqual("1024", targetColumn.Value);
            Assert.AreEqual("uint64", targetColumn.TypeName);
            Assert.AreEqual("package0", targetColumn.TypePackageName);
            Assert.IsNotNull(targetColumn.IdentityKey);
            Assert.AreEqual(targetColumn.Name, targetColumn.IdentityKey.Name);
        }

        /// <summary>
        /// Tests the TargetColumnInfo when column value is null.
        /// </summary>
        public void TestTargetColumnInfoValueNull()
        {
            targetColumn = store.EventFileTargetInfo.TargetColumnInfoSet["filename"];
            Assert.AreEqual(0, targetColumn.ID);
            Assert.AreEqual(1, targetColumn.Capabilities);
            Assert.AreEqual("mandatory", targetColumn.CapabilitiesDesc);
            Assert.AreEqual(null, targetColumn.Value);
        }


        /// <summary>
        /// Tests the object query for TargetColumnInfo.
        /// </summary>
        public void TestTargetColumnInfoObjectQuery()
        {
        
            SfcObjectQuery query = new SfcObjectQuery(store);
            //construct the query
            foreach (object obj in query.ExecuteIterator(new SfcQueryExpression("DatabaseXEStore[@Name='msdb']/Package[@Name='package0']/TargetInfo[@Name='event_file']/TargetColumnInfo[@Name='filename']"), null, null))
            {
                targetColumn = obj as TargetColumnInfo;
            }

            Assert.IsNotNull(targetColumn);
            Assert.AreEqual(0, targetColumn.ID);
            Assert.AreEqual("unicode_string_ptr", targetColumn.TypeName);
            Assert.AreEqual("package0", targetColumn.TypePackageName);
            Assert.AreEqual(new Guid("60AA9FBF-673B-4553-B7ED-71DCA7F5E972"), targetColumn.TypePackageID);
            //Assert.AreEqual("customizable", targetColumn.ColumnType);
            Assert.AreEqual("mandatory", targetColumn.CapabilitiesDesc);
            Assert.IsNull(targetColumn.Value);
        }

        /// <summary>
        /// Tests ToString() function.
        /// </summary>
        public void TestTargetColumnToString()
        {

            targetColumn = store.EventFileTargetInfo.TargetColumnInfoSet["filename"];
            Assert.IsNotNull(targetColumn.ToString());
            Assert.IsTrue(targetColumn.ToString().Length > 0);
        }

        /// <summary>
        /// Tests the Contains for TargetColumnCollection.
        /// </summary>
        public void TestTargetColumnCollectionContains()
        {
            Assert.IsTrue(store.EventFileTargetInfo.TargetColumnInfoSet.Contains("max_file_size"));
            Assert.IsFalse(store.RingBufferTargetInfo.TargetColumnInfoSet.Contains("max_file_size"));
        }

        /// <summary>
        /// Tests TargetColumnInfoSet collection contains the correct number of elements.
        /// </summary>
        public void TestTargetColumnInfoSet()
        {
            var columns = store.EventFileTargetInfo.TargetColumnInfoSet.OfType<TargetColumnInfo>().Select(c => c.Name);
            Assert.That(columns, Is.EquivalentTo(new string[] { "add_app_name", "external_telemetry_query", "filename", "increment", "is_indexed_file_target", "lazy_create_blob", "max_file_size", "max_rollover_files", "metadatafile"}), 
                "Unexpected columns for file target");
        }
    }
}


