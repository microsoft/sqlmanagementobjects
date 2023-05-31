// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    internal enum EnumScriptOptions
    {
        ScriptBatchTerminator,
        NoCommandTerminator,
        AppendToFile,
        AnsiFile,
        ToFileOnly,
        //filter begins
        DriPrimaryKey,
        DriForeignKeys,
        DriUniqueKeys,
        DriIndexes,
        DriClustered,
        DriNonClustered,
        DriChecks,
        DriDefaults,
        Triggers,
        ClusteredIndexes,
        NonClusteredIndexes,
        FullTextCatalogs,
        FullTextStopLists,
        FullTextIndexes,
        Statistics,
        NoAssemblies,
        Indexes,
        DriAllKeys,
        DriAllConstraints,
        DriAll,
        XmlIndexes,
        SpatialIndexes,
        ColumnStoreIndexes,
        //Filter Ends
        //No direct sp
        TotalFilterOptions,
        NoTablePartitioningSchemes,
        NoIndexPartitioningSchemes,
        SchemaQualifyForeignKeysReferences,
        IncludeDatabaseRoleMemberships,
        //direct sp
        SchemaQualify,
        IncludeHeaders,
        IncludeIfNotExists,
        WithDependencies,
        Bindings,
        ContinueScriptingOnError,
        Permissions,
        AllowSystemObjects,
        DriWithNoCheck,
        ConvertUserDefinedDataTypesToBaseType,
        TimestampToBinary,
        AnsiPadding,
        ExtendedProperties,
        DdlHeaderOnly,
        DdlBodyOnly,
        NoViewColumns,
        LoginSid,
        IncludeDatabaseContext,
        AgentAlertJob,
        AgentJobId,
        AgentNotify,
        DriIncludeSystemNames,
        OptimizerData,
        NoExecuteAs,
        EnforceScriptingOptions, //used for text objects
        NoVardecimal,
        ScriptSchema,
        ScriptData,
        IncludeFullTextCatalogRootPath,
        ChangeTracking,
        ScriptDataCompression,
        ScriptXmlCompression,
        ScriptOwner,
        NoFileGroup,
        NoFileStream,
        NoFileStreamColumn,
        NoCollation,
        NoIdentities,
        NoXmlNamespaces,
        NoMailProfileAccounts,
        NoMailProfilePrincipals,
        PrimaryObject,
        TotalOptions
    }

    /// <summary>
    /// Internal enum - contains all versions supported by SMO.
    /// Note that 7.0 is not exposed publicly so this is why we have this enum.
    /// </summary>
    internal enum SqlServerVersionInternal
    {
        Version70 = 0,
        Version80 = 1,
        Version90 = 2,
        Version100 = 3,
        Version105 = 4,
        Version110 = 5,
        Version120 = 6,
        Version130 = 7,
        Version140 = 8,
        Version150 = 9,
        Version160 = 10
    }

    /// <summary>
    /// Enumerates versions of SqlServer supported by SMO.
    /// </summary>

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [TypeConverter(typeof(LocalizableEnumConverter))]
    public enum SqlServerVersion
    {
        [DisplayNameKey("ServerShiloh")]
        Version80 = 1,
        [DisplayNameKey("ServerYukon")]
        Version90 = 2,
        [DisplayNameKey("ServerKatmai")]
        Version100 = 3,
        [DisplayNameKey("ServerKilimanjaro")]
        Version105 = 4,
        [DisplayNameKey("ServerDenali")]
        Version110 = 5,
        [DisplayNameKey("ServerSQL14")]
        Version120 = 6,
        [DisplayNameKey("ServerSQL15")]
        Version130 = 7,
        [DisplayNameKey("ServerSQL2017")]
        Version140 = 8,
        [DisplayNameKey("ServerSQLv150")]
        Version150 = 9,
        [DisplayNameKey("ServerSQLv160")]
        Version160 = 10,
    }

    public static class TypeConverters
    {
        public static readonly TypeConverter SqlServerVersionTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(SqlServerVersion));
    }
    /// <summary>
    /// A single scripting option. It represents a single value
    /// from the above enum.
    /// </summary>
    public sealed class ScriptOption
    {
        private EnumScriptOptions m_value;
        internal EnumScriptOptions Value
        {
            get
            {
                return m_value;
            }
        }

        internal ScriptOption(EnumScriptOptions optionValue)
        {
            m_value = optionValue;
        }

        static public implicit operator ScriptingOptions(ScriptOption scriptOption)
        {
            return new ScriptingOptions(scriptOption);
        }

        static public ScriptingOptions operator |(ScriptOption leftOption, ScriptOption rightOption)
        {
            return new ScriptingOptions(leftOption.Value, rightOption.Value);
        }

        static public ScriptingOptions BitwiseOr(ScriptOption leftOption, ScriptOption rightOption)
        {
            return leftOption | rightOption;
        }

        /// <summary>
        /// This method allows us to set multiple options by using plus sign
        /// </summary>
        /// <param name="so1"></param>
        /// <param name="so2"></param>
        /// <returns></returns>
        static public ScriptingOptions operator +(ScriptOption leftOption, ScriptOption rightOption)
        {
            return new ScriptingOptions(leftOption.Value, rightOption.Value);
        }

        static public ScriptingOptions Add(ScriptOption leftOption, ScriptOption rightOption)
        {
            return leftOption + rightOption;
        }

        public override string ToString()
        {
            return m_value.ToString();
        }

        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return (this.m_value == ((ScriptOption)obj).m_value);
        }

        public override int GetHashCode()
        {
            return this.m_value.GetHashCode();
        }

        public static ScriptOption AppendToFile { get { return new ScriptOption(EnumScriptOptions.AppendToFile); } }
        public static ScriptOption ToFileOnly { get { return new ScriptOption(EnumScriptOptions.ToFileOnly); } }
        public static ScriptOption SchemaQualify { get { return new ScriptOption(EnumScriptOptions.SchemaQualify); } }
        public static ScriptOption IncludeHeaders { get { return new ScriptOption(EnumScriptOptions.IncludeHeaders); } }
        public static ScriptOption IncludeIfNotExists { get { return new ScriptOption(EnumScriptOptions.IncludeIfNotExists); } }
        public static ScriptOption WithDependencies { get { return new ScriptOption(EnumScriptOptions.WithDependencies); } }
        public static ScriptOption DriPrimaryKey { get { return new ScriptOption(EnumScriptOptions.DriPrimaryKey); } }
        public static ScriptOption DriForeignKeys { get { return new ScriptOption(EnumScriptOptions.DriForeignKeys); } }
        public static ScriptOption DriUniqueKeys { get { return new ScriptOption(EnumScriptOptions.DriUniqueKeys); } }
        public static ScriptOption DriClustered { get { return new ScriptOption(EnumScriptOptions.DriClustered); } }
        public static ScriptOption DriNonClustered { get { return new ScriptOption(EnumScriptOptions.DriNonClustered); } }
        public static ScriptOption DriChecks { get { return new ScriptOption(EnumScriptOptions.DriChecks); } }
        public static ScriptOption DriDefaults { get { return new ScriptOption(EnumScriptOptions.DriDefaults); } }
        public static ScriptOption Triggers { get { return new ScriptOption(EnumScriptOptions.Triggers); } }
        public static ScriptOption Bindings { get { return new ScriptOption(EnumScriptOptions.Bindings); } }
        public static ScriptOption NoFileGroup { get { return new ScriptOption(EnumScriptOptions.NoFileGroup); } }
        public static ScriptOption NoFileStream { get { return new ScriptOption(EnumScriptOptions.NoFileStream); } }
        public static ScriptOption NoFileStreamColumn { get { return new ScriptOption(EnumScriptOptions.NoFileStreamColumn); } }
        public static ScriptOption NoCollation { get { return new ScriptOption(EnumScriptOptions.NoCollation); } }
        public static ScriptOption ContinueScriptingOnError { get { return new ScriptOption(EnumScriptOptions.ContinueScriptingOnError); } }
        public static ScriptOption Permissions { get { return new ScriptOption(EnumScriptOptions.Permissions); } }
        public static ScriptOption AllowSystemObjects { get { return new ScriptOption(EnumScriptOptions.AllowSystemObjects); } }
        public static ScriptOption NoIdentities { get { return new ScriptOption(EnumScriptOptions.NoIdentities); } }
        public static ScriptOption ConvertUserDefinedDataTypesToBaseType { get { return new ScriptOption(EnumScriptOptions.ConvertUserDefinedDataTypesToBaseType); } }
        public static ScriptOption TimestampToBinary { get { return new ScriptOption(EnumScriptOptions.TimestampToBinary); } }
        public static ScriptOption AnsiPadding { get { return new ScriptOption(EnumScriptOptions.AnsiPadding); } }
        public static ScriptOption ExtendedProperties { get { return new ScriptOption(EnumScriptOptions.ExtendedProperties); } }
        public static ScriptOption DdlHeaderOnly { get { return new ScriptOption(EnumScriptOptions.DdlHeaderOnly); } }
        public static ScriptOption DdlBodyOnly { get { return new ScriptOption(EnumScriptOptions.DdlBodyOnly); } }
        public static ScriptOption NoViewColumns { get { return new ScriptOption(EnumScriptOptions.NoViewColumns); } }
        public static ScriptOption Statistics { get { return new ScriptOption(EnumScriptOptions.Statistics); } }
        public static ScriptOption SchemaQualifyForeignKeysReferences { get { return new ScriptOption(EnumScriptOptions.SchemaQualifyForeignKeysReferences); } }

        public static ScriptOption ClusteredIndexes { get { return new ScriptOption(EnumScriptOptions.ClusteredIndexes); } }
        public static ScriptOption NonClusteredIndexes { get { return new ScriptOption(EnumScriptOptions.NonClusteredIndexes); } }

        public static ScriptOption AnsiFile { get { return new ScriptOption(EnumScriptOptions.AnsiFile); } }
        public static ScriptOption AgentAlertJob { get { return new ScriptOption(EnumScriptOptions.AgentAlertJob); } }
        public static ScriptOption AgentJobId { get { return new ScriptOption(EnumScriptOptions.AgentJobId); } }
        public static ScriptOption AgentNotify { get { return new ScriptOption(EnumScriptOptions.AgentNotify); } }
        public static ScriptOption LoginSid { get { return new ScriptOption(EnumScriptOptions.LoginSid); } }
        public static ScriptOption NoCommandTerminator { get { return new ScriptOption(EnumScriptOptions.NoCommandTerminator); } }
        public static ScriptOption NoIndexPartitioningSchemes { get { return new ScriptOption(EnumScriptOptions.NoIndexPartitioningSchemes); } }
        public static ScriptOption NoTablePartitioningSchemes { get { return new ScriptOption(EnumScriptOptions.NoTablePartitioningSchemes); } }
        public static ScriptOption IncludeDatabaseContext { get { return new ScriptOption(EnumScriptOptions.IncludeDatabaseContext); } }
        public static ScriptOption FullTextCatalogs { get { return new ScriptOption(EnumScriptOptions.FullTextCatalogs); } }
        public static ScriptOption FullTextStopLists { get { return new ScriptOption(EnumScriptOptions.FullTextStopLists); } }
        public static ScriptOption FullTextIndexes { get { return new ScriptOption(EnumScriptOptions.FullTextIndexes); } }
        public static ScriptOption NoXmlNamespaces { get { return new ScriptOption(EnumScriptOptions.NoXmlNamespaces); } }
        public static ScriptOption NoAssemblies { get { return new ScriptOption(EnumScriptOptions.NoAssemblies); } }
        public static ScriptOption PrimaryObject { get { return new ScriptOption(EnumScriptOptions.PrimaryObject); } }
        public static ScriptOption DriIncludeSystemNames { get { return new ScriptOption(EnumScriptOptions.DriIncludeSystemNames); } }
        public static ScriptOption Default { get { return new ScriptOption(EnumScriptOptions.PrimaryObject); } }
        public static ScriptOption XmlIndexes { get { return new ScriptOption(EnumScriptOptions.XmlIndexes); } }
        public static ScriptOption OptimizerData { get { return new ScriptOption(EnumScriptOptions.OptimizerData); } }
        public static ScriptOption NoExecuteAs { get { return new ScriptOption(EnumScriptOptions.NoExecuteAs); } }
        public static ScriptOption EnforceScriptingOptions { get { return new ScriptOption(EnumScriptOptions.EnforceScriptingOptions); } }
        public static ScriptOption NoMailProfileAccounts { get { return new ScriptOption(EnumScriptOptions.NoMailProfileAccounts); } }
        public static ScriptOption NoMailProfilePrincipals { get { return new ScriptOption(EnumScriptOptions.NoMailProfilePrincipals); } }

        public static ScriptOption DriWithNoCheck { get { return new ScriptOption(EnumScriptOptions.DriWithNoCheck); } }
        public static ScriptOption DriAllKeys { get { return new ScriptOption(EnumScriptOptions.DriAllKeys); } }
        public static ScriptOption Indexes { get { return new ScriptOption(EnumScriptOptions.Indexes); } }
        public static ScriptOption DriIndexes { get { return new ScriptOption(EnumScriptOptions.DriIndexes); } }
        public static ScriptOption DriAllConstraints { get { return new ScriptOption(EnumScriptOptions.DriAllConstraints); } }
        public static ScriptOption DriAll { get { return new ScriptOption(EnumScriptOptions.DriAll); } }
        public static ScriptOption NoVardecimal { get { return new ScriptOption(EnumScriptOptions.NoVardecimal); } }
        public static ScriptOption IncludeDatabaseRoleMemberships { get { return new ScriptOption(EnumScriptOptions.IncludeDatabaseRoleMemberships); } }
        // new scripting option for scripting change tracking script on database and table
        public static ScriptOption ChangeTracking { get { return new ScriptOption(EnumScriptOptions.ChangeTracking); } }
        public static ScriptOption ScriptOwner { get { return new ScriptOption(EnumScriptOptions.ScriptOwner); } }

        public static ScriptOption IncludeFullTextCatalogRootPath
        {
            get
            {
                return new ScriptOption(EnumScriptOptions.IncludeFullTextCatalogRootPath);
            }
        }

        public static ScriptOption ScriptSchema
        {
            get
            {
                return new ScriptOption(EnumScriptOptions.ScriptSchema);
            }
        }
        public static ScriptOption ScriptData
        {
            get
            {
                return new ScriptOption(EnumScriptOptions.ScriptData);
            }
        }
        public static ScriptOption ScriptBatchTerminator
        {
            get
            {
                return new ScriptOption(EnumScriptOptions.ScriptBatchTerminator);
            }
        }
        public static ScriptOption ScriptDataCompression
        {
            get
            {
                return new ScriptOption(EnumScriptOptions.ScriptDataCompression);
            }
        }

        public static ScriptOption ScriptXmlCompression
        {
            get
            {
                return new ScriptOption(EnumScriptOptions.ScriptXmlCompression);
            }
        }
    }

    /// <summary>
    /// Instance class encapsulating SQL Server database
    /// </summary>
    public sealed class ScriptingOptions
    {
        #region Constructors
        /// <summary>
        /// Default constructor. Sets all default options.
        /// </summary>
        public ScriptingOptions()
        {
            Init();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public ScriptingOptions(ScriptingOptions so)
        {
            this.m_options = (BitArray)so.m_options.Clone();
            this.scriptingPreferences = (ScriptingPreferences)so.scriptingPreferences.Clone();
            this.m_sFileName = so.m_sFileName;
            this.m_encoding = so.m_encoding;
        }

        /// <summary>
        /// Creates a ScriptingOptions object with a single option set.
        /// </summary>
        /// <param name="so">An option to set</param>
        public ScriptingOptions(ScriptOption so)
        {
            InitializeOptionsAsFalse();

            this[so.Value] = true;

            // if the option passed to the ctor does not have ScriptData then set ScriptSchema to
            // true. This will ensure that ScriptSchema is set to true by default for existing code.
            //
            if (so.Value != EnumScriptOptions.ScriptData)
            {
                this[EnumScriptOptions.ScriptSchema] = true;
            }
        }

        private void InitializeOptionsAsFalse()
        {
            m_options = InitializeBitArray();
            this.scriptingPreferences = new ScriptingPreferences();
            this.AllowSystemObjects = false;
            this.SchemaQualify = false;
            this.PrimaryObject = false;
            this.ScriptDataCompression = false;
            this.ScriptXmlCompression = false;
            this.AgentJobId = false;
            this.NoVardecimal = false;
            this.ScriptSchema = false;
            this.DriWithNoCheck = false;
        }

        private BitArray InitializeBitArray()
        {
            return new BitArray(Convert.ToInt32(EnumScriptOptions.TotalFilterOptions, SmoApplication.DefaultCulture));
        }

        /// <summary>
        /// Constructs a ScriptingOptions object using target settings from the parent object's server
        /// </summary>
        /// <param name="parent"></param>
        public ScriptingOptions(SqlSmoObject parent)
        {
            Init();
            SetTargetServerInfo(parent);
        }

        internal ScriptingOptions(params EnumScriptOptions[] options)
        {
            InitializeOptionsAsFalse();
            bool isScriptDataSpecifed = false;
            foreach (EnumScriptOptions option in options)
            {
                this[option] = true;
                if (option == EnumScriptOptions.ScriptData)
                {
                    isScriptDataSpecifed = true;
                }
            }

            // if the option passed to the ctor does not have ScriptData then set ScriptSchema to
            // true. This will ensure that ScriptSchema is set to true by default for existing code.
            //
            if (!isScriptDataSpecifed)
            {
                this[EnumScriptOptions.ScriptSchema] = true;
            }
        }

        /// <summary>
        /// Sets all default options.
        /// </summary>
        private void Init()
        {
            m_options = InitializeBitArray();

            this.scriptingPreferences = new ScriptingPreferences();

            // we allow scripting system objects by default
            AllowSystemObjects = true;

            // WITH CHECK is the default for scripting Checks and ForeignKeys creation
            DriWithNoCheck = false;

            // by default we will script with IDENTITY flag
            NoIdentities = false;

            // by default UDDT are scripted with their own name
            ConvertUserDefinedDataTypesToBaseType = false;

            // by default timestamp types don't get scripted as binary(8)
            TimestampToBinary = false;

            // by default always schema qualify
            SchemaQualify = true;

            // default value
            AnsiPadding = false;

            // don't script extended properties by default
            ExtendedProperties = false;

            DdlHeaderOnly = false;
            DdlBodyOnly = false;

            //script view columns if initialy scripted with columns
            NoViewColumns = false;

            PrimaryObject = true;

            //by default don't script system generated names
            DriIncludeSystemNames = false;

            // by default we will script EXECUTE AS ... clause
            NoExecuteAs = false;

            //by default we script text objects as they are
            EnforceScriptingOptions = false;

            // by default we script all profile accounts
            NoMailProfileAccounts = false;

            //By default compression code should be scripted if compression attribute has been initialized
            ScriptDataCompression = true;

            //By default compression code should be scripted if compression attribute has been initialized
            ScriptXmlCompression = true;

            // by default we script all profile principals
            NoMailProfilePrincipals = false;

            OptimizerData = false;

            // This option used to be false by default but Job ID was always scripted
            // regardless, i.e. option was ignored. We will now honor it, but to preserve
            // the existing behavior we will change the default to true
            AgentJobId = true;

            // by default do not include vardecimal compression
            NoVardecimal = true;

            // by default include IF {NOT} EXISTS clause in scripts
            //IncludeIfNotExists = true;

            // by default do not include adding users (or other database roles) to database roles to be backcompat with Yukon SP1-
            IncludeDatabaseRoleMemberships = false;

            // sets the change tracking to false by default
            ChangeTracking = false;

            ScriptSchema = true;

            // by default don't script data
            ScriptData = false;

            // by default don't script batch terminator. This option is used while script insert statements
            // for data
            ScriptBatchTerminator = false;

            // By default don't script root path for full text catalog
            IncludeFullTextCatalogRootPath = false;

            ScriptOwner = false;
        }
        #endregion

        #region Public operators

        public ScriptingOptions Add(ScriptOption scriptOption)
        {
            this[scriptOption.Value] = true;
            return this;
        }

        public ScriptingOptions Remove(ScriptOption scriptOption)
        {
            this[scriptOption.Value] = false;
            return this;
        }

        static public ScriptingOptions operator +(ScriptingOptions options, ScriptOption scriptOption)
        {
            ScriptingOptions retOptions = new ScriptingOptions(options);
            retOptions[scriptOption.Value] = true;
            return retOptions;
        }

        static public ScriptingOptions Add(ScriptingOptions options, ScriptOption scriptOption)
        {
            return options + scriptOption;
        }

        static public ScriptingOptions operator -(ScriptingOptions options, ScriptOption scriptOption)
        {
            ScriptingOptions retOptions = new ScriptingOptions(options);
            retOptions[scriptOption.Value] = false;
            return retOptions;
        }

        static public ScriptingOptions Subtract(ScriptingOptions options, ScriptOption scriptOption)
        {
            return options - scriptOption;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.GetType().Name + ": ");

            int i = 0;
            bool first = true;

            foreach (bool isSet in this.m_options)
            {
                if (isSet)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(((EnumScriptOptions)i).ToString());
                }
                i++;
            }

            for (int j = (int)EnumScriptOptions.TotalFilterOptions + 1; j < (int)EnumScriptOptions.TotalOptions; j++)
            {
                bool isSet = this[(EnumScriptOptions)j];
                if (isSet)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(((EnumScriptOptions)j).ToString());
                }

            }

            return sb.ToString();
        }
        #endregion

        #region Internal options

        internal string NewLine
        {
            get
            {
                return this.scriptingPreferences.NewLine;
            }
            set
            {
                this.scriptingPreferences.NewLine = value;
            }
        }

        private string m_sFileName = String.Empty;
        public string FileName
        {
            get
            {
                return m_sFileName;
            }
            set
            {
                m_sFileName = value;
            }
        }

        private Encoding m_encoding = null;
        public Encoding Encoding
        {
            get
            {
                if (null == m_encoding)
                {
                    m_encoding = new UnicodeEncoding();
                }
                return m_encoding;
            }

            set
            {
                m_encoding = value;
                this[EnumScriptOptions.AnsiFile] = (m_encoding == Encoding.GetEncoding(1252));
            }
        }

        /// <summary>
        /// Scripts DROP and Create statement for the objects.
        /// </summary>
        public bool ScriptForCreateDrop
        {
            get
            {
                return this.scriptingPreferences.ScriptForCreateDrop;
            }
            set
            {
                this.scriptingPreferences.ScriptForCreateDrop = value;
            }
        }

        /// <summary>
        /// Scripts object using "CREATE OR ALTER" syntax. 
        /// </summary>
        /// <remarks>
        /// This setting is not supported for all object types, and only works when the destination is SQL Server 2016 Service Pack 1+.
        /// If toggled from true to false, the script behavior will change to CREATE for all objects.
        /// When set to true:
        /// 1. Objects that do not support "CREATE OR ALTER" will be scripted as CREATE. 
        ///    Their script content will also depend on the value of the IncludeIfNotExists property.
        /// 2. Objects that support "CREATE OR ALTER" will ignore the IncludeIfNotExists property.
        /// </remarks>
        public bool ScriptForCreateOrAlter
        {
            get
            {
                return scriptingPreferences.Behavior == ScriptBehavior.CreateOrAlter;
            }
            set
            {
                scriptingPreferences.Behavior = value ? ScriptBehavior.CreateOrAlter : ScriptBehavior.Create;
            }
        }

        /// <summary>
        /// Scripts ALTER statement for the objects.
        /// </summary>
        public bool ScriptForAlter
        {
            get
            {
                return this.scriptingPreferences.ScriptForAlter;
            }
            set
            {
                this.scriptingPreferences.ScriptForAlter = value;
            }
        }

        // if set, clustered indexes won't be scripted
        // regardless of all other scripting options.
        private bool skipClusteredIndexes = false;
        internal bool SkipClusteredIndexes
        {
            get
            {
                return skipClusteredIndexes;
            }
            set
            {
                skipClusteredIndexes = value;
            }
        }

        public bool DriWithNoCheck
        {
            get
            {
                return this.scriptingPreferences.Table.ConstraintsWithNoCheck;
            }
            set
            {
                this.scriptingPreferences.Table.ConstraintsWithNoCheck = value;
            }
        }

        public bool IncludeFullTextCatalogRootPath
        {
            get
            {
                return this.scriptingPreferences.OldOptions.IncludeFullTextCatalogRootPath;
            }
            set
            {
                this.scriptingPreferences.OldOptions.IncludeFullTextCatalogRootPath = value;
            }
        }

        public bool SpatialIndexes
        {
            get
            {
                return this[EnumScriptOptions.SpatialIndexes];
            }
            set
            {
                this[EnumScriptOptions.SpatialIndexes] = value;
            }
        }

        public bool ColumnStoreIndexes
        {
            get
            {
                return this[EnumScriptOptions.ColumnStoreIndexes];
            }
            set
            {
                this[EnumScriptOptions.ColumnStoreIndexes] = value;
            }
        }
        #endregion

        #region Public options

        private ScriptingPreferences scriptingPreferences;
        private int m_batchSize = 1;

        /// <summary>
        /// The number of statements after which to script batch terminator
        /// </summary>
        public int BatchSize
        {
            get
            {
                return m_batchSize;
            }
            set
            {
                m_batchSize = value;
            }
        }

        /// <summary>
        /// Scripts DROP statements for the objects.
        /// </summary>
        public bool ScriptDrops
        {
            get
            {
                return (this.scriptingPreferences.Behavior & ScriptBehavior.Drop) == ScriptBehavior.Drop;
            }
            set
            {
                if (value)
                {
                    this.scriptingPreferences.Behavior = ScriptBehavior.Drop;
                }
                else
                {
                    this.scriptingPreferences.Behavior = ScriptBehavior.Create;
                }
            }
        }

        /// <summary>
        /// The server version on which the scripts will run.
        /// </summary>
        internal SqlServerVersionInternal TargetServerVersionInternal
        {
            get
            {
                return this.scriptingPreferences.TargetServerVersionInternal;
            }
            set
            {
                this.scriptingPreferences.TargetServerVersionInternal = value;
            }
        }

        /// <summary>
        /// The server version on which the scripts will run.
        /// </summary>
        public SqlServerVersion TargetServerVersion
        {
            get
            {
                return this.scriptingPreferences.TargetServerVersion;
            }
            set
            {
                this.scriptingPreferences.TargetServerVersion = value;
            }
        }

        /// <summary>
        /// The server database engine type on which the scripts will run.
        /// </summary>
        public DatabaseEngineType TargetDatabaseEngineType
        {
            get
            {
                return this.scriptingPreferences.TargetDatabaseEngineType;
            }
            set
            {
                this.scriptingPreferences.TargetDatabaseEngineType = value;
            }
        }

        /// <summary>
        /// The server database edition on which the scripts will run.
        /// </summary>
        public DatabaseEngineEdition TargetDatabaseEngineEdition
        {
            get
            {
                return this.scriptingPreferences.TargetDatabaseEngineEdition;
            }
            set
            {
                this.scriptingPreferences.TargetDatabaseEngineEdition = value;
            }
        }

        /// <summary>
        /// Sets the TargetServerVersionInternal based on a ServerVersion structure.
        /// </summary>
        /// <param name="ver"></param>
        public void SetTargetServerVersion(ServerVersion ver)
        {
            this.scriptingPreferences.SetTargetServerVersion(ver);
        }

        internal static ServerVersion ConvertToServerVersion(SqlServerVersion ver)
        {
            switch (ver)
            {
                case SqlServerVersion.Version80:
                    return new ServerVersion(8, 0, 0);
                case SqlServerVersion.Version90:
                    return new ServerVersion(9, 0, 0);
                case SqlServerVersion.Version100:
                    return new ServerVersion(10, 0, 0);
                case SqlServerVersion.Version105:
                    return new ServerVersion(10, 50, 0);
                case SqlServerVersion.Version110:
                    return new ServerVersion(11, 0, 0);
                case SqlServerVersion.Version120:
                    return new ServerVersion(12, 0, 0);
                case SqlServerVersion.Version130:
                    return new ServerVersion(13, 0, 0);
                case SqlServerVersion.Version140:
                    return new ServerVersion(14, 0, 0);
                case SqlServerVersion.Version150:
                    return new ServerVersion(15, 0, 0);
                default:
                    Diagnostics.TraceHelper.Assert(ver == SqlServerVersion.Version160, "unexpected server version");
                    return new ServerVersion(16, 0, 0);
            }
        }
        /// <summary>
        /// Sets the TargetServerDatabasseEngineType based on a DatabaseEngineType structure.
        /// </summary>
        /// <param name="ver"></param>
        public void SetTargetDatabaseEngineType(DatabaseEngineType databaseEngineType)
        {
            this.TargetDatabaseEngineType = databaseEngineType;
        }


        /// <summary>
        /// Sets the target database engine type
        /// </summary>
        /// <param name="o"></param>
        internal void SetTargetDatabaseEngineType(SqlSmoObject o)
        {
            SetTargetDatabaseEngineType(o.DatabaseEngineType);
        }

        /// <summary>
        /// Sets the target server version based on the server version
        /// </summary>
        /// <param name="o"></param>
        internal void SetTargetServerVersion(SqlSmoObject o)
        {
            this.scriptingPreferences.SetTargetServerVersion(o);
        }

        /// <summary>
        /// Sets the target server info based on the server info
        /// </summary>
        /// <param name="o"></param>
        internal void SetTargetServerInfo(SqlSmoObject o)
        {
            this.scriptingPreferences.SetTargetServerInfo(o);
        }

        /// <summary>
        /// Sets the target server info based on the server info if not dirty
        /// </summary>
        /// <param name="o"></param>
        /// <param name="forced"></param>
        internal void SetTargetServerInfo(SqlSmoObject o, bool forced)
        {
            this.scriptingPreferences.SetTargetServerInfo(o,forced);
        }

        /// <summary>
        /// If set will create an ANSI file in which to write the script results.
        /// </summary>
        public bool AnsiFile
        {
            get
            {
                return this[EnumScriptOptions.AnsiFile];
            }
            set
            {
                this[EnumScriptOptions.AnsiFile] = value;

                // if setting this to true, then we use code page 1252
                if (value)
                {
                    m_encoding = Encoding.GetEncoding(1252);
                }
                else
                {
                    if (m_encoding == Encoding.GetEncoding(1252))
                    {
                        m_encoding = new UnicodeEncoding();
                    }
                }
            }
        }

        /// <summary>
        /// Appends to file instead of overwriting it.
        /// </summary>
        public bool AppendToFile
        {
            get
            {
                return this[EnumScriptOptions.AppendToFile];
            }
            set
            {
                this[EnumScriptOptions.AppendToFile] = value;
            }
        }

        public bool ToFileOnly
        {
            get
            {
                return this[EnumScriptOptions.ToFileOnly];
            }
            set
            {
                this[EnumScriptOptions.ToFileOnly] = value;
            }
        }

        /// <summary>
        /// Whether object names are schema qualified
        /// </summary>
        public bool SchemaQualify
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.SchemaQualify;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.SchemaQualify = value;
            }
        }

        /// <summary>
        /// Whether a header containing information about the object being scripted (such as name
        /// and time scripted ) is included
        /// </summary>
        public bool IncludeHeaders
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.Header;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.Header = value;
            }
        }

        /// <summary>
        /// Whether a header containing information about the scripting parameters is included
        /// </summary>
        public bool IncludeScriptingParametersHeader
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.ScriptingParameterHeader;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.ScriptingParameterHeader = value;
            }
        }

        /// <summary>
        /// Whether an existence check is added
        /// </summary>
        public bool IncludeIfNotExists
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.ExistenceCheck;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.ExistenceCheck = value;
            }
        }

        public bool WithDependencies
        {
            get
            {
                return this.scriptingPreferences.DependentObjects;
            }
            set
            {
                this.scriptingPreferences.DependentObjects = value;
            }
        }

        #region Filter
        public bool DriPrimaryKey
        {
            get
            {
                return this[EnumScriptOptions.DriPrimaryKey];
            }
            set
            {
                this[EnumScriptOptions.DriPrimaryKey] = value;
            }
        }

        internal bool ScriptDriPrimaryKey()
        {
            return DriPrimaryKey || Indexes || DriIndexes ||
                DriAllKeys || DriAllConstraints || DriAll;
        }

        public bool DriForeignKeys
        {
            get
            {
                return this[EnumScriptOptions.DriForeignKeys];
            }
            set
            {
                this[EnumScriptOptions.DriForeignKeys] = value;
            }
        }

        internal bool ScriptDriForeignKeys()
        {
            return DriForeignKeys || DriAllKeys || DriAllConstraints || DriAll;
        }

        public bool DriUniqueKeys
        {
            get
            {
                return this[EnumScriptOptions.DriUniqueKeys];
            }
            set
            {
                this[EnumScriptOptions.DriUniqueKeys] = value;
            }
        }

        internal bool ScriptDriUniqueKeys()
        {
            return DriUniqueKeys || Indexes || DriIndexes ||
                DriAllKeys || DriAllConstraints || DriAll;
        }

        public bool DriClustered
        {
            get
            {
                return this[EnumScriptOptions.DriClustered];
            }
            set
            {
                this[EnumScriptOptions.DriClustered] = value;
            }
        }

        internal bool ScriptDriClustered()
        {
            return DriClustered || Indexes || DriIndexes || DriAll;
        }

        public bool DriNonClustered
        {
            get
            {
                return this[EnumScriptOptions.DriNonClustered];
            }
            set
            {
                this[EnumScriptOptions.DriNonClustered] = value;
            }
        }

        internal bool ScriptDriNonClustered()
        {
            return DriNonClustered || Indexes || DriIndexes || DriAll;
        }

        public bool DriChecks
        {
            get
            {
                return this[EnumScriptOptions.DriChecks];
            }
            set
            {
                this[EnumScriptOptions.DriChecks] = value;
            }
        }

        internal bool ScriptDriChecks()
        {
            return DriChecks || DriAllConstraints || DriAll;
        }

        public bool DriDefaults
        {
            get
            {
                return this[EnumScriptOptions.DriDefaults];
            }
            set
            {
                this[EnumScriptOptions.DriDefaults] = value;
            }
        }

        internal bool ScriptDriDefaults()
        {
            return DriDefaults || DriAllConstraints || DriAll;
        }

        public bool Triggers
        {
            get
            {
                return this[EnumScriptOptions.Triggers];
            }
            set
            {
                this[EnumScriptOptions.Triggers] = value;
            }
        }

        public bool Statistics
        {
            get
            {
                return this[EnumScriptOptions.Statistics];
            }
            set
            {
                this[EnumScriptOptions.Statistics] = value;
            }
        }

        public bool ClusteredIndexes
        {
            get
            {
                return this[EnumScriptOptions.ClusteredIndexes];
            }
            set
            {
                this[EnumScriptOptions.ClusteredIndexes] = value;
            }
        }

        internal bool ScriptClusteredIndexes()
        {
            return ClusteredIndexes || Indexes;
        }

        public bool NonClusteredIndexes
        {
            get
            {
                return this[EnumScriptOptions.NonClusteredIndexes];
            }
            set
            {
                this[EnumScriptOptions.NonClusteredIndexes] = value;
            }
        }

        internal bool ScriptNonClusteredIndexes()
        {
            return NonClusteredIndexes || Indexes;
        }

        public bool NoAssemblies
        {
            get
            {
                return this[EnumScriptOptions.NoAssemblies];
            }
            set
            {
                this[EnumScriptOptions.NoAssemblies] = value;
            }
        }

        public bool PrimaryObject
        {
            get
            {
                return this.scriptingPreferences.OldOptions.PrimaryObject;
            }
            set
            {
                this.scriptingPreferences.OldOptions.PrimaryObject = value;
            }
        }

        public bool Default
        {
            get
            {
                return this.scriptingPreferences.OldOptions.PrimaryObject;
            }
            set
            {
                this.scriptingPreferences.OldOptions.PrimaryObject = value;
            }
        }

        public bool XmlIndexes
        {
            get
            {
                return this[EnumScriptOptions.XmlIndexes];
            }
            set
            {
                this[EnumScriptOptions.XmlIndexes] = value;
            }
        }

        internal bool ScriptXmlIndexes()
        {
            return XmlIndexes || Indexes;
        }

        public bool FullTextCatalogs
        {
            get
            {
                return this[EnumScriptOptions.FullTextCatalogs];
            }
            set
            {
                this[EnumScriptOptions.FullTextCatalogs] = value;
            }
        }

        public bool FullTextIndexes
        {
            get
            {
                return this[EnumScriptOptions.FullTextIndexes];
            }
            set
            {
                this[EnumScriptOptions.FullTextIndexes] = value;
            }
        }

        public bool FullTextStopLists
        {
            get
            {
                return this[EnumScriptOptions.FullTextStopLists];
            }
            set
            {
                this[EnumScriptOptions.FullTextStopLists] = value;
            }
        }

        #region Aggregated_options

        public bool Indexes
        {
            get
            {
                return this[EnumScriptOptions.Indexes];
            }
            set
            {
                this[EnumScriptOptions.Indexes] = value;
            }
        }

        public bool DriIndexes
        {
            get
            {
                return this[EnumScriptOptions.DriIndexes];
            }
            set
            {
                this[EnumScriptOptions.DriIndexes] = value;
            }
        }

        public bool DriAllKeys
        {
            get
            {
                return this[EnumScriptOptions.DriAllKeys];
            }
            set
            {
                this[EnumScriptOptions.DriAllKeys] = value;
            }
        }

        public bool DriAllConstraints
        {
            get
            {
                return this[EnumScriptOptions.DriAllConstraints];
            }
            set
            {
                this[EnumScriptOptions.DriAllConstraints] = value;
            }
        }

        public bool DriAll
        {
            get
            {
                return this[EnumScriptOptions.DriAll];
            }
            set
            {
                this[EnumScriptOptions.DriAll] = value;
            }
        }
        #endregion

        #endregion

        public bool Bindings
        {
            get
            {
                return this.scriptingPreferences.OldOptions.Bindings;
            }
            set
            {
                this.scriptingPreferences.OldOptions.Bindings = value;
            }
        }

        public bool NoFileGroup
        {
            get
            {
                return !this.scriptingPreferences.Storage.FileGroup;
            }
            set
            {
                this.scriptingPreferences.Storage.FileGroup = !value;
            }
        }

        /// <summary>
        /// Whether to exclude filestream filegroups
        /// </summary>
        public bool NoFileStream
        {
            get
            {
                return !this.scriptingPreferences.Storage.FileStreamFileGroup;
            }
            set
            {
                this.scriptingPreferences.Storage.FileStreamFileGroup = !value;
            }
        }

        /// <summary>
        /// Whether to include filestream column
        /// </summary>
        public bool NoFileStreamColumn
        {
            get
            {
                return !this.scriptingPreferences.Storage.FileStreamColumn;
            }
            set
            {
                this.scriptingPreferences.Storage.FileStreamColumn = !value;
            }
        }

        /// <summary>
        /// Whether collation details are excluded
        /// </summary>
        public bool NoCollation
        {
            get
            {
                return !this.scriptingPreferences.IncludeScripts.Collation;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.Collation = !value;
            }
        }

        /// <summary>
        /// Whether execution should continue on scripting error
        /// </summary>
        public bool ContinueScriptingOnError
        {
            get
            {
                return this.scriptingPreferences.ContinueOnScriptingError;
            }
            set
            {
                this.scriptingPreferences.ContinueOnScriptingError = value;
            }
        }

        /// <summary>
        /// Whether binding memberships are included
        /// </summary>
        public bool IncludeDatabaseRoleMemberships
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.Associations;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.Associations = value;
            }
        }

        /// <summary>
        /// Whether permissions are included
        /// </summary>
        public bool Permissions
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.Permissions;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.Permissions = value;
            }
        }

        /// <summary>
        /// Whether system objects are scripted
        /// </summary>
        public bool AllowSystemObjects
        {
            get
            {
                return this.scriptingPreferences.SystemObjects;
            }
            set
            {
                this.scriptingPreferences.SystemObjects = value;
            }
        }

        /// <summary>
        /// Whether identities are excluded
        /// </summary>
        public bool NoIdentities
        {
            get
            {
                return !this.scriptingPreferences.Table.Identities;
            }
            set
            {
                this.scriptingPreferences.Table.Identities = !value;
            }
        }

        /// <summary>
        /// Whether user defined data types are converted to base type
        /// </summary>
        public bool ConvertUserDefinedDataTypesToBaseType
        {
            get
            {
                return this.scriptingPreferences.DataType.UserDefinedDataTypesToBaseType;
            }
            set
            {
                this.scriptingPreferences.DataType.UserDefinedDataTypesToBaseType = value;
            }
        }

        /// <summary>
        /// Whether timestamps are converted to binary
        /// </summary>
        public bool TimestampToBinary
        {
            get
            {
                return this.scriptingPreferences.DataType.TimestampToBinary;
            }
            set
            {
                this.scriptingPreferences.DataType.TimestampToBinary = value;
            }
        }

        /// <summary>
        /// Whether ansi padding scripts are included
        /// </summary>
        public bool AnsiPadding
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.AnsiPadding;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.AnsiPadding = value;
            }
        }

        /// <summary>
        /// Whether extended properties are scripted
        /// </summary>
        public bool ExtendedProperties
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.ExtendedProperties;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.ExtendedProperties = value;
            }
        }

        public bool DdlHeaderOnly
        {
            get
            {
                return this.scriptingPreferences.OldOptions.DdlHeaderOnly;
            }
            set
            {
                this.scriptingPreferences.OldOptions.DdlHeaderOnly = value;
            }
        }

        public bool DdlBodyOnly
        {
            get
            {
                return this.scriptingPreferences.OldOptions.DdlBodyOnly;
            }
            set
            {
                this.scriptingPreferences.OldOptions.DdlBodyOnly = value;
            }
        }

        public bool NoViewColumns
        {
            get
            {
                return this.scriptingPreferences.OldOptions.NoViewColumns;
            }
            set
            {
                this.scriptingPreferences.OldOptions.NoViewColumns = value;
            }
        }

        /// <summary>
        /// Whether referenced table names are schema qualified
        /// </summary>
        public bool SchemaQualifyForeignKeysReferences
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.SchemaQualifyForeignKeysReferences;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.SchemaQualifyForeignKeysReferences = value;
            }
        }

        /// <summary>
        /// Whether to include scripting Agent Alert jobs
        /// </summary>
        public bool AgentAlertJob
        {
            get
            {
                return this.scriptingPreferences.Agent.AlertJob;
            }
            set
            {
                this.scriptingPreferences.Agent.AlertJob = value;
            }
        }

        /// <summary>
        /// Whether to include scripting Agent job id
        /// </summary>
        public bool AgentJobId
        {
            get
            {
                return this.scriptingPreferences.Agent.JobId;
            }
            set
            {
                this.scriptingPreferences.Agent.JobId = value;
            }
        }

        /// <summary>
        /// Whether to include scripting Agent notifications
        /// </summary>
        public bool AgentNotify
        {
            get
            {
                return this.scriptingPreferences.Agent.Notify;
            }
            set
            {
                this.scriptingPreferences.Agent.Notify = value;
            }
        }

        /// <summary>
        /// Whether security identifier (SID) is included
        /// </summary>
        public bool LoginSid
        {
            get
            {
                return this.scriptingPreferences.Security.Sid;
            }
            set
            {
                this.scriptingPreferences.Security.Sid = value;
            }
        }

        public bool NoCommandTerminator
        {
            get
            {
                return this[EnumScriptOptions.NoCommandTerminator];
            }
            set
            {
                this[EnumScriptOptions.NoCommandTerminator] = value;
            }
        }

        public bool NoIndexPartitioningSchemes
        {
            get
            {
                return !((this.scriptingPreferences.Storage.PartitionSchemeInternal & PartitioningScheme.Index) == PartitioningScheme.Index);
            }
            set
            {
                if (value)
                {
                    this.scriptingPreferences.Storage.PartitionSchemeInternal &= ~PartitioningScheme.Index;
                }
                else
                {
                    this.scriptingPreferences.Storage.PartitionSchemeInternal |= PartitioningScheme.Index;
                }
            }
        }

        public bool NoTablePartitioningSchemes
        {
            get
            {
                return !((this.scriptingPreferences.Storage.PartitionSchemeInternal & PartitioningScheme.Table) == PartitioningScheme.Table);
            }
            set
            {
                if (value)
                {
                    this.scriptingPreferences.Storage.PartitionSchemeInternal &= ~PartitioningScheme.Table;
                }
                else
                {
                    this.scriptingPreferences.Storage.PartitionSchemeInternal |= PartitioningScheme.Table;
                }
            }
        }

        /// <summary>
        /// Whether use database script are included
        /// </summary>
        public bool IncludeDatabaseContext
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.DatabaseContext;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.DatabaseContext = value;
            }
        }

        /// <summary>
        /// Whether XmlNamespaces are included for data types
        /// </summary>
        public bool NoXmlNamespaces
        {
            get
            {
                return !this.scriptingPreferences.DataType.XmlNamespaces;
            }
            set
            {
                this.scriptingPreferences.DataType.XmlNamespaces = !value;
            }
        }

        /// <summary>
        /// Whether to include system names for constraints
        /// </summary>
        public bool DriIncludeSystemNames
        {
            get
            {
                return this.scriptingPreferences.Table.SystemNamesForConstraints;
            }
            set
            {
                this.scriptingPreferences.Table.SystemNamesForConstraints = value;
            }
        }

        /// <summary>
        /// Whether to include OptimizerData
        /// </summary>
        public bool OptimizerData
        {
            get
            {
                return this.scriptingPreferences.Data.OptimizerData;
            }
            set
            {
                this.scriptingPreferences.Data.OptimizerData = value;
            }
        }

        /// <summary>
        /// Whether to exclude EXECUTE AS statements
        /// </summary>
        public bool NoExecuteAs
        {
            get
            {
                return !this.scriptingPreferences.Security.ExecuteAs;
            }
            set
            {
                this.scriptingPreferences.Security.ExecuteAs = !value;
            }
        }

        public bool EnforceScriptingOptions
        {
            get
            {
                return this.scriptingPreferences.OldOptions.EnforceScriptingPreferences;
            }
            set
            {
                this.scriptingPreferences.OldOptions.EnforceScriptingPreferences = value;
            }
        }

        /// <summary>
        /// Whether mail accounts are excluded
        /// </summary>
        public bool NoMailProfileAccounts
        {
            get
            {
                return !this.scriptingPreferences.Mail.Accounts;
            }
            set
            {
                this.scriptingPreferences.Mail.Accounts = !value;
            }
        }

        /// <summary>
        /// Whether mail account principals are excluded
        /// </summary>
        public bool NoMailProfilePrincipals
        {
            get
            {
                return !this.scriptingPreferences.Mail.Principals;
            }
            set
            {
                this.scriptingPreferences.Mail.Principals = !value;
            }
        }

        public bool NoVardecimal
        {
            get
            {
                return this.scriptingPreferences.OldOptions.NoVardecimal;
            }
            set
            {
                this.scriptingPreferences.OldOptions.NoVardecimal = value;
            }
        }

        /// <summary>
        /// Whether to include Change Tracking options
        /// </summary>
        public bool ChangeTracking
        {
            get
            {
                return this.scriptingPreferences.Data.ChangeTracking;
            }
            set
            {
                this.scriptingPreferences.Data.ChangeTracking = value;
            }
        }

        /// <summary>
        /// Whether to include Data Compression options
        /// </summary>
        public bool ScriptDataCompression
        {
            get
            {
                return this.scriptingPreferences.Storage.DataCompression;
            }
            set
            {
                this.scriptingPreferences.Storage.DataCompression = value;
            }
        }

        /// <summary>
        /// Whether to include Xml Compression options
        /// </summary>
        public bool ScriptXmlCompression
        {
            get
            {
                return this.scriptingPreferences.Storage.XmlCompression;
            }
            set
            {
                this.scriptingPreferences.Storage.XmlCompression = value;
            }
        }

        /// <summary>
        /// Whether metadata script are included
        /// </summary>
        public bool ScriptSchema
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.Ddl;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.Ddl = value;
            }
        }

        /// <summary>
        /// Whether data is included
        /// </summary>
        public bool ScriptData
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.Data;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.Data = value;
            }
        }

        public bool ScriptBatchTerminator
        {
            get
            {
                return this[EnumScriptOptions.ScriptBatchTerminator];
            }
            set
            {
                this[EnumScriptOptions.ScriptBatchTerminator] = value;
            }
        }

        /// <summary>
        /// Whether owner statements are included
        /// </summary>
        public bool ScriptOwner
        {
            get
            {
                return this.scriptingPreferences.IncludeScripts.Owner;
            }
            set
            {
                this.scriptingPreferences.IncludeScripts.Owner = value;
            }
        }
        #endregion

        #region Implementation

        private bool this[EnumScriptOptions eso]
        {
            get
            {
                if ((int)(eso) <= (int)EnumScriptOptions.TotalFilterOptions)
                {
                    return m_options[Convert.ToInt32(eso, SmoApplication.DefaultCulture)];
                }
                else
                {
                    return GetScriptingPreference(eso);
                }
            }
            set
            {
                if ((int)(eso) <= (int)EnumScriptOptions.TotalFilterOptions)
                {
                    m_options[Convert.ToInt32(eso, SmoApplication.DefaultCulture)] = value;
                }
                else
                {
                    SetScriptingPreference(eso, value);
                }
            }
        }

        private void SetScriptingPreference(EnumScriptOptions eso, bool value)
        {
            switch (eso)
            {
                case EnumScriptOptions.NoTablePartitioningSchemes:
                    this.NoTablePartitioningSchemes = value;
                    break;
                case EnumScriptOptions.NoIndexPartitioningSchemes:
                    this.NoIndexPartitioningSchemes = value;
                    break;
                case EnumScriptOptions.SchemaQualifyForeignKeysReferences:
                    this.SchemaQualifyForeignKeysReferences = value;
                    break;
                case EnumScriptOptions.IncludeDatabaseRoleMemberships:
                    this.IncludeDatabaseRoleMemberships = value;
                    break;
                case EnumScriptOptions.SchemaQualify:
                    this.SchemaQualify = value;
                    break;
                case EnumScriptOptions.IncludeHeaders:
                    this.IncludeHeaders = value;
                    break;
                case EnumScriptOptions.IncludeIfNotExists:
                    this.IncludeIfNotExists = value;
                    break;
                case EnumScriptOptions.WithDependencies:
                    this.WithDependencies = value;
                    break;
                case EnumScriptOptions.Bindings:
                    this.Bindings = value;
                    break;
                case EnumScriptOptions.ContinueScriptingOnError:
                    this.ContinueScriptingOnError = value;
                    break;
                case EnumScriptOptions.Permissions:
                    this.Permissions = value;
                    break;
                case EnumScriptOptions.AllowSystemObjects:
                    this.AllowSystemObjects = value;
                    break;
                case EnumScriptOptions.DriWithNoCheck:
                    this.DriWithNoCheck = value;
                    break;
                case EnumScriptOptions.ConvertUserDefinedDataTypesToBaseType:
                    this.ConvertUserDefinedDataTypesToBaseType = value;
                    break;
                case EnumScriptOptions.TimestampToBinary:
                    this.TimestampToBinary = value;
                    break;
                case EnumScriptOptions.AnsiPadding:
                    this.AnsiPadding = value;
                    break;
                case EnumScriptOptions.ExtendedProperties:
                    this.ExtendedProperties = value;
                    break;
                case EnumScriptOptions.DdlHeaderOnly:
                    this.DdlHeaderOnly = value;
                    break;
                case EnumScriptOptions.DdlBodyOnly:
                    this.DdlBodyOnly = value;
                    break;
                case EnumScriptOptions.NoViewColumns:
                    this.NoViewColumns = value;
                    break;
                case EnumScriptOptions.LoginSid:
                    this.LoginSid = value;
                    break;
                case EnumScriptOptions.IncludeDatabaseContext:
                    this.IncludeDatabaseContext = value;
                    break;
                case EnumScriptOptions.AgentAlertJob:
                    this.AgentAlertJob = value;
                    break;
                case EnumScriptOptions.AgentJobId:
                    this.AgentJobId = value;
                    break;
                case EnumScriptOptions.AgentNotify:
                    this.AgentNotify = value;
                    break;
                case EnumScriptOptions.DriIncludeSystemNames:
                    this.DriIncludeSystemNames = value;
                    break;
                case EnumScriptOptions.OptimizerData:
                    this.OptimizerData = value;
                    break;
                case EnumScriptOptions.NoExecuteAs:
                    this.NoExecuteAs = value;
                    break;
                case EnumScriptOptions.EnforceScriptingOptions:
                    this.EnforceScriptingOptions = value;
                    break;
                case EnumScriptOptions.NoVardecimal:
                    this.NoVardecimal = value;
                    break;
                case EnumScriptOptions.ScriptSchema:
                    this.ScriptSchema = value;
                    break;
                case EnumScriptOptions.ScriptData:
                    this.ScriptData = value;
                    break;
                case EnumScriptOptions.IncludeFullTextCatalogRootPath:
                    this.IncludeFullTextCatalogRootPath = value;
                    break;
                case EnumScriptOptions.ChangeTracking:
                    this.ChangeTracking = value;
                    break;
                case EnumScriptOptions.ScriptDataCompression:
                    this.ScriptDataCompression = value;
                    break;
                case EnumScriptOptions.ScriptXmlCompression:
                    this.ScriptXmlCompression = value;
                    break;
                case EnumScriptOptions.ScriptOwner:
                    this.ScriptOwner = value;
                    break;
                case EnumScriptOptions.NoFileGroup:
                    this.NoFileGroup = value;
                    break;
                case EnumScriptOptions.NoFileStream:
                    this.NoFileStream = value;
                    break;
                case EnumScriptOptions.NoFileStreamColumn:
                    this.NoFileStreamColumn = value;
                    break;
                case EnumScriptOptions.NoCollation:
                    this.NoCollation = value;
                    break;
                case EnumScriptOptions.NoIdentities:
                    this.NoIdentities = value;
                    break;
                case EnumScriptOptions.NoXmlNamespaces:
                    this.NoXmlNamespaces = value;
                    break;
                case EnumScriptOptions.NoMailProfileAccounts:
                    this.NoMailProfileAccounts = value;
                    break;
                case EnumScriptOptions.NoMailProfilePrincipals:
                    this.NoMailProfilePrincipals = value;
                    break;
                case EnumScriptOptions.PrimaryObject:
                    this.PrimaryObject = value;
                    break;
                default:
                    Diagnostics.TraceHelper.Assert(false, "incorrect index specified");
                    break;
            }
        }

        private bool GetScriptingPreference(EnumScriptOptions eso)
        {
            switch (eso)
            {
                case EnumScriptOptions.NoTablePartitioningSchemes:
                    return this.NoTablePartitioningSchemes;
                case EnumScriptOptions.NoIndexPartitioningSchemes:
                    return this.NoIndexPartitioningSchemes;
                case EnumScriptOptions.SchemaQualifyForeignKeysReferences:
                    return this.SchemaQualifyForeignKeysReferences;
                case EnumScriptOptions.IncludeDatabaseRoleMemberships:
                    return this.IncludeDatabaseRoleMemberships;
                case EnumScriptOptions.SchemaQualify:
                    return this.SchemaQualify;
                case EnumScriptOptions.IncludeHeaders:
                    return this.IncludeHeaders;
                case EnumScriptOptions.IncludeIfNotExists:
                    return this.IncludeIfNotExists;
                case EnumScriptOptions.WithDependencies:
                    return this.WithDependencies;
                case EnumScriptOptions.Bindings:
                    return this.Bindings;
                case EnumScriptOptions.ContinueScriptingOnError:
                    return this.ContinueScriptingOnError;
                case EnumScriptOptions.Permissions:
                    return this.Permissions;
                case EnumScriptOptions.AllowSystemObjects:
                    return this.AllowSystemObjects;
                case EnumScriptOptions.DriWithNoCheck:
                    return this.DriWithNoCheck;
                case EnumScriptOptions.ConvertUserDefinedDataTypesToBaseType:
                    return this.ConvertUserDefinedDataTypesToBaseType;
                case EnumScriptOptions.TimestampToBinary:
                    return this.TimestampToBinary;
                case EnumScriptOptions.AnsiPadding:
                    return this.AnsiPadding;
                case EnumScriptOptions.ExtendedProperties:
                    return this.ExtendedProperties;
                case EnumScriptOptions.DdlHeaderOnly:
                    return this.DdlHeaderOnly;
                case EnumScriptOptions.DdlBodyOnly:
                    return this.DdlBodyOnly;
                case EnumScriptOptions.NoViewColumns:
                    return this.NoViewColumns;
                case EnumScriptOptions.LoginSid:
                    return this.LoginSid;
                case EnumScriptOptions.IncludeDatabaseContext:
                    return this.IncludeDatabaseContext;
                case EnumScriptOptions.AgentAlertJob:
                    return this.AgentAlertJob;
                case EnumScriptOptions.AgentJobId:
                    return this.AgentJobId;
                case EnumScriptOptions.AgentNotify:
                    return this.AgentNotify;
                case EnumScriptOptions.DriIncludeSystemNames:
                    return this.DriIncludeSystemNames;
                case EnumScriptOptions.OptimizerData:
                    return this.OptimizerData;
                case EnumScriptOptions.NoExecuteAs:
                    return this.NoExecuteAs;
                case EnumScriptOptions.EnforceScriptingOptions:
                    return this.EnforceScriptingOptions;
                case EnumScriptOptions.NoVardecimal:
                    return this.NoVardecimal;
                case EnumScriptOptions.ScriptSchema:
                    return this.ScriptSchema;
                case EnumScriptOptions.ScriptData:
                    return this.ScriptData;
                case EnumScriptOptions.IncludeFullTextCatalogRootPath:
                    return this.IncludeFullTextCatalogRootPath;
                case EnumScriptOptions.ChangeTracking:
                    return this.ChangeTracking;
                case EnumScriptOptions.ScriptDataCompression:
                    return this.ScriptDataCompression;
                case EnumScriptOptions.ScriptXmlCompression:
                    return this.ScriptXmlCompression;
                case EnumScriptOptions.ScriptOwner:
                    return this.ScriptOwner;
                case EnumScriptOptions.NoFileGroup:
                    return this.NoFileGroup;
                case EnumScriptOptions.NoFileStream:
                    return this.NoFileStream;
                case EnumScriptOptions.NoFileStreamColumn:
                    return this.NoFileStreamColumn;
                case EnumScriptOptions.NoCollation:
                    return this.NoCollation;
                case EnumScriptOptions.NoIdentities:
                    return this.NoIdentities;
                case EnumScriptOptions.NoXmlNamespaces:
                    return this.NoXmlNamespaces;
                case EnumScriptOptions.NoMailProfileAccounts:
                    return this.NoMailProfileAccounts;
                case EnumScriptOptions.NoMailProfilePrincipals:
                    return this.NoMailProfilePrincipals;
                case EnumScriptOptions.PrimaryObject:
                    return this.PrimaryObject;
                default:
                    Diagnostics.TraceHelper.Assert(false, "incorrect index specified");
                    return false;
            }
        }

        /// <summary>
        /// A bit array that storeas all public options
        /// </summary>
        private BitArray m_options;

        #endregion

        #region Public Methods
        /// <summary>
        /// Method to convert the server version to SqlServerVersion
        /// </summary>
        /// <param name="majorVersion">server version</param>
        /// <returns></returns>
        public static SqlServerVersion ConvertVersion(System.Version version)
        {
            return ConvertToSqlServerVersion(version.Major, version.Minor);
        }

        /// <summary>
        /// Converts a <see cref="ServerVersion"/> into the equivalent <see cref="SqlServerVersion"/> value
        /// </summary>
        /// <param name="serverVersion"></param>
        /// <returns></returns>
        public static SqlServerVersion ConvertToSqlServerVersion(ServerVersion serverVersion)
        {
            return ConvertToSqlServerVersion(serverVersion.Major, serverVersion.Minor);
        }

        /// <summary>
        /// Converts a Major and Minor version number pair into the equivalent <see cref="SqlServerVersion"/>
        /// </summary>
        /// <param name="majorVersion"></param>
        /// <param name="minorVersion"></param>
        /// <returns></returns>
        /// <exception cref="SmoException">If the version numbers are invalid</exception>
        public static SqlServerVersion ConvertToSqlServerVersion(int majorVersion, int minorVersion)
        {
            SqlServerVersion sqlSvrVersion;
            switch (majorVersion)
            {
                case 8:
                    sqlSvrVersion = SqlServerVersion.Version80;
                    break;
                case 9:
                    sqlSvrVersion = SqlServerVersion.Version90;
                    break;
                case 10:
                    if (minorVersion == 0)
                    {
                        sqlSvrVersion = SqlServerVersion.Version100;
                    }
                    else
                    {
                        sqlSvrVersion = SqlServerVersion.Version105;
                    }
                    break;
                case 11:
                    sqlSvrVersion = SqlServerVersion.Version110;
                    break;
                case 12:
                    sqlSvrVersion = SqlServerVersion.Version120;
                    break;
                case 13:
                    sqlSvrVersion = SqlServerVersion.Version130;
                    break;
                case 14:
                    sqlSvrVersion = SqlServerVersion.Version140;
                    break;
                case 15:
                    sqlSvrVersion = SqlServerVersion.Version150;
                    break;
                case int n when n >= 16:
                    sqlSvrVersion = SqlServerVersion.Version160;
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.InvalidVersion(majorVersion.ToString()));
            }
            return sqlSvrVersion;
        }

        #endregion

        /// <summary>
        /// Returns Scripting Preferences corresponding to the ScriptingOptions
        /// </summary>
        /// <returns></returns>
        public ScriptingPreferences GetScriptingPreferences()
        {
            return this.scriptingPreferences;
        }

        /// <summary>
        /// Returns SmoUrnFilter corresponding to the ScriptingOptions for discovery
        /// </summary>
        /// <param name="srv"></param>
        /// <returns></returns>
        internal SmoUrnFilter GetSmoUrnFilterForDiscovery(Server srv)
        {
            SmoUrnFilter smoUrnFilter = new SmoUrnFilter(srv);

            if (!this.ExtendedProperties)
            {
                smoUrnFilter.AddFilteredType(ExtendedProperty.UrnSuffix, null);
            }

            if (!this.ScriptDriChecks())
            {
                smoUrnFilter.AddFilteredType(Check.UrnSuffix, null);
            }

            if (!this.ScriptDriDefaults())
            {
                smoUrnFilter.AddFilteredType(DefaultConstraint.UrnSuffix, Column.UrnSuffix);
            }

            if (!this.ScriptDriForeignKeys())
            {
                smoUrnFilter.AddFilteredType(ForeignKey.UrnSuffix, null);
            }

            if (!this.FullTextCatalogs)
            {
                smoUrnFilter.AddFilteredType(FullTextCatalog.UrnSuffix, null);
            }

            if (!this.FullTextIndexes)
            {
                smoUrnFilter.AddFilteredType(FullTextIndex.UrnSuffix, null);
            }

            if (!this.FullTextStopLists)
            {
                smoUrnFilter.AddFilteredType(FullTextStopList.UrnSuffix, null);
            }

            this.AddIndexFilters(smoUrnFilter);

            if (this.NoAssemblies)
            {
                smoUrnFilter.AddFilteredType(SqlAssembly.UrnSuffix, null);
            }

            if (!this.Statistics)
            {
                smoUrnFilter.AddFilteredType(Statistic.UrnSuffix, null);
            }

            if (!this.Triggers)
            {
                smoUrnFilter.AddFilteredType(Trigger.UrnSuffix, null);
            }

            return smoUrnFilter;
        }

        /// <summary>
        /// Returns SmoUrnFilter corresponding to the ScriptingOptions for discovery
        /// </summary>
        /// <param name="srv"></param>
        /// <returns></returns>
        internal static SmoUrnFilter GetAllFilters(Server srv)
        {
            SmoUrnFilter smoUrnFilter = new SmoUrnFilter(srv);
            smoUrnFilter.AddFilteredType(ExtendedProperty.UrnSuffix, null);
            smoUrnFilter.AddFilteredType(Check.UrnSuffix, null);
            smoUrnFilter.AddFilteredType(DefaultConstraint.UrnSuffix, Column.UrnSuffix);
            smoUrnFilter.AddFilteredType(ForeignKey.UrnSuffix, null);
            smoUrnFilter.AddFilteredType(FullTextCatalog.UrnSuffix, null);
            smoUrnFilter.AddFilteredType(FullTextIndex.UrnSuffix, null);
            smoUrnFilter.AddFilteredType(FullTextStopList.UrnSuffix, null);
            smoUrnFilter.AddFilteredType("XmlIndex", null);
            smoUrnFilter.AddFilteredType("SpatialIndex", null);
            smoUrnFilter.AddFilteredType("ColumnstoreIndex", null);
            smoUrnFilter.AddFilteredType("ClusteredIndex", null);
            smoUrnFilter.AddFilteredType("NonclusteredIndex", null);
            smoUrnFilter.AddFilteredType("ClusteredPrimaryKey", null);
            smoUrnFilter.AddFilteredType("NonclusteredPrimaryKey", null);
            smoUrnFilter.AddFilteredType("ClusteredUniqueKey", null);
            smoUrnFilter.AddFilteredType("NonclusteredUniqueKey", null);
            smoUrnFilter.AddFilteredType(SqlAssembly.UrnSuffix, null);
            smoUrnFilter.AddFilteredType(Statistic.UrnSuffix, null);
            smoUrnFilter.AddFilteredType(Trigger.UrnSuffix, null);

            return smoUrnFilter;
        }

        /// <summary>
        /// Returns SmoUrnFilter corresponding to the ScriptingOptions for filtering.
        /// </summary>
        /// <param name="srv"></param>
        /// <returns></returns>
        internal SmoUrnFilter GetSmoUrnFilterForFiltering(Server srv)
        {
            SmoUrnFilter smoUrnFilter = null;
            if (!this.PrimaryObject || this.NoAssemblies)
            {
                smoUrnFilter = new SmoUrnFilter(srv);

                if (!this.PrimaryObject)
                {
                    smoUrnFilter.AddFilteredType(Table.UrnSuffix, null);
                    smoUrnFilter.AddFilteredType(View.UrnSuffix, null);
                    smoUrnFilter.AddFilteredType(UserDefinedFunction.UrnSuffix, null);
                    smoUrnFilter.AddFilteredType(Agent.Job.UrnSuffix, null);
                }

                if (this.NoAssemblies)
                {
                    smoUrnFilter.AddFilteredType(SqlAssembly.UrnSuffix, null);
                }
            }
            return smoUrnFilter;
        }

        private void AddIndexFilters(SmoUrnFilter smoUrnFilter)
        {
            if (this.TargetDatabaseEngineType != DatabaseEngineType.Standalone)
            {
                smoUrnFilter.AddFilteredType("XmlIndex", null);
                smoUrnFilter.AddFilteredType("ColumnstoreIndex", null);
            }
            else if (this.TargetServerVersion < SqlServerVersion.Version110)
            {
                smoUrnFilter.AddFilteredType("ColumnstoreIndex", null);
            }

            if (!this.Indexes)
            {
                if (!this.SpatialIndexes)
                {
                    smoUrnFilter.AddFilteredType("SpatialIndex", null);
                }

                if (!this.ColumnStoreIndexes)
                {
                    smoUrnFilter.AddFilteredType("ColumnstoreIndex", null);
                }

                if (!this.ClusteredIndexes)
                {
                    //clustered columnstore index is default option for DW tables
                    //pre-fetched independent of what the user chose for scripting options.
                    if (this.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                    {
                        smoUrnFilter.AddFilteredType("ClusteredIndex", null);
                    }
                }

                if (!this.NonClusteredIndexes)
                {
                    smoUrnFilter.AddFilteredType("NonclusteredIndex", null);
                }

                if (!this.XmlIndexes)
                {
                    smoUrnFilter.AddFilteredType("XmlIndex", null);
                }


                if (!(this.DriIndexes || this.DriAll || this.DriAllConstraints || this.DriAllKeys))
                {
                    if (!this.DriPrimaryKey)
                    {
                        if (!this.DriClustered)
                        {
                            smoUrnFilter.AddFilteredType("ClusteredPrimaryKey", null);
                        }

                        if (!this.DriNonClustered)
                        {
                            smoUrnFilter.AddFilteredType("NonclusteredPrimaryKey", null);
                        }
                    }

                    if (!this.DriUniqueKeys)
                    {
                        if (!this.DriClustered)
                        {
                            smoUrnFilter.AddFilteredType("ClusteredUniqueKey", null);
                        }

                        if (!this.DriNonClustered)
                        {
                            smoUrnFilter.AddFilteredType("NonclusteredUniqueKey", null);
                        }
                    }
                }

            }
        }
    }
}

