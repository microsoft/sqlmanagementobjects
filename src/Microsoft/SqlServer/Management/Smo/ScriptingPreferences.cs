// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Enumerates ScriptBehavior as Drop,Create,CreateOrAlter or Drop and Create
    /// </summary>
    [Flags]
    public enum ScriptBehavior
    {
        /// <summary>
        /// Create Script
        /// </summary>
        Create = 1,
        /// <summary>
        /// Drop Script
        /// </summary>
        Drop = 2,
        /// <summary>
        /// Drop and create Script
        /// </summary>
        DropAndCreate = Create | Drop,
        /// <summary>
        /// Create or alter Script
        /// </summary>
        CreateOrAlter = 4 
    }

    /// <summary>
    /// Enumerates whether Partition Scheme is scripted for None,Table,Index or All
    /// </summary>
    [Flags]
    public enum PartitioningScheme
    {
        /// <summary>
        /// No Partition Scheme
        /// </summary>
        None = 0,
        /// <summary>
        /// Partition Scheme for table
        /// </summary>
        Table = 1,
        /// <summary>
        /// Partition Scheme for index
        /// </summary>
        Index = 2,
        /// <summary>
        /// Partition Scheme for both table and index
        /// </summary>
        All = Table | Index
    }

    /// <summary>
    /// Defines preferences for scripting
    /// </summary>
    public class ScriptingPreferences
    {
        /// <summary>
        /// Gets or sets ScriptBehaviour
        /// </summary>
        public ScriptBehavior Behavior { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that script should be generated for Alter
        /// </summary>
        public bool ScriptForAlter { get; set; }

        /// <summary>
        /// Gets or sets should execution continue on scripting error
        /// </summary>
        internal bool ContinueOnScriptingError { get; set; }

        /// <summary>
        /// Gets or sets system objects are scripted or not
        /// </summary>
        internal bool SystemObjects { get; set; }

        /// <summary>
        /// Gets or sets should dependency error get ignored
        /// </summary>
        internal bool IgnoreDependencyError { get; set; }

        /// <summary>
        /// Gets or sets dependent objects are scripted or not
        /// </summary>
        internal bool DependentObjects { get; set; }


        /// <summary>
        /// Gets or sets sfc children are scripted or not
        /// </summary>
        internal bool SfcChildren { get; set; }

        internal string NewLine { get; set; }
        internal bool ScriptForCreateDrop { get; set; }
        internal bool ForDirectExecution { get; set; }
        internal bool SuppressDirtyCheck { get; set; }
        internal bool VersionDirty { get; private set; }
        internal bool DatabaseEngineTypeDirty { get; private set; }
        internal bool DatabaseEngineEditionDirty { get; private set; }

        internal bool TargetVersionAndDatabaseEngineTypeDirty
        {
            get
            {
                return (this.VersionDirty && this.DatabaseEngineTypeDirty);
            }
        }

        private SqlServerVersionInternal m_eTargetServerVersion;

        /// <summary>
        /// The server version on which the scripts will run.
        /// </summary>
        internal SqlServerVersionInternal TargetServerVersionInternal
        {
            get
            {
                return m_eTargetServerVersion;
            }
            set
            {
                m_eTargetServerVersion = value;
                this.VersionDirty = true;
            }
        }

        /// <summary>
        /// The server version on which the scripts will run.
        /// </summary>
        internal SqlServerVersion TargetServerVersion
        {
            get
            {
                // the main purpose of this second enumeration is to
                // hide the fact that 7.0 is supported - it is only exposed
                // internally for a limited set of functionality.
                switch (m_eTargetServerVersion)
                {
                    case SqlServerVersionInternal.Version70:
                    // 7.0 is reported as 8.0
                    case SqlServerVersionInternal.Version80:
                        return SqlServerVersion.Version80;
                    case SqlServerVersionInternal.Version90:
                        return SqlServerVersion.Version90;
                    case SqlServerVersionInternal.Version100:
                        return SqlServerVersion.Version100;
                    case SqlServerVersionInternal.Version105:
                        return SqlServerVersion.Version105;
                    case SqlServerVersionInternal.Version110:
                        return SqlServerVersion.Version110;
                    case SqlServerVersionInternal.Version120:
                        return SqlServerVersion.Version120;
                    case SqlServerVersionInternal.Version130:
                        return SqlServerVersion.Version130;
                    case SqlServerVersionInternal.Version140:
                        return SqlServerVersion.Version140;
                    case SqlServerVersionInternal.Version150:
                        return SqlServerVersion.Version150;
                    case SqlServerVersionInternal.Version160:
                        return SqlServerVersion.Version160;
                    default:
                        Diagnostics.TraceHelper.Assert(false, "unexpected server version");
                        return SqlServerVersion.Version160;
                }
            }
            set
            {
                switch (value)
                {
                    case SqlServerVersion.Version80:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version80;
                        break;
                    case SqlServerVersion.Version90:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version90;
                        break;
                    case SqlServerVersion.Version100:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version100;
                        break;
                    case SqlServerVersion.Version105:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version105;
                        break;
                    case SqlServerVersion.Version110:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version110;
                        break;
                    case SqlServerVersion.Version120:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version120;
                        break;
                    case SqlServerVersion.Version130:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version130;
                        break;
                    case SqlServerVersion.Version140:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version140;
                        break;
                    case SqlServerVersion.Version150:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version150;
                        break;
                    case SqlServerVersion.Version160:
                        m_eTargetServerVersion = SqlServerVersionInternal.Version160;
                        break;
                    default:
                        Diagnostics.TraceHelper.Assert(false, "unexpected server version");
                        m_eTargetServerVersion = SqlServerVersionInternal.Version160;
                        break;
                }

                this.VersionDirty = true;
            }
        }


        private DatabaseEngineType m_eTargetDatabaseEngineType;
        /// <summary>
        /// The server database engine type on which the scripts will run.
        /// </summary>
        internal DatabaseEngineType TargetDatabaseEngineType
        {
            get
            {
                return m_eTargetDatabaseEngineType;
            }
            set
            {
                this.m_eTargetDatabaseEngineType = value;
                this.DatabaseEngineTypeDirty = true;
            }
        }

        private DatabaseEngineEdition m_eTargetDatabaseEngineEdition;
        /// <summary>
        /// The server database engine type on which the scripts will run.
        /// </summary>
        internal DatabaseEngineEdition TargetDatabaseEngineEdition
        {
            get
            {
                return m_eTargetDatabaseEngineEdition;
            }
            set
            {
                this.m_eTargetDatabaseEngineEdition = value;
                this.DatabaseEngineEditionDirty = true;
            }
        }

        /// <summary>
        /// Sets the TargetServerVersionInternal based on input ServerVersion structure.
        /// </summary>
        /// <param name="ver"></param>
        internal void SetTargetServerVersion(ServerVersion ver)
        {
            this.VersionDirty = true;
            switch (ver.Major)
            {
                case 8:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version80;
                    break;

                case 9:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version90;
                    break;

                case 10:
                    if (ver.Minor == 0)
                    {
                        m_eTargetServerVersion = SqlServerVersionInternal.Version100;
                        break;
                    }
                    Diagnostics.TraceHelper.Assert(ver.Minor == 50, "unexpected server version");
                    m_eTargetServerVersion = SqlServerVersionInternal.Version105;
                    break;

                case 11:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version110;
                    break;

                case 12:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version120;
                    break;

                case 13:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version130;
                    break;

                case 14:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version140;
                    break;

                case 15:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version150;
                    break;

                case 16:
                    m_eTargetServerVersion = SqlServerVersionInternal.Version160;
                    break;

                default:
                    Diagnostics.TraceHelper.Assert(false, "unexpected server version");
                    break;
            }
        }

        /// <summary>
        /// Sets the target database engine type to input DatabaseEngineType structure
        /// </summary>
        /// <param name="databaseEngineType"></param>
        internal void SetTargetDatabaseEngineType(DatabaseEngineType databaseEngineType)
        {
            this.TargetDatabaseEngineType = databaseEngineType;
        }

        /// <summary>
        /// Sets the target database engine edition to input DatabaseEngineEdition structure
        /// </summary>
        /// <param name="databaseEngineEdition"></param>
        internal void SetTargetDatabaseEngineEdition(DatabaseEngineEdition databaseEngineEdition)
        {
            this.TargetDatabaseEngineEdition = databaseEngineEdition;
        }

        /// <summary>
        /// Sets the target server version based on SMO object's ServerVersion
        /// </summary>
        /// <param name="o"></param>
        internal void SetTargetServerVersion(SqlSmoObject o)
        {
            SetTargetServerVersion(o.ServerVersion);
        }

        /// <summary>
        /// Sets the target database engine type based on SMO object's DatabaseEngineType
        /// </summary>
        /// <param name="o"></param>
        internal void SetTargetDatabaseEngineType(SqlSmoObject o)
        {
            SetTargetDatabaseEngineType(o.DatabaseEngineType);
        }

        /// <summary>
        /// Sets the target database engine edition based on SMO object's DatabaseEngineEdition
        /// </summary>
        /// <param name="o"></param>
        internal void SetTargetDatabaseEngineEdition(SqlSmoObject o)
        {
            SetTargetDatabaseEngineEdition(o.DatabaseEngineEdition);
        }

        /// <summary>
        /// Returns true if the target is an Azure Stretch database
        /// </summary>
        internal bool TargetEngineIsAzureStretchDb()
        {
            return (this.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase &&
                    this.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlStretchDatabase);
        }

        /// <summary>
        /// Returns true if the target is an Azure SQL DW database
        /// </summary>
        internal bool TargetEngineIsAzureSqlDw()
        {
            return (this.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase &&
                    this.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse);
        }

        /// <summary>
        /// Sets the target server info based on input SMO object
        /// </summary>
        /// <param name="o"></param>
        internal void SetTargetServerInfo(SqlSmoObject o)
        {
            this.SetTargetServerInfo(o, true);
        }

        /// <summary>
        /// Sets the target server info based on input SMO object, if not dirty OR we force this operation
        /// </summary>
        /// <param name="o"></param>
        /// <param name="forced"></param>
        internal void SetTargetServerInfo(SqlSmoObject o, bool forced)
        {
            if (forced || !this.DatabaseEngineTypeDirty)
            {
                SetTargetDatabaseEngineType(o.DatabaseEngineType);
            }

            if (forced || !this.VersionDirty)
            {
                SetTargetServerVersion(o.ServerVersion);
            }

            if (forced || !this.DatabaseEngineEditionDirty)
            {
                SetTargetDatabaseEngineEdition(o.DatabaseEngineEdition);
            }
        }

        /// <summary>
        /// Gets  additional script preferences
        /// </summary>
        internal IncludeScriptPreferences IncludeScripts { get; private set; }

        /// <summary>
        /// Gets  security preferences
        /// </summary>
        internal SecurityPreferences Security { get; private set; }

        /// <summary>
        /// Gets  storage preferences
        /// </summary>
        internal StoragePreferences Storage { get; private set; }

        /// <summary>
        /// Gets  table preferences
        /// </summary>
        internal TablePreferences Table { get; private set; }

        /// <summary>
        /// Gets  datatype preferences
        /// </summary>
        internal DataTypePreferences DataType { get; private set; }

        /// <summary>
        /// Gets  data preferences
        /// </summary>
        internal DataPreferences Data { get; private set; }

        internal OldScriptingOptions OldOptions { get; private set; }

        private AgentPreferences agentPreferences;

        /// <summary>
        /// Gets  agent preferences
        /// </summary>
        internal AgentPreferences Agent
        {
            get
            {
                if (agentPreferences == null)
                {
                    agentPreferences = new AgentPreferences();
                }
                return agentPreferences;
            }
        }

        private MailPreferences mailPreferences;

        /// <summary>
        /// Gets  mail preferences
        /// </summary>
        internal MailPreferences Mail
        {
            get
            {
                if (mailPreferences == null)
                {
                    mailPreferences = new MailPreferences();
                }
                return mailPreferences;
            }
        }

        /// <summary>
        /// default constructor
        /// </summary>
        internal ScriptingPreferences()
        {
            Init();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Obj">Object to script</param>
        internal ScriptingPreferences(SqlSmoObject Obj)
        {
            Init();
            this.SetTargetServerInfo(Obj);
        }

        private void Init()
        {
            this.Behavior = ScriptBehavior.Create;
            this.SystemObjects = true;
            this.NewLine = Globals.newline;
            this.SuppressDirtyCheck = true;
            this.SfcChildren = true;

            // the default target version is latest (in-market)
            this.m_eTargetServerVersion = SqlServerVersionInternal.Version140;

            // the default target database engine type is Singleton
            m_eTargetDatabaseEngineType = DatabaseEngineType.Standalone;

            this.IncludeScripts = new IncludeScriptPreferences();
            this.Table = new TablePreferences();
            this.Security = new SecurityPreferences();
            this.Storage = new StoragePreferences();
            this.DataType = new DataTypePreferences();
            this.Data = new DataPreferences();
            this.OldOptions = new OldScriptingOptions();
        }

        internal object Clone()
        {
            ScriptingPreferences clone = (ScriptingPreferences)this.MemberwiseClone();
            clone.IncludeScripts = (IncludeScriptPreferences)this.IncludeScripts.Clone();
            clone.agentPreferences = (AgentPreferences)this.Agent.Clone();
            clone.mailPreferences = (MailPreferences)this.Mail.Clone();
            clone.Data = (DataPreferences)this.Data.Clone();
            clone.DataType = (DataTypePreferences)this.DataType.Clone();
            clone.OldOptions = (OldScriptingOptions)this.OldOptions.Clone();
            clone.Security = (SecurityPreferences)this.Security.Clone();
            clone.Storage = (StoragePreferences)this.Storage.Clone();
            clone.Table = (TablePreferences)this.Table.Clone();
            return clone;
        }
    }

    /// <summary>
    /// Class for additional scripts
    /// </summary>
    internal class IncludeScriptPreferences
    {
        /// <summary>
        /// Gets or sets data is included or not
        /// </summary>
        public bool Data { get; set; }

        /// <summary>
        /// Gets or sets permissions are included or not
        /// </summary>
        public bool Permissions { get; set; }

        /// <summary>
        /// Gets or sets existence check is added or not
        /// </summary>
        public bool ExistenceCheck { get; set; }

        /// <summary>
        /// Gets or sets descriptive header is included or not
        /// </summary>
        public bool Header { get; set; }

        /// <summary>
        /// Whether the header containing information about the scripting parameters is included or not
        /// </summary>
        public bool ScriptingParameterHeader { get; set; }

        /// <summary>
        /// Gets or sets object name are schema qualified or not
        /// </summary>
        public bool SchemaQualify { get; set; }

        /// <summary>
        /// Gets or sets referenced table name are schema qualified or not
        /// </summary>
        internal bool SchemaQualifyForeignKeysReferences { get; set; }

        /// <summary>
        /// Gets or sets extended properties are scripted or not
        /// </summary>
        internal bool ExtendedProperties { get; set; }

        /// <summary>
        /// Gets or sets collation details are included or not
        /// </summary>
        public bool Collation { get; set; }

        /// <summary>
        /// Gets or sets whether to script object owners
        /// </summary>
        public bool Owner { get; set; }

        /// <summary>
        /// Gets or sets use database script are included or not
        /// </summary>
        public bool DatabaseContext { get; set; }

        /// <summary>
        /// Gets or sets binding memberships are included or not
        /// </summary>
        public bool Associations { get; set; }

        /// <summary>
        /// Gets or sets ansi padding scripts are included or not
        /// </summary>
        public bool AnsiPadding { get; set; }

        /// <summary>
        /// Gets or sets metadata script are included or not
        /// </summary>
        public bool Ddl { get; set; }

        /// <summary>
        /// Gets or sets DDL Triggers are created disabled or not
        /// </summary>
        internal bool CreateDdlTriggerDisabled { get; set; }

        internal IncludeScriptPreferences()
        {
            Init();
        }

        private void Init()
        {
            SchemaQualify = true;
            Ddl = true;
            Collation = true;
            ScriptingParameterHeader = false;
        }

        internal object Clone()
        {
            return this.MemberwiseClone();
        }

    }

    /// <summary>
    /// Class for security related preferences
    /// </summary>
    internal class SecurityPreferences
    {
        /// <summary>
        /// Gets or sets execute as is included or not
        /// </summary>
        public bool ExecuteAs { get; set; }

        /// <summary>
        /// Gets or sets security identifier is included or not
        /// </summary>
        public bool Sid { get; set; }

        internal SecurityPreferences()
        {
            Init();
        }

        private void Init()
        {
            ExecuteAs = true;
        }



        internal object Clone()
        {
            return this.MemberwiseClone();
        }


    }

    /// <summary>
    /// Class for mail related preferences
    /// </summary>
    internal class MailPreferences
    {
        /// <summary>
        /// Gets or sets mail accounts are included or not
        /// </summary>
        public bool Accounts { get; set; }

        /// <summary>
        /// Gets or set mail account principals are included or not
        /// </summary>
        public bool Principals { get; set; }

        internal MailPreferences()
        {
            Init();
        }

        private void Init()
        {
            Accounts = true;
            Principals = true;
        }



        internal object Clone()
        {
            return this.MemberwiseClone();
        }


    }

    /// <summary>
    /// Class for agenet related preferences
    /// </summary>
    internal class AgentPreferences
    {
        /// <summary>
        /// Gets or sets include Alert job
        /// </summary>
        public bool AlertJob { get; set; }

        /// <summary>
        /// Gets or sets include job id
        /// </summary>
        public bool JobId { get; set; }

        /// <summary>
        /// Gets or sets include notifications
        /// </summary>
        public bool Notify { get; set; }

        internal bool InScriptJob { get; set; }

        internal AgentPreferences()
        {
            Init();
        }

        private void Init()
        {
            JobId = true;
        }



        internal object Clone()
        {
            return this.MemberwiseClone();
        }


    }

    /// <summary>
    /// Class for storage related preferences
    /// </summary>
    internal class StoragePreferences
    {
        /// <summary>
        /// Gets or sets include filestream filegroups
        /// </summary>
        internal bool FileStreamFileGroup { get; set; }

        /// <summary>
        /// Gets or sets include filestream column
        /// </summary>
        internal bool FileStreamColumn { get; set; }

        /// <summary>
        /// Gets or sets whether filestream features are scripted or not
        /// </summary>
        public bool FileStream
        {
            get
            {
                return (this.FileStreamFileGroup && this.FileStreamColumn);
            }
            set
            {
                this.FileStreamColumn = value;
                this.FileStreamFileGroup = value;
            }
        }

        /// <summary>
        /// Gets or sets partitioning scheme behaviour
        /// </summary>
        internal PartitioningScheme PartitionSchemeInternal { get; set; }

        /// <summary>
        /// Gets or sets partitioning scheme behaviour
        /// </summary>
        public bool PartitionScheme
        {
            get
            {
                return ((this.PartitionSchemeInternal & PartitioningScheme.All) == PartitioningScheme.All);
            }
            set
            {
                this.PartitionSchemeInternal = (value ? PartitioningScheme.All : PartitioningScheme.None);
            }

        }

        /// <summary>
        /// Gets or sets Data Compression
        /// </summary>
        public bool DataCompression { get; set; }

        /// <summary>
        /// Gets or sets Xml Compression
        /// </summary>
        public bool XmlCompression { get; set; }

        /// <summary>
        /// Gets or sets to include filegroup script
        /// </summary>
        public bool FileGroup { get; set; }

        internal StoragePreferences()
        {
            Init();
        }

        private void Init()
        {
            DataCompression = true;
			XmlCompression = true;
            PartitionSchemeInternal = PartitioningScheme.All;
            this.FileGroup = true;
            this.FileStream = true;
        }

        internal object Clone()
        {
            return this.MemberwiseClone();
        }


    }

    /// <summary>
    /// Class for data related preferences
    /// </summary>
    internal class DataPreferences
    {
        /// <summary>
        /// Gets or sets include ChangeTracking
        /// </summary>
        public bool ChangeTracking { get; set; }

        /// <summary>
        /// Gets or sets include OptimizerData
        /// </summary>
        public bool OptimizerData { get; set; }

        internal DataPreferences()
        {
        }



        internal object Clone()
        {
            return this.MemberwiseClone();
        }


    }

    /// <summary>
    /// Class for table related preferences
    /// </summary>
    internal class TablePreferences
    {
        /// <summary>
        /// Gets or sets to include system names for constraints
        /// </summary>
        public bool SystemNamesForConstraints { get; set; }

        /// <summary>
        /// Gets or sets to include NO CHECK for constraints
        /// </summary>
        public bool ConstraintsWithNoCheck { get; set; }

        /// <summary>
        /// Gets or sets to script identity or not
        /// </summary>
        public bool Identities { get; set; }

        internal TablePreferences()
        {
            Init();
        }

        private void Init()
        {
            Identities = true;
        }

        internal object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Class for datatype related preferences
    /// </summary>
    internal class DataTypePreferences
    {
        /// <summary>
        /// Gets or sets user defined data types are converted to base type
        /// </summary>
        public bool UserDefinedDataTypesToBaseType { get; set; }

        /// <summary>
        /// Gets or sets timestamp is converted to binary
        /// </summary>
        public bool TimestampToBinary { get; set; }

        /// <summary>
        /// Gets or sets XmlNamespaces are included for data types
        /// </summary>
        public bool XmlNamespaces { get; set; }

        internal DataTypePreferences()
        {
            Init();
        }

        private void Init()
        {
            XmlNamespaces = true;
        }

        internal object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    internal class OldScriptingOptions
    {
        public bool Bindings { get; set; }
        public bool IncludeDatabaseRoleMemberships { get; set; }
        public bool NoViewColumns { get; set; }
        public bool EnforceScriptingPreferences { get; set; }
        public bool DdlHeaderOnly { get; set; }
        public bool DdlBodyOnly { get; set; }
        public bool NoVardecimal { get; set; }
        public bool IncludeFullTextCatalogRootPath { get; set; }
        public bool PrimaryObject { get; set; }

        public OldScriptingOptions()
        {
            Init();
        }

        private void Init()
        {
            PrimaryObject = true;
            NoVardecimal = true;
        }

        internal object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
