// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Extension methods for Table objects
    /// </summary>
    public static class TableExtensions
    {
        /// <summary>
        /// Returns true if the table is a ledger table or otherwise not able to be dropped. Safe to call for any server version.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool IsImmutable(this Table table)
        {
            return table.IsSupportedProperty(nameof(Table.LedgerType)) && table.LedgerType != LedgerTableType.None;
        }
    }
}
