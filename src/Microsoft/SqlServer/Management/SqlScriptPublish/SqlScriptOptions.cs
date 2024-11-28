// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// Designer-friendly class that defines the options that can be set for publishing SQL scripts using SqlScriptPublishModel
    /// </summary>
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.SqlScriptPublish.SqlScriptOptionsSR")]
    public class SqlScriptOptions : IDynamicVisible, IDynamicReadOnly
    {
        #region Enums
        /// <summary>
        /// Defines the compatibility level of the script feature set
        /// </summary>
        public enum ScriptCompatibilityOptions
        {
            [DisplayNameKey("OnlyScript90CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(9)]
            Script90Compat,
            [DisplayNameKey("OnlyScript100CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(10)]
            Script100Compat,
            [DisplayNameKey("OnlyScript105CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(10, 50)]
            Script105Compat,
            [DisplayNameKey("OnlyScript110CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(11)]
            Script110Compat,
            [DisplayNameKey("OnlyScript120CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(12)]
            Script120Compat,
            [DisplayNameKey("OnlyScript130CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(13)]
            Script130Compat,
            [DisplayNameKey("OnlyScript140CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(14)]
            Script140Compat,
            [DisplayNameKey("OnlyScript150CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(15)]
            Script150Compat,
            [DisplayNameKey("OnlyScript160CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(16)]
            Script160Compat,
            // VBUMP
            [DisplayNameKey("OnlyScript170CompatibleFeatures")]
            [CompatibilityLevelSupportedVersion(17)]
            Script170Compat,
        }

        /// <summary>
        /// Defines whether the script will be for on-premises SQL Server or for Azure SQL Database
        /// </summary>
        public enum ScriptDatabaseEngineType
        {
            [DisplayNameKey("SingleInstanceEngineType")]
            SingleInstance,
            [DisplayNameKey("SqlAzureEngineType")]
            SqlAzure
        }

        /// <summary>
        /// Defines the edition of SQL Server for the script
        /// </summary>
        public enum ScriptDatabaseEngineEdition
        {
            [DisplayNameKey("PersonalEdition")]
            SqlServerPersonalEdition,
            [DisplayNameKey("StandardEdition")]
            SqlServerStandardEdition,
            [DisplayNameKey("EnterpriseEdition")]
            SqlServerEnterpriseEdition,
            [DisplayNameKey("ExpressEdition")]
            SqlServerExpressEdition,
            [DisplayNameKey("SqlAzureDatabaseEdition")]
            SqlAzureDatabaseEdition,
            [DisplayNameKey("SqlDataWarehouseEdition")]
            SqlDatawarehouseEdition,
            [DisplayNameKey("StretchEdition")]
            SqlServerStretchEdition,
            [DisplayNameKey("SqlServerManagedInstanceEdition")]
            SqlServerManagedInstanceEdition,
            [DisplayNameKey("SqlServerOnDemandEdition")]
            SqlServerOnDemandEdition,
            [DisplayNameKey("SqlDatabaseEdgeEdition")]
            SqlDatabaseEdgeEdition,
            [DisplayNameKey("SqlAzureArcManagedInstanceEdition")]
            SqlAzureArcManagedInstanceEdition,

            /*
             * NOTE: If you're adding new value here,
             * please update appropriate enums in ConnectionEnums.cs, Enumerations.cs
             * and src\Microsoft\SqlServer\Management\ConnectionInfo\StringConnectionInfo.strings
             */
        }

        /// <summary>
        /// Defines options for scripting statistics
        /// </summary>
        public enum ScriptStatisticsOptions
        {
            [DisplayNameKey("ScriptStatisticsNone")]
            ScriptStatsNone,
            [DisplayNameKey("ScriptStatisticsDDL")]
            ScriptStatsDDL,
            [DisplayNameKey("ScriptStatisticsAll")]
            ScriptStatsAll
        }

        /// <summary>
        /// Defines potential values for boolean property types
        /// </summary>
        public enum BooleanTypeOptions
        {
            [DisplayNameKey("TrueText")]
            True,
            [DisplayNameKey("FalseText")]
            False,
        }

        /// <summary>
        /// Defines options for scripting Create, Drop, or Drop and Create
        /// </summary>
        public enum ScriptCreateDropOptions
        {
            [DisplayNameKey("ScriptCreate")]
            ScriptCreate,
            [DisplayNameKey("ScriptDrop")]
            ScriptDrop,
            [DisplayNameKey("ScriptCreateDrop")]
            ScriptCreateDrop
        }

        /// <summary>
        /// Defines options for scripting schema, data, or both
        /// </summary>
        public enum TypeOfDataToScriptOptions
        {
            [DisplayNameKey("SchemaAndData")]
            SchemaAndData,
            [DisplayNameKey("DataOnly")]
            DataOnly,
            [DisplayNameKey("SchemaOnly")]
            SchemaOnly
        }
        #endregion

        #region Private variables
        private BooleanTypeOptions generateScriptForDependentObjects = BooleanTypeOptions.False;
        private BooleanTypeOptions includeDescriptiveHeaders = BooleanTypeOptions.False;
        private BooleanTypeOptions includeScriptingParametersHeader = BooleanTypeOptions.False;
        private BooleanTypeOptions includeVarDecimal = BooleanTypeOptions.True;
        private BooleanTypeOptions scriptExtendedProperties = BooleanTypeOptions.True;
        private BooleanTypeOptions scriptUseDatabase = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptSQLLogins = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptObjectLevelPermissions = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptOwner = BooleanTypeOptions.False;
        private ScriptCreateDropOptions scriptCreateDrop = ScriptCreateDropOptions.ScriptCreate;
        private ScriptCompatibilityOptions compatMode;
        private ScriptDatabaseEngineType engineType;
        private ScriptDatabaseEngineEdition databaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerStandardEdition;
        private Common.DatabaseEngineEdition sourceEngineEdition = Common.DatabaseEngineEdition.SqlDatabase;
        private Common.DatabaseEngineType sourceEngineType = Common.DatabaseEngineType.Unknown;
        private BooleanTypeOptions scriptIndexes = BooleanTypeOptions.True;
        private BooleanTypeOptions scriptFullTextIndexes = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptTriggers = BooleanTypeOptions.True;
        private BooleanTypeOptions scriptPrimaryKeys = BooleanTypeOptions.True;
        private BooleanTypeOptions scriptForeignKeys = BooleanTypeOptions.True;
        private BooleanTypeOptions uniqueKeys = BooleanTypeOptions.True;
        private BooleanTypeOptions scriptCheckConstraints = BooleanTypeOptions.True;
        private BooleanTypeOptions schemaQualify = BooleanTypeOptions.True;
        private BooleanTypeOptions includeIfNotExists = BooleanTypeOptions.True;
        private BooleanTypeOptions collation = BooleanTypeOptions.False;
        private BooleanTypeOptions continueScriptingOnError = BooleanTypeOptions.False;
        private BooleanTypeOptions appendToFile = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptDefaults = BooleanTypeOptions.True;
        private BooleanTypeOptions convertUDDTToBaseType = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptDriIncludeSystemNames = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptAnsiPadding = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptChangeTracking = BooleanTypeOptions.False;
        private BooleanTypeOptions scriptDataCompressionOptions = BooleanTypeOptions.True;
        private BooleanTypeOptions scriptXmlCompressionOptions = BooleanTypeOptions.True;
        private BooleanTypeOptions bindings = BooleanTypeOptions.True;
        private TypeOfDataToScriptOptions typeOfDataToScript = TypeOfDataToScriptOptions.SchemaOnly;
        private ScriptStatisticsOptions scriptStatistics = ScriptStatisticsOptions.ScriptStatsNone;
        private int sqlServerVersion = 0;  // used to determine what options should be shown
        private int sqlServerVersionMinor = 0;
        private BooleanTypeOptions scriptUnsupportedStatments = BooleanTypeOptions.False;

        // defaults and props to make read/only
        private System.Collections.Generic.Dictionary<string, object> readonlyProperties = new Dictionary<string, object>(8);
        // dictionary to store any values that the defaults would have wiped away
        private System.Collections.Generic.Dictionary<string, object> originalValues = new Dictionary<string, object>(8);

        #endregion

        /// <summary>
        /// Constructions a SqlScriptOptions object whose destination settings are for the given SQL Server version
        /// </summary>
        /// <param name="version"></param>
        public SqlScriptOptions(Version version)
        {
            sqlServerVersion = version.Major;
            sqlServerVersionMinor = version.Minor;
            if (sqlServerVersion < 9)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            var compatOption = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(sqlServerVersion, sqlServerVersionMinor);
            if (compatOption != null)
            {
                this.compatMode = compatOption.Value;
            }
            // VBUMP
            else
            {
                SqlScriptPublishModelTraceHelper.Assert(false, "Unexpected server version. Setting Compatibility Mode to 17.0!");
                compatMode = ScriptCompatibilityOptions.Script170Compat;
            }

            // setup the SqlAzure read/only properites and their default values
            readonlyProperties.Add(nameof(ScriptUseDatabase), BooleanTypeOptions.False);
        }

        /// <summary>
        /// Copies current properties to another SqlScriptOptions instance
        /// </summary>
        /// <param name="source"></param>
        public void Copy(SqlScriptOptions source)
        {
            if (source != null)
            {
                this.appendToFile = source.appendToFile;
                this.bindings = source.bindings;
                this.collation = source.collation;
                this.compatMode = source.compatMode;
                this.engineType = source.engineType;
                this.databaseEngineEdition = source.databaseEngineEdition;
                this.continueScriptingOnError = source.continueScriptingOnError;
                this.convertUDDTToBaseType = source.convertUDDTToBaseType;
                this.generateScriptForDependentObjects = source.generateScriptForDependentObjects;
                this.includeDescriptiveHeaders = source.includeDescriptiveHeaders;
                this.includeScriptingParametersHeader = source.includeScriptingParametersHeader;
                this.includeIfNotExists = source.includeIfNotExists;
                this.includeVarDecimal = source.includeVarDecimal;
                this.schemaQualify = source.schemaQualify;
                this.scriptAnsiPadding = source.scriptAnsiPadding;
                this.scriptChangeTracking = source.scriptChangeTracking;
                this.scriptCheckConstraints = source.scriptCheckConstraints;
                this.scriptCreateDrop = source.scriptCreateDrop;
                this.typeOfDataToScript = source.typeOfDataToScript;
                this.scriptDataCompressionOptions = source.scriptDataCompressionOptions;
                this.scriptXmlCompressionOptions = source.scriptXmlCompressionOptions;
                this.scriptDefaults = source.scriptDefaults;
                this.scriptDriIncludeSystemNames = source.scriptDriIncludeSystemNames;
                this.scriptExtendedProperties = source.scriptExtendedProperties;
                this.scriptForeignKeys = source.scriptForeignKeys;
                this.scriptFullTextIndexes = source.scriptFullTextIndexes;
                this.scriptIndexes = source.scriptIndexes;
                this.scriptObjectLevelPermissions = source.scriptObjectLevelPermissions;
                this.scriptOwner = source.scriptOwner;
                this.scriptPrimaryKeys = source.scriptPrimaryKeys;
                this.scriptSQLLogins = source.scriptSQLLogins;
                this.scriptStatistics = source.scriptStatistics;
                this.scriptTriggers = source.scriptTriggers;
                this.scriptUseDatabase = source.scriptUseDatabase;
                this.sourceEngineEdition = source.sourceEngineEdition;
                this.sourceEngineType = source.sourceEngineType;
                this.sqlServerVersion = source.sqlServerVersion;
                this.uniqueKeys = source.uniqueKeys;
            }
        }

        public ICollection ConfigureVisibleEnumFields(ITypeDescriptorContext context, ArrayList values)
        {
            if (context.PropertyDescriptor.PropertyType == typeof(ScriptDatabaseEngineType))
            {
                if (this.sourceEngineEdition == Common.DatabaseEngineEdition.SqlDataWarehouse)
                {
                    values.Remove(ScriptDatabaseEngineType.SingleInstance);
                }
            }
            else if (context.PropertyDescriptor.PropertyType == typeof(ScriptDatabaseEngineEdition))
            {
                // if the target engine type is SQL Server on prem
                // remove any SQL Azure related engine editions
                if (this.TargetDatabaseEngineType == ScriptDatabaseEngineType.SingleInstance)
                {
                    // remove the enum values that are not applicable
                    values.Remove(ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition);
                    values.Remove(ScriptDatabaseEngineEdition.SqlDatawarehouseEdition);
                    values.Remove(ScriptDatabaseEngineEdition.SqlServerOnDemandEdition);
                }
                else if (this.TargetDatabaseEngineType == ScriptDatabaseEngineType.SqlAzure)
                {
                    //  Need to block picking DW edition if the source is not DW,
                    //  and block picking any other edition than DW if the source is DW.
                    if (this.sourceEngineEdition == Common.DatabaseEngineEdition.SqlDataWarehouse)
                    {
                        // remove the enum values that are not applicable
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerPersonalEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerStandardEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerExpressEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerStretchEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlDatabaseEdgeEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerOnDemandEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlAzureArcManagedInstanceEdition);
                    }
                    else
                    {
                        // remove the enum values that are not applicable
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerPersonalEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerStandardEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerExpressEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerStretchEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlDatabaseEdgeEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlDatawarehouseEdition);
                        values.Remove(ScriptDatabaseEngineEdition.SqlAzureArcManagedInstanceEdition);
                    }
                }
                else
                {
                    throw new ArgumentException(SR.ERROR_UnexpectedDatabaseEngineTypeDetected(this.TargetDatabaseEngineType.ToString()));
                }
            }
            else if (context.PropertyDescriptor.PropertyType == typeof(ScriptCompatibilityOptions))
            {
                // Only check specific engine versions for standalone (non-Azure) instances,
                // since Azure supports all the latest SQL versions despite presenting a
                // server version of 12.0.
                if (engineType == ScriptDatabaseEngineType.SingleInstance)
                {
                    var options = values.Cast<ScriptCompatibilityOptions>().ToList();
                    options = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(options, sqlServerVersion, sqlServerVersionMinor);
                    values = new ArrayList(options);
                }
                else
                {
                    // Remove 170 since it's currently only for standalone instances
                    values.Remove(ScriptCompatibilityOptions.Script170Compat);
                }
            }
            return values;
        }


        public virtual void LoadShellScriptingOptions(IScriptPublishOptions scriptingOptions, Smo.SqlSmoObject smoObject)
        {
            this.generateScriptForDependentObjects = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptDependentObjects);
            this.includeDescriptiveHeaders = ConvertBooleanToBooleanTypeOption(scriptingOptions.IncludeHeaders);
            this.includeScriptingParametersHeader = ConvertBooleanToBooleanTypeOption(scriptingOptions.IncludeScriptingParametersHeader);
            this.bindings = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptBinding);
            this.includeVarDecimal = ConvertBooleanToBooleanTypeOption(scriptingOptions.IncludeVarDecimal);
            this.scriptExtendedProperties = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptExtendedProperties);
            this.scriptUseDatabase = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptUseDatabase);
            this.scriptObjectLevelPermissions = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptPermissions);
            this.scriptOwner = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptOwner);
            this.scriptIndexes = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptIndexes);
            this.scriptFullTextIndexes = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptFullTextIndexes);
            this.scriptTriggers = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptTriggers);
            this.scriptPrimaryKeys = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptPrimaryKeys);
            this.scriptForeignKeys = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptForeignKeys);
            this.uniqueKeys = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptUniqueKeys);
            this.scriptCheckConstraints = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptCheckConstraints);
            this.schemaQualify = ConvertBooleanToBooleanTypeOption(scriptingOptions.SchemaQualify);
            this.includeIfNotExists = ConvertBooleanToBooleanTypeOption(scriptingOptions.IncludeIfNotExists);
            this.collation = ConvertBooleanToBooleanTypeOption(scriptingOptions.IncludeCollation);
            this.scriptDefaults = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptDefaults);
            this.convertUDDTToBaseType = ConvertBooleanToBooleanTypeOption(scriptingOptions.ConvertUddtToBaseType);
            this.scriptDriIncludeSystemNames = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptDriIncludeSystemNames);
            this.scriptAnsiPadding = ConvertBooleanToBooleanTypeOption(scriptingOptions.GenerateAnsiPadding);
            if (scriptingOptions.ScriptStatistics)
            {
                scriptStatistics = ScriptStatisticsOptions.ScriptStatsDDL;
            }
            this.scriptChangeTracking = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptChangeTracking);
            this.scriptDataCompressionOptions = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptDataCompressionOptions);
            this.scriptXmlCompressionOptions = ConvertBooleanToBooleanTypeOption(scriptingOptions.ScriptXmlCompressionOptions);
            this.sourceEngineEdition = smoObject.DatabaseEngineEdition;
            this.sourceEngineType = smoObject.DatabaseEngineType;
            LoadDestinationOptions(scriptingOptions, smoObject);
        }

        void LoadDestinationOptions(IScriptPublishOptions scriptingOptions, Smo.SqlSmoObject smoObject)
        {
            var smoOptions = smoObject == null ? scriptingOptions.GetSmoScriptingOptions() : scriptingOptions.GetSmoScriptingOptions(smoObject);
            switch (smoOptions.TargetServerVersion)
            {
                case Smo.SqlServerVersion.Version90:
                    this.compatMode = ScriptCompatibilityOptions.Script90Compat;
                    break;
                case Smo.SqlServerVersion.Version100:
                case Smo.SqlServerVersion.Version105:
                    this.compatMode = ScriptCompatibilityOptions.Script100Compat;
                    break;
                case Smo.SqlServerVersion.Version110:
                    this.compatMode = ScriptCompatibilityOptions.Script110Compat;
                    break;
                case Smo.SqlServerVersion.Version120:
                    this.compatMode = ScriptCompatibilityOptions.Script120Compat;
                    break;
                case Smo.SqlServerVersion.Version130:
                    this.compatMode = ScriptCompatibilityOptions.Script130Compat;
                    break;
                case Smo.SqlServerVersion.Version140:
                    this.compatMode = ScriptCompatibilityOptions.Script140Compat;
                    break;
                case Smo.SqlServerVersion.Version150:
                    this.compatMode = ScriptCompatibilityOptions.Script150Compat;
                    break;
                case Smo.SqlServerVersion.Version160:
                default:
                    compatMode = ScriptCompatibilityOptions.Script160Compat;
                    break;
            }

            this.TargetDatabaseEngineType = ScriptDatabaseEngineType.SingleInstance;

            switch (smoOptions.TargetDatabaseEngineEdition)
            {
                case Common.DatabaseEngineEdition.Enterprise:
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition;
                    break;
                case Common.DatabaseEngineEdition.Express:
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerExpressEdition;
                    break;
                case Common.DatabaseEngineEdition.Personal:
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerPersonalEdition;
                    break;
                case Common.DatabaseEngineEdition.SqlDatabase:
                    this.TargetDatabaseEngineType = ScriptDatabaseEngineType.SqlAzure;
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition;
                    break;
                case Common.DatabaseEngineEdition.SqlDataWarehouse:
                    this.TargetDatabaseEngineType = ScriptDatabaseEngineType.SqlAzure;
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlDatawarehouseEdition;
                    break;
                case Common.DatabaseEngineEdition.SqlStretchDatabase:
                    this.TargetDatabaseEngineType = ScriptDatabaseEngineType.SqlAzure;
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerStretchEdition;
                    break;
                case Common.DatabaseEngineEdition.Standard:
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerStandardEdition;
                    break;
                case Common.DatabaseEngineEdition.SqlManagedInstance:
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition;
                    break;
                case Common.DatabaseEngineEdition.SqlOnDemand:
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerOnDemandEdition;
                    break;
                case Common.DatabaseEngineEdition.SqlDatabaseEdge:
                    this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlDatabaseEdgeEdition;
                    break;
                default:
                    throw new ArgumentException(SR.ERROR_UnexpectedDatabaseEngineEditionDetected(scriptingOptions.TargetDatabaseEngineEdition));
            }

        }
        [DisplayNameKey("ScriptConvertUDDT")]
        [DisplayDescriptionKeyAttribute("Generateconvertuddt")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ConvertUDDTToBaseType
        {
            get
            {
                return this.convertUDDTToBaseType;
            }
            set
            {
                this.convertUDDTToBaseType = value;
            }
        }

        [DisplayNameKey("ScriptUseDatabase")]
        [DisplayDescriptionKeyAttribute("Generateusedatabase")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ScriptUseDatabase
        {
            get
            {
                return this.scriptUseDatabase;
            }
            set
            {
                this.scriptUseDatabase = value;
            }
        }

        [DisplayNameKey("ScriptLogins")]
        [DisplayDescriptionKeyAttribute("Generatescriptfordatabaselogins")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ScriptLogins
        {
            get
            {
                return this.scriptSQLLogins;
            }
            set
            {
                this.scriptSQLLogins = value;
            }
        }

        [DisplayNameKey("ScriptCreateDrop")]
        [DisplayDescriptionKeyAttribute("ScriptCreateDropDescription")]
        [DisplayCategoryKey("General")]
        public virtual ScriptCreateDropOptions ScriptCreateDrop
        {
            get
            {
                return this.scriptCreateDrop;
            }
            set
            {
                this.scriptCreateDrop = value;
            }
        }

        [DisplayNameKey("TypesOfDataToScript")]
        [DisplayDescriptionKeyAttribute("TypesOfDataToScriptDesc")]
        [DisplayCategoryKey("General")]
        public virtual TypeOfDataToScriptOptions TypeOfDataToScript
        {
            get
            {
                return this.typeOfDataToScript;
            }
            set
            {
                this.typeOfDataToScript = value;
            }
        }

        [DisplayNameKey("ScriptObjectLevelPermissions")]
        [DisplayDescriptionKeyAttribute("Generateobjectlevelpermissions")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ScriptObjectLevelPermissions
        {
            get
            {
                return this.scriptObjectLevelPermissions;
            }
            set
            {
                this.scriptObjectLevelPermissions = value;
            }
        }

        [DisplayNameKey("ScriptOwner")]
        [DisplayDescriptionKeyAttribute("ScriptOwnerForTheObjects")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ScriptOwner
        {
            get
            {
                return this.scriptOwner;
            }
            set
            {
                this.scriptOwner = value;
            }
        }

        [DisplayNameKey("GenerateScriptForDependentObjects")]
        [DisplayDescriptionKeyAttribute("Generatescriptfordependentobjectsofeachtablescripted")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions GenerateScriptForDependentObjects
        {
            get
            {
                return this.generateScriptForDependentObjects;
            }
            set
            {
                this.generateScriptForDependentObjects = value;
            }
        }

        [DisplayNameKey("IncludeDescriptiveHeaders")]
        [DisplayDescriptionKeyAttribute("Includedescriptiveheadersforeachtablescripted")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions IncludeDescriptiveHeaders
        {
            get
            {
                return this.includeDescriptiveHeaders;
            }
            set
            {
                this.includeDescriptiveHeaders = value;
            }
        }

        [DisplayNameKey("IncludeScriptingParametersHeader")]
        [DisplayDescriptionKeyAttribute("IncludeScriptingParametersHeaderDescription")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions IncludeScriptingParametersHeader
        {
            get
            {
                return this.includeScriptingParametersHeader;
            }
            set
            {
                this.includeScriptingParametersHeader = value;
            }
        }

        [DisplayNameKey("IncludeVarDecimal")]
        [DisplayDescriptionKeyAttribute("IncludeVarDecimalDescription")]
        [DisplayCategoryKey("General")]
        [BrowsableAttribute(false)]
        public virtual BooleanTypeOptions IncludeVarDecimal
        {
            get
            {
                return this.includeVarDecimal;
            }

            set
            {
                this.includeVarDecimal = value;
            }
        }

        [DisplayNameKey("Bindings")]
        [DisplayDescriptionKeyAttribute("BindingsDescription")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions Bindings
        {
            get
            {
                return this.bindings;
            }

            set
            {
                this.bindings = value;
            }
        }

        [DisplayNameKey("ContinueScriptingOnError")]
        [DisplayDescriptionKeyAttribute("Continuescriptifanerroroccuredorotherwisestop")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ContinueScriptingOnError
        {
            get
            {
                return this.continueScriptingOnError;
            }
            set
            {
                this.continueScriptingOnError = value;
            }
        }

        [DisplayNameKey("AppendToFile")]
        [DisplayDescriptionKeyAttribute("Appendtofilethegeneratedscript")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions AppendToFile
        {
            get
            {
                return this.appendToFile;
            }
            set
            {
                this.appendToFile = value;
            }
        }

        [DisplayNameKey("ScriptExtendedProperties")]
        [DisplayDescriptionKeyAttribute("Scriptextendedpropertiesforeachtablescripted")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ScriptExtendedProperties
        {
            get
            {
                return this.scriptExtendedProperties;
            }
            set
            {
                this.scriptExtendedProperties = value;
            }
        }


        [DisplayNameKey("ScriptStatistics")]
        [DisplayDescriptionKeyAttribute("generatescriptstatistics")]
        [DisplayCategoryKey("General")]
        public virtual ScriptStatisticsOptions ScriptStatistics
        {
            get
            {
                return this.scriptStatistics;
            }
            set
            {
                this.scriptStatistics = value;
            }
        }

        [DisplayNameKey("ScriptDriIncludeSystemNames")]
        [DisplayDescriptionKeyAttribute("generateDriIncludeSystemNames")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ScriptDriIncludeSystemNames
        {
            get
            {
                return this.scriptDriIncludeSystemNames;
            }
            set
            {
                this.scriptDriIncludeSystemNames = value;
            }
        }

        [DisplayNameKey("ScriptAnsiPadding")]
        [DisplayDescriptionKeyAttribute("generateAnsiPadding")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions ScriptAnsiPadding
        {
            get
            {
                return this.scriptAnsiPadding;
            }
            set
            {
                this.scriptAnsiPadding = value;
            }
        }

        [DisplayNameKey("SchemaQualify")]
        [DisplayDescriptionKeyAttribute("Prefixschemaforobjects")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions SchemaQualify
        {
            get
            {
                return this.schemaQualify;
            }
            set
            {
                this.schemaQualify = value;
            }
        }

        internal virtual Common.DatabaseEngineEdition SourceEngineEdition
        {
            get
            {
                return this.sourceEngineEdition;
            }
            set
            {
                this.sourceEngineEdition = value;
            }
        }

        internal virtual Common.DatabaseEngineType SourceEngineType
        {
            get
            {
                return this.sourceEngineType;
            }
            set
            {
                this.sourceEngineType = value;
            }
        }

        [DisplayNameKey("IncludeIfNotExists")]
        [DisplayDescriptionKeyAttribute("Scriptobjectswithifnotexistoption")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions IncludeIfNotExists
        {
            get
            {
                return this.includeIfNotExists;
            }
            set
            {
                this.includeIfNotExists = value;
            }
        }

        [DisplayNameKey("GenerateScriptCollation")]
        [DisplayDescriptionKeyAttribute("Scriptobjectswithcollation")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions Collation
        {
            get
            {
                return this.collation;
            }
            set
            {
                this.collation = value;
            }
        }

        [DisplayNameKey("Default")]
        [DisplayDescriptionKeyAttribute("ScriptDefaults")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions Default
        {
            get
            {
                return this.scriptDefaults;
            }
            set
            {
                this.scriptDefaults = value;
            }
        }

        [DisplayNameKey("CompatibleFeatures")]
        [DisplayDescriptionKeyAttribute("CompatibleFeaturesDescription")]
        [DisplayCategoryKey("General")]
        public virtual ScriptCompatibilityOptions ScriptCompatibilityOption
        {
            get
            {
                return this.compatMode;
            }
            set
            {
                this.compatMode = value;
            }
        }

        [DisplayNameKey("TargetEngineType")]
        [DisplayDescriptionKeyAttribute("TargetEngineTypeDescription")]
        [DisplayCategoryKey("General")]
        public virtual ScriptDatabaseEngineType TargetDatabaseEngineType
        {
            get
            {
                return this.engineType;
            }
            set
            {
                // only bother if this is a new value
                if (this.engineType != value)
                {
                    ScriptDatabaseEngineType currentValue = this.engineType;
                    this.engineType = value;

                    // reset the database engine edition default based on the engine type value
                    if (this.engineType == ScriptDatabaseEngineType.SingleInstance)
                    {
                        this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlServerStandardEdition;
                    }
                    else
                    {
                        SqlScriptPublishModelTraceHelper.Assert(this.engineType == ScriptDatabaseEngineType.SqlAzure, "Unexpected database engine type detected, expecting SQL Azure.");
                        this.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition;
                    }

                    // if we don't have a handler we are done
                    if (this.ReadOnlyPropertyChanged != null)
                    {
                        // if we are going to SqlAzure or coming from SqlAzure then we fire the event
                        if (value == ScriptDatabaseEngineType.SqlAzure
                            || currentValue == ScriptDatabaseEngineType.SqlAzure)
                        {
                            this.ReadOnlyPropertyChanged(this, new ReadOnlyPropertyChangedEventArgs("TargetDatabaseEngineType"));
                        }
                    }
                }
            }
        }


        [DisplayNameKey("TargetEngineEdition")]
        [DisplayDescriptionKeyAttribute("TargetEngineEditionDescription")]
        [DisplayCategoryKey("General")]
        public virtual ScriptDatabaseEngineEdition TargetDatabaseEngineEdition
        {
            get
            {
                return this.databaseEngineEdition;
            }
            set
            {
                // only bother if this is a new value
                if (this.databaseEngineEdition != value)
                {
                    ScriptDatabaseEngineEdition currentValue = this.databaseEngineEdition;

                    if (value == ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition
                        || value == ScriptDatabaseEngineEdition.SqlDatawarehouseEdition)
                    {
                        if (this.TargetDatabaseEngineType != ScriptDatabaseEngineType.SqlAzure)
                        {
                            throw new ArgumentException(SR.ERROR_UnsupportedDatabaseEngineEditionAndDatabaseEngineTypeOptionCombination(value.ToString(), this.TargetDatabaseEngineType.ToString()));
                        }
                    }
                    else if (value == ScriptDatabaseEngineEdition.SqlServerPersonalEdition
                        || value == ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition
                        || value == ScriptDatabaseEngineEdition.SqlServerExpressEdition
                        || value == ScriptDatabaseEngineEdition.SqlServerStandardEdition
                        || value == ScriptDatabaseEngineEdition.SqlServerStretchEdition
                        || value == ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition
                        || value == ScriptDatabaseEngineEdition.SqlServerOnDemandEdition
                        )
                    {
                        if (this.TargetDatabaseEngineType != ScriptDatabaseEngineType.SingleInstance)
                        {
                            throw new ArgumentException(SR.ERROR_UnsupportedDatabaseEngineEditionAndDatabaseEngineTypeOptionCombination(value.ToString(), this.TargetDatabaseEngineType.ToString()));
                        }
                    }

                    this.databaseEngineEdition = value;

                    // if we don't have a handler we are done
                    if (this.ReadOnlyPropertyChanged != null)
                    {
                        this.ReadOnlyPropertyChanged(this, new ReadOnlyPropertyChangedEventArgs(nameof(TargetDatabaseEngineEdition)));
                    }
                }
            }
        }

        [DisplayNameKey("IncludeUnsupportedStatements")]
        [DisplayDescriptionKeyAttribute("IncludeUnsupportedStatementsDescription")]
        [DisplayCategoryKey("General")]
        public virtual BooleanTypeOptions IncludeUnsupportedStatements
        {
            get
            {
                return this.scriptUnsupportedStatments;
            }
            set
            {
                this.scriptUnsupportedStatments = value;
            }
        }

        [DisplayNameKey("ScriptIndexes")]
        [DisplayDescriptionKeyAttribute("Scriptindexesforeachtablescripted")]
        [DisplayCategoryKey("TableView")]
        public virtual BooleanTypeOptions ScriptIndexes
        {
            get
            {
                return this.scriptIndexes;
            }
            set
            {
                this.scriptIndexes = value;
            }
        }

        [DisplayNameKey("ScriptFullTextIndexes")]
        [DisplayDescriptionKeyAttribute("Scriptfulltextindexesforeachtablescripted")]
        [DisplayCategoryKey("TableView")]
        public virtual BooleanTypeOptions ScriptFullTextIndexes
        {
            get
            {
                return this.scriptFullTextIndexes;
            }
            set
            {
                this.scriptFullTextIndexes = value;
            }
        }

        [DisplayNameKey("ScriptTriggers")]
        [DisplayDescriptionKeyAttribute("Scripttriggersforeachtablescripted")]
        [DisplayCategoryKey("TableView")]
        public virtual BooleanTypeOptions ScriptTriggers
        {
            get
            {
                return this.scriptTriggers;
            }
            set
            {
                this.scriptTriggers = value;
            }
        }

        [DisplayNameKey("ScriptPrimaryKeys")]
        [DisplayDescriptionKeyAttribute("Scriptprimarykeysforeachtablescripted")]
        [DisplayCategoryKey("TableView")]
        public virtual BooleanTypeOptions ScriptPrimaryKeys
        {
            get
            {
                return this.scriptPrimaryKeys;
            }
            set
            {
                this.scriptPrimaryKeys = value;
            }
        }

        [DisplayNameKey("ScriptUniqueKeys")]
        [DisplayDescriptionKeyAttribute("Scriptuniqueskeysforeachtablescripted")]
        [DisplayCategoryKey("TableView")]
        public virtual BooleanTypeOptions UniqueKeys
        {
            get
            {
                return this.uniqueKeys;
            }
            set
            {
                this.uniqueKeys = value;
            }
        }

        [DisplayNameKey("ScriptForeignKeys")]
        [DisplayDescriptionKeyAttribute("Scriptforeignkeysforeachtablescripted")]
        [DisplayCategoryKey("TableView")]
        public virtual BooleanTypeOptions ScriptForeignKeys
        {
            get
            {
                return this.scriptForeignKeys;
            }
            set
            {
                this.scriptForeignKeys = value;
            }
        }

        [DisplayNameKey("ScriptChangeTracking")]
        [DisplayDescriptionKeyAttribute("ScriptChangeTrackingInformation")]
        [DisplayCategoryKey("TableView")]
        public BooleanTypeOptions ScriptChangeTracking
        {
            get
            {
                return this.scriptChangeTracking;
            }
            set
            {
                this.scriptChangeTracking = value;
            }
        }

        [DisplayNameKey("ScriptDataCompression")]
        [DisplayDescriptionKeyAttribute("ScriptDataCompressionInformation")]
        [DisplayCategoryKey("TableView")]
        public BooleanTypeOptions ScriptDataCompressionOptions
        {
            get
            {
                return this.scriptDataCompressionOptions;
            }
            set
            {
                this.scriptDataCompressionOptions = value;
            }
        }

        [DisplayNameKey("ScriptXmlCompression")]
        [DisplayDescriptionKeyAttribute("ScriptXmlCompressionInformation")]
        [DisplayCategoryKey("TableView")]
        public BooleanTypeOptions ScriptXmlCompressionOptions
        {
            get
            {
                return this.scriptXmlCompressionOptions;
            }
            set
            {
                this.scriptXmlCompressionOptions = value;
            }
        }

        [DisplayNameKey("ScriptCheckConstraints")]
        [DisplayDescriptionKeyAttribute("Scriptcheckconstraintsforeachtablescripted")]
        [DisplayCategoryKey("TableView")]
        public virtual BooleanTypeOptions ScriptCheckConstraints
        {
            get
            {
                return this.scriptCheckConstraints;
            }
            set
            {
                this.scriptCheckConstraints = value;
            }
        }

        /// <summary>
        /// Converts BooleanTypeOptions type to boolean type
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        internal static bool ConvertBooleanTypeOptionToBoolean(BooleanTypeOptions option)
        {
            if (option == BooleanTypeOptions.True)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Converts boolean type to BooleanTypeOptions type
        /// </summary>
        /// <param name="boolValue"></param>
        /// <returns></returns>
        internal static BooleanTypeOptions ConvertBooleanToBooleanTypeOption(bool boolValue)
        {
            if (boolValue)
            {
                return BooleanTypeOptions.True;
            }
            else
            {
                return BooleanTypeOptions.False;
            }
        }

        public event EventHandler<ReadOnlyPropertyChangedEventArgs> ReadOnlyPropertyChanged;
        void IDynamicReadOnly.OverrideReadOnly(IList<LocalizablePropertyDescriptor> properties, ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            // we only will override ReadOnlyness if the ServerVersion is "SqlAzure"
            // we don't have to set the Override back because each call into AddProperties is a new set
            // of PropertyDesctipors which have the default read/only behaviour
            if (this.engineType == ScriptDatabaseEngineType.SqlAzure)
            {
                foreach (LocalizablePropertyDescriptor propertyDescriptor in properties)
                {
                    object defaultValue = null;
                    if (readonlyProperties.TryGetValue(propertyDescriptor.Name, out defaultValue))
                    {
                        propertyDescriptor.ForceReadOnly();
                        // if the current value is different then the default value
                        // and we don't have an original value, store the value
                        // we'll put it back later.

                        // Note: We have to check for the existance of the value beause it is possible this function
                        // could be called multiple times without the value of CompatibiltyOption changing.
                        // ForceReadOnly needs to be called every time since they are new PropertyDescirptors
                        // but the original values would have been captured the first time through, so we don't want to keep
                        // doing it, or we'll lose the true original value.
                        object originalValue = propertyDescriptor.GetValue(this);
                        if (defaultValue != originalValue && !originalValues.ContainsKey(propertyDescriptor.Name))
                        {
                            originalValues.Add(propertyDescriptor.Name, originalValue);
                        }
                        propertyDescriptor.SetValue(this, defaultValue);
                    }
                }
            }
            else if (originalValues.Count > 0)   // if we have any orignal values we need to put them back
            {
                foreach (PropertyDescriptor propertyDescriptor in properties)
                {
                    object originalValue = null;
                    if (originalValues.TryGetValue(propertyDescriptor.Name, out originalValue))
                    {
                        propertyDescriptor.SetValue(this, originalValue);
                    }
                }
                // we've put all the values back so clear out the dictionary so we don't keep doing this
                originalValues.Clear();
            }
        }

        /// <summary>
        /// Attribute for storing the minimum supported engine version for script compatibility levels.
        /// This is needed because version v105 shares a major version with v100, which throws off the
        /// enum offset for <see cref="ScriptCompatibilityOptions"/>. So, we can't do something easy
        /// like adding the minimum supported version to all the enum values to get their actual version.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class CompatibilityLevelSupportedVersionAttribute : Attribute
        {
            public int MinimumMajorVersion { get; private set; }
            public int MinimumMinorVersion { get; private set; }

            public CompatibilityLevelSupportedVersionAttribute(int majorVersion, int minorVersion = 0)
            {
                MinimumMajorVersion = majorVersion;
                MinimumMinorVersion = minorVersion;
            }

            /// <summary>
            /// Gets the matching <see cref="ScriptCompatibilityOptions"/> value for the specified engine version.
            /// </summary>
            /// <param name="majorVersion">The major version number of the engine version.</param>
            /// <param name="minorVersion">The minor version number of the engine version.</param>
            /// <returns>The corresponding compatibility option value, or null if none match the provided version.</returns>
            public static ScriptCompatibilityOptions? GetOptionForVersion(int majorVersion, int minorVersion = 0)
            {
                ScriptCompatibilityOptions? result = null;
                var allOptions = Enum.GetValues(typeof(ScriptCompatibilityOptions));
                foreach (ScriptCompatibilityOptions compatOption in allOptions)
                {
                    var attr = GetAttributeForOption(compatOption);
                    if (attr != null)
                    {
                        if (attr.MinimumMajorVersion == majorVersion && attr.MinimumMinorVersion == minorVersion)
                        {
                            result = compatOption;
                            break;
                        }
                    }
                }
                return result;
            }

            /// <summary>
            /// Filters the <see cref="ScriptCompatibilityOptions"/> from the provided list that
            /// are not supported for the specified engine version.
            /// </summary>
            /// <param name="options">List of options to check.</param>
            /// <param name="majorVersion">The major version number of the engine version.</param>
            /// <param name="minorVersion">The minor version number of the engine version.</param>
            /// <returns>The same list of options with unsupported options removed.</returns>
            public static List<ScriptCompatibilityOptions> FilterUnsupportedOptions(List<ScriptCompatibilityOptions> options, int majorVersion, int minorVersion)
            {
                if (options != null)
                {
                    // Start for loop from the end so we can remove elements as we go without
                    // throwing off the list ordering.
                    for (var i = options.Count - 1; i >= 0; i--)
                    {
                        var attr = GetAttributeForOption(options[i]);
                        if (attr != null)
                        {
                            if (attr.MinimumMajorVersion > majorVersion ||
                                (attr.MinimumMajorVersion == majorVersion && attr.MinimumMinorVersion > minorVersion))
                            {
                                options.RemoveAt(i);
                            }
                        }
                    }
                }
                return options;
            }

            /// <summary>
            /// Gets the <see cref="CompatibilityLevelSupportedVersionAttribute"/> associated with the specified <see cref="ScriptCompatibilityOptions"/> value.
            /// </summary>
            /// <param name="option">The compatibility option to retrieve an attribute for.</param>
            /// <returns>The associated version attribute, or null if none are set for the provided option.</returns>
            public static CompatibilityLevelSupportedVersionAttribute GetAttributeForOption(ScriptCompatibilityOptions option)
            {
                var enumType = typeof(ScriptCompatibilityOptions);
                var attrType = typeof(CompatibilityLevelSupportedVersionAttribute);
                var attributes =
                    enumType.GetMember(option.ToString())
                    .First(m => m.DeclaringType == enumType)
                    .GetCustomAttributes(attrType, false);

                if (attributes == null || attributes.Length == 0)
                {
                    return null;
                }
                else
                {
                    return (CompatibilityLevelSupportedVersionAttribute)attributes[0];
                }
            }
        }
    }
}
