// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using ScriptingOptions = Microsoft.SqlServer.Management.Smo.ScriptingOptions;
namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// Defines settings that control the formatting of scripts and which types of objects are included
    /// </summary>
    public interface IScriptPublishOptions
    {
        bool ConvertUddtToBaseType { get; }
        bool DelimitStatements { get; }
        bool GenerateAnsiPadding { get; }
        bool IncludeCollation { get; }
        bool IncludeScriptingParametersHeader { get; }
        bool IncludeHeaders { get; }
        bool IncludeIdentity { get; }
        bool IncludeIfNotExists { get; }
        bool IncludeVarDecimal { get; }
        bool SchemaQualify { get; }
        bool SchemaQualifyForeignKeys { get; }
        bool ScriptBinding { get; }
        bool ScriptChangeTracking { get; }
        bool ScriptCheckConstraints { get; }
        bool ScriptDataCompressionOptions { get; }
        bool ScriptXmlCompressionOptions { get; }
        bool ScriptDefaults { get; }
        bool ScriptDependentObjects { get; }
        bool ScriptExtendedProperties { get; }
        bool ScriptFileGroups { get; }
        bool ScriptForeignKeys { get; }
        bool ScriptFullTextCatalogs { get; }
        bool ScriptFullTextIndexes { get; }
        bool ScriptIndexes { get; }
        bool ScriptPartitionSchemes { get; }
        bool ScriptPermissions { get; }
        bool ScriptOwner { get; }
        bool ScriptPrimaryKeys { get; }
        bool ScriptStatistics { get; }
        bool ScriptDriIncludeSystemNames { get; }
        bool ScriptTriggers { get; }
        bool ScriptUniqueKeys { get; }
        bool ScriptUseDatabase { get; }
        bool ScriptViewColumns { get; }
        string TargetVersion { get; }
        string BatchSeparator { get; }
        string TargetDatabaseEngineType { get; }
        string TargetDatabaseEngineEdition { get; }
        /// <summary>
        /// Returns the SMO ScriptingOptions represented by the current IScriptPublishOptions properties
        /// </summary>
        /// <returns></returns>
        ScriptingOptions GetSmoScriptingOptions();
        /// <summary>
        /// Returns a value indicating that script target settings should be based on the version of the source SQL Server
        /// </summary>
        bool TargetSourceServer { get; }
        /// <summary>
        /// Returns the SMO ScriptingOptions derived from combining the current IScriptPublishOptions properties with the version of SQL Server instance associated with the sourceObject
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <returns></returns>
        ScriptingOptions GetSmoScriptingOptions(object sourceObject);
    }
}
