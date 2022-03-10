// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for PackageUnitTest
    /// </summary>
    [TestClass]
    public class PackageUnitTest : DbScopedXEventTestBase
    {

        private Package package;

        [TestMethod]
        public void DbScoped_Package_Tests()
        {
            ExecuteFromDbPool(PoolName, (db) => ExecuteTest(db, () =>
           {
               TestPackageActionInfoSet();
               TestPackageCollectionContains();
               TestPackageMapInfoSet();
               TestPackageObjectQuery();
               TestPackagePredCompareInfoSet();
               TestPackagePredSourceInfoSet();
               TestPackageProperties();
               TestPackageTargetInfoSet();
               TestPackageToString();
               TestPackageTypeInfoSet();
           }));
        }

        /// <summary>
        /// Tests the package properties. Assert if every property is correct.
        /// </summary>
        public void TestPackageProperties()
        {
            package = store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"];
            Assert.IsNotNull(package);
            Assert.IsInstanceOfType(package, typeof(Package));
            Assert.AreEqual("sqlserver", package.Name);
            Assert.AreEqual(new Guid("655FD93F-3364-40D5-B2BA-330F7FFB6491"), package.ID);
            Assert.AreEqual(new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), package.ModuleID);
            Assert.AreEqual("Extended events for Microsoft SQL Server", package.Description);
            Assert.AreEqual(0, package.Capabilities);
            Assert.AreEqual(null, package.CapabilitiesDesc);
            Assert.IsNotNull(package.IdentityKey);
            Assert.IsNotNull(package.ModuleAddress);
            //assert the count is correct for packages.
            //Assert.AreEqual(3, store.Packages.Count);
            Assert.IsTrue(VerifyPackageCount(store.Packages.Count));

            Package package2 = store.Packages[new Guid("655FD93F-3364-40D5-B2BA-330F7FFB6491")];
            Assert.AreEqual(package, package2);
        }


        /// <summary>
        /// Tests the package action info set.
        /// </summary>
        public void TestPackageActionInfoSet()
        {
            package = store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"];
            ActionInfo action = package.ActionInfoSet["session_id"];
            Assert.IsNotNull(action);
            Assert.AreEqual("Collect session ID", action.Description);
            Assert.AreEqual(2, action.Capabilities);
            Assert.AreEqual("sds_visible", action.CapabilitiesDesc);
            Assert.AreEqual("uint16", action.TypeName);
            Assert.AreEqual("package0", action.TypePackageName);
        }

        /// <summary>
        /// Tests the target constant defined in XEstore.
        /// </summary>
        public void TestPackageTargetInfoSet()
        {
            TargetInfo target = store.EventFileTargetInfo;
            TargetInfo target2 = store.Package0Package.TargetInfoSet["event_file"];
            Assert.AreEqual(target, target2);
            Assert.AreEqual("Use the event_file target to save the event data to an XEL file, which can be archived and used for later analysis and review. You can merge multiple XEL files to view the combined data from separate event sessions.", target2.Description);
            Assert.AreEqual(258, target2.Capabilities);
            Assert.AreEqual("sds_visible process_whole_buffers", target2.CapabilitiesDesc);
        }

        /// <summary>
        /// Tests the package map info set.
        /// </summary>

        public void TestPackageMapInfoSet()
        {
            package = store.Packages[new Guid("D5149520-6282-11DE-8A39-0800200C9A66"), "sqlserver"];
            MapInfo map = package.MapInfoSet["file_io_mode"];
            Assert.IsNotNull(map);
            Assert.AreEqual("File I/O mode", map.Description);
            Assert.AreEqual(0, map.Capabilities);
            Assert.AreEqual(null, map.CapabilitiesDesc);

        }

        /// <summary>
        /// Tests the package pred compare info set.
        /// </summary>

        public void TestPackagePredCompareInfoSet()
        {
            package = store.Packages[new Guid("CE79811F-1A80-40E1-8F5D-7445A3F375E7"), "sqlserver"];
            PredCompareInfo compare = package.PredCompareInfoSet["not_equal_i_sql_unicode_string"];
            Assert.IsNotNull(compare);
            Assert.AreEqual("Inequality operator between two SQL UNICODE string values", compare.Description);
            Assert.AreEqual("unicode_string", compare.TypeName);
            Assert.AreEqual("package0", compare.TypePackageName);
        }

        /// <summary>
        /// Tests the package pred source info set.
        /// </summary>

        public void TestPackagePredSourceInfoSet()
        {
            PredSourceInfo predSource = store.Package0Package.PredSourceInfoSet["current_thread_id"];
            Assert.IsNotNull(predSource);
            Assert.AreEqual("Get the current Windows thread ID", predSource.Description);
            Assert.AreEqual("uint32", predSource.TypeName);
            Assert.AreEqual("package0", predSource.TypePackageName);
        }

        /// <summary>
        /// Tests the package type info set.
        /// </summary>

        public void TestPackageTypeInfoSet()
        {
            TypeInfo type = store.Package0Package.TypeInfoSet["int8"];
            Assert.AreEqual(256, type.Capabilities);
            Assert.AreEqual("sign_extended", type.CapabilitiesDesc);
            Assert.AreEqual(1, type.Size);
        }
        /// <summary>
        /// Tests the object query can work on package correctly.
        /// </summary>

        public void TestPackageObjectQuery()
        {
            int count = 0;
            SfcObjectQuery query = new SfcObjectQuery(store);
            //construct the query
            foreach (object obj in query.ExecuteIterator(new SfcQueryExpression("DatabaseXEStore/Package[@Name='package0']"), null, null))
            {
                package = obj as Package;
                count++;
            }

            Assert.AreEqual(1, count, "Expected 1 package.");

            DataSet directSet = connection.ExecuteWithResults("Select * From sys.dm_xe_packages Where name = 'package0'");
            DataRow directRow = directSet.Tables[0].Rows[0];

            Assert.IsNotNull(package);
            Assert.IsInstanceOfType(package, typeof(Package));
            Assert.AreEqual("package0", package.Name);
            Assert.AreEqual(directRow["guid"], package.ID);
            Assert.AreEqual(directRow["description"], package.Description);
            Assert.AreEqual(directRow["capabilities"] == DBNull.Value ? 0 : directRow["capabilities"], package.Capabilities);
            Assert.AreEqual(directRow["capabilities_desc"] == DBNull.Value ? null : directRow["capabilities_desc"], package.CapabilitiesDesc);
        }

        /// <summary>
        /// Tests the ToString.
        /// </summary>

        public void TestPackageToString()
        {

            package = store.Package0Package;
            Assert.IsNotNull(package.ToString());
            Assert.IsTrue(package.ToString().Length > 0);
        }

        /// <summary>
        /// Tests the Contains() in PackageCollection.
        /// </summary>

        public void TestPackageCollectionContains()
        {
            Assert.IsTrue(store.Packages.Contains("package0"));
            Assert.IsFalse(store.Packages.Contains("invalid"));
        }
    }
}
