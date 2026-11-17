// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Availability Group Data
    /// this Object contains info for an Availability Group
    /// </summary>
    public class AvailabilityGroupData
    {
        #region Constructors
        /// <summary>
        /// ctor: the base ctor
        /// set replicas = null
        /// set listener = new listener
        /// set backupreference = default secondary
        /// set Sync option = full
        /// </summary>
        public AvailabilityGroupData()
        {
            this.BackupPreference = AvailabilityGroupAutomatedBackupPreference.Secondary;
            this.IsBasic = false;
            this.IsDatabaseHealthTriggerOn = true;
            this.IsDtcSupportEnabled = true;
            this.AvailabilityGroupReplicas = null;
            this.PerformDataSynchronization = DataSynchronizationOption.Full;
            this.RequiredSynchronizedSecondariesToCommit = 0;
            this.IsContained = false;
        }

        /// <summary>
        /// the ctor for New AG, only Service Connection is passed
        /// </summary>
        /// <param name="primaryConnection"></param>
        public AvailabilityGroupData(ServerConnection primaryConnection)
            : this()
        {
            if (primaryConnection == null)
            {
                throw new ArgumentNullException("primaryConnection");
            }

            // Initialize the Service Connection
            this.PrimaryServer = new Smo.Server(primaryConnection);

            // the state is creating
            this.AvailabilityGroupState = AvailabilityObjectState.Creating;

            this.AvailabilityGroupReplicas = new AvailabilityGroupReplicaCollection();

            this.ExistingAvailabilityDatabases = new List<PrimaryDatabaseData>();

            this.NewAvailabilityDatabases = new List<PrimaryDatabaseData>();

            this.AvailabilityGroupListener = null;

            this.RequiredSynchronizedSecondariesToCommit = 0;
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// AG listener object
        /// </summary>
        public AvailabilityGroupListenerConfiguration AvailabilityGroupListener
        {
            get;
            set;
        }

        /// <summary>
        /// Primary Server object property
        /// </summary>
        public Smo.Server PrimaryServer
        {
            get;
            private set;
        }

        /// <summary>
        /// Availability Group name
        /// </summary>
        public string GroupName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the cluster type
        /// </summary>
        public AvailabilityGroupClusterType ClusterType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the required copies to commit
        /// </summary>
        public int RequiredSynchronizedSecondariesToCommit
        {
            get;
            set;
        }

        /// <summary>
        /// Network share location in Windows format used for storing backup files when user chooses to do backup/restore Data Synchronization
        /// </summary>
        public string BackupLocationInWindowsFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Network share location in Linux format used for storing backup files when user chooses to do backup/restore Data Synchronization
        /// </summary>
        public string BackupLocationInLinuxFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Databases that are already part of existing AvailabilityGroup
        /// </summary>
        public List<PrimaryDatabaseData> ExistingAvailabilityDatabases
        {
            get;
            private set;
        }

        /// <summary>
        /// Databases that are being added to AvailabilityGroup
        /// </summary>
        public List<PrimaryDatabaseData> NewAvailabilityDatabases
        {
            get;
            private set;
        }

        /// <summary>
        /// All replicas - including the primary
        /// </summary>
        public IList<AvailabilityGroupReplica> AvailabilityGroupReplicas
        {
            get;
            set;
        }

        /// <summary>
        /// Secondary replicas
        /// </summary>
        public IEnumerable<AvailabilityGroupReplica> Secondaries
        {
            get
            {
                return this.AvailabilityGroupReplicas.Where(replica => replica.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Secondary);
            }
        }

        /// <summary>
        /// Secondary replicas excluding the ConfigurationOnly replica
        /// </summary>
        public IEnumerable<AvailabilityGroupReplica> DataSecondaries
        {
            get
            {
                return this.AvailabilityGroupReplicas.Where(replica =>
                    replica.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Secondary
                    && replica.AvailabilityGroupReplicaData.AvailabilityMode != AvailabilityReplicaAvailabilityMode.ConfigurationOnly);
            }
        }

        /// <summary>
        /// Availability Group BackUp Preference
        /// </summary>
        public AvailabilityGroupAutomatedBackupPreference BackupPreference
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether to create a BASIC or ADVANCED Availability Group.  
        /// </summary>
        public bool IsBasic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether a DB Health event triggers Failover to a auto-failover synchronous replica.
        /// </summary>
        public bool IsDatabaseHealthTriggerOn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether per-database DTC support is enabled.
        /// </summary>
        public bool IsDtcSupportEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether this is a Contained AG or not.
        /// </summary>
        public bool IsContained
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the Contained AG should be
        /// created by reusing the system databases or not.
        /// </summary>
        /// <remarks>
        /// This property is read/write, however keep in mind that the corresponding
        /// propery on the AvailabilityGroup SMO object is write-only, because the
        /// value cannot be fetched from SQL Server once the AG has been created.
        /// </remarks>
        public bool ReuseSystemDatabases
        {
            get;
            set;
        }

        /// <summary>
        /// Data Synchronization preference
        /// </summary>
        public DataSynchronizationOption PerformDataSynchronization
        {
            get;
            set;
        }

        /// <summary>
        /// Checks to see if DataSyncronization is set to full
        /// </summary>
        public bool WillPerformBackupRestore
        {
            get
            {
                return PerformDataSynchronization == DataSynchronizationOption.Full;
            }
        }

        /// <summary>
        /// Checks to see if DataSynchronizationOption is Full, JoinOnly or AutomaticSeeding
        /// </summary>
        public bool WillPerformDatabaseJoin
        {
            get
            {
                return PerformDataSynchronization == DataSynchronizationOption.Full 
                    || PerformDataSynchronization == DataSynchronizationOption.JoinOnly
                    || PerformDataSynchronization == DataSynchronizationOption.AutomaticSeeding;
            }
        }

        /// <summary>
        /// Checks to see if DataSynchronizationOption is Full or AutomaticSeeding
        /// </summary>
        public bool WillPerformDatabaseInitialization
        {
            get
            {
                return PerformDataSynchronization == DataSynchronizationOption.Full 
                    || PerformDataSynchronization == DataSynchronizationOption.AutomaticSeeding;
            }
        }

        /// <summary>
        /// Checks to see if DataSynchronizationOption is AutomaticSeeding
        /// </summary>
        public bool WillPerformAutomaticSeeding
        {
            get
            {
                return PerformDataSynchronization == DataSynchronizationOption.AutomaticSeeding;
            }
        }

        /// <summary>
        /// Windows failover cluster DNS name
        /// </summary>
        public string ClusterName
        {
            get;
            set;
        }

        /// <summary>
        /// Determines if the primary connection is part of cluster quorum.  Returns true if yes, false otherwise.
        /// </summary>
        public bool IsPrimaryInQuorum
        {
            get
            {
                var quorumState = this.PrimaryServer.ClusterQuorumState;
                return quorumState != ClusterQuorumState.NotApplicable && quorumState != ClusterQuorumState.UnknownQuorumState;
            }
        }

        /// <summary>
        /// AG State flag
        /// </summary>
        public AvailabilityObjectState AvailabilityGroupState
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper method for checking View Server State Permissiom
        /// </summary>
        public bool HasViewServerStatePermission(Smo.Server server)
        {
            return HasPermissionOnServer(server, "VIEW SERVER STATE");
        }

        /// <summary>
        /// Determines if PrimaryServer is actually the primary. Returns true if yes, false otherwise.
        /// </summary>
        public bool IsConnectedToPrimary
        {
            get
            {
                if (this.PrimaryServer != null && this.PrimaryServer.ConnectionContext != null)
                {
                    AvailabilityGroup ag = this.PrimaryServer.AvailabilityGroups[this.GroupName];
                    if (ag != null)
                    {
                        string trueName = this.PrimaryServer.ConnectionContext.TrueName;
                        return string.Equals(ag.PrimaryReplicaServerName, trueName, StringComparison.OrdinalIgnoreCase);
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// This property is used specially for creating a listener. We want the step of creating a AG listener not to include
        /// in the step of creating AG. However, The creation of a listener needs such info.
        /// </summary>
        public AvailabilityGroup SqlAvailabilityGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether the replicas of the availability group are on different OS platforms.
        /// </summary>
        public bool IsCrossPlatform
        {
            get { return this.Secondaries.Any(replica => replica.GetServer().HostPlatform != this.PrimaryServer.HostPlatform); }
        }

        /// <summary>
        /// Specifies the cluster connection options used by WSFC to connect to SQL Server via ODBC. 
        /// This enables TDS 8.0 and is only applicable when the cluster type is set to "WSFC" 
        /// and the SQL Server version is 2025 or later.
        /// </summary>
        public string ClusterConnectionOptions
        {
            get;
            set;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Return the Login Names
        /// Method behavior based on the input parameter:
        /// 1. If the replica is a non-Windows replica, returns an empty collection
        /// 2. else go through all replicas in Replicas and return
        /// KeyValuePair contains a localServiceAccount and a flag for the existance of sid
        /// </summary>
        /// <param name="replica"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, bool>> GetLoginNames(AvailabilityGroupReplica replica)
        {
            if (replica.AvailabilityGroupReplicaData.Connection.HostPlatform != HostPlatformNames.Windows)
            {
                return Enumerable.Empty<KeyValuePair<string, bool>>();
            }

            return replica.GetLoginsForRemoteReplicas(this.GetUniqueServiceAccountSids(replica));
        }

        /// <summary>
        /// retrieve the total size of datafiles and logs of the primary server
        /// </summary>
        /// <param name="totalDataFilesSize"></param>
        /// <param name="totalLogFilesSize"></param>
        public void GetPrimaryTotalDataFileAndLogSize(out double totalDataFilesSize, out double totalLogFilesSize)
        {
            totalDataFilesSize = 0.0;
            totalLogFilesSize = 0.0;
            List<string> databaseNames = new List<string>();

            if (this.NewAvailabilityDatabases.Count != 0)
            {
                foreach (PrimaryDatabaseData dbData in this.NewAvailabilityDatabases)
                {
                    databaseNames.Add(dbData.Name);
                }
            }
            else
            {
                AvailabilityGroup availabilityGroup = PrimaryServer.AvailabilityGroups[this.GroupName];

                // Before accessing the AvailabilityDatabases collection of AG, verify once again
                // if the AG exists on the backend currently
                if (availabilityGroup == null)
                {
                    throw new InvalidOperationException(string.Format(Resource.AvailabilityGroupNotExistError, this.GroupName, this.PrimaryServer.Name));
                }
                else
                {
                    foreach (AvailabilityDatabase adb in availabilityGroup.AvailabilityDatabases)
                    {
                        databaseNames.Add(adb.Name);
                    }
                }
            }

            // Calculates the total data and log file size of each selected database in the wizard
            foreach (string dbName in databaseNames)
            {
                double dataFilesSize = 0.0;
                double logFilesSize = 0.0;

                Database database = this.PrimaryServer.Databases[dbName];

                foreach (FileGroup fg in database.FileGroups)
                {
                    // Filegroup.Size returns the size in KBytes
                    dataFilesSize += (fg.Size / 1024);

                }
                totalDataFilesSize += dataFilesSize;

                logFilesSize = (database.Size - dataFilesSize);
                totalLogFilesSize += logFilesSize;
            }
        }

        /// <summary>
        /// The method used to add a database to the AvailabilityGroup
        /// </summary>
        /// <param name="database">The database data to add</param>
        public void AddAvailabilityDatabase(PrimaryDatabaseData database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            this.NewAvailabilityDatabases.Add(database);
        }

        /// <summary>
        /// The method used to add a database to the AvailabilityGroup
        /// </summary>
        /// <param name="database">The database data to add</param>
        public void AddExistingAvailabilityDatabase(PrimaryDatabaseData database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            this.ExistingAvailabilityDatabases.Add(database);
        }

        /// <summary>
        /// Get the backup location for the server
        /// </summary>
        /// <param name="server">The target server</param>
        /// <returns>Backup location in the format that can be used by the server</returns>
        public string GetBackupPathForServer(Smo.Server server)
        {
            return server.HostPlatform == HostPlatformNames.Windows ? this.BackupLocationInWindowsFormat : this.BackupLocationInLinuxFormat;
        }

        /// <summary>
        /// retrive all the SIDs and service accounts in other replicas in an AG
        /// </summary>
        /// <param name="replica"></param>
        /// <returns></returns>
        private IEnumerable<KeyValuePair<byte[], string>> GetUniqueServiceAccountSids(AvailabilityGroupReplica replica)
        {
            Dictionary<byte[], string> sidServiceAccountPairCollection = new Dictionary<byte[], string>();

            // add and grant connect on endpoint
            foreach (AvailabilityGroupReplica additionalReplica in AvailabilityGroupReplicas)
            {
                if (replica == additionalReplica || additionalReplica.AvailabilityGroupReplicaData.EndpointServiceAccountSid == null)
                {
                    continue;
                }

                if (!sidServiceAccountPairCollection.Any<KeyValuePair<byte[], string>>(x => x.Key.SequenceEqual<byte>(additionalReplica.AvailabilityGroupReplicaData.EndpointServiceAccountSid)))
                {
                    sidServiceAccountPairCollection.Add(additionalReplica.AvailabilityGroupReplicaData.EndpointServiceAccountSid, additionalReplica.AvailabilityGroupReplicaData.EndpointServiceAccount);
                }
            }
            return sidServiceAccountPairCollection;
        }

        private static bool HasPermissionOnServer(Smo.Server server, string permissionName)
        {
            return Convert.ToBoolean(server.ConnectionContext.ExecuteScalar(
                string.Format(CultureInfo.InvariantCulture,
                    "SELECT HAS_PERMS_BY_NAME(null, null, '{0}');",
                    permissionName)));
        }
        #endregion
    }
}
