// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// The feature of SQL Server
    /// Test cases uses this enum to mark the required features in order to run the test and test framework will find the servers with the required features enabled
    /// </summary>
    public enum SqlFeature
    {
        /// <summary>
        /// Always On Availability Groups
        /// </summary>
        AlwaysOn,

        /// <summary>
        /// Azure SSIS
        /// </summary>
        AzureSSIS,

        /// <summary>
        /// In-memory aka Hekaton
        /// </summary>
        Hekaton,

        /// <summary>
        /// SqlClr hosting
        /// </summary>
        SqlClr,

        /// <summary>
        /// Azure Active Directory Logins SqlDB
        /// </summary>
        AADLoginsSqlDB,

        /// <summary>
        /// Azure Ledger Database support
        /// </summary>
        AzureLedger
    }
}
