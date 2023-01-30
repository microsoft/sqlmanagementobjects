// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Utility and helper methods for SMO
    /// </summary>
    public static class SmoUtility
    {
        /// <summary>
        /// Whether the specified type is supported by the specified server Version, Engine Type and Engine Edition.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serverVersion"></param>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns></returns>
        public static bool IsSupportedObject(Type type, ServerVersion serverVersion, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            if (databaseEngineEdition == DatabaseEngineEdition.SqlOnDemand)
            {
                if (databaseEngineType == DatabaseEngineType.SqlAzureDatabase && serverVersion.Major >= 12)
                {
                    switch (type.Name)
                    {
                        case nameof(FileGroup):
                        case nameof(Database.ServiceBroker):
                        case nameof(DatabaseScopedConfiguration):
                        case nameof(QueryStoreOptions):
                            return false;
                        case nameof(ExternalFileFormat):
                        case nameof(DatabaseScopedCredential):
                            return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            if (databaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
            {
                switch (type.Name)
                {
                    // Objects disabled on edge
                    //
                    case nameof(UserDefinedAggregate):
                    case nameof(UserDefinedAggregateParameter):
                    case nameof(SqlAssembly):
                    case nameof(SearchProperty):
                    case nameof(SearchPropertyList):
                    case nameof(ResourceGovernor):
                    case nameof(AvailabilityGroup):
                    case nameof(FullTextCatalog):
                    case nameof(FullTextIndex):
                    case nameof(FullTextIndexColumn):
                    case nameof(FullTextService):
                    case nameof(FullTextStopList):
                    case nameof(LinkedServer):
                    case nameof(LinkedServerLogin):
                        return false;
                }
            }
            if (databaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {
                switch (type.Name)
                {
                    case nameof(ExternalLanguage):
                        return false;
                }
            }
            if (databaseEngineType == DatabaseEngineType.Standalone)
            {
                switch (type.Name)
                {
                    case nameof(WorkloadManagementWorkloadClassifier):
                    case nameof(WorkloadManagementWorkloadGroup):
                        return false;
                }

                if (databaseEngineEdition != DatabaseEngineEdition.SqlDatabaseEdge)
                {
                    switch (type.Name)
                    {
                        // This type is currently only supported on edge and
                        // not on Sql version 15. However this may change soon
                        // and this will need to be removed.
                        //
                        case nameof(ExternalStream):
                        case nameof(ExternalStreamingJob):
                            return false;
                    }
                }

                if (serverVersion.Major < 15)
                {
                    switch (type.Name)
                    {
                        case nameof(ExternalLanguage):
                        case nameof(ExternalLanguageFile):
                        case nameof(ExternalStream):
                        case nameof(ExternalStreamingJob):
                        case "EdgeConstraint":
                        case "EdgeConstraintClause":
                            return false;
                    }
                }
                if (serverVersion.Major < 14)
                {
                    switch (type.Name)
                    {
                        case "ExternalLibrary":
                        case "ExternalLibraryFile":
                        case "ResumableIndex":
                            return false;
                    }
                }
                if (serverVersion.Major < 13)
                {
                    switch (type.Name)
                    {
                        case "ColumnEncryptionKey":
                        case "ColumnEncryptionKeyValue":
                        case "ColumnMasterKey":
                        case "ExternalDataSource":
                        case "ExternalFileFormat":
                        case "ExternalResourcePool":
                        case "ExternalResourcePoolAffinityInfo":
                        case "SecurityPolicy":
                        case "SecurityPredicate":
                        case "QueryStoreOptions":
                        case "DatabaseScopedCredential":
                        case "DatabaseScopedConfiguration":
                            return false;
                    }
                }
                if (serverVersion.Major == 11 && serverVersion.Minor == 0  && serverVersion.BuildNumber < 2813)
                {
                    switch (type.Name)
                    {
                        case "IndexedXmlPath":
                        case "IndexedXmlPathNamespace":
                            return false;
                    }
                }
                if (serverVersion.Major < 11)
                {
                    switch (type.Name)
                    {
                        case "AffinityInfo":
                            if ((serverVersion.Major >= 10 && serverVersion.Minor < 50) || (serverVersion.Major < 10))
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        case "AvailabilityDatabase":
                        case "AvailabilityGroup":
                        case "AvailabilityGroupListener":
                        case "AvailabilityGroupListenerIPAddress":
                        case "IndexedXmlPath":
                        case "IndexedXmlPathNamespace":
                        case "SearchProperty":
                        case "SearchPropertyList":
                        case "Sequence":

                        // $ISSUE - SQL 14 feature - revisit after version bump
                        // Smart Admin is available in SQL 14 onwards, If SMO object model is used to connect to SQL 11 and servers below,
                        // we need to return back IsSupportedObject() as false
                        // Right now, in DS_Main SQL version is 11.0;
                        // After version bump changes are in DS_Main, we need to check this condition
                        //         only on server versions less than 12.0
                        case "SmartAdmin":
                            return false;
                    }
                }
                if (serverVersion.Major < 10)
                {
                    switch (type.Name)
                    {
                        case "Audit" :
                        case "AuditSpecification" :
                        case "CryptographicProvider":
                        case "DatabaseAuditSpecification" :
                        case "DatabaseEncryptionKey":
                        case "FullTextStopList":
                        case "OrderColumn":
                        case "ResourceGovernor":
                        case "ResourcePool":
                        case "ServerAuditSpecification":
                        case "UserDefinedTableType" :
                        case "WorkLoadGroup":
                            return false;
                    }
                }
                if (serverVersion.Major < 9)
                {
                    switch (type.Name)
                    {
                        case "BrokerPriority" :
                        case "BrokerService" :
                        case "Certificate" :
                        case "Credential" :
                        case "DatabaseDdlTrigger" :
                        case "DatabaseMirroringPayload":
                        case "Endpoint" :
                        case "EndpointPayload" :
                        case "EndpointProtocol" :
                        case "FullTextCatalog":
                        case "MailAccount" :
                        case "MailProfile":
                        case "MailServer" :
                        case "MessageType" :
                        case "MessageTypeMapping" :
                        case "PartitionFunction" :
                        case "PartitionFunctionParameter" :
                        case "PartitionScheme" :
                        case "PartitionSchemeParameter" :
                        case "PhysicalPartition" :
                        case "PlanGuide" :
                        case "RemoteServiceBinding":
                        case "Schema" :
                        case "ServerDdlTrigger" :
                        case "ServiceBroker":
                        case "ServiceContract":
                        case "ServiceMasterKey" :
                        case "ServiceContractMapping":
                        case "ServiceQueue":
                        case "ServiceRoute":
                        case "SqlAssembly" :
                        case "Synonym" :
                        case "SymmetricKey" :
                        case "UserDefinedAggregate":
                        case "UserDefinedAggregateParameter":
                        case "UserDefinedType" :
                        case "XmlSchemaCollection" :
                            return false;
                    }
                }
            }
            else if (databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                if (databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
                {
                    return isObjectSupportedBySqlDw(type.Name);
                }

                switch (type.Name)
                {
                    case nameof(Check):
                    case nameof(Column):
                    case nameof(Database):
                    case nameof(DatabaseDdlTrigger):
                    case nameof(DatabaseRole):
                    case nameof(DefaultConstraint):
                    case nameof(EdgeConstraint):
                    case nameof(ForeignKey):
                    case nameof(ForeignKeyColumn):
                    case nameof(Index):
                    case nameof(IndexedColumn):
                    case nameof(Login):
                    case nameof(Parameter):
                    case nameof(Schema):
                    case nameof(Server):
                    case nameof(Statistic):
                    case nameof(StatisticColumn):
                    case nameof(StoredProcedure):
                    case nameof(Synonym):
                    case nameof(Table):
                    case nameof(Trigger):
                    case nameof(User):
                    case nameof(UserDefinedDataType):
                    case nameof(UserDefinedFunction):
                    case nameof(UserDefinedTableType):
                    case nameof(View):
                        if (databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
                        {
                            return isObjectSupportedBySqlDw(type.Name);
                        }
                        else
                        {
                            return true;
                        }

                    // Additional SQL Azure v12 (Sterling) features
                    case nameof(ApplicationRole) :
                    case nameof(AsymmetricKey) :
                    case nameof(Certificate) :
                    case nameof(ColumnEncryptionKey) :
                    case nameof(ColumnEncryptionKeyValue) :
                    case nameof(ColumnMasterKey) :
                    case nameof(DatabaseScopedCredential) :
                    case nameof(Default) :
                    case nameof(ExtendedProperty):
                    case nameof(ExternalDataSource):
                    case nameof(FullTextCatalog) :
                    case nameof(FullTextIndex) :
                    case nameof(FullTextIndexColumn) :
                    case nameof(FullTextService) :
                    case nameof(FullTextStopList) :
                    case nameof(MasterKey) :
                    case nameof(NumberedStoredProcedure) :
                    case nameof(PartitionFunction) :
                    case nameof(PartitionFunctionParameter) :
                    case nameof(PartitionScheme) :
                    case nameof(PartitionSchemeParameter) :
                    case nameof(PhysicalPartition) :
                    case nameof(PlanGuide) :
                    case nameof(QueryStoreOptions) :
                    case nameof(Rule) :
                    case nameof(SecurityPolicy) :
                    case nameof(SecurityPredicate) :
                    case nameof(DatabaseScopedConfiguration):
                    case nameof(Sequence) :
                    case nameof(SqlAssembly) :
                    case nameof(SymmetricKey) :
                    case nameof(UserDefinedAggregate) :
                    case nameof(UserDefinedAggregateParameter) :
                    case nameof(UserDefinedType) :
                    case nameof(UserOptions) :
                    case nameof(XmlSchemaCollection) :
                         return serverVersion.Major >= 12;
                    case nameof(ExternalFileFormat):
                    case nameof(WorkloadManagementWorkloadGroup):
                         return false;
                    default:
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if object is supported by Sql Dw
        /// </summary>
        private static bool isObjectSupportedBySqlDw(string name)
        {
            switch (name)
            {
                case nameof(AsymmetricKey):
                case nameof(Certificate):
                case nameof(Column):
                case nameof(Database):
                case nameof(DatabaseRole):
                case nameof(DefaultConstraint):
                case nameof(DatabaseScopedCredential):
                case nameof(ExternalDataSource):
                case nameof(ExternalFileFormat):
                case nameof(Index):
                case nameof(IndexedColumn):
                case nameof(Login):
                case nameof(MasterKey):
                case nameof(Parameter):
                case nameof(PartitionFunctionParameter):
                case nameof(PartitionSchemeParameter):
                case nameof(PhysicalPartition):
                case nameof(Schema):
                case nameof(Server):
                case nameof(SecurityPolicy):
                case nameof(SecurityPredicate):
                case nameof(Statistic):
                case nameof(StatisticColumn):
                case nameof(StoredProcedure):
                case nameof(SymmetricKey):
                case nameof(Table):
                case nameof(User):
                case nameof(UserDefinedFunction):
                case nameof(UserOptions):
                case nameof(View):
                case nameof(WorkloadManagementWorkloadGroup):
                case nameof(WorkloadManagementWorkloadClassifier):
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the specified type is supported by the <see cref="ServerVersion"/> and <see cref="DatabaseEngineType"/>
        /// of the root server for this object. If ScriptingPreferences are non-null will also check if specified type
        /// is supported by the <see cref="ServerVersion"/> and <see cref="DatabaseEngineType"/> of the target server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="smoObject"></param>
        /// <param name="sp">Optional - If provided will also check if target server supports specified type</param>
        /// <returns>TRUE if the specified type is supported by the current connection and/or ScriptingPreferences</returns>
        public static bool IsSupportedObject<T>(this SqlSmoObject smoObject, ScriptingPreferences sp = null)
    where T : SqlSmoObject
        {
            return SmoUtility.IsSupportedObject(typeof(T), smoObject.ServerVersion, smoObject.DatabaseEngineType, smoObject.DatabaseEngineEdition)
                && (sp == null || SmoUtility.IsSupportedObject(typeof(T), ScriptingOptions.ConvertToServerVersion(sp.TargetServerVersion), sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition));
        }

        /// <summary>
        /// Checks if the specified type is supported by the <see cref="ServerVersion"/>, <see cref="DatabaseEngineType"/>
        /// and <see cref="DatabaseEngineEdition"/> of the root server for this object. If ScriptingPreferences are non-null
        /// will also check if specified type is supported by the <see cref="ServerVersion"/>, <see cref="DatabaseEngineType"/>
        /// and <see cref="DatabaseEngineEdition"/> of the target server.
        /// </summary>
        /// <param name="smoObject"></param>
        /// <param name="type"></param>
        /// <param name="sp">Optional - If provided will also check if target server supports specified type</param>
        /// <returns>TRUE if the specified type is supported by the current connection and/or ScriptingPreferences</returns>
        internal static bool IsSupportedObject(this SqlSmoObject smoObject, Type type, ScriptingPreferences sp = null)
        {
            return SmoUtility.IsSupportedObject(type, smoObject.ServerVersion, smoObject.DatabaseEngineType, smoObject.DatabaseEngineEdition)
                && (sp == null || SmoUtility.IsSupportedObject(type, ScriptingOptions.ConvertToServerVersion(sp.TargetServerVersion), sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition));
        }

        /// <summary>
        /// Checks if the specified type is supported by the <see cref="ServerVersion"/>, <see cref="DatabaseEngineType"/>
        /// and <see cref="DatabaseEngineEdition"/> of the root server for this object. If ScriptingPreferences are non-null
        /// will also check if specified type is supported by the <see cref="ServerVersion"/>, <see cref="DatabaseEngineType"/>
        /// and <see cref="DatabaseEngineEdition"/> of the target server.
        ///
        /// Throws an exception if either of these checks fail.
        ///
        /// </summary>
        /// <param name="smoObject"></param>
        /// <param name="type"></param>
        /// <param name="sp">Optional - If provided will also check if target server supports specified type</param>
        internal static void ThrowIfNotSupported(this SqlSmoObject smoObject, Type type, ScriptingPreferences sp = null)
        {
            smoObject.ThrowIfNotSupported(type, message: null, sp: sp);
        }

        /// <summary>
        /// Checks if the specified type is supported by the <see cref="ServerVersion"/>, <see cref="DatabaseEngineType"/>
        /// and <see cref="DatabaseEngineEdition"/> of the root server for this object. If ScriptingPreferences are non-null
        /// will also check if specified type is supported by the <see cref="ServerVersion"/>, <see cref="DatabaseEngineType"/>
        /// and <see cref="DatabaseEngineEdition"/> of the target server.
        ///
        /// Throws an exception with the specified message if either of these checks fail.
        ///
        /// </summary>
        /// <param name="smoObject"></param>
        /// <param name="type"></param>
        /// <param name="message">The exception message to display</param>
        /// <param name="sp"></param>
        internal static void ThrowIfNotSupported(this SqlSmoObject smoObject, Type type, string message, ScriptingPreferences sp = null)
        {
            bool supportedOnSource = SmoUtility.IsSupportedObject(type, smoObject.ServerVersion, smoObject.DatabaseEngineType, smoObject.DatabaseEngineEdition);
            bool supportedOnTarget = (sp != null &&
                 SmoUtility.IsSupportedObject(type, ScriptingOptions.ConvertToServerVersion(sp.TargetServerVersion), sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition));

            if (!supportedOnSource || (sp != null && !supportedOnTarget))
            {
                // If the object is supported on the source but not on the target then throw the error for the target
                // If the object is not supported on the source but it is supported on the target then throw the error for the source
                // If the object is neither supported on the source or on the taget throw the error for the source

                DatabaseEngineEdition DbEngineEditionToUse = smoObject.DatabaseEngineEdition;
                DatabaseEngineType DbEngineTypeToUse = smoObject.DatabaseEngineType;

                if (supportedOnSource && !supportedOnTarget)
                {
                    if (sp != null)
                    {
                        DbEngineEditionToUse = sp.TargetDatabaseEngineEdition;
                        DbEngineTypeToUse = sp.TargetDatabaseEngineType;
                    }
                }

                if (DbEngineTypeToUse  == DatabaseEngineType.Standalone)
                {
                    ServerVersion minSupportedVersion = SmoUtility.GetMinimumSupportedVersion(type, DbEngineTypeToUse , DbEngineEditionToUse );
                    if (DbEngineEditionToUse == DatabaseEngineEdition.SqlDatabaseEdge)
                    {
                        message = string.IsNullOrEmpty(message) ? ExceptionTemplates.NotSupportedOnSqlEdge(type.Name) : message;
                        throw new UnsupportedVersionException(message).SetHelpContext("NotSupportedOnSqlEdge");
                    }
                    if (minSupportedVersion.Major == 15)
                    {
                        message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn150 : message;
                        throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn150");
                    }
                    else if (minSupportedVersion.Major == 14)
                    {
                        message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn140 : message;
                        throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn140");
                    }
                    else if (minSupportedVersion.Major == 13)
                    {
                        message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn130 : message;
                        throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn130");
                    }
                    else if (minSupportedVersion.Major == 12)
                    {
                        message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn120 : message;
                        throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn120");
                    }
                    else if (minSupportedVersion.Major == 11)
                    {
                        message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn110 : message;
                        throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn110");
                    }
                    else if (minSupportedVersion.Major == 10)
                    {
                        if (minSupportedVersion.Minor == 5)
                        {
                            message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn105 : message;
                            throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn105");
                        }
                        else
                        {
                            message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn100 : message;
                            throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn100");
                        }
                    }
                    else if (minSupportedVersion.Major == 9)
                    {
                        message = string.IsNullOrEmpty(message) ? ExceptionTemplates.SupportedOnlyOn90 : message;
                        throw new UnsupportedVersionException(message).SetHelpContext("SupportedOnlyOn90");
                    }
                    else
                    {
                        throw new UnsupportedFeatureException(ExceptionTemplates.NotSupportedOnStandaloneWithDetails(type.Name))
                            .SetHelpContext("NotSupportedOnStandaloneWithDetails");
                    }
                }
                else if (DbEngineEditionToUse  == DatabaseEngineEdition.SqlOnDemand)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.NotSupportedForSqlOd(type.Name))
                        .SetHelpContext("NotSupportedForSqlOd");
                }
                else if (DbEngineTypeToUse  == DatabaseEngineType.SqlAzureDatabase)
                {
                    if (DbEngineEditionToUse  == DatabaseEngineEdition.SqlDataWarehouse &&
                        ((SmoUtility.IsSupportedObject(type, smoObject.ServerVersion, DbEngineTypeToUse ,
                            DbEngineEditionToUse ) == false)))
                    {
                        throw new UnsupportedVersionException(ExceptionTemplates.NotSupportedForSqlDw(type.Name))
                            .SetHelpContext("NotSupportedForSqlDw");
                    }
                    else
                    {
                        throw new UnsupportedVersionException(ExceptionTemplates.NotSupportedOnCloudWithDetails(type.Name))
                            .SetHelpContext("NotSupportedOnCloudWithDetails");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the minimum supported version of the specified type for the given engine type/engine edition.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns></returns>
        internal static ServerVersion GetMinimumSupportedVersion(Type type, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            foreach (ServerVersion version in GetSupportedVersions(databaseEngineType, databaseEngineEdition))
            {
                if (IsSupportedObject(type, version, databaseEngineType, databaseEngineEdition))
                {
                    return version;
                }
            }

            return new ServerVersion(99,99);
        }

        /// <summary>
        /// The server versions we support for on-prem (box) databases
        /// </summary>
        private static readonly ServerVersion[] supportedOnPremVersions =
        {
            //IMPORTANT!! - always keep this list in increasing order of version number!
            (new ServerVersion(9, 0)),   //2005
            (new ServerVersion(10, 0)),  //2008
            (new ServerVersion(10, 50)), //2008 R2
            (new ServerVersion(11, 0)),  //2012
            (new ServerVersion(12, 0)),  //2014
            (new ServerVersion(13, 0)),  //2016
            (new ServerVersion(14, 0)),  //2017
            (new ServerVersion(15, 0))   //2019
        };

        /// <summary>
        /// The server versions we support for cloud databases
        /// </summary>
        private static readonly ServerVersion[] supportedCloudVersions =
        {
            //IMPORTANT!! - always keep this list in increasing order of version number!
            new ServerVersion(12, 0),
        };

        /// <summary>
        /// Gets the list of supported server versions for the specified engine type and engine edition.
        /// </summary>
        /// <param name="dbEngineType"></param>
        /// <param name="dbEngineEdition"></param>
        /// <returns></returns>
        internal static IEnumerable<ServerVersion> GetSupportedVersions(DatabaseEngineType dbEngineType, DatabaseEngineEdition dbEngineEdition)
        {
            //Note - currently the edition doesn't change the supported versions we essentially ignore it. It's being added though since it's very
            //likely at some point (especially with cloud editions) that this will no longer be true.
            switch (dbEngineType)
            {
                case DatabaseEngineType.Standalone:
                    return supportedOnPremVersions;
                case DatabaseEngineType.SqlAzureDatabase:
                    return supportedCloudVersions;
                default:
                    //Default return empty array for unknown engine type
                    return new ServerVersion[0];
            }
        }

        /// <summary>
        /// Encodes a string collection as a comment block.
        /// </summary>
        /// <param name="stringCollection">String collection object</param>
        /// <param name="headComment">Extra comment in the head of comment block</param>
        internal static void EncodeStringCollectionAsComment(StringCollection stringCollection, string headComment = "")
        {
            if (!String.IsNullOrEmpty(headComment))
            {
                stringCollection.Insert(0, "/*** " + headComment + " ***/");
            }

            for (int id = 1; id < stringCollection.Count; id++)
            {
                stringCollection[id] = "-- " + stringCollection[id];
            }
        }
    }
}
