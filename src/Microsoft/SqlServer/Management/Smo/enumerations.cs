// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// List of options that may be passed to Server.AttachDatabase
    /// </summary>
    public enum AttachOptions
    {
        /// Attach without any options specified
        None                     = 0,
        /// Enables the Service Broker
        EnableBroker             = 1,
        /// Creates a new broker
        NewBroker                = 2,
        /// Stops all active broker conversations at moment of save point with an error message
        ErrorBrokerConversations = 3, 
        /// Specifies the database is created by attaching an existing set of operating system files. If one or more transaction log files are missing, the log file is rebuilt.
        RebuildLog               = 4
    }

    /// <summary>
    /// List of object and database privileges
    /// </summary>
    [Flags]
    public enum PrivilegeTypes
    {
        Unknown = 0,

        // Object privileges.
        Select          = 0x00000001,
        Insert          = 0x00000002,
        Update          = 0x00000004,
        Delete          = 0x00000008,
        Execute         = 0x00000010,
        References      = 0x00000020,
        ViewDefinition  = 0x00100000,
        Control             = 0x00200000,
        Alter           = 0x00400000,
        Drop            = 0x00800000,
        AllObjectPrivileges     = Select | Insert | Update | Delete |
            Execute | References | ViewDefinition | Control | 
            Alter | Drop,

        // Database (statement) privileges.
        CreateTable         = 0x00000080,// 128
        CreateDatabase  = 0x00000100,//256
        CreateView          = 0x00000200,//512,
        CreateProcedure     = 0x00000400,//1024,
        DumpDatabase        = 0x00000800,//2048,
        CreateDefault       = 0x00001000,//4096,
        DumpTransaction     = 0x00002000,//8192,
        CreateRule          = 0x00004000,//16384,
        DumpTable           = 0x00008000,//32768,
        CreateFunction      = 0x00010000,//65536,
        CreateType          = 0x00020000,//65536,
        AllDatabasePrivileges   = 0x0003ff80, //130944
            
        BackupDatabase      = 0x00040000,
        BackupLog           = 0x00080000
    }


    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum StatisticsScanType
    {
        Default = 5,
        Resample = 4,
        FullScan = 3, //Perform a full scan of the index or column to determine statistics values. 
        Percent = 1 , //Perform a sampled scan using a percentage value. When specified, use the ScanNumber value to indicate percentage. Specify percentage using a whole number, for example, 55 specifies 55 percent. 
        Rows = 2  //Perform a sampled scan using a number of rows. When specified, use the ScanNumber argument to indicate number of rows. 
    }

    public enum StatisticsTarget
    {
        All = 2, //Update all statistics regardless of the source. 
        Column = 1, // Update statistics derived from column data only. 
        Index = 0, //Default. Update statistics derived from indexes only. 
    }

    /// <summary>
    /// List of SQL Server types of objects
    /// </summary>
    //[Flags]
    internal enum ObjectType 
    {
        Unknown = 16384,        // Make it the only bit set
        Application = 0,

        UserDefinedDataType         = 0x00000001,    // 1
        SystemTable             = 0x00000002,    // 2
        View                        = 0x00000004,    // 4
        UserTable                   = 0x00000008,    // 8
        StoredProcedure             = 0x00000010,    // 16
        Default                     = 0x00000040,    // 64
        Rule                        = 0x00000080,    // 128
        Trigger                         = 0x00000100,    // 256
        XmlSchemaCollection     = 0x00000200,   // 512
        UserDefinedTableType    = 0x00000400,   // 1024
        UserDefinedFunction         = 0x00001000,    

        /*
                AllDatabaseUserObjects      = 0x000011fd,    // All but system tables
                AllDatabaseObjects          = 0x000011ff,    // All including system tables
                AllButSystemObjects             = 0x000013ff,    // All but system objects
        */      

        // Other Database objects (not in sysobjects)
        SystemDataType =                0x00001000,
        User =                          0x00002000,
        Group =                         0x00003000,
        Index =                         0x00004000,
        Key =                           0x00005000,
        Column =                        0x00006000,
        DBObject =                      0x00007000,
        DBOption =                      0x00008000,
        ProcedureParameter =            0x00009000,
        Permission =                    0x0000A000,
        IntegratedSecurity =            0x0000B000,
        Check =                         0x0000C000,
        DRIDefault =                    0x0000D000,
        EdgeConstraint =                0x0000E000,


        // Objects not in databases.
        SqlServer =                     0x00020000,
        Database =                      0x00021000,
        BackupDevice =                  0x00022000,
        Login =                         0x00023000,
        Language =                      0x00024000,
        RemoteServer =                  0x00025000,
        RemoteLogin =                   0x00026000,
        Configuration =                 0x00027000,
        ConfigValue =                   0x00028000,
        QueryResults =                  0x00029000,
        TransactionLog =                0x0002A000,
        Registry =                      0x0002B000,
        Transfer =                      0x0002C000,
        Backup =                        0x0002D000,
        AutoProperty =                  0x0002E000,
        ServerGroup =                   0x0002F000,
        RegisteredServer =              0x00031000,
        BulkCopy =                      0x00032000,
        FileGroup =                     0x00033000,
        DBFile =                        0x00034000,
        LogFile =                       0x00035000,
        ServerRole =                    0x00036000,
        DatabaseRole =                  0x00037000,
        Restore =                       0x00038000,
        LinkedServer =                  0x00039000,
        LinkedServerLogin =             0x0003A000,
        FullTextCatalog =               0x0003B000,
        FullTextService =               0x0003C000,
    }

    /// <summary>
    /// List of SQL Server repair types
    /// </summary>
    public enum RepairType
    {
        None                = 0 ,
        Fast                = 1,
        Rebuild         = 2,
        AllowDataLoss   = 3 
    }

    internal enum PropertyBagState
    {
        Empty = 0x01,
        Lazy = 0x02,
        Full = 0x04,
        Unknown = 0x10
    }

    public enum ShrinkMethod 
    {
        Default = 0,
        NoTruncate = 1,
        TruncateOnly = 2, 
        EmptyFile = 3
    }

    /// <summary>
    /// Specifies the types of transactions that may be active in a database
    /// </summary>
    [Flags]
    public enum TransactionTypes
    {
        /// <summary>
        /// A Versioned transaction has a row in sys.dm_tran_active_snapshot_database_transactions
        /// </summary>
        Versioned = 1,
        /// <summary>
        /// An UnVersioned transaction does not have a row in sys.dm_tran_active_snapshot_database_transactions
        /// </summary>
        UnVersioned = 2,
        /// <summary>
        /// Specifies inclusion of both Versioned and UnVersioned transactions
        /// </summary>
        Both = Versioned | UnVersioned
    }

    /// <summary>
    /// Types of tables that can be enumerated on the linked server
    /// </summary>
    public enum LinkedTableType 
    {
        Default = 0, // No restriction 
        Alias = 1, // Restrict result set membership to alias tables 
        GlobalTemporary = 2, // Restrict result set membership to global temporary tables 
        LocalTemporary = 3, // Restrict result set membership to local temporary tables 
        SystemTable = 4, // Restrict result set membership to system tables 
        Table = 5, // Restrict result set membership to user tables 
        View = 6, // Restrict result set membership to views 
        SystemView = 7 // Restrict result set membership to System views 
    }

    /// <summary>
    /// specifies how an index should be re-enabled
    /// </summary>

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum IndexEnableAction
    {
        /// <summary>
        /// The index will be rebuilt with the current property-settings
        /// </summary>
        Rebuild = 1,        
        /// <summary>
        /// The existing index will be dropped and recreated with the current property-settings.
        /// </summary>
        Recreate = 2        
    }

    /// <summary>
    /// specifies what operation to perform on an index alter
    /// </summary>
    public enum IndexOperation
    {
        /// <summary>
        /// The index will be rebuilt with the current property-settings.
        /// </summary>
        Rebuild = 0,
        /// <summary>
        /// The index will be reorganized with the current property-settings.
        /// </summary>
        Reorganize = 1,
        /// <summary>
        /// The Index will be disabled.
        /// </summary>
        Disable = 2
    }

    /// <summary>
    /// specifies how index fragmentation will be retrieved
    /// </summary>

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum FragmentationOption
    {
        Fast        = 1,
        Sampled     = 2,
        Detailed    = 3
    }

    [Flags]
    public enum SqlServerVersions
    {
        Unknown = 0,
        Version70 = 1,
        Version80 = 2,
        Version90 = 4,
        Version100 = 8,
        Version105 = 16,
        Version110 = 32,
        Version120 = 64,
        Version130 = 128,
        Version140 = 256,
        Version150 = 512
        // VBUMP
    }

    public enum SortOrder
    {
        Name,
        Schema,
        Type,
        Urn
    }

    // WARNING: we have overflowed this enum, int is no longer large enough
    // uint appears to not be CLS compliant so we can't use that
    // this will have to be better scaled, but changing to long for now
    // long can have up to 63 positions 2^63 = 0x4fffffffffffffff so expanding
    // further https://docs.microsoft.com/en-us/dotnet/api/system.int64?view=net-5.0
    //
    [Flags]
    public enum DatabaseObjectTypes : long
    {
        ApplicationRole                      = 0x0000000000000001,
        ServiceBroker                        = 0x0000000000000002,
        Default                              = 0x0000000000000004,
        ExtendedStoredProcedure              = 0x0000000000000008,
        FullTextCatalog                      = 0x0000000000000010,
        MessageType                          = 0x0000000000000020,
        PartitionFunction                    = 0x0000000000000040,
        PartitionScheme                      = 0x0000000000000080,
        DatabaseRole                         = 0x0000000000000100,
        RemoteServiceBinding                 = 0x0000000000000200,
        Rule                                 = 0x0000000000000400,
        Schema                               = 0x0000000000000800,
        ServiceContract                      = 0x0000000000001000,
        ServiceQueue                         = 0x0000000000002000,
        ServiceRoute                         = 0x0000000000004000,
        SqlAssembly                          = 0x0000000000008000,
        StoredProcedure                      = 0x0000000000010000,
        Synonym                              = 0x0000000000020000,
        Table                                = 0x0000000000040000,
        User                                 = 0x0000000000080000,
        UserDefinedAggregate                 = 0x0000000000100000,
        UserDefinedDataType                  = 0x0000000000200000,
        UserDefinedFunction                  = 0x0000000000400000,
        UserDefinedType                      = 0x0000000000800000,
        View                                 = 0x0000000001000000,
        XmlSchemaCollection                  = 0x0000000002000000,
        SymmetricKey                         = 0x0000000004000000,
        Certificate                          = 0x0000000008000000,
        AsymmetricKey                        = 0x0000000010000000,
        UserDefinedTableTypes                = 0x0000000020000000,
        PlanGuide                            = 0x0000000040000000,
        DatabaseEncryptionKey                = 0x0000000080000000,
        DatabaseAuditSpecification           = 0x0000000100000000,
        FullTextStopList                     = 0x0000000200000000,
        SearchPropertyList                   = 0x0000000400000000,
        Sequence                             = 0x0000000800000000,
        SecurityPolicy                       = 0x0000001000000000,
        ExternalDataSource                   = 0x0000002000000000,
        ExternalFileFormat                   = 0x0000004000000000,
        ColumnMasterKey                      = 0x0000008000000000,
        ColumnEncryptionKey                  = 0x0000010000000000,
        QueryStoreOptions                    = 0x0000020000000000,
        DatabaseScopedCredential             = 0x0000040000000000,
        DatabaseScopedConfiguration          = 0x0000080000000000,
        ExternalLibrary                      = 0x0000100000000000,
        WorkloadManagementWorkloadGroup      = 0x0000200000000000,
        WorkloadManagementWorkloadClassifier = 0x0000400000000000,
        ExternalLanguage                     = 0x0000800000000000,
        ExternalStream                       = 0x0001000000000000,
        ExternalStreamingJob                 = 0x0002000000000000,
        ExternalModel                        = 0x0004000000000000,
        // If any of the above is changed, please change the "All" line as well.  The "All" value is a minimal mask.
        // For example, if the last value is 0x10000000, then the mask is 0x1fffffff.
        // If the last value is 0x20000000, then the mask is 0x3fffffff.
        // If the last value is 0x40000000, then the mask is 0x7fffffff.
        // If the last value is 0x80000000, then the mask is 0xffffffff.
        All                                  = 0x0007ffffffffffff // all above combined 
    }

    [Flags]
    public enum RoleTypes
    {
        Database = 1, //List database roles in which the connected login is a member
        Server = 2, //List server roles in which the connected login is a member
        All = 3 //List server and database roles in which the connected login is a member
    }

    public enum DependencyType
    {
        Children,
        Parents
    }

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum Month
    {
        January = 1,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }

    /// <summary>
    /// The possible values returned by SERVERPROPERTY('EngineEdition').
    /// See <see href="https://learn.microsoft.com/sql/t-sql/functions/serverproperty-transact-sql" /> for more details
    /// </summary>
    public enum Edition
    {
        /**
           Currently these values are duplicated in $\Sql\ssms\shared\ConnectionInfo\src\ConnectionEnums.cs. 
           Eventually we would like to remove the type defined here (see TFS#7818885) but until that happens
           make sure to update both enums with any changes
        **/
        Unknown = 0,
        /// <summary>
        /// Personal or Desktop Engine (Not available in SQL Server 2005 (9.x) and later versions.)
        /// </summary>
        PersonalOrDesktopEngine = 0x000001,

        /// <summary>
        /// Standard (For Standard, Web, and Business Intelligence.)
        /// </summary>
        Standard = 0x000002,

        /// <summary>
        /// Enterprise (For Evaluation, Developer, and Enterprise editions.)
        /// </summary>
        EnterpriseOrDeveloper = 0x000003,

        /// <summary>
        /// Express (For Express, Express with Tools, and Express with Advanced Services)
        /// </summary>
        Express = 0x000004,

        /// <summary>
        /// Azure SQL Database
        /// </summary>
        SqlDatabase = 0x000005,

        /// <summary>
        /// Azure Synapse dedicated SQL pool (formerly DataWarehouse)
        /// </summary>
        SqlDataWarehouse = 0x000006,
        /// <summary>
        /// Azure Synapse dedicated SQL pool (formerly DataWarehouse)
        /// </summary>
        AzureSynapseDedicatedSqlPool = 0x000006,
        /// <summary>
        /// Stretch Database
        /// </summary>
        SqlStretchDatabase = 0x000007,

        /// <summary>
        /// Azure SQL Managed Instance
        /// </summary>
        SqlManagedInstance = 0x000008,

        /// <summary>
        /// Azure SQL Edge (For all editions of Azure SQL Edge)
        /// </summary>
        SqlDatabaseEdge = 0x000009,

        /// <summary>
        /// Azure Arc Managed SQL Instance
        /// </summary>
        SqlAzureArcManagedInstance = 0x00000A,

        /// <summary>
        /// Azure Synapse serverless SQL pool (SQL OnDemand)
        /// </summary>
        SqlOnDemand = 0x00000B,
        /*
         * NOTE: If you're adding new value here,
         * please update as well ScriptDatabaseEngineEdition enum
         * in src\Microsoft\SqlServer\Management\SqlScriptPublish\SqlScriptOptions.cs
         */
    }

    /// <summary>
    /// the possible values of server status
    /// </summary>
    [Flags]
    public enum ServerStatus
    {
        /// <summary>
        /// Unknown state - value doesn't match any of the states below</summary>
        Unknown = 0,
        /// <summary>
        ///Referenced server is available for use (Online).</summary>
        Online = 0x000001,
        /// <summary>
        ///Server startup is underway.</summary>
        OnlinePending = 0x000003,
        /// <summary>
        ///Referenced server has been placed offline by a system or user action.</summary>
        Offline = 0x00010,
        /// <summary>
        ///Server is in being shuted down.</summary>
        OfflinePending = 0x000030
    }

    /// <summary>
    /// The start mode for a service
    /// </summary>
    public enum ServiceStartMode
    {
        Boot = 0,
        System,
        Auto,
        Manual,
        Disabled
    }

    /// <summary>
    /// The effective level for the FILESTREAM feature
    /// </summary>
    public enum FileStreamEffectiveLevel
    {
        /// Disabled
        Disabled = 0,
        /// T-SQL access only
        TSqlAccess,
        /// T-SQL access and local File system access
        TSqlLocalFileSystemAccess,
        /// T-SQL access and full File system access
        TSqlFullFileSystemAccess
    }

    /// <summary>
    /// InstanceState values
    /// </summary>
    public enum InstanceState
    {
        /// <summary>
        /// Unknown state - value doesn't match any of the states below</summary>
        Unknown = 0,
        /// <summary>
        ///Instance is online.</summary>
        Online = 0x000001,
        /// <summary>
        ///Instance is starting up.</summary>
        OnlinePending = 0x000003,
        /// <summary>
        ///Instance is offline.</summary>
        Offline = 0x000010,
        /// <summary>
        ///Shutdown pending.</summary>
        OfflinePending = 0x000030
    }

    /// <summary>
    /// Specifies the algorithm type used for backup encryption.
    /// </summary>
    public enum BackupEncryptionAlgorithm
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
    /// Specifies the encryptor type used to encrypt an encryption key.
    /// </summary>
    public enum BackupEncryptorType
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
    /// Types of temporal auto-generated columns
    /// </summary>
    public enum GeneratedAlwaysType
    {
        /// <summary>
        /// Column value is not auto-generated (non-temporal columns)
        /// </summary>
        None = 0,
        /// <summary>
        /// Column value is GENERATED ALWAYS AS ROW START
        /// </summary>
        AsRowStart = 1,
        /// <summary>
        /// Column value is GENERATED ALWAYS AS ROW END
        /// </summary>
        AsRowEnd = 2,
        /// <summary>
        /// Column value is GENERATED ALWAYS  AS TRANSACTION_ID START
        /// enum jumps to 7 instead of 3 as 
        /// </summary>
        AsTransactionIdStart = 7,
        /// <summary>
        /// Column value is GENERATED ALWAYS AS TRANSACTION_ID END
        /// </summary>
        AsTransactionIdEnd = 8,
        /// <summary>
        /// Column value is GENERATED ALWAYS AS SEQUENCE_NUMBER START
        /// </summary>
        AsSequenceNumberStart = 9,
        /// <summary>
        /// Column value is GENERATED ALWAYS AS SEQUENCE_NUMBER END
        /// </summary>
        AsSequenceNumberEnd = 10
    }

    /// <summary>
    /// Types of Temporal tables
    /// </summary>
    public enum TableTemporalType
    {
        /// <summary>
        /// System time period
        /// </summary>
        None = 0,
        /// <summary>
        /// History table of a system-versioned table
        /// </summary>
        HistoryTable = 1,
        /// <summary>
        /// System time table
        /// </summary>
        SystemVersioned = 2,
    }
}
