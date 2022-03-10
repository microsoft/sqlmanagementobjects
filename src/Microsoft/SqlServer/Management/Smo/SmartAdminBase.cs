// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class that handles Altering and Scripting the current state of smart admin
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class SmartAdmin : SqlSmoObject, Cmn.IAlterable, IScriptable
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartAdmin"/> class.
        /// </summary>
        public SmartAdmin()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartAdmin"/> class.
        /// </summary>
        /// <param name="parentsrv">SMO Server instance.</param>
        /// <param name="key">The key.</param>
        /// <param name="state">The state.</param>
        internal SmartAdmin(Server parentsrv, ObjectKeyBase key, SqlSmoState state)
            :
            base(key, state)
        {
            singletonParent = parentsrv;
            SetServerObject(parentsrv.GetServerObject());

            m_comparer = parentsrv.StringComparer;
        }

        #endregion

        #region Private member

        private string m_databaseName = null;
        private Boolean? m_isDroppedDB = false;
        private Boolean? m_isAvailabilityDB = false;

        #endregion

        #region Properties and their Public Accessors
        /// <summary>
        /// Gets Whether the name of database which the current object is managing
        /// when this property is set, a refresh will be triggered
        /// if the value is a null or empty string, instance level properties will be loaded in
        /// else, DB level properties will be loaded in
        /// </summary>
        /// <value>String.</value>
        [SfcProperty(SfcPropertyFlags.Computed)]
        public String DatabaseName
        {
            get
            {
                return m_databaseName;
            }
            set
            {
                if (NetCoreHelpers.StringCompare(m_databaseName, value, true, SmoApplication.DefaultCulture) != 0 &&
                    !(string.IsNullOrEmpty(value) && string.IsNullOrEmpty(m_databaseName)))
                {
                    this.BypassValues ();
                }
                m_databaseName = value;
            }
        }

        /// <summary>
        /// Gets Whether the DB is dropped
        /// This property only take effects when the current
        /// object represent management at DB Level
        /// </summary>
        /// <value>Bool.</value>
        [SfcProperty(SfcPropertyFlags.Computed)]
        public Boolean? IsDroppedDB
        {
            get
            {
                if (string.IsNullOrEmpty(m_databaseName))
                {
                    return null;
                }
                return m_isDroppedDB;
            }
            set
            {
                m_isDroppedDB = value;
            }
        }

        /// <summary>
        /// Gets Whether the DB is in AV group
        /// This property only take effects when the current
        /// object represent management at DB Level
        /// </summary>
        /// <value>Bool.</value>
        [SfcProperty(SfcPropertyFlags.Computed)]
        public Boolean? IsAvailabilityDB
        {
            get
            {
                if (string.IsNullOrEmpty(m_databaseName))
                {
                    return null;
                }
                return m_isAvailabilityDB;
            }
            set
            {
                m_isAvailabilityDB = value;
            }
        }


        /// <summary>
        /// Gets the parent Object. In this case it is Server
        /// </summary>
        /// <value>The parent.</value>
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Server;
            }
            internal set
            {
                SetParentImpl(value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Alters this instance.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Scripts this instance.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting optiions
        /// </summary>
        /// <param name="scriptingOptions">Scripting Options</param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Returns DataTable that has the health status of smart admin with default time range returned by TVF [fn_get_health_status]
        /// </summary>
        /// <returns>DataTable - based on query output from health status TVF</returns>
        public DataTable EnumHealthStatus()
        {
            return EnumHealthStatus(null, null);
        }

        /// <summary>
        /// Returns DataTable that has the health status of smart admin by specifying the start and end time ranges
        /// </summary>
        /// <param name="startDate">start date time for time range</param>
        /// <param name="endDate">end date time for the time range</param>
        /// <returns>DataTable - based on query output from health status TVF</returns>
        public DataTable EnumHealthStatus(DateTime? startDate, DateTime? endDate)
        {
            ThrowIfCloud();

            string startDateString = "default";
            string endDateString = "default";

            if (startDate != null && startDate.HasValue)
            {
                startDateString = String.Format(SmoApplication.DefaultCulture, "'{0}'", SqlSmoObject.SqlDateString((DateTime)startDate));
            }

            if (endDate != null && endDate.HasValue)
            {
                endDateString = String.Format(SmoApplication.DefaultCulture, "'{0}'", SqlSmoObject.SqlDateString((DateTime)endDate));
            }
            
            string healthStatusQuery = String.Format(SmoApplication.DefaultCulture,
                @"SELECT number_of_storage_connectivity_errors,
                        number_of_sql_errors,
                        number_of_invalid_credential_errors,
                        number_of_other_errors,
                        number_of_corrupted_or_deleted_backups,
                        number_of_backup_loops,
                        number_of_retention_loops
                    FROM  [msdb].[smart_admin].[fn_get_health_status]({0}, {1} )",
                startDateString,
                endDateString);

            DataSet ds = ExecutionManager.ExecuteWithResults(healthStatusQuery);
            DataTable dt = null;
            if (ds != null && ds.Tables.Count > 0)
            {
                dt = ds.Tables[0];
            }

            return dt;
        }
        /// <summary>
        /// override the parent class's Refresh method
        /// so that if the DatabaseName is not set
        /// call the parent class's Refresh to load in the most
        /// recent instance level auto admin records
        /// else load in the db level auto admin records
        /// </summary>
        public override sealed void Refresh()
        {
            if (string.IsNullOrEmpty(this.DatabaseName))
            {
                base.Refresh();
            }
            else
            {
                this.RefreshDBLevelProperties();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the Full Urn by traversing parent hierarchy
        /// </summary>
        /// <param name="urnbuilder">The urnbuilder.</param>
        /// <param name="idOption">The id option.</param>
        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        /// <summary>
        /// Generates Queries for Create operation
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="sp">Scripting  preferences</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        /// <summary>
        /// Generates Queries for Alter operation
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="sp">Scripting Preferences</param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Set parameters for scripts
        /// </summary>
        /// <param name="parameters">parameter list to be filled out</param>
        /// <param name="sp">Scripting preferences</param>
        private void SetParameters(List<string> parameters, ScriptingPreferences sp)
        {
            // flag to track if we need to script @enable_backup only - this is when smartbackup is disabled and 
            // we don't need to script any other property
            bool ignoreScriptingAllProperties = false;

            Property backupEnabled = Properties.Get("BackupEnabled");
            if (null != backupEnabled)
            {
                if (backupEnabled.Dirty || !sp.ScriptForAlter)
                {
                    if ((bool)backupEnabled.Value)
                    {
                        parameters.Add("@enable_backup = 1");
                    }
                    else
                    {
                        //ignoreScriptingAllProperties = true;
                        parameters.Add("@enable_backup = 0");
                    }
                }
            }

            // Ignore scripting other properties if @enable_backup = 0
            if (!ignoreScriptingAllProperties)
            {
                if (!String.IsNullOrEmpty(m_databaseName))
                {
                    parameters.Add(String.Format(SmoApplication.DefaultCulture, "@database_name = {0}", m_databaseName));
                }

                Property backupRetentionPeriod = Properties.Get("BackupRetentionPeriodInDays");
                if (null != backupRetentionPeriod)
                {
                    if (backupRetentionPeriod.Dirty || !sp.ScriptForAlter)
                    {
                        // script retention period
                        parameters.Add(String.Format(SmoApplication.DefaultCulture, "@retention_days = {0}", backupRetentionPeriod.Value.ToString()));
                    }
                }

                Property credentialName = Properties.Get("CredentialName");
                if (null != credentialName)
                {
                    if (credentialName.Dirty || !sp.ScriptForAlter)
                    {
                        // script credential name
                        parameters.Add(String.Format(SmoApplication.DefaultCulture, "@credential_name = N'{0}'", credentialName.Value.ToString()));
                    }
                }

                Property storageUrl = Properties.Get("StorageUrl");
                if (null != storageUrl)
                {
                    if (storageUrl.Dirty || !sp.ScriptForAlter)
                    {
                        // script storage url
                        parameters.Add(String.Format(SmoApplication.DefaultCulture, "@storage_url = N'{0}'", storageUrl.Value.ToString()));
                    }
                }

                Property encryptionAlgorithm = Properties.Get("EncryptionAlgorithm");
                if (null != encryptionAlgorithm)
                {
                    if (encryptionAlgorithm.Dirty || !sp.ScriptForAlter)
                    {
                        // script encryption algorithm
                        parameters.Add(String.Format(SmoApplication.DefaultCulture, "@encryption_algorithm = N'{0}'", encryptionAlgorithm.Value.ToString()));
                    }
                }

                Property encryptorType = Properties.Get("EncryptorType");
                if (null != encryptorType)
                {
                    if (encryptorType.Dirty || !sp.ScriptForAlter)
                    {
                        // script encryptor type
                        parameters.Add(String.Format(SmoApplication.DefaultCulture, "@encryptor_type = N'{0}'", encryptorType.Value.ToString()));
                    }
                }

                Property encryptorName = Properties.Get("EncryptorName");
                if (null != encryptorName)
                {
                    if (encryptorName.Dirty || !sp.ScriptForAlter)
                    {
                        // script encryptor name
                        parameters.Add(String.Format(SmoApplication.DefaultCulture, "@encryptor_name = N'{0}'", encryptorName.Value.ToString()));
                    }
                }
            }
        }

        /// <summary>
        /// Generates corresponding T-SQL based on the set properties
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="sp">Scripting preferences</param>
        private void ScriptProperties(StringCollection queries, ScriptingPreferences sp)
        {

            ThrowIfSourceOrDestBelowVersion120(sp.TargetServerVersionInternal);

            // Sample Query : 
            // turn on - master switch - EXEC [msdb].[smart_admin].[sp_backup_master_switch] @new_state  = 1
            // instance configuration : EXEC [msdb].[smart_admin].[sp_set_instance_backup] @retention_days = 1, @credential_name= 'AzureStorageCredential', @enable_backup = 1


            // Smart Admin's master switch
            Property masterSwitch = Properties.Get("MasterSwitch");
            if (null != masterSwitch && string.IsNullOrEmpty (this.DatabaseName))
            {
                if (masterSwitch.Dirty || !sp.ScriptForAlter)
                {
                    if ((bool)masterSwitch.Value)
                    {
                        queries.Add("EXEC [msdb].[smart_admin].[sp_backup_master_switch] @new_state  = 1;");
                    }
                    else
                    {
                        queries.Add("EXEC [msdb].[smart_admin].[sp_backup_master_switch] @new_state  = 0;");
                    }
                }
            }

            List<string> parameters = new List<string>();

            SetParameters(parameters, sp);

            if (parameters.Count > 0)
            {
                if (String.IsNullOrEmpty(m_databaseName))
                {
                    queries.Add(String.Format(SmoApplication.DefaultCulture,
                        "EXEC [msdb].[smart_admin].[sp_set_instance_backup] {0};",
                        String.Join(", ", parameters.ToArray()))
                        );
                }
                else
                {
                    queries.Add(String.Format(SmoApplication.DefaultCulture,
                        "EXEC [msdb].[smart_admin].[sp_set_db_backup] {0};",
                        String.Join(", ", parameters.ToArray()))
                        );
                }
            }

        }

        private void RefreshDBLevelProperties()
        {
            string databaseBackupQuery = String.Format(SmoApplication.DefaultCulture,
                @"SELECT is_availability_database,
                        is_dropped,
                        is_managed_backup_enabled,
                        credential_name,
                        retention_days,
                        storage_url,
                        encryption_algorithm,
                        encryptor_type,
                        encryptor_name
                    FROM  [msdb].[smart_admin].[fn_backup_db_config]('{0}')",
                this.m_databaseName);

            DataSet ds = ExecutionManager.ExecuteWithResults(databaseBackupQuery);
            DataTable dt = null;
            if (ds != null && ds.Tables.Count > 0)
            {
                dt = ds.Tables[0];
                if (dt.Rows.Count == 1)
                {
                    DataRow row = dt.Rows[0];
                    this.IsDroppedDB = (Boolean)row["is_dropped"];
                    this.IsAvailabilityDB = (Boolean)row["is_availability_database"];
                    Properties.SetValueWithConsistencyCheck("BackupEnabled", row["is_managed_backup_enabled"]);
                    Properties.SetValueWithConsistencyCheck("CredentialName", row["credential_name"]);
                    Properties.SetValueWithConsistencyCheck("BackupRetentionPeriodInDays", row["retention_days"]);
                    Properties.SetValueWithConsistencyCheck("StorageUrl", row["storage_url"]);
                    Properties.SetValueWithConsistencyCheck("EncryptionAlgorithm", row["encryption_algorithm"]);
                    if (row["encryptor_type"] == null || System.DBNull.Value.Equals (row["encryptor_type"]))
                    {
                        Properties.SetValueWithConsistencyCheck("EncryptorType", string.Empty);
                    }
                    else
                    {
                        Properties.SetValueWithConsistencyCheck("EncryptorType", row["encryptor_type"]);
                    }
                    if (row["encryptor_name"] == null || System.DBNull.Value.Equals (row["encryptor_name"]))
                    {
                        Properties.SetValueWithConsistencyCheck("EncryptorName", string.Empty);
                    }
                    else
                    {
                        Properties.SetValueWithConsistencyCheck("EncryptorName", row["encryptor_name"]);
                    }
                    
                    Properties.SetAllDirty(false);
                }
                else if (dt.Rows.Count == 0)
                {
                    throw new ArgumentException(String.Format(SmoApplication.DefaultCulture, LocalizableResources.SmartAdmin_NoSuchDB, this.DatabaseName));
                }
                else
                {
                    throw new ArgumentException(String.Format(SmoApplication.DefaultCulture, LocalizableResources.SmartAdmin_WrongRecords, this.DatabaseName, dt.Rows.Count));
                }
            }
        }

        /// <summary>
        /// In case we want to transfer the smart admin config value from one DB to another DB
        /// we need to set all the properties to be dirty, so that ScriptProperties will update them
        /// However we could not call the SetAllDirty, because then all the property values of current
        /// obj will be lost (a bug of SMO?), so manually set values back to themselves to trigger the dirty
        /// bit to be true
        /// </summary>
        private void BypassValues()
        {
            Properties.SetValueWithConsistencyCheck("BackupEnabled", this.BackupEnabled);
            Properties.SetValueWithConsistencyCheck("CredentialName", this.CredentialName);
            Properties.SetValueWithConsistencyCheck("BackupRetentionPeriodInDays", this.BackupRetentionPeriodInDays);
            Properties.SetValueWithConsistencyCheck("StorageUrl", this.StorageUrl);
            Properties.SetValueWithConsistencyCheck("EncryptionAlgorithm", this.EncryptionAlgorithm);
            Properties.SetValueWithConsistencyCheck("EncryptorType", this.EncryptorType, true);
            Properties.SetValueWithConsistencyCheck("EncryptorName", this.EncryptorName, true);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the urn suffix in the urn expression
        /// </summary>
        /// <value>The urn suffix for smartadmin.</value>
        public static string UrnSuffix
        {
            get
            {
                return "SmartAdmin";
            }
        }

        #endregion
    }
}

