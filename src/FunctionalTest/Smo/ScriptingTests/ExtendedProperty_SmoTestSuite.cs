// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ExtendedProperty properties and scripting
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
    public class ExtendedProperty_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ExtendedProperty ep = (_SMO.ExtendedProperty)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.ExtendedProperties.Refresh();
            Assert.IsNull(database.ExtendedProperties[ep.Name],
                            "Current extended property not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an extended property with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        public void SmoDropIfExists_ExtendedProperty()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.ExtendedProperty ep = new _SMO.ExtendedProperty(database,
                        GenerateSmoObjectName("ep"))
                    {
                        Value = "Test database"
                    };

                    VerifySmoObjectDropIfExists(ep, database);
                });
        }

        [TestMethod]
        public void ExtendedProperty_Create_and_Drop()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    var epName = GenerateSmoObjectName("ep");
                    var epValue = "Test database";
                    _SMO.ExtendedProperty ep = new _SMO.ExtendedProperty(database, epName)
                    {
                        Value = epValue
                    };

                    Assert.DoesNotThrow(ep.Create, "Extended property create should succeed");
                    Assert.That(database.ExtendedProperties.Cast<ExtendedProperty>().Select(e => (e.Name, e.Value)), Is.EquivalentTo(new[] { (epName, epValue) }), "Unexpected extended property collection");
                    Assert.DoesNotThrow(database.ExtendedProperties[epName].Drop, "Drop should succeed");
                    database.ExtendedProperties.Refresh();
                    Assert.That(database.ExtendedProperties.Cast<ExtendedProperty>().Select(e => e.Name), Is.Empty, "ExtendedProperties should be empty after Drop");
                });
        }

        #endregion
    }
}
