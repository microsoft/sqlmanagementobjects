// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Broker;
using Cmn = Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [PhysicalFacet]
    public partial class Database : ScriptNameObjectBase, ICreatable, IAlterable, IDroppable, IDropIfExists,
    ISafeRenamable, IExtendedProperties, IScriptable, IDatabaseOptions
    {
        internal Database(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            m_LogFiles = null;
            m_FileGroups = null;
            m_PlanGuides = null;
            m_Tables = null;
            m_SensitivityClassifications = null;
            m_StoredProcedures = null;
            m_ExtendedStoredProcedures = null;
            m_UserDefinedFunctions = null;
            m_Views = null;
            m_Users = null;
            m_Roles = null;
            m_Defaults = null;
            m_Rules = null;
            m_UserDefinedDataTypes = null;
            m_UserDefinedTableTypes = null;
            m_ServiceBroker = null;
            m_PartitionFunctions = null;
            m_PartitionSchemes = null;
            m_SqlAssemblies = null;
            m_UserDefinedTypes = null;
            m_UserDefinedAggregates = null;
            m_FullTextCatalogs = null;
            m_DatabaseEncryptionKey = null;
            databaseAuditSpecifications = null;
            m_FullTextStopLists = null;
            m_SearchPropertyLists = null;
            m_SecurityPolicies = null;
            m_ExternalDataSources = null;
            m_ExternalFileFormats = null;
            m_ColumnMasterKeys = null;
            m_ColumnEncryptionKeys = null;
            m_DatabaseScopedCredentials = null;
            m_DatabaseScopedConfigurations = null;
        }

        /// <summary>
        /// Constructs a new Database object to be created with the given edition
        /// </summary>
        /// <param name="server"></param>
        /// <param name="name"></param>
        /// <param name="edition"></param>
        public Database(Server server, string name, DatabaseEngineEdition edition) : this(server, name)
        {
            m_edition = edition;
        }

        /// <summary>
        /// The Engine Edition (such as SQL Datawarehouse) of this database. This is either set by the caller or
        /// loaded
        /// </summary>
        /// <remarks>This is nullable so we can tell when the value has been checked but the DB does not yet
        /// exist (the value will be set to Unknown in that case) versus not yet checked</remarks>
        private DatabaseEngineEdition? m_edition = null;
        private object syncRoot = new Object();
        private ExecutionManager m_dbExecutionManager;
        /// <summary>
        ///   This is required for CloudDB
        /// </summary>
        internal ExecutionManager DatabaseExecutionManager
        {
            get
            {
                if (this.m_dbExecutionManager == null)
                {
                    lock (this.syncRoot)
                    {
                        if (this.m_dbExecutionManager == null)
                        {
                            var serverContext = this.GetServerObject().ConnectionContext;
                            var pooling = !serverContext.NonPooledConnection;
                            this.m_dbExecutionManager =
                                new ExecutionManager(
                                    this.GetServerObject().ConnectionContext.GetDatabaseConnection(this.Name, pooling))
                                {
                                    Parent = this
                                };
                        }
                    }
                }
                return this.m_dbExecutionManager;
            }
        }

        /// <summary>
        /// Returns the ExecutionManager instance for running queries to gather information
        /// about the database.
        /// </summary>
        public override ExecutionManager ExecutionManager
        {
            get
            {
                
                if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    return DatabaseExecutionManager;
                }
                return base.ExecutionManager;
            }
        }
        
        /// <summary>
        /// Returns the DatabaseEngineType of the database instance
        /// </summary>
        public override DatabaseEngineType DatabaseEngineType
        {
            get
            {
                Server server = this.GetServerObject();
                if (server != null)
                {
                    // handles the cloud DB case too (using Server's Execution Manager)
                    return server.ExecutionManager.GetDatabaseEngineType();
                }
                else
                {
                    // when we don't have a server connected, assume an on-prem server
                    // This is used in a scenario, when we need
                    // to get metadata without a server connection
                    return DatabaseEngineType.Standalone;
                }
            }
        }

        public override DatabaseEngineEdition DatabaseEngineEdition
        {
            get
            {
                //DatabaseEngineEdition is a database level property so we need to query
                //the execution manager directly so that it goes to the database on Azure

                //We cache this value locally so that the caller can specify what edition
                //they want to target when creating a DB (since we won't be able to know
                //what kind it is otherwise). If it's unknown and the state is created though
                //we still want to query for the value since it should be able to be
                //retrieved at that point.
                if (m_edition == null ||
                    (m_edition == DatabaseEngineEdition.Unknown && this.State == SqlSmoState.Existing))
                {

                    try
                    {
                        m_edition = this.ExecutionManager.GetDatabaseEngineEdition();
                    }
                    catch (ConnectionFailureException cfe)
                    {
                        SqlException se = cfe.InnerException as SqlException;
                        if (se != null && (se.Number == 40892 || se.Number == 4060 ||
                           (this.State == SqlSmoState.Existing && (se.Number == 916 || se.Number == 18456 || se.Number == 110003))))
                        {
                            // Pass args as params since cfe may contain {#} in it which will cause formatting to fail if inserted directly
                            Diagnostics.TraceHelper.Trace("Database SMO Object", "Failed to connect for edition fetch, defaulting to Unknown edition. State: {0} PropertyBagState: {1} {2}", State, propertyBagState, cfe);
                            // - 916 is "Cannot open database <...> requested by the login.. The login failed....
                            // - 18456 is Login failed for user <...>
                            // - 4060 is "Database not accessible" error. It might still be in sys.databases but being deleted.
                            // - 40892 means database is deactivated
                            // - 110003 is the DW/Synapse equivalent of 18456/916
                            //   We expect this if the database may not exist, it is deactivated,
                            //   or the user does not have permissions to open it.
                            //   In this case, we'll set it as Unknown and continue on as normal
                            m_edition = DatabaseEngineEdition.Unknown;
                        }
                        else
                        {
                            Diagnostics.TraceHelper.Trace("Database SMO Object", $"Failed to connect for edition fetch. State: {State}, SqlException number: {(cfe.InnerException as SqlException)?.Number}");
                            throw;
                        }
                    }
                }
                //If we haven't been able to retrieve a value yet (due to unexpected exception)
                //we'll leave it as null so we can try again later, but still return Unknown
                return m_edition ?? DatabaseEngineEdition.Unknown;
               }
        }

        private OptionTerminationStatement optionTerminationStatement = null;
        /// <summary>
        /// Returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "Database";
            }
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "CompatibilityLevel")
            {

                bool compatIsValid = false;
                // For Azure we just have a min compat; they get higher compat levels before box
                if (DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    compatIsValid = ((CompatibilityLevel)value) >= CompatibilityLevel.Version100;
                }
                else
                {
                    switch (this.ServerVersion.Major)
                    {
                        // SMO that is external to users exposes versions greater than 8, so there are no cases less than 8
                        case 8:  // Shiloh
                            if ((CompatibilityLevel)value <= CompatibilityLevel.Version80)
                            {
                                compatIsValid = true; // we're fine
                            }
                            break;

                        case 9: // Yukon
                            if ((CompatibilityLevel)value <= CompatibilityLevel.Version90)
                            {
                                compatIsValid = true; // we're fine
                            }
                            break;
                        case 10: // Katmai
                            if ((CompatibilityLevel)value <= CompatibilityLevel.Version100)
                            {
                                compatIsValid = true; // we're fine
                            }
                            break;

                        case 11://Denali
                            if ((CompatibilityLevel)value >= CompatibilityLevel.Version90 && (CompatibilityLevel)value <= CompatibilityLevel.Version110)
                            {
                                compatIsValid = true;
                            }
                            break;
                        case 12://SQL14
                            var maxVersion = CompatibilityLevel.Version120;
                            if ((CompatibilityLevel)value >= CompatibilityLevel.Version100 && (CompatibilityLevel)value <= maxVersion)
                            {
                                compatIsValid = true;
                            }
                            break;
                        case 13://SQL15
                            if ((CompatibilityLevel)value >= CompatibilityLevel.Version100 && (CompatibilityLevel)value <= CompatibilityLevel.Version130)
                            {
                                compatIsValid = true;
                            }
                            break;
                        default:
                            // This is catching the beyond SQL15 case, but using default to be forward compatable for beyond SQL15
                            // Minimum supported compat level for SQL 15 is 100.
                            if ((this.ServerVersion.Major >= 14) && ((CompatibilityLevel)value >= CompatibilityLevel.Version100))
                            {
                                compatIsValid = true; // we're fine
                            }
                            break;
                    }
                }

                if (!compatIsValid)
                {
                    // if we make it here, we didn't pass the switch statement and need to throw
                    throw new UnsupportedVersionException(
                                ExceptionTemplates.InvalidPropertyValueForVersion(
                                this.GetType().Name,
                                "CompatibilityLevel",
                                value.ToString(),
                                GetSqlServerVersionName()));
                }
            }
        }


        /// <summary>
        /// This object supports permissions.
        /// </summary>
        internal override UserPermissionCollection Permissions
        {
            get
            {
                // call the base class
                return GetUserPermissions();
            }
        }

        bool bForAttach = false;

        /// <summary>
        /// Creates the database on the instance of SQL Server
        /// </summary>
        /// <param name="forAttach">When true, will create the database by attaching existing files. 
        /// There must be at least one FileGroup with a valid DataFile for the attach to succeed.</param>
        /// <example>
        /// var databaseAttach = new Database(server, dbName);
        /// databaseAttach.FileGroups.Add(new FileGroup(databaseAttach, "PRIMARY"));
        /// databaseAttach.FileGroups["PRIMARY"].Files.Add(new DataFile(databaseAttach.FileGroups["PRIMARY"], primaryName, primaryFileName));
        /// databaseAttach.Create(forAttach:true);
        /// </example>
        public void Create(bool forAttach)
        {
            bForAttach = forAttach;
            try
            {
                base.CreateImpl();
            }
            finally
            {
                bForAttach = false;
            }
        }

        /// <summary>
        /// Creates the database on the instance of SQL Server.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        ///<summary>
        ///Scripts object creation. Resulting script comes back in an array.
        ///</summary>
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ContainmentRelatedValidation(sp);

            StringBuilder sbStatement = new StringBuilder();
            string scriptName = this.FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sbStatement.Append(ExceptionTemplates.IncludeHeader(UrnSuffix, scriptName,
                    DateTime.Now.ToString(GetDbCulture())));
                sbStatement.Append(sp.NewLine);
            }

            var viewName = string.Empty;
            var databaseIsView = false;
            var emptyFileGroups = new StringCollection();
            var isAzureDb = Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType;
            var bSuppressDirtyCheck = sp.SuppressDirtyCheck;
            var targetEditionIsManagedServer = !isAzureDb && 
                (sp.TargetDatabaseEngineEdition == Cmn.DatabaseEngineEdition.SqlManagedInstance || 
                 sp.TargetDatabaseEngineEdition == Cmn.DatabaseEngineEdition.SqlAzureArcManagedInstance);

            if (IsSupportedProperty("DatabaseSnapshotBaseName"))
            {
                viewName = (string) GetPropValueOptional("DatabaseSnapshotBaseName", string.Empty);
                databaseIsView = (viewName.Length > 0);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                if ((int) SqlServerVersionInternal.Version90 <= (int) sp.TargetServerVersionInternal)
                {
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_DATABASE90, "NOT",
                        FormatFullNameForScripting(sp, false));
                }
                else
                {
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_DATABASE80, "NOT",
                        FormatFullNameForScripting(sp, false));
                }
                sbStatement.Append(sp.NewLine);
                sbStatement.Append(Scripts.BEGIN);
                sbStatement.Append(sp.NewLine);
            }
            if (isAzureDb)
            {
                this.ScriptCreateForCloud(sbStatement, sp, scriptName);
                if (sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
                {
                    createQuery.Add(sbStatement.ToString());
                    return;
                }
            }
            else
            {
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, "CREATE DATABASE {0}", scriptName);
            }

            if (this.IsSupportedProperty("ContainmentType", sp))
            {
                Property cType = this.GetPropertyOptional("ContainmentType");
                if (cType.Value != null)
                {
                    sbStatement.Append(sp.NewLine);
                    sbStatement.Append(" CONTAINMENT = ");
                    ContainmentType value = (ContainmentType) cType.Value;
                    switch (value)
                    {
                        case ContainmentType.None:
                            sbStatement.Append("NONE");
                            break;
                        case ContainmentType.Partial:
                            sbStatement.Append("PARTIAL");
                            break;
                        default:
                            throw new WrongPropertyValueException(Properties.Get("ContainmentType"));
                    }

                    sbStatement.Append(sp.NewLine);
                }
            }
            if (SmoUtility.IsSupportedObject<FileGroup>(this, sp) && !IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                //hit the server if collection is not initialized
                //Control will reach here only if TargetEngineType  is not cloud.
                if (sp.Storage.FileGroup && FileGroups.Count > 0 && !targetEditionIsManagedServer)
                {
                    StringBuilder filegroups = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    GetFileGroupsScript(filegroups, databaseIsView, emptyFileGroups, sp);
                    if (filegroups.Length > 0)
                    {
                        sbStatement.Append(" ON ");
                        sbStatement.Append(filegroups.ToString());
                    }
                }

                if (!databaseIsView)
                {
                    // Cannot specify a log file in a CREATE DATABASE statement without
                    // also specifying at least one non-log file.
                    if (sp.Storage.FileGroup && LogFiles.Count > 0 && !targetEditionIsManagedServer)
                    {
                        sbStatement.Append(Globals.newline);
                        sbStatement.Append(" LOG ON ");
                        GetLogFilesScript(sp, sbStatement);
                    }
                }
            }

            if (!databaseIsView)
            {
                // Used in scripting ledger option to see if we need to append a comma or add "WITH"  
                var hasCatalogCollation = false;

                // on 7.0 server we do not script collation , and ScriptCreateForCloud handles collation for azure
                if (!isAzureDb && sp.IncludeScripts.Collation && this.ServerVersion.Major > 7 &&
                    ((int) SqlServerVersionInternal.Version80 <= (int) sp.TargetServerVersionInternal))
                {
                    Property propCollation = State == SqlSmoState.Creating
                        ? Properties.Get("Collation")
                        : Properties["Collation"];

                    if (propCollation.Value != null && (bSuppressDirtyCheck || propCollation.Dirty))
                    {
                        CheckCollation((string) propCollation.Value, sp);
                        sbStatement.Append(Globals.newline);
                        sbStatement.Append(" COLLATE ");
                        sbStatement.Append((string) propCollation.Value);
                    }
                }

                if (true == bForAttach)
                {
                    sbStatement.Append(Globals.newline);
                    sbStatement.Append(" FOR ATTACH");
                }
                else if (!isAzureDb)
                {
                    if (IsSupportedProperty("CatalogCollation", sp) && !targetEditionIsManagedServer)
                    {
                        // Catalog Collation property is handled by ScriptCreateForCloud in Azure.  Do not append if this is a DB for attach.
                        //
                        Property catalogCollationType = this.GetPropertyOptional("CatalogCollation");

                        // Don't script the property if set to ContainedDatabaseFixedCollation this will be handled by DB Containment.
                        //
                        if (catalogCollationType != null &&
                            catalogCollationType.Value != null &&
                            (CatalogCollationType)catalogCollationType.Value != CatalogCollationType.ContainedDatabaseFixedCollation)
                        {
                            TypeConverter catalogCollationTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(CatalogCollationType));
                            string catalogCollationString = catalogCollationTypeConverter.ConvertToInvariantString(catalogCollationType.Value);
                            sbStatement.Append(Globals.newline);
                            sbStatement.Append(" WITH CATALOG_COLLATION = ");
                            sbStatement.Append(catalogCollationString);
                            hasCatalogCollation = true;
                        }
                    }

                    // Appending Ledger Property only if Ledger is supported and Value is set.
                    //
                    if (IsSupportedProperty(nameof(IsLedger), sp))
                    {
                        var isLedger = this.GetPropertyOptional(nameof(IsLedger));

                        // Only script the property if the value is set by the application
                        //
                        if (isLedger?.Value != null)
                        {
                            // Ledger property - Appending Ledger = ON to the Statement
                            //
                            sbStatement.Append(hasCatalogCollation ? ", " : $"{Globals.newline} WITH ");
                            sbStatement.AppendFormat("LEDGER = {0}", (bool)isLedger.Value ? "ON" : "OFF");
                        }
                    }
                }
            }
            else
            {
                if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.SupportedOnlyOn90).SetHelpContext(
                        "SupportedOnlyOn90");
                }

                sbStatement.AppendFormat(SmoApplication.DefaultCulture,
                    " AS SNAPSHOT OF [{0}]", SqlBraket(viewName));
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sbStatement.Append(sp.NewLine);
                sbStatement.Append(Scripts.END);
                sbStatement.Append(sp.NewLine);
            }

            createQuery.Add(sbStatement.ToString());

            // Script explicitly all file groups other than PRIMARY and MEMORY OPTIMIZED groups
            // for the managed instance.
            // It has to be scripted here with
            // instead of part of CREATE DB statement.
            // Managed instances do not support this construct.
            //
            FileGroup defaultFileGroupManagedInstance = null;

            if (targetEditionIsManagedServer)
            {
                FileGroup fgPrimary = m_FileGroups["PRIMARY"];

                foreach (FileGroup fg in FileGroups)
                {
                    // Don't script PRIMARY or MEMORY OPTIMIZED filegroups since these are already present.
                    //
                    if (fg != fgPrimary && fg.FileGroupType != FileGroupType.MemoryOptimizedDataFileGroup)
                    {
                        if (State == SqlSmoState.Existing)
                        {
                            fg.Initialize(true);
                        }

                        fg.ScriptCreate(createQuery, sp);

                        foreach(DataFile file in fg.Files)
                        {
                            file.ScriptCreate(createQuery, sp);
                        }

                        // Record the default filegroup, since we have to script it explicitly later
                        //
                        // ALTER DB MODIFY FILEGROUP [FG] DEFAULT
                        if (fg.IsDefault)
                        {
                            defaultFileGroupManagedInstance = fg;
                        }
                    }
                }
            }

            foreach (string stmt in emptyFileGroups)
            {
                createQuery.Add(stmt);
            }

            if(SmoUtility.IsSupportedObject<FileGroup>(this, sp) && !IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                if(this.ServerVersion.Major >= 13 &&
                    (int) SqlServerVersionInternal.Version130 <= (int) sp.TargetServerVersionInternal)
                {
                    GetAutoGrowFilesScript(createQuery, sp);
                }
            }

            if (!databaseIsView)
            {
                if (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version70)
                {
                    ScriptDbProps70Comp(createQuery, sp);
                }
                else
                {
                    ScriptDbProps80Comp(createQuery, sp, isAzureDb);
                }
            }

            // enable vardecimal compression as needed
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                bool forCreateScript = true;
                ScriptVardecimalCompression(createQuery, sp, forCreateScript);
            }

            // Script for Change tracking options on database
            if (IsSupportedProperty("ChangeTrackingEnabled", sp) && sp.Data.ChangeTracking)
            {
                ScriptChangeTracking(createQuery, sp);
            }

            if (IsSupportedProperty("RemoteDataArchiveEnabled", sp))
            {
                ScriptRemoteDataArchive(createQuery, sp);
            }

            if (sp.IncludeScripts.Owner)
            {
                this.ScriptChangeOwner(createQuery, sp);
            }

            if (IsSupportedProperty("EncryptionEnabled", sp))
            {
                if (!isAzureDb && (null != m_DatabaseEncryptionKey
                                   && (!IsDEKInitializedWithoutAnyPropertiesSet()
                                       || this.Properties.Get("EncryptionEnabled").Dirty))
                    //When EncryptionEnabled Property is set then it means User wants to
                    ) // do Encryption operations on the database, hence we should try to script
                {
                    // the DatabaseEncryptionKey in that case.
                    AddUseDb(createQuery, sp);
                    DatabaseEncryptionKey.ScriptCreateInternal(createQuery, sp);
                }

                // User should be allowed to set the EncryptionEnabled property irrespective of specifying the DEK properties
                Property property = Properties.Get("EncryptionEnabled");

                // We are scripting EncryptionEnabled property only when it's ON, as engine throws an error on scripting with OFF option during create time
                // Once the engine fixes this issue we can remove this check for "true" and script the option as it is.
                if (null != property.Value && (bool) property.Value == true)
                {
                    StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE {0}",
                        this.FormatFullNameForScripting(sp));
                    sb.AppendFormat(SmoApplication.DefaultCulture, " SET ENCRYPTION {0}",
                        ((bool) property.Value) ? "ON" : "OFF");
                    createQuery.Add(sb.ToString());
                }
            }
            this.ScriptCreateUsersIfRequired(createQuery, sp);

            // do not script SMO settings on create
            if (this.IsSupportedObject<QueryStoreOptions>(sp) && this.State != SqlSmoState.Creating)
            {
                this.QueryStoreOptions.ScriptCreate(createQuery, sp);
            }

            // DatabaseScopedConfiguration is not currently supported for SQL OD. Using a IsSupportedObject check as general way to prevent unsupported scripts
            if (this.IsSupportedObject<DatabaseScopedConfiguration>(sp))
            {
                // Script for the alter of database scoped configurations. On Azure, these scripts should be executed inside the target
                // database connection, so they are shown as comments for users.
                StringCollection alterDbScopedConfigurationsCollection = new StringCollection();
                this.ScriptDbScopedConfigurations(alterDbScopedConfigurationsCollection, sp);

                if (alterDbScopedConfigurationsCollection.Count != 0)
                {
                    if (Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType)
                    {
                        SmoUtility.EncodeStringCollectionAsComment(alterDbScopedConfigurationsCollection,
                            LocalizableResources.DatabaseScopedConfiguration_CreateScriptOnAzureDesc);
                    }

                    createQuery.AddCollection(alterDbScopedConfigurationsCollection);
                }
            }

            // Managed instances are creating file groups as a set of separate ALTER DATABASE statements
            // post CREATE DATABASE statement (creating everything at once is not supported). This last
            // piece of FILEGROUP work is to set the default filegroup, if it's not set to PRIMARY.
            //
            if (defaultFileGroupManagedInstance != null)
            {
                createQuery.AddCollection(GetDefaultFileGroupScript(sp, defaultFileGroupManagedInstance.Name));
            }
        }

        /// <summary>
        /// Scripts database scoped configurations for a create or alter statement if necessary.
        /// </summary>
        /// <param name="query">The StringCollection representing the current query.</param>
        /// <param name="sp">The scripting preferences for this script create/alter call.</param>
        private void ScriptDbScopedConfigurations(StringCollection query, ScriptingPreferences sp)
        {
            // Check if we support the DatabaseScopedConfigurations property in this database.
            if (SmoUtility.IsSupportedObject<DatabaseScopedConfiguration>(this, sp))
            {
                String[] earlySqlPropertyNames = new String[]
                {
                    "MAXDOP",
                    "LEGACY_CARDINALITY_ESTIMATION",
                    "PARAMETER_SNIFFING",
                    "QUERY_OPTIMIZER_HOTFIXES"
                };

                // For each database scoped configuration, scripts the alter operations if necessary.
                StringCollection alterStatementCollection = new StringCollection();
                for (int i = 0; i < this.DatabaseScopedConfigurations.Count; i++)
                {
                    DatabaseScopedConfiguration dbScopedConfiguration = this.DatabaseScopedConfigurations[i];

                    // when scripting an existing database we omit configs that have default value
                    var isValueDefault = this.State == SqlSmoState.Existing &&
                                         dbScopedConfiguration.State == SqlSmoState.Existing &&
                                         dbScopedConfiguration.IsSupportedProperty("IsValueDefault") &&
                                         dbScopedConfiguration.IsValueDefault;

                    Property property = dbScopedConfiguration.Properties.GetPropertyObject("Value");
                    if ((property.Dirty || (!sp.ScriptForAlter && !isValueDefault)) && dbScopedConfiguration.Value != string.Empty)
                    {
                        // Somehow, a value can end up with a null terminator. 
                        var idxnull = dbScopedConfiguration.Value.IndexOf('\0');
                        if (idxnull >= 0)
                        {
                            dbScopedConfiguration.Value = dbScopedConfiguration.Value.Substring(0, idxnull);
                        }

                        StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                        sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE SCOPED CONFIGURATION SET {0} = {1};", dbScopedConfiguration.Name.ToUpper(CultureInfo.InvariantCulture), dbScopedConfiguration.Value);
                        alterStatementCollection.Add(sb.ToString());
                    }

                    property = dbScopedConfiguration.Properties.GetPropertyObject("ValueForSecondary");


                    // Only the 4 listed configs support a "PRIMARY" option. When set to PRIMARY, resetting the value of these configs on the primary
                    // automatically resets their value on the secondaries, so we only script them out explicitly during if they have
                    // a value other than empty or "PRIMARY"
                    // We treat empty values as "PRIMARY"
                    var secondaryValue = string.IsNullOrEmpty(dbScopedConfiguration.ValueForSecondary)
                        ? "PRIMARY"
                        : dbScopedConfiguration.ValueForSecondary;
                    bool shouldScriptForSecondary = (earlySqlPropertyNames.Contains(dbScopedConfiguration.Name.ToUpper())
                        && !secondaryValue.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase));
                    // property.Dirty can be true yet the value still not be meaningful
                    if ((!string.IsNullOrEmpty(dbScopedConfiguration.ValueForSecondary) && property.Dirty) || (!sp.ScriptForAlter && shouldScriptForSecondary))
                    {
                        StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                        sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE SCOPED CONFIGURATION FOR SECONDARY SET {0} = {1};", dbScopedConfiguration.Name.ToUpper(CultureInfo.InvariantCulture), dbScopedConfiguration.ValueForSecondary);
                        alterStatementCollection.Add(sb.ToString());
                    }
                }

                if (alterStatementCollection.Count != 0)
                {
                    this.AddUseDb(query, sp);
                    query.AddCollection(alterStatementCollection);
                }
            }
        }

        /// <summary>
        ///   if we script permissions then we need to add users
        /// </summary>
        /// <param name="createQuery"></param>
        /// <param name="sp"></param>
        private void ScriptCreateUsersIfRequired(StringCollection createQuery, ScriptingPreferences sp)
        {
            //  if we script permissions then we need to add users
            if (sp.IncludeScripts.Permissions)
            {
                StringCollection users = new StringCollection();
                foreach (User u in this.Users)
                {
                    // ignore system users and users not mapped to a login
                    if (!u.IsSystemObject && u.Login.Length > 0)
                    {
                        u.ScriptCreateInternal(users, sp);
                    }
                }

                if (users.Count > 0)
                {
                    AddUseDb(createQuery, sp);
                    foreach (string s in users)
                    {
                        createQuery.Add(s);
                    }
                }

            }
        }

        ///<summary>
        ///Scripts object creation for Cloud DB.
        ///</summary>
        /// <param name="sbStatement"></param>
        /// <param name="sp"></param>
        /// <param name="scriptName">Name of the DB</param>
        private void ScriptCreateForCloud(StringBuilder sbStatement, ScriptingPreferences sp, string scriptName)
        {
            //note: conditional scripting (so.IncludeIfNotExists) is not supported for
            //  the gateway intercepted statements like Database/Login DDLs -sivasat
            if (this.IsSupportedProperty("IsSqlDw", sp))
            {
                Property isSqlDw = this.GetPropertyOptional("IsSqlDw");

                // for SQL DW database, EDITION and SERVICE_OBJECTIVES are required properties
                // the MAXSIZE is an optional property
                /*
                 * CREATE DATABASE database_name [ COLLATE collation_name ]
                 * (
                 *      [ MAXSIZE = { 250 | 500 | 750 | 1024 | 5120 | 10240 | 20480 | 30720 | 40960 | 51200 } GB ,]
                 *      EDITION = 'DataWarehouse',
                 *      SERVICE_OBJECTIVE = { 'DW100' | 'DW200' | 'DW300' | 'DW400' | 'DW500' | 'DW600' | 'DW1000' | 'DW1200' | 'DW1500' | 'DW2000' }
                 * )
                 * [;]
                 */
                // note we can be called with a source database that isn't an azure SQL DB. SMO defines IsSqlDW for pre-cloud versions of Database, but not these other properties.
                // If they aren't provided we'll just let Azure create whatever the current default is.
                var expectAzureProperties = (this.State == SqlSmoState.Creating) ||
                                            this.Properties.Contains("AzureEdition");
                var maxSizeInBytes = expectAzureProperties ? this.GetPropertyOptional("MaxSizeInBytes").Value : null;
                var isMaxSizeApplicable = expectAzureProperties ? this.GetPropertyOptional("IsMaxSizeApplicable").Value : null;
                var azureEdition = expectAzureProperties ? this.GetPropertyOptional("AzureEdition").Value : null;
                var slo = expectAzureProperties ? this.GetPropertyOptional("AzureServiceObjective").Value : null;
                var isLedger = IsSupportedProperty(nameof(IsLedger), sp) ? this.GetPropertyOptional(nameof(IsLedger)) : null;
                var collation = this.GetPropertyOptional("Collation");
                var catalogCollationType = IsSupportedProperty("CatalogCollation", sp) ? this.GetPropertyOptional("CatalogCollation") :  null;
                if (isSqlDw.Value != null && (bool)isSqlDw.Value)
                {
                    if (azureEdition == null || slo == null)
                    {
                        throw new ArgumentException(ExceptionTemplatesImpl.SqlDwCreateRequiredParameterMissing);
                    }
                }

                var baseStatement = string.Format(SmoApplication.DefaultCulture, "CREATE DATABASE {0} ", scriptName);

                if (sp.IncludeScripts.Collation && collation.Value != null)
                {
                    baseStatement += string.Format("COLLATE {0} ", collation.Value);
                }

                var statementBuilder = new ScriptStringBuilder(baseStatement);

                // Bugfix: 9289937 Don't script out incompatible edition parameters if the edition types don't match
                // SLO/maxsize etc don't cross-script between DW and regular Azure SQL DB
                if (sp.TargetEngineIsAzureSqlDw())
                {
                    statementBuilder.SetParameter("EDITION", "DataWarehouse");
                }
                else if (azureEdition != null && this.DatabaseEngineEdition == sp.TargetDatabaseEngineEdition)
                {
                    statementBuilder.SetParameter("EDITION", azureEdition.ToString());
                }

                if (this.DatabaseEngineEdition == sp.TargetDatabaseEngineEdition && sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlOnDemand)
                {
                    if (slo != null)
                    {
                        statementBuilder.SetParameter("SERVICE_OBJECTIVE", slo.ToString());
                    }
                    if ((isMaxSizeApplicable != null && bool.Parse(isMaxSizeApplicable.ToString())) || (maxSizeInBytes != null && (double)maxSizeInBytes > 0))
                    {
                        statementBuilder.SetParameter("MAXSIZE", GetMaxSizeString((double) maxSizeInBytes), ParameterValueFormat.NotString);
                    }
                }
                else if (sp.TargetEngineIsAzureSqlDw())
                {
                    statementBuilder.SetParameter("SERVICE_OBJECTIVE", "DW100c");
                }

                sbStatement.Append(statementBuilder.ToString(scriptSemiColon: false));

                bool hasCatalogCollation = catalogCollationType != null && catalogCollationType.Value != null;

                if (hasCatalogCollation)
                {
                    TypeConverter catalogCollationTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(CatalogCollationType));
                    string catalogCollationString = catalogCollationTypeConverter.ConvertToInvariantString(catalogCollationType.Value);
                    sbStatement.AppendFormat(" WITH CATALOG_COLLATION = {0}", catalogCollationString);
                }

                // Ledger Changes appending Ledger = ON / OFF
                // Scripting only if the value is Set.
                if (isLedger != null && isLedger.Value != null)
                {
                    sbStatement.Append(hasCatalogCollation ? ", " : " WITH ");
                    sbStatement.AppendFormat("LEDGER = {0}", (bool)isLedger.Value ? "ON" : "OFF");
                }

                sbStatement.AppendFormat(";{0}", sp.NewLine);
            }
            else
            {
                // for Azure SAWA V1, when EDITION and SERVICE_OBJECTIVE are not specified, the default values are used:
                // EDTION = 'Standard'
                // SERVICE_OBJECTIVE = 'S0'
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, "CREATE DATABASE {0} ", scriptName);
            }

        }

        private static string GetMaxSizeString(double maxSizeInBytes)
        {
            string maxSizeString;
            var maxSize = (double)maxSizeInBytes;
            if (maxSize < 1024.0 * 1024.0 * 1024.0)
            {
                maxSizeString = string.Format(SmoApplication.DefaultCulture, "{0} MB", maxSize / 1024.0 / 1024.0);
            }
            else
            {
                maxSizeString = string.Format(SmoApplication.DefaultCulture, "{0} GB",
                    maxSize / 1024.0 / 1024.0 / 1024.0);
            }
            return maxSizeString;
        }

        /// <summary>
        /// Script alter for cloud-specific properties
        /// </summary>
        /// <param name="sbStatement"></param>
        /// <param name="sp"></param>
        private void ScriptAlterForCloud(StringCollection sbStatement, ScriptingPreferences sp)
        {
            var azureEditionProperty = Properties.Get("AzureEdition");
            var maxSizeProperty = Properties.Get("MaxSizeInBytes");
            var sloProperty = Properties.Get("AzureServiceObjective");
            var temporalRetentionProperty =
                IsSupportedProperty("TemporalHistoryRetentionEnabled") &&
                (sp == null || IsSupportedProperty("TemporalHistoryRetentionEnabled", sp))
                    ? Properties.Get("TemporalHistoryRetentionEnabled")
                    : null;
            var cloudProperties =
                new[] {azureEditionProperty, maxSizeProperty, sloProperty}.Where(
                    prop => prop.Value != null && (prop.Dirty || !sp.ForDirectExecution));
            if (cloudProperties.Any())
            {
                var statementBuilder =
                    new ScriptStringBuilder(string.Format(SmoApplication.DefaultCulture, @"ALTER DATABASE {0} MODIFY",
                        this.FormatFullNameForScripting(sp)));
                foreach (var property in cloudProperties)
                {
                    switch (property.Name.ToLowerInvariant())
                    {
                        case "azureedition":
                            statementBuilder.SetParameter("EDITION", property.Value.ToString());
                            break;
                        case "azureserviceobjective":
                            statementBuilder.SetParameter("SERVICE_OBJECTIVE", property.Value.ToString());
                            break;
                        case "maxsizeinbytes":
                            statementBuilder.SetParameter("MAXSIZE", GetMaxSizeString((double) property.Value), ParameterValueFormat.NotString);
                            break;
                    }
                }
                sbStatement.Add(statementBuilder.ToString());
            }

            if (temporalRetentionProperty != null && temporalRetentionProperty.Value != null && (temporalRetentionProperty.Dirty || !sp.ForDirectExecution))
            {
                string strTemporal = string.Format(SmoApplication.DefaultCulture, @"ALTER DATABASE {0} SET TEMPORAL_HISTORY_RETENTION {1}",
                       this.FormatFullNameForScripting(sp),
                       (bool)temporalRetentionProperty.Value ? "ON" : "OFF");

                 sbStatement.Add(strTemporal);
            }
        }

        private void ScriptDbProps70Comp(StringCollection query, ScriptingPreferences sp)
        {
            bool bSuppressDirtyCheck = sp.SuppressDirtyCheck;

            string dbName = this.FormatFullNameForScripting(sp, false);

            if (IsSupportedProperty("IsFullTextEnabled", sp))
            {
                Property propFullTextEnabled = Properties.Get("IsFullTextEnabled");
                if (null != propFullTextEnabled.Value &&
                     (propFullTextEnabled.Dirty || !sp.ScriptForAlter))
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture,
                        "IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')){0}begin{0}EXEC {1}.[dbo].[sp_fulltext_database] @action = '{2}'{0}end",
                        Globals.newline, FormatFullNameForScripting(sp), (bool)propFullTextEnabled.Value ? "enable" : "disable"));
                }
            }

            Property propCompat = Properties.Get("CompatibilityLevel");
            if (null != propCompat.Value && (propCompat.Dirty || !sp.ScriptForAlter))
            {
                //script only if compatibility level is less than the target server
                if ((int)(CompatibilityLevel)propCompat.Value <= 70)
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture, "EXEC sp_dbcmptlevel @dbname={0}, @new_cmptlevel={1}",
                                             dbName, Enum.Format(typeof(CompatibilityLevel), (CompatibilityLevel)propCompat.Value, "d")));
                }
            }

        }

        private void ScriptDbProps80Comp(StringCollection query, ScriptingPreferences sp, bool isAzureDb)
        {
            AddCompatibilityLevel(query, sp);
            if (sp.ScriptForAlter && !isAzureDb) // azure db collation is fixed after creation
            {
                Property propCollate = Properties.Get("Collation");
                if (null != propCollate.Value && (propCollate.Dirty || sp.ScriptForCreateDrop))
                {
                    CheckCollation((string)propCollate.Value, sp);
                    // we use this funky sql in order to be safe from code injection
                    // and we do not want to make another roundtrip to check for collation
                    query.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} COLLATE {1}",
                                             FormatFullNameForScripting(sp), (string)propCollate.Value));
                }
            }

            if (!isAzureDb && IsSupportedProperty("IsFullTextEnabled", sp))
            {
                Property propFullTextEnabled = Properties.Get("IsFullTextEnabled");
                // The last line of the condition is to only avoid emitting the statement for scripts/create when version80
                if (null != propFullTextEnabled.Value &&
                     (propFullTextEnabled.Dirty || !sp.ScriptForAlter) &&
                     (sp.TargetServerVersion >= SqlServerVersion.Version90 || sp.ScriptForAlter || (bool)propFullTextEnabled.Value))
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture,
                        "IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')){0}begin{0}EXEC {1}.[dbo].[sp_fulltext_database] @action = '{2}'{0}end",
                        Globals.newline, FormatFullNameForScripting(sp), (bool)propFullTextEnabled.Value ? "enable" : "disable"));
                }
            }

            //Scripting for DatabaseOptions related properties

            ScriptDbOptionsProps(query, sp, isAzureDb);
        }

        private void AddCompatibilityLevel(StringCollection query, ScriptingPreferences sp)
        {
            Property propCompat = Properties.Get("CompatibilityLevel");
            if (null != propCompat.Value && (propCompat.Dirty || !sp.ScriptForAlter))
            {
                bool isTargetSqlAzureOrMIOrMIAA = (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase) ||
                    (sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance) ||
                    (sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlAzureArcManagedInstance);

                bool isVersion160WithCompatLevelLessThan160 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version160) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 160);

                bool isVersion150WithCompatLevelLessThan150 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version150) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 150);

                bool isVersion140WithCompatLevelLessThan140 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version140) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 140);

                bool isVersion130WithCompatLevelLessThan130 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version130) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 130);

                bool isVersion120WithCompatLevelLessThan120 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version120) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 120);

                bool isVersion110WithCompatLevelLessThan110 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version110) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 110);

                bool isVersion105WithCompatLevelLessThan105 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version105) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 105);

                bool isVersion100WithCompatLevelLessThan100 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version100) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 100);

                bool isVersion90WithCompatLevelLessThan90 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version90) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 90);

                bool isVersion80WithCompatLevelLessThan80 =
                    (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version80) &&
                    ((int)(CompatibilityLevel)propCompat.Value <= 80);

                bool isVersion80Or90WithLowerCompatLevel = isVersion90WithCompatLevelLessThan90 || isVersion80WithCompatLevelLessThan80;

                bool isVersionWithLowerCompatLevel =
                    isVersion105WithCompatLevelLessThan105 ||
                    isVersion100WithCompatLevelLessThan100 ||
                    isVersion110WithCompatLevelLessThan110 ||
                    isVersion120WithCompatLevelLessThan120 ||
                    isVersion130WithCompatLevelLessThan130 ||
                    isVersion140WithCompatLevelLessThan140 ||
                    isVersion150WithCompatLevelLessThan150 ||
                    isVersion160WithCompatLevelLessThan160 ||
                    isTargetSqlAzureOrMIOrMIAA;

                //script only if compatibility level is less than the target server
                // on Alter() we just script it and let the server fail if it is not correct
                if (IsSupportedProperty("CompatibilityLevel", sp) && (sp.ScriptForAlter || isVersionWithLowerCompatLevel || isVersion80Or90WithLowerCompatLevel))
                {
                    CompatibilityLevel upgradedCompatLevel = UpgradeCompatibilityValueIfRequired(sp, (CompatibilityLevel)propCompat.Value);
                    if (isVersionWithLowerCompatLevel)
                    {
                        query.Add(
                            string.Format(
                            SmoApplication.DefaultCulture,
                            "ALTER DATABASE {0} SET COMPATIBILITY_LEVEL = {1}",
                            FormatFullNameForScripting(sp),
                            Enum.Format(typeof(CompatibilityLevel), upgradedCompatLevel, "d")));
                    }
                    else if (isVersion80Or90WithLowerCompatLevel)
                    {
                        query.Add(
                            string.Format(
                            SmoApplication.DefaultCulture,
                            "EXEC dbo.sp_dbcmptlevel @dbname={0}, @new_cmptlevel={1}",
                            this.FormatFullNameForScripting(sp, false),
                            Enum.Format(typeof(CompatibilityLevel), upgradedCompatLevel, "d")));
                    }
                }
            }
        }

        private CompatibilityLevel UpgradeCompatibilityValueIfRequired(ScriptingPreferences sp, CompatibilityLevel compatibilityLevel)
        {
            // Return Compatibility level 90 if it is less when scripting for server version 110.
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version110 && compatibilityLevel <= CompatibilityLevel.Version80)
            {
                return CompatibilityLevel.Version90;
            }

            //Return Compatibility level 80 if it is less when scripting for Server version 100/105
            if (((sp.TargetServerVersionInternal == SqlServerVersionInternal.Version105) ||
                (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version100)) &&
                (compatibilityLevel <= CompatibilityLevel.Version70))
            {
                return CompatibilityLevel.Version80;
            }

            //Return Compatibility level 70 if it is less when Scripting for Server version 90
            if (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version90 && compatibilityLevel <= CompatibilityLevel.Version65)
            {
                return CompatibilityLevel.Version70;
            }
            return compatibilityLevel;
        }


        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            bool bWithScript = action != PropagateAction.Create;
            PropagateInfo[] pi;

            if (DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                var piList = new List<PropagateInfo>();
                if (this.IsSupportedObject<ExtendedProperty>())
                {
                    piList.Add(new PropagateInfo(m_ExtendedProperties, true, ExtendedProperty.UrnSuffix));
                }
                if (this.IsSupportedObject<DatabaseDdlTrigger>())
                {
                    piList.Add(new PropagateInfo(databaseDdlTriggerCollection, bWithScript, bWithScript));
                }
                if (this.IsSupportedObject<FullTextCatalog>())
                {
                    piList.Add(new PropagateInfo(m_FullTextCatalogs, bWithScript, bWithScript));
                }
                pi = piList.ToArray();
            }
            
            // bWithScript flag is only set to true for Database.Alter()
            // therefore it's sufficient to check for the current server
            // version to be Yukon or higher. Otherwise FullTextCatalog object
            // cannot be altered
            else if (this.ServerVersion.Major >= 9)
            {
                //for mirroring -> propagate only if exists
                if (action == PropagateAction.Alter)
                {
                    pi = new PropagateInfo[] {
                            new PropagateInfo(m_ExtendedProperties, true, ExtendedProperty.UrnSuffix ),
                            new PropagateInfo(databaseDdlTriggerCollection, bWithScript, bWithScript),
                            new PropagateInfo(m_FileGroups, bWithScript, bWithScript),
                            new PropagateInfo(m_LogFiles, bWithScript, bWithScript),
                            new PropagateInfo(m_FullTextCatalogs, bWithScript, bWithScript),
                            //Propagating the DEK object is taken care in the ScriptAlter method
                            new PropagateInfo((ServerVersion.Major < 10)? (FullTextStopListCollection)null : m_FullTextStopLists, bWithScript, bWithScript)
                        };
                }
                else
                {
                    //fisrt parameter controls if the next level is scripted
                    //second parameter controls if the levels after the first level is scripted
                    bool prop = false;
                    if (IsSupportedProperty("IsDatabaseSnapshot"))
                    {
                        prop = (bool)GetPropValueOptional("IsDatabaseSnapshot", false);
                    }
                    pi = new PropagateInfo[] {
                            //ExtendedProperties do not need to be propagated for Snapshot while Creation
                            new PropagateInfo(ExtendedProperties, !prop, ExtendedProperty.UrnSuffix ),
                            new PropagateInfo(FileGroups, bWithScript, bWithScript),
                            new PropagateInfo(LogFiles, bWithScript, bWithScript),
                            new PropagateInfo(m_FullTextCatalogs, bWithScript, bWithScript),
                            //Propagating the DEK object is taken care in the ScriptCreate method
                            new PropagateInfo((ServerVersion.Major < 10)? null : m_FullTextStopLists, bWithScript, bWithScript)

                            //Propagating the DEK object is taken care in the ScriptCreate method
                        };
                }
            }
            else if (this.ServerVersion.Major >= 8)
            {
                //fisrt parameter controls if the next level is scripted
                //second parameter controls if the levels after the first level is scripted
                pi = new PropagateInfo[] {
                    new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix ),
                    new PropagateInfo(FileGroups, bWithScript, bWithScript),
                    new PropagateInfo(LogFiles, bWithScript, bWithScript)
                };
            }
            else
            {
                //fisrt parameter controls if the next level is scripted
                //second parameter controls if the levels after the first level is scripted
                pi = new PropagateInfo[] {
                    new PropagateInfo(FileGroups, bWithScript, bWithScript),
                    new PropagateInfo(LogFiles, bWithScript, bWithScript)
                };
            }

            return pi;
        }

        /// <summary>
        /// Overrides the standard behavior of scripting object permissions.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // script database permissions.
            AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Database, sp);
        }

        public void SetOffline()
        {
            SetOfflineImpl(true);
        }

        public void SetOnline()
        {
            SetOfflineImpl(false);
        }

        /// <summary>
        /// Enables or disables snapshot isolation for the current database
        /// </summary>
        /// <param name="enabled"></param>
        public void SetSnapshotIsolation(bool enabled)
        {
            try
            {
                CheckObjectState();
                if (State == SqlSmoState.Creating)
                {
                    throw new InvalidSmoOperationException("SetSnapshotIsolation", State);
                }

                if (ServerVersion.Major < 9)
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }

                this.ExecutionManager.ExecuteNonQuery(
                    string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET ALLOW_SNAPSHOT_ISOLATION {1}",
                    SqlBraket(this.Name), enabled ? "ON" : "OFF"));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetSnapshotIsolation, this, e);
            }
        }

        private void SetOfflineImpl(bool offline)
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(Scripts.USEMASTER);
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET  {1}",
                                           SqlBraket(this.Name), offline ? "OFFLINE" : "ONLINE"));

                this.ExecutionManager.ExecuteNonQuery(queries);

                if (!this.ExecutionManager.Recording)
                {
                    if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                    {
                        SmoApplication.eventsSingleton.CallDatabaseEvent(this.Parent,
                            new DatabaseEventArgs(this.Urn, this, Name,
                                offline ? DatabaseEventType.Offline : DatabaseEventType.Online));
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetOffline, this, e);
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Adds the script to change the owner of a DB for SQL 2005 (Shiloh). That doesn't
        /// support ALTER AUTHORIZATION so sp_changedbowner is used.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the query to</param>
        /// <param name="sp">Options (used for formatting the name)</param>
        /// <param name="newOwner">The name of the login</param>
        /// <returns></returns>
        internal override void ScriptOwnerForShiloh(StringBuilder sb, ScriptingPreferences sp, string newOwner)
        {
            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC {0}.dbo.sp_changedbowner @loginame = {1}, @map = false", FormatFullNameForScripting(sp), MakeSqlString(newOwner));
        }

        /// <summary>
        /// Sets the owner of this database to the login with the specified name. Does not drop any existing user
        /// accounts mapped to the login before attempting to change the owner.
        /// </summary>
        /// <param name="loginName">The name of the login to be the new owner</param>
        /// <remarks>If dropExistingUser is not set to true then an existing non-dbo user account mapped to the specified
        /// login will cause this to fail.</remarks>
        public void SetOwner(string loginName)
        {
            CheckObjectState(true);
            SetOwnerImpl(loginName, false);
        }

        /// <summary>
        /// Sets the owner of this database to the login with the specified name.
        /// </summary>
        /// <param name="loginName">The name of the login to be the new owner</param>
        /// <param name="dropExistingUser">Whether to drop any existing Users mapped to the specified login for this DB before
        /// changing the owner.</param>
        /// <remarks>If dropExistingUser is not set to true then an existing non-dbo user account mapped to the specified
        /// login will cause this to fail.</remarks>
        public void SetOwner(string loginName, bool dropExistingUser)
        {
            CheckObjectState(true);
            SetOwnerImpl(loginName, dropExistingUser);
        }

        /// <summary>
        /// Implementation to change the owner of this database to the login with the specified name.
        /// </summary>
        /// <param name="loginName">The name of the login to be the new owner</param>
        /// <param name="dropExistingUser">Whether to drop existing non-dbo users before changing the owner</param>
        private void SetOwnerImpl(string loginName, bool dropExistingUser)
        {
            try
            {
                if (null == loginName)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("loginName"));
                }

                // Owner is a read-only property.
                // To stay true to the design principle in design mode that dictates that we keep the same access
                // pattern to objects, we can only set the owner by calling the setowner method.
                // The non-design mode code actually doesn't set the owner property, it only propagates the new
                // owner value to the server
                if (this.IsDesignMode)
                {
                    Property owner = this.Properties.Get("Owner");
                    owner.SetValue(loginName);
                    owner.SetRetrieved(true);
                }
                else
                {

                    var query = new StringCollection();

                    InsertUseDb(index: 0, col:query, targetEngineType: this.DatabaseEngineType);

                    //Check to see if login is already mapped to a non-dbo user in this database.
                    //If it is and the caller has specified to drop any existing users then
                    //we add a query to do so. If the user hasn't specified to drop
                    //existing users then we skip this completely - though the query to change
                    //the owner is likely to fail since you can't have a login mapped to more
                    //one user in a db.
                    if (dropExistingUser)
                    {
                        Login l = ((Server)ParentColl.ParentInstance).Logins[loginName];
                        if (null == l)
                        {
                            throw new SmoException(ExceptionTemplates.InnerException,
                                                   new ArgumentException(ExceptionTemplates.InvalidLogin(loginName)));
                        }

                        //Find the user that maps to our target login - if one exists. Script
                        //the drop for that user if it's anything but dbo (since that can't be
                        //dropped and means the login is already the owner)
                        User user = this.Users.Cast<User>().FirstOrDefault(u => u.Login.Equals(loginName));
                        if (user != null && !user.Name.Equals("dbo", StringComparison.Ordinal))
                        {
                            user.ScriptDrop(query, this.GetScriptingPreferencesForCreate());
                        }
                    }

                    //Script the query to actually change the owner
                    this.ScriptChangeOwner(query, loginName);

                    this.ExecutionManager.ExecuteNonQuery(query);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetOwner, this, e);
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        private bool isDefaultLanguageModified = false;
        private bool isDefaultFulltextLanguageModified = false;

        protected override void CleanObject()
        {
            base.CleanObject();

            //After Alter() or Create() we can't guarantee that these values are
            //correct or not. Hence we need to retrieve these from server again.
            //But by using SetRetrieved(false) method, we are delaying these properties
            //retrieval to when the user uses these properties next.
            if (this.isDefaultLanguageModified)
            {
                Property defaultLanguageName = this.Properties.Get("DefaultLanguageName");
                Property defaultLanguageLcid = this.Properties.Get("DefaultLanguageLcid");

                defaultLanguageName.SetRetrieved(false);
                defaultLanguageLcid.SetRetrieved(false);
            }

            if (this.isDefaultFulltextLanguageModified)
            {
                Property defaultFullTextLanguageName = this.Properties.Get("DefaultFullTextLanguageName");
                Property defaultFullTextLanguageLcid = this.Properties.Get("DefaultFullTextLanguageLcid");

                defaultFullTextLanguageName.SetRetrieved(false);
                defaultFullTextLanguageLcid.SetRetrieved(false);
            }

            if (this.isDefaultLanguageModified || this.isDefaultFulltextLanguageModified)
            {
                //If PropertyBagState is Lazy, that means it still has un-retrieved properties.
                this.propertyBagState = PropertyBagState.Lazy;
            }
            //resetting to original
            this.isDefaultLanguageModified = false;
            this.isDefaultFulltextLanguageModified = false;
        }

        public void Alter(TerminationClause terminationClause)
        {
            try
            {
                optionTerminationStatement = new OptionTerminationStatement(terminationClause);
                base.AlterImpl();
            }
            finally
            {
                optionTerminationStatement = null;
            }
        }

        // emits ROLLBACK AFTER integer [SECONDS]
        public void Alter(TimeSpan transactionTerminationTime)
        {
            if (transactionTerminationTime.Seconds < 0)
            {
                throw new FailedOperationException(ExceptionTemplates.Alter, this, null,
                                                    ExceptionTemplates.TimeoutMustBePositive);
            }
            try
            {
                optionTerminationStatement = new OptionTerminationStatement(transactionTerminationTime);
                base.AlterImpl();
            }
            finally
            {
                optionTerminationStatement = null;
            }
        }

        private void ScriptAutoCreateStatistics(StringCollection queries, ScriptingPreferences sp)
        {
            if (!IsSupportedProperty("AutoCreateStatisticsEnabled", sp))
            {
                return;
            }
            Property propAutoCreateStatistics = this.Properties.Get("AutoCreateStatisticsEnabled");
            Property propAutoCreateStatisticsIncremental = null;
            if (IsSupportedProperty("AutoCreateIncrementalStatisticsEnabled"))
            {
                propAutoCreateStatisticsIncremental = this.Properties.Get("AutoCreateIncrementalStatisticsEnabled");
            }

            StringBuilder sbStatement = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // Generate the Autocreate block if the associated property exists and is dirty.
            // We may need to generate it if it is clean, but we will take care of this later.
            bool generateScript = !propAutoCreateStatistics.IsNull && propAutoCreateStatistics.Dirty;
            if (generateScript)
            {
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, "{0}", (bool)propAutoCreateStatistics.Value ? Globals.On : Globals.Off);
            }

            // Only add the alter command if Autocreate is true. Setting autocreate to false will automatically
            // set incremental to false.
            // We don't want to enable incremental statistics yet for cloud.
            if (!propAutoCreateStatistics.IsNull
                && (bool)propAutoCreateStatistics.Value
                && propAutoCreateStatisticsIncremental != null // We set this to null if Incremental is not supported (backwards compat).
                && !propAutoCreateStatisticsIncremental.IsNull
                && propAutoCreateStatisticsIncremental.Dirty
                && !IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                // If we didn't already generate the parent fragment before, do it now.
                if (!generateScript )
                {
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, "{0}", Globals.On);
                }
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, "INCREMENTAL = {0}", (bool)propAutoCreateStatisticsIncremental.Value ? Globals.On : Globals.Off);
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
            }

            if (sbStatement.Length > 0)
            {
                // ALTER DATABASE statement is added only if any of the properties is changed
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET AUTO_CREATE_STATISTICS " + sbStatement.ToString(), this.FormatFullNameForScripting(sp)));
            }
        }

        private void ScriptChangeTracking(StringCollection queries, ScriptingPreferences sp)
        {            

            Property propChangeTracking = this.Properties.Get("ChangeTrackingEnabled");
            Property retentionPeriod = this.Properties.Get("ChangeTrackingRetentionPeriod");
            Property retentionPeriodUnits = this.Properties.Get("ChangeTrackingRetentionPeriodUnits");
            Property isAutoCleanUp = this.Properties.Get("ChangeTrackingAutoCleanUp");


            bool changeTracking = false;
            StringBuilder sbStatement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (!propChangeTracking.IsNull)
            {
                changeTracking = (bool)propChangeTracking.Value;
                //While creating or scripting, script change tracking only when it is enabled. For details see VSTS bug 282883.
                if ((propChangeTracking.Dirty && sp.ScriptForAlter) || (changeTracking && !sp.ScriptForAlter))
                {
                    // this will be appended at the end before adding to a query
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, "= {0} ", (bool)changeTracking ? Globals.On : Globals.Off);
                }
            }

            // retentionPeriodChanged and retentionPeriodUnitsChanged are used to indicate whether those properties need to be scripted or changed
            bool retentionPeriodChanged = false;
            bool retentionPeriodUnitsChanged = false;

            // used to indicate that change retention properties are set
            bool parenthesis = false;

            if (!retentionPeriod.IsNull && (retentionPeriod.Dirty || !sp.ScriptForAlter))
            {
                if (sp.ForDirectExecution || (int)retentionPeriod.Value != 0)
                {
                    retentionPeriodChanged = true;
                }
            }

            if (!retentionPeriodUnits.IsNull && (retentionPeriodUnits.Dirty || !sp.ScriptForAlter))
            {
                if ((RetentionPeriodUnits)retentionPeriodUnits.Value != RetentionPeriodUnits.None)
                {
                    retentionPeriodUnitsChanged = true;
                }
            }

            if (retentionPeriodChanged && retentionPeriodUnitsChanged)
            {
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                sbStatement.AppendFormat(SmoApplication.DefaultCulture, "CHANGE_RETENTION = {0} {1}", (int)retentionPeriod.Value, retentionPeriodUnits.Value.ToString().ToUpperInvariant());
                parenthesis = true;
            }
            else if (retentionPeriodChanged || retentionPeriodUnitsChanged)
            {
                // Throws an error when either of the property is set and the other is not set
                throw new WrongPropertyValueException(ExceptionTemplates.MissingChangeTrackingParameters);
            }

            if (!isAutoCleanUp.IsNull && (isAutoCleanUp.Dirty || !sp.ScriptForAlter))
            {
                bool autoCleanUp = (bool)isAutoCleanUp.Value;
                if (autoCleanUp || changeTracking)
                {
                    if (parenthesis)
                    {
                        sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
                    }
                    else
                    {
                        sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                        parenthesis = true;
                    }

                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, "AUTO_CLEANUP = {0}", autoCleanUp ? "ON" : "OFF");
                }
            }

            if (parenthesis)
            {
                if (!changeTracking)
                {
                    // Cannot set the change tracking properties when the change tracking on database is off
                    throw new WrongPropertyValueException(ExceptionTemplates.ChangeTrackingException);
                }

                sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
            }

            if (sbStatement.Length > 0)
            {
                // ALTER DATABASE statement is added only if any of the properties is changed
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET CHANGE_TRACKING " + sbStatement.ToString(), this.FormatFullNameForScripting(sp)));
            }
        }


        void ScriptMirroringOptions(StringCollection queries, ScriptingPreferences sp)
        {
            if (IsSupportedProperty("MirroringPartner", sp))
            {
                Property prop = this.Properties.Get("MirroringPartner");
                if (prop.Dirty)
                {
                    string mirroringPartner = (string)prop.Value;
                    if (null != mirroringPartner && mirroringPartner.Length > 0)
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET PARTNER = {1}",
                                                   this.FormatFullNameForScripting(sp), SqlSmoObject.MakeSqlString(mirroringPartner)));
                    }
                }

                prop = this.Properties.Get("MirroringWitness");
                if (prop.Dirty)
                {
                    string mirroringWitness = (string)prop.Value;
                    if (null != mirroringWitness && mirroringWitness.Length > 0)
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET WITNESS = {1}",
                                                   this.FormatFullNameForScripting(sp), SqlSmoObject.MakeSqlString(mirroringWitness)));
                    }
                }

                prop = this.Properties.Get("MirroringSafetyLevel");
                if (prop.Dirty)
                {
                    object o = prop.Value;
                    if (null != o)
                    {
                        MirroringSafetyLevel mirroringSafetyLevel = (MirroringSafetyLevel)o;
                        string sMirroringSafetyLevel = null;

                        switch (mirroringSafetyLevel)
                        {
                            case MirroringSafetyLevel.Full:
                                sMirroringSafetyLevel = "FULL";
                                break;

                            case MirroringSafetyLevel.Off:
                                sMirroringSafetyLevel = "OFF";
                                break;

                            default:
                                // only allowed values that make sense to set MirroringSafetyLevel
                                // to are Full and Off.  if they set it to any others, we should
                                // warn the caller by throwing rather than hiding the error.
                                throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration(mirroringSafetyLevel.ToString()));
                        }

                        if (null != sMirroringSafetyLevel)
                        {
                            queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET SAFETY {1}",
                                                       this.FormatFullNameForScripting(sp), sMirroringSafetyLevel));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the appropriate ALTER DATABASE ... statements to the StringCollection based on
        /// the current state of Remote Data Archive on the database.
        /// </summary>
        /// <param name="queries">The collection of statements to add to</param>
        /// <param name="sp">The settings for generating the scripts</param>
        private void ScriptRemoteDataArchive(StringCollection queries, ScriptingPreferences sp)
        {
            // caller already verified RemoteDataArchieEnabled is supported
            Property propRemoteDataArchiveEnabled = this.Properties.Get("RemoteDataArchiveEnabled");

            bool remoteDataArchiveEnabled = false;
            StringBuilder sbStatement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (!propRemoteDataArchiveEnabled.IsNull)
            {
                remoteDataArchiveEnabled = (bool)propRemoteDataArchiveEnabled.Value;
                //While creating or scripting, script remote data archive only when it is enabled. (default is disabled so no reason to script it in that case)
                if ((propRemoteDataArchiveEnabled.Dirty && sp.ScriptForAlter) || (remoteDataArchiveEnabled && !sp.ScriptForAlter))
                {
                    Property propRemoteDataArchiveEndpoint = this.Properties.Get("RemoteDataArchiveEndpoint");
                    Property propRemoteDataArchiveUsedFederatedServiceAccount = this.Properties.Get("RemoteDataArchiveUseFederatedServiceAccount");
                    Property propRemoteDataArchiveCredential = this.Properties.Get("RemoteDataArchiveCredential");

                    // this will be appended at the end before adding to a query
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, "= {0} ", (bool)remoteDataArchiveEnabled ? Globals.On : Globals.Off);

                    if(((propRemoteDataArchiveEndpoint.Dirty || propRemoteDataArchiveCredential.Dirty) && sp.ScriptForAlter) || !sp.ScriptForAlter)
                    {
                        //Append the SERVER option if remote data archive is enabled. It's only valid when REMOTE_DATA_ARCHIVE = ON (and is required in that case)
                        if (remoteDataArchiveEnabled)
                        {
                            string remoteEndpoint = (propRemoteDataArchiveEndpoint.Value == null) ? string.Empty : propRemoteDataArchiveEndpoint.Value.ToString();
                            if (string.IsNullOrEmpty(remoteEndpoint))
                            {
                                throw new ArgumentException(ExceptionTemplates.RemoteServerEndpointRequired);
                            }

                            bool remoteDataArchiveUseFederatedServiceAccount = !propRemoteDataArchiveUsedFederatedServiceAccount.IsNull ?
                                (bool)propRemoteDataArchiveUsedFederatedServiceAccount.Value : false;

                            if (!remoteDataArchiveUseFederatedServiceAccount)
                            {
                                string credential = (propRemoteDataArchiveCredential.Value == null) ? string.Empty : propRemoteDataArchiveCredential.Value.ToString();
                                if (string.IsNullOrEmpty(credential))
                                {
                                    throw new ArgumentException(ExceptionTemplates.DatabaseScopedCredentialsRequired);
                                }
                            }

                            sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                            sbStatement.AppendFormat(SmoApplication.DefaultCulture, "SERVER = N'{0}'", Util.EscapeString(propRemoteDataArchiveEndpoint.Value.ToString(), '\''));

                            if (remoteDataArchiveUseFederatedServiceAccount)
                            {
                                sbStatement.AppendFormat(SmoApplication.DefaultCulture, ", FEDERATED_SERVICE_ACCOUNT = ON");
                            }
                            else
                            {
                                sbStatement.AppendFormat(SmoApplication.DefaultCulture, ", CREDENTIAL = [{0}]", SqlBraket(propRemoteDataArchiveCredential.Value.ToString()));
                            }

                            sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
                        }
                    }
                }
            }

            if (sbStatement.Length > 0)
            {
                // ALTER DATABASE statement is added only if any of the properties is changed
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET REMOTE_DATA_ARCHIVE {1}", this.FormatFullNameForScripting(sp), sbStatement.ToString()));
            }
        }

        private void ContainmentRelatedValidation(ScriptingPreferences sp)
        {
            //If containment supported on source database, check the version and enginetype of target.
            if (this.IsSupportedProperty("ContainmentType"))
            {
                ContainmentType cType = this.GetPropValueOptional("ContainmentType", ContainmentType.None);

                if (cType == ContainmentType.None)
                {
                    Property defaultFulltextLanguageLcid = this.GetPropertyOptional("DefaultFullTextLanguageLcid");
                    Property defaultFulltextLanguageName = this.GetPropertyOptional("DefaultFullTextLanguageName");
                    Property defaultLanguageLcid = this.GetPropertyOptional("DefaultLanguageLcid");
                    Property defaultLanguageName = this.GetPropertyOptional("DefaultLanguageName");
                    Property nestedTriggersEnabled = this.GetPropertyOptional("NestedTriggersEnabled");
                    Property transformNoiseWords = this.GetPropertyOptional("TransformNoiseWords");
                    Property twoDigitYearCutoff = this.GetPropertyOptional("TwoDigitYearCutoff");

                    if ((defaultFulltextLanguageLcid.Dirty && ((int)defaultFulltextLanguageLcid.Value) >= 0)
                        || (defaultFulltextLanguageName.Dirty && !string.IsNullOrEmpty(defaultFulltextLanguageName.Value.ToString()))
                        || (defaultLanguageLcid.Dirty && ((int)defaultLanguageLcid.Value) >= 0)
                        || (defaultLanguageName.Dirty && !string.IsNullOrEmpty(defaultLanguageName.Value.ToString()))
                        || nestedTriggersEnabled.Dirty
                        || transformNoiseWords.Dirty
                        || twoDigitYearCutoff.Dirty)
                    {
                        throw new SmoException(string.Format(CultureInfo.CurrentCulture,
                            ExceptionTemplates.FollowingPropertiesCanBeSetOnlyWithContainmentEnabled,
                            "DefaultFullTextLanguage",
                            "DefaultLanguage",
                            nestedTriggersEnabled.Name,
                            transformNoiseWords.Name,
                            twoDigitYearCutoff.Name));
                    }
                }
                else
                {
                    //When ContainmentType is not None, then only we will not script the database
                    //to the earlier versions and to SqlAzure database engine.
                    ThrowIfCloud(sp.TargetDatabaseEngineType);
                    ThrowIfBelowVersion110(sp.TargetServerVersionInternal);

                    this.DefaultFullTextLanguage.VerifyBothLcidAndNameNotDirty(false);
                    this.DefaultLanguage.VerifyBothLcidAndNameNotDirty(false);
                }
            }
        }

        /// <summary>
        /// Scripts out alter statements based on the current state of the Database object.
        /// </summary>
        /// <remarks>Currently only partially supported for SQL Azure Databases tracked by VSTS#6669499</remarks>
        /// <param name="alterQuery"></param>
        /// <param name="sp"></param>
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ContainmentRelatedValidation(sp);
            if (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                ScriptAlterForCloud(alterQuery, sp);
            }
            //Scripts containment related part of create database ddl
            this.ScriptAlterContainmentDDL(sp, alterQuery);

            if (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version70)
            {
                ScriptDbProps70Comp(alterQuery, sp);
            }
            else
            {
                ScriptDbProps80Comp(alterQuery, sp, Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType);
                if (IsSupportedProperty("MirroringPartner", sp))
                {
                    ScriptMirroringOptions(alterQuery, sp);
                }
            }

            if (IsSupportedProperty("EncryptionEnabled", sp))
            {
                if (IsSupportedProperty(nameof(this.HasDatabaseEncryptionKey)))
                {
                    bool bUseDBOption = sp.IncludeScripts.DatabaseContext;  //saving the original IncludeDatabaseContext option
                    sp.IncludeScripts.DatabaseContext = true;   //USE [db] statement is essential for creating a DEK object

                    try
                    {
                        if (IsDatabaseEncryptionKeyPresent())    //If DEK object already exists, check if any DEK property is being altered and generate the ALTER script
                        {
                            DatabaseEncryptionKey.Alter();
                        }
                        else if (databaseEncryptionKeyInitialized  //If DEK properties are set, then generate the CREATE script
                                && (!IsDEKInitializedWithoutAnyPropertiesSet()
                                    || this.Properties.Get("EncryptionEnabled").Dirty)  // When EncryptionEnabled Property is set then it means User wants to
                            )                                                           // do Encryption operations on the database, hence we should try to script
                        {                                                               // the DatabaseEncryptionKey in that case.
                            DatabaseEncryptionKey.Create();
                        }
                    }
                    finally
                    {
                        sp.IncludeScripts.DatabaseContext = bUseDBOption;   //restore the IncludeDatabaseContext option
                    }

                }
                Property property = Properties.Get("EncryptionEnabled");
                if (null != property.Value && property.Dirty)
                {
                    StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE {0}", this.FormatFullNameForScripting(sp));
                    sb.AppendFormat(SmoApplication.DefaultCulture, " SET ENCRYPTION {0}", ((bool)property.Value) ? "ON" : "OFF");
                    alterQuery.Add(sb.ToString());
                }
            }

            if (IsSupportedProperty("DefaultFullTextCatalog", sp))
            {
                Property propDefFTC = Properties.Get("DefaultFullTextCatalog");
                if (null != propDefFTC.Value &&
                     (propDefFTC.Dirty || sp.ScriptForCreateDrop) && ((string)propDefFTC.Value).Length > 0)
                {
                    alterQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER FULLTEXT CATALOG [{0}] AS DEFAULT",
                                                  SqlBraket((string)propDefFTC.Value)));
                }
            }

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                bool forCreateScript = false;
                ScriptVardecimalCompression(alterQuery, sp, forCreateScript);
            }

            // Script for change tracking options on database
            if (sp.Data.ChangeTracking && IsSupportedProperty("ChangeTrackingEnabled", sp))
            {
                ScriptChangeTracking(alterQuery, sp);
            }

            if(IsSupportedProperty("RemoteDataArchiveEnabled", sp))
            {
                ScriptRemoteDataArchive(alterQuery, sp);
            }

            if (this.IsSupportedObject<QueryStoreOptions>(sp))
            {
                this.QueryStoreOptions.ScriptAlter(alterQuery, sp);
            }

            // Avoid trying the call to ScriptDbScopedConfigurations() when m_DatabaseScopedConfigurations is null.
            // This codepath would be hit when starting Mirroring and would cause a problem.
            if (m_DatabaseScopedConfigurations != null)
            {
                this.ScriptDbScopedConfigurations(alterQuery, sp);
            }
        }

        /// <summary>
        /// Drops the database
        /// </summary>
        public void Drop()
        {
            DatabaseDropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            DatabaseDropImpl(ifExists: true);
        }

        private void DatabaseDropImpl(bool ifExists = false)
        {
            // If the Server connection is directly to the user database in Azure, Drop will successfully drop the db
            // but a SqlException will be thrown as the connection gets broken.
            // An application should avoid this situation but the SSMS UI uses such a connection in Object Explorer
            var handleSevereError = DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase &&
                (
                Parent.ConnectionContext.DatabaseName.Equals(Name, StringComparison.InvariantCultureIgnoreCase)
                ||
                Parent.ConnectionContext.CurrentDatabase.Equals(Name, StringComparison.InvariantCultureIgnoreCase)
                );
         
            base.DropImpl(ifExists, handleSevereError);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            // no one can delete "master" database with this function!!
            //note: conditional scripting (so.IncludeIfNotExists) is not supported for
            //  the gateway intercepted statements like Database/Login DDLs -sivasat
            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130 &&
                DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType)
            {
                if ((int)SqlServerVersionInternal.Version90 <= (int)sp.TargetServerVersionInternal)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_DATABASE90,
                                "", FormatFullNameForScripting(sp, false));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_DATABASE80,
                                "", FormatFullNameForScripting(sp, false));
                }
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP DATABASE {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130 &&
                DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType) ? "IF EXISTS " : string.Empty,
                FormatFullNameForScripting(sp));
            dropQuery.Add(sb.ToString());
        }

        #region IRenamable implementation
        public void Rename(string newName)
        {
            base.RenameImpl(newName);
            // On a rename make sure we reset the execution manager so we make a connection to the new
            // DB on Azure instances (standalone uses the Server ExecutionManager so don't have this issue)
            this.m_dbExecutionManager = null;
        }

        /// <summary>
        /// Renaming a database should ask for confirmation
        /// </summary>
        public bool WarnOnRename
        {
            get { return true; }
        }
        #endregion
        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            // the user is responsible to put the database in single user mode on 7.0 server
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} MODIFY NAME = {1}",
                                          MakeSqlBraket(this.Name),
                                          MakeSqlBraket(newName)));
        }

        protected override void PostCreate()
        {
            this.PostAlterAndCreate();

            base.PostCreate();
        }

        protected override void PostAlter()
        {
            this.PostAlterAndCreate();

            base.PostAlter();
        }

        private void PostAlterAndCreate()
        {
            CheckObjectState();
            this.SetComparerToNullIfRequired();
            this.isDefaultLanguageModified = this.IsDefaultLanguageDirty();
            this.isDefaultFulltextLanguageModified = this.IsDefaultFullTextLanguageDirty();
        }

        private bool IsDefaultLanguageDirty()
        {
            if (this.IsSupportedProperty("DefaultLanguageLcid"))
            {
                StringCollection sc = new StringCollection();
                sc.Add("DefaultLanguageLcid");
                sc.Add("DefaultLanguageName");

                return this.Properties.ArePropertiesDirty(sc);
            }
            else
            {
                return false;
            }
        }

        private bool IsDefaultFullTextLanguageDirty()
        {
            if (this.IsSupportedProperty("DefaultFullTextLanguageLcid"))
            {
                StringCollection sc = new StringCollection();
                sc.Add("DefaultFullTextLanguageLcid");
                sc.Add("DefaultFullTextLanguageName");

                return this.Properties.ArePropertiesDirty(sc);
            }
            else
            {
                return false;
            }
        }

        private void SetComparerToNullIfRequired()
        {
            // change the comparer, if the user has specified a collation name
            StringCollection sc = new StringCollection();
            sc.Add("Collation");
            if (this.IsSupportedProperty("ContainmentType"))
            {
                sc.Add("ContainmentType");
            }
            if (Properties.ArePropertiesDirty(sc))
            {
                m_comparer = null;
            }
        }

        /// <summary>
        /// Performs the CHECKPOINT command against the current database
        /// </summary>
        public void Checkpoint()
        {
            try
            {
                CheckObjectState();
                StringCollection query = new StringCollection();
                InsertUseDb(0, query, DatabaseEngineType);
                query.Add("CHECKPOINT");
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Checkpoint, this, e);
            }
        }

        internal string GetUseDbStatement(string databaseName)
        {
            return string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(databaseName));
        }

        /// <summary>
        /// Adds USE Db statements, using the script name if specified. Will not add
        /// the statement if the Target Engine Type is Azure since the USE statement
        /// is only supported by Azure if the connection is already to that Database.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="sp"></param>
        internal void AddUseDb(StringCollection col, ScriptingPreferences sp)
        {
            InsertUseDb(col.Count, col, sp.TargetDatabaseEngineType);
        }

        /// <summary>
        /// Adds the USE DB statement to the end of the specified string collection
        /// </summary>
        /// <param name="col">The StringCollection to add the statement to</param>
        /// <remarks>This does not check the engine type, so will add the statement even if this is for use on Azure (which doesn't support USE to switch database context)</remarks>
        internal void AddUseDb(StringCollection col)
        {
            col.Add(GetUseDbStatement(this.Name));
        }

        //
       /// <summary>
        /// Adds the USE Db statements at given index if the target engine type is not cloud.
       /// </summary>
       /// <param name="index">The index to insert the statement at</param>
       /// <param name="col">The StringCollection to add the statement to</param>
       /// <param name="targetEngineType">The target DatabaseEngineType</param>
       /// <remarks>Does nothing if the target engine type is Azure</remarks>
        private void InsertUseDb(int index, StringCollection col, DatabaseEngineType targetEngineType)
        {
            //Azure doesn't support USE DB for switching database contexts
            if (DatabaseEngineType.SqlAzureDatabase != targetEngineType)
            {
                col.Insert(index, GetUseDbStatement(Name));
            }
        }


        public void ExecuteNonQuery(string sqlCommand)
        {
            if (null == sqlCommand)
            {
                throw new ArgumentNullException("sqlCommand");
            }

            ExecuteNonQuery(sqlCommand, ExecutionTypes.Default);
        }


        public void ExecuteNonQuery(string sqlCommand, ExecutionTypes executionType)
        {
            if (null == sqlCommand)
            {
                throw new ArgumentNullException("sqlCommand");
            }

            CheckObjectState();
            StringCollection sqlCommands = new StringCollection();
            sqlCommands.Add(sqlCommand);
            ExecuteNonQuery(sqlCommands, executionType);
        }

        public void ExecuteNonQuery(StringCollection sqlCommands)
        {
            if (null == sqlCommands)
            {
                throw new ArgumentNullException("sqlCommands");
            }

            ExecuteNonQuery(sqlCommands, ExecutionTypes.Default);
        }

        public void ExecuteNonQuery(StringCollection sqlCommands, ExecutionTypes executionType)
        {
            if (null == sqlCommands)
            {
                throw new ArgumentNullException("sqlCommands");
            }

            try
            {
                CheckObjectState();
                this.InsertUseDb(0, sqlCommands, this.DatabaseEngineType);
                this.ExecutionManager.ExecuteNonQuery(sqlCommands, executionType);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ExecuteNonQuery, this, e);
            }
        }

        public DataSet ExecuteWithResults(StringCollection sqlCommands)
        {
            if (null == sqlCommands)
            {
                throw new ArgumentNullException("sqlCommands");
            }

            try
            {
                CheckObjectState();
                this.InsertUseDb(0, sqlCommands, this.DatabaseEngineType);
                return this.ExecutionManager.ExecuteWithResults(sqlCommands);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ExecuteWithResults, this, e);
            }
        }

        public DataSet ExecuteWithResults(string sqlCommand)
        {
            if (null == sqlCommand)
            {
                throw new ArgumentNullException("sqlCommand");
            }

            StringCollection sqlCommands = new StringCollection();
            sqlCommands.Add(sqlCommand);
            return ExecuteWithResults(sqlCommands);
        }

        /// <summary>
        /// Determines if the current user is a member of the given group or role in the current database
        /// </summary>
        /// <param name="groupOrRole"></param>
        public bool IsMember(string groupOrRole)
        {
            try
            {
                CheckObjectState();
                bool OK = false;
                string cmdSQL = "IF IS_MEMBER('" + SqlString(groupOrRole) + "') = 1 BEGIN SELECT 1 END ELSE BEGIN SELECT 0 END";
                DataSet dsResult = this.ExecuteWithResults(cmdSQL);
                DataTable tblResult = dsResult.Tables[0];
                DataRow rowResult = tblResult.Rows[0];
                DataColumn colResult = tblResult.Columns[0];
                if (rowResult[colResult].ToString() == "1")
                {
                    OK = true;
                }

                dsResult.Dispose();
                return OK;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.IsMember, this, e);
            }

        }

        internal void GetAutoGrowFilesScript(StringCollection query, ScriptingPreferences sp)
        {
            string scriptName = this.FormatFullNameForScripting(sp);
            foreach (FileGroup fg in m_FileGroups)
            {
                if (!fg.IsFileStream && fg.AutogrowAllFiles && fg.Files.Count > 0)
                {
                    string autogrowFilesScript = string.Format(SmoApplication.DefaultCulture,
                        "ALTER DATABASE {0} MODIFY FILEGROUP [{1}] AUTOGROW_ALL_FILES",
                        scriptName, SqlBraket(fg.Name));
                    query.Add(autogrowFilesScript.ToString());
                }
            }
        }

        // scripting helper functions
        internal void GetFileGroupsScript(StringBuilder query, bool databaseIsView,
                                          StringCollection emptyfgs, ScriptingPreferences sp)
        {
            FileGroup fgPrimary = m_FileGroups["PRIMARY"];
            if (null == fgPrimary)
            {
                throw new PropertyNotSetException("Primary file");
            }

            if (State == SqlSmoState.Existing)
            {
                fgPrimary.Initialize(true);
            }

            fgPrimary.ScriptDdl(sp, query, databaseIsView);

            foreach (FileGroup fg in m_FileGroups)
            {
                // skip primary file, we have scripted it above
                if (fg == fgPrimary)
                {
                    continue;
                }

                if (State == SqlSmoState.Existing)
                {
                    fg.Initialize(true);
                }

                if (fg.Files.Count > 0)
                {
                    query.Append(Globals.commaspace);
                    query.Append(Globals.newline);
                    fg.ScriptDdl(sp, query, databaseIsView);
                }
                else
                {
                    fg.ScriptCreateInternal(emptyfgs, sp);
                }
            }
        }

        internal void GetLogFilesScript(ScriptingPreferences sp, StringBuilder query)
        {
            int nFile = 0;
            foreach (LogFile lgf in m_LogFiles)
            {
                if (nFile++ > 0)
                {
                    query.Append(Globals.commaspace);
                }

                query.Append(Globals.newline);  // to have a better formatting of the query
                if (State == SqlSmoState.Existing)
                {
                    lgf.Initialize(true);
                }

                FileGroup.GetFileScriptWithCheck(sp, lgf, query, false);
            }
        }

        /// <summary>
        /// Alters the database mirroring status
        /// </summary>
        /// <param name="mirroringOption"></param>
        public void ChangeMirroringState(MirroringOption mirroringOption)
        {
            try
            {
                CheckObjectState();
                ThrowIfBelowVersion90();

                string sMirroringOption = null;
                string sMirroringOptionSubject = "PARTNER";
                bool bSwitchToMaster = false;

                switch (mirroringOption)
                {
                    case MirroringOption.Off: sMirroringOption = "OFF"; break;
                    case MirroringOption.Suspend: sMirroringOption = "SUSPEND"; break;
                    case MirroringOption.Resume: sMirroringOption = "RESUME"; break;
                    case MirroringOption.RemoveWitness: sMirroringOptionSubject = "WITNESS"; sMirroringOption = "OFF"; break;
                    case MirroringOption.Failover: sMirroringOption = "FAILOVER"; bSwitchToMaster = true; break;
                    case MirroringOption.ForceFailoverAndAllowDataLoss: sMirroringOption = "FORCE_SERVICE_ALLOW_DATA_LOSS "; break;
                }

                if (null != sMirroringOption)
                {
                    string query = string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET {1} {2}",
                                                 this.FormatFullNameForScripting(new ScriptingPreferences()), sMirroringOptionSubject, sMirroringOption);

                    // Do we need to switch to the master database before the query
                    if (bSwitchToMaster)
                    {
                        // Yes, then do so
                        query = Scripts.USEMASTER + ";" + query;
                    }
                    this.ExecutionManager.ExecuteNonQuery(query);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangeMirroringState, this, e);
            }
        }

        /// <summary>
        /// Deletes the entries in the backup and restore history tables for database
        /// </summary>
        public void DropBackupHistory()
        {
            try
            {
                CheckObjectState();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropBackupHistory, this, e);
            }

            //
            //if we are here we know we have a valid parent Server
            //just call DeleteBackupHistory on the Server object
            //
            Server srv = this.Parent;
            srv.DeleteBackupHistory(this.Name);
        }

        // this property is a little special, because its accessibility depends on the
        // SP version, not only on the server version
        [SfcProperty(SfcPropertyFlags.Deploy | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.Boolean DatabaseOwnershipChaining
        {
            get
            {
                ThrowIfBelowVersion80SP3();
                return (System.Boolean)(Properties["DatabaseOwnershipChaining"].Value);
            }

            set
            {
                ThrowIfBelowVersion80SP3();
                Properties.Get("DatabaseOwnershipChaining").Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the MD Catalog Collation type.  Only valid during creation, and we cannot specify ContainedDatabaseCollation explicitly
        /// </summary>
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.SqlAzureDatabase)]
        public CatalogCollationType CatalogCollation
        {
            get
            {
                return (CatalogCollationType)this.Properties.GetValueWithNullReplacement("CatalogCollation");
            }
            set
            {
                if (CatalogCollationType.ContainedDatabaseFixedCollation == value)
                {
                    throw new SmoException(ExceptionTemplates.InnerException,
                        new ArgumentException(ExceptionTemplates.CantSetContainedDatabaseCatalogCollation));
                }

                Properties.SetValueWithConsistencyCheck("CatalogCollation", value);
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        private DatabaseOptions m_DatabaseOptions;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public DatabaseOptions DatabaseOptions
        {
            get
            {
                CheckObjectStateImpl(false);
                if (null == m_DatabaseOptions)
                {
                    m_DatabaseOptions = new DatabaseOptions(this, new ObjectKeyBase(), this.State/*SqlSmoState.Existing*/);
                }
                return m_DatabaseOptions;
            }
        }

        private QueryStoreOptions m_QueryStoreOptions;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public QueryStoreOptions QueryStoreOptions
        {
            get
            {
                CheckObjectStateImpl(false);
                this.ThrowIfNotSupported(typeof(QueryStoreOptions));
                if (null == m_QueryStoreOptions)
                {
                    m_QueryStoreOptions = new QueryStoreOptions(this, new ObjectKeyBase(), this.State/*SqlSmoState.Existing*/);
                }
                return m_QueryStoreOptions;
            }
        }

        private SynonymCollection m_Synonyms;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Synonym), SfcObjectFlags.Design)]
        public SynonymCollection Synonyms
        {
            get
            {
                CheckObjectState();
                if (null == m_Synonyms)
                {
                    m_Synonyms = new SynonymCollection(this);
                }
                return m_Synonyms;
            }
        }

        private SequenceCollection m_Sequences;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Sequence))]
        public SequenceCollection Sequences
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(Sequence));
                if (null == m_Sequences)
                {
                    m_Sequences = new SequenceCollection(this);
                }
                return m_Sequences;
            }
        }

        TableCollection m_Tables;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Table), SfcObjectFlags.Design)]
        public TableCollection Tables
        {
            get
            {
                CheckObjectState();
                if (m_Tables == null)
                {
                    m_Tables = new TableCollection(this);
                }
                return m_Tables;
            }
        }

        SensitivityClassificationCollection m_SensitivityClassifications;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(SensitivityClassification))]
        public SensitivityClassificationCollection SensitivityClassifications
        {
            get
            {
                CheckObjectState();
                if (m_SensitivityClassifications == null)
                {
                    m_SensitivityClassifications = new SensitivityClassificationCollection(this);
                }
                return m_SensitivityClassifications;
            }
        }

        DatabaseScopedCredentialCollection m_DatabaseScopedCredentials;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(DatabaseScopedCredential))]
        public DatabaseScopedCredentialCollection DatabaseScopedCredentials
        {
            get
            {
                this.ThrowIfNotSupported(typeof(DatabaseScopedCredential));
                CheckObjectState();
                if (m_DatabaseScopedCredentials == null)
                {
                    m_DatabaseScopedCredentials = new DatabaseScopedCredentialCollection(this);
                }
                return m_DatabaseScopedCredentials;
            }
        }

        WorkloadManagementWorkloadClassifierCollection m_WlmWorkloadClassifiers = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(WorkloadManagementWorkloadClassifier))]
        public WorkloadManagementWorkloadClassifierCollection WorkloadManagementWorkloadClassifiers
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(WorkloadManagementWorkloadClassifier));
                if (m_WlmWorkloadClassifiers == null)
                {
                    m_WlmWorkloadClassifiers = new WorkloadManagementWorkloadClassifierCollection(this);
                }

                return m_WlmWorkloadClassifiers;
            }
        }

        StoredProcedureCollection m_StoredProcedures;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(StoredProcedure), SfcObjectFlags.Design)]
        public StoredProcedureCollection StoredProcedures
        {
            get
            {
                CheckObjectState();
                if (m_StoredProcedures == null)
                {
                    m_StoredProcedures = new StoredProcedureCollection(this);
                }
                return m_StoredProcedures;
            }
        }

        SqlAssemblyCollection m_SqlAssemblies;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(SqlAssembly))]
        public SqlAssemblyCollection Assemblies
        {
            get
            {
                this.ThrowIfNotSupported(typeof(SqlAssembly));
                CheckObjectState();
                if (m_SqlAssemblies == null)
                {
                    m_SqlAssemblies = new SqlAssemblyCollection(this);
                }
                return m_SqlAssemblies;
            }
        }

        ExternalLanguageCollection m_ExternalLanguages;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalLanguage))]
        public ExternalLanguageCollection ExternalLanguages
        {
            get
            {
                this.ThrowIfNotSupported(typeof(ExternalLanguage));
                CheckObjectState();
                if (m_ExternalLanguages == null)
                {
                    m_ExternalLanguages = new ExternalLanguageCollection(this);
                }
                return m_ExternalLanguages;
            }
        }

        ExternalLibraryCollection m_ExternalLibraries;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalLibrary))]
        public ExternalLibraryCollection ExternalLibraries
        {
            get
            {
                this.ThrowIfNotSupported(typeof(ExternalLibrary));
                CheckObjectState();
                if (m_ExternalLibraries == null)
                {
                    m_ExternalLibraries = new ExternalLibraryCollection(this);
                }
                return m_ExternalLibraries;
            }
        }

        UserDefinedTypeCollection m_UserDefinedTypes;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(UserDefinedType))]
        public UserDefinedTypeCollection UserDefinedTypes
        {
            get
            {
                CheckObjectState();
                if (m_UserDefinedTypes == null)
                {
                    m_UserDefinedTypes = new UserDefinedTypeCollection(this);
                }
                return m_UserDefinedTypes;
            }
        }

        UserDefinedAggregateCollection m_UserDefinedAggregates;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(UserDefinedAggregate))]
        public UserDefinedAggregateCollection UserDefinedAggregates
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(UserDefinedAggregate));
                if (m_UserDefinedAggregates == null)
                {
                    m_UserDefinedAggregates = new UserDefinedAggregateCollection(this);
                }
                return m_UserDefinedAggregates;
            }
        }

        FullTextCatalogCollection m_FullTextCatalogs;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(FullTextCatalog))]
        public FullTextCatalogCollection FullTextCatalogs
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(FullTextCatalog));
                if (m_FullTextCatalogs == null)
                {
                    m_FullTextCatalogs = new FullTextCatalogCollection(this);
                }
                return m_FullTextCatalogs;
            }
        }

        //StopList collection
        FullTextStopListCollection m_FullTextStopLists;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(FullTextStopList))]
        public FullTextStopListCollection FullTextStopLists
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(FullTextStopList));
                if (m_FullTextStopLists == null)
                {
                    m_FullTextStopLists = new FullTextStopListCollection(this);
                }
                return m_FullTextStopLists;
            }
        }

        //SearchPropertyList collection
        SearchPropertyListCollection m_SearchPropertyLists;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(SearchPropertyList))]
        public SearchPropertyListCollection SearchPropertyLists
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(SearchPropertyList));
                if (m_SearchPropertyLists == null)
                {
                    m_SearchPropertyLists = new SearchPropertyListCollection(this);
                }
                return m_SearchPropertyLists;
            }
        }

        //SecurityPolicies collection
        SecurityPolicyCollection m_SecurityPolicies;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(SecurityPolicy))]
        public SecurityPolicyCollection SecurityPolicies
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(SecurityPolicy));
                if (m_SecurityPolicies == null)
                {
                    m_SecurityPolicies = new SecurityPolicyCollection(this);
                }
                return m_SecurityPolicies;
            }
        }

        //DatabaseScopedConfiguration collection
        DatabaseScopedConfigurationCollection m_DatabaseScopedConfigurations;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(DatabaseScopedConfiguration))]
        public DatabaseScopedConfigurationCollection DatabaseScopedConfigurations
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(DatabaseScopedConfiguration));
                if (m_DatabaseScopedConfigurations == null)
                {
                    m_DatabaseScopedConfigurations = new DatabaseScopedConfigurationCollection(this);
                }

                return m_DatabaseScopedConfigurations;
            }
        }

        //ExternalDataSources collection
        ExternalDataSourceCollection m_ExternalDataSources;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalDataSource))]
        public ExternalDataSourceCollection ExternalDataSources
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(ExternalDataSource));
                if (m_ExternalDataSources == null)
                {
                    m_ExternalDataSources = new ExternalDataSourceCollection(this);
                }
                return m_ExternalDataSources;
            }
        }
        //ExternalFileFormats collection
        ExternalFileFormatCollection m_ExternalFileFormats;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalFileFormat))]
        public ExternalFileFormatCollection ExternalFileFormats
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(ExternalFileFormat));
                if (m_ExternalFileFormats == null)
                {
                    m_ExternalFileFormats = new ExternalFileFormatCollection(this);
                }
                return m_ExternalFileFormats;
            }
        }

        //ExternalStreams collection
        ExternalStreamCollection m_ExternalStreams;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalStream))]
        [SfcIgnore]
        public ExternalStreamCollection ExternalStreams
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(ExternalStream));
                if (m_ExternalStreams == null)
                {
                    m_ExternalStreams = new ExternalStreamCollection(this);
                }
                return m_ExternalStreams;
            }
        }

        //ExternalStreamsJobs collection
        ExternalStreamingJobCollection m_ExternalStreamingJobs;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalStreamingJob))]
        [SfcIgnore]
        public ExternalStreamingJobCollection ExternalStreamingJobs
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(ExternalStreamingJob));
                if (m_ExternalStreamingJobs == null)
                {
                    m_ExternalStreamingJobs = new ExternalStreamingJobCollection(this);
                }
                return m_ExternalStreamingJobs;
            }
        }

        //certificates collection
        CertificateCollection certificateCollection;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Certificate))]
        public CertificateCollection Certificates
        {
            get
            {
                //available on Yukon or bigger
                this.ThrowIfNotSupported(typeof(Certificate));
                CheckObjectState();

                if (certificateCollection == null)
                {
                    certificateCollection = new CertificateCollection(this);
                }
                return certificateCollection;
            }
        }

        //ColumnMasterKey collection
        ColumnMasterKeyCollection m_ColumnMasterKeys;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ColumnMasterKey))]
        public ColumnMasterKeyCollection ColumnMasterKeys
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(ColumnMasterKey));
                if (m_ColumnMasterKeys == null)
                {
                    m_ColumnMasterKeys = new ColumnMasterKeyCollection(this);
                }
                return m_ColumnMasterKeys;
            }
        }

        //ColumnEncryptionKey collection
        ColumnEncryptionKeyCollection m_ColumnEncryptionKeys;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ColumnEncryptionKey))]
        public ColumnEncryptionKeyCollection ColumnEncryptionKeys
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(ColumnEncryptionKey));
                if (m_ColumnEncryptionKeys == null)
                {
                    m_ColumnEncryptionKeys = new ColumnEncryptionKeyCollection(this);
                }
                return m_ColumnEncryptionKeys;
            }
        }

        //SymmetricKey collection
        SymmetricKeyCollection symmetricKeyCollection;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(SymmetricKey))]
        public SymmetricKeyCollection SymmetricKeys
        {
            get
            {
                //available on Yukon or bigger
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState();

                if (symmetricKeyCollection == null)
                {
                    symmetricKeyCollection = new SymmetricKeyCollection(this);
                }
                return symmetricKeyCollection;
            }
        }

        //AsymmetricKey collection
        AsymmetricKeyCollection asymmetricKeyCollection;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(AsymmetricKey))]
        public AsymmetricKeyCollection AsymmetricKeys
        {
            get
            {
                //available on Yukon or bigger
                this.ThrowIfNotSupported(typeof(AsymmetricKey));
                CheckObjectState();

                if (asymmetricKeyCollection == null)
                {
                    asymmetricKeyCollection = new AsymmetricKeyCollection(this);
                }
                return asymmetricKeyCollection;
            }
        }

        //DatabaseEncryptionKey object
        DatabaseEncryptionKey m_DatabaseEncryptionKey = null;
        internal bool databaseEncryptionKeyInitialized = false;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public DatabaseEncryptionKey DatabaseEncryptionKey
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(DatabaseEncryptionKey));
                if (!databaseEncryptionKeyInitialized)
                {
                    m_DatabaseEncryptionKey = InitializeDatabaseEncryptionKey();
                    databaseEncryptionKeyInitialized = true;
                }
                return m_DatabaseEncryptionKey;
            }
        }

        private DatabaseEncryptionKey InitializeDatabaseEncryptionKey()
        {
            bool keyExists = IsDatabaseEncryptionKeyPresent();
            SqlSmoState state = keyExists ? SqlSmoState.Existing : SqlSmoState.Creating;
            return new DatabaseEncryptionKey(this, new ObjectKeyBase(), state);
        }

        /// <summary>
        /// This method will return a true value if the database has a DEK, otherwise false
        /// </summary>
        /// <returns></returns>
        private bool IsDatabaseEncryptionKeyPresent()
        {
            bool isKeyPresent = false;

            if (!this.IsDesignMode)
            {
                try
                {
                    //This is tricky : Check whether the DEK for the database exists from the system catalog view
                    //as the Enumerator request method generates the USE [DB] statement which fails to execute if we are in Capture mode(if the [DB] database is created in capture mode)
                    String query = string.Format(SmoApplication.DefaultCulture, "select create_date from sys.dm_database_encryption_keys where database_id = DB_ID({0})", MakeSqlString(this.Name));

                    DataTable dt = ExecuteSql.ExecuteWithResults(query, this.ExecutionManager.ConnectionContext);
                    isKeyPresent = (dt.Rows.Count != 0);
                }
                catch (ExecutionFailureException exc)
                {
                    SqlException sqlExc = exc.InnerException as SqlException;
                    if (sqlExc != null &&
                        // sys.dm_database_encryption_keys requires VIEW SERVER STATE.
                        //
                        (sqlExc.Number == 300 ||  // No permission on object.
                        //sys.dm_database_encryption_keys requires VIEW DATABASE STATE
                         sqlExc.Number == 262 ))  //No permission in Database
                    {
                        // In case user doesn't have permission to view sys.dm_database_encryption_keys, we shall only return false instead of throwing exception.
                        Diagnostics.TraceHelper.Trace("Database SMO Object", exc.Message);
                    }
                    else
                    {
                        // Rethrow.
                        //
                        throw;
                    }
                }
            }

            return isKeyPresent;
        }

        /// <summary>
        /// This method will return a true value if DEK is initialized but none of its properties required for script or create are set.
        /// </summary>
        /// <returns></returns>
        private bool IsDEKInitializedWithoutAnyPropertiesSet()
        {
            return (m_DatabaseEncryptionKey.State == SqlSmoState.Creating           // As DEK is a property of Database, when we expand the DatabaseEncryptionKey
                    && !(m_DatabaseEncryptionKey.InternalIsObjectDirty));           // Property in the debugger's watch window, a new DEK object is initialized
            // but as this is un-intentional, we don't set properties of the DEK object
            // which are required to script/create it. VSTS ID: 280404
        }

        ExtendedStoredProcedureCollection m_ExtendedStoredProcedures;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedStoredProcedure))]
        public ExtendedStoredProcedureCollection ExtendedStoredProcedures
        {
            get
            {
                CheckObjectState();
                if (m_ExtendedStoredProcedures == null)
                {
                    m_ExtendedStoredProcedures = new ExtendedStoredProcedureCollection(this);
                }
                return m_ExtendedStoredProcedures;
            }
        }

        UserDefinedFunctionCollection m_UserDefinedFunctions;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(UserDefinedFunction), SfcObjectFlags.Design)]
        public UserDefinedFunctionCollection UserDefinedFunctions
        {
            get
            {
                // on 7.0 server the user does not have acces to udf's
                ThrowIfBelowVersion80();
                CheckObjectState();

                if (m_UserDefinedFunctions == null)
                {
                    m_UserDefinedFunctions = new UserDefinedFunctionCollection(this);
                }
                return m_UserDefinedFunctions;
            }
        }

        ViewCollection m_Views;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(View), SfcObjectFlags.Design)]
        public ViewCollection Views
        {
            get
            {
                CheckObjectState();
                if (m_Views == null)
                {
                    m_Views = new ViewCollection(this);
                }
                return m_Views;
            }
        }

        UserCollection m_Users;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(User), SfcObjectFlags.Design)]
        public UserCollection Users
        {
            get
            {
                CheckObjectState();
                if (m_Users == null)
                {
                    m_Users = new UserCollection(this);
                }
                return m_Users;
            }
        }

        DatabaseAuditSpecificationCollection databaseAuditSpecifications;
        /// <summary>
        /// DatabaseAuditSpecification Collection
        /// </summary>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(DatabaseAuditSpecification))]
        public DatabaseAuditSpecificationCollection DatabaseAuditSpecifications
        {
            get
            {
                this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
                CheckObjectState();
                if (databaseAuditSpecifications == null)
                {
                    databaseAuditSpecifications = new DatabaseAuditSpecificationCollection(this);
                }
                return databaseAuditSpecifications;
            }
        }

        SchemaCollection m_Schemas;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Schema), SfcObjectFlags.Design)]
        public SchemaCollection Schemas
        {
            get
            {
                CheckObjectState();
                if (m_Schemas == null)
                {
                    m_Schemas = new SchemaCollection(this);
                }
                return m_Schemas;
            }
        }

        DatabaseRoleCollection m_Roles;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(DatabaseRole), SfcObjectFlags.Design)]
        public DatabaseRoleCollection Roles
        {
            get
            {
                CheckObjectState();
                if (m_Roles == null)
                {
                    m_Roles = new DatabaseRoleCollection(this);
                }
                return m_Roles;
            }
        }

        ApplicationRoleCollection m_ApplcicationRoles;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ApplicationRole))]
        public ApplicationRoleCollection ApplicationRoles
        {
            get
            {
                CheckObjectState();
                if (m_ApplcicationRoles == null)
                {
                    m_ApplcicationRoles = new ApplicationRoleCollection(this);
                }
                return m_ApplcicationRoles;
            }
        }

        BackupSetCollection m_BackupSets;
        /// <summary>
        /// Gets the backup sets of the database.
        /// </summary>
        /// <value>The backup sets.</value>
        internal BackupSetCollection BackupSets
        {
            get
            {
                CheckObjectState();
                if (m_BackupSets == null)
                {
                    m_BackupSets = new BackupSetCollection(this);
                }
                return m_BackupSets;
            }
        }

        LogFileCollection m_LogFiles;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(LogFile))]
        public LogFileCollection LogFiles
        {
            get
            {
                CheckObjectState();
                if (m_LogFiles == null)
                {
                    m_LogFiles = new LogFileCollection(this);
                }
                return m_LogFiles;
            }
        }

        FileGroupCollection m_FileGroups;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(FileGroup))]
        public FileGroupCollection FileGroups
        {
            get
            {
                CheckObjectState();
                if (m_FileGroups == null)
                {
                    m_FileGroups = new FileGroupCollection(this);
                }
                return m_FileGroups;
            }
        }

        PlanGuideCollection m_PlanGuides;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(PlanGuide))]
        public PlanGuideCollection PlanGuides
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(PlanGuide));
                if (m_PlanGuides == null)
                {
                    m_PlanGuides = new PlanGuideCollection(this);
                }
                return m_PlanGuides;
            }
        }

        DefaultCollection m_Defaults;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Default))]
        public DefaultCollection Defaults
        {
            get
            {
                CheckObjectState();
                if (m_Defaults == null)
                {
                    m_Defaults = new DefaultCollection(this);
                }
                return m_Defaults;
            }
        }

        RuleCollection m_Rules;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Rule))]
        public RuleCollection Rules
        {
            get
            {
                CheckObjectState();
                if (m_Rules == null)
                {
                    m_Rules = new RuleCollection(this);
                }
                return m_Rules;
            }
        }

        UserDefinedDataTypeCollection m_UserDefinedDataTypes;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(UserDefinedDataType), SfcObjectFlags.Design)]
        public UserDefinedDataTypeCollection UserDefinedDataTypes
        {
            get
            {
                CheckObjectState();
                if (m_UserDefinedDataTypes == null)
                {
                    m_UserDefinedDataTypes = new UserDefinedDataTypeCollection(this);
                }
                return m_UserDefinedDataTypes;
            }
        }

        UserDefinedTableTypeCollection m_UserDefinedTableTypes;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(UserDefinedTableType), SfcObjectFlags.Design)]
        public UserDefinedTableTypeCollection UserDefinedTableTypes
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(UserDefinedTableType));
                if (m_UserDefinedTableTypes == null)
                {
                    m_UserDefinedTableTypes = new UserDefinedTableTypeCollection(this);
                }
                return m_UserDefinedTableTypes;
            }
        }

        XmlSchemaCollectionCollection m_XmlSchemaCollections = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(XmlSchemaCollection))]
        public XmlSchemaCollectionCollection XmlSchemaCollections
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(XmlSchemaCollection));

                if (m_XmlSchemaCollections == null)
                {
                    m_XmlSchemaCollections = new XmlSchemaCollectionCollection(this);
                }
                return m_XmlSchemaCollections;
            }
        }

        PartitionFunctionCollection m_PartitionFunctions;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(PartitionFunction))]
        public PartitionFunctionCollection PartitionFunctions
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(PartitionFunction));
                if (m_PartitionFunctions == null)
                {
                    m_PartitionFunctions = new PartitionFunctionCollection(this);
                }
                return m_PartitionFunctions;
            }
        }

        PartitionSchemeCollection m_PartitionSchemes;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(PartitionScheme))]
        public PartitionSchemeCollection PartitionSchemes
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(PartitionScheme));
                if (m_PartitionSchemes == null)
                {
                    m_PartitionSchemes = new PartitionSchemeCollection(this);
                }
                return m_PartitionSchemes;
            }
        }

        MasterKey masterKey = null;
        internal bool masterKeyInitialized = false;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.ZeroToOne)]
        public MasterKey MasterKey
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(MasterKey));
                if (!masterKeyInitialized && !this.IsDesignMode)
                {

                    masterKey = InitializeMasterKey();
                    masterKeyInitialized = true;
                }
                return masterKey;
            }
        }

        internal bool DoesMasterKeyAlreadyExist()
        {
            return (masterKey != null);
        }

        internal void SetRefMasterKey(MasterKey mk)
        {
            masterKey = mk;
        }

        internal void SetNullRefMasterKey()
        {
            masterKey = null;
        }

        private MasterKey InitializeMasterKey()
        {
            //create request for master key
            Request req = new Request(this.Urn + "/" + MasterKey.UrnSuffix);
            req.Fields = new String[] { "CreateDate" };
            DataTable dt = this.ExecutionManager.GetEnumeratorData(req);


            if (1 != dt.Rows.Count)
            {
                //if it doesn't have master key return null
                return null;
            }
            else
            {
                return new MasterKey(this, new ObjectKeyBase(), SqlSmoState.Existing);
            }
        }


        DatabaseDdlTriggerCollection databaseDdlTriggerCollection = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(DatabaseDdlTrigger))]
        public DatabaseDdlTriggerCollection Triggers
        {
            get
            {
                this.ThrowIfNotSupported(typeof(DatabaseDdlTrigger));
                if (databaseDdlTriggerCollection == null)
                {
                    databaseDdlTriggerCollection = new DatabaseDdlTriggerCollection(this);
                }
                return databaseDdlTriggerCollection;
            }
        }

        private DefaultLanguage defaultLanguageObj;

        /// <summary>
        /// Gets or sets the default language for the users of this database.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public DefaultLanguage DefaultLanguage
        {
            get
            {
                this.ThrowIfCloudProp("DefaultLanguage");
                this.ThrowIfBelowVersion110Prop("DefaultLanguage");

                if (this.defaultLanguageObj == null)
                {
                    this.defaultLanguageObj = new DefaultLanguage(this, "DefaultLanguage");
                }

                return this.defaultLanguageObj;
            }
            //This property is not like other SfcProperties. In order to deserialize this property
            //we need to have a setter for this. Hence implementing an internal setter.
            internal set //Design Mode
            {
                this.ThrowIfCloudProp("DefaultLanguage");
                this.ThrowIfBelowVersion110Prop("DefaultLanguage");

                if (value.IsProperlyInitialized())
                {
                    this.defaultLanguageObj = value;
                }
                else
                {
                    this.defaultLanguageObj = value.Copy(this, "DefaultLanguage");
                }
            }
        }

        private DefaultLanguage defaultFullTextLanguageObj;

        /// <summary>
        /// Gets or sets the default fulltext language of this database.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public DefaultLanguage DefaultFullTextLanguage
        {
            get
            {
                this.ThrowIfCloudProp("DefaultFullTextLanguage");
                this.ThrowIfBelowVersion110Prop("DefaultFullTextLanguage");

                if (this.defaultFullTextLanguageObj == null)
                {
                    this.defaultFullTextLanguageObj = new DefaultLanguage(this, "DefaultFullTextLanguage");
                }

                return this.defaultFullTextLanguageObj;
            }
            //This property is not like other SfcProperties. In order to deserialize this property
            //we need to have a setter for this. Hence implementing an internal setter.
            internal set //Design Mode
            {
                this.ThrowIfCloudProp("DefaultFullTextLanguage");
                this.ThrowIfBelowVersion110Prop("DefaultFullTextLanguage");

                if (value.IsProperlyInitialized())
                {
                    this.defaultFullTextLanguageObj = value;
                }
                else
                {
                    this.defaultFullTextLanguageObj = value.Copy(this, "DefaultFullTextLanguage");
                }
            }
        }

        /*
        Disabling Symmetric keys for B3

        SymmetricKeyCollection m_SymmetricKeys = null;
        public SymmetricKeyCollection SymmetricKeys
        {
            get
            {
                CheckObjectState();
                if (m_SymmetricKeys == null)
                {
                    m_SymmetricKeys = new SymmetricKeyCollection(this);
                }

                return m_SymmetricKeys;
            }
        }
        */

        WorkloadManagementWorkloadGroupCollection m_WorkloadManagementWorkloadGroups = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(WorkloadManagementWorkloadGroup))]
        public WorkloadManagementWorkloadGroupCollection WorkloadManagementWorkloadGroups
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(WorkloadManagementWorkloadGroup));
                if (m_WorkloadManagementWorkloadGroups == null)
                {
                    m_WorkloadManagementWorkloadGroups = new WorkloadManagementWorkloadGroupCollection(this);
                }

                return m_WorkloadManagementWorkloadGroups;
            }
        }

        ServiceBroker m_ServiceBroker;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public ServiceBroker ServiceBroker
        {
            get
            {
                // only 9.0 servers support ServiceBroker
                this.ThrowIfNotSupported(typeof(ServiceBroker));
                if (null == m_ServiceBroker)
                {
                    /*
                    // is service Broker enabled on this database ?
                    // We have to check with the enumerator. Since we do a roundtrip, we'll
                    // get all its properties
                    Request req = new Request(this.Urn + "/" + ServiceBrokerBase.UrnSuffix);
                    DataTable dt = this.ExecutionManager.GetEnumeratorData(req);
                    */

                    // create a ServiceBroker object
                    m_ServiceBroker = new ServiceBroker(this, new ObjectKeyBase(), SqlSmoState.Existing);
                    /*
                    if( 0 != dt.Rows.Count )
                    {
                        // if the enumerator returned non empty DataSet this means
                        // that the broker is enabled
                        // update state to existing
                        m_ServiceBroker.SetState( SqlSmoState.Existing );

                        // set its properties
                        m_ServiceBroker.AddObjectProps(dt.Rows[0]);

                        // update enabled flag
                        m_ServiceBroker.SetEnabled(true);

                        // get EnvironmentServices collection
                        DataTable dtES = this.ExecutionManager.GetEnumeratorData( new Request( m_ServiceBroker.Urn + "/EnvironmentService" ));
                        foreach( DataRow dr in dtES.Rows )
                        {
                            m_ServiceBroker.EnvironmentServices.Add( (string)dr["Name"] );
                        }
                    }
                      */
                }

                return m_ServiceBroker;
            }
        }

        /// <summary>
        /// Gets or sets the MaxDop of the database scoped configuration.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public int MaxDop
        {
            get
            {
                ThrowIfPropertyNotSupported("MaxDop");
                return Convert.ToInt32(DatabaseScopedConfigurations["MAXDOP"].Value);
            }

            set
            {
                ThrowIfPropertyNotSupported("MaxDop");
                DatabaseScopedConfigurations["MAXDOP"].Value = value.ToString();
            }
        }

        /// <summary>
        /// Get or set the MaxDop for secondary of the database scoped configuration special accounting for nulls.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public int? MaxDopForSecondary
        {
            get
            {
                ThrowIfPropertyNotSupported("MaxDopForSecondary");
                if (0 == string.Compare(DatabaseScopedConfigurations["MAXDOP"].ValueForSecondary, "PRIMARY", true))
                {
                    return null;
                }

                return Convert.ToInt32(DatabaseScopedConfigurations["MAXDOP"].ValueForSecondary);
            }

            set
            {
                ThrowIfPropertyNotSupported("MaxDopForSecondary");
                DatabaseScopedConfigurations["MAXDOP"].ValueForSecondary = (value == null ? "PRIMARY": value.ToString());
            }
        }

        /// <summary>
        /// Get or set the Legacy_Cardinality_Estimation of the database scoped configuration.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public DatabaseScopedConfigurationOnOff LegacyCardinalityEstimation
        {
            get
            {
                ThrowIfPropertyNotSupported("LegacyCardinalityEstimation");
                return (DatabaseScopedConfigurationOnOff)Enum.Parse(typeof(DatabaseScopedConfigurationOnOff),
                                                                    DatabaseScopedConfigurations["Legacy_Cardinality_Estimation"].Value,
                                                                    ignoreCase: true);
            }

            set
            {
                ThrowIfPropertyNotSupported("LegacyCardinalityEstimation");
                DatabaseScopedConfigurations["Legacy_Cardinality_Estimation"].Value = value.ToString();
            }
        }

        /// <summary>
        /// Get or set the Legacy_Cardinality_Estimation for secondary of the database scoped configuration.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public DatabaseScopedConfigurationOnOff LegacyCardinalityEstimationForSecondary
        {
            get
            {
                ThrowIfPropertyNotSupported("LegacyCardinalityEstimationForSecondary");
                return (DatabaseScopedConfigurationOnOff)Enum.Parse(typeof(DatabaseScopedConfigurationOnOff),
                                                                    DatabaseScopedConfigurations["Legacy_Cardinality_Estimation"].ValueForSecondary,
                                                                    ignoreCase: true);
            }

            set
            {
                ThrowIfPropertyNotSupported("LegacyCardinalityEstimationForSecondary");
                DatabaseScopedConfigurations["Legacy_Cardinality_Estimation"].ValueForSecondary = value.ToString();
            }
        }

        /// <summary>
        /// Get or set the Parameter_Sniffing of the database scoped configuration.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public DatabaseScopedConfigurationOnOff ParameterSniffing
        {
            get
            {
                ThrowIfPropertyNotSupported("ParameterSniffing");
                return (DatabaseScopedConfigurationOnOff)Enum.Parse(typeof(DatabaseScopedConfigurationOnOff),
                                                                    DatabaseScopedConfigurations["Parameter_Sniffing"].Value,
                                                                    ignoreCase: true);
            }

            set
            {
                ThrowIfPropertyNotSupported("ParameterSniffing");
                DatabaseScopedConfigurations["Parameter_Sniffing"].Value = value.ToString();
            }
        }

        /// <summary>
        /// Get or set the Parameter_Sniffing for secondary of the database scoped configuration.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public DatabaseScopedConfigurationOnOff ParameterSniffingForSecondary
        {
            get
            {
                ThrowIfPropertyNotSupported("ParameterSniffingForSecondary");
                return (DatabaseScopedConfigurationOnOff)Enum.Parse(typeof(DatabaseScopedConfigurationOnOff),
                                                                    DatabaseScopedConfigurations["Parameter_Sniffing"].ValueForSecondary,
                                                                    ignoreCase: true);
            }

            set
            {
                ThrowIfPropertyNotSupported("ParameterSniffingForSecondary");
                DatabaseScopedConfigurations["Parameter_Sniffing"].ValueForSecondary = value.ToString();
            }
        }

        /// <summary>
        /// Get or set the Query_Optimizer_Hotfixes of the database scoped configuration.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public DatabaseScopedConfigurationOnOff QueryOptimizerHotfixes
        {
            get
            {
                ThrowIfPropertyNotSupported("QueryOptimizerHotfixes");
                return (DatabaseScopedConfigurationOnOff)Enum.Parse(typeof(DatabaseScopedConfigurationOnOff),
                                                                    DatabaseScopedConfigurations["Query_Optimizer_Hotfixes"].Value,
                                                                    ignoreCase: true);
            }

            set
            {
                ThrowIfPropertyNotSupported("QueryOptimizerHotfixes");
                DatabaseScopedConfigurations["Query_Optimizer_Hotfixes"].Value = value.ToString();
            }
        }

        /// <summary>
        /// Get or set the Query_Optimizer_Hotfixes for secondary of the database scoped configuration.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public DatabaseScopedConfigurationOnOff QueryOptimizerHotfixesForSecondary
        {
            get
            {
                ThrowIfPropertyNotSupported("QueryOptimizerHotfixesForSecondary");
                return (DatabaseScopedConfigurationOnOff)Enum.Parse(typeof(DatabaseScopedConfigurationOnOff),
                                                                    DatabaseScopedConfigurations["Query_Optimizer_Hotfixes"].ValueForSecondary,
                                                                    ignoreCase: true);
            }

            set
            {
                ThrowIfPropertyNotSupported("QueryOptimizerHotfixesForSecondary");
                DatabaseScopedConfigurations["Query_Optimizer_Hotfixes"].ValueForSecondary = value.ToString();
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != m_ColumnEncryptionKeys)
            {
                m_ColumnEncryptionKeys.MarkAllDropped();
            }

            if (null != m_ColumnMasterKeys)
            {
                m_ColumnMasterKeys.MarkAllDropped();
            }

            if (null != m_LogFiles)
            {
                m_LogFiles.MarkAllDropped();
            }

            if (null != m_FileGroups)
            {
                m_FileGroups.MarkAllDropped();
            }

            if (null != m_Tables)
            {
                m_Tables.MarkAllDropped();
            }

            if (null != m_SensitivityClassifications)
            {
                m_SensitivityClassifications.MarkAllDropped();
            }

            if (null != m_StoredProcedures)
            {
                m_StoredProcedures.MarkAllDropped();
            }

            if (null != m_ExtendedStoredProcedures)
            {
                m_ExtendedStoredProcedures.MarkAllDropped();
            }

            if (null != m_Views)
            {
                m_Views.MarkAllDropped();
            }

            if (null != m_Users)
            {
                m_Users.MarkAllDropped();
            }

            if (null != m_Roles)
            {
                m_Roles.MarkAllDropped();
            }

            if (null != m_Defaults)
            {
                m_Defaults.MarkAllDropped();
            }

            if (null != m_Rules)
            {
                m_Rules.MarkAllDropped();
            }

            if (null != m_UserDefinedDataTypes)
            {
                m_UserDefinedDataTypes.MarkAllDropped();
            }

            if (null != m_UserDefinedTableTypes)
            {
                m_UserDefinedTableTypes.MarkAllDropped();
            }

            if (null != m_PartitionFunctions)
            {
                m_PartitionFunctions.MarkAllDropped();
            }

            if (null != m_PartitionSchemes)
            {
                m_PartitionSchemes.MarkAllDropped();
            }

            if (null != m_SqlAssemblies)
            {
                m_SqlAssemblies.MarkAllDropped();
            }

            if (null != m_UserDefinedTypes)
            {
                m_UserDefinedTypes.MarkAllDropped();
            }

            if (null != m_UserDefinedAggregates)
            {
                m_UserDefinedAggregates.MarkAllDropped();
            }

            if (null != m_FullTextCatalogs)
            {
                m_FullTextCatalogs.MarkAllDropped();
            }

            if (null != m_FullTextStopLists)
            {
                m_FullTextStopLists.MarkAllDropped();
            }

            if (null != m_SearchPropertyLists)
            {
                m_SearchPropertyLists.MarkAllDropped();
            }

            if (null != m_SecurityPolicies)
            {
                m_SecurityPolicies.MarkAllDropped();
            }

            if (null != m_ExternalDataSources)
            {
                m_ExternalDataSources.MarkAllDropped();
            }

            if (null != m_ExternalFileFormats)
            {
                m_ExternalFileFormats.MarkAllDropped();
            }

            if (null != m_XmlSchemaCollections)
            {
                m_XmlSchemaCollections.MarkAllDropped();
            }

            if (null != databaseDdlTriggerCollection)
            {
                databaseDdlTriggerCollection.MarkAllDropped();
            }

            if (null != m_PlanGuides)
            {
                m_PlanGuides.MarkAllDropped();
            }

            if (null != m_DatabaseEncryptionKey)
            {
                m_DatabaseEncryptionKey.MarkDroppedInternal();
            }

            if (null != databaseAuditSpecifications)
            {
                databaseAuditSpecifications.MarkAllDropped();
            }
        }


        private DataTable EnumGeneric(Urn urn)
        {
            return this.ExecutionManager.GetEnumeratorData(new Request(urn));
        }

        /// <summary>
        /// Returns a table of active locks associated with the database
        /// </summary>
        /// <param name="processID">The RequestorSpid for which the table will be filtered.</param>
        /// <returns></returns>
        public DataTable EnumLocks(int processId)
        {
            CheckObjectState();
            return EnumGeneric(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/Lock[@RequestorSpid={0}]", processId));
        }

        /// <summary>
        /// Returns a table of active locks associated with the database
        /// </summary>
        /// <returns></returns>
        public DataTable EnumLocks()
        {
            CheckObjectState();
            return EnumGeneric(this.Urn.Value + "/Lock");
        }

        /// <summary>
        /// Returns a table of database User objects that are mapped to a Login
        /// </summary>
        /// <returns></returns>
        public DataTable EnumLoginMappings()
        {
            CheckObjectState();
            Request req = new Request(this.Urn.Value + "/User[@Login != '']", new string[] { "Name", "Login" });
            req.PropertyAlias = new PropertyAlias(new string[] { "UserName", "LoginName" });
            return this.ExecutionManager.GetEnumeratorData(req);
        }

        /// <summary>
        /// Returns a table of database User objects that are mapped to Windows groups
        /// </summary>
        /// <returns></returns>
        public DataTable EnumWindowsGroups()
        {
            CheckObjectState();
            return EnumGeneric(string.Format(SmoApplication.DefaultCulture, "{0}/User[@LoginType={1}]", this.Urn.Value, ((int)LoginType.WindowsGroup)));
        }

        /// <summary>
        /// EnumWindowsGroups
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public DataTable EnumWindowsGroups(string groupName)
        {
            CheckObjectState();
            if (null == groupName)
            {
                return EnumWindowsGroups();
            }

            return EnumGeneric(string.Format(SmoApplication.DefaultCulture, "{0}/User[@LoginType={2} and @Name='{1}']", this.Urn.Value, Urn.EscapeString(groupName), ((int)LoginType.WindowsGroup)));
        }

        /// <summary>
        /// Invokes DBCC CHECKALLOC with the given repair type
        /// </summary>
        /// <param name="repairType"></param>
        /// <returns></returns>
        public StringCollection CheckAllocations(RepairType repairType)
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.Append("DBCC CHECKALLOC");
                switch (repairType)
                {
                    case RepairType.Fast:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_FAST) ", SqlString(Name));
                        break;
                    case RepairType.Rebuild:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_REBUILD) ", SqlString(Name));
                        break;
                    case RepairType.AllowDataLoss:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_ALLOW_DATA_LOSS) ", SqlString(Name));
                        break;
                    case RepairType.None:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}') ", SqlString(Name));
                        break;
                }

                sb.Append(" WITH NO_INFOMSGS");
                queries.Add(sb.ToString());

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckAllocations, this, e);
            }
        }

        /// <summary>
        /// Wrapper for DBCC CHECKALLOC NOINDEX
        /// </summary>
        /// <returns></returns>
        public StringCollection CheckAllocationsDataOnly()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKALLOC(N'{0}', NOINDEX)", SqlString(Name)));

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckAllocations, this, e);
            }
        }


        /// <summary>
        /// Wrapper for DBCC CHECKCATALOG
        /// </summary>
        /// <returns></returns>
        public StringCollection CheckCatalog()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add($"DBCC CHECKCATALOG({MakeSqlBraket(Name)})");

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckCatalog, this, e);
            }
        }

        /// <summary>
        /// Invokes DBCC CHECKDB with the given repair type and NO_INFOMSGS
        /// </summary>
        /// <param name="repairType"></param>
        /// <returns></returns>
        public StringCollection CheckTables(RepairType repairType)
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.Append("DBCC CHECKDB");

                switch (repairType)
                {
                    case RepairType.Fast:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_FAST) ", SqlString(Name));
                        break;
                    case RepairType.Rebuild:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_REBUILD) ", SqlString(Name));
                        break;
                    case RepairType.AllowDataLoss:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_ALLOW_DATA_LOSS) ", SqlString(Name));
                        break;
                    case RepairType.None:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}') ", SqlString(Name));
                        break;
                }

                sb.Append(" WITH NO_INFOMSGS");
                queries.Add(sb.ToString());

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckTables, this, e);
            }
        }

        //Supported T-SQL
        //DBCC CHECKDB
        //    [
        //        [ ( database_name | database_id | 0
        //            [ , NOINDEX
        //            | , { REPAIR_ALLOW_DATA_LOSS | REPAIR_FAST | REPAIR_REBUILD } ]
        //            ) ]
        //        [ WITH
        //            {
        //                [ ALL_ERRORMSGS ]
        //                [ , EXTENDED_LOGICAL_CHECKS ]
        //                [ , NO_INFOMSGS ]
        //                [ , TABLOCK ]
        //                [ , ESTIMATEONLY ]
        //                [ , MAXDOP = <dop> ]
        //                [ , { PHYSICAL_ONLY | DATA_PURITY } ]
        //            }
        //        ]
        //    ]

        /// <summary>
        /// Invokes DBCC CHECKDB with the given options
        /// </summary>
        /// <param name="repairType"></param>
        /// <param name="repairOptions"></param>
        /// <param name="repairStructure"></param>
        /// <param name="maxDOP"></param>
        /// <returns>Messages returned by DBCC CHECKDB</returns>
        public StringCollection CheckTables(RepairType repairType, RepairOptions repairOptions, RepairStructure repairStructure, long? maxDOP = null)
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.Append("DBCC CHECKDB");

                switch (repairType)
                {
                    case RepairType.Fast:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_FAST) ", SqlString(Name));
                        break;
                    case RepairType.Rebuild:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_REBUILD) ", SqlString(Name));
                        break;
                    case RepairType.AllowDataLoss:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}', REPAIR_ALLOW_DATA_LOSS) ", SqlString(Name));
                        break;
                    case RepairType.None:
                        sb.AppendFormat(SmoApplication.DefaultCulture, "(N'{0}') ", SqlString(Name));
                        break;
                }

                string withClause = GenerateRepairOptionsScript(repairOptions, repairStructure, maxDOP);
                if (withClause.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " WITH {0} ", withClause);
                }
                else
                {
                    sb.Append(" WITH NO_INFOMSGS");
                }

                queries.Add(sb.ToString());

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckTables, this, e);
            }
        }

        /// <summary>
        /// Invokes DBCC CHECKDB with the given repair type and repair structure
        /// </summary>
        /// <param name="repairType"></param>
        /// <param name="repairStructure"></param>
        /// <returns>Messages returned by DBCC CHECKDB</returns>
        public StringCollection CheckTables(RepairType repairType, RepairStructure repairStructure)
        {
            return CheckTables(repairType, RepairOptions.None, repairStructure);
        }

        /// <summary>
        /// Invokes DBCC CHECKDB with the given repair type and repair options
        /// </summary>
        /// <param name="repairType"></param>
        /// <param name="repairOptions"></param>
        /// <returns>Messages returned by DBCC CHECKDB</returns>
        public StringCollection CheckTables(RepairType repairType, RepairOptions repairOptions)
        {
            return CheckTables(repairType, repairOptions, RepairStructure.None);
        }

        /// <summary>
        /// Invokes DBCC CHECKDB with the NOINDEX option
        /// </summary>
        /// <returns>Messages generated by DBCC CHECKDB</returns>
        public StringCollection CheckTablesDataOnly()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKDB(N'{0}', NOINDEX)", SqlString(Name)));

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckTables, this, e);
            }
        }

        /// <summary>
        /// Invokes DBCC CHECKDB with the NO_INDEX option and the given repair options
        /// </summary>
        /// <param name="repairOptions"></param>
        /// <returns>Messages generated by DBCC CHECKDB</returns>
        public StringCollection CheckTablesDataOnly(RepairOptions repairOptions)
        {
            return CheckTablesDataOnly(repairOptions, RepairStructure.None);
        }

        /// <summary>
        /// Invokes DBCC CHECKDB with the NO_INDEX option and the given repair structure
        /// </summary>
        /// <param name="repairStructure"></param>
        /// <returns>Messages generated by DBCC CHECKDB</returns>
        public StringCollection CheckTablesDataOnly(RepairStructure repairStructure)
        {
            return CheckTablesDataOnly(RepairOptions.None, repairStructure);
        }

        /// <summary>
        /// Invokes DBCC CHECKDB with the NO_INDEX option and the given repair options and repair structure.
        /// </summary>
        /// <param name="repairOptions"></param>
        /// <param name="repairStructure"></param>
        /// <param name="maxDOP">Degree of parallelism to allow for the check. Only valid for sql version >= 13</param>
        /// <returns>Messages generated by DBCC CHECKDB</returns>
        public StringCollection CheckTablesDataOnly(RepairOptions repairOptions, RepairStructure repairStructure, long? maxDOP = null)
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.AppendFormat(SmoApplication.DefaultCulture, "DBCC CHECKDB(N'{0}', NOINDEX)", SqlString(Name));

                string withClause = GenerateRepairOptionsScript(repairOptions, repairStructure, maxDOP);
                if (withClause.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " WITH {0} ", withClause);
                }
                else
                {
                    sb.Append(" WITH NO_INFOMSGS");
                }
                queries.Add(sb.ToString());
                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckTables, this, e);
            }
        }

        private string GenerateRepairOptionsScript(RepairOptions repairOptions, RepairStructure repairStructure, long? maxDOP = null)
        {
            StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if ((repairOptions & RepairOptions.AllErrorMessages) == RepairOptions.AllErrorMessages)
            {
                withClause.AppendFormat(SmoApplication.DefaultCulture, " ALL_ERRORMSGS ");
                withClause.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
            }
            if ((repairOptions & RepairOptions.ExtendedLogicalChecks) == RepairOptions.ExtendedLogicalChecks)
            {
                withClause.AppendFormat(SmoApplication.DefaultCulture, " EXTENDED_LOGICAL_CHECKS ");
                withClause.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
            }
            if ((repairOptions & RepairOptions.NoInformationMessages) == RepairOptions.NoInformationMessages)
            {
                withClause.AppendFormat(SmoApplication.DefaultCulture, " NO_INFOMSGS ");
                withClause.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
            }
            if ((repairOptions & RepairOptions.TableLock) == RepairOptions.TableLock)
            {
                withClause.AppendFormat(SmoApplication.DefaultCulture, " TABLOCK ");
                withClause.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
            }
            if ((repairOptions & RepairOptions.EstimateOnly) == RepairOptions.EstimateOnly)
            {
                withClause.AppendFormat(SmoApplication.DefaultCulture, " ESTIMATEONLY ");
                withClause.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
            }
            if (maxDOP.HasValue)
            {
                // MAXDOP is an unsupported feature in prior versions
                //
                ThrowIfBelowVersion130Prop("MAXDOP");

                withClause.AppendFormat(SmoApplication.DefaultCulture, " MAXDOP = {0} ", maxDOP.Value);
                withClause.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
            }

            switch (repairStructure)
            {
                case RepairStructure.PhysicalOnly:
                    withClause.AppendFormat(SmoApplication.DefaultCulture, " PHYSICAL_ONLY ");
                    break;
                case RepairStructure.DataPurity:
                    withClause.AppendFormat(SmoApplication.DefaultCulture, " DATA_PURITY ");
                    break;
                default:
                    if (withClause.ToString().EndsWith(Globals.comma))
                    {
                        withClause.Remove(withClause.Length - 1, 1);
                    }
                    break;
            }

            return withClause.ToString();
        }

        ///<summary>
        /// Shrinks a database
        ///</summary>
        public void Shrink(int percentFreeSpace, ShrinkMethod shrinkMethod)
        {
            try
            {
                CheckObjectState();
                StringCollection query = new StringCollection();
                AddUseDb(query);

                StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                string shrinkMethodStr = string.Empty;
                switch (shrinkMethod)
                {
                    case ShrinkMethod.Default:
                        shrinkMethodStr = " )";
                        break;
                    case ShrinkMethod.NoTruncate:
                        shrinkMethodStr = ", NOTRUNCATE)";
                        break;
                    case ShrinkMethod.TruncateOnly:
                        shrinkMethodStr = ", TRUNCATEONLY)";
                        break;
                    case ShrinkMethod.EmptyFile:
                        // throw invalid parameter
                        throw new SmoException(ExceptionTemplates.InnerException, new ArgumentException(ExceptionTemplates.InvalidShrinkMethod(ShrinkMethod.EmptyFile.ToString())));
                    default:
                        throw new SmoException(ExceptionTemplates.InnerException, new ArgumentException(ExceptionTemplates.UnknownShrinkType));
                }

                if (percentFreeSpace <= 0)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, "DBCC SHRINKDATABASE(N'{0}'{1}",
                                           SqlString(Name), shrinkMethodStr);
                }
                else
                {
                    // do a correction if input is wrong
                    if (percentFreeSpace > 100)
                    {
                        percentFreeSpace = 100;
                    }
                    statement.AppendFormat(SmoApplication.DefaultCulture, "DBCC SHRINKDATABASE(N'{0}', {1}{2}",
                                           SqlString(Name), percentFreeSpace, shrinkMethodStr);
                }
                query.Add(statement.ToString());

                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Shrink, this, e);
            }
        }

        public void RecalculateSpaceUsage()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(Name)));
                queries.Add("DBCC UPDATEUSAGE(0) WITH NO_INFOMSGS");

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RecalculateSpaceUsage, e);
            }
        }

        /// <summary>
        /// Initializes all the objects in the database
        /// </summary>
        public void PrefetchObjects()
        {
            ScriptingPreferences sp = new ScriptingPreferences();
            sp.IncludeScripts.ExtendedProperties = true;
            PrefetchTables(sp);
            PrefetchViews(sp);
            PrefetchPartitionSchemes(sp);
            PrefetchPartitionFunctions(sp);
            PrefetchOtherObjects(sp);
            PrefetchScriptingOnlyChildren(sp);
        }

        /// <summary>
        /// Initializes all the objects of type t and their children
        /// </summary>
        /// <param name="t"></param>
        public void PrefetchObjects(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }
            ScriptingPreferences sp = new ScriptingPreferences();
            sp.IncludeScripts.ExtendedProperties = true;
            PrefetchObjectsImpl(objectType, sp);
        }

        /// <summary>
        /// Initializes all the objects of type t and their children needed to script
        /// </summary>
        /// <param name="t"></param>
        /// <param name="so"></param>
        internal void PrefetchObjects(Type objectType, ScriptingPreferences scriptingPreferences)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }
            if (scriptingPreferences == null)
            {
                throw new ArgumentNullException(nameof(scriptingPreferences));
            }
            ScriptingPreferences sp = (ScriptingPreferences)scriptingPreferences.Clone();
            sp.IncludeScripts.ExtendedProperties = true;
            PrefetchObjectsImpl(objectType, sp);
        }

        /// <summary>
        /// Initializes all the objects of type t and their children needed to script
        /// with so
        /// </summary>
        /// <param name="t"></param>
        /// <param name="so"></param>
        public void PrefetchObjects(Type objectType, ScriptingOptions scriptingOptions)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }
            if (scriptingOptions == null)
            {
                throw new ArgumentNullException(nameof(scriptingOptions));
            }
            PrefetchObjects(objectType, scriptingOptions.GetScriptingPreferences());
        }

        private void PrefetchObjectsImpl(Type objectType, ScriptingPreferences scriptingPreferences)
        {
            try
            {

                CheckObjectState();

                // if the object does not exist, there's nothing to prefetch
                if (State == SqlSmoState.Creating)
                {
                    return;
                }

                if (objectType.Equals(typeof(Table)))
                {
                    PrefetchTables(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(View)))
                {
                    PrefetchViews(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(StoredProcedure)))
                {
                    PrefetchStoredProcedures(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(User)))
                {
                    PrefetchUsers(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(Default)))
                {
                    PrefetchDefaults(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(Rule)))
                {
                    PrefetchRules(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(UserDefinedFunction)))
                {
                    PrefetchUserDefinedFunctions(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(ExtendedStoredProcedure)))
                {
                    PrefetchExtendedStoredProcedures(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(UserDefinedType)))
                {
                    PrefetchUserDefinedTypes(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(UserDefinedTableType)))
                {
                    PrefetchUserDefinedTableTypes(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(UserDefinedAggregate)))
                {
                    PrefetchUserDefinedAggregates(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(PartitionScheme)))
                {
                    PrefetchPartitionSchemes(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(PartitionFunction)))
                {
                    PrefetchPartitionFunctions(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(XmlSchemaCollection)))
                {
                    PrefetchXmlSchemaCollections(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(SqlAssembly)))
                {
                    PrefetchSqlAssemblies(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(Schema)))
                {
                    PrefetchSchemas(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(DatabaseRole)))
                {
                    PrefetchDatabaseRoles(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(UserDefinedDataType)))
                {
                    PrefetchUDDT(scriptingPreferences);
                }
                else if (objectType.Equals(typeof(Sequence)))
                {
                    PrefetchSequences(scriptingPreferences);
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.InvalidType(objectType.FullName));
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.PrefetchObjects, this, e);
            }

        }

        internal void PrefetchStoredProcedures(ScriptingPreferences options)
        {
            InitChildLevel("StoredProcedure", options, true);
            InitChildLevel("StoredProcedure[@IsSystemObject=false()]/Param", options, true);

            //init numbered stored procedures. we don't get params for them as there will very rarely be any present
            if (this.IsSupportedObject<NumberedStoredProcedure>(options))
            {
                InitChildLevel("StoredProcedure/Numbered", options, true);
            }

            if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
            {
                InitChildLevel("StoredProcedure/ExtendedProperty", options, true);
                InitChildLevel("StoredProcedure[@IsSystemObject=false()]/Param/ExtendedProperty", options, true);
            }

            if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
            {
                InitChildLevel("StoredProcedure/Permission", options, true);
            }
        }

        internal void PrefetchUsers(ScriptingPreferences options)
        {
            InitChildLevel("User", options, true);

            if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
            {
                InitChildLevel("User/ExtendedProperty", options, true);
            }

            if (options.IncludeScripts.Permissions && ServerVersion.Major > 8 && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
            {
                InitChildLevel("User/Permission", options, true);
            }
        }

        /// <summary>
        /// Prefetch the child objects that database scripts itself during Create or Alter
        /// </summary>
        /// <param name="options"></param>
        internal void PrefetchScriptingOnlyChildren(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<DatabaseScopedConfiguration>(options))
            {
                InitChildLevel("DatabaseScopedConfiguration", options, true);
            }
        }

        internal void PrefetchDatabaseRoles(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<DatabaseRole>(options))
            {
                InitChildLevel("Role", options, true);

                if (options.IncludeScripts.Permissions && ServerVersion.Major > 8 && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("Role/Permission", options, true);
                }
            }
        }

        internal void PrefetchDefaults(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<Default>(options))
            {
                InitChildLevel("Default", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("Default/ExtendedProperty", options, true);
                }
            }
        }

        internal void PrefetchRules(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<Rule>(options))
            {
                InitChildLevel("Rule", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("Rule/ExtendedProperty", options, true);
                }
            }
        }

        internal void PrefetchExternalLanguages(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<ExternalLanguage>(options))
            {
                InitChildLevel("ExternalLanguage", options, true);
                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("ExternalLanguage/ExtendedProperty", options, true);
                }
            }
        }

        internal void PrefetchExternalLibraries(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<ExternalLibrary>(options))
            {
                InitChildLevel("ExternalLibrary", options, true);
                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("ExternalLibrary/ExtendedProperty", options, true);
                }
            }
        }

        internal void PrefetchUserDefinedFunctions(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<UserDefinedFunction>(options))
            {
                InitChildLevel("UserDefinedFunction", options, true);
                InitChildLevel("UserDefinedFunction/Param", options, true);
                InitChildLevel("UserDefinedFunction/Check", options, true);
                InitChildLevel("UserDefinedFunction/Column", options, true);
                InitChildLevel("UserDefinedFunction/Column/Default", options, true);
                InitChildLevel("UserDefinedFunction/Index", options, true);
                InitChildLevel("UserDefinedFunction/Index/IndexedColumn", options, true);


                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("UserDefinedFunction/ExtendedProperty", options, true);
                    InitChildLevel("UserDefinedFunction/Param/ExtendedProperty", options, true);
                }

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("UserDefinedFunction/Permission", options, true);
                    InitChildLevel("UserDefinedFunction/Column/Permission", options, true);
                }
            }
        }

        internal void PrefetchUserDefinedAggregates(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<UserDefinedAggregate>(options))
            {
                InitChildLevel("UserDefinedAggregate", options, true);
                InitChildLevel("UserDefinedAggregate/Param", options, true);


                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("UserDefinedAggregate/ExtendedProperty", options, true);
                    InitChildLevel("UserDefinedAggregate/Param/ExtendedProperty", options, true);
                }

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("UserDefinedAggregate/Permission", options, true);
                }
            }
        }

        internal void PrefetchColumnEncryptionKey(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<ColumnEncryptionKey>(options))
            {
                InitChildLevel($"{nameof(ColumnEncryptionKey)}", options, true);
                InitChildLevel($"{nameof(ColumnEncryptionKey)}/{nameof(ColumnEncryptionKeyValue)}", options, true);
            }
        }


        internal void PrefetchExtendedStoredProcedures(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<ExtendedStoredProcedure>(options))
            {
                InitChildLevel("ExtendedStoredProcedure", options, true);

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("ExtendedStoredProcedure/Permission", options, true);
                }
            }
        }

        internal void PrefetchSequences(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<Sequence>(options))
            {
                InitChildLevel("Sequence", options, true);

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("Sequence/Permission", options, true);
                }

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("Sequence/ExtendedProperty", options, true);
                }

            }
        }

        internal void PrefetchTables(ScriptingPreferences options)
        {
            PrefetchTables(options, "Table");
        }

        internal void PrefetchTables(ScriptingPreferences options, string tableFilter)
        {
            PrefetchUDDT(options);

            PrefetchObjects(options, EnumerateTableFiltersForPrefetch(tableFilter, options));
        }

        internal void PrefetchViews(ScriptingPreferences options)
        {
            PrefetchViews(options, "View");
        }

        internal void PrefetchViews(ScriptingPreferences options, string viewFilter)
        {
            PrefetchObjects(options, EnumerateViewFiltersForPrefetch(viewFilter, options));
        }

        internal void PrefetchSecurityPolicy(ScriptingPreferences options)
        {
            InitChildLevel($"{nameof(SecurityPolicy)}", options, true);

            if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
            {
                InitChildLevel($"{nameof(SecurityPolicy)}/{nameof(ExtendedProperty)}", options, true);
            }
        }

        private void PrefetchObjects(ScriptingPreferences options, IEnumerable<string> filters)
        {
            foreach (string filter in filters)
            {
                InitChildLevel(filter, options, true);
            }
        }

        internal IEnumerable<string> EnumerateTableFiltersForPrefetch(string tableFilter, ScriptingPreferences options)
        {
            yield return tableFilter;
            if (this.IsSupportedObject<Column>(options))
            {
                yield return tableFilter + "/Column";
            }

            if (this.IsSupportedObject<Default>(options))
            {
                yield return tableFilter + "/Column/Default";
            }

            if (this.IsSupportedObject<Check>(options))
            {
                yield return tableFilter + "/Check";
            }

            if (this.IsSupportedObject<EdgeConstraint>(options))
            {
                yield return tableFilter + "/EdgeConstraint";
            }

            if (this.IsSupportedObject<EdgeConstraintClause>(options))
            {
                yield return tableFilter + "/EdgeConstraint/EdgeConstraintClause";
            }

            if (this.IsSupportedObject<ForeignKey>(options))
            {
                yield return tableFilter + "/ForeignKey";
                yield return tableFilter + "/ForeignKey/Column";
            }

            if (this.IsSupportedObject<Trigger>(options))
            {
                yield return tableFilter + "/Trigger";
            }


            if (this.IsSupportedObject<Index>(options))
            {
                yield return tableFilter + "/Index";
                yield return tableFilter + "/Index/IndexedColumn";
            }


            if (this.IsSupportedObject<FullTextIndex>(options))
            {
                yield return tableFilter + "/FullTextIndex";
                yield return tableFilter + "/FullTextIndex/FullTextIndexColumn";
            }

            if (this.IsSupportedObject<Statistic>(options))
            {
                yield return tableFilter + "/Statistic";
                yield return tableFilter + "/Statistic/Column";
            }

            if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
            {
                yield return tableFilter + "/ExtendedProperty";

                if (this.IsSupportedObject<Column>(options))
                {
                    yield return tableFilter + "/Column/ExtendedProperty";
                }

                if (this.IsSupportedObject<Default>(options))
                {
                    yield return tableFilter + "/Column/Default/ExtendedProperty";
                }

                if (this.IsSupportedObject<Check>(options))
                {
                    yield return tableFilter + "/Check/ExtendedProperty";
                }

                if (this.IsSupportedObject<ForeignKey>(options))
                {
                    yield return tableFilter + "/ForeignKey/ExtendedProperty";
                }

                if (this.IsSupportedObject<Trigger>(options))
                {
                    yield return tableFilter + "/Trigger/ExtendedProperty";
                }

                if (this.IsSupportedObject<Index>(options))
                {
                    yield return tableFilter + "/Index/ExtendedProperty";
                }

            }

            if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
            {
                yield return tableFilter + "/Permission";

                if (this.IsSupportedObject<Column>(options))
                {
                    yield return tableFilter + "/Column/Permission";
                }
            }
        }

        internal IEnumerable<string> EnumerateViewFiltersForPrefetch(string viewFilter, ScriptingPreferences options)
        {
            yield return viewFilter;

            if (this.IsSupportedObject<Column>(options))
            {
                yield return viewFilter + "/Column";

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    yield return viewFilter + "/Column/Permission";
                }
            }

            if (this.IsSupportedObject<Trigger>(options))
            {
                yield return viewFilter + "/Trigger";
            }

            if (this.IsSupportedObject<Index>(options))
            {
                yield return viewFilter + "/Index";
                yield return viewFilter + "/Index/IndexedColumn";
            }

            if (this.IsSupportedObject<Statistic>(options))
            {
                yield return viewFilter + "/Statistic";
                yield return viewFilter + "/Statistic/Column";
            }


            if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
            {
                yield return viewFilter + "/ExtendedProperty";

                if (this.IsSupportedObject<Column>(options))
                {
                    yield return viewFilter + "/Column/ExtendedProperty";
                }

                if (this.IsSupportedObject<Trigger>(options))
                {
                    yield return viewFilter + "/Trigger/ExtendedProperty";
                }

                if (this.IsSupportedObject<Index>(options))
                {
                    yield return viewFilter + "/Index/ExtendedProperty";
                }

            }

            if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
            {
                yield return viewFilter + "/Permission";
            }

            if (ServerVersion.Major > 8 && (this.IsSupportedObject<FullTextIndex>(options)))
            {
                yield return viewFilter + "/FullTextIndex";
                yield return viewFilter + "/FullTextIndex/FullTextIndexColumn";
            }
        }

        internal void PrefetchUDDT(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<UserDefinedDataType>(options))
            {
                // also init System Data Types
                this.Parent.SystemDataTypes.GetEnumerator();
                InitChildLevel("UserDefinedDataType", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("UserDefinedDataType/ExtendedProperty", options, true);
                }

                if (options.IncludeScripts.Permissions && ServerVersion.Major > 8 && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("UserDefinedDataType/Permission", options, true);
                }
            }

        }

        internal void PrefetchUserDefinedTableTypes(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<UserDefinedTableType>(options))
            {
                InitChildLevel("UserDefinedTableType", options, true);
                InitChildLevel("UserDefinedTableType/Column", options, true);
                InitChildLevel("UserDefinedTableType/Index", options, true);
                InitChildLevel("UserDefinedTableType/Index/IndexedColumn", options, true);
                InitChildLevel("UserDefinedTableType/Check", options, true);

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("UserDefinedTableType/Permission", options, true);
                }
                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("UserDefinedTableType/ExtendedProperty", options, true);
                }
            }
        }

        internal void PrefetchUserDefinedTypes(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<UserDefinedType>(options))
            {
                InitChildLevel("UserDefinedType", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("UserDefinedType/ExtendedProperty", options, true);
                }

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("UserDefinedType/Permission", options, true);
                }
            }
        }

        internal void PrefetchPartitionSchemes(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<PartitionScheme>(options))
            {
                InitChildLevel("PartitionScheme", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("PartitionScheme/ExtendedProperty", options, true);
                }
            }
        }

        internal void PrefetchPartitionFunctions(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<PartitionFunction>(options))
            {
                InitChildLevel("PartitionFunction", options, true);
                InitChildLevel("PartitionFunction/PartitionFunctionParameter", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("PartitionFunction/ExtendedProperty", options, true);
                }
            }
        }

        internal void PrefetchSchemas(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<Schema>(options))
            {
                InitChildLevel("Schema", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("Schema/ExtendedProperty", options, true);
                }

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("Schema/Permission", options, true);
                }
            }
        }

        internal void PrefetchXmlSchemaCollections(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<XmlSchemaCollection>(options))
            {
                InitChildLevel("XmlSchemaCollection", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("XmlSchemaCollection/ExtendedProperty", options, true);
                }

                if (options.IncludeScripts.Permissions && this.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                {
                    InitChildLevel("XmlSchemaCollection/Permission", options, true);
                }
            }
        }

        internal void PrefetchSqlAssemblies(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<SqlAssembly>(options))
            {
                InitChildLevel("SqlAssembly", options, true);
                InitChildLevel("SqlAssembly[@IsSystemObject=false()]/SqlAssemblyFile", options, true);

                if (options.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(options))
                {
                    InitChildLevel("SqlAssembly/ExtendedProperty", options, true);
                }

                if (options.IncludeScripts.Permissions)
                {
                    InitChildLevel("SqlAssembly/Permission", options, true);
                }
            }
        }

        internal void PrefetchDatabaseScopedCredentials(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<DatabaseScopedCredential>(options))
            {
                InitChildLevel("DatabaseScopedCredential", options, true);
            }
        }

        internal void PrefetchExternalFileFormats(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<ExternalFileFormat>(options))
            {
                InitChildLevel("ExternalFileFormat", options, true);
            }
        }

        internal void PrefetchExternalDataSources(ScriptingPreferences options)
        {
            if (this.IsSupportedObject<ExternalDataSource>(options))
            {
                InitChildLevel("ExternalDataSource", options, true);
            }
        }

        internal void PrefetchOtherObjects(ScriptingPreferences options)
        {
            PrefetchStoredProcedures(options);
            PrefetchUsers(options);
            PrefetchDatabaseRoles(options);
            PrefetchDefaults(options);
            PrefetchRules(options);
            PrefetchUserDefinedFunctions(options);
            PrefetchExtendedStoredProcedures(options);
            PrefetchUserDefinedTypes(options);
            PrefetchExternalLanguages(options);
            PrefetchExternalLibraries(options);
            PrefetchUserDefinedTableTypes(options);
            PrefetchUserDefinedAggregates(options);
            PrefetchXmlSchemaCollections(options);
            PrefetchSqlAssemblies(options);
            PrefetchSchemas(options);
            PrefetchDatabaseScopedCredentials(options);
            PrefetchExternalDataSources(options);
            PrefetchExternalFileFormats(options);
            PrefetchSecurityPolicy(options);
        }

        /// <summary>
        /// Makes sure we have all the properties before attempting to prefetch
        /// anything, because we will need it to build our collections
        /// </summary>
        internal override void PreInitChildLevel()
        {
            // get the string comparer
            InitializeStringComparer();
        }

        /// <summary>
        /// Returns a set of active transactions
        /// </summary>
        /// <returns></returns>
        public DataTable EnumTransactions()
        {
            try
            {
                return EnumTransactions(TransactionTypes.Both);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumTransactions, this, e);
            }
        }

        // for the moment, this function will only return versioned transactions
        // because this is what the server gives us
        /// <summary>
        /// Returns a set of active transactions
        /// </summary>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public DataTable EnumTransactions(TransactionTypes transactionType)
        {
            try
            {
                CheckObjectState();

                if (State == SqlSmoState.Creating)
                {
                    throw new InvalidSmoOperationException("EnumTransactions", State);
                }

                if (ServerVersion.Major < 9)
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }

                return this.ExecutionManager.GetEnumeratorData(new Request("Server/Transaction" + GetTranFilterExpr(transactionType)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumTransactions, this, e);
            }
        }


        /// <summary>
        /// Returns the number of open transactions
        /// </summary>
        /// <returns></returns>
        public Int32 GetTransactionCount()
        {
            try
            {
                return GetTransactionCount(TransactionTypes.Both);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetTransactionCount, this, e);
            }
        }

        /// <summary>
        /// Returns the number of open transactions with with given transaction type
        /// </summary>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public Int32 GetTransactionCount(TransactionTypes transactionType)
        {
            try
            {
                CheckObjectState(true);
                ThrowIfBelowVersion90();

                DataTable dt = EnumTransactions(transactionType);
                return dt.Rows.Count;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetTransactionCount, this, e);
            }
        }

        private string GetTranFilterExpr(TransactionTypes tt)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[@DatabaseID = " + this.ID);

            switch (tt)
            {
                case TransactionTypes.Versioned: sb.Append(" and @IsVersioned = true()");
                    break;
                case TransactionTypes.UnVersioned: sb.Append(" and @IsVersioned = false()");
                    break;
                //case TransactionTypes.Both: do nothing
            }
            sb.Append("]");
            return sb.ToString();
        }

        public void RemoveFullTextCatalogs()
        {
            try
            {
                this.ThrowIfNotSupported(typeof(FullTextCatalog));
                CheckObjectState();

                if (FullTextCatalogs.Count > 0)
                {
                    StringCollection statements = new StringCollection();
                    AddUseDb(statements);

                    ScriptingPreferences sp = new ScriptingPreferences();
                    sp.ScriptForCreateDrop = true;
                    sp.SetTargetServerInfo(this);

                    foreach (FullTextCatalog cat in FullTextCatalogs)
                    {
                        if (cat.State != SqlSmoState.Creating)
                        {
                            cat.ScriptDropInternal(statements, sp);
                        }
                    }

                    this.ExecutionManager.ExecuteNonQuery(statements);

                    this.FullTextCatalogs.Refresh();
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveFullTextCatalogs, this, e);
            }
        }

        public void SetDefaultFullTextCatalog(string catalog)
        {
            try
            {
                ThrowIfBelowVersion90();
                this.ThrowIfNotSupported(typeof(FullTextCatalog));
                CheckObjectState();

                if (null == catalog)
                {
                    throw new ArgumentNullException("catalog");
                }

                if (catalog.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.EmptyInputParam("catalog", "string"));
                }

                StringCollection queries = new StringCollection();
                AddUseDb(queries);
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER FULLTEXT CATALOG [{0}] AS DEFAULT",
                                           SqlBraket(catalog)));
                this.ExecutionManager.ExecuteNonQuery(queries);

                if (!this.ExecutionManager.Recording)
                {
                    Property defcatprop = this.Properties.Get("DefaultFullTextCatalog");
                    defcatprop.SetValue(catalog);
                    defcatprop.SetRetrieved(true);

                    defcatprop = this.FullTextCatalogs[catalog].Properties.Get("IsDefault");
                    defcatprop.SetValue(true);
                    defcatprop.SetRetrieved(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetDefaultFullTextCatalog, this, e);
            }
        }

        /// <summary>
        /// Sets the named file group as the default file group for the database.
        /// </summary>
        /// <param name="fileGroupName"></param>
        public void SetDefaultFileGroup(string fileGroupName)
        {
            if (null == fileGroupName)
            {
                throw new ArgumentNullException("fileGroupName");
            }

            if (fileGroupName.Length == 0)
            {
                throw new ArgumentException(ExceptionTemplates.EmptyInputParam("fileGroupName", "string"));
            }

            try
            {        
                ScriptingPreferences sp = new ScriptingPreferences(this);
                StringCollection cmds = GetDefaultFileGroupScript(sp, fileGroupName);
                this.ExecutionManager.ExecuteNonQuery(cmds);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetDefaultFileGroup, this, e);
            }
        }

        private StringCollection GetDefaultFileGroupScript(ScriptingPreferences sp, string dataSpaceName)
        {
            StringCollection results = new StringCollection();
            if (sp.TargetServerVersionInternal <= SqlServerVersionInternal.Version70)
            {
                throw new UnsupportedVersionException(ExceptionTemplates.SupportedOnlyOn80).SetHelpContext("SupportedOnlyOn80");
            }
            else if (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version80)
            {
                // we check whether the filegroup exists and is already default.  engine throws if
                // we try to make set default on a filegroup that is already default.
                this.AddUseDb(results, sp);
                results.Add(string.Format(SmoApplication.DefaultCulture,
                                           "IF NOT EXISTS (SELECT groupname FROM dbo.sysfilegroups WHERE (status & 0x10) != 0 AND groupname = {2}) ALTER DATABASE {0} MODIFY FILEGROUP [{1}] DEFAULT",
                                           this.FormatFullNameForScripting(sp),
                                           SqlBraket(dataSpaceName),
                                           MakeSqlString(dataSpaceName)));
            }
            else
            {
                // Yukon - we could have other data spaces become default, but there is
                // no DDL support for this yet.
                //
                // we check whether the filegroup exists and is already default.  engine throws if
                // we try to make set default on a filegroup that is already default.
                this.AddUseDb(results, sp);
                results.Add(string.Format(SmoApplication.DefaultCulture,
                                           "IF NOT EXISTS (SELECT name FROM sys.filegroups WHERE is_default=1 AND name = {2}) ALTER DATABASE {0} MODIFY FILEGROUP [{1}] DEFAULT",
                                           this.FormatFullNameForScripting(sp),
                                           SqlBraket(dataSpaceName),
                                           MakeSqlString(dataSpaceName)));
            }

            return results;
        }

        /// <summary>
        /// Sets the named file group as the default file group for the database.
        /// It works for any file group, not just filestream
        /// </summary>
        /// <param name="fileGroupName"></param>
        public void SetDefaultFileStreamFileGroup(string fileGroupName)
        {
            if (null == fileGroupName)
            {
                throw new ArgumentNullException("fileGroupName");
            }

            if (fileGroupName.Length == 0)
            {
                throw new ArgumentException(ExceptionTemplates.EmptyInputParam("fileGroupName", "string"));
            }

            try
            {
                ScriptingPreferences sp = new ScriptingPreferences(this);
                if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version100)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.SupportedOnlyOn100).SetHelpContext("SupportedOnlyOn100");
                }
                else
                {
                    StringCollection cmds = GetDefaultFileGroupScript(sp, fileGroupName);
                    this.ExecutionManager.ExecuteNonQuery(cmds);
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.SetDefaultFileStreamFileGroup, this, e);
            }
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();
            this.masterKeyInitialized = false;
            this.databaseEncryptionKeyInitialized = false;
            this.m_edition = null;
        }

        /// <summary>
        /// Returns information about all the backup sets for the database
        /// </summary>
        /// <returns></returns>
        public DataTable EnumBackupSets()
        {
            Request req = new Request($"Server/BackupSet[@DatabaseName='{Urn.EscapeString(this.Name)}']");
            return this.ExecutionManager.GetEnumeratorData(req);
        }

        /// <summary>
        /// Returns information about the files included in the given backup set
        /// </summary>
        /// <param name="backupSetID">The ID of the backup set. Can be found using the ID column returned by EnumBackupSets</param>
        /// <returns></returns>
        public DataTable EnumBackupSetFiles(int backupSetID)
        {
            Request req = new Request($"Server/BackupSet[@DatabaseName='{Urn.EscapeString(this.Name)}' and @ID='{backupSetID}']/File");
            return this.ExecutionManager.GetEnumeratorData(req);
        }

        /// <summary>
        /// Returns information about all backup set files for the database
        /// </summary>
        /// <returns></returns>
        public DataTable EnumBackupSetFiles()
        {
            Request req = new Request($"Server/BackupSet[@DatabaseName='{Urn.EscapeString(this.Name)}']/File");
            return this.ExecutionManager.GetEnumeratorData(req);
        }

        /// <summary>
        /// The EnumCandidateKeys method returns a QueryResults object that enumerates the user tables of a Microsoft® SQL Server™ 2000 database and the constraints on those tables that could define primary keys.
        /// </summary>
        /// <returns></returns>

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.Data.DataColumn.set_Caption(System.String)")]
        public DataTable EnumCandidateKeys()
        {
            try
            {
                CheckObjectState();
                Request req = new Request(this.Urn.Value + "/Table/Index[@IndexKeyType > 0]", new string[] { "Name" });
                req.ParentPropertiesRequests = new PropertiesRequest[] { new PropertiesRequest(new string[] { "Name" }) };

                DataTable dt = this.ExecutionManager.GetEnumeratorData(req);
                dt.Columns[0].Caption = "candidate_table"; //SQL Server table name
                dt.Columns[1].Caption = "candidate_key"; //Name of an existing UNIQUE or PRIMARY KEY constraint

                return dt;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumCandidateKeys, this, e);
            }
        }

        /// <summary>
        /// Update statistics for all indexes in the database.
        /// </summary>
        /// <returns></returns>
        public void UpdateIndexStatistics()
        {
            try
            {
                if (this.Name == "tempdb")
                {
                    throw new FailedOperationException(ExceptionTemplates.FailedOperationExceptionText3("UpdateIndexStatistics", ExceptionTemplates.Database, "tempdb", ExceptionTemplates.FailedOperationMessageNotSupportedTempdb));
                }

                CheckObjectState();

                StringCollection queries = new StringCollection();

                StringBuilder sb = new StringBuilder(256);
                //We need to set options with every table scripting
                sb.AppendLine("SET ARITHABORT ON");
                sb.AppendLine("SET CONCAT_NULL_YIELDS_NULL ON");
                sb.AppendLine("SET QUOTED_IDENTIFIER ON");
                sb.AppendLine("SET ANSI_NULLS ON");
                sb.AppendLine("SET ANSI_PADDING ON");
                sb.AppendLine("SET ANSI_WARNINGS ON");
                sb.AppendLine("SET NUMERIC_ROUNDABORT OFF");
                queries.Add(sb.ToString());

                Server srv = GetServerObject();
                StringCollection sc = srv.GetDefaultInitFields(typeof(Table), this.DatabaseEngineEdition);
                try
                {
                    // optimization: make IsSystemObject available by default when tables are retrieved
                    StringCollection fields = srv.GetDefaultInitFields(typeof(Table), this.DatabaseEngineEdition);
                    fields.Add("IsSystemObject");
                    srv.SetDefaultInitFields(typeof(Table), fields, this.DatabaseEngineEdition);

                    foreach (Table table in this.Tables)
                    {
                        // 9.0+ servers: update statistics for all tables
                        // otherwise: update statistics for all non-system tables
                        if ((this.ServerVersion.Major > 8) || (table.IsSystemObject == false))
                        {
                            //
                            // build the update script for tables
                            //
                            string update_cmd = String.Format(SmoApplication.DefaultCulture, "UPDATE STATISTICS {0}.{1}",
                                                         MakeSqlBraket(table.Schema), MakeSqlBraket(table.Name));
                            queries.Add(update_cmd);
                        }
                    }
                }
                finally
                {
                    srv.SetDefaultInitFields(typeof(Table), sc, this.DatabaseEngineEdition);
                }

                //
                // if the server version is bigger that 7.0 there are also statistics on views
                //
                if (this.ServerVersion.Major > 7)
                {
                    // find out the list of views that have statistics
                    Request req = new Request();
                    req.Urn = this.Urn + "/View/Statistic[@IsFromIndexCreation = true()]";
                    req.Fields = new String[] { "ID" }; //dummy request ID from the statisitics level
                    req.ParentPropertiesRequests = new PropertiesRequest[] { new PropertiesRequest() };
                    // request Schema and Name from the View level
                    req.ParentPropertiesRequests[0].Fields = new String[] { "Schema", "Name" };
                    req.ParentPropertiesRequests[0].OrderByList = new OrderBy[] {
                        new OrderBy("Schema", OrderBy.Direction.Asc), new OrderBy("Name", OrderBy.Direction.Asc)
                        };

                    // execute enumerator request
                    DataTable dt = this.ExecutionManager.GetEnumeratorData(req);

                    if (dt.Rows.Count > 0)
                    {
                        //assert that we have schema on the first column and name on second
                        Diagnostics.TraceHelper.Assert("View_Schema" == dt.Columns[0].Caption);
                        Diagnostics.TraceHelper.Assert("View_Name" == dt.Columns[1].Caption);

                        // iterate through the list of views, it may contain duplicates
                        string schema = string.Empty;
                        string name = string.Empty;
                        foreach (DataRow row in dt.Rows)
                        {
                            if (schema == (string)row[0] && name == (string)row[1])
                            {
                                // duplicate row, skip it
                                continue;
                            }

                            schema = (string)row[0];
                            name = (string)row[1];

                            // generate ddl
                            queries.Add(String.Format(SmoApplication.DefaultCulture, "UPDATE STATISTICS {0}.{1}",
                                                    MakeSqlBraket(schema), MakeSqlBraket(name)));
                        }
                    }
                }

                this.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.UpdateIndexStatistics, this, e);
            }
        }

        /// <summary>
        /// The EnumMatchingSPs method returns a QueryResults object that enumerates the stored procedures that contain the specified search text
        /// </summary>
        /// <returns></returns>
        /// <param name="description">The text for which to search</param>
        /// <param name="includeSystem">Whether to include system stored procedures in the search. Default is false.</param>
        public UrnCollection EnumMatchingSPs(string description, bool includeSystem)
        {
            if (null == description)
            {
                throw new ArgumentNullException("description");
            }

            try
            {
                CheckObjectState();

                Request req = new Request(this.Urn.Value + "/StoredProcedure" + (includeSystem ? "" : "[@IsSystemObject = false()]") + "/Text[contains(@Text, '" + Urn.EscapeString(description) + "')]", new string[] { });

                req.ParentPropertiesRequests = new PropertiesRequest[] { new PropertiesRequest(new string[] { "Urn" }) };
                DataTable dt = this.ExecutionManager.GetEnumeratorData(req);

                UrnCollection urns = new UrnCollection();
                foreach (DataRow row in dt.Rows)
                {
                    Urn u = new Urn((string)row[0]);
                    if (!urns.Contains(u))
                    {
                        urns.Add(u);
                    }
                }
                return urns;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumMatchingSPs, this, e);
            }
        }

        /// <summary>
        /// The EnumMatchingSPs method returns a QueryResults object that enumerates the stored procedures that contain the specified search text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public UrnCollection EnumMatchingSPs(string description)
        {
            return EnumMatchingSPs(description, false);
        }

        /// <summary>
        /// Supported EngineTypeCollections
        /// </summary>
        internal enum SupportedEngineType
        {
            Standalone = 1
        }

        private class UrnInfo
        {
            public string UrnType;
            public bool HasSchema;
            public bool HasName;
            public DatabaseObjectTypes DatabaseObjectTypes;
            public int VersionMajor;
            public int VersionMinor;
            public int SupportedEngineTypes = (int)(SupportedEngineType.Standalone);



            public UrnInfo(string urnType, bool hasSchema, bool hasName, DatabaseObjectTypes databaseObjectType, int versionMajor)
            {
                UrnType = urnType;
                HasSchema = hasSchema;
                HasName = hasName;
                this.DatabaseObjectTypes = databaseObjectType;
                VersionMajor = versionMajor;
            }

            public UrnInfo(string urnType, bool hasSchema, bool hasName,int supportedEngineTypes, DatabaseObjectTypes databaseObjectType, int versionMajor)
            {
                UrnType = urnType;
                HasSchema = hasSchema;
                HasName = hasName;
                this.DatabaseObjectTypes = databaseObjectType;
                VersionMajor = versionMajor;
                this.SupportedEngineTypes = supportedEngineTypes;
            }

            public UrnInfo(string urnType, bool hasSchema, bool hasName, DatabaseObjectTypes databaseObjectType, int versionMajor, int versionMinor):this(urnType, hasSchema, hasName, databaseObjectType, versionMajor)
            {
                VersionMinor = versionMinor;
            }
        }

        /// <summary>
        /// The EnumObjects method returns a DataTable that enumerates the system and user-defined objects defining the database referenced.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public DataTable EnumObjects(DatabaseObjectTypes types, SortOrder order)
        {
            try
            {
SortedList list = new SortedList();

                list[DatabaseObjectTypes.ApplicationRole] = new UrnInfo("ApplicationRole", false, true, DatabaseObjectTypes.ApplicationRole, 7);
                list[DatabaseObjectTypes.ServiceBroker] = new UrnInfo("ServiceBroker", false, false, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.ServiceBroker, 9);
                list[DatabaseObjectTypes.Default] = new UrnInfo("Default", false, true, DatabaseObjectTypes.Default, 7);
                list[DatabaseObjectTypes.ExtendedStoredProcedure] = new UrnInfo("ExtendedStoredProcedure", true, true, DatabaseObjectTypes.ExtendedStoredProcedure, 7);
                list[DatabaseObjectTypes.FullTextCatalog] = new UrnInfo("FullTextCatalog", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.FullTextCatalog, 7);
                list[DatabaseObjectTypes.MessageType] = new UrnInfo("ServiceBroker/MessageType", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.MessageType, 9);
                list[DatabaseObjectTypes.PartitionFunction] = new UrnInfo("PartitionFunction", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.PartitionFunction, 9);
                list[DatabaseObjectTypes.PartitionScheme] = new UrnInfo("PartitionScheme", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.PartitionScheme, 9);
                list[DatabaseObjectTypes.DatabaseRole] = new UrnInfo("Role", false, true, DatabaseObjectTypes.DatabaseRole, 7);
                list[DatabaseObjectTypes.RemoteServiceBinding] = new UrnInfo("ServiceBroker/RemoteServiceBinding", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.RemoteServiceBinding, 9);
                list[DatabaseObjectTypes.Rule] = new UrnInfo("Rule", false, true, DatabaseObjectTypes.Rule, 7);
                list[DatabaseObjectTypes.Schema] = new UrnInfo("Schema", false, true, DatabaseObjectTypes.Schema, 9);
                list[DatabaseObjectTypes.ServiceContract] = new UrnInfo("ServiceBroker/ServiceContract", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.ServiceContract, 9);
                list[DatabaseObjectTypes.ServiceQueue] = new UrnInfo("ServiceBroker/ServiceQueue", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.ServiceQueue, 9);
                list[DatabaseObjectTypes.ServiceRoute] = new UrnInfo("ServiceBroker/ServiceRoute", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.ServiceRoute, 9);
                list[DatabaseObjectTypes.SqlAssembly] = new UrnInfo("SqlAssembly", false, true, DatabaseObjectTypes.SqlAssembly, 9);
                list[DatabaseObjectTypes.StoredProcedure] = new UrnInfo("StoredProcedure", true, true, DatabaseObjectTypes.StoredProcedure, 7);
                list[DatabaseObjectTypes.Synonym] = new UrnInfo("Synonym", true, true, DatabaseObjectTypes.Synonym, 9);
                list[DatabaseObjectTypes.Sequence] = new UrnInfo("Sequence", false, true, DatabaseObjectTypes.Sequence, 11);
                list[DatabaseObjectTypes.Table] = new UrnInfo("Table", true, true, DatabaseObjectTypes.Table, 7);
                list[DatabaseObjectTypes.User] = new UrnInfo("User", false, true, DatabaseObjectTypes.User, 7);
                list[DatabaseObjectTypes.UserDefinedAggregate] = new UrnInfo("UserDefinedAggregate", true, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.UserDefinedAggregate, 9);
                list[DatabaseObjectTypes.UserDefinedDataType] = new UrnInfo("UserDefinedDataType", false, true, DatabaseObjectTypes.UserDefinedDataType, 7);
                list[DatabaseObjectTypes.UserDefinedTableTypes] = new UrnInfo("UserDefinedTableType", true, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.UserDefinedTableTypes, 9);
                list[DatabaseObjectTypes.UserDefinedFunction] = new UrnInfo("UserDefinedFunction", true, true, DatabaseObjectTypes.UserDefinedFunction, 8);
                list[DatabaseObjectTypes.UserDefinedType] = new UrnInfo("UserDefinedType", false, true, DatabaseObjectTypes.UserDefinedType, 9);
                list[DatabaseObjectTypes.View] = new UrnInfo("View", true, true, DatabaseObjectTypes.View, 7);
                list[DatabaseObjectTypes.XmlSchemaCollection] = new UrnInfo("XmlSchemaCollection", false, true, DatabaseObjectTypes.XmlSchemaCollection, 9);
                list[DatabaseObjectTypes.Certificate] = new UrnInfo("Certificate", false, true, DatabaseObjectTypes.Certificate, 9);
                list[DatabaseObjectTypes.SymmetricKey] = new UrnInfo("SymmetricKey", false, true, DatabaseObjectTypes.SymmetricKey, 9);
                list[DatabaseObjectTypes.AsymmetricKey] = new UrnInfo("AsymmetricKey", false, true, DatabaseObjectTypes.AsymmetricKey, 9);
                list[DatabaseObjectTypes.PlanGuide] = new UrnInfo("PlanGuide", false, true, DatabaseObjectTypes.PlanGuide, 9);
                list[DatabaseObjectTypes.DatabaseEncryptionKey] = new UrnInfo("DatabaseEncryptionKey", false, false, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.DatabaseEncryptionKey, 10);
                list[DatabaseObjectTypes.DatabaseAuditSpecification] = new UrnInfo("DatabaseAuditSpecification", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.DatabaseAuditSpecification, 10);
                list[DatabaseObjectTypes.FullTextStopList] = new UrnInfo("FullTextStopList", false, true, (int)DatabaseEngineType.Standalone, DatabaseObjectTypes.FullTextStopList, 10);
                //list[DatabaseObjectTypes.SearchPropertyList] = new UrnInfo("SearchPropertyList", false, true, DatabaseObjectTypes.SearchPropertyList, 10, 50)
                list[DatabaseObjectTypes.SearchPropertyList] = new UrnInfo("SearchPropertyList", false, true, (int)SupportedEngineType.Standalone, DatabaseObjectTypes.SearchPropertyList, 10);
                list[DatabaseObjectTypes.SecurityPolicy] = new UrnInfo("SecurityPolicy", true, true, DatabaseObjectTypes.SecurityPolicy, 13);
                list[DatabaseObjectTypes.ExternalDataSource] = new UrnInfo("ExternalDataSource", false, true, DatabaseObjectTypes.ExternalDataSource, 13);
                list[DatabaseObjectTypes.ExternalFileFormat] = new UrnInfo("ExternalFileFormat", false, true, DatabaseObjectTypes.ExternalFileFormat, 13);
                list[DatabaseObjectTypes.DatabaseScopedCredential] = new UrnInfo("DatabaseScopedCredential", false, true, DatabaseObjectTypes.DatabaseScopedCredential, 13);
                list[DatabaseObjectTypes.DatabaseScopedConfiguration] = new UrnInfo("DatabaseScopedConfiguration", false, true, DatabaseObjectTypes.DatabaseScopedConfiguration, 13);

                StringCollection queries = new StringCollection();

                string[] fieldsNoname = new string[] { "Urn" };
                string[] fieldsNoschema = new string[] { "Name", "Urn" };
                string[] fieldsSchema = new string[] { "Schema", "Name", "Urn" };

                int verMaj = this.ServerVersion.Major;
                int verMin = this.ServerVersion.Minor;
                SupportedEngineType engineType = SupportedEngineType.Standalone;

                foreach (DatabaseObjectTypes dot in list.Keys)
                {
                    if ((dot & types) == dot)
                    {
                        UrnInfo ui = (UrnInfo)list[dot];
                        if ((ui.VersionMajor > verMaj
                            || ui.VersionMajor == verMaj && ui.VersionMinor > verMin)
                            || ((ui.SupportedEngineTypes & (int)engineType)) != ((int)engineType))
                        {
                            continue;
                        }
                        string[] fields = ui.HasSchema ? fieldsSchema : (ui.HasName ? fieldsNoschema : fieldsNoname);
                        Request req = new Request(this.Urn + "/" + ui.UrnType, fields);
                        req.ResultType = ResultType.Reserved2;

                        ServerInformation si = new ServerInformation(this.ExecutionManager.GetServerVersion(),
                            this.ExecutionManager.GetProductVersion(),
                            this.ExecutionManager.GetDatabaseEngineType(),
                            this.ExecutionManager.GetDatabaseEngineEdition());
                        SqlEnumResult ser = (SqlEnumResult)Enumerator.GetData(si, req);

                        StatementBuilder stm = ser.StatementBuilder;

                        stm.AddProperty("DatabaseObjectTypes",
                            "N'" + dot.ToString() + "'");

                        if (!ui.HasSchema)
                        {
                            stm.AddProperty("Schema", "''");
                        }

                        if (!ui.HasName)
                        {
                            stm.AddProperty("Name", "''");
                        }

                        queries.Add(stm.GetSqlNoPrefixPostfix());
                    }
                }

                StringBuilder finalQuery = new StringBuilder();

                finalQuery.Append("create table #t([DatabaseObjectTypes] nvarchar(100), [Schema] sysname, [Name] sysname, [Urn] nvarchar(2000))\n");

                for (int i = 0; i < queries.Count; i++)
                {
                    finalQuery.Append("\ninsert #t\n");
                    finalQuery.Append(queries[i]);
                }

                //fix urn
                finalQuery.Replace("\n + ", "\n'" + Urn.EscapeString(this.Urn) + "' + ");

                finalQuery.Append("\nselect [DatabaseObjectTypes], [Schema], [Name], [Urn] from #t");

                finalQuery.Append(" ORDER BY ");
                switch (order)
                {
                    case SortOrder.Name: finalQuery.Append("[Name]"); break;
                    case SortOrder.Schema: finalQuery.Append("[Schema]"); break;
                    case SortOrder.Type: finalQuery.Append("[DatabaseObjectTypes]"); break;
                    default: finalQuery.Append("[Urn]"); break;
                }

                finalQuery.Append("\ndrop table #t");

                return this.ExecuteWithResults(finalQuery.ToString()).Tables[0];
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumObjects, this, e);
            }
        }

        public DataTable EnumObjects()
        {
            return EnumObjects(DatabaseObjectTypes.All, SortOrder.Type);
        }

        public DataTable EnumObjects(DatabaseObjectTypes types)
        {
            return EnumObjects(types, SortOrder.Type);
        }

        /// <summary>
        /// Truncate log.  This is supported in SQL Server 2005 for backwards compatibility reasons.
        /// </summary>
        public void TruncateLog()
        {
            if (this.ServerVersion.Major >= 10)
            {
                throw new UnsupportedVersionException(ExceptionTemplates.SupportedOnlyBelow100).SetHelpContext("SupportedOnlyBelow100");
            }

            try
            {
                CheckObjectState();

                string stmt = string.Format(SmoApplication.DefaultCulture, "BACKUP LOG {0} WITH TRUNCATE_ONLY ",
                                            this.FormatFullNameForScripting(new ScriptingPreferences()));
                this.ExecutionManager.ExecuteNonQuery(stmt);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.TruncateLog, this, e);
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
            ServerVersion version,
            DatabaseEngineType databaseEngineType,
            DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            string[] fields =
            {
                nameof(AcceleratedRecoveryEnabled),
                nameof(CatalogCollation),
                nameof(Collation),
                nameof(ContainmentType),
                nameof(DatabaseSnapshotBaseName),
                nameof(DefaultSchema),
                nameof(IsLedger),
                nameof(PersistentVersionStoreFileGroup)
            };
            List<string> list = GetSupportedScriptFields(typeof(PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }

        internal static string[] GetScriptFields2(Type parentType, ServerVersion version,
            DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode, ScriptingPreferences sp)
        {
            string[] fields =
            {
                "CompatibilityLevel",
                "IsMirroringEnabled",
                "IsVarDecimalStorageFormatEnabled",
            };
            List<string> list = GetSupportedScriptFields(typeof(PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }

        /// <summary>
        /// Whether vardecimal compression is supported on the server
        /// </summary>
        public System.Boolean IsVarDecimalStorageFormatSupported
        {
            get
            {
                // vardecimal is supported in SQL Server 2005, SP2 and later, for Enterprise Edition only.
                // vardecimal will be replaced by a different compression feature in Katmai
                Version yukonSp2 = new Version(9, 0, 3003);

                Version thisversion = new Version(
                        this.Parent.ConnectionContext.ServerVersion.Major,
                        this.Parent.ConnectionContext.ServerVersion.Minor,
                        this.Parent.ConnectionContext.ServerVersion.BuildNumber);

                if (this.IsDesignMode)
                {
                    // TODO: edition is ignored for now in design mode.
                    // Synthesis may require specification of the edition.
                    return (thisversion > yukonSp2);
                }

                return (thisversion > yukonSp2) &&
                    (this.Parent.Information.EngineEdition == Edition.EnterpriseOrDeveloper ||
                     this.Parent.Information.EngineEdition == Edition.SqlManagedInstance ||
                     this.Parent.Information.EngineEdition == Edition.SqlAzureArcManagedInstance ||
                     this.Parent.Information.EngineEdition == Edition.SqlDatabaseEdge);
            }
        }

        /// <summary>
        /// Whether vardecimal compression is enabled
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Deploy | SfcPropertyFlags.Standalone)]
        public System.Boolean IsVarDecimalStorageFormatEnabled
        {
            get
            {
                bool result =
                    this.IsVarDecimalStorageFormatSupported &&
                    (System.Boolean)this.Properties.GetValueWithNullReplacement("IsVarDecimalStorageFormatEnabled");

                return result;
            }

            set
            {
                if (!this.IsVarDecimalStorageFormatSupported)
                {
                    throw new PropertyNotAvailableException(
                        ExceptionTemplates.ReasonPropertyIsNotSupportedOnCurrentServerVersion);
                }

                this.Properties.SetValueWithConsistencyCheck("IsVarDecimalStorageFormatEnabled", value);
            }
        }

        /// <summary>
        /// Emit script to set vardecimal storage
        /// </summary>
        /// <param name="query">the sql script that is being created</param>
        /// <param name="so">The scripting options</param>
        /// <param name="forCreate">True if we are generating a create script, false otherwise</param>
        private void ScriptVardecimalCompression(StringCollection query, ScriptingPreferences sp, bool forCreate)
        {
            if (!IsSupportedProperty("IsVarDecimalStorageFormatEnabled", sp))
            {
                return;
            }

            if ((sp.ForDirectExecution || !sp.OldOptions.NoVardecimal) &&
                this.IsVarDecimalStorageFormatSupported)
            {
                Property enableVarDecimal = Properties.Get("IsVarDecimalStorageFormatEnabled");

                // script this when:
                //   1) its value has changed, or
                //   2) we're generating a create script for an existing database and vardecimal is enabled
                if (enableVarDecimal.Dirty ||
                    (forCreate && (this.State == SqlSmoState.Existing) && this.IsVarDecimalStorageFormatEnabled))
                {
                    query.Add(
                        string.Format(
                            SmoApplication.DefaultCulture,
                            "EXEC sys.sp_db_vardecimal_storage_format N'{0}', N'{1}'",
                            Util.EscapeString(this.Name, '\''),
                            (this.IsVarDecimalStorageFormatEnabled ? "ON" : "OFF")));
                }
            }
        }

        /// <summary>
        /// ENABLE all the planguides
        /// </summary>
        public void EnableAllPlanGuides()
        {
            EnableDisableDropAllPlanGuides("ENABLE ALL");
        }

        /// <summary>
        /// DISABLE all the planguides
        /// </summary>
        public void DisableAllPlanGuides()
        {
            EnableDisableDropAllPlanGuides("DISABLE ALL");
        }

        /// <summary>
        /// DROP all the planguides
        /// </summary>
        public void DropAllPlanGuides()
        {
            EnableDisableDropAllPlanGuides("DROP ALL");
        }

        private void EnableDisableDropAllPlanGuides(string action)
        {
            try
            {
                CheckObjectStateImpl(true);
                this.ThrowIfNotSupported(typeof(PlanGuide));
                StringCollection queries = new StringCollection();
                AddDatabaseContext(queries, new ScriptingPreferences(this));
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SP_CONTROLPLANGUIDE,
                                        MakeSqlString(action)));
                this.ExecutionManager.ExecuteNonQuery(queries);
                if (m_PlanGuides != null)
                {
                    m_PlanGuides.Refresh();
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.PlanGuide, this, e);
            }
        }

        /// <summary>
        /// Validate all the planguides
        /// </summary>
        /// <returns></returns>
        public bool ValidateAllPlanGuides()
        {
            DataTable dt;
            return this.ValidateAllPlanGuides(out dt);
        }

        /// <summary>
        /// Validate all the planguides
        /// </summary>
        /// <param name="dt">ou parameter datatable</param>
        /// <returns></returns>
        public bool ValidateAllPlanGuides(out DataTable errorInfo)
        {
            try
            {
                CheckObjectStateImpl(true);
                ThrowIfBelowVersion100();
                StringBuilder statement = new StringBuilder();
                StringCollection queries = new StringCollection();
                AddDatabaseContext(queries, new ScriptingPreferences(this));


                queries.Add(string.Format(SmoApplication.DefaultCulture, "SELECT name, msgnum, severity, state, message FROM sys.plan_guides CROSS APPLY sys.fn_validate_plan_guide(plan_guide_id)"));

                errorInfo = this.ExecutionManager.ExecuteWithResults(queries).Tables[0];

                bool result = false;
                if (errorInfo.Rows.Count == 0)
                {
                    result = true;
                }
                return result;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.PlanGuide, this, e);
            }
        }

        /// <summary>
        /// Reauthorize a stretched database with the SQL Azure server after the database is restored from a backup
        /// </summary>
        /// <param name="credentialName">The name of the credential to use for re-authorization</param>
        /// <param name="withCopy">A boolean flag to indicate if the remote stretched database should be duplicated (true by default)</param>
        public void ReauthorizeRemoteDataArchiveConnection(string credentialName, bool withCopy = true)
        {
            try
            {
                CheckObjectState();
                ThrowIfPropertyNotSupported("RemoteDataArchiveEnabled");

                if (string.IsNullOrEmpty(credentialName))
                {
                    throw new ArgumentNullException("credentialName");
                }

                StringCollection queries = new StringCollection();
                AddDatabaseContext(queries, new ScriptingPreferences(this));
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SP_RDA_REAUTHORIZE_DB, MakeSqlString(credentialName), withCopy ? 1 : 0));
                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReauthorizeRemoteDataArchive, this, e);
            }
        }

        /// <summary>
        /// Gets a list of remote data archive migration status reports
        /// </summary>
        /// <param name="migrationStartTime">Data migration start time</param>
        /// <param name="statusReportCount">Number of reports to be retrieved</param>
        /// <returns>List of remote data archive migration status reports</returns>
        public IEnumerable<RemoteDataArchiveMigrationStatusReport> GetRemoteDataArchiveMigrationStatusReports(DateTime migrationStartTime, int statusReportCount, string tableName = null)
        {
            try
            {
                CheckObjectState();
                ThrowIfPropertyNotSupported("RemoteDataArchiveEnabled");

                StringCollection queries = new StringCollection();

                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(this.Name)));
            
                StatementBuilder sqlStatement = new StatementBuilder();

                sqlStatement.AddFields("dbs.name as database_name");
                sqlStatement.AddFields("tabs.name as table_name");
                sqlStatement.AddFields("rdams.migrated_rows");
                sqlStatement.AddFields("rdams.start_time_utc");
                sqlStatement.AddFields("rdams.end_time_utc");
                sqlStatement.AddFields("rdams.error_number");
                sqlStatement.AddFields("rdams.error_severity");
                sqlStatement.AddFields("rdams.error_state");
                sqlStatement.AddFields("msgs.text as details");
                sqlStatement.TopN = statusReportCount;
                sqlStatement.AddFrom("sys.dm_db_rda_migration_status rdams");
                sqlStatement.AddJoin("INNER JOIN sys.databases dbs ON rdams.database_id = dbs.database_id");
                sqlStatement.AddJoin("INNER JOIN sys.tables tabs ON rdams.table_id = tabs.object_id");
                sqlStatement.AddJoin("LEFT OUTER JOIN sys.messages msgs ON rdams.error_number = msgs.message_id");
                if (string.IsNullOrEmpty(tableName))
                {
                    sqlStatement.AddWhere(string.Format(CultureInfo.InvariantCulture, @"start_time_utc > '{0}'", Urn.EscapeString(migrationStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"))));
                }
                else
                {
                    sqlStatement.AddWhere(string.Format(CultureInfo.InvariantCulture, @"start_time_utc > '{0}' AND tabs.name = '{1}'", Urn.EscapeString(migrationStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff")), Urn.EscapeString(tableName)));
                }

                queries.Add(sqlStatement.SqlStatement);

                // execute the query
                DataSet ds = this.ExecutionManager.ExecuteWithResults(queries);
                IList<RemoteDataArchiveMigrationStatusReport> migrationReports = null;
                if(ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    migrationReports = new List<RemoteDataArchiveMigrationStatusReport>();
                    DataTable resultTable = ds.Tables[0];

                    foreach(DataRow row in resultTable.Rows)
                    {
                        int intVal;
                        int? errorNumber = (row["error_number"] != null) ? (Int32.TryParse(row["error_number"].ToString(), out intVal) ? intVal : (int?)null) : null;
                        int? errorSeverity = (row["error_severity"] != null) ? (Int32.TryParse(row["error_severity"].ToString(), out intVal) ? intVal : (int?)null) : null;
                        int? errorState = (row["error_state"] != null) ? (Int32.TryParse(row["error_state"].ToString(), out intVal) ? intVal : (int?)null) : null;
                        string details = (row["details"] != null) ? row["details"].ToString() : string.Empty;

                        RemoteDataArchiveMigrationStatusReport statusReport = new RemoteDataArchiveMigrationStatusReport((string)row["database_name"], (string)row["table_name"],
                                                                                                                         (long)row["migrated_rows"], (DateTime)row["start_time_utc"],
                                                                                                                         (DateTime)row["end_time_utc"], errorNumber, errorSeverity, errorState, details);
                        migrationReports.Add(statusReport);
                    }
                }

                return migrationReports;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetRemoteDataArchiveMigrationStatusReports, this, e);
            }
        }

        /// <summary>
        /// Get remote database migration statistics. Null if Remote Data Archive is not enabled for database
        /// </summary>
        /// <returns>Database Migration statistics if Remote Data Archive is enabled, else null</returns>
        public RemoteDatabaseMigrationStatistics GetRemoteDatabaseMigrationStatistics()
        {
            try
            {
                CheckObjectState();
                ThrowIfPropertyNotSupported("RemoteDataArchiveEnabled");

                if (this.RemoteDataArchiveEnabled)
                {

                    StringCollection queries = new StringCollection();

                    queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(this.Name)));
                    queries.Add(@"exec sp_spaceused @mode = 'REMOTE_ONLY', @oneresultset = 1");

                    DataSet ds = this.ExecutionManager.ExecuteWithResults(queries);
                    double remoteDatabaseSize = 0;
                    if (ds != null && ds.Tables != null && ds.Tables.Count > 0 && ds.Tables[0].Rows != null && ds.Tables[0].Rows.Count > 0)
                    {
                        DataTable resultTable = ds.Tables[0];
                        string databaseSize = resultTable.Rows[0]["database_size"].ToString();
                        if (databaseSize.ToUpperInvariant().IndexOf("MB") > -1)
                        {
                            string sizeInMb = databaseSize.Substring(0, databaseSize.ToUpperInvariant().IndexOf("MB"));
                            remoteDatabaseSize = Double.Parse(sizeInMb.Trim());
                        }
                    }
                    return new RemoteDatabaseMigrationStatistics(remoteDatabaseSize);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetRemoteDatabaseMigrationStatistics, this, e);
            }
        }

        /// <summary>
        /// Populate the FileGroup collection and each Files collection in each FileGroup optimally.
        /// </summary>
        public void InitFileGroupFiles()
        {
            InitChildLevel("FileGroup", null, false);
            InitChildLevel("FileGroup/File", null, false);
        }

        /// <summary>
        /// Populate the Tables collection and each Columns collection in each Table optimally
        /// </summary>
        public void InitTableColumns()
        {
            InitChildLevel("Table", null, false);
            InitChildLevel("Table/Column", null, false);
        }
        private void ScriptDbOptionsProps(StringCollection query, ScriptingPreferences sp, bool isAzureDb)
        {
            var targetEditionIsManagedServer = 
                ((sp.TargetDatabaseEngineEdition == Cmn.DatabaseEngineEdition.SqlManagedInstance) ||
                 (sp.TargetDatabaseEngineEdition == Cmn.DatabaseEngineEdition.SqlAzureArcManagedInstance));

            ScriptAlterPropBool("AnsiNullDefault", "ANSI_NULL_DEFAULT", sp, query);
            ScriptAlterPropBool("AnsiNullsEnabled", "ANSI_NULLS", sp, query);
            ScriptAlterPropBool("AnsiPaddingEnabled", "ANSI_PADDING", sp, query);
            ScriptAlterPropBool("AnsiWarningsEnabled", "ANSI_WARNINGS", sp, query);
            ScriptAlterPropBool("ArithmeticAbortEnabled", "ARITHABORT", sp, query);
            if (IsSupportedProperty("AutoClose", sp) && !targetEditionIsManagedServer)
            {
                ScriptAlterPropBool("AutoClose", "AUTO_CLOSE", sp, query);
            }
            ScriptAlterPropBool("AutoShrink", "AUTO_SHRINK", sp, query);
            ScriptAutoCreateStatistics(query, sp);
            ScriptAlterPropBool("AutoUpdateStatisticsEnabled", "AUTO_UPDATE_STATISTICS", sp, query);
            ScriptAlterPropBool("CloseCursorsOnCommitEnabled", "CURSOR_CLOSE_ON_COMMIT", sp, query);
            if (IsSupportedProperty("LocalCursorsDefault", sp) && !isAzureDb)
            {
                ScriptAlterPropBool("LocalCursorsDefault", "CURSOR_DEFAULT ", sp, query, "LOCAL", "GLOBAL");
            }
            ScriptAlterPropBool("ConcatenateNullYieldsNull", "CONCAT_NULL_YIELDS_NULL", sp, query);
            ScriptAlterPropBool("NumericRoundAbortEnabled", "NUMERIC_ROUNDABORT", sp, query);
            ScriptAlterPropBool("QuotedIdentifiersEnabled", "QUOTED_IDENTIFIER", sp, query);
            ScriptAlterPropBool("RecursiveTriggersEnabled", "RECURSIVE_TRIGGERS", sp, query);

            //script only if we are on Yukon or target Yukon and later
            if (this.ServerVersion.Major >= 9 &&
                sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                // Don't script for Managed Instances - not supported
                //
                if (IsSupportedProperty("BrokerEnabled", sp) && !targetEditionIsManagedServer)
                {
                    ScriptAlterPropBool("BrokerEnabled", string.Empty, sp, query, "ENABLE_BROKER", "DISABLE_BROKER");
                }
                ScriptAlterPropBool("AutoUpdateStatisticsAsync", "AUTO_UPDATE_STATISTICS_ASYNC", sp, query);

                // trying to enable or disable date_correlation_optimization on Azure also hangs indefinitely
                if (!targetEditionIsManagedServer && !isAzureDb)
                {
                    ScriptAlterPropBool("DateCorrelationOptimization", "DATE_CORRELATION_OPTIMIZATION", sp, query);
                }

                if (!isAzureDb)
                {
                    ScriptAlterPropBool("Trustworthy", "TRUSTWORTHY", sp, query);
                }
                if (IsSupportedProperty("SnapshotIsolationState", sp))
                {
                    ScriptSnapshotIsolationState(sp, query);
                }
                ScriptAlterPropBool("IsParameterizationForced", "PARAMETERIZATION", sp, query, "FORCED", "SIMPLE");
                ScriptAlterPropBool("IsReadCommittedSnapshotOn", "READ_COMMITTED_SNAPSHOT", sp, query);

                if (IsSupportedProperty("MirroringPartner", sp))
                {
                    if (this.GetPropValueOptional("IsMirroringEnabled", false))
                    {
                        Property propMirroringTimeout = Properties.Get("MirroringTimeout");
                        if (null != propMirroringTimeout.Value && (propMirroringTimeout.Dirty || !sp.ForDirectExecution))
                        {
                            query.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET PARTNER TIMEOUT {1} {2}",
                                this.FormatFullNameForScripting(sp),
                                Convert.ToInt32(propMirroringTimeout.Value, SmoApplication.DefaultCulture),
                                null != optionTerminationStatement ? optionTerminationStatement.GetTerminationScript() : ""));
                        }
                    }
                }


            }
            if (IsSupportedProperty("HonorBrokerPriority", sp) && !isAzureDb && !targetEditionIsManagedServer)
            {
                ScriptAlterPropBool("HonorBrokerPriority", "HONOR_BROKER_PRIORITY", sp, query);
            }

            if (IsSupportedProperty(nameof(ReadOnly), sp) && !targetEditionIsManagedServer && (sp.ScriptForAlter || sp.ForDirectExecution))
            {
                Property prop = Properties.Get("ReadOnly");
                if (prop.Value != null)
                {
                    ScriptAlterPropReadonly(query, sp, (bool)prop.Value);
                }
            }

            if (IsSupportedProperty("RecoveryModel", sp) && !targetEditionIsManagedServer)
            {
                Property propRecoveryModel = Properties.Get("RecoveryModel");
                if (null != propRecoveryModel.Value && (propRecoveryModel.Dirty || !sp.ForDirectExecution))
                {
                    RecoveryModel recoveryModel = (RecoveryModel) propRecoveryModel.Value;
                    // we are on 8.0 or bigger, we can call ALTER DATABASE
                    string recoveryModelStr = string.Empty;

                    switch (recoveryModel)
                    {
                        case RecoveryModel.Full:
                            recoveryModelStr = "FULL";
                            break;
                        case RecoveryModel.Simple:
                            recoveryModelStr = "SIMPLE";
                            break;
                        case RecoveryModel.BulkLogged:
                            recoveryModelStr = "BULK_LOGGED";
                            break;
                        default:
                            throw new SmoException(ExceptionTemplates.UnknownRecoveryModel(recoveryModel.ToString()));
                    }


                    ScriptAlterPropBool("RecoveryModel", "RECOVERY", sp, query, recoveryModelStr);
                }
            }

            Property propUserAccess = Properties.Get("UserAccess");
            if (IsSupportedProperty("UserAccess", sp) && null != propUserAccess.Value &&
                (propUserAccess.Dirty || !sp.ForDirectExecution)
                && !targetEditionIsManagedServer)
            {
                DatabaseUserAccess ua = (DatabaseUserAccess)propUserAccess.Value;
                string access = "MULTI_USER";
                switch (ua)
                {
                    case DatabaseUserAccess.Single:
                        access = "SINGLE_USER";
                        break;
                    case DatabaseUserAccess.Restricted:
                        access = "RESTRICTED_USER";
                        break;
                    case DatabaseUserAccess.Multiple:
                        access = "MULTI_USER";
                        break;
                }
                ScriptAlterPropBool("UserAccess", "", sp, query, access);
            }

            if (!targetEditionIsManagedServer)
            {
                ScriptPageVerify(sp, query);
            }

            if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90)//for 8.0
            {
                Property propDbChaining = Properties.Get("DatabaseOwnershipChaining");
                if (null != propDbChaining.Value &&
                    (propDbChaining.Dirty || !sp.ForDirectExecution))
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture,
                        "if ( ((@@microsoftversion / power(2, 24) = 8) and (@@microsoftversion & 0xffff >= 760)) or \n\t\t(@@microsoftversion / power(2, 24) >= 9) )" +
                        "begin \n\texec dbo.sp_dboption @dbname =  {0}, @optname = 'db chaining', @optvalue = '{1}'\n end",
                        this.FormatFullNameForScripting(sp, false),
                        (bool)propDbChaining.Value ? "ON" : "OFF"));
                }
            }
            else if (!isAzureDb)//for 9.0
            {
                ScriptAlterPropBool("DatabaseOwnershipChaining", "DB_CHAINING", sp, query);
            }

            if (this.IsSupportedProperty("ContainmentType", sp) && !targetEditionIsManagedServer)
            {
                ContainmentType cType = this.GetPropValueOptional("ContainmentType", ContainmentType.None);
                if (cType != ContainmentType.None)
                {
                    this.AddDefaultLanguageOption("DefaultFullTextLanguageName", "DefaultFullTextLanguageLcid",
                                                "DEFAULT_FULLTEXT_LANGUAGE", sp, query);

                    this.AddDefaultLanguageOption("DefaultLanguageName", "DefaultLanguageLcid",
                                                "DEFAULT_LANGUAGE", sp, query);

                    ScriptAlterPropBool("NestedTriggersEnabled", "NESTED_TRIGGERS", sp, query, true);
                    ScriptAlterPropBool("TransformNoiseWords", "TRANSFORM_NOISE_WORDS", sp, query, true);

                    ScriptAlterPropBool("TwoDigitYearCutoff",
                                        "TWO_DIGIT_YEAR_CUTOFF",
                                        sp,
                                        query,
                                        Convert.ToString(
                                            this.GetPropValueOptional("TwoDigitYearCutoff"),
                                            SmoApplication.DefaultCulture),
                                        true);
                }
            }
            if (!targetEditionIsManagedServer)
            {
                ScriptAlterFileStreamProp(sp, query);
            }

            if (this.IsSupportedProperty("TargetRecoveryTime", sp) && !targetEditionIsManagedServer)
            {
                object targetRecoveryTime = this.GetPropValueOptionalAllowNull("TargetRecoveryTime");

                if (targetRecoveryTime != null)
                {
                    if ((int)targetRecoveryTime < 0)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.TargetRecoveryTimeNotNegative);
                    }

                    string recoveryTime = Convert.ToString(targetRecoveryTime, SmoApplication.DefaultCulture) + " SECONDS";

                    ScriptAlterPropBool("TargetRecoveryTime",
                                        "TARGET_RECOVERY_TIME",
                                        sp,
                                        query,
                                        recoveryTime,
                                        true);
                }
            }

            if (IsSupportedProperty("DelayedDurability", sp) && !targetEditionIsManagedServer)
            {
                object delayedDurabilityValue = this.GetPropValueOptionalAllowNull("DelayedDurability");

                if (delayedDurabilityValue != null)
                {
                    string delayedDurability = Convert.ToString(delayedDurabilityValue, SmoApplication.DefaultCulture).ToUpperInvariant();

                    ScriptAlterPropBool("DelayedDurability",
                                        "DELAYED_DURABILITY",
                                        sp,
                                        query,
                                        delayedDurability,
                                        true);
                }
            }

            // eg: ALTER DATABASE [MyDatabase] SET ACCELERATED_DATABASE_RECOVERY = ON (PERSISTENT_VERSION_STORE_FILEGROUP = [VersionStoreFG])
            // The filegroup can only be changed if one disables ADR first
            // https://docs.microsoft.com/sql/relational-databases/accelerated-database-recovery-management?view=sql-server-ver15
            if (IsSupportedProperty(nameof(AcceleratedRecoveryEnabled), sp))
            {
                var propAdr = Properties.Get(nameof(AcceleratedRecoveryEnabled));                
                if (null != propAdr.Value &&
                (propAdr.Dirty || !sp.ForDirectExecution))
                {
                    var adrEnable = (bool)propAdr.Value;
                    var filegroupSetting = string.Empty;
                    if (adrEnable && IsSupportedProperty(nameof(PersistentVersionStoreFileGroup), sp))
                    {
                        var pvsProp = GetPropValueOptional(nameof(PersistentVersionStoreFileGroup), string.Empty);
                        if (!string.IsNullOrEmpty(pvsProp))
                        {
                            filegroupSetting = $" (PERSISTENT_VERSION_STORE_FILEGROUP = {MakeSqlBraket(pvsProp)})";
                        }
                    }
                    ScriptAlterPropBool(nameof(AcceleratedRecoveryEnabled),
                        "ACCELERATED_DATABASE_RECOVERY",
                        sp,
                        query,
                        $"{(adrEnable ? Globals.On : Globals.Off)} {filegroupSetting}",
                        useEqualityOperator: true);
                }
            }

            ScriptAlterPropBool(nameof(DataRetentionEnabled), "DATA_RETENTION", sp, query, false);
        }

        private void ScriptAlterFileStreamProp(ScriptingPreferences sp,StringCollection query )
        {
            //FileStream Properties
            if (IsSupportedProperty("FilestreamDirectoryName",sp))
            {
                Property propFsAccess = Properties.Get("FilestreamNonTransactedAccess");
                StringBuilder subScript = new StringBuilder();
                if (propFsAccess.Value != null)
                {
                    FilestreamNonTransactedAccessType accessType = (FilestreamNonTransactedAccessType)propFsAccess.Value;
                    if (propFsAccess.Dirty || !sp.ForDirectExecution)
                    {
                        string accessTypeStr = null;
                        switch (accessType)
                        {
                            case FilestreamNonTransactedAccessType.Off:
                                accessTypeStr = "OFF";
                                break;
                            case FilestreamNonTransactedAccessType.ReadOnly:
                                accessTypeStr = "READ_ONLY";
                                break;
                            case FilestreamNonTransactedAccessType.Full:
                                accessTypeStr = "FULL";
                                break;
                        }
                        subScript.AppendFormat(SmoApplication.DefaultCulture, "NON_TRANSACTED_ACCESS = {0}{1}", accessTypeStr, Globals.commaspace);
                    }
                }

                Property propFsDirName = Properties.Get("FilestreamDirectoryName");
                if ((propFsDirName.Dirty || !sp.ForDirectExecution) && !string.IsNullOrEmpty((string)propFsDirName.Value))
                {
                    subScript.AppendFormat(SmoApplication.DefaultCulture, "DIRECTORY_NAME = {0}{1}",
                        SqlSmoObject.MakeSqlString((string)propFsDirName.Value),Globals.commaspace);
                }

                if (!string.IsNullOrEmpty(subScript.ToString()))
                {
                    subScript.Remove(subScript.ToString().Length - 2, 2); //Remove the extra commaspace
                    query.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET FILESTREAM( {1} ) {2}",
                            this.FormatFullNameForScripting(sp),
                            subScript.ToString(),
                            null != optionTerminationStatement ? optionTerminationStatement.GetTerminationScript() : ""));
                }
            }
        }

        /// <summary>
        /// Takes care of the scripting logic of DefaultLanguage and DefaultFullTextLanguage properties.
        /// </summary>
        /// <param name="nameProperty"></param>
        /// <param name="lcidProperty"></param>
        /// <param name="optname"></param>
        /// <param name="sp"></param>
        /// <param name="query"></param>
        private void AddDefaultLanguageOption(
            string nameProperty,
            string lcidProperty,
            string optname,
            ScriptingPreferences sp,
            StringCollection query)
        {
            string languageName = Convert.ToString(this.GetPropValueOptional(nameProperty), CultureInfo.InvariantCulture);

            //Try scripting default fulltext language lcid only if default fulltext language name is not scripted.
            bool nameScripted = this.GetPropertyOptional(nameProperty).Dirty //Name shall only be scripted if user has changed it otherwise scripting lcid should get the priority.
                                && !string.IsNullOrEmpty(languageName) //string.Empty will be ignored for scripting.
                                && this.ScriptAlterPropBool(nameProperty, optname, sp, query,
                                                            MakeSqlBraket(languageName), true);

            if(!nameScripted)
            {
                int? lcid = this.GetPropValueOptional<int>(lcidProperty);
                //Negative values will be ignored for scripting.
                if (lcid >= 0)
                {
                    this.ScriptAlterPropBool(lcidProperty, optname, sp, query,
                                            Convert.ToString(lcid, CultureInfo.InvariantCulture),
                                            true);
                }
            }
        }

        /// <summary>
        /// The READONLY property had to be factored out so transfer can toggle a destination database that is supposed to be READONLY
        /// to READWRITE then back again to allow actual transfers to take place.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal void ScriptAlterPropReadonly(StringCollection query, ScriptingPreferences sp, bool readonlyMode)
        {

            if (IsSupportedProperty(nameof(ReadOnly)) && IsSupportedProperty(nameof(ReadOnly), sp) &&
                sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse &&
                sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance &&
                sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlAzureArcManagedInstance)
            {
                // Specify READONLY or READWRITE based on the readonlyMode passed in, ignoring alters for dirty-only, etc.
                ScriptAlterPropBool("ReadOnly", "", sp, query, readonlyMode ? "READ_ONLY" : "READ_WRITE");
            }
        }

        internal void Encryption(bool encryptionEnabled)
        {
            this.ThrowIfNotSupported(typeof(DatabaseEncryptionKey));
            StringCollection query = new StringCollection();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE {0}", MakeSqlBraket(this.Name));
            sb.AppendFormat(SmoApplication.DefaultCulture, " SET ENCRYPTION {0}", encryptionEnabled ? "ON" : "OFF");
            query.Add(sb.ToString());
            this.ExecutionManager.ExecuteNonQuery(query);
        }

        /// <summary>
        /// Enables or disables encryption
        /// </summary>
        /// <param name="isEnabled">If true, enables encryption. If false, disables encryption.</param>
        public void EnableEncryption(bool isEnabled)
        {
            Encryption(isEnabled);
        }

        PageVerify GetPageVerify(ScriptingPreferences sp)
        {
            return (PageVerify)GetPropValueOptional("PageVerify", PageVerify.None);
        }


        void ScriptAlterContainmentDDL(ScriptingPreferences sp, StringCollection queries)
        {
            if (this.IsSupportedProperty("ContainmentType", sp))
            {
                ContainmentType cType = this.GetPropValueOptional("ContainmentType", ContainmentType.None);
                switch (cType)
                {
                    case ContainmentType.None:
                        ScriptAlterPropBool("ContainmentType", "CONTAINMENT", sp, queries, "NONE", true);
                        break;
                    case ContainmentType.Partial:
                        ScriptAlterPropBool("ContainmentType", "CONTAINMENT", sp, queries, "PARTIAL", true);
                        break;
                    default:
                        throw new WrongPropertyValueException(Properties.Get("ContainmentType"));
                }
            }
        }

        void ScriptPageVerify(ScriptingPreferences sp, StringCollection queries)
        {
            if (IsSupportedProperty("PageVerify", sp))
            {
                if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90) //for 8.0
                {
                    string dbName = this.FormatFullNameForScripting(sp, false);
                    switch (GetPageVerify(sp))
                    {
                        case PageVerify.TornPageDetection:
                            ScriptAlterPropBool("PageVerify", "TORN_PAGE_DETECTION", sp, queries, "ON");
                            break;
                        case PageVerify.None:
                            ScriptAlterPropBool("PageVerify", "TORN_PAGE_DETECTION", sp, queries, "OFF");
                            break;
                        default:
                            //throw if for direct execution, ignore if for scripting
                            if (sp.ScriptForCreateDrop)
                            {
                                throw new WrongPropertyValueException(Properties.Get("PageVerify"));
                            }
                            break;
                    }
                }
                else //for 9.0
                {
                    Property prop = Properties.Get("PageVerify");
                    if (null == prop.Value || !(prop.Dirty || !sp.ForDirectExecution))
                    {
                        return;
                    }
                    string valPageVerify = string.Empty;
                    switch (GetPageVerify(sp))
                    {
                        case PageVerify.TornPageDetection:
                            valPageVerify = "TORN_PAGE_DETECTION ";
                            break;
                        case PageVerify.Checksum:
                            valPageVerify = "CHECKSUM ";
                            break;
                        case PageVerify.None:
                            valPageVerify = "NONE ";
                            break;
                        default:
                            //throw if for direct execution, ignore if for scripting
                            if (sp.ScriptForCreateDrop)
                            {
                                throw new WrongPropertyValueException(prop);
                            }
                            break;
                    }
                    ScriptAlterPropBool("PageVerify", "PAGE_VERIFY", sp, queries, valPageVerify);
                }
            }
        }

        SnapshotIsolationState GetSnapshotIsolationState(ScriptingPreferences sp)
        {
            return (SnapshotIsolationState)GetPropValueOptional("SnapshotIsolationState");
        }

        void ScriptSnapshotIsolationState(ScriptingPreferences sp, StringCollection queries)
        {
            if (!IsSupportedProperty("SnapshotIsolationState", sp))
            {
                return;
            }
            Property prop = Properties.Get("SnapshotIsolationState");
            if (null == prop.Value || !(prop.Dirty || !sp.ForDirectExecution))
            {
                return;
            }
            string valSnapshotIsolationState = string.Empty;
            switch (GetSnapshotIsolationState(sp))
            {
                case SnapshotIsolationState.Enabled:
                    valSnapshotIsolationState = "ON";
                    break;
                case SnapshotIsolationState.Disabled:
                    valSnapshotIsolationState = "OFF";
                    break;
                default:
                    //throw if for direct execution, ignore if for scripting
                    if (sp.ScriptForCreateDrop)
                    {
                        throw new WrongPropertyValueException(prop);
                    }
                    break;
            }
            ScriptAlterPropBool("SnapshotIsolationState", "ALLOW_SNAPSHOT_ISOLATION", sp, queries, valSnapshotIsolationState);
        }

        private void ScriptAlterPropBool(string propname, string optname, ScriptingPreferences sp, StringCollection queries)
        {
            ScriptAlterPropBool(propname, optname, sp, queries, false);
        }

        private void ScriptAlterPropBool(string propname, string optname, ScriptingPreferences sp, StringCollection queries, bool useEqualityOperator)
        {
            ScriptAlterPropBool(propname, optname, sp, queries, "ON", "OFF", useEqualityOperator);
        }

        private void ScriptAlterPropBool(string propname, string optname,
            ScriptingPreferences sp, StringCollection queries,
            string scriptTrue, string scriptFalse)
        {
            this.ScriptAlterPropBool(propname, optname,
                sp, queries,
                scriptTrue, scriptFalse, false);
        }

        private void ScriptAlterPropBool(string propname, string optname,
            ScriptingPreferences sp, StringCollection queries,
            string scriptTrue, string scriptFalse, bool useEqualityOperator)
        {
            if (!IsSupportedProperty(propname, sp))
            {
                return;
            }
            Property prop = (State == SqlSmoState.Creating || this.IsDesignMode) ? Properties.Get(propname) : Properties[propname];
            if (null != prop.Value && (prop.Dirty || !sp.ForDirectExecution))
            {
                ScriptAlterPropBool(propname, optname, sp, queries, (bool)prop.Value ? scriptTrue : scriptFalse, useEqualityOperator);
            }
        }

        private void ScriptAlterPropBool(string propname, string optname,
            ScriptingPreferences sp, StringCollection queries,
            string val)
        {
            this.ScriptAlterPropBool(propname, optname,
                sp, queries,
                val, false);
        }

        /// <summary>
        /// Scripts Database options
        /// </summary>
        /// <param name="propname"></param>
        /// <param name="optname"></param>
        /// <param name="sp"></param>
        /// <param name="queries"></param>
        /// <param name="val"></param>
        /// <param name="useEqualityOperator"></param>
        /// <returns>Returns true if Script is generated otherwise false</returns>
        private bool ScriptAlterPropBool(string propname, string optname,
            ScriptingPreferences sp, StringCollection queries,
            string val, bool useEqualityOperator)
        {
            if (!IsSupportedProperty(propname, sp))
            {
                return false;
            }
            bool scriptGenerated = false;
            Property prop = (State == SqlSmoState.Creating) ? Properties.Get(propname) : Properties[propname];
            if (null != prop.Value && (prop.Dirty || !sp.ForDirectExecution))
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET {1} {2}{3} {4}",
                    this.FormatFullNameForScripting(sp), optname,
                    useEqualityOperator ? "= " : string.Empty,
                    val,
                    null != optionTerminationStatement ? optionTerminationStatement.GetTerminationScript() : ""));

                scriptGenerated = true;
            }

            return scriptGenerated;
        }


        internal class OptionTerminationStatement
        {
            TimeSpan m_time;
            TerminationClause m_clause;

            internal OptionTerminationStatement(TimeSpan time)
            {
                m_time = time;
            }

            internal OptionTerminationStatement(TerminationClause clause)
            {
                m_time = TimeSpan.Zero;
                m_clause = clause;
            }

            internal string GetTerminationScript()
            {
                if (TimeSpan.Zero != m_time)
                {
                    return string.Format(SmoApplication.DefaultCulture, "WITH ROLLBACK AFTER {0} SECONDS", m_time.Seconds);
                }
                if (m_clause == TerminationClause.FailOnOpenTransactions)
                {
                    return "WITH NO_WAIT";
                }
                return "WITH ROLLBACK IMMEDIATE"; //TerminationClause.RollbackTransactionsImmediately
            }
        }


        /// <summary>
        /// Returns true if the database is part of an Availability Group and resides on the primary replica server
        /// </summary>
        /// <returns></returns>
        public bool IsLocalPrimaryReplica()
        {
            var retVal = false;
            var server = GetServerObject();
            server.SetDefaultInitFields(typeof(AvailabilityGroup), nameof(AvailabilityGroup.PrimaryReplicaServerName));
            //HADR is an unsupported feature in prior versions
            if (ServerVersion.Major < 11)
            {
                return retVal;
            }

            //HADR unsupported in Cloud
            if (DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || !server.IsHadrEnabled)
            {
                return retVal;
            }

            //Non HADR databases will not have the property value set
            if (string.IsNullOrEmpty(AvailabilityGroupName))
            {
                return retVal;
            }

            //It will return null if the DB is not part of any AG
            var ag = server.AvailabilityGroups[AvailabilityGroupName];

            if (ag == null)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("AvailabilityGroup"));
            }

            try
            {
                var obj = ExecutionManager.ExecuteScalar($"SELECT sys.fn_hadr_is_primary_replica({MakeSqlString(Name)})");
                if (obj is bool isPrimary)
                {
                    return isPrimary;
                }
            }
            catch (Exception e)
            {
                Diagnostics.TraceHelper.Trace("Database SMO Object", "Unable to query sys.fn_hadr_is_primary_replica. {0} {1}", e.Message, e.InnerException?.Message ?? "");
            }
            // If the query fails for some reason fall back to the old behavior
            var primaryReplicaServerName = ag.PrimaryReplicaServerName;
            // InvariantCulture?
            retVal = (NetCoreHelpers.StringCompare(GetServerName(), primaryReplicaServerName, true, CultureInfo.InvariantCulture) == 0);
            
            return retVal;
        }

        /// <summary>
        /// Clears Tables collection and initializes it with tables
        /// that have classified columns.
        /// The caller can always restore Tables collection by calling:
        /// db.Tables.ClearAndInitialize()
        /// </summary>
        public void InitializeClassifiedColumns()
        {
            Tables.ClearAndInitialize($"[@{nameof(Table.HasClassifiedColumn)} = '1']", null);

            List<string> classifiedFields = new List<string>()
            {
                nameof(Column.IsClassified),
                nameof(Column.SensitivityLabelName),
                nameof(Column.SensitivityLabelId),
                nameof(Column.SensitivityInformationTypeName),
                nameof(Column.SensitivityInformationTypeId)
            };

            if (VersionUtils.IsSql15Azure12OrLater(DatabaseEngineType, ServerVersion))
            {
                classifiedFields.Add(nameof(Column.SensitivityRank));
            }

            foreach (Table t in Tables)
            {
                t.Columns.ClearAndInitialize($"[@{nameof(Column.IsClassified)} = '1']", classifiedFields);
            }
        }

        /// <summary>
        /// Purges version information from the previous persistent version store for this database by invoking sp_persistent_version_cleanup
        /// </summary>
        public void CleanupPersistentVersionStore()
        {
            ExecuteNonQuery($"exec sys.sp_persistent_version_cleanup {MakeSqlBraket(Name)}");
        }

        /// <summary>
        /// Populates the object's property bag from the current row of the DataReader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="skipIfDirty">If true do not initialize the property if it has
        /// been changed by the user</param>
        /// <param name="startColIdx">Index of the first column</param>
        /// <param name="endColIdx">Index of the last column. If -1 then go to the end.</param>
        internal override void AddObjectPropsFromDataReader(IDataReader reader, bool skipIfDirty,
            int startColIdx, int endColIdx)
        {
            // We need the DatabaseEngineEdition for initializing the properties list for a Database, but this
            // can cause problems on Azure servers since getting the EngineEdition requires logging into the 
            // database itself which is something we want to avoid for serverless databases or inaccessible
            // databases. So to avoid that we prepopulate the edition by checking if it's DW beforehand (which
            // doesn't require connecting to the database to retrieve)
            if (m_edition == null && Parent.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                if (reader.GetSchemaTable().Rows.Cast<DataRow>().FirstOrDefault(r=> (string)r["ColumnName"] == "RealEngineEdition") != null)
                {

                    m_edition = (DatabaseEngineEdition)reader["RealEngineEdition"];
                }
#if DEBUG
                var name = (string)reader["Name"];
                TraceHelper.Trace("SMO", "Database: {0} Edition: {1} ", name, m_edition?.ToString() ?? "<NULL>");
#endif
            }
            base.AddObjectPropsFromDataReader(reader, skipIfDirty, startColIdx, endColIdx);
        }
    }
}
