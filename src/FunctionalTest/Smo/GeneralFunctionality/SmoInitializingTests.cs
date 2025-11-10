// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;


namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// SFC enumerator tests
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class SmoInitializingTests : SqlTestBase
    {
        /// <summary>
        /// Verifies that calling ClearAndInitialize with a filter on a Table collection
        /// will correctly clear the collection and fill it with the tables that meet the
        /// filter. This works for DW too but it's slow to run
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void ClearAndInitializeTestInitializesTheTablesUsingTheFilter()
        {
            ExecuteWithDbDrop(
                dbNamePrefix: "SmoCleanAndInitializeTest",
                testMethod:
                db =>
                {
                    var table1Name = db.CreateTable("SmoCleanAndInitialize1").Name;
                    var table2Name = db.CreateTable("SmoCleanAndInitialize2").Name;
                    var table3Name = db.CreateTable("Initi'alize3SmoCleanAnd").Name;
                    db.Parent.SetDefaultInitFields(typeof(Table), nameof(Table.IsSystemObject), nameof(Table.LedgerType));
                    // no filter is our control group
                    Assert.That(db.Tables.Cast<Table>().Where(t => !t.IsImmutable()).Select(t => t.Name), Is.EquivalentTo(new[] {table1Name, table2Name, table3Name}),
                        "Unexpected number of tables before clearing and filtering");
                    db.Tables.ClearAndInitialize($"[(@Name = '{Urn.EscapeString(table1Name)}')]",
                        Array.Empty<string>());
                    var tableId = db.Tables[0].ID;
                    Assert.That(db.Tables.GetNames(), Is.EqualTo(new[] {table1Name}), "@Name =");
                    db.Tables.ClearAndInitialize("[contains(@Name, 'Initialize2')]", Array.Empty<string>());
                    Assert.That(db.Tables.GetNames(), Is.EqualTo(new[] {table2Name}), "contains(@Name");
                    db.Tables.ClearAndInitialize("[like(@Name, 'Initi''alize%')]", Array.Empty<string>());
                    Assert.That(db.Tables.GetNames(), Is.EqualTo(new[] {table3Name}), "like(@Name");
                    // The In filter function only works with integer values
                    db.Tables.ClearAndInitialize($"[in(@ID, '999, {tableId}')]", Array.Empty<string>());
                    Assert.That(db.Tables.GetNames(), Is.EqualTo(new[] { table1Name }), "in(@ID");
                    // We can use 'or' to match multiple names
                    db.Tables.ClearAndInitialize($"[@Name = '{Urn.EscapeString(table2Name)}' or @Name = '{Urn.EscapeString(table3Name)}']",
                        Array.Empty<string>());
                    Assert.That(db.Tables.GetNames(), Is.EquivalentTo(new[] { table2Name, table3Name }), "= or =");
                    db.Tables.ClearAndInitialize($"[not(like(@Name, '{Urn.EscapeString(table2Name)}'))]", Array.Empty<string>());
                    Assert.That(db.Tables.Cast<Table>().Where(t => !t.IsImmutable()).Select( t=> t.Name), Is.EquivalentTo(new[] { table1Name, table3Name }), "not like");
                });
        }
    }

    internal static class CollectionExtensions
    {
        public static IList<string> GetNames(this IEnumerable<SqlSmoObject> collection) 
        {
            return collection.Cast<NamedSmoObject>().Select(o => o.Name).ToList();
        }
    }
}
