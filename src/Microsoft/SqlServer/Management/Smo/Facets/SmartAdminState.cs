// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This facet aggregates smartadmin state information. It is used to support 
    /// SQL Server manageability tools.
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("SmartAdminStateName")]
    [Sfc.DisplayDescriptionKey("SmartAdminStateDesc")]
    public interface ISmartAdminState : Sfc.IDmfFacet, IRefreshable 
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether master switch is enabled.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_IsMasterSwitchEnabledName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_IsMasterSwitchEnabledDesc")]
        bool IsMasterSwitchEnabled
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether smartbackup is enaabled.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_IsBackupEnabledName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_IsBackupEnabledDesc")]
        bool IsBackupEnabled
        {
            get;
        }

        /// <summary>
        /// Gets the number of storage account connectivity errors.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_NumberOfStorageConnectivityErrorsName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_NumberOfStorageConnectivityErrorsDesc")]
        int NumberOfStorageConnectivityErrors
        {
            get;
        }

        /// <summary>
        /// Gets the number of SQL Server connectivity errors.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_NumberOfSqlErrorsName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_NumberOfSqlErrorsDesc")]
        int NumberOfSqlErrors
        {
            get;
        }

        /// <summary>
        /// Gets the number of invalid Microsoft Azure storage credential errors.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_NumberOfInvalidCredentialErrorsName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_NumberOfInvalidCredentialErrorsDesc")]
        int NumberOfInvalidCredentialErrors
        {
            get;
        }

        /// <summary>
        /// Gets the number of all other errors.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_NumberOfOtherErrorsName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_NumberOfOtherErrorsDesc")]
        int NumberOfOtherErrors
        {
            get;
        }

        /// <summary>
        /// Gets the number of invalid Microsoft Azure srotage credential errors.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_NumberOfCorruptedOrDeletedBackupsName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_NumberOfCorruptedOrDeletedBackupsDesc")]
        int NumberOfCorruptedOrDeletedBackups
        {
            get;
        }

        /// <summary>
        /// Gets the number of backup loops.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_NumberOfBackupLoopsName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_NumberOfBackupLoopsDesc")]
        int NumberOfBackupLoops
        {
            get;
        }

        /// <summary>
        /// Gets the number of retention loops.
        /// </summary>
        [Sfc.DisplayNameKey("SmartAdminState_NumberOfRetentionLoopsName")]
        [Sfc.DisplayDescriptionKey("SmartAdminState_NumberOfRetentionLoopsDesc")]
        int NumberOfRetentionLoops
        {
            get;
        }

        #endregion
    }

    /// <summary>
    /// This is an adapter class that implements the <see cref="ISmartAdminState"/> logical facet for 
    /// an Availability Group.
    /// </summary>
    public partial class SmartAdminState : ISmartAdminState, IDmfAdapter, IRefreshable
    {
        /// <summary>
        /// Initializes a new instance of the SmartAdminState class.
        /// </summary>
        /// <param name="smartadmin">The smartadmin object.</param>
        public SmartAdminState(SmartAdmin smartadmin)
        {
            this.smartAdmin = smartadmin;
            this.isInitialized = false;
        }

        #region ISmartAdminState Members

        /// <summary>
        /// Gets a value indicating whether master switch is on
        /// </summary>
        public bool IsMasterSwitchEnabled
        {
            get 
            {
                this.CheckInitialized();

                return this.isMasterSwitchEnabled; 
            }
        }

        /// <summary>
        /// Gets a value indicating whether smart backup is enabled or not.
        /// </summary>
        public bool IsBackupEnabled
        {
            get 
            {
                this.CheckInitialized();

                return this.isBackupEnabled; 
            }
        }
        
 
        /// <summary>
        /// Gets the number of Storage connectivity errors.
        /// </summary>
        public int NumberOfStorageConnectivityErrors
        {
            get 
            { 
                this.CheckInitialized();

                return this.numberOfStorageConnectivityErrors;
            }
        }

        /// <summary>
        /// Gets the number of SQL Errors seen by smartadmin components(main loop, retention threads).
        /// </summary>
        public int NumberOfSqlErrors
        {
            get 
            {
                this.CheckInitialized();

                return this.numberOfSqlErrors;
            }
        }

        /// <summary>
        /// Gets the number of Invalid storage credential errors.
        /// </summary>
        public int NumberOfInvalidCredentialErrors
        {
            get
            {
                this.CheckInitialized();

                return this.numberOfInvalidCredentialErrors;
            }
        }

        /// <summary>
        /// Gets the number of all other errors.
        /// </summary>
        public int NumberOfOtherErrors
        {
            get
            {
                this.CheckInitialized();

                return this.numberOfOtherErrors;
            }
        }

        /// <summary>
        /// Gets the number Corrupted or deleted backups
        /// </summary>
        public int NumberOfCorruptedOrDeletedBackups
        {
            get
            {
                this.CheckInitialized();

                return this.numberOfCorruptedOrDeletedBackups;
            }
        }

        /// <summary>
        /// Gets the number of backup loops. If zero then main loop is stalled.
        /// </summary>
        public int NumberOfBackupLoops
        {
            get
            {
                this.CheckInitialized();

                return this.numberOfBackupLoops;
            }
        }

        /// <summary>
        /// Gets the number of retention loops. If zero then retention thread is stalled.
        /// </summary>
        public int NumberOfRetentionLoops
        {
            get
            {
                this.CheckInitialized();

                return this.numberOfRetentionLoops;
            }
        }
        #endregion

        #region IRefreshable Members

        /// <summary>
        /// Refresh the smartadmin object state
        /// </summary>
        public void Refresh()
        {
            this.smartAdmin.Refresh();
            this.isInitialized = false;
        }

        #endregion

        #region Private memebers

        private bool isInitialized;
        private SmartAdmin smartAdmin;

        private bool isMasterSwitchEnabled;
        private bool isBackupEnabled;
        private int numberOfStorageConnectivityErrors;
        private int numberOfSqlErrors;
        private int numberOfInvalidCredentialErrors;
        private int numberOfOtherErrors;
        private int numberOfCorruptedOrDeletedBackups;
        private int numberOfBackupLoops;
        private int numberOfRetentionLoops;

        private void Initialize()
        {
            this.isMasterSwitchEnabled = this.smartAdmin.MasterSwitch;
            this.isBackupEnabled = this.smartAdmin.BackupEnabled;

            DataTable dt = this.smartAdmin.EnumHealthStatus();
            if (dt.Rows.Count > 0)
            {
                // We are expecting only one row in resultset
                DataRow row = dt.Rows[0];
                this.numberOfStorageConnectivityErrors = (int)row["number_of_storage_connectivity_errors"];
                this.numberOfSqlErrors = (int)row["number_of_sql_errors"];
                this.numberOfInvalidCredentialErrors = (int)row["number_of_invalid_credential_errors"];
                this.numberOfOtherErrors = (int)row["number_of_other_errors"];
                this.numberOfCorruptedOrDeletedBackups = (int)row["number_of_corrupted_or_deleted_backups"];
                this.numberOfBackupLoops = (int)row["number_of_backup_loops"];
                this.numberOfRetentionLoops = (int)row["number_of_retention_loops"];
            }

            this.isInitialized = true;
        }

        private void CheckInitialized()
        {
            if (!this.isInitialized)
            {
                this.Initialize();
            }
        }

        #endregion
    }
}
