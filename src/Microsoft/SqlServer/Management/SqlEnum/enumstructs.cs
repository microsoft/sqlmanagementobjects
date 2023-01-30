// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Smo
{
    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    enum DatabaseCategory
    {
        ///
        Published = 1,
        ///
        Subscribed = 2,
        ///
        MergePublished = 4,
        ///
        MergeSubscribed = 8
    }

    /// <summary>
    /// 
    /// </summary>
    public enum FileGrowthType
    {
        /// <summary>
        /// Default </summary>
        KB = 0,
        ///
        Percent = 1,
        /// <summary>
        /// the file is not supposed to grow</summary>
        None = 99
    }

    /// <summary>
    /// The enumeration specifies the attributes of the Index object
    /// </summary>
    public enum IndexKeyType
    {
        /// <summary>
        /// index is not a key
        /// </summary>
        None = 0,
        /// <summary>
        /// index is a primary key
        /// </summary>
        DriPrimaryKey = 1,
        /// <summary>
        /// index is a unique key constraint
        /// </summary>
        DriUniqueKey = 2
    }

    /// <summary>
    /// The enumeration specifies the type of the Index.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.IndexTypeConverter))]
    public enum IndexType
    {
        /// <summary>
        /// Clustered Index.
        /// </summary>
        [TsqlSyntaxString("CLUSTERED INDEX")]
        [LocDisplayName("Clustered")]
        ClusteredIndex = 0,
        /// <summary>
        /// Nonclustered Index.
        /// </summary>
        [TsqlSyntaxString("INDEX")]
        [LocDisplayName("NonClustered")]
        NonClusteredIndex = 1,
        /// <summary>
        /// Primary XML Index.
        /// </summary>
        [TsqlSyntaxString("PRIMARY XML INDEX")]
        [LocDisplayName("PrimaryXml")]
        PrimaryXmlIndex = 2,
        /// <summary>
        /// Secondary XML Index.
        /// </summary>
        [TsqlSyntaxString("XML INDEX")]
        [LocDisplayName("SecondaryXml")]
        SecondaryXmlIndex = 3,
        /// <summary>
        /// Spatial Index.
        /// </summary>
        [TsqlSyntaxString("SPATIAL INDEX")]
        [LocDisplayName("Spatial")]
        SpatialIndex = 4,
        /// <summary>
        /// Nonclustered Columnstore Index.
        /// </summary>
        [TsqlSyntaxString("NONCLUSTERED COLUMNSTORE INDEX")]
        [LocDisplayName("NonClusteredColumnStore")]
        NonClusteredColumnStoreIndex = 5,
        /// <summary>
        /// Nonclustered Hash Index.
        /// </summary> 
        [TsqlSyntaxString("NONCLUSTERED HASH INDEX")]
        [LocDisplayName("NonClusteredHash")]
        NonClusteredHashIndex = 6,
        /// <summary>
        /// Primary Selective Xml Index.
        /// </summary>
        [TsqlSyntaxString("SELECTIVE XML INDEX")]
        [LocDisplayName("SelectiveXml")]
        SelectiveXmlIndex = 7,
        /// <summary>
        /// Secondary Selective Xml Index.
        /// </summary>
        [TsqlSyntaxString("")]
        [LocDisplayName("SecondarySelectiveXml")]
        SecondarySelectiveXmlIndex = 8,
        /// <summary>
        /// Clustered Columnstore Index.
        /// </summary>
        [TsqlSyntaxString("CLUSTERED COLUMNSTORE INDEX")]
        [LocDisplayName("ClusteredColumnStore")]
        ClusteredColumnStoreIndex = 9,
        /// <summary>
        /// Heap Index.
        /// </summary>
        [TsqlSyntaxString("HEAP")]
        [LocDisplayName("Heap")]
        HeapIndex = 10
    }


    /// <summary>
    /// The enumeration specifies the durability type of hekaton tables
    /// </summary>
    public enum DurabilityType
    {
        /// <summary>
        /// Non-Durable (SCHEMA_ONLY)
        /// </summary>
        SchemaOnly = 0,

        /// <summary>
        /// Durable (SCHEMA_AND_DATA)
        /// </summary>
        SchemaAndData = 1
    }

    public enum GraphType
    {
        /// <summary>
        /// Not a graph column.
        /// </summary>
        None = 0,

        /// <summary>
        /// The graph internal identifier column.
        /// </summary>
        GraphId = 1,

        /// <summary>
        /// The graph internal identifier computed column.
        /// </summary>
        GraphIdComputed = 2,

        /// <summary>
        /// The graph edge from identifier column.
        /// </summary>
        GraphFromId = 3,

        /// <summary>
        /// The graph edge from object identifier column.
        /// </summary>
        GraphFromObjId = 4,

        /// <summary>
        /// The graph edge from computed column.
        /// </summary>
        GraphFromIdComputed = 5,

        /// <summary>
        /// The graph edge to identifier column.
        /// </summary>
        GraphToId = 6,

        /// <summary>
        /// The graph edge to object identifier column.
        /// </summary>
        GraphToObjId = 7,

        /// <summary>
        /// The graph edge to computed column.
        /// </summary>
        GraphToIdComputed = 8,
    }

    /// <summary>
    /// The enumeration specifies the lock escalation granularity
    /// </summary>
    public enum LockEscalationType
    {
        /// <summary>
        /// Enable lock escalation on a table level granularity
        /// </summary>
        Table = 0,
        /// <summary>
        /// Disable lock escalation on a table
        /// </summary>
        Disable = 1,
        /// <summary>
        /// Enable lock escalation for a partitioned table
        /// </summary>
        Auto = 2

    }

    /// <summary>
    /// The enumeration specifies a view is ledger view or non ledger view
    /// </summary>
    public enum LedgerViewType
    {
        /// <summary>
        /// Non ledger view
        /// </summary>
        NonLedgerView = 0,
        /// <summary>
        /// Ledger_view
        /// </summary>
        LedgerView = 1,
    }

    /// <summary>
    /// Types of ledger table
    /// </summary>
    public enum LedgerTableType
    {
        /// <summary>
        /// Non ledger table
        /// </summary>
        None = 0,
        /// <summary>
        /// History table of a system-versioned table
        /// Temporal table type which is History table
        /// </summary>
        HistoryTable = 1,
        /// <summary>
        /// Updatable ledger table
        /// </summary>
        UpdatableLedgerTable = 2,
        /// <summary>
        /// AppendOnly ledger table which is not a system-versioned table
        /// </summary>
        AppendOnlyLedgerTable = 3
    }

    /// <summary>
    /// Enumerates possible Pushdown options on External Datasource
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ExternalDataSourcePushdownOptionConverter))]
    public enum ExternalDataSourcePushdownOption
    {
        [TsqlSyntaxString("OFF")]
        Off = 0,

        [TsqlSyntaxString("ON")]
        On = 1
    }


    /// <summary>
    /// The enumeration specifies the external data source type for external tables
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ExternalDataSourceTypeConverter))]
    public enum ExternalDataSourceType
    {
        /// <summary>
        /// Hadoop external data source type; used by Polybase.
        /// </summary>
        [TsqlSyntaxString("HADOOP")]
        Hadoop = 0,

        /// <summary>
        /// Rdbms external data source type; used by Global Query (GQ).
        /// </summary>
        [TsqlSyntaxString("RDBMS")]
        Rdbms = 1,

        /// <summary>
        /// ShardMapManager external data source type; used by Global Query (GQ).
        /// </summary>
        [TsqlSyntaxString("SHARD_MAP_MANAGER")]
        ShardMapManager = 2,

        /// <summary>
        /// Azure blob storage for use with BULK INSERT or OPENROWSET
        /// </summary>
        [TsqlSyntaxString("BLOB_STORAGE")]
        BlobStorage = 5,

        /// <summary>
        /// PolyBase External Generics; used by PolyBase ODBC Generic Connectors.
        /// For PolyBase External Generics we remove TYPE from
        /// CREATE / ALTER EXTERNAL DATA SOURCE statement entirely.
        /// </summary>
        ExternalGenerics = 6
    }

    /// <summary>
    /// The enumeration specifies the external file format types
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ExternalFileFormatTypeConverter))]
    public enum ExternalFileFormatType
    {
        /// <summary>
        /// No reject type. Applicable for GQ external tables.
        /// </summary>
        None = -1,

        /// <summary>
        /// Delimited text file format.
        /// </summary>
        [TsqlSyntaxString("DELIMITEDTEXT")]
        DelimitedText = 0,

        /// <summary>
        /// RCFILE file format.
        /// </summary>
        [TsqlSyntaxString("RCFILE")]
        RcFile = 1,

        /// <summary>
        /// ORC file format.
        /// </summary>
        [TsqlSyntaxString("ORC")]
        Orc = 2,

        /// <summary>
        /// Parquet file format.
        /// </summary>
        [TsqlSyntaxString("PARQUET")]
        Parquet = 3,
        
        /// <summary>
        /// JSON file format. 
        /// </summary>
        [TsqlSyntaxString("JSON")]
        JSON = 4,
        
        /// <summary>
        /// DELTA file format. 
        /// </summary>
        [TsqlSyntaxString("DELTA")]
        Delta = 5
    }

    /// <summary>
    /// The enumeration specifies the external streaming job status types
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ExternalStreamingJobStatusTypeConverter))]
    public enum ExternalStreamingJobStatusType
    {
        /// <summary>
        ///  The streaming job was created, but has not yet been started.
        /// </summary>
        [TsqlSyntaxString("CREATED")]
        Created = 0,

        /// <summary>
        /// The streaming job is in the starting phase.
        /// </summary>
        [TsqlSyntaxString("STARTING")]
        Starting = 1,

        /// <summary>
        /// The streaming job is in the stopping phase.
        /// </summary>
        [TsqlSyntaxString("STOPPING")]
        Stopping = 2,

        /// <summary>
        /// The streaming job Failed. This is generally an indication of a fatal error during processing.
        /// </summary>
        [TsqlSyntaxString("FAILED")]
        Failed = 6,

        /// <summary>
        /// The streaming job has been stopped.
        /// </summary>
        [TsqlSyntaxString("STOPPED")]
        Stopped = 4,

        /// <summary>
        ///  The streaming job is running, however there is no input to process.
        /// </summary>
        [TsqlSyntaxString("IDLE")]
        Idle = 7,

        /// <summary>
        /// The streaming job is running, and is processing inputs. This state indicates a healthy state for the streaming job.
        /// </summary>
        [TsqlSyntaxString("PROCESSING")]
        Processing = 8,

        /// <summary>
        /// The streaming job is running, however there were some non-fatal input/output serialization/de-serialization errors during input processing. The input job will continue to run, but will drop inputs that encounter errors.
        /// </summary>
        [TsqlSyntaxString("DEGRADED")]
        Degraded = 9
    }

    /// <summary>
    /// The enumeration specifies the external table reject types
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ExternalTableRejectTypeConverter))]
    public enum ExternalTableRejectType
    {
        /// <summary>
        /// Value reject type format.  This is the default.
        /// </summary>
        [TsqlSyntaxString("VALUE")]
        Value = 0,

        /// <summary>
        /// Percentage reject type format.
        /// </summary>
        [TsqlSyntaxString("PERCENTAGE")]
        Percentage = 1,

        /// <summary>
        /// No reject type. Applicable for GQ and PolyBase external generics tables.
        /// </summary>
        None = 255
    }

    /// <summary>
    /// The enumeration specifies the external table distribution. Valid for tables
    /// with ShardMapManager external data sources.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ExternalTableDistributionConverter))]
    public enum ExternalTableDistributionType
    {
        /// <summary>
        /// External table has SHARDED distribution.
        /// </summary>
        [TsqlSyntaxString("SHARDED")]
        Sharded = 0,

        /// <summary>
        /// External table has REPLICATED distribution.
        /// </summary>
        [TsqlSyntaxString("REPLICATED")]
        Replicated = 1,

        /// <summary>
        /// External table has ROUND_ROBIN distribution.
        /// </summary>
        [TsqlSyntaxString("ROUND_ROBIN")]
        RoundRobin = 2,

        /// <summary>
        /// External table doesn't have a distribution.
        /// </summary>
        None = 255
    }

    /// <summary>
    /// The enumeration specifies the SQL DW distributed table distribution types.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.DwTableDistributionConverter))]
    public enum DwTableDistributionType
    {
        /// <summary>
        /// A table distribution is undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// A table doesn't have a distribution.
        /// </summary>
        None = 1,

        /// <summary>
        /// SQL DW table has a HASH distribution.
        /// </summary>
        [TsqlSyntaxString("HASH")]
        Hash = 2,

        /// <summary>
        /// SQL DW table has a REPLICATE distribution.
        /// </summary>
        [TsqlSyntaxString("REPLICATE")]
        Replicate = 3,

        /// <summary>
        /// SQL DW table has a ROUND_ROBIN distribution.
        /// </summary>
        [TsqlSyntaxString("ROUND_ROBIN")]
        RoundRobin = 4
    }

    /// <summary>
    /// The enumeration specifies the SQL DW materialized view distribution types.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.DwViewDistributionConverter))]
    public enum DwViewDistributionType
    {
        /// <summary>
        /// A view distribution is undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// SQL DW view has a HASH distribution.
        /// </summary>
        [TsqlSyntaxString("HASH")]
        Hash = 2,

        /// <summary>
        /// SQL DW View has a ROUND_ROBIN distribution.
        /// </summary>
        [TsqlSyntaxString("ROUND_ROBIN")]
        RoundRobin = 4
    }

    /// <summary>
    /// The Spatial Index type for the Spatial Indices
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum SpatialIndexType
    {
        /// <summary>
        /// index is not a spatial
        /// </summary>
        None = 0,
        /// <summary>
        /// Geometric Spatial Index Type
        /// </summary>
        GeometryGrid = 1,
        /// <summary>
        /// Geography Grid Spatial Index Type
        /// </summary>
        GeographyGrid = 2,
        /// <summary>
        /// Geometry Auto Grid Spatial Index Type
        /// </summary>
        GeometryAutoGrid = 3,
        /// <summary>
        /// Geography Auto Grid Spatial Index Type
        /// </summary>
        GeographyAutoGrid = 4
    }

    /// <summary>
    /// The Spatial Geo Level Sizes
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum SpatialGeoLevelSize
    {
        /// <summary>
        /// Index is not spatial
        /// </summary>
        None = 0,
        /// <summary>
        /// Low Size
        /// </summary>
        Low = 16,
        /// <summary>
        /// Medium
        /// </summary>
        Medium = 64,
        /// <summary>
        /// High
        /// </summary>
        High = 256
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum PermissionState
    {
        ///
        Deny = 68,
        ///
        Revoke = 82,
        ///
        Grant = 71,
        ///
        GrantWithGrant = 87
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum BackupDeviceType
    {
        /// <summary>
        /// Reserved for future use. </summary>
        CDRom = 7,
        /// <summary>
        /// Device is a disk file. </summary>
        Disk = 2,
        /// <summary>
        /// Device is a disk file created on removable media in the A drive. </summary>
        FloppyA = 3,
        /// <summary>
        /// Device is a disk file created on removable media in the B drive. </summary>
        FloppyB = 4,
        /// <summary>
        /// Device identifies a named pipe. </summary>
        Pipe = 6,
        /// <summary>
        /// Device is a tape. </summary>
        Tape = 5,
        /// <summary>
        /// Device is a URL. </summary>
        Url = 9,
        /// <summary>
        /// Bad or invalid device type. </summary>
        Unknown = 100,
    }

    ///
    public enum UserDefinedFunctionType
    {
        ///
        Inline = 3,
        ///
        Scalar = 1,
        ///
        Table = 2,
        ///
        Unknown = 0
    }

    ///
    public enum ServerLoginMode
    {
        ///
        Normal = 0,
        ///
        Integrated = 1,
        ///
        Mixed = 2,
        ///
        Unknown = 9
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum LockRequestStatus
    {
        ///
        Granted = 1,
        ///
        Converting = 2,
        ///
        Waiting = 3
    }

    /// <summary>
    ///enum values are aranget to mach sys.databases.recovery_option</summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.RecoveryModelConverter))]
    public enum RecoveryModel
    {
        ///
        [LocDisplayName("rmSimple")]
        Simple = 3,

        ///
        [LocDisplayName("rmBulkLogged")]
        BulkLogged = 2,

        ///
        [LocDisplayName("rmFull")]
        Full = 1
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    internal enum CategoryClass
    {
        ///
        Job = 1,
        ///
        Alert = 2,
        ///
        Operator = 3
    }

    ///
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum MediaTypes
    {
        /// <summary>
        /// Default. List all media. </summary>
        All = 15,
        /// <summary>
        /// List visible CD-ROM devices. </summary>
        CDRom = 8,
        /// <summary>
        /// List visible fixed disk drive devices. </summary>
        FixedDisk = 2,
        /// <summary>
        /// List visible floppy disk drive devices. </summary>
        Floppy = 1,
        /// <summary>
        /// List visible fixed disk drive devices shared on a clustered computer. </summary>
        SharedFixedDisk = 16,
        /// <summary>
        /// List visible tape devices. </summary>
        Tape = 4
    }

    /// Specifies the type of Login.
    public enum LoginType
    {
        /// <summary>
        ///Login is based on Windows User.
        /// </summary>
        WindowsUser = 0,
        /// <summary>
        ///Login is based on Windows Group.
        /// </summary>
        WindowsGroup = 1,
        /// <summary>
        ///Login is based on SQL login.
        /// </summary>
        SqlLogin = 2,
        /// <summary>
        ///Login is based on certificate.
        /// </summary>
        Certificate = 3,
        /// <summary>
        ///Login is based on asymmetric key.
        /// </summary>
        AsymmetricKey = 4,
        /// <summary>
        /// Login is based on External User
        /// </summary>
        ExternalUser = 5,
        /// <summary>
        ///Login is based on External Group
        /// </summary>
        ExternalGroup = 6
    }

    /// <summary>
    /// Specifies the type of user in a database.
    /// </summary>
    public enum UserType
    {
        /// <summary>
        /// Specifies a SQLLogin user. This is deprecated, use SQLUser instead.
        /// </summary>
        SqlLogin = 0,
        /// <summary>
        /// Specifies that the user is either a SQLLogin user or a user with password.
        /// </summary>
        SqlUser = 0,
        /// <summary>
        /// Specifies that the user login for the database is based on a certificate.
        /// </summary>
        Certificate = 1,
        /// <summary>
        /// Specifies that the user login for the database is based on an asymmetric key.
        /// </summary>
        AsymmetricKey = 2,
        /// <summary>
        /// Specifies that the user does not have a login for the database.
        /// </summary>
        NoLogin = 3,
        /// <summary>
        /// Specifies that the user is based on external authentication
        /// </summary>
        External = 4
    }

    ///
    public enum WindowsLoginAccessType
    {
        /// <summary>
        ///This login has explicit deny permissions to access this server. </summary>
        Deny = 2,
        /// <summary>
        ///This login has explicit grant permissions to access this server. </summary>
        Grant = 1,
        /// <summary>
        ///The login is a standard SQL Server login; the property does not apply. </summary>
        NonNTLogin = 99,
        /// <summary>
        ///The login has not been explicitly granted or denied permissions to access this server. </summary>
        ///The login may still have access through a group membership, but this is not recorded as a login property. 
        Undefined = 0
    }

    /// <summary>
    ///enum values are aranget to mach sys.databases.user_access</summary>
    public enum DatabaseUserAccess
    {
        /// <summary>
        /// only one db_owner, dbcreator, or sysadmin user at a time</summary>
        Single = 1,
        /// <summary>
        /// only members of db_owner, dbcreator, and sysadmin roles</summary>
        Restricted = 2,
        /// <summary>
        /// all users</summary>
        Multiple = 0
    }

    /// <summary>
    /// Specifies the type of Authentication supported by Cryptographic Provider
    /// </summary>
    public enum ProviderAuthenticationType
    {
        /// <summary>
        /// Windows Authentication
        /// </summary>
        Windows = 0,

        /// <summary>
        /// Basic Authentication
        /// </summary>
        Basic = 1,

        /// <summary>
        /// Other
        /// </summary>
        Other = 2
    }

    /// <summary>
    /// Specifies the class to which a Credential is mapped
    /// </summary>
    public enum MappedClassType
    {
        /// <summary>
        /// Credential mapped to none
        /// </summary>
        None = 0,

        /// <summary>
        /// Credential mapped to a cryptographic provider
        /// </summary>
        CryptographicProvider = 1
    }

    /// <summary>
    /// Specifies the Collation Version
    /// </summary>
    public enum CollationVersion
    {
        ///
        Version80 = 0,
        ///
        Version90 = 1,
        /// 
        Version100 = 2,
        ///
        Version105 = 3,
        ///
        Version110 = 4,
        ///
        Version120 = 5,
        /// <summary>
        /// Collation for SQL 2016
        /// </summary>
        Version130 = 6,
        /// <summary>
        /// Collation for SQL 2017
        /// </summary>
        Version140 = 7,
        /// <summary>
        /// Collation for SQL 2019
        /// </summary>
        Version150 = 8,
        /// <summary>
        /// Collation for SQL v160
        /// </summary>
        Version160 = 9

    }

    /// <summary>
    /// EncryptionType for a column encrypted with TCE
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ColumnEncryptionType
    {
        /// <summary>
        /// Deterministic
        /// </summary>
        Deterministic = 1,
        /// <summary>
        /// Randomized
        /// </summary>
        Randomized = 2
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum CompatibilityLevel
    {
        ///
        Version60 = 60,
        ///
        Version65 = 65,
        ///
        Version70 = 70,
        ///
        Version80 = 80,
        ///
        Version90 = 90,
        /// 
        Version100 = 100,
        /// 
        Version110 = 110,
        ///
        Version120 = 120,
        /// <summary>
        /// Compatibility level for SQL 15
        /// </summary>
        Version130 = 130,
        /// <summary>
        /// Compatibility level for SQL 2017
        /// </summary>
        Version140 = 140,
        /// <summary>
        /// Compatibility level for SQL 2019
        /// </summary>
        Version150 = 150,
        /// <summary>
        /// Compatibility level for SQL v160
        /// </summary>
        Version160 = 160
    }

    ///
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum DatabaseStatus
    {
        /// <summary>
        ///Referenced database is available for use (Online).</summary>
        Normal = 0x000001,
        /// <summary>
        ///Database restore is underway on the referenced database.</summary>
        Restoring = 0x000002,
        /// <summary>
        ///Database recovery is being prepared for the referenced database.</summary>
        RecoveryPending = 0x000004,
        /// <summary>
        ///Database recovery is underway on the referenced database.</summary>
        Recovering = 0x000008,
        /// <summary>
        ///Database integrity is suspect for the referenced database.</summary>
        Suspect = 0x000010,
        /// <summary>
        ///Referenced database has been placed offline by a system or user action.</summary>
        Offline = 0x000020,
        /// <summary>
        ///Referenced database defined on a standby server.</summary>
        Standby = 0x000040,
        /// <summary>
        ///Database is in Shutdown</summary>
        Shutdown = 0x000080,
        /// <summary>
        ///Emergency mode has been initiated on the referenced database.</summary>
        EmergencyMode = 0x000100,
        /// <summary>
        ///The database has been autoclosed.</summary>
        AutoClosed = 0x000200,
        /// <summary>
        ///Property value that may be used for bitwisee AND operation to determine accessibility of the database (Restoring | Offline | Suspect | Recovering | RecoveryPending).</summary>
        Inaccessible = Restoring | Offline | Suspect | Recovering | RecoveryPending
    }

    ///
    public enum SnapshotIsolationState
    {
        /// <summary>
        ///Snapshot isolation is disabled.</summary>
        Disabled = 0,
        /// <summary>
        ///Snapshot isolation is enabled.</summary>
        Enabled = 1,
        /// <summary>
        ///Snapshot isolation is in process of being disabled.      </summary>
        PendingOff = 2,
        /// <summary>
        ///Snapshot isolation is in process of being enabled.       </summary>
        PendingOn = 3
    }

    /// <summary>
    /// Specifies the delayed durability option of the database.
    /// </summary>
    public enum DelayedDurability
    {
        /// <summary>
        /// Delayed durability is disabled.
        ///</summary>
        Disabled = 0,
        /// <summary>
        /// Delayed durability is allowed.
        ///</summary>
        Allowed = 1,
        /// <summary>
        /// Delayed durability is forced.      
        ///</summary>
        Forced = 2
    }

    /// <summary>
    /// The RangeType  enum specifies whether the boundary values specified with 
    /// RangeValues are placed in the LEFT or RIGHT side of the interval. None corresponds
    /// to no boundary condition. Table with no partition is suitable example. 
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.RangeTypeConverter))]
    public enum RangeType
    {
        /// <summary>
        /// RangeType is not set. 
        /// </summary>
        [TsqlSyntaxString("")]
        None = -1,
        /// <summary>
        /// Boundary values belong to left side of the interval.
        /// </summary>
        [TsqlSyntaxString("LEFT")]
        Left = 0,
        /// <summary>
        /// Boundary values belong to right side of the interval.
        /// </summary>
        [TsqlSyntaxString("RIGHT")]
        Right = 1
    }

    /// <summary>
    /// DataCompressionType describe about the compression status of a table/index partition.
    /// None means no compression, Row means compression row wise,Page means compression
    /// applied page wise and ColumnStore is compression columnstore wise
    /// </summary>
    public enum DataCompressionType
    {
        /// <summary>
        /// No data compression for this object
        /// </summary>
        None = 0,

        /// <summary>
        /// Row compression is set for this object
        /// </summary>
        Row = 1,

        /// <summary>
        /// Page compression is set for this object
        /// </summary>
        Page = 2,

        /// <summary>
        /// ColumnStore compression, default for columnstores
        /// </summary>
        ColumnStore = 3,

        /// <summary>
        /// ColumnStore archival compression mode
        /// </summary>
        ColumnStoreArchive = 4
    }

    /// <summary>
    /// XmlCompressionType describe about the compression status of a table with xml datatype
    /// column or xml index partition.
    /// Off means no xml compression, On means xml compression is enabled
    /// </summary>
    public enum XmlCompressionType
    {
        /// <summary>
        /// Xml compression option doesn't exist
        /// </summary>
        Invalid = -1,

        /// <summary>
        /// No xml compression for this object
        /// </summary>
        Off = 0,

        /// <summary>
        /// xml compression is set for this object
        /// </summary>
        On = 1
    }

    /// <summary>
    /// Specifies the ABORT_AFTER_WAIT option of a DDL operation.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AbortAfterWaitConverter))]
    public enum AbortAfterWait
    {
        /// <summary>
        /// Abort none.
        /// </summary>
        [TsqlSyntaxString("NONE")]
        None,

        /// <summary>
        /// Abort blocking user transactions.
        /// </summary>
        [TsqlSyntaxString("BLOCKERS")]
        Blockers,

        /// <summary>
        /// Abort the DDL.
        /// </summary>
        [TsqlSyntaxString("SELF")]
        Self
    }

    /// <summary>
    /// Specifies the access rights for an Assembly.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum AssemblySecurityLevel
    {
        /// <summary>
        /// Safe access only.</summary>
        Safe = 1,
        /// <summary>
        /// External access allowed.</summary>
        External = 2,
        /// <summary>
        /// Unrestricted access allowed</summary>
        Unrestricted = 3
    }

    /// <summary>
    /// Specifies the content type for an external library installation or alteration.
    /// </summary>
    public enum ExternalLibraryContentType
    {
        Binary = 0,
        Path
    }

    /// <summary>
    /// Specifies the platform language was created for.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ExternalLanguageFilePlatform))]
    public enum ExternalLanguageFilePlatform
    {
        /// <summary>
        /// Platform was not specified
        /// </summary>
        [TsqlSyntaxString("NONE")]
        Default = 0,

        /// <summary>
        /// Was created for Windows
        /// </summary>
        [TsqlSyntaxString("WINDOWS")]
        Windows = 1,

        /// <summary>
        /// Was created for Linux
        /// </summary>
        [TsqlSyntaxString("LINUX")]
        Linux = 2
    }

    /// <summary>
    /// Specifies the user context in which assembly code will run.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ExecutionContext
    {
        /// <summary>
        /// Run in context of caller.</summary>
        Caller = 1,
        /// <summary>
        /// Run as owner of assembly.</summary>
        Owner = 2,
        /// <summary>
        /// Run as specified user with ExecuteAsUser property.</summary>
        ExecuteAsUser = 3,
        /// <summary>
        /// shortcut for ExecuteAsUser. resolves to user doing the alter/create</summary>
        Self = 4
    }

    /// <summary>
    /// Specifies the user context in which assembly code will run.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public enum ServerDdlTriggerExecutionContext
    {
        /// <summary>
        /// Run in context of caller.</summary>
        Caller = 1,
        /// <summary>
        /// Run as specified login with ExecuteAsLogin property.</summary>
        ExecuteAsLogin = 2,
        /// <summary>
        /// shortcut for ExecuteAsUser. resolves to user doing the alter/create</summary>
        Self = 3
    }

    /// <summary>
    /// Specifies the user context in which assembly code will run.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public enum DatabaseDdlTriggerExecutionContext
    {
        /// <summary>
        /// Run in context of caller.</summary>
        Caller = 1,
        /// <summary>
        /// Run as specified login with ExecuteAsLogin property.</summary>
        ExecuteAsUser = 2,
        /// <summary>
        /// shortcut for ExecuteAsUser. resolves to user doing the alter/create</summary>
        Self = 3
    }

    /// <summary>
    /// Specifies the user context for objects activation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ActivationExecutionContext
    {
        ///
        Owner = 2,
        ///
        ExecuteAsUser = 3,
        ///
        Self = 4
    }


    ///
    [Flags]
    public enum AssemblyAlterOptions
    {
        /// <summary>
        /// No options specified.</summary>
        None = 0,
        /// TBD
        NoChecks = 2
    }

    /// <summary>
    /// Specifies the implementation type of a StoredProcedure, UserDefinedFunction,, and other objects.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ImplementationType
    {
        /// <summary>
        /// Implmemented with Transact-SQL.</summary>
        TransactSql = 1,
        /// <summary>
        /// Implemented with a SQL CLR type.</summary>
        SqlClr = 2
    }

    /// <summary>
    /// Specifies how a UDT is stored.
    /// </summary>
    public enum UserDefinedTypeFormat
    {
        /// <summary>
        /// Store in native format.</summary>
        Native = 0,
        /// <summary>
        /// Store in user defined format.</summary>
        UserDefined = 1,
        /// <summary>
        /// Compute serialized format once for this type.</summary>
        SerializedData = 2,
        /// <summary>
        /// Store serialized format with each instance.</summary>
        SerializedDataWithMetadata = 3
    }

    ///<summary>
    /// Specifies the available system resources for the MSSearch Service.</summary>
    public enum ResourceUsage
    {
        ///
        Unknown = 0,
        /// <summary>
        /// Lowest priority. Fulltext population may take a long time.</summary>
        Background = 1,
        /// <summary>
        /// Below normal priority. Less impact on other processes, prolong population.</summary>
        BelowNormal = 2,
        /// <summary>
        /// Normal priority.</summary>
        Normal = 3,
        /// <summary>
        /// Above normal. Other processes may run slower.</summary>
        AboveNormal = 4,
        /// <summary>
        /// Highest priority. Other processes may not be given any time if FullText is active.</summary>
        Dedicated = 5
    }

    ///<summary>
    /// Specifies the Full Text Catalog Upgrade options</summary>
    public enum FullTextCatalogUpgradeOption
    {
        /// <summary>
        /// This will always rebuild the FullText Indexes as part of upgrade.</summary>
        AlwaysRebuild = 0,
        /// <summary>
        /// This will get the metadata upgraded and reset the FullText index so user can rebuild at latter time.</summary>
        AlwaysReset = 1,
        /// <summary>
        /// This will import the metadata and indexes for the available FullText catalogs and for unavailable ones it will rebuild the indexes.</summary>
        ImportWithRebuild = 2
    }

    ///<summary>
    /// Specifies the Stoplist options associated with a FullText Index.</summary>
    public enum StopListOption
    {
        /// <summary>
        ///  This will not associate any fulltext stoplist with the specified FullText index.</summary>
        Off = 0,
        /// <summary>
        ///  This will associate system default stoplist with the specified FullText index.</summary>
        System = 1,
        /// <summary>
        ///  This will associate a stoplist with the specified FullText index.</summary>
        Name = 2
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum RestoreType
    {
        ///
        Database = 1,
        ///
        File = 2,
        ///
        FileGroup = 3,
        ///
        VerifyOnly = 4
    }

    /// <summary>
    /// Specifies the population state of a Microsoft Search fulltext catalog.
    /// </summary>
    public enum CatalogPopulationStatus
    {
        /// <summary>
        /// No action is performed against the referenced fulltext catalog.</summary>
        Idle = 0,
        /// <summary>
        /// Fulltext index population is in progress for the referenced fulltext catalog.</summary>
        CrawlinProgress = 1,
        /// <summary>
        /// Lack of available resource, such as disk space, has caused an interruption.</summary>
        Paused = 2,
        /// <summary>
        /// Search service has paused the referenced fulltext index population.</summary>
        Throttled = 3,
        /// <summary>
        /// Interrupted population on the referenced fulltext catalog is resuming.</summary>
        Recovering = 4,
        /// <summary>
        /// The referenced fulltext catalog is being deleted or not otherwise accessible.</summary>
        Shutdown = 5,
        /// <summary>
        /// Incremental index population is in progress for the referenced fulltext catalog.</summary>
        Incremental = 6,
        /// <summary>
        /// Referenced fulltext catalog is being assembled by the Search service.</summary>
        UpdatingIndex = 7,
        /// <summary>
        /// Lack of available disk space has caused an interruption.</summary>
        DiskFullPause = 8,
        /// <summary>
        /// Fulltext catalog is processing notifications.</summary>
        Notification = 9
    }

    /// <summary>
    /// Specifies catalog population action.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum CatalogPopulationAction
    {
        /// <summary>
        /// Full population.</summary>
        Full = 1,
        /// <summary>
        /// Incremental population.</summary>
        Incremental = 2
    }

    /// <summary>
    /// Specifies index population action.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum IndexPopulationAction
    {
        /// <summary>
        /// Full population.</summary>
        Full = 1,
        /// <summary>
        /// Incremental population.</summary>
        Incremental = 2,
        ///
        Update = 3
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum BackupSetFlag
    {
        ///
        MinimalLogData = 1,
        ///
        WithSnapshot = 2,
        ///
        ReadOnlyDatabase = 4,
        ///
        SingleUserModeDatabase = 8
    }



    /// <summary>
    /// Backupset type :
    /// Full Database, Differential, Log, File or Filegroup
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum BackupSetType
    {
        /// <summary>
        /// Full database backupset.
        /// </summary>
        Database = 1,
        /// <summary>
        /// Differential backupset.
        /// </summary>
        Differential = 2,
        /// <summary>
        /// Differential backupset. 
        /// MSSQL doesn't support incremental backup.
        /// (present here just for backward compatibility.)
        /// </summary>
        Incremental = 2,
        /// <summary>
        /// T-Log backupset.
        /// </summary>
        Log = 3,
        /// <summary>
        /// File or Filegroup backupset.
        /// </summary>
        FileOrFileGroup = 4,
        /// <summary>
        /// File or Filegroup differential backupset
        /// </summary>
        FileOrFileGroupDifferential = 5
    }

    /// <summary>
    /// Specifies the population state of a full-text table index.
    /// </summary>
    public enum IndexPopulationStatus
    {
        /// <summary>
        /// No population</summary>
        None = 0,
        /// <summary>
        /// Full population</summary>
        Full = 1,
        /// <summary>
        /// Incremental population</summary>
        Incremental = 2,
        /// <summary>
        /// Manual CT push in progress</summary>
        Manual = 3,
        /// <summary>
        /// Background CT push in progress</summary>
        Background = 4,
        /// <summary>
        /// Paused or Throttled</summary>
        PausedOrThrottled = 5
    }

    ///
    public enum ChangeTracking
    {
        ///
        Off = 0,
        ///
        Automatic = 2,
        ///
        Manual = 1
    }

    /// <summary>
    /// The ReplicationOptions enum specifies the active replication settings for a database,</summary>
    [Flags]
    public enum ReplicationOptions
    {
        /// <summary>
        /// No options
        /// </summary>
        None = 0,
        /// <summary>
        /// Database is published.</summary>
        Published = 1,
        /// <summary>
        /// Database has subscription.</summary>
        Subscribed = 2,
        /// <summary>
        /// Database is merge published.</summary>
        MergePublished = 4,
        /// <summary>
        ///Database has merge subscription.</summary>
        MergeSubscribed = 8
    }

    /// <summary>
    ///     Specifies the type of the server or database principal</summary>
    public enum PrincipalType
    {
        /// <summary>
        ///None.</summary>
        None = -1,
        /// <summary>
        ///Login.</summary>
        Login = 0,
        /// <summary>
        ///Server role.</summary>
        ServerRole = 1,
        /// <summary>
        ///User.</summary>
        User = 2,
        /// <summary>
        ///Database role.</summary>
        DatabaseRole = 3,
        /// <summary>
        ///Application role.</summary>
        ApplicationRole = 4
    }

    /// <summary>
    ///     Specifies the type of encryption of a key</summary>
    public enum PrivateKeyEncryptionType
    {
        /// <summary>
        ///Returned when certificate does not have a key.</summary>
        NoKey = 0,
        /// <summary>
        ///Enrypted with master key.</summary>
        MasterKey = 1,
        /// <summary>
        ///User password.</summary>
        Password = 2,
        /// <summary>
        /// Encryption by provider.
        /// </summary>
        Provider = 3
    }

    /// <summary>
    ///     Specifies the algorithm used to encrypt a (symmetric) key</summary>
    public enum SymmetricKeyEncryptionAlgorithm
    {
        /// Encryption using Cryptographic Provider
        CryptographicProviderDefined = -1,
        /// in DDL: RC2
        RC2 = 0,
        /// in DDL: RC4
        RC4 = 1,
        /// in DDL: Des
        Des = 2,
        /// in DDL: TripleDes
        TripleDes = 3,
        /// in DDL: DesX
        DesX = 4,
        /// in DDL: Aes_128
        Aes128 = 5,
        /// in DDL: Aes_192
        Aes192 = 6,
        /// in DDL: Aes_256
        Aes256 = 7,
        ///in DDL: TRIPLE_DES_3KEY
        TripleDes3Key = 8

    }

    ///<summary>
    /// Specifies the algorithm used to encrypt a (asymmetric) key.
    ///</summary>
    public enum AsymmetricKeyEncryptionAlgorithm
    {
        /// Encryption using Cryptographic Provider
        CryptographicProviderDefined = -1,
        /// in DDL: RSA_512
        Rsa512 = 0,
        /// in DDL: RSA_1024
        Rsa1024 = 1,
        /// in DDL: RSA_2048
        Rsa2048 = 2,
        /// in DDL: RSA_3072
        Rsa3072 = 3,
        /// in DDL: RSA_4096
        Rsa4096 = 4,
    }

    /// <summary>
    /// Contains the values of CREATION_DISPOSITION option
    /// </summary>
    public enum CreateDispositionType
    {
        /// <summary>
        /// Create New.
        /// </summary>
        CreateNew = 1,
        /// <summary>
        /// Open Existing.
        /// </summary>
        OpenExisting = 2
    }

    /// <summary>
    ///     Specifies the type of encryption of a key.</summary>
    public enum SymmetricKeyEncryptionType
    {
        /// <summary>
        /// Encrypted by symmetric key.         </summary>
        SymmetricKey = 0,
        /// <summary>
        /// Encrypted by certificate   </summary>
        Certificate = 1,
        /// <summary>
        /// Encrypted by password</summary>
        Password = 2,
        /// <summary>
        /// Encrypted by asymmetric key.
        /// </summary>
        AsymmetricKey = 3,
        /// <summary>
        /// Encrypted by master key.
        /// </summary>
        MasterKey = 4
    }

    /// <summary>
    /// Specifies the algorithm used to encrypt the database encryption key
    /// </summary>
    public enum DatabaseEncryptionAlgorithm
    {
        /// <summary>
        /// Aes128 Algorithm
        /// </summary>
        Aes128 = 0,
        /// <summary>
        /// Aes192 Algorithm
        /// </summary>
        Aes192 = 1,
        /// <summary>
        /// Aes256 Algorithm
        /// </summary>
        Aes256 = 2,
        /// <summary>
        /// TripleDes Algorithm
        /// </summary>
        TripleDes = 3
    }


    /// <summary>
    /// Specifies the type of base object of a synonym.
    /// </summary>
    public enum SynonymBaseType
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Table
        /// </summary>
        Table = 1,
        /// <summary>
        /// View
        /// </summary>
        View = 2,
        /// <summary>
        /// Sql Stored Procedure
        /// </summary>
        SqlStoredProcedure = 3,
        /// <summary>
        /// Sql Scalar Function
        /// </summary>
        SqlScalarFunction = 4,
        /// <summary>
        /// Sql Table-valued Function
        /// </summary>
        SqlTableValuedFunction = 5,
        /// <summary>
        /// Sql Inline-table-valued Function
        /// </summary>
        SqlInlineTableValuedFunction = 6,
        /// <summary>
        /// Extended Stored Procedure
        /// </summary>
        ExtendedStoredProcedure = 7,
        /// <summary>
        /// Replication-filter-procedure
        /// </summary>
        ReplicationFilterProcedure = 8,
        /// <summary>
        /// Assembly (CLR) Stored Procedure
        /// </summary>
        ClrStoredProcedure = 9,
        /// <summary>
        /// Assembly (CLR) Scalar Function
        /// </summary>
        ClrScalarFunction = 10,
        /// <summary>
        /// Assembly (CLR) Table-valued Function
        /// </summary>
        ClrTableValuedFunction = 11,
        /// <summary>
        /// Assembly (CLR) Aggregate Function
        /// </summary>
        ClrAggregateFunction = 12
    }

    /// <summary>
    /// Specifies the encryption type used to encrypt the database encryption key
    /// </summary>
    public enum DatabaseEncryptionType
    {
        /// <summary>
        /// Server Certificate
        /// </summary>
        ServerCertificate = 0,
        /// <summary>
        /// Server Asymmetric Key
        /// </summary>
        ServerAsymmetricKey = 1
    }


    /// <summary>
    /// Specifies the cache type of a Sequence object.
    /// </summary>
    public enum SequenceCacheType
    {
        /// <summary>
        /// To use default cache size
        /// </summary>
        DefaultCache = 0,
        /// <summary>
        /// No cache option
        /// </summary>
        NoCache = 1,
        /// <summary>
        ///To specify the cache size
        /// </summary>
        CacheWithSize = 2

    }

    /// <summary>
    /// Specifies the current encryption transition state of the database
    /// </summary>
    public enum DatabaseEncryptionState
    {
        /// <summary>
        /// Database encryption not defined
        /// </summary>
        None = 0,
        /// <summary>
        /// Database is unencrypted
        /// </summary>
        Unencrypted = 1,
        /// <summary>
        /// Databse encryption in progress
        /// </summary>
        EncryptionInProgress = 2,
        /// <summary>
        /// Databse is encrypted
        /// </summary>
        Encrypted = 3,
        /// <summary>
        /// Database encryption key change in progress
        /// </summary>
        EncryptionKeyChangesInProgress = 4,
        /// <summary>
        /// Database Decryption in progress 
        /// </summary>
        DecryptionInProgress = 5
    }

    /// <summary>
    ///see engine spec: CatalogViewsRef: Appendix  Universal Entity Classes
    ///The following table represents the domain of entity classes in the system, defined in the header file cmedscan.h.   Entity class usages include:
    ///sys.database_permissions 
    ///sys.server_permissions 
    ///sys.extended_properties 
    ///included here are only securable objects</summary>
    public enum ObjectClass
    {
        /// <summary>
        ///Database.</summary>
        Database = 0,
        /// <summary>
        ///An object (Table, StoredProcedure, etc), or Column.</summary>
        ObjectOrColumn = 1,
        /// <summary>
        ///Schema.</summary>
        Schema = 3,
        /// <summary>
        ///User.</summary>
        User = 200,
        /// <summary>
        ///Database role.</summary>
        DatabaseRole = 201,
        /// <summary>
        ///Application role.</summary>
        ApplicationRole = 202,
        /// <summary>
        ///Assembly</summary>
        SqlAssembly = 5,
        /// <summary>
        ///User Defined Type</summary>
        UserDefinedType = 6,
        /// <summary>
        ///Security expression.</summary>
        SecurityExpression = 8,
        /// <summary>
        ///XML Namespace</summary>
        XmlNamespace = 10,
        /// <summary>
        ///Message Type</summary>
        MessageType = 15,
        /// <summary>
        ///Message Contract</summary>
        ServiceContract = 16,
        /// <summary>
        ///Service (Broker)</summary>
        Service = 17,
        ///
        RemoteServiceBinding = 18,
        ///
        ServiceRoute = 19,
        /// <summary>
        ///Fulltext Catalog</summary>
        FullTextCatalog = 23,
        /// <summary>
        ///Search Property List</summary>
        SearchPropertyList = 31,
        /// <summary>
        ///SymmetricKey</summary>
        SymmetricKey = 24,
        /// <summary>
        ///Server</summary>
        Server = 100,
        /// <summary>
        ///Login</summary>
        Login = 101,
        /// <summary>
        ///Login.</summary>
        ServerPrincipal = 300,
        /// <summary>
        ///Server role.</summary>
        ServerRole = 301,
        /// <summary>
        ///Endpoint</summary>
        Endpoint = 105,
        /// <summary>
        ///Certificate</summary>
        Certificate = 25,
        /// <summary>
        ///Full Text Stoplist</summary>
        FullTextStopList = 29,
        /// <summary>
        ///AsymmetricKey</summary>
        AsymmetricKey = 26,
        /// <summary>
        /// AvailabilityGroup</summary>
        AvailabilityGroup = 108,
        /// <summary>
        /// ExternalDataSource</summary>
        ExternalDataSource = 302,
        /// <summary>
        /// ExternalFileFormat</summary>
        ExternalFileFormat = 303
    }

    /// <summary>
    /// The PageVerify enum specifies the type of integrity check performed on page reads.
    /// </summary>
    public enum PageVerify
    {
        /// <summary>
        /// No integrity check will be performed.
        /// </summary>
        None = 0,
        /// <summary>
        ///     The server will check for torn pages (incomplete I/O operations).
        /// </summary>
        TornPageDetection = 1,
        /// <summary>
        ///     Server applies a checksum for every page.
        /// </summary>
        Checksum = 2
    }

    /// <summary>
    /// Role the database plays in mirroring, one of:
    /// </summary>
    public enum MirroringRole
    {
        ///
        None = 0,
        ///
        Principal = 1,
        ///
        Mirror = 2
    }

    /// <summary>
    /// Safety guarantee of updates on the backup, one of:
    /// </summary>
    public enum MirroringSafetyLevel
    {
        ///
        None = 0,
        ///
        Unknown = 1,
        ///
        Off = 2,
        ///
        Full = 3
    }

    /// <summary>
    /// The MirroringOption enum is used to change the state or a Database mirror.
    /// </summary>
    public enum MirroringOption
    {
        /// <summary>
        ///Terminate database mirroring.</summary>
        Off = 0,
        /// <summary>
        ///Suspend database mirroring.</summary>
        Suspend = 1,
        /// <summary>
        ///Resume database mirroring.</summary>
        Resume = 2,
        /// <summary>
        ///Removes the witness.</summary>
        RemoveWitness = 3,
        /// <summary>
        ///Initiate a failover.</summary>
        Failover = 4,
        /// <summary>
        ///Forces a failover</summary>
        ForceFailoverAndAllowDataLoss = 5
    }

    /// <summary>
    /// Role the database plays in mirroring, one of:
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.MirroringStatusConverter))]
    public enum MirroringStatus
    {
        ///
        [LocDisplayName("msNone")]
        None = 0,
        ///
        [LocDisplayName("msSuspended")]
        Suspended = 1,
        ///
        [LocDisplayName("msDisconnected")]
        Disconnected = 2,
        ///
        [LocDisplayName("msSynchronizing")]
        Synchronizing = 3,
        ///
        [LocDisplayName("msPendingFailover")]
        PendingFailover = 4,
        ///
        [LocDisplayName("msSynchronized")]
        Synchronized = 5
    }

    ///
    public enum MirroringWitnessStatus
    {
        ///
        None = 0,
        ///
        Unknown = 1,
        ///
        Connected = 2,
        ///
        Disconnected = 3
    }

    /// <summary>
    /// Change Tracking Retention Period Units
    /// </summary>
    [Flags]
    public enum RetentionPeriodUnits
    {
        /// <summary>
        /// InvalidUnits
        /// </summary>
        None = 0,
        /// <summary>
        /// Minutes
        /// </summary>
        Minutes = 1,
        /// <summary>
        /// Hours
        /// </summary>
        Hours = 2,
        /// <summary>
        /// Days
        /// </summary>
        Days = 3
    }

    ///
    [Flags]
    public enum HttpPortTypes
    {
        ///
        None = 0,
        ///
        Ssl = 1,
        ///
        Clear = 2,
        ///
        All = 3
    }

    ///
    public enum EndpointState
    {
        /// <summary>
        /// Endpoint is started.</summary>
        Started = 0,
        /// <summary>
        /// Endpoint is stopped.</summary>
        Stopped = 1,
        /// <summary>
        /// Endpoint is disabled.</summary>
        Disabled = 2
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum XmlFormatOption
    {
        ///
        XmlFormat = 1,
        ///
        SqlFormat = 2
    }

    /// <summary>
    ///Specifies if an XSD schema will be returned for a SOAP method. </summary>
    public enum XsdSchemaOption
    {
        /// <summary>
        /// Do not return an inline XSD schema with the result.</summary>
        None = 0,
        /// <summary>
        ///Return an inline XSD with the result.</summary>
        Standard = 1
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ResultFormat
    {
        /// <summary>
        /// Return all results.</summary>
        AllResults = 1,
        /// <summary>
        /// Row sets only.</summary>
        RowSets = 2,
        /// <summary>
        /// Row sets only.</summary>
        None = 3
    }

    ///
    public enum MethodXsdSchemaOption
    {
        /// <summary>
        /// Do not return an inline XSD schema with the result.</summary>
        None = 0,
        /// <summary>
        /// Return an inline XSD with the result.</summary>
        Standard = 1,
        /// <summary>
        ///Return according to default settings defined at the Endpoint.</summary>
        Default = 2
    }

    ///
    public enum WsdlGeneratorOption
    {
        /// <summary>
        /// Do not generate a WSDL description.</summary>
        None = 0,
        /// <summary>
        /// Use default WSDL generator.</summary>
        DefaultProcedure = 1,
        /// <summary>
        /// Uses a specified procedure for WSDL generation.</summary>
        Procedure = 2
    }

    /// <summary>
    /// Enumerates the hash algorithms that are used to authenticate SQL Login passwords.
    /// </summary>
    public enum PasswordHashAlgorithm
    {
        /// <summary>
        /// No special algorithm used.
        /// </summary>
        None = 0,
        /// <summary>
        /// Use the SQL7.0 hash algorithm.
        /// </summary>
        SqlServer7 = 1,
        /// <summary>
        /// Use the SHA-1 hash algorithm.
        /// </summary>
        ShaOne = 2,
        /// <summary>
        /// Use the SHA-2 hash algorithm.
        /// </summary>
        ShaTwo = 3
    }

    /// <summary>
    /// Enumerates the containment types of a database.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ContainmentTypeConverter))]
    public enum ContainmentType
    {
        /// <summary>
        /// Specifies that the database is not contained.
        /// </summary>
        [LocDisplayName("ctNone")]
        None = 0,
        /// <summary>
        /// Specifies that the database is partially contained.
        /// </summary>
        [LocDisplayName("ctPartial")]
        Partial = 1
        // Specifies that the database is fully contained.
        // Full containment is not supported in MinCDB.
        //,Full = 2
    }

    ///
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum HttpAuthenticationModes
    {
        ///
        Anonymous = 1,
        ///
        Basic = 2,
        ///
        Digest = 4,
        ///
        Integrated = 8,
        ///
        Ntlm = 16,
        /// 
        Kerberos = 32,
        /// 
        All = Anonymous | Basic | Digest | Integrated | Ntlm | Kerberos
    }

    /// <summary>
    /// Reuse of transaction log space is currently waiting on.</summary>
    public enum LogReuseWaitStatus
    {
        ///
        Nothing = 0,
        ///
        Checkpoint = 1,
        ///
        LogBackup = 2,
        ///
        BackupOrRestore = 3,
        ///
        Transaction = 4,
        ///
        Mirroring = 5,
        ///
        Replication = 6,
        ///
        SnapshotCreation = 7,
        ///
        LogScan = 8,
        ///
        Other = 9
    }

    /// <summary>
    /// Specifies the kind of xml component
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum XmlTypeKind
    {
        /// <summary>
        /// 'Any' type
        /// </summary>
        Any = 1,
        /// <summary>
        /// 'Any simple' type
        /// </summary>
        AnySimple = 2,
        /// <summary>
        /// Primitive type
        /// </summary>
        Primitive = 3,
        /// <summary>
        /// Simple type
        /// </summary>
        Simple = 4,
        /// <summary>
        /// List type
        /// </summary>
        List = 5,
        /// <summary>
        /// Union type
        /// </summary>
        Union = 6,
        /// <summary>
        /// 'Complex Simple' type
        /// </summary>
        ComplexSimple = 7,
        /// <summary>
        /// Complex type
        /// </summary>
        Complex = 8,
        /// <summary>
        /// Element
        /// </summary>
        Element = 9,
        /// <summary>
        /// Model group
        /// </summary>
        ModelGroup = 10,
        /// <summary>
        /// Element wildcard
        /// </summary>
        ElementWildcard = 11,
        /// <summary>
        /// Attribute
        /// </summary>
        Attribute = 12,
        /// <summary>
        /// Attribute group
        /// </summary>
        AttributeGroup = 13,
        /// <summary>
        /// Attribute wildcard
        /// </summary>
        AttributeWildcard = 14
    }

    /// <summary>
    /// Specifies the repair options
    /// </summary>
    [Flags]
    public enum RepairOptions
    {
        /// <summary>
        ///None.
        ///</summary>
        None = 0,
        /// <summary>
        ///Displays all reported errors per object.
        ///</summary>
        AllErrorMessages = 1,
        /// <summary>
        ///Performs logical consistency.
        ///</summary>
        ExtendedLogicalChecks = 2,
        /// <summary>
        ///Suppress all informational messages.
        ///</summary>
        NoInformationMessages = 4,
        /// <summary>
        ///Obtains lock instead of using an internal database snapshot.
        ///</summary>
        TableLock = 8,
        /// <summary>
        ///Displays the amount of estimated tempdb space.
        ///</summary>
        EstimateOnly = 16
    }

    /// <summary>
    /// Specifies the kind of repair structure
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum RepairStructure
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Limits the checking to the integrity of the physical structure of the page and record headers.
        /// </summary>
        PhysicalOnly = 1,
        /// <summary>
        /// Checks the database for the column values that are not valid or out of range.
        /// </summary>
        DataPurity = 2
    }

    /// <summary>
    /// Specifies the xml type derivation
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum XmlTypeDerivation
    {
        /// <summary>
        /// Not derived
        /// </summary>
        None = 1,
        /// <summary>
        /// Extension
        /// </summary>
        Extension = 2,
        /// <summary>
        /// Restriction
        /// </summary>
        Restriction = 3,
        /// <summary>
        /// Substitution
        /// </summary>
        Substitution = 4
    }

    /// <summary>
    /// represents the type of the secondary xml index
    /// </summary>
    public enum SecondaryXmlIndexType
    {
        ///
        None = 0,
        ///
        Path = 1,
        ///
        Value = 2,
        ///
        Property = 3
    }

    /// <summary>
    /// represents the type of indexed path in Selective Xml Index
    ///</summary>
    public enum IndexedXmlPathType
    {
        /// <summary>
        ///XQuery</summary>
        XQuery = 0,
        /// <summary>
        ///Sql</summary>
        Sql = 1
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum EndpointType
    {
        /// <summary>
        ///SOAP</summary>
        Soap = 1,
        /// <summary>
        ///TSQL</summary>
        TSql = 2,
        /// <summary>
        ///SERVICE_BROKER</summary>
        ServiceBroker = 3,
        /// <summary>
        ///DATABASE_MIRRORING</summary>
        DatabaseMirroring = 4
    }

    /// <summary>
    /// EndpointEncryption 
    /// </summary>
    public enum EndpointEncryption
    {
        /// The data sent over the connection is not encrypted
        Disabled = 0,
        /// Data is encrypted if the opposite endpoint specifies either Supported or Required.
        Supported = 1,
        /// Encryption is used if the opposite endpoint is either Supported or Required.  
        Required = 2
    }

    /// <summary>
    /// EndpointEncryptionAlgorithm
    /// </summary>
    public enum EndpointEncryptionAlgorithm
    {
        /// 
        None = 0,
        /// RC4
        RC4 = 1,
        /// AES
        Aes = 2,
        /// AES RC4
        AesRC4 = 3,
        /// RC4 AES
        RC4Aes = 4
    }

    /// <summary>
    /// The type of connection authentication required for connections to this endpoint.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum EndpointAuthenticationOrder
    {
        /// 
        Ntlm = 1,
        /// 
        Kerberos = 2,
        /// 
        Negotiate = 3,
        /// 
        Certificate = 4,
        /// 
        NtlmCertificate = 5,
        /// 
        KerberosCertificate = 6,
        /// 
        NegotiateCertificate = 7,
        /// 
        CertificateNtlm = 8,
        /// 
        CertificateKerberos = 9,
        /// 
        CertificateNegotiate = 10
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ProtocolType
    {
        ///
        Http = 1,
        ///TCP
        Tcp = 2,
        ///
        NamedPipes = 3,
        ///
        SharedMemory = 4,
        ///
        Via = 5
    }

    ///
    public enum ServerMirroringRole
    {
        ///
        None = 0,
        ///
        Partner = 1,
        ///
        Witness = 2,
        ///
        All = 3
    }

    /// <summary>
    /// see fk syntax:
    /// | [ FOREIGN KEY ] 
    ///     REFERENCES [ schema_name . ] referenced_table_name [ ( ref_column ) ] 
    ///     [ ON DELETE { NO ACTION | CASCADE | SET NULL | SET DEFAULT } ] 
    ///     [ ON UPDATE { NO ACTION | CASCADE | SET NULL | SET DEFAULT } ] 
    /// </summary>
    public enum ForeignKeyAction
    {
        ///
        NoAction = 0,
        ///
        Cascade = 1,
        ///
        SetNull = 2,
        ///
        SetDefault = 3
    }

    /// <summary>
    /// Enumerates possible referential actions when an EdgeConstraint object is modified.
    /// see ec syntax:
    /// | [ EdgeConstraint ] 
    ///     CONNECTION ([ schema_name . ] referenced_table_from_name TO  [ schema_name . ] referenced_table_to_name] 
    ///     [ ON DELETE { NO ACTION | CASCADE } ]
    /// </summary>
    public enum EdgeConstraintDeleteAction
    {
        ///
        NoAction = 0,
        ///
        Cascade = 1
    }

    /// <summary>
    /// Specifies types of XML document constraints
    /// </summary>
    public enum XmlDocumentConstraint
    {
        /// <summary>
        /// Use server default (will not emit CONTENT or DOCUMENT)
        /// </summary>
        Default = 0,
        /// <summary>
        /// An XML fragment.
        /// </summary>
        Content = 1,
        /// <summary>
        /// Restricts an XML instance to a well-formed XML 1.0 instance.
        /// </summary>
        Document = 2
    }

    /// <summary>
    /// This enum describes the state of a Notification Service application component. </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]
    public enum NSActivationState : byte
    {
        /// <summary>
        /// The service or execution engine has yet to process the state of being enabled</summary>
        EnablePending = 1,
        /// <summary>
        /// The service or execution engine has processed the EnablePending state and is now enabled the component</summary>
        Enabled = 2,
        /// <summary>
        /// The service or execution engine has yet to process the state of being disabled</summary>
        DisablePending = 3,
        /// <summary>
        /// The service or execution engine has processed the DisablePending state and is now disabled the component</summary>
        Disabled = 4
    }

    /// <summary>
    /// LoginType of SoapPayloadMethod
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]
    public enum MethodLoginType : byte
    {
        /// <summary>
        /// Mixed mode.
        /// </summary>
        Mixed = 1,
        /// <summary>
        /// Windows authentication mode.
        /// </summary>
        Windows = 2
    }

    ///The AuditLevel property exposes SQL Server Authentication logging behavior.
    [Flags]
    public enum AuditLevel
    {
        ///Do not log authentication attempts
        None = 0,
        ///Log successful authentication
        Success = 1,
        ///Log failed authentication 
        Failure = 2,
        ///Log all authentication attempts regardless of success or failure
        All = 3
    }

    /// <summary>
    /// Specifies the mode in which PerfMon works
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public enum PerfMonMode
    {
        ///No PerfMon Integration
        None = 1000,
        ///Report PerfMon data continuously
        Continuous = 0,
        ///Report PerfMon data on demand
        OnDemand = 1,
    }

    /// <summary>
    /// Specifies the Importance Type for Resource Governor Workload group
    /// </summary>
    public enum WorkloadGroupImportance
    {
        /// <summary>
        /// Importance for workload group is Low
        /// </summary>
        Low = 0,
        /// <summary>
        /// Importance for workload group is Medium
        /// </summary>
        Medium = 1,
        /// <summary>
        /// Importance for workload group is High
        /// </summary>
        High = 2
    }

    /// <summary>
    /// Filestream level options
    /// </summary>
    public enum FileStreamLevel
    {
        /// Disabled
        Disabled = 0,
        /// T-SQL access only
        TSqlAccess = 1,
        /// T-SQL and local File system access
        TSqlLocalFileSystemAccess = 2,
        /// T-SQL and full File system access
        TSqlFullFileSystemAccess = 3
    }

    /// <summary>
    /// Type of plan guide
    /// </summary>    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum PlanGuideType
    {
        ///Object
        Object = 1,
        ///Sql
        Sql = 2,
        ///Template
        Template = 3
    }

    /// <summary>
    /// Specifies the destination type of an Audit
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AuditDestinationTypeConverter))]
    public enum AuditDestinationType
    {
        /// <summary>
        /// Write to file
        /// </summary>
        [LocDisplayName("FileDest")]
        [TsqlSyntaxString("FILE")]
        File = 0,
        /// <summary>
        /// Write to security log
        /// </summary>
        [LocDisplayName("SecurityLogDest")]
        [TsqlSyntaxString("SECURITY_LOG")]
        SecurityLog = 1,
        /// <summary>
        /// Write to application log
        /// </summary>
        [LocDisplayName("ApplicationLogDest")]
        [TsqlSyntaxString("APPLICATION_LOG")]
        ApplicationLog = 2,
        /// <summary>
        /// Write to URL
        /// </summary>
        [LocDisplayName("UrlDest")]
        [TsqlSyntaxString("URL")]
        Url = 3,
        /// <summary>
        /// Write to EXTERNAL_MONITOR
        /// </summary>
        [LocDisplayName("ExternalMonitorDest")]
        [TsqlSyntaxString("EXTERNAL_MONITOR")]
        ExternalMonitor = 4,
        /// <summary>
        /// The destination type of this audit is unknown
        /// </summary>
        [LocDisplayName("UnknownDest")]
        [TsqlSyntaxString("UNKNOWN")]
        Unknown = 100
    }

    /// <summary>
    /// Specifies the unit of file size
    /// </summary>
    public enum AuditFileSizeUnit
    {   //Do not change the order
        /// <summary>
        /// File size in MB
        /// </summary>
        Mb = 0,
        /// <summary>
        /// File size in GB
        /// </summary>
        Gb = 1,
        /// <summary>
        /// File size in TB
        /// </summary>
        Tb = 2
    }

    /// <summary>
    /// Specifies the action that needs to be taken when the audit sink cannot perform the write
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AuditOnFailureActionConverter))]
    public enum OnFailureAction
    {
        /// <summary>
        /// Audit should continue running
        /// </summary>
        [LocDisplayName("OnFailureActionContinue")]
        [TsqlSyntaxString("CONTINUE")]
        Continue = 0,

        /// <summary>
        /// Audit should shut down
        /// </summary>
        [LocDisplayName("OnFailureActionShutdown")]
        [TsqlSyntaxString("SHUTDOWN")]
        Shutdown = 1,

        /// <summary>
        /// User operation causing failure will fail. No event loss
        /// </summary>
        [LocDisplayName("OnFailureActionFail")]
        [TsqlSyntaxString("FAIL_OPERATION")]
        FailOperation = 2
    }

    /// <summary>
    /// Specifies the state of the Audit
    /// </summary>
    public enum AuditStatusType
    {
        /// <summary>
        /// Audit is running
        /// </summary>
        Started = 0,
        /// <summary>
        /// Audit is stopped
        /// </summary>
        Stopped = 1,
        /// <summary>
        /// Audit failed and stopped
        /// </summary>
        Failed = 2
    }

    /// <summary>
    /// Specifies the sensitivity rank of a column
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.SensitivityRankConverter))]
    public enum SensitivityRank
    {
        [LocDisplayName("Undefined")]
        Undefined = -1,
        [LocDisplayName("None")]
        [TsqlSyntaxString("NONE")]
        None = 0,
        [LocDisplayName("Low")]
        [TsqlSyntaxString("LOW")]
        Low = 10,
        [LocDisplayName("Medium")]
        [TsqlSyntaxString("MEDIUM")]
        Medium = 20,
        [LocDisplayName("High")]
        [TsqlSyntaxString("HIGH")]
        High = 30,
        [LocDisplayName("Critical")]
        [TsqlSyntaxString("CRITICAL")]
        Critical = 40
    }

    /// <summary>
    /// Specifies the authentication type of the database principals
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// Specifies a SQL user without a login, a user mapped with a certificate, or a user mapped
        /// with an asymmetric key.
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies that the user is mapped to a login.
        /// </summary>
        Instance = 1,
        /// <summary>
        /// Specifies that the user has a password.
        /// </summary>
        Database = 2,
        /// <summary>
        /// Specifies that the user is a Windows user or group.
        /// </summary>
        Windows = 3,
        /// <summary>
        /// Specifies that the user is a External user or group.
        /// </summary>
        External = 4,
    }

    /// <summary>
    /// Specifies the type of action for sql server audit events
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AuditActionTypeConverter))]
    public enum AuditActionType
    {
        /// <summary>
        /// APPLICATION_ROLE_CHANGE_PASSWORD_GROUP
        /// </summary>
        [TsqlSyntaxString("APPLICATION_ROLE_CHANGE_PASSWORD_GROUP")]
        ApplicationRoleChangePasswordGroup,
        /// <summary>
        /// AUDIT_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("AUDIT_CHANGE_GROUP")]
        AuditChangeGroup,
        /// <summary>
        /// BACKUP_RESTORE_GROUP
        /// </summary>
        [TsqlSyntaxString("BACKUP_RESTORE_GROUP")]
        BackupRestoreGroup,
        /// <summary>
        /// BATCH_COMPLETED_GROUP
        /// </summary>
        [TsqlSyntaxString("BATCH_COMPLETED_GROUP")]
        BatchCompletedGroup,
        /// <summary>
        /// BATCH_STARTED_GROUP
        /// </summary>
        [TsqlSyntaxString("BATCH_STARTED_GROUP")]
        BatchStartedGroup,
        /// <summary>
        /// BROKER_LOGIN_GROUP
        /// </summary>
        [TsqlSyntaxString("BROKER_LOGIN_GROUP")]
        BrokerLoginGroup,
        /// <summary>
        /// DATABASE_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_CHANGE_GROUP")]
        DatabaseChangeGroup,
        /// <summary>
        /// DATABASE_LOGOUT_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_LOGOUT_GROUP")]
        DatabaseLogoutGroup,
        /// <summary>
        /// DATABASE_MIRRORING_LOGIN_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_MIRRORING_LOGIN_GROUP")]
        DatabaseMirroringLoginGroup,
        /// <summary>
        /// DATABASE_OBJECT_ACCESS_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_OBJECT_ACCESS_GROUP")]
        DatabaseObjectAccessGroup,
        /// <summary>
        /// DATABASE_OBJECT_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_OBJECT_CHANGE_GROUP")]
        DatabaseObjectChangeGroup,
        /// <summary>
        /// DATABASE_OBJECT_OWNERSHIP_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_OBJECT_OWNERSHIP_CHANGE_GROUP")]
        DatabaseObjectOwnershipChangeGroup,
        /// <summary>
        /// DATABASE_OBJECT_PERMISSION_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_OBJECT_PERMISSION_CHANGE_GROUP")]
        DatabaseObjectPermissionChangeGroup,
        /// <summary>
        /// DATABASE_OPERATION_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_OPERATION_GROUP")]
        DatabaseOperationGroup,
        /// <summary>
        /// DATABASE_OWNERSHIP_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_OWNERSHIP_CHANGE_GROUP")]
        DatabaseOwnershipChangeGroup,
        /// <summary>
        /// DATABASE_PERMISSION_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_PERMISSION_CHANGE_GROUP")]
        DatabasePermissionChangeGroup,
        /// <summary>
        /// DATABASE_PRINCIPAL_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_PRINCIPAL_CHANGE_GROUP")]
        DatabasePrincipalChangeGroup,
        /// <summary>
        /// DATABASE_PRINCIPAL_IMPERSONATION_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_PRINCIPAL_IMPERSONATION_GROUP")]
        DatabasePrincipalImpersonationGroup,
        /// <summary>
        /// DATABASE_ROLE_MEMBER_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("DATABASE_ROLE_MEMBER_CHANGE_GROUP")]
        DatabaseRoleMemberChangeGroup,
        /// <summary>
        /// DBCC_GROUP
        /// </summary>
        [TsqlSyntaxString("DBCC_GROUP")]
        DbccGroup,
        /// <summary>
        /// DELETE
        /// </summary>
        [TsqlSyntaxString("DELETE")]
        Delete,
        /// <summary>
        /// EXECUTE
        /// </summary>
        [TsqlSyntaxString("EXECUTE")]
        Execute,
        /// <summary>
        /// FAILED_DATABASE_AUTHENTICATION_GROUP
        /// </summary>
        [TsqlSyntaxString("FAILED_DATABASE_AUTHENTICATION_GROUP")]
        FailedDatabaseAuthenticationGroup,
        /// <summary>
        /// FAILED_LOGIN_GROUP
        /// </summary>
        [TsqlSyntaxString("FAILED_LOGIN_GROUP")]
        FailedLoginGroup,
        /// <summary>
        /// FULLTEXT_GROUP
        /// </summary>
        [TsqlSyntaxString("FULLTEXT_GROUP")]
        FullTextGroup,
        /// <summary>
        /// GLOBAL_TRANSACTIONS_LOGIN_GROUP
        /// </summary>
        [TsqlSyntaxString("GLOBAL_TRANSACTIONS_LOGIN_GROUP")]
        GlobalTransactionsLoginGroup,
        /// <summary>
        /// INSERT
        /// </summary>
        [TsqlSyntaxString("INSERT")]
        Insert,
        /// <summary>
        /// LOGIN_CHANGE_PASSWORD_GROUP
        /// </summary>
        [TsqlSyntaxString("LOGIN_CHANGE_PASSWORD_GROUP")]
        LoginChangePasswordGroup,
        /// <summary>
        /// LOGOUT_GROUP
        /// </summary>
        [TsqlSyntaxString("LOGOUT_GROUP")]
        LogoutGroup,
        /// <summary>
        /// RECEIVE
        /// </summary>
        [TsqlSyntaxString("RECEIVE")]
        Receive,
        /// <summary>
        /// REFERENCES
        /// </summary>
        [TsqlSyntaxString("REFERENCES")]
        References,
        /// <summary>
        /// SCHEMA_OBJECT_ACCESS_GROUP
        /// </summary>
        [TsqlSyntaxString("SCHEMA_OBJECT_ACCESS_GROUP")]
        SchemaObjectAccessGroup,
        /// <summary>
        /// SCHEMA_OBJECT_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SCHEMA_OBJECT_CHANGE_GROUP")]
        SchemaObjectChangeGroup,
        /// <summary>
        /// SCHEMA_OBJECT_OWNERSHIP_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SCHEMA_OBJECT_OWNERSHIP_CHANGE_GROUP")]
        SchemaObjectOwnershipChangeGroup,
        /// <summary>
        /// SCHEMA_OBJECT_PERMISSION_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SCHEMA_OBJECT_PERMISSION_CHANGE_GROUP")]
        SchemaObjectPermissionChangeGroup,
        /// <summary>
        /// SELECT
        /// </summary>
        [TsqlSyntaxString("SELECT")]
        Select,
        /// <summary>
        /// SERVER_OBJECT_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_OBJECT_CHANGE_GROUP")]
        ServerObjectChangeGroup,
        /// <summary>
        /// SERVER_OBJECT_OWNERSHIP_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_OBJECT_OWNERSHIP_CHANGE_GROUP")]
        ServerObjectOwnershipChangeGroup,
        /// <summary>
        /// SERVER_OBJECT_PERMISSION_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_OBJECT_PERMISSION_CHANGE_GROUP")]
        ServerObjectPermissionChangeGroup,
        /// <summary>
        /// SERVER_OPERATION_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_OPERATION_GROUP")]
        ServerOperationGroup,
        /// <summary>
        /// SERVER_PERMISSION_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_PERMISSION_CHANGE_GROUP")]
        ServerPermissionChangeGroup,
        /// <summary>
        /// SERVER_PRINCIPAL_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_PRINCIPAL_CHANGE_GROUP")]
        ServerPrincipalChangeGroup,
        /// <summary>
        /// SERVER_PRINCIPAL_IMPERSONATION_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_PRINCIPAL_IMPERSONATION_GROUP")]
        ServerPrincipalImpersonationGroup,
        /// <summary>
        /// SERVER_ROLE_MEMBER_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_ROLE_MEMBER_CHANGE_GROUP")]
        ServerRoleMemberChangeGroup,
        /// <summary>
        /// SERVER_STATE_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SERVER_STATE_CHANGE_GROUP")]
        ServerStateChangeGroup,
        /// <summary>
        /// SUCCESSFUL_DATABASE_AUTHENTICATION_GROUP
        /// </summary>
        [TsqlSyntaxString("SUCCESSFUL_DATABASE_AUTHENTICATION_GROUP")]
        SuccessfulDatabaseAuthenticationGroup,
        /// <summary>
        /// SUCCESSFUL_LOGIN_GROUP
        /// </summary>
        [TsqlSyntaxString("SUCCESSFUL_LOGIN_GROUP")]
        SuccessfulLoginGroup,
        /// <summary>
        /// TRACE_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("TRACE_CHANGE_GROUP")]
        TraceChangeGroup,
        /// <summary>
        /// UPDATE
        /// </summary>
        [TsqlSyntaxString("UPDATE")]
        Update,
        /// <summary>
        /// USER_CHANGE_PASSWORD_GROUP
        /// </summary>
        [TsqlSyntaxString("USER_CHANGE_PASSWORD_GROUP")]
        UserChangePasswordGroup,
        /// <summary>
        /// USER_DEFINED_AUDIT_GROUP
        /// </summary>
        [TsqlSyntaxString("USER_DEFINED_AUDIT_GROUP")]
        UserDefinedAuditGroup,
        /// <summary>
        /// TRANSACTION_GROUP
        /// </summary>
        [TsqlSyntaxString("TRANSACTION_GROUP")]
        TransactionGroup,
        /// <summary>
        /// SENSITIVITY_CLASSIFICATION_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SENSITIVITY_CLASSIFICATION_CHANGE_GROUP")]
        SensitiveClassificationChangeGroup,
        /// <summary>
        /// STORAGE_LOGIN_GROUP
        /// </summary>
        [TsqlSyntaxString("STORAGE_LOGIN_GROUP")]
        StorageLoginGroup,
        /// <summary>
        /// STATEMENT_ROLLBACK_GROUP
        /// </summary>
        [TsqlSyntaxString("STATEMENT_ROLLBACK_GROUP")]
        StatementRollbackGroup,
        /// <summary>
        /// TRANSACTION_BEGIN_GROUP
        /// </summary>
        [TsqlSyntaxString("TRANSACTION_BEGIN_GROUP")]
        TransactionBeginGroup,
        /// <summary>
        /// TRANSACTION_COMMIT_GROUP
        /// </summary>
        [TsqlSyntaxString("TRANSACTION_COMMIT_GROUP")]
        TransactionCommitGroup,
        /// <summary>
        /// TRANSACTION_ROLLBACK_GROUP
        /// </summary>
        [TsqlSyntaxString("TRANSACTION_ROLLBACK_GROUP")]
        TransactionRollbackGroup,
        /// <summary>
        /// LEDGER_OPERATION_GROUP
        /// </summary>
        [TsqlSyntaxString("LEDGER_OPERATION_GROUP")]
        LedgerOperationGroup,
        /// <summary>
        /// SENSITIVITY_CLASSIFICATION_CHANGE_GROUP
        /// </summary>
        [TsqlSyntaxString("SENSITIVE_BATCH_COMPLETED_GROUP")]
        SensitiveBatchCompletedGroup,
        /// <summary>
        /// EXTGOV_OPERATION_GROUP
        /// </summary>
        [TsqlSyntaxString("EXTGOV_OPERATION_GROUP")]
        ExternalGovernanceOperationGroup,
    }

    /// <summary>
    /// Filestream non-transacted access type
    /// </summary>
    public enum FilestreamNonTransactedAccessType
    {
        /// <summary>
        /// No access
        /// </summary>
        Off = 0,
        /// <summary>
        /// Read-Only access
        /// </summary>
        ReadOnly = 1,
        /// <summary>
        /// Read-Write access
        /// </summary>
        Full = 2
    }

    #region HADR region

    /// <summary>
    /// A rollup of the synchronization states of the availability replicas in the availability group.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupRollupSynchronizationStateConverter))]
    public enum AvailabilityGroupRollupSynchronizationState
    {
        /// <summary>
        /// None of the availability replicas is synchronizing.
        /// </summary>
        [LocDisplayName("agshNoneSynchronizing")]
        NoneSynchronizing = 0,

        /// <summary>
        /// At least one of the replicas is in "synchronizing" state.
        /// </summary>
        [LocDisplayName("agshPartiallySynchronizing")]
        PartiallySynchronizing = 1,

        /// <summary>
        /// All of the replicas are in "synchronizing" state.
        /// </summary>
        [LocDisplayName("agshAllSynchronizing")]
        AllSynchronizing = 2,

        /// <summary>
        /// All of the replicas are in "synchronized" state.
        /// </summary>
        [LocDisplayName("agshAllSynchronized")]
        AllSynchronized = 3,

        /// <summary>
        /// The synchronization state is unknown, this would be the case if the property is viewed on a secondary replica
        /// </summary>
        [LocDisplayName("Unknown")]
        Unknown = 4,
    }

    /// <summary>
    /// The status of the HADR Manager Service
    /// </summary>
    public enum HadrManagerStatus
    {
        /// <summary>
        /// The manager service hasn't started, pending communication.
        /// </summary>
        [LocDisplayName("hmsPendingCommunication")]
        PendingCommunication = 0,

        /// <summary>
        /// The manager service is up and running.
        /// </summary>
        [LocDisplayName("hmsRunning")]
        Running = 1,

        /// <summary>
        /// The manager service has failed, is not running.
        /// </summary>
        [LocDisplayName("hmsFailed")]
        Failed = 2,
    }

    /*
     * This is commented until the connection director work is implemented in the engine
        /// <summary>
        /// 
        /// </summary>
        public enum AvailabilityGroupVirtualNameHealth
        {
        }
        */

    /// <summary>
    /// The state of the replica's readiness to process client requests for all databases replicas in the availability group residing on it.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaOperationalStateConverter))]
    public enum AvailabilityReplicaOperationalState
    {
        /// <summary>
        /// A failover command is in progress, the replica cannot receive client requests.
        /// </summary>
        [LocDisplayName("arosPendingFailover")]
        PendingFailover = 0,

        /// <summary>
        /// The replica is pending a switch to primary role. This is a transient state.
        /// </summary>
        [LocDisplayName("arosPending")]
        Pending = 1,

        /// <summary>
        /// The replica is ready to client requests.
        /// </summary>
        [LocDisplayName("arosOnline")]
        Online = 2,

        /// <summary>
        /// The availability group currently has no primary, the replica cannot receive client requests.
        /// </summary>
        [LocDisplayName("arosOffline")]
        Offline = 3,

        /// <summary>
        /// The replica is unable to communicate with the Windows cluster, the replica cannot receive client requests.
        /// </summary>
        [LocDisplayName("arosFailed")]
        Failed = 4,

        /// <summary>
        /// The availability group has lost quorum, the replica cannot receive client requests.
        /// </summary>
        [LocDisplayName("arosFailedNoQuorum")]
        FailedNoQuorum = 5,

        /// <summary>
        /// The operational state of this replica is unknown, the availability replica object referes to a remote instance.
        /// </summary>
        [LocDisplayName("Unknown")]
        Unknown = 6,
    }

    /// <summary>
    /// Represents a rollup of the recovery state of all database replicas in the availability group that reside on this availability replica.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaRollupRecoveryStateConverter))]
    public enum AvailabilityReplicaRollupRecoveryState
    {
        /// <summary>
        /// At least one of the database replicas' state is not online.
        /// </summary>
        [LocDisplayName("arrhInProgress")]
        InProgress = 0,

        /// <summary>
        /// All the database replicas' states are online.
        /// </summary>
        [LocDisplayName("arrhOnline")]
        Online = 1,

        /// <summary>
        /// The recovery health state of this replica is unknown, the availability replica object referes to a remote instance.
        /// </summary>
        [LocDisplayName("Unknown")]
        Unknown = 2,
    }

    /// <summary>
    /// The current synchronization state of the availability replica. This is based on the synchronization states of database
    /// replicas in the availaiblity group residing on the instance.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaRollupSynchronizationStateConverter))]
    public enum AvailabilityReplicaRollupSynchronizationState
    {
        /// <summary>
        /// At least one database replica on is not synchronizing with the primary.
        /// </summary>
        [LocDisplayName("arshNotSynchronizing")]
        NotSynchronizing = 0,

        /// <summary>
        /// All database replicas are at least synchronizing with the primary.
        /// </summary>
        [LocDisplayName("arshSynchronizing")]
        Synchronizing = 1,

        /// <summary>
        /// All database replicas are synchronized with the primary.
        /// </summary>
        [LocDisplayName("arshSynchronized")]
        Synchronized = 2,

        /// <summary>
        /// The synchronization state of the replica is unknown. This will be the case for a remote secondary replica
        /// if the property is accessed from another secondary.
        /// </summary>
        [LocDisplayName("Unknown")]
        Unknown = 3,
    }

    /// <summary>
    /// The current role a replica is playing in an availability group.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaRoleConverter))]
    public enum AvailabilityReplicaRole
    {
        /// <summary>
        /// The replica is in a resolving state
        /// </summary>
        [LocDisplayName("arrResolving")]
        Resolving = 0,
        /// <summary>
        /// The replica is the current primary in the availability group
        /// </summary>
        [LocDisplayName("arrPrimary")]
        Primary = 1,
        /// <summary>
        /// The replica is a secondary in the availability group
        /// </summary>
        [LocDisplayName("arrSecondary")]
        Secondary = 2,

        /// <summary>
        /// The replica is in an unknown state.
        /// </summary>
        [LocDisplayName("Unknown")]
        Unknown = 3
    }

    /// <summary>
    /// The current connection state of an availability replica.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaConnectionStateConverter))]
    public enum AvailabilityReplicaConnectionState
    {
        /// <summary>
        /// The replica is not currently connected to the primary (could be an indication of a communication issue).
        /// </summary>
        [LocDisplayName("arcsDisconnected")]
        Disconnected = 0,
        /// <summary>
        /// The replica is currently connected to the primary.
        /// </summary>
        [LocDisplayName("arcsConnected")]
        Connected = 1,
        /// <summary>
        /// The replica connection state is unknown.
        /// </summary>
        [LocDisplayName("Unknown")]
        Unknown = 2
    }

    /// <summary>
    /// Connection intent modes of an Availability Replica in primary role
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaConnectionModeInPrimaryRoleConverter))]
    public enum AvailabilityReplicaConnectionModeInPrimaryRole
    {
        /// <summary>
        /// The availability replica in primary role will allow all connections
        /// </summary>
        [LocDisplayName("cmprAllConnections")]
        [TsqlSyntaxString("ALL")]
        AllowAllConnections = 2,

        /// <summary>
        /// The availability replica in primary role will allow read/write connections
        /// </summary>
        [LocDisplayName("cmprReadWriteConnections")]
        [TsqlSyntaxString("READ_WRITE")]
        AllowReadWriteConnections = 3,

        /// <summary>
        /// The availability replica in the primary role is unknown. The replica may not be able to communicate with the cluster or quorum may not be set across the Windows Server Failover Cluster.
        /// </summary>
        [Browsable(false), LocDisplayName("Unknown")]
        Unknown = 4,
    }

    /// <summary>
    /// Connection intent modes of an Availability Replica in secondary role
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaConnectionModeInSecondaryRoleConverter))]
    public enum AvailabilityReplicaConnectionModeInSecondaryRole
    {
        /// <summary>
        /// The availability replica in secondary role will not allow any connections
        /// </summary>
        [LocDisplayName("cmsrNoConnections")]
        [TsqlSyntaxString("NO")]
        AllowNoConnections = 0,

        /// <summary>
        /// The availability replica in secondary role will allow only read-intent connections
        /// </summary>
        [LocDisplayName("cmsrReadIntentConnectionsOnly")]
        [TsqlSyntaxString("READ_ONLY")]
        AllowReadIntentConnectionsOnly = 1,

        /// <summary>
        /// The availability replica in secondary role will allow all connections.
        /// This is for client connections unaware of the availability replica participation in an availability group.
        /// </summary>
        [LocDisplayName("cmsrAllConnections")]
        [TsqlSyntaxString("ALL")]
        AllowAllConnections = 2,

        /// <summary>
        /// The availability replica in the secondary role is unknown. The replica may not be able to communicate with the cluster or quorum may not be set across the Windows Server Failover Cluster.
        /// </summary>
        [Browsable(false), LocDisplayName("Unknown")]
        Unknown = 3,
    }

    /// <summary>
    /// Seeding mode of Availability Replica
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaSeedingModeConverter))]
    public enum AvailabilityReplicaSeedingMode
    {
        /// <summary>
        /// Automatic mode
        /// </summary>
        [TsqlSyntaxString("AUTOMATIC")]
        [LocDisplayName("seedingModeAutomatic")]
        Automatic = 0,

        /// <summary>
        /// Manual Mode
        /// </summary>
        [TsqlSyntaxString("MANUAL")]
        [LocDisplayName("seedingModeManual")]
        Manual = 1,
    }

    /// <summary>
    /// The join state of an Availability Replica
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaJoinStateConverter))]
    public enum AvailabilityReplicaJoinState
    {
        /// <summary>
        /// The replica is not joined.
        /// </summary>
        [LocDisplayName("arjsNotJoined")]
        NotJoined = 0,

        /// <summary>
        /// The replica is a joined standalone instance.
        /// </summary>
        [LocDisplayName("arjsJoinedStandaloneInstance")]
        JoinedStandaloneInstance = 1,

        /// <summary>
        /// The replica is a joined SQL Server Failover Cluster Instance.
        /// </summary>
        [LocDisplayName("arjsJoinedFailoverClusterInstance")]
        JoinedFailoverClusterInstance = 2,

        /// <summary>
        /// The join state is unknown.
        /// </summary>
        [LocDisplayName("Unknown")]
        Unknown = 99
    }

    /// <summary>
    /// State of the Availability Group Listener IP Address
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupListenerIPStateConverter))]
    public enum AvailabilityGroupListenerIPState
    {
        /// <summary>
        /// Availability Group Listener IP resources is online
        /// </summary>
        [LocDisplayName("aglipOffline")]
        Offline = 0,

        /// <summary>
        ///  Availability Group Listener IP resources is offline
        /// </summary>
        [LocDisplayName("aglipOnline")]
        Online = 1,

        /// <summary>
        ///  Availability Group Listener IP resources is online pending
        /// </summary>
        [LocDisplayName("aglipOnlinePending")]
        OnlinePending = 2,

        /// <summary>
        ///  Availability Group Listener IP resources failed
        /// </summary>
        [LocDisplayName("agliFailure")]
        Failure = 3,

        /// <summary>
        /// unknown state for Availability Group Listener IP resources
        /// </summary>
        [LocDisplayName("agliUnknown")]
        Unknown = 4,
    }

    /// <summary>
    /// Availability modes of Availability Replica
    /// The int value has to match the values defined by the engine, can be found here:
    /// https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-availability-replicas-transact-sql
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaAvailabilityModeConverter))]
    public enum AvailabilityReplicaAvailabilityMode
    {

        /// <summary>
        /// The availability mode is ASynchronous Commit
        /// </summary>
        [TsqlSyntaxString("ASYNCHRONOUS_COMMIT")]
        [LocDisplayName("aramAsynchronousCommit")]
        AsynchronousCommit = 0,

        /// <summary>
        /// The availability mode is Synchronous Commit
        /// </summary>
        [TsqlSyntaxString("SYNCHRONOUS_COMMIT")]
        [LocDisplayName("aramSynchronousCommit")]
        SynchronousCommit = 1,

        /// <summary>
        /// The availability mode is Configuration Only
        /// </summary>
        [TsqlSyntaxString("CONFIGURATION_ONLY")]
        [LocDisplayName("aramConfigurationOnly")]
        ConfigurationOnly = 4,

        /// <summary>
        /// The availability mode is unknown. The replica may not be able to communicate with the cluster or quorum may not be set across the Windows Server Failover Cluster.
        /// </summary>
        [Browsable(false), LocDisplayName("Unknown")]
        Unknown = 2,
    }

    /// <summary>
    /// Failover modes of Availability Replica
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaFailoverModeConverter))]
    public enum AvailabilityReplicaFailoverMode
    {
        /// <summary>
        /// The failover mode is automatic
        /// </summary>
        [LocDisplayName("arfmAutomatic")]
        [TsqlSyntaxString("AUTOMATIC")]
        Automatic = 0,

        /// <summary>
        /// The failover mode is manual
        /// </summary>
        [LocDisplayName("arfmManual")]
        [TsqlSyntaxString("MANUAL")]
        Manual = 1,

        /// <summary>
        /// The failover mode is external, this is only applicable to External cluster type.
        /// </summary>
        [LocDisplayName("arfmExternal")]
        [TsqlSyntaxString("EXTERNAL")]
        External = 2,

        /// <summary>
        /// The failover mode is unknown. The replica may not be able to communicate with the cluster or quorum may not be set across the Windows Server Failover Cluster.
        /// </summary>
        [Browsable(false), LocDisplayName("Unknown")]
        Unknown = 3,
    }

    /// <summary>
    /// The different synchronization states a database participating in an HADR Availability Group can be in.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityDatabaseSynchronizationStateConverter))]
    public enum AvailabilityDatabaseSynchronizationState
    {
        /// <summary>
        /// The primary is not pushing data to the secondary.
        /// </summary>
        [LocDisplayName("adssNotSynchronizing")]
        NotSynchronizing = 0,

        /// <summary>
        /// Data movement is happening between the primary and the secondary. This will 
        /// be the state even if there is currently no data to be sent between the two.
        /// </summary>
        [LocDisplayName("adssSynchronizing")]
        Synchronizing = 1,

        /// <summary>
        /// The database replica is synchronized with the primary.
        /// </summary>
        [LocDisplayName("adssSynchronized")]
        Synchronized = 2,

        /// <summary>
        /// The database replica is reverting after a failover.
        /// </summary>
        [LocDisplayName("adssReverting")]
        Reverting = 3,

        /// <summary>
        /// The database replica is initializing after a failover.
        /// </summary>
        [LocDisplayName("adssInitializing")]
        Initializing = 4,
    }

    /// <summary>
    /// The different reasons for a database replica to be in suspended state.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.DatabaseReplicaSuspendReasonConverter))]
    public enum DatabaseReplicaSuspendReason
    {
        /// <summary>
        /// User initiated suspend command.
        /// </summary>
        [LocDisplayName("drsrSuspendFromUser")]
        SuspendFromUser = 0,

        /// <summary>
        /// Partner initiated suspend command.
        /// </summary>
        [LocDisplayName("drsrSuspendFromPartner")]
        SuspendFromPartner = 1,

        /// <summary>
        /// The database replica is currently in redo mode.
        /// </summary>
        [LocDisplayName("drsrSuspendFromRedo")]
        SuspendFromRedo = 2,

        /// <summary>
        /// The database replica is currently in apply mode.
        /// </summary>
        [LocDisplayName("drsrSuspendFromApply")]
        SuspendFromApply = 3,

        /// <summary>
        /// The database replica is currently in capture mode.
        /// </summary>
        [LocDisplayName("drsrSuspendFromCapture")]
        SuspendFromCapture = 4,

        /// <summary>
        /// The database replica is restarting.
        /// </summary>
        [LocDisplayName("drsrSuspendFromRestart")]
        SuspendFromRestart = 5,

        /// <summary>
        /// The database replica is currently in undo mode.
        /// </summary>
        [LocDisplayName("drsrSuspendFromUndo")]
        SuspendFromUndo = 6,

        /// <summary>
        /// The database is not suspended.
        /// </summary>
        [LocDisplayName("drsrNotApplicable")]
        NotApplicable = 7,
    }

    /// <summary>
    /// The different types of ways a cluster can decide on a quorum
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ClusterQuorumTypeConverter))]
    public enum ClusterQuorumType
    {
        /// <summary>
        /// Quorum is decided by node majority
        /// </summary>
        [LocDisplayName("cqtNodeMajority")]
        NodeMajority = 0,

        /// <summary>
        /// Quorum is decided by node and disk majority
        /// </summary>
        [LocDisplayName("cqtNodeAndDiskMajority")]
        NodeAndDiskMajority = 1,

        /// <summary>
        /// Quorum is decided by node and fileshare majority
        /// </summary>
        [LocDisplayName("cqtNodeAndFileshareMajority")]
        NodeAndFileshareMajority = 2,

        /// <summary>
        /// Quorum is decided by disk only vote
        /// </summary>
        [LocDisplayName("cqtDiskOnly")]
        DiskOnly = 3,

        /// <summary>
        /// The server is not in a Windows Cluster
        /// </summary>
        [LocDisplayName("cqtNotApplicable")]
        NotApplicable = 4,

        /// <summary>
        /// Quorum is decided by node and cloud majority
        /// </summary>
        [LocDisplayName("cqtCloudWitness")]
        CloudWitness = 5,

    }

    /// <summary>
    /// The current stat of the cluster quorum
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ClusterQuorumStateConverter))]
    public enum ClusterQuorumState
    {
        /// <summary>
        /// Cluster has unknown quorum
        /// </summary>
        [LocDisplayName("cqsUnknownQuorumState")]
        UnknownQuorumState = 0,

        /// <summary>
        /// Cluster has quorum
        /// </summary>
        [LocDisplayName("cqsNormalQuorum")]
        NormalQuorum = 1,

        /// <summary>
        /// Cluster is in forced quorum state
        /// </summary>
        [LocDisplayName("cqsForcedQuorum")]
        ForcedQuorum = 2,

        /// <summary>
        /// The server is not in a windows cluster
        /// </summary>
        [LocDisplayName("cqsNotApplicable")]
        NotApplicable = 3,
    }

    /// <summary>
    /// The type of node in the windows cluster
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ClusterMemberTypeConverter))]
    public enum ClusterMemberType
    {
        /// <summary>
        /// A node member
        /// </summary>
        [LocDisplayName("cmtNode")]
        Node = 0,

        /// <summary>
        /// A disk witness member
        /// </summary>
        [LocDisplayName("cmtDiskWitness")]
        DiskWitness = 1,

        /// <summary>
        /// A fileshare witness member
        /// </summary>
        [LocDisplayName("cmtFileshareWitness")]
        FileshareWitness = 2,

        /// <summary>
        /// A cloud witness member
        /// </summary>
        [LocDisplayName("cmtCloudWitness")]
        CloudWitness = 3,
    }

    /// <summary>
    /// The state of a member in a Windows Cluster.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ClusterMemberStateConverter))]
    public enum ClusterMemberState
    {
        /// <summary>
        /// The member is offline.
        /// </summary>
        [LocDisplayName("cmsOffline")]
        Offline = 0,

        /// <summary>
        /// The member is online.
        /// </summary>
        [LocDisplayName("cmsOnline")]
        Online = 1,

        /// <summary>
        /// The member is online.
        /// </summary>
        [LocDisplayName("cmsPartiallyOnline")]
        PartiallyOnline = 2,

        /// <summary>
        /// The member is unknown
        /// </summary>
        [LocDisplayName("cmsUnknown")]
        Unknown = 3,

    }

    /// <summary>
    /// This enumeration specifies how replicas in the primary role are treated in the evaluation to pick the desired replica to perform a backup.  
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupAutomatedBackupPreferenceConverter))]
    public enum AvailabilityGroupAutomatedBackupPreference
    {
        /// <summary>
        /// Backups occur only on the primary replica, wherever it is.
        /// </summary>
        [LocDisplayName("agabpPrimary")]
        Primary = 0,

        /// <summary>
        /// Backups occur only on the secondary replicas. If no secondary replicas are online, backup will not be performed.
        /// </summary>
        [LocDisplayName("agabpSecondaryOnly")]
        SecondaryOnly = 1,

        /// <summary>
        /// Backups occur on the secondary replicas, except when the primary replica is the only replica online.
        /// </summary>
        [LocDisplayName("agabpSecondary")]
        Secondary = 2,

        /// <summary>
        /// No preference is stated for backup on replicas based on its current role. Only BackupPriority and online/connected states will be considered for replica choice.
        /// </summary>
        [LocDisplayName("agabpNone")]
        None = 3,

        /// <summary>
        /// The automated backup preference is unknown. The replica may not be able to communicate with the cluster or quorum may not be set across the Windows Server Failover Cluster.
        /// </summary>
        [Browsable(false), LocDisplayName("Unknown")]
        Unknown = 4,
    }

    /// <summary>
    /// The different conditions that can trigger an automatic failover in an Availability Group.
    /// These setting are cumulative, meaning that as the setting increases in value it encompases all the previous conditions
    /// and adds extra ones.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupFailureConditionLevelConverter))]
    public enum AvailabilityGroupFailureConditionLevel
    {
        /// <summary>
        /// Automatic failover is triggered when the SQL Server service is down.
        /// </summary>
        [LocDisplayName("agfcOnServerDown")]
        OnServerDown = 1,

        /// <summary>
        /// Automatic failover is triggered when SQL Server is down or unresponsive, or when the Avaialbility Group's primary replica is in a failed state.
        /// </summary>
        [LocDisplayName("agfcOnServerUnresponsive")]
        OnServerUnresponsive = 2,

        /// <summary>
        /// Automatic failover is triggered when any condition level of lower value is satisfied or when a critical server error occurs.
        /// If no setting for an Availability Group is specified, this is the default value.
        /// </summary>
        [LocDisplayName("agfcOnCriticalServerErrors")]
        OnCriticalServerErrors = 3,

        /// <summary>
        /// Automatic failover is triggered when any condition level of lower value is satisfied or when a moderate server error occurs.
        /// </summary>
        [LocDisplayName("agfcOnModerateServerErrors")]
        OnModerateServerErrors = 4,

        /// <summary>
        /// Automatic failover is triggered when any condition level of lower value is satisfied or when a qualifying failure condition occurs.
        /// </summary>
        [LocDisplayName("agfcOnAnyQualifiedFailureCondition")]
        OnAnyQualifiedFailureCondition = 5,

        /// <summary>
        /// The failure condition level is unknown. The replica may not be able to communicate with the cluster or quorum may not be set across the Windows Server Failover Cluster.
        /// </summary>
        [Browsable(false), LocDisplayName("Unknown")]
        Unknown = 6,
    }

    /// <summary>
    /// Cluster type of the Availability Group
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupClusterTypeConverter))]
    public enum AvailabilityGroupClusterType
    {
        /// <summary>
        /// The availability group is stored in WSFC.
        /// </summary>
        [LocDisplayName("agctWsfc")]
        Wsfc = 0,

        /// <summary>
        /// The availability group is cluster-independent.
        /// </summary>
        [LocDisplayName("agctNone")]
        None = 1,

        /// <summary>
        /// The availability group uses external cluster solutions.
        /// </summary>
        [LocDisplayName("agctExternal")]
        External = 2,
    }
    #endregion

    /// <summary>
    /// Type of a filegroup file
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.FileGroupTypeConverter))]
    public enum FileGroupType
    {
        /// <summary>
        /// Standard rows file group
        /// </summary>
        [LocDisplayName("fgtRowsFileGroup")]
        RowsFileGroup = 0,

        /// <summary>
        /// A FileGroup used for file stream data
        /// </summary>
        [LocDisplayName("fgtFileStreamDataFileGroup")]
        FileStreamDataFileGroup = 2,

        /// <summary>
        /// A FileGroup used for memory optimized data
        /// </summary>
        [LocDisplayName("fgtMemoryOptimizedDataFileGroup")]
        MemoryOptimizedDataFileGroup = 3
    }

    /// <summary>
    /// Security predicate type
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.SecurityPredicateTypeConverter))]
    public enum SecurityPredicateType
    {
        /// <summary>
        /// A filter predicate.
        /// </summary>
        [LocDisplayName("securityPredicateTypeFilter")]
        [TsqlSyntaxString("FILTER")]
        Filter = 0,

        /// <summary>
        /// A block predicate.
        /// </summary>
        [LocDisplayName("securityPredicateTypeBlock")]
        [TsqlSyntaxString("BLOCK")]
        Block = 1
    }

    /// <summary>
    /// Security predicate operation types
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.SecurityPredicateOperationConverter))]
    public enum SecurityPredicateOperation
    {
        /// <summary>
        /// Predicate is applied for all applicable operations.
        /// </summary>
        [LocDisplayName("securityPredicateOperationAll")]
        [TsqlSyntaxString("")]
        All = 0,

        /// <summary>
        /// Predicate is applied after insert operations.
        /// </summary>
        [LocDisplayName("securityPredicateOperationAfterInsert")]
        [TsqlSyntaxString("AFTER INSERT")]
        AfterInsert = 1,

        /// <summary>
        /// Predicate is applied after update operations.
        /// </summary>
        [LocDisplayName("securityPredicateOperationAfterUpdate")]
        [TsqlSyntaxString("AFTER UPDATE")]
        AfterUpdate = 2,

        /// <summary>
        /// Predicate is applied before update operations.
        /// </summary>
        [LocDisplayName("securityPredicateOperationBeforeUpdate")]
        [TsqlSyntaxString("BEFORE UPDATE")]
        BeforeUpdate = 3,

        /// <summary>
        /// Predicate is applied before delete operations.
        /// </summary>
        [LocDisplayName("securityPredicateOperationBeforeDelete")]
        [TsqlSyntaxString("BEFORE DELETE")]
        BeforeDelete = 4
    }

    /// <summary>
    /// Database Scoped Configuration on and off states.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.DatabaseScopedConfigurationOnOffConverter))]
    public enum DatabaseScopedConfigurationOnOff
    {
        /// <summary>
        /// Off.
        /// </summary>
        [LocDisplayName("dbScopedConfigurationOff")]
        [TsqlSyntaxString("OFF")]
        Off = 0,

        /// <summary>
        /// On.
        /// </summary>
        [LocDisplayName("dbScopedConfigurationOn")]
        [TsqlSyntaxString("ON")]
        On = 1,

        /// <summary>
        /// Follow the same value as the primary (only applicable to secondaries).
        /// </summary>
        [LocDisplayName("dbScopedConfigurationPrimary")]
        [TsqlSyntaxString("PRIMARY")]
        Primary = 2,
    }

    /// <summary>
    /// Resumable Operation State: Running, Paused, None.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.ResumableOperationStateTypeConverter))]
    public enum ResumableOperationStateType
    {
        /// <summary>
        /// Running.
        /// </summary>
        [LocDisplayName("ResumableOperationStateTypeRunning")]
        [TsqlSyntaxString("RUNNING")]
        Running = 0,

        /// <summary>
        /// Paused.
        /// </summary>
        [LocDisplayName("ResumableOperationStateTypePaused")]
        [TsqlSyntaxString("PAUSED")]
        Paused = 1,

        /// <summary>
        /// None.
        /// </summary>
        [LocDisplayName("ResumableOperationStateTypeNone")]
        [TsqlSyntaxString("NONE")]
        None = 2,
    }

    /// <summary>
    /// Temporal retention period unit description
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.TemporalHistoryRetentionPeriodUnitTypeConverter))]
    public enum TemporalHistoryRetentionPeriodUnit
    {
        /// <summary>
        /// Retention undefined - non-temporal table
        /// </summary>
        [TsqlSyntaxString("UNDEFINED")]
        [LocDisplayName("Undefined")]
        Undefined = -2,
        /// <summary>
        /// Infinite (not specified) retention
        /// </summary>
        [TsqlSyntaxString("INFINITE")]
        [LocDisplayName("Infinite")]
        Infinite = -1,
        /// <summary>
        /// Day
        /// </summary>
        [TsqlSyntaxString("DAY")]
        [LocDisplayName("Day")]
        Day = 3,
        /// <summary>
        /// Week
        /// </summary>
        [TsqlSyntaxString("WEEK")]
        [LocDisplayName("Week")]
        Week = 4,
        /// <summary>
        /// Month
        /// </summary>
        [TsqlSyntaxString("MONTH")]
        [LocDisplayName("Month")]
        Month = 5,
        /// <summary>
        /// Year
        /// </summary>
        [TsqlSyntaxString("YEAR")]
        [LocDisplayName("Year")]
        Year = 6,
    }

    /// <summary>
    /// Temporal retention period unit description
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.DataRetentionPeriodUnitTypeConverter))]
    public enum DataRetentionPeriodUnit
    {
        /// <summary>
        /// Infinite (not specified) retention
        /// </summary>
        [TsqlSyntaxString("INFINITE")]
        [LocDisplayName("Infinite")]
        Infinite = -1,
        /// <summary>
        /// Day
        /// </summary>
        [TsqlSyntaxString("DAY")]
        [LocDisplayName("Day")]
        Day = 3,
        /// <summary>
        /// Week
        /// </summary>
        [TsqlSyntaxString("WEEK")]
        [LocDisplayName("Week")]
        Week = 4,
        /// <summary>
        /// Month
        /// </summary>
        [TsqlSyntaxString("MONTH")]
        [LocDisplayName("Month")]
        Month = 5,
        /// <summary>
        /// Year
        /// </summary>
        [TsqlSyntaxString("YEAR")]
        [LocDisplayName("Year")]
        Year = 6,
    }

    /// <summary>
    /// Catalog Collation Type values.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.CatalogCollationTypeConverter))]
    public enum CatalogCollationType
    {
        /// <summary>
        /// Database Default, metadata Collation matches the data collation.
        /// </summary>
        [LocDisplayName("dbCatalogCollationDatabaseDefault")]
        [TsqlSyntaxString("DATABASE_DEFAULT")]
        DatabaseDefault = 0,

        /// <summary>
        /// Fixed Contained database metadata collation.
        /// </summary>
        [LocDisplayName("dbCatalogCollationContained")]
        [TsqlSyntaxString("Latin1_General_100_CI_AS_KS_WS_SC")]
        ContainedDatabaseFixedCollation = 1,

        /// <summary>
        /// Fixed SQL_Latin1_General_CP1_CI_AS metadata collation.
        /// </summary>
        [LocDisplayName("dbCatalogCollationSQL_Latin1_General_CP1_CI_AS")]
        [TsqlSyntaxString("SQL_Latin1_General_CP1_CI_AS")]
        SQLLatin1GeneralCP1CIAS = 2,
    }

    /// <summary>
    /// Specifies the default importance of a request for the workload group.
    /// A user can also set importance at the classifier level, which can override the workload group importance setting.
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.WorkloadManagementImportanceConverter))]
    public enum WorkloadManagementImportance
    {
        /// <summary>
        /// Importance for workload management object is Low
        /// </summary>
        [TsqlSyntaxString("LOW")]
        Low = 0,
        /// <summary>
        /// Importance for workload management object is Below Normal
        /// </summary>
        [TsqlSyntaxString("BELOW_NORMAL")]
        BelowNormal = 1,
        /// <summary>
        /// Importance for workload management object is Normal (default)
        /// </summary>
        [TsqlSyntaxString("NORMAL")]
        Normal = 2,
        /// <summary>
        /// Importance for workload management object is Above Normal
        /// </summary>
        [TsqlSyntaxString("ABOVE_NORMAL")]
        AboveNormal = 3,
        /// <summary>
        /// Importance for workload management object is High
        /// </summary>
        [TsqlSyntaxString("HIGH")]
        High = 4,
    }

}


namespace Microsoft.SqlServer.Management.Smo.Agent
{
    // WARNING!!
    // DO NOT CHANGE THE ORDER OF THOSE MEMBERS
    // UI DEPENDS ON THIS!!
    ///
    public enum ActivationOrder
    {
        ///
        First = 0,
        ///
        None = 1,
        ///
        Last = 2
    }

    ///<summary>
    ///specifies the level of information that is logged in the SQL Server Agent error log.</summary>
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum AgentLogLevels
    {
        /// <summary>
        /// Log errors.     </summary>
        Errors = 1,
        /// <summary>
        /// Log warning messages.</summary>
        Warnings = 2,
        /// <summary>
        /// Log informational messages.</summary>
        Informational = 4,
        ///
        All = Errors | Warnings | Informational
    }

    /// <summary>
    /// Specifies mail subsystems that can be used by SqlAgent
    /// </summary>
    public enum AgentMailType
    {
        ///
        SqlAgentMail,
        ///
        DatabaseMail
    }

    /// <summary>
    /// Since proxy accounts are tightly coupled with SQL Agent subsystems and we don't have a special 
    /// SubSystem object, but rather an enum.
    /// The values of this enum must match the service_id value from the engine
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [TypeConverter(typeof(AgentSubSystemTypeConverter))]
    public enum AgentSubSystem
    {
        /// <summary>
        /// Starting from 1 since that's how sp_enum_sqlagent_subsystems returns it
        /// Transact-SQL script (T-SQL)
        /// </summary>
        [LocDisplayName("TransactSql")]
        [TsqlSyntaxString("TSQL")]
        TransactSql = 1,
        /// <summary>
        /// ActiveX Scripting
        /// </summary>
        [LocDisplayName("ActiveScripting")]
        [TsqlSyntaxString("ActiveScripting")]
        ActiveScripting,
        /// <summary>
        /// Operating system (CmdExec)
        /// </summary>
        [LocDisplayName("CmdExec")]
        [TsqlSyntaxString("CmdExec")]
        CmdExec,
        /// <summary>
        /// Replication snapshot
        /// </summary>
        [LocDisplayName("ReplSnapshot")]
        [TsqlSyntaxString("Snapshot")]
        Snapshot,
        /// <summary>
        /// Replication Transaction-Log reader
        /// </summary>
        [LocDisplayName("ReplLogReader")]
        [TsqlSyntaxString("LogReader")]
        LogReader,
        /// <summary>
        /// Replication Distributor
        /// </summary>
        [LocDisplayName("ReplDistribution")]
        [TsqlSyntaxString("Distribution")]
        Distribution,
        /// <summary>
        /// Replication Merge
        /// </summary>
        [LocDisplayName("ReplMerge")]
        [TsqlSyntaxString("Merge")]
        Merge,
        /// <summary>
        /// Replication Queue Reader
        /// </summary>
        [LocDisplayName("ReplQueueReader")]
        [TsqlSyntaxString("QueueReader")]
        QueueReader,
        /// <summary>
        /// SQL Server Analysis Services query
        /// </summary>
        [LocDisplayName("AnalysisQuery")]
        [TsqlSyntaxString("ANALYSISQUERY")]
        AnalysisQuery,
        /// <summary>
        /// SQL Servier Analysis Services Command
        /// </summary>
        [LocDisplayName("AnalysisCommand")]
        [TsqlSyntaxString("ANALYSISCOMMAND")]
        AnalysisCommand,
        /// <summary>
        /// SQL Server Integration Services package
        /// </summary>
        [LocDisplayName("SSIS")]
        [TsqlSyntaxString("SSIS")]
        Ssis,
        /// <summary>
        /// PowerShell subsystem
        /// </summary>
        [LocDisplayName("PowerShell")]
        [TsqlSyntaxString("PowerShell")]
        PowerShell
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum AlertType
    {
        ///
        SqlServerEvent = 1,
        ///
        SqlServerPerformanceCondition = 2,
        ///
        NonSqlServerEvent = 3,
        ///
        WmiEvent = 4
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum CategoryType
    {
        ///
        LocalJob = 1,
        ///
        MultiServerJob = 2,
        ///
        None = 3
    }

    ///
    public enum CompletionAction
    {
        ///
        Never = 0,
        ///
        OnSuccess = 1,
        ///
        OnFailure = 2,
        ///
        Always = 3,
    }

    ///
    public enum CompletionResult
    {
        ///
        Failed = 0,
        ///
        Succeeded = 1,
        ///
        Retry = 2,
        ///
        Cancelled = 3,
        ///
        InProgress = 4,
        ///
        Unknown = 5
    }

    /// Note that this enum's values MUST match the definition of enum MonthRelativeInterval in native
    /// code, defined in qsched.h and used by SqlAgent.
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum FrequencyRelativeIntervals
    {
        ///
        First = 1,
        ///
        Second = 2,
        ///
        Third = 4,
        ///
        Fourth = 8,
        ///
        Last = 16
    }

    /// Note that this enum's values MUST match the definition of enum FreqSubTypes in native
    /// code, defined in qsched.h and used by SqlAgent.
    [Flags]
    public enum FrequencySubDayTypes
    {
        /// <summary>
        /// Schedule reflects an activity scheduled using an hour as the unit. </summary>
        Hour = 8,
        /// <summary>
        /// Schedule reflects an activity scheduled using a minute as the unit. </summary>
        Minute = 4,
        /// <summary>
        /// Schedule reflects an activity scheduled using a second as the unit. </summary>
        Second = 2,
        /// <summary>
        /// Schedule reflects an activity that occurs once on a scheduled unit. </summary>
        Once = 1,
        /// <summary>
        /// Subunits are invalid for the scheduled activity. </summary>
        Unknown = 0,
    }

    /// Note that this enum's values MUST match the definition of enum FreqTypes in native
    /// code, defined in qsched.h and used by SqlAgent.
    [Flags]
    public enum FrequencyTypes
    {
        /// <summary>
        /// Scheduled activity is started when SQL Server Agent service starts. </summary>
        AutoStart = 64,
        /// <summary>
        /// Schedule is evaluated daily. This has special handling for sub-day types; see the
        /// FrequencySubDayTypes enum above that let the schedule run on a second/minute/hour
        /// basis during a single day. </summary>
        Daily = 4,
        /// <summary>
        /// Schedule is evaluated monthly. </summary>
        Monthly = 16,
        /// <summary>
        /// Schedule is evaluated relative to a part of a month, such as the second week. </summary>
        MonthlyRelative = 32,
        /// <summary>
        /// Scheduled activity will occur once at a scheduled time or event. </summary>
        OneTime = 1,
        /// <summary>
        /// SQL Server Agent service will schedule the activity for any time during which the processor is idle. </summary>
        OnIdle = 128,
        /// <summary>
        /// No schedule frequency, or frequency not applicable. </summary>
        Unknown = 0,
        /// <summary>
        /// Schedule is evaluated weekly. </summary>
        Weekly = 8
    }

    /// <summary>
    /// Represents current_execution_status values from sp_help_job result
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.JobExecutionStatusConverter))]
    public enum JobExecutionStatus
    {
        /// <summary>
        /// Executing
        /// </summary>
        [LocDisplayName("Executing")]
        Executing = 1,

        /// <summary>
        /// Waiting for thread
        /// </summary>
        [LocDisplayName("WaitingForWorkerThread")]
        WaitingForWorkerThread = 2,

        /// <summary>
        /// Between retries
        /// </summary>
        [LocDisplayName("BetweenRetries")]
        BetweenRetries = 3,

        /// <summary>
        /// Idle
        /// </summary>
        [LocDisplayName("Idle")]
        Idle = 4,

        /// <summary>
        /// Suspended
        /// </summary>
        [LocDisplayName("Suspended")]
        Suspended = 5,

        /// <summary>
        /// Obsolete
        /// </summary>
        [LocDisplayName("WaitingForStepToFinish")]
        WaitingForStepToFinish = 6,

        /// <summary>
        /// Performing completion action
        /// </summary>
        [LocDisplayName("PerformingCompletionAction")]
        PerformingCompletionAction = 7,

        /// <summary>
        /// Queued
        /// </summary>
        [LocDisplayName("Queued")]
        Queued = 8
    }

    ///
    public enum JobOutcome
    {
        /// <summary>
        /// Execution canceled by user action. </summary>
        Cancelled = 3,
        /// <summary>
        /// Execution failed. </summary>
        Failed = 0,
        /// <summary>
        /// Job or job step is executing. </summary>
        InProgress = 4,
        /// <summary>
        /// Execution succeeded. </summary>
        Succeeded = 1,
        /// <summary>
        /// Unable to determine execution state. </summary>
        Unknown = 5
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum JobServerType
    {
        ///
        Standalone = 1,
        ///
        Tsx = 2,
        ///
        Msx = 3
    }

    /// <summary>
    /// controls parts of the behavior of the job step
    /// </summary>
    [Flags]
    public enum JobStepFlags
    {
        ///
        None = 0,
        /// <summary>
        /// Appends the job output (if any) to the log file, if any log file 
        /// has been specified. If not set, the log file is overwritten
        /// </summary>
        AppendToLogFile = 2,
        /// <summary>
        /// appends step output to the job history table (sysjobhistory)
        /// </summary>
        AppendToJobHistory = 4,
        ///
        LogToTableWithOverwrite = 8,
        ///
        AppendToTableLog = 16,
        ///
        AppendAllCmdExecOutputToJobHistory = 32,
        ///
        ProvideStopProcessEvent = 64
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum JobType
    {
        ///
        Local = 1,
        ///
        MultiServer = 2
    }

    ///
    [Flags]
    public enum NotifyMethods
    {
        ///
        None = 0,
        ///
        NotifyEmail = 1,
        ///
        Pager = 2,
        ///
        NetSend = 4,
        ///
        NotifyAll = 7
    }

    /// <summary>
    /// controls execution thread scheduling for job steps executing operating system tasks</summary>
    public enum OSRunPriority
    {
        ///
        AboveNormal = 1,
        ///
        BelowNormal = -1,
        ///
        Idle = -15,
        ///
        Normal = 0,
        ///
        TimeCritical = 15
    }

    ///
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum StepCompletionAction
    {
        ///
        QuitWithSuccess = 1,
        ///
        QuitWithFailure = 2,
        ///
        GoToNextStep = 3,
        ///
        GoToStep = 4
    }

    ///
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum TargetServerStatus
    {
        ///
        Normal = 1,
        ///
        SuspectedOffline = 2,
        ///
        Blocked = 4
    }

    /// Note that this enum's values MUST match the definition of enum SchDayOfWeek in native
    /// code, defined in qsched.h and used by SqlAgent.
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum WeekDays
    {
        ///
        Sunday = 1,
        ///
        Monday = 2,
        ///
        Tuesday = 4,
        ///
        Wednesday = 8,
        ///
        Thursday = 16,
        ///
        Friday = 32,
        ///
        Saturday = 64,
        /// The native enum SchDayOfWeek ends with Saturday. The following values are not part of
        /// the native enum and are added to SMO as a convenience of AND'd bitfields.
        EveryDay = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday,
        ///
        WeekDays = Monday | Tuesday | Wednesday | Thursday | Friday,
        ///
        WeekEnds = Saturday | Sunday
    }

    /// Note that this enum's values MUST match the definition of enum MonthRelativeTypes in native
    /// code, defined in qsched.h and used by SqlAgent.
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum MonthlyRelativeWeekDays
    {
        ///
        Sunday = 1,
        ///
        Monday = 2,
        ///
        Tuesday = 3,
        ///
        Wednesday = 4,
        ///
        Thursday = 5,
        ///
        Friday = 6,
        ///
        Saturday = 7,
        ///
        EveryDay = 8,
        ///
        WeekDays = 9,
        ///
        WeekEnds = 10
    }
}

namespace Microsoft.SqlServer.Management.Dmf
{

    /// <summary>
    /// This enum is used to represent the policy health states.
    /// Note: We only use Critical and Unknown for now.
    /// </summary>
    public enum PolicyHealthState
    {
        /// Not enough information is available for this target.
        Unknown = 0,
        /// The target is in violation of one or more policies
        Critical = 1,
        /// The target is confirmed to be healthy
        Healthy = 2,
        /// There is no policy that controls this target
        NoPolicy = 3
    }
}
