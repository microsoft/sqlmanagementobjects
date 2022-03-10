// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    ///<summary>
    /// Network protocols
    /// </summary>
    public enum NetworkProtocol
    {
        /// <summary>
        /// TcpIp
        /// </summary>
        TcpIp,
        /// <summary>
        /// NamedPipes
        /// </summary>
        NamedPipes,
        /// <summary>
        /// Multiprotocol
        /// </summary>
        Multiprotocol,
        /// <summary>
        /// AppleTalk
        /// </summary>
        AppleTalk,
        /// <summary>
        /// BanyanVines
        /// </summary>
        BanyanVines,
        /// <summary>
        /// Via
        /// </summary>
        Via,
        /// <summary>
        /// SharedMemory
        /// </summary>
        SharedMemory,
        /// <summary>
        /// NWLinkIpxSpx
        /// </summary>
        NWLinkIpxSpx,
        /// <summary>
        /// NotSpecified
        /// </summary>
        NotSpecified
    }

    /// <summary>
    /// the possible values of server configuration
    /// </summary>
    [CommonLocalizedPropertyResources("Microsoft.SqlServer.Management.Common.StringConnectionInfo")]
    [TypeConverter(typeof(CommonLocalizableEnumConverter))]
    public enum DatabaseEngineType
    {
        Unknown = 0x000000,

        ///The server has a Standalone configuration
        [CommonDisplayNameKey("Standalone")]
        Standalone = 0x000001,

        ///The server has a Cloud configuration
        [CommonDisplayNameKey("SqlAzureDatabase")]
        SqlAzureDatabase = 0x000002
    }

    /// <summary>
    /// the possible values of server edition - match SERVERPROPERTY('EngineEdition')
    /// </summary>
    [CommonLocalizedPropertyResources("Microsoft.SqlServer.Management.Common.StringConnectionInfo")]
    [TypeConverter(typeof(CommonLocalizableEnumConverter))]
    public enum DatabaseEngineEdition
    {
        /**
           Currently these values are duplicated in src\Microsoft\SqlServer\Management\Smo\enumerations.cs
           Eventually we would like to remove the type defined there (see TFS#7818885) but until that happens
           make sure to update both enums with any changes
        **/
        Unknown = 0x000000,

        ///The server is Personal or standard
        [CommonDisplayNameKey("PersonalEdition")]
        Personal = 0x000001,

        ///The server is Standard
        [CommonDisplayNameKey("StandardEdition")]
        Standard = 0x000002,

        ///The server is Enterprise
        [CommonDisplayNameKey("EnterpriseEdition")]
        Enterprise = 0x000003,

        ///The server is Express
        [CommonDisplayNameKey("ExpressEdition")]
        Express = 0x000004,

        ///The server is SqlDatabase
        [CommonDisplayNameKey("SqlAzureDatabaseEdition")]
        SqlDatabase = 0x000005,

        ///The server is a DataWarehouse
        [CommonDisplayNameKey("SqlDataWarehouseEdition")]
        SqlDataWarehouse = 0x000006,

        ///The server is a StretchDatabase
        [CommonDisplayNameKey("StretchEdition")]
        SqlStretchDatabase = 0x000007,

        ///The server is a Sql Managed Instance
        [CommonDisplayNameKey("SqlManagedInstanceEdition")]
        SqlManagedInstance = 0x000008,

        ///The server is an Azure SQL Edge Edition
        [CommonDisplayNameKey("SqlDatabaseEdgeEdition")]
        SqlDatabaseEdge = 0x000009,

        ///The server is Sql SqlOnDemand
        [CommonDisplayNameKey("SqlOnDemandEdition")]
        SqlOnDemand = 0x00000B,

        /*
        * NOTE: If you're adding new value here,
        * please update as well ScriptDatabaseEngineEdition enum
        * in src\Microsoft\SqlServer\Management\SqlScriptPublish\SqlScriptOptions.cs
        * and src\Microsoft\SqlServer\Management\SqlScriptPublish\SqlScriptOptionsSR.strings
        */

        /* !!IMPORTANT!! When updating this enum make sure to update GetSupportedDatabaseEngineEditions below as well */
    }

    /// <remarks>
    /// Statement execution constants are used to direct the behavior of the ExecuteImmediate method, altering execution behavior or interpretation of the statement submitted for execution
    /// </remarks>
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ExecutionTypes
    {
        /// <summary>
        /// No statement execution options set
        /// </summary>
        Default = 0,
        /// <summary>
        /// Ignore the command terminator in the script. Execute as a single batch
        /// </summary>
        NoCommands = 1,
        /// <summary>
        /// Batch execution continues on any error that does not break the connection
        /// </summary>
        ContinueOnError = 2,
        /// <summary>
        /// Execute SET NOEXEC ON prior to batch execution. Execute SET NOEXEC OFF after batch execution.
        /// </summary>
        NoExec = 4,
        /// <summary>
        /// Execute SET PARSEONLY ON prior to batch execution. Execute SET PARSEONLY OFF after batch execution
        /// </summary>
        ParseOnly = 8,
        /// <summary>
        /// Execute SET QUOTED_IDENTIFIER ON prior to batch execution. Execute SET QUOTED_IDENTIFIER OFF after batch execution.
        /// </summary>
        QuotedIdentifierOn = 16
    }

    /// <summary>
    /// Determines if SQL statements are captured or sent to the server.
    /// </summary>
    [Flags]
    public enum SqlExecutionModes
    {
        /// <summary>
        /// execute sql
        /// </summary>
        ExecuteSql = 1,
        /// <summary>
        /// sql is captured
        /// </summary>
        CaptureSql = 2,
        /// <summary>
        /// sql is executed and captured
        /// </summary>
        ExecuteAndCaptureSql = ExecuteSql | CaptureSql
    }

    /// <summary>
    ///
    /// </summary>
    [Flags]
    public enum FixedServerRoles
    {
        /// <summary>
        /// none
        /// </summary>
        None = 0,
        /// <summary>
        /// System administrators
        /// </summary>
        SysAdmin = 1,
        /// <summary>
        /// Server administrators
        /// </summary>
        ServerAdmin = 2,
        /// <summary>
        /// Setup administrators
        /// </summary>
        SetupAdmin = 4,
        /// <summary>
        /// Security administrators
        /// </summary>
        SecurityAdmin = 8,
        /// <summary>
        /// Process administrators
        /// </summary>
        ProcessAdmin = 16,
        /// <summary>
        /// Database creators
        /// </summary>
        DBCreator = 32,
        /// <summary>
        /// Disk administrators
        /// </summary>
        DiskAdmin = 64,
        /// <summary>
        /// Bulk insert administrators
        /// </summary>
        BulkAdmin = 128
    }

    /* This feature will be removed in a future version of Microsoft SQL Server. [ SQL SERVER 2008, Books Online ] */
    /// <summary>
    ///
    /// </summary>
    [Flags]
    public enum ServerUserProfiles
    {
        /// <summary>
        ///
        /// </summary>
        None = 0,
        /// <summary>
        /// Login is a member of the sysadmin role.
        /// </summary>
        SALogin = 1,
        /// <summary>
        /// Login has CREATE DATABASE permission.
        /// </summary>
        CreateDatabase = 2,
        /// <summary>
        /// Login can execute sp_addextendedproc and sp_dropextendedproc (loading and unloading extended stored procedures).
        /// </summary>
        /* sp_addextendedproc and sp_dropextendedproc are deprecated procedures and are not used anymore */
        CreateXP = 4,
        /// <summary>
        /// Login has all specifiable SQL Server maintenance permissions.
        /// </summary>
        All = 7
    }

    /// <summary>
    /// regulates the disconnect policy
    /// </summary>
    public enum AutoDisconnectMode
    {
        /// <summary>
        /// after statement is executed if connection is pooled disconnect connection
        /// </summary>
        DisconnectIfPooled,
        /// <summary>
        /// don't disconnect connection
        /// </summary>
        NoAutoDisconnect
    }

    /// <summary>
    /// Valid connection types
    /// </summary>
    public enum ServerType
    {
        /// <summary>
        /// SQL Engine
        /// </summary>
        DatabaseEngine = 0,
        /// <summary>
        ///
        /// </summary>
        AnalysisServices = 1,
        /// <summary>
        ///
        /// </summary>
        ReportingServices = 2,
        /// <summary>
        ///
        /// </summary>
        IntegrationServices = 3,
        /// <summary>
        /// DO NOT USE - the name is SQL Server Compact Edition
        /// </summary>
        [Obsolete("use ServerType.SqlServerCompactEdition")]
        SqlServerEverywhere = 4,
        /// <summary>
        /// SQL CE
        /// </summary>
        SqlServerCompactEdition = 4,

    }

    public enum ServerCaseSensitivity
    {
        Unknown,
        CaseSensitive,
        CaseInsensitive,
    }

    public enum ConnectionType
    {
        Sql,
        Olap,
        SqlConnection,
        WmiManagementScope,
        SqlCE,
        ReportServer,
        IntegrationServer,
        AzureStorage,
        AzureAccount,
        SsisIr
    }

    /// <summary>
    /// Possible values for Server.HostPlatform property in SMO
    /// </summary>
    static public class HostPlatformNames
    {
        public const string Windows = "Windows";
        public const string Linux = "Linux";
    }

    /// <summary>
    /// Type converters to use to translate enum values to their string values and back
    /// </summary>
    public static class TypeConverters
    {
        public static readonly TypeConverter DatabaseEngineEditionTypeConverter = TypeDescriptor.GetConverter(typeof(DatabaseEngineEdition));
        public static readonly TypeConverter DatabaseEngineTypeTypeConverter = TypeDescriptor.GetConverter(typeof(DatabaseEngineType));
    }

    /// <summary>
    /// Set of helper methods for use with the enums defined here
    /// </summary>
    public static class ConnectionEnumsHelpers
    {
        /// <summary>
        /// Gets the set of supported <see cref="DatabaseEngineEdition"/> for a given <see cref="DatabaseEngineType"/>
        /// </summary>
        /// <param name="engineType"></param>
        /// <returns></returns>
        public static IEnumerable<DatabaseEngineEdition> GetSupportedDatabaseEngineEditions(this DatabaseEngineType engineType)
        {
            switch (engineType)
            {
                case DatabaseEngineType.SqlAzureDatabase:
                    yield return DatabaseEngineEdition.SqlDatabase;
                    yield return DatabaseEngineEdition.SqlDataWarehouse;
                    yield return DatabaseEngineEdition.SqlOnDemand;
                    break;
                case DatabaseEngineType.Standalone:
                    yield return DatabaseEngineEdition.Personal;
                    yield return DatabaseEngineEdition.Enterprise;
                    yield return DatabaseEngineEdition.Express;
                    yield return DatabaseEngineEdition.Standard;
                    yield return DatabaseEngineEdition.SqlStretchDatabase;
                    yield return DatabaseEngineEdition.SqlManagedInstance;
                    yield return DatabaseEngineEdition.SqlDatabaseEdge;
                    break;
                default:
                    break;
            }
        }
    }
}
