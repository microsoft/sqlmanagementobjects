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
    /// Unit Test for ActionInfo.
    /// </summary>
    [TestClass]
    public class ActionInfoUnitTest : DbScopedXEventTestBase
    {
        

        private ActionInfo actionInfo;

        /// <summary>
        /// Invokes all the tests for ActionInfo
        /// </summary>
        [TestMethod]
        public void DBScoped_ActionInfo_Tests()
        {
            ExecuteFromDbPool(PoolName, (db) => ExecuteTest(db, () =>
            {
                TestActionInfoProperties();
                TestActionCollectionContains();
                TestActionInfoObjectQuery();
                TestActionInfoSet();
                TestActionInfoToString();
            }));
        }


        /// <summary>
        /// Tests the ActionInfo properties.
        /// </summary>
        
        public void TestActionInfoProperties()
        {
            actionInfo =  store.ObjectInfoSet.Get<ActionInfo>("sqlserver.username");
            Assert.IsNotNull(actionInfo);
            Assert.IsInstanceOfType(actionInfo, typeof(ActionInfo));
            Assert.AreEqual("unicode_string", actionInfo.TypeName);
            Assert.AreEqual(new Guid("60AA9FBF-673B-4553-B7ED-71DCA7F5E972"), actionInfo.TypePackageID);
            Assert.AreEqual("package0", actionInfo.TypePackageName);
            Assert.IsNotNull(actionInfo.IdentityKey);
            Assert.AreEqual(actionInfo.Name, actionInfo.IdentityKey.Name);
        }

        /// <summary>
        /// Tests the ActionInfo object query.
        /// </summary>
        public void TestActionInfoObjectQuery()
        {
            
            int count = 0;

            SfcObjectQuery query = new SfcObjectQuery(store);
            //construct the query
            foreach (object obj in query.ExecuteIterator(new SfcQueryExpression(@"DatabaseXEStore/Package[@Name='package0']/ActionInfo[@Name='event_sequence']"), null, null))
            {
                actionInfo = obj as ActionInfo;
                count++;
            }

            Assert.AreEqual(1, count, "Expected 1 object.");

            DataSet directSet = connection.ExecuteWithResults(@"select a.*, tp.name 'pname' 
from sys.dm_xe_packages p, sys.dm_xe_objects a, sys.dm_xe_packages tp
where a.object_type = N'action' and a.package_guid = p.guid and p.name = 'package0' and a.name='event_sequence' and tp.guid = a.type_package_guid");
            DataRow directRow = directSet.Tables[0].Rows[0];

            Assert.IsNotNull(actionInfo);
            Assert.IsInstanceOfType(actionInfo, typeof(ActionInfo));
            Assert.AreEqual(actionInfo.Parent.Name, "package0");
            actionInfo.Parent = null;
            Assert.IsNull(actionInfo.Parent);
            Assert.AreEqual("event_sequence", actionInfo.Name);
            Assert.AreEqual(directRow["description"], actionInfo.Description);
            Assert.AreEqual(directRow["capabilities"] == DBNull.Value ? 0 : directRow["capabilities"], actionInfo.Capabilities);
            Assert.AreEqual(directRow["capabilities_desc"] == DBNull.Value ? null : directRow["capabilities_desc"], actionInfo.CapabilitiesDesc);
            Assert.AreEqual(directRow["type_name"], actionInfo.TypeName);
            Assert.AreEqual(directRow["pname"], actionInfo.TypePackageName);
        }

        /// <summary>
        /// Tests the ToString.
        /// </summary>
        public void TestActionInfoToString()
        {
            
            actionInfo = store.ObjectInfoSet.Get<ActionInfo>("sqlserver.username");
            Assert.IsNotNull(actionInfo.ToString());
            Assert.IsTrue(actionInfo.ToString().Length > 0);
        }

        /// <summary>
        /// Tests the Contains() in ActionCollection.
        /// </summary>
        public void TestActionCollectionContains()
        {
            
            Assert.IsTrue(store.Package0Package.ActionInfoSet.Contains("event_sequence"));
            Assert.IsFalse(store.Package0Package.ActionInfoSet.Contains("attach_activity_id"));
            Assert.IsFalse(store.Package0Package.ActionInfoSet.Contains("sql_text"));
        }

        /// <summary>
        /// Tests the ActionInfoSet for each package has the correct number of elements.
        /// </summary>
        public void TestActionInfoSet()
        {
            Assert.IsTrue(VerifyObjectCount(XEUtil.Package0PackageName, XEUtil.ActionTypeName, store.Package0Package.ActionInfoSet.Count));
        }
    }
}
