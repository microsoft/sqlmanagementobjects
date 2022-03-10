// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.Sfc
{
    /// <summary>
    /// For IAlienObject methods not used in serialization
    /// </summary>
    [TestClass]
    // Unsupported for SQL OD because CREATE TABLE is unsupported
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class AlienTests : SqlTestBase
    {
        [TestMethod]
        public void AlienObject_methods_on_database()
        {
            // SqlSmoObject has most of the implementation of these methods
            ExecuteFromDbPool(db =>
            {
                var table = db.CreateTable("alien");
                var alienObject = (IAlienObject)db;
                var resolvedObject = alienObject.Resolve(table.Urn);
                var domainRoot = alienObject.GetDomainRoot();
                var parent = alienObject.GetParent();
                var propertyType = alienObject.GetPropertyType(nameof(db.EncryptionEnabled));
                var propertyValue = alienObject.GetPropertyValue(nameof(db.EncryptionEnabled), propertyType);
                var urn = alienObject.GetUrn();
                if (db.IsSupportedProperty(nameof(db.ChangeTrackingRetentionPeriod)))
                {
                    alienObject.SetPropertyValue(nameof(db.ChangeTrackingRetentionPeriod), typeof(int), 1111);
                }
                alienObject.SetObjectState(SfcObjectState.Dropped);
                Assert.Multiple(() =>
                {
                    Assert.That(db.State, Is.EqualTo(SqlSmoState.Dropped), "SetObjectState(Dropped) sets State");
                    Assert.That(() => { var x = db.EncryptionEnabled; }, Throws.InstanceOf<SmoException>(), "Unable to access properties after setting state to dropped");
                    db.SetState(SqlSmoState.Existing);
                    Assert.That(resolvedObject, Is.InstanceOf<Table>(), "Resolve(table.Urn) returns a table");
                    Assert.That((resolvedObject as Table)?.Name, Is.EqualTo(table.Name), "Resolved correct table");
                    Assert.That(domainRoot, Is.InstanceOf<_SMO.Server>(), "GetDomainRoot");
                    Assert.That(propertyType, Is.EqualTo(typeof(bool)), "GetPropertyType(EncryptionEnabled)");
                    Assert.That(propertyValue, Is.EqualTo(db.EncryptionEnabled), "GetPropertyValue(EncryptionEnabled)");

                    if (db.IsSupportedProperty(nameof(db.ChangeTrackingRetentionPeriod)))
                    {
                        Assert.That(db.ChangeTrackingRetentionPeriod, Is.EqualTo(1111), "SetPropertyValue(ChangeTrackingRetentionPeriod)");
                    }
                });
            });
        }
    }
}
