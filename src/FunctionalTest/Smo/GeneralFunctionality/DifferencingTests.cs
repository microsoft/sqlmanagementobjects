// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Differencing;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.SqlServer.Test.SMO.ScriptingTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Tests for the differencing service
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    [UnsupportedFeature(SqlFeature.Fabric)]
    public class DifferencingTests : SmoTestBase
    {
        [TestMethod]
        public void Differencing_identifies_one_table_change()
        {
            ExecuteFromDbPool("Differencing", db =>
            {
                var table1 = db.CreateTable("tbl1", new ColumnProperties("col1") { Nullable  = false });
                var table2 = db.CreateTable("tbl2", new ColumnProperties("colExtra") { Nullable = false });
                var differencer = DifferencingService.Service.CreateDifferencer();
                // ToList forces the enumeration to walk the whole graph before we move on to the Assert
                var diff = differencer.CompareGraphs(table1, table2).ToList();
                Assert.Multiple(() =>
                {
                    // Note that EqualTo enforces ordering while EquivalentTo does not
                    Assert.That(diff.Select(d => d.ChangeType), Is.EqualTo(new[] {DiffType.Created,DiffType.Deleted}), nameof(IDiffEntry.ChangeType));
                    Assert.That(diff.Select(d => d.Source?.Type), Is.EqualTo(new [] {Column.UrnSuffix, null}), nameof(IDiffEntry.Source) + nameof(Urn.Type));
                    Assert.That(diff.Select(d => d.Source?.GetNameForType(Column.UrnSuffix)), Is.EqualTo(new [] {"col1", null}), nameof(IDiffEntry.Source) + " Name");
                    Assert.That(diff.Select(d => d.Target?.Type), Is.EqualTo(new[] {null, Column.UrnSuffix}), nameof(IDiffEntry.Target) + nameof(Urn.Type));
                    Assert.That(diff.Select(d => d.Target?.GetNameForType(Column.UrnSuffix)), Is.EqualTo(new[] { null, "colExtra" }), nameof(IDiffEntry.Target) + " Name");
                });
            });
        }

    }
}
