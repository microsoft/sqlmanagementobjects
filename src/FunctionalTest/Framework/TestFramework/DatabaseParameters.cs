// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Defines parameters for the database used in the tests when using ExecuteWithDbDrop
    /// </summary>
    public class DatabaseParameters
    {
        /// <summary>
        /// The prefix to use for the database name. 
        /// </summary>
        public string NamePrefix { get; set; } = string.Empty;
        /// <summary>
        /// The required Azure edition of the database. Default is NotApplicable.
        /// </summary>
        public SqlTestBase.AzureDatabaseEdition AzureDatabaseEdition { get; set; } = SqlTestBase.AzureDatabaseEdition.NotApplicable;
        /// <summary>
        /// The path to a bak file or DAC package used to initialize the database.
        /// </summary>
        public string BackupFile { get; set; } = string.Empty;
        /// <summary>
        /// Whether to also create a snapshot of the database.
        /// </summary>
        public bool CreateSnapshot { get; set; }
        /// <summary>
        /// The desired collation for the database. When empty, the server default is used.
        /// </summary>
        public string Collation { get; set; } = string.Empty;
        /// <summary>
        /// Whether to include escaped characters in the database name. Default is true.
        /// </summary>
        public bool UseEscapedCharacters { get; set; } = true;
    }
}
