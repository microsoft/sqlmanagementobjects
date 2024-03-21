// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.SqlEnum;

namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Enum containing all possible DatabasePermissions
    /// use sys.fn_builtin_permissions to see the latest values on new versions of SQL
    ///
    /// IMPORTANT: When adding enums here, you must also update permissionOptions.cs with the getters and setters for the permission objects.
    /// The ContainsPermissionType methods in DatabasePrincipal.cs must also be updated when adding database and object permissions.
    /// The DatabasePermissionType enum used in those functions are derived from SqlParser.
    ///</summary>
    public enum DatabasePermissionSetValue
    {
        [PermissionType("AL")]
        [PermissionName("ALTER")]
        Alter = 0,
        [PermissionType("ALAK")]
        [PermissionName("ALTER ANY ASYMMETRIC KEY")]
        AlterAnyAsymmetricKey = 1,
        [PermissionType("ALAR")]
        [PermissionName("ALTER ANY APPLICATION ROLE")]
        AlterAnyApplicationRole = 2,
        [PermissionType("ALAS")]
        [PermissionName("ALTER ANY ASSEMBLY")]
        AlterAnyAssembly = 3,
        [PermissionType("ALCF")]
        [PermissionName("ALTER ANY CERTIFICATE")]
        AlterAnyCertificate = 4,
        [PermissionType("ALDS")]
        [PermissionName("ALTER ANY DATASPACE")]
        AlterAnyDataspace = 5,
        [PermissionType("ALED")]
        [PermissionName("ALTER ANY DATABASE EVENT NOTIFICATION")]
        AlterAnyDatabaseEventNotification = 6,
        [PermissionType("ALFT")]
        [PermissionName("ALTER ANY FULLTEXT CATALOG")]
        AlterAnyFulltextCatalog = 7,
        [PermissionType("ALMT")]
        [PermissionName("ALTER ANY MESSAGE TYPE")]
        AlterAnyMessageType = 8,
        [PermissionType("ALRL")]
        [PermissionName("ALTER ANY ROLE")]
        AlterAnyRole = 9,
        [PermissionType("ALRT")]
        [PermissionName("ALTER ANY ROUTE")]
        AlterAnyRoute = 10,
        [PermissionType("ALSB")]
        [PermissionName("ALTER ANY REMOTE SERVICE BINDING")]
        AlterAnyRemoteServiceBinding = 11,
        [PermissionType("ALSC")]
        [PermissionName("ALTER ANY CONTRACT")]
        AlterAnyContract = 12,
        [PermissionType("ALSK")]
        [PermissionName("ALTER ANY SYMMETRIC KEY")]
        AlterAnySymmetricKey = 13,
        [PermissionType("ALSM")]
        [PermissionName("ALTER ANY SCHEMA")]
        AlterAnySchema = 14,
        [PermissionType("ALSV")]
        [PermissionName("ALTER ANY SERVICE")]
        AlterAnyService = 15,
        [PermissionType("ALTG")]
        [PermissionName("ALTER ANY DATABASE DDL TRIGGER")]
        AlterAnyDatabaseDdlTrigger = 16,
        [PermissionType("ALUS")]
        [PermissionName("ALTER ANY USER")]
        AlterAnyUser = 17,
        [PermissionType("AUTH")]
        [PermissionName("AUTHENTICATE")]
        Authenticate = 18,
        [PermissionType("BADB")]
        [PermissionName("BACKUP DATABASE")]
        BackupDatabase = 19,
        [PermissionType("BALO")]
        [PermissionName("BACKUP LOG")]
        BackupLog = 20,
        [PermissionType("CL")]
        [PermissionName("CONTROL")]
        Control = 21,
        [PermissionType("CO")]
        [PermissionName("CONNECT")]
        Connect = 22,
        [PermissionType("CORP")]
        [PermissionName("CONNECT REPLICATION")]
        ConnectReplication = 23,
        [PermissionType("CP")]
        [PermissionName("CHECKPOINT")]
        Checkpoint = 24,
        [PermissionType("CRAG")]
        [PermissionName("CREATE AGGREGATE")]
        CreateAggregate = 25,
        [PermissionType("CRAK")]
        [PermissionName("CREATE ASYMMETRIC KEY")]
        CreateAsymmetricKey = 26,
        [PermissionType("CRAS")]
        [PermissionName("CREATE ASSEMBLY")]
        CreateAssembly = 27,
        [PermissionType("CRCF")]
        [PermissionName("CREATE CERTIFICATE")]
        CreateCertificate = 28,
        [PermissionType("CRDB")]
        [PermissionName("CREATE DATABASE")]
        CreateDatabase = 29,
        [PermissionType("CRDF")]
        [PermissionName("CREATE DEFAULT")]
        CreateDefault = 30,
        [PermissionType("CRED")]
        [PermissionName("CREATE DATABASE DDL EVENT NOTIFICATION")]
        CreateDatabaseDdlEventNotification = 31,
        [PermissionType("CRFN")]
        [PermissionName("CREATE FUNCTION")]
        CreateFunction = 32,
        [PermissionType("CRFT")]
        [PermissionName("CREATE FULLTEXT CATALOG")]
        CreateFulltextCatalog = 33,
        [PermissionType("CRMT")]
        [PermissionName("CREATE MESSAGE TYPE")]
        CreateMessageType = 34,
        [PermissionType("CRPR")]
        [PermissionName("CREATE PROCEDURE")]
        CreateProcedure = 35,
        [PermissionType("CRQU")]
        [PermissionName("CREATE QUEUE")]
        CreateQueue = 36,
        [PermissionType("CRRL")]
        [PermissionName("CREATE ROLE")]
        CreateRole = 37,
        [PermissionType("CRRT")]
        [PermissionName("CREATE ROUTE")]
        CreateRoute = 38,
        [PermissionType("CRRU")]
        [PermissionName("CREATE RULE")]
        CreateRule = 39,
        [PermissionType("CRSB")]
        [PermissionName("CREATE REMOTE SERVICE BINDING")]
        CreateRemoteServiceBinding = 40,
        [PermissionType("CRSC")]
        [PermissionName("CREATE CONTRACT")]
        CreateContract = 41,
        [PermissionType("CRSK")]
        [PermissionName("CREATE SYMMETRIC KEY")]
        CreateSymmetricKey = 42,
        [PermissionType("CRSM")]
        [PermissionName("CREATE SCHEMA")]
        CreateSchema = 43,
        [PermissionType("CRSN")]
        [PermissionName("CREATE SYNONYM")]
        CreateSynonym = 44,
        [PermissionType("CRSV")]
        [PermissionName("CREATE SERVICE")]
        CreateService = 45,
        [PermissionType("CRTB")]
        [PermissionName("CREATE TABLE")]
        CreateTable = 46,
        [PermissionType("CRTY")]
        [PermissionName("CREATE TYPE")]
        CreateType = 47,
        [PermissionType("CRVW")]
        [PermissionName("CREATE VIEW")]
        CreateView = 48,
        [PermissionType("CRXS")]
        [PermissionName("CREATE XML SCHEMA COLLECTION")]
        CreateXmlSchemaCollection = 49,
        [PermissionType("DL")]
        [PermissionName("DELETE")]
        Delete = 50,
        [PermissionType("EX")]
        [PermissionName("EXECUTE")]
        Execute = 51,
        [PermissionType("IN")]
        [PermissionName("INSERT")]
        Insert = 52,
        [PermissionType("RF")]
        [PermissionName("REFERENCES")]
        References = 53,
        [PermissionType("SL")]
        [PermissionName("SELECT")]
        Select = 54,
        [PermissionType("SPLN")]
        [PermissionName("SHOWPLAN")]
        Showplan = 55,
        [PermissionType("SUQN")]
        [PermissionName("SUBSCRIBE QUERY NOTIFICATIONS")]
        SubscribeQueryNotifications = 56,
        [PermissionType("TO")]
        [PermissionName("TAKE OWNERSHIP")]
        TakeOwnership = 57,
        [PermissionType("UP")]
        [PermissionName("UPDATE")]
        Update = 58,
        [PermissionType("VW")]
        [PermissionName("VIEW DEFINITION")]
        ViewDefinition = 59,
        [PermissionType("VWDS")]
        [PermissionName("VIEW DATABASE STATE")]
        ViewDatabaseState = 60,
        [PermissionType("ALDA")]
        [PermissionName("ALTER ANY DATABASE AUDIT")]
        AlterAnyDatabaseAudit = 61,
        [PermissionType("ALSP")]
        [PermissionName("ALTER ANY SECURITY POLICY")]
        AlterAnySecurityPolicy = 62,
        [PermissionType("AEDS")]
        [PermissionName("ALTER ANY EXTERNAL DATA SOURCE")]
        AlterAnyExternalDataSource = 63,
        [PermissionType("AEFF")]
        [PermissionName("ALTER ANY EXTERNAL FILE FORMAT")]
        AlterAnyExternalFileFormat = 64,
        [PermissionType("AAMK")]
        [PermissionName("ALTER ANY MASK")]
        AlterAnyMask = 65,
        [PermissionType("UMSK")]
        [PermissionName("UNMASK")]
        Unmask = 66,
        [PermissionType("VWCK")]
        [PermissionName("VIEW ANY COLUMN ENCRYPTION KEY DEFINITION")]
        ViewAnyColumnEncryptionKeyDefinition = 67,
        [PermissionType("VWCM")]
        [PermissionName("VIEW ANY COLUMN MASTER KEY DEFINITION")]
        ViewAnyColumnMasterKeyDefinition = 68,
        [PermissionType("AADS")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION")]
        AlterAnyDatabaseEventSession = 69,
        [PermissionType("ALCK")]
        [PermissionName("ALTER ANY COLUMN ENCRYPTION KEY")]
        AlterAnyColumnEncryptionKey = 70,
        [PermissionType("ALCM")]
        [PermissionName("ALTER ANY COLUMN MASTER KEY")]
        AlterAnyColumnMasterKey = 71,
        [PermissionType("ALDC")]
        [PermissionName("ALTER ANY DATABASE SCOPED CONFIGURATION")]
        AlterAnyDatabaseScopedConfiguration = 72,
        [PermissionType("ALEL")]
        [PermissionName("ALTER ANY EXTERNAL LIBRARY")]
        AlterAnyExternalLibrary = 73,
        [PermissionType("DABO")]
        [PermissionName("ADMINISTER DATABASE BULK OPERATIONS")]
        AdministerDatabaseBulkOperations = 74,
        [PermissionType("EAES")]
        [PermissionName("EXECUTE ANY EXTERNAL SCRIPT")]
        ExecuteAnyExternalScript = 75,
        [PermissionType("KIDC")]
        [PermissionName("KILL DATABASE CONNECTION")]
        KillDatabaseConnection = 76,
        [PermissionType("CREL")]
        [PermissionName("CREATE EXTERNAL LIBRARY")]
        CreateExternalLibrary = 77,
        [PermissionType("AASC")]
        [PermissionName("ALTER ANY SENSITIVITY CLASSIFICATION")]
        AlterAnySensitivityClassification = 78,
        [PermissionType("VASC")]
        [PermissionName("VIEW ANY SENSITIVITY CLASSIFICATION")]
        ViewAnySensitivityClassification = 79,
        [PermissionType("ALLA")]
        [PermissionName("ALTER ANY EXTERNAL LANGUAGE")]
        AlterAnyExternalLanguage = 80,
        [PermissionType("CRLA")]
        [PermissionName("CREATE EXTERNAL LANGUAGE")]
        CreateExternalLanguage = 81,
        [PermissionType("AEST")]
        [PermissionName("ALTER ANY EXTERNAL STREAM")]
        AlterAnyExternalStream = 82,
        [PermissionType("AESJ")]
        [PermissionName("ALTER ANY EXTERNAL JOB")]
        AlterAnyExternalJob = 83,
        [PermissionType("OC")]
        [PermissionName("OWNERSHIP CHAINING")]
        OwnershipChaining = 84,
        [PermissionType("CUSR")]
        [PermissionName("CREATE USER")]
        CreateUser = 85,
        [PermissionType("VDS")]
        [PermissionName("VIEW DATABASE SECURITY STATE")]
        ViewDatabaseSecurityState = 86,
        [PermissionType("VDP")]
        [PermissionName("VIEW DATABASE PERFORMANCE STATE")]
        ViewDatabasePerformanceState = 87,
        [PermissionType("VWS")]
        [PermissionName("VIEW SECURITY DEFINITION")]
        ViewSecurityDefinition = 88,
        [PermissionType("VCD")]
        [PermissionName("VIEW CRYPTOGRAPHICALLY SECURED DEFINITION")]
        ViewCryptographicallySecuredDefinition = 89,
        [PermissionType("EL")]
        [PermissionName("ENABLE LEDGER")]
        EnableLedger = 90,
        [PermissionType("ALR")]
        [PermissionName("ALTER LEDGER")]
        AlterLedger = 91,
        [PermissionType("VLC")]
        [PermissionName("VIEW LEDGER CONTENT")]
        ViewLedgerContent = 92,
        [PermissionType("EAEE")]
        [PermissionName("EXECUTE ANY EXTERNAL ENDPOINT")]
        ExecuteAnyExternalEndpoint = 93,
        [PermissionType("CRDS")]
        [PermissionName("CREATE ANY DATABASE EVENT SESSION")]
        CreateAnyDatabaseEventSession = 94,
        [PermissionType("DRDS")]
        [PermissionName("DROP ANY DATABASE EVENT SESSION")]
        DropAnyDatabaseEventSession = 95,
        [PermissionType("LDSO")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION OPTION")]
        AlterAnyDatabaseEventSessionOption = 96,
        [PermissionType("LDAE")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION ADD EVENT")]
        AlterAnyDatabaseEventSessionAddEvent = 97,
        [PermissionType("LDDE")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION DROP EVENT")]
        AlterAnyDatabaseEventSessionDropEvent = 98,

        [PermissionType("EDES")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION ENABLE")]
        AlterAnyDatabaseEventSessionEnable = 99,

        [PermissionType("DDES")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION DISABLE")]
        AlterAnyDatabaseEventSessionDisable = 100,

        [PermissionType("LDAT")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION ADD TARGET")]
        AlterAnyDatabaseEventSessionAddTarget = 101,

        [PermissionType("LDDT")]
        [PermissionName("ALTER ANY DATABASE EVENT SESSION DROP TARGET")]
        AlterAnyDatabaseEventSessionDropTarget = 102,

        [PermissionType("VWP")]
        [PermissionName("VIEW PERFORMANCE DEFINITION")]
        ViewPerformanceDefinition = 103,

        [PermissionType("VDSA")]
        [PermissionName("VIEW DATABASE SECURITY AUDIT")]
        ViewDatabaseSecurityAudit = 104,

        [PermissionType("ALC")]
        [PermissionName("ALTER LEDGER CONFIGURATION")]
        AlterLedgerConfiguration = 105,

        [PermissionType("ALEM")]
        [PermissionName("ALTER ANY EXTERNAL MIRROR")]
        AlterAnyExternalMirror = 106,

    }

    ///<summary>enum containing all possible ObjectPermissions</summary>
    // select distinct permission_name, type from sys.fn_builtin_permissions (DEFAULT) where class_desc <> 'SERVER' and class_desc <> 'DATABASE'
    public enum ObjectPermissionSetValue
    {
        [PermissionType("AL")]
        [PermissionName("ALTER")]
        Alter = 0,
        [PermissionType("CL")]
        [PermissionName("CONTROL")]
        Control = 1,
        [PermissionType("CO")]
        [PermissionName("CONNECT")]
        Connect = 2,
        [PermissionType("DL")]
        [PermissionName("DELETE")]
        Delete = 3,
        [PermissionType("EX")]
        [PermissionName("EXECUTE")]
        Execute = 4,
        [PermissionType("IM")]
        [PermissionName("IMPERSONATE")]
        Impersonate = 5,
        [PermissionType("IN")]
        [PermissionName("INSERT")]
        Insert = 6,
        [PermissionType("RC")]
        [PermissionName("RECEIVE")]
        Receive = 7,
        [PermissionType("RF")]
        [PermissionName("REFERENCES")]
        References = 8,
        [PermissionType("SL")]
        [PermissionName("SELECT")]
        Select = 9,
        [PermissionType("SN")]
        [PermissionName("SEND")]
        Send = 10,
        [PermissionType("TO")]
        [PermissionName("TAKE OWNERSHIP")]
        TakeOwnership = 11,
        [PermissionType("UP")]
        [PermissionName("UPDATE")]
        Update = 12,
        [PermissionType("VW")]
        [PermissionName("VIEW DEFINITION")]
        ViewDefinition = 13,
        [PermissionType("VWCT")]
        [PermissionName("VIEW CHANGE TRACKING")]
        ViewChangeTracking = 14,
        [PermissionType("CRSO")]
        [PermissionName("CREATE SEQUENCE")]
        CreateSequence = 15,
        [PermissionType("EXES")]
        [PermissionName("EXECUTE EXTERNAL SCRIPT")]
        ExecuteExternalScript = 16,
        [PermissionType("UMSK")]
        [PermissionName("UNMASK")]
        Unmask = 17
    }

    ///<summary>enum containing all possible ServerPermissions</summary>
    public enum ServerPermissionSetValue
    {
        [PermissionType("ADBO")]
        [PermissionName("ADMINISTER BULK OPERATIONS")]
        AdministerBulkOperations = 0,
        [PermissionType("ALCD")]
        [PermissionName("ALTER ANY CREDENTIAL")]
        AlterAnyCredential = 1,
        [PermissionType("ALCO")]
        [PermissionName("ALTER ANY CONNECTION")]
        AlterAnyConnection = 2,
        [PermissionType("ALDB")]
        [PermissionName("ALTER ANY DATABASE")]
        AlterAnyDatabase = 3,
        [PermissionType("ALES")]
        [PermissionName("ALTER ANY EVENT NOTIFICATION")]
        AlterAnyEventNotification = 4,
        [PermissionType("ALHE")]
        [PermissionName("ALTER ANY ENDPOINT")]
        AlterAnyEndpoint = 5,
        [PermissionType("ALLG")]
        [PermissionName("ALTER ANY LOGIN")]
        AlterAnyLogin = 6,
        [PermissionType("ALLS")]
        [PermissionName("ALTER ANY LINKED SERVER")]
        AlterAnyLinkedServer = 7,
        [PermissionType("ALRS")]
        [PermissionName("ALTER RESOURCES")]
        AlterResources = 8,
        [PermissionType("ALSS")]
        [PermissionName("ALTER SERVER STATE")]
        AlterServerState = 9,
        [PermissionType("ALST")]
        [PermissionName("ALTER SETTINGS")]
        AlterSettings = 10,
        [PermissionType("ALTR")]
        [PermissionName("ALTER TRACE")]
        AlterTrace = 11,
        [PermissionType("AUTH")]
        [PermissionName("AUTHENTICATE SERVER")]
        AuthenticateServer = 12,
        [PermissionType("CL")]
        [PermissionName("CONTROL SERVER")]
        ControlServer = 13,
        [PermissionType("COSQ")]
        [PermissionName("CONNECT SQL")]
        ConnectSql = 14,
        [PermissionType("CRDB")]
        [PermissionName("CREATE ANY DATABASE")]
        CreateAnyDatabase = 15,
        [PermissionType("CRDE")]
        [PermissionName("CREATE DDL EVENT NOTIFICATION")]
        CreateDdlEventNotification = 16,
        [PermissionType("CRHE")]
        [PermissionName("CREATE ENDPOINT")]
        CreateEndpoint = 17,
        [PermissionType("CRTE")]
        [PermissionName("CREATE TRACE EVENT NOTIFICATION")]
        CreateTraceEventNotification = 18,
        [PermissionType("SHDN")]
        [PermissionName("SHUTDOWN")]
        Shutdown = 19,
        [PermissionType("VWAD")]
        [PermissionName("VIEW ANY DEFINITION")]
        ViewAnyDefinition = 20,
        [PermissionType("VWDB")]
        [PermissionName("VIEW ANY DATABASE")]
        ViewAnyDatabase = 21,
        [PermissionType("VWSS")]
        [PermissionName("VIEW SERVER STATE")]
        ViewServerState = 22,
        [PermissionType("XA")]
        [PermissionName("EXTERNAL ACCESS ASSEMBLY")]
        ExternalAccessAssembly = 23,
        [PermissionType("XU")]
        [PermissionName("UNSAFE ASSEMBLY")]
        UnsafeAssembly = 24,
        [PermissionType("ALAA")]
        [PermissionName("ALTER ANY SERVER AUDIT")]
        AlterAnyServerAudit = 25,
        [PermissionType("ALSR")]
        [PermissionName("ALTER ANY SERVER ROLE")]
        AlterAnyServerRole = 26,
        [PermissionType("CRSR")]
        [PermissionName("CREATE SERVER ROLE")]
        CreateServerRole = 27,
        [PermissionType("ALAG")]
        [PermissionName("ALTER ANY AVAILABILITY GROUP")]
        AlterAnyAvailabilityGroup = 28,
        [PermissionType("CRAC")]
        [PermissionName("CREATE AVAILABILITY GROUP")]
        CreateAvailabilityGroup = 29,
        [PermissionType("AAES")]
        [PermissionName("ALTER ANY EVENT SESSION")]
        AlterAnyEventSession = 30,
        [PermissionType("SUS")]
        [PermissionName("SELECT ALL USER SECURABLES")]
        SelectAllUserSecurables = 31,
        [PermissionType("CADB")]
        [PermissionName("CONNECT ANY DATABASE")]
        ConnectAnyDatabase = 32,
        [PermissionType("IAL")]
        [PermissionName("IMPERSONATE ANY LOGIN")]
        ImpersonateAnyLogin = 33,
        // Sql2014 had a couple permissions at SERVER scope that got removed in later versions
        [PermissionType("AEDS")]
        [PermissionName("ALTER ANY EXTERNAL DATA SOURCE")]
        AlterAnyExternalDataSource = 34,
        [PermissionType("AEFF")]
        [PermissionName("ALTER ANY EXTERNAL FILE FORMAT")]
        AlterAnyExternalFileFormat = 35,
        [PermissionType("CRLG")]
        [PermissionName("CREATE LOGIN")]
        CreateLogin = 36,
        [PermissionType("VAS")]
        [PermissionName("VIEW ANY SECURITY DEFINITION")]
        ViewAnySecurityDefinition = 37,
        [PermissionType("VSS")]
        [PermissionName("VIEW SERVER SECURITY STATE")]
        ViewServerSecurityState = 38,
        [PermissionType("VSP")]
        [PermissionName("VIEW SERVER PERFORMANCE STATE")]
        ViewServerPerformanceState = 39,
        [PermissionType("VACD")]
        [PermissionName("VIEW ANY CRYPTOGRAPHICALLY SECURED DEFINITION")]
        ViewAnyCryptographicallySecuredDefinition = 40,

        [PermissionType("VAP")]
        [PermissionName("VIEW ANY PERFORMANCE DEFINITION")]
        ViewAnyPerformanceDefinition = 41,

        [PermissionType("CRES")]
        [PermissionName("CREATE ANY EVENT SESSION")]
        CreateAnyEventSession = 42,

        [PermissionType("DRES")]
        [PermissionName("DROP ANY EVENT SESSION")]
        DropAnyEventSession = 43,

        [PermissionType("LESO")]
        [PermissionName("ALTER ANY EVENT SESSION OPTION")]
        AlterAnyEventSessionOption = 44,

        [PermissionType("LSAE")]
        [PermissionName("ALTER ANY EVENT SESSION ADD EVENT")]
        AlterAnyEventSessionAddEvent = 45,

        [PermissionType("LSDE")]
        [PermissionName("ALTER ANY EVENT SESSION DROP EVENT")]
        AlterAnyEventSessionDropEvent = 46,

        [PermissionType("EES")]
        [PermissionName("ALTER ANY EVENT SESSION ENABLE")]
        AlterAnyEventSessionEnable = 47,

        [PermissionType("DES")]
        [PermissionName("ALTER ANY EVENT SESSION DISABLE")]
        AlterAnyEventSessionDisable = 48,

        [PermissionType("LSAT")]
        [PermissionName("ALTER ANY EVENT SESSION ADD TARGET")]
        AlterAnyEventSessionAddTarget = 49,

        [PermissionType("LSDT")]
        [PermissionName("ALTER ANY EVENT SESSION DROP TARGET")]
        AlterAnyEventSessionDropTarget = 50,

        [PermissionType("VEL")]
        [PermissionName("VIEW ANY ERROR LOG")]
        ViewAnyErrorLog = 51,

        [PermissionType("VSSA")]
        [PermissionName("VIEW SERVER SECURITY AUDIT")]
        ViewServerSecurityAudit = 52,

    }

    ///<summary>encapsulates functions that translate from sql codes into enum used to represent the permissions</summary>
    internal static class PermissionDecode
    {
        /// <summary>
        /// Used to cache the Permission Type -> enum values mappings. We're only caching this mapping because this one
        /// requires iterating through all of the values of the specified enum type and so can get expensive with all
        /// the reflection lookups. The other methods in this class work on a single instance of an enum value so the
        /// cost is much smaller and not worth the overhead of the caching.
        /// </summary>
        private static IDictionary<string, object> permissionTypeToEnumMapping = new Dictionary<string, object>();

        ///<summary>Translates a Permission Type name (from the type column in sys.database_permissions/sys.server_permissions)
        /// into the corresponding <see cref="DatabasePermissionSetValue"/></summary>
        internal static T ToPermissionSetValueEnum<T>(string permissionType) where T : struct, IConvertible
        {
            //We need a unique key (just the permission type name isn't unique) but Tuple isn't currently in the
            //framework version we're built against so just convert to string representation
            string key = permissionType + typeof (T);
            if (permissionTypeToEnumMapping.ContainsKey(key))
            {
                return (T)permissionTypeToEnumMapping[key];
            }

            foreach (T value in Enum.GetValues(typeof(T)))
            {
                //Check if the Permission Type marked on the enum matches the one we're looking for
                var attr = SqlEnumNetCoreExtension.GetCustomAttributes(typeof(T).GetMember(value.ToString())[0], typeof(PermissionTypeAttribute), false).FirstOrDefault() as PermissionTypeAttribute;
                if (attr != null && attr.Value.Equals(permissionType.TrimEnd(' ')))
                {
                    permissionTypeToEnumMapping[key] = value;
                    return value;
                }
            }
            //Couldn't find a value with the asked for type, means a new permission must not have been added
            throw new ArgumentException(StringSqlEnumerator.UnknownPermissionType(permissionType));
        }

        /// <summary>
        /// Converts a permission code (enum value) to the corresponding Permission Name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="permissionCode"></param>
        /// <returns>The Permission Name if the enum value exists and has the attribute defined, throws an exception if not</returns>
        internal static string PermissionCodeToPermissionName<T>(int permissionCode) where T : struct, IConvertible
        {
            if (Enum.IsDefined(typeof(T), permissionCode))
            {
                T obj = (T)Enum.ToObject(typeof (T), permissionCode);
                return obj.PermissionName();
            }
            TraceHelper.Trace("SqlEnum.PermissionDecode.PermissionCodeToPermissionName", SQLToolsCommonTraceLvl.Error, "Undefined permission code {0} - has it been added to {1}?", permissionCode, typeof(T));
            throw new InvalidOperationException(StringSqlEnumerator.UnknownPermissionCode(permissionCode));
        }

        /// <summary>
        /// Converts a permission code (enum value) to the corresponding Permission Type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="permissionCode"></param>
        /// <returns>The Permission Type if the enum value exists and has the attribute defined, String.Empty if not</returns>
        internal static string PermissionCodeToPermissionType<T>(int permissionCode) where T : struct, IConvertible
        {
            if (Enum.IsDefined(typeof (T), permissionCode))
            {
                T obj = (T) Enum.ToObject(typeof (T), permissionCode);
                return obj.PermissionType();
            }
            TraceHelper.Trace("SqlEnum.PermissionDecode.PermissionCodeToPermissionType", SQLToolsCommonTraceLvl.Error, "Undefined permission code {0} - has it been added to {1}?", permissionCode, typeof(T));
            throw new InvalidOperationException(StringSqlEnumerator.UnknownPermissionCode(permissionCode));
        }

        /// <summary>
        /// Gets the value of the PermissionName attribute for the specified enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <returns>The value of the PermissionName attribute, empty string if there is no such attribute</returns>
        internal static string PermissionName<T>(this T val) where T : struct, IConvertible
        {
            var attr = SqlEnumNetCoreExtension.GetCustomAttributes(typeof(T).GetMember(val.ToString())[0], typeof(PermissionNameAttribute), false).FirstOrDefault() as PermissionNameAttribute;
            TraceHelper.Trace("SqlEnum.PermissionDecode.PermissionName", SQLToolsCommonTraceLvl.Error, "{0} doesn't have a PermissionName attribute defined", val);
            return attr == null ? string.Empty : attr.Value;
        }

        /// <summary>
        /// Gets the value of the PermissionType attribute for the specified enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <returns>The value of the PermissionType attribute, empty string if there is no such attribute</returns>
        internal static string PermissionType<T>(this T val) where T : struct, IConvertible
        {
            var attr = SqlEnumNetCoreExtension.GetCustomAttributes(typeof(T).GetMember(val.ToString())[0], typeof(PermissionTypeAttribute), false).FirstOrDefault() as PermissionTypeAttribute;
            if (attr == null)
            {
                TraceHelper.Trace("SqlEnum.PermissionDecode.PermissionName", SQLToolsCommonTraceLvl.Error, $"{val} doesn't have a PermissionType attribute defined");
            }
            return attr == null ? string.Empty : attr.Value;
        }
    }

    /// <summary>
    /// Simple attribute class for storing String Values
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal abstract class StringValueAttribute : Attribute
    {
        private readonly string _value;

        /// <summary>
        /// Creates a new <see cref="StringValueAttribute"/> instance.
        /// </summary>
        /// <param name="value">Value.</param>
        protected StringValueAttribute(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value></value>
        public string Value
        {
            get { return _value; }
        }
    }

    /// <summary>
    /// Attribute for marking the Permission type of the marked permission (the short code
    /// name from the type column in sys.database_permissions/sys.server_permissions, e.g. CO)
    /// </summary>
    internal sealed class PermissionTypeAttribute : StringValueAttribute
    {
        public PermissionTypeAttribute(string permissionType) : base(permissionType) { }
    }

    /// <summary>
    /// Attribute for marking the Permission "name" of the marked permission (the permission_name
    /// column in sys.database_permissions/sys.server_permissions)
    /// </summary>
    internal sealed class PermissionNameAttribute : StringValueAttribute
    {
        public PermissionNameAttribute(string permissionName) : base(permissionName) { }
    }
}
