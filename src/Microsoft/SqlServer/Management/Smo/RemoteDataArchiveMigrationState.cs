// Copyright (c) Microsoft.
// Licensed under the MIT license.
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Current data migration state of a stretched table
    /// Refer remote_data_archive_migration_state in https://msdn.microsoft.com/en-us/library/ms187406.aspx
    /// </summary>
    public enum RemoteDataArchiveMigrationState
    {
        /// <summary>
        /// The table is not Stretch-enabled
        /// </summary>
        Disabled = 0,
        
        /// <summary>
        /// The table is stretch-enabled, but the data migration is paused
        /// when the direction of data-flow is outbound
        /// </summary>
        PausedOutbound = 1,

        /// <summary>
        /// The table is stretch-enabled, but the data migration is paused
        /// when the direction of data-flow is inbound
        /// </summary>
        PausedInbound = 2,
        
        /// <summary>
        /// The table is Stretch-enabled, and data migration direction is out-bound to azure
        /// </summary>
        Outbound = 3,

        /// <summary>
        /// The table is Stretch-enabled, and data migration direction is in-bound from azure
        /// </summary>
        Inbound = 4,
        
        /// <summary>
        /// This value has been kept for backward compatibility. It will be removed once all
        /// the references to it are removed.
        /// TODO: remove this value
        /// </summary>
        Paused = 5
    }
}
