// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlScriptPublish;

namespace Microsoft.SqlServer.Test.SqlScriptPublishTests
{
    public class TestPublishingOptions :  IScriptPublishOptions
    {
        public TestPublishingOptions()
        {
            // match SSMS usersettings defaults
            BatchSeparator = "GO";
            DelimitStatements = true;
            IncludeHeaders = true;
            IncludeScriptingParametersHeader = false;
            IncludeVarDecimal = false;
            ScriptUseDatabase = true;
            SchemaQualify = true;
            ScriptExtendedProperties = true;
            IncludeIdentity = true;
            SchemaQualifyForeignKeys = true;
            ScriptCheckConstraints = true;
            ScriptDefaults = true;
            ScriptFileGroups = true;
            ScriptForeignKeys = true;
            ScriptPrimaryKeys = true;
            ScriptUniqueKeys = true;
            ScriptViewColumns = true;
        }

        public bool ConvertUddtToBaseType { get; set; }
        public bool DelimitStatements { get; set; }
        public bool GenerateAnsiPadding { get; set; }
        public bool IncludeCollation { get; set; }
        public bool IncludeScriptingParametersHeader { get; set; }
        public bool IncludeHeaders { get; set; }
        public bool IncludeIdentity { get; set; }
        public bool IncludeIfNotExists { get; set; }
        public bool IncludeVarDecimal { get; set; }
        public bool SchemaQualify { get; set; }
        public bool SchemaQualifyForeignKeys { get; set; }
        public bool ScriptBinding { get; set; }
        public bool ScriptChangeTracking { get; set; }
        public bool ScriptCheckConstraints { get; set; }
        public bool ScriptDataCompressionOptions { get; set; }
        public bool ScriptXmlCompressionOptions { get; set; }
        public bool ScriptDefaults { get; set; }
        public bool ScriptDependentObjects { get; set; }
        public bool ScriptExtendedProperties { get; set; }
        public bool ScriptFileGroups { get; set; }
        public bool ScriptForeignKeys { get; set; }
        public bool ScriptFullTextCatalogs { get; set; }
        public bool ScriptFullTextIndexes { get; set; }
        public bool ScriptIndexes { get; set; }
        public bool ScriptPartitionSchemes { get; set; }
        public bool ScriptPermissions { get; set; }
        public bool ScriptOwner { get; set; }
        public bool ScriptPrimaryKeys { get; set; }
        public bool ScriptStatistics { get; set; }
        public bool ScriptDriIncludeSystemNames { get; set; }
        public bool ScriptTriggers { get; set; }
        public bool ScriptUniqueKeys { get; set; }
        public bool ScriptUseDatabase { get; set; }
        public bool ScriptViewColumns { get; set; }
        public string TargetVersion { get; set; }
        public string BatchSeparator { get; set; }
        public string TargetDatabaseEngineType { get; set; }
        public string TargetDatabaseEngineEdition { get; set; }

        public SqlServerVersion TargetServerVersion { get; set; }

        private DatabaseEngineType ScriptDatabaseEngineType =>
            TargetDatabaseEngineType != null
                ? (DatabaseEngineType)Management.Common.TypeConverters.DatabaseEngineTypeTypeConverter
                    .ConvertFromString(TargetDatabaseEngineType)
                : DatabaseEngineType.Standalone;

        private DatabaseEngineEdition ScriptDatabaseEngineEdition =>
            TargetDatabaseEngineEdition != null
                ? (DatabaseEngineEdition)Management.Common.TypeConverters.DatabaseEngineEditionTypeConverter
                    .ConvertFromString(TargetDatabaseEngineEdition)
                : DatabaseEngineEdition.Standard;

        public ScriptingOptions GetSmoScriptingOptions()
        {
            return GetSmoScriptingOptions(ScriptDatabaseEngineEdition, ScriptDatabaseEngineType, TargetServerVersion);
        }

        public bool TargetSourceServer { get; set; }

        public ScriptingOptions GetSmoScriptingOptions(object sourceObject)
        {
            var options = !(sourceObject is SqlSmoObject smoObj)
                ? GetSmoScriptingOptions()
                : GetSmoScriptingOptions(
                    smoObj.DatabaseEngineEdition,
                    smoObj.DatabaseEngineType,
                    ScriptingOptions.ConvertToSqlServerVersion(smoObj.ServerVersion));
            return options;
        }

        private ScriptingOptions GetSmoScriptingOptions(DatabaseEngineEdition sourceEngineEdition,
            DatabaseEngineType sourceEngineType, SqlServerVersion sourceVersion)
        {

            var result = new Microsoft.SqlServer.Management.Smo.ScriptingOptions
            {
                NoCommandTerminator = !DelimitStatements,
                IncludeHeaders = IncludeHeaders,
                IncludeScriptingParametersHeader = IncludeScriptingParametersHeader,
                IncludeDatabaseContext = ScriptUseDatabase,
                WithDependencies = ScriptDependentObjects,
                IncludeIfNotExists = IncludeIfNotExists,
                NoVardecimal = !IncludeVarDecimal,
                SchemaQualify = SchemaQualify,
                ExtendedProperties = ScriptExtendedProperties,
                Permissions = ScriptPermissions,
                ScriptOwner = ScriptOwner,
                ConvertUserDefinedDataTypesToBaseType = ConvertUddtToBaseType,
                AnsiPadding = GenerateAnsiPadding,
                NoCollation = !IncludeCollation,
                NoIdentities = !IncludeIdentity,
                SchemaQualifyForeignKeysReferences = SchemaQualifyForeignKeys,
                Bindings = ScriptBinding,
                DriChecks = ScriptCheckConstraints,
                DriDefaults = ScriptDefaults,
                NoFileGroup = !ScriptFileGroups,
                NoFileStream = !ScriptFileGroups,
                DriForeignKeys = ScriptForeignKeys,
                FullTextCatalogs = ScriptFullTextCatalogs,
                FullTextIndexes = ScriptFullTextIndexes,
                ClusteredIndexes = ScriptIndexes,
                NonClusteredIndexes = ScriptIndexes,
                XmlIndexes = ScriptIndexes,
                SpatialIndexes = ScriptIndexes,
                ColumnStoreIndexes = ScriptIndexes,
                NoTablePartitioningSchemes = !ScriptPartitionSchemes,
                NoIndexPartitioningSchemes = !ScriptPartitionSchemes,
                DriPrimaryKey = ScriptPrimaryKeys,
                Statistics = ScriptStatistics,
                Triggers = ScriptTriggers,
                DriUniqueKeys = ScriptUniqueKeys,
                NoViewColumns = !ScriptViewColumns,
                DriIncludeSystemNames = ScriptDriIncludeSystemNames,
                EnforceScriptingOptions = true,
                ChangeTracking = ScriptChangeTracking,
                ScriptDataCompression = ScriptDataCompressionOptions,
                ScriptXmlCompression = ScriptXmlCompressionOptions
            };

            result.SetTargetDatabaseEngineType(ScriptDatabaseEngineType);

            if (TargetSourceServer)
            {
                result.TargetDatabaseEngineEdition = sourceEngineEdition;
                result.TargetDatabaseEngineType = sourceEngineType;
                result.TargetServerVersion = sourceVersion;
            }
            else
            {

                result.TargetServerVersion = TargetServerVersion;
                result.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition;
                result.TargetDatabaseEngineType = ScriptDatabaseEngineType;
            }
            return result;
        }
    }
}
