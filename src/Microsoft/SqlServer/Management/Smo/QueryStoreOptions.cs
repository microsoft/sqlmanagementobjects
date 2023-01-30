// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

//1591 is for verifying all public members have xml documentation. It's really heavy handed though - requiring
//docs for things as specific as each enum value. Disabling since the documentation we have is enough. 
#pragma warning disable 1591

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// type converter for QueryStoreOperationMode
    /// </summary>
    public class QueryStoreOperationModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreOperationModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreOperationMode))
        { }
    }

    /// <summary>
    /// type converter for QueryStoreCaptureMode
    /// </summary>
    public class QueryStoreCaptureModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreCaptureModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreCaptureMode))
        { }
    }

    /// <summary>
    /// type converter for QueryStoreSizeBasedCleanupMode
    /// </summary>
    public class QueryStoreSizeBasedCleanupModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreSizeBasedCleanupModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreSizeBasedCleanupMode))
        { }

        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">An optional System.Globalization.CultureInfo. If not supplied, the current culture is assumed.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <param name="destinationType">The System.Type to convert the value to.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value == null)
            {
                return string.Empty;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// type converter for QueryStoreWaitStatsCaptureMode
    /// </summary>
    public class QueryStoreWaitStatsCaptureModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreWaitStatsCaptureModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreWaitStatsCaptureMode))
        { }

        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">An optional System.Globalization.CultureInfo. If not supplied, the current culture is assumed.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <param name="destinationType">The System.Type to convert the value to.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value == null)
            {
                return string.Empty;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// Operation Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreOperationModeConverter))]
    public enum QueryStoreOperationMode
    {
        [LocDisplayName("Off")]
        [TsqlSyntaxString("OFF")]
        Off = 0,
        [LocDisplayName("ReadOnly")]
        [TsqlSyntaxString("READ_ONLY")]
        ReadOnly = 1,
        [LocDisplayName("ReadWrite")]
        [TsqlSyntaxString("READ_WRITE")]
        ReadWrite = 2,
    }

    /// <summary>
    /// Capture Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreCaptureModeConverter))]
    public enum QueryStoreCaptureMode
    {
        [LocDisplayName("All")]
        [TsqlSyntaxString("ALL")]
        All = 1,
        [LocDisplayName("Auto")]
        [TsqlSyntaxString("AUTO")]
        Auto = 2,
        [LocDisplayName("None")]
        [TsqlSyntaxString("NONE")]
        None = 3,
        [LocDisplayName("Custom")]
        [TsqlSyntaxString("CUSTOM")]
        Custom = 4,
    }

    /// <summary>
    /// Size Based Cleanup Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreSizeBasedCleanupModeConverter))]
    public enum QueryStoreSizeBasedCleanupMode
    {
        [LocDisplayName("Off")]
        [TsqlSyntaxString("OFF")]
        Off = 0,
        [LocDisplayName("Auto")]
        [TsqlSyntaxString("AUTO")]
        Auto = 1,
    }

    /// <summary>
    /// Wait Statistics Capture Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreWaitStatsCaptureModeConverter))]
    public enum QueryStoreWaitStatsCaptureMode
    {
        [LocDisplayName("Off")]
        [TsqlSyntaxString("OFF")]
        Off = 0,
        [LocDisplayName("On")]
        [TsqlSyntaxString("ON")]
        On = 1,
    }

    /// <summary>
    /// QueryStore Options Smo Object.
    /// </summary>
    [SfcElementType("QueryStoreOptions")]
    public sealed partial class QueryStoreOptions : SqlSmoObject, IAlterable,  IScriptable
    {
        internal QueryStoreOptions(Database parentdb, ObjectKeyBase key, SqlSmoState state)
            : base(key, state)
        {
            this.singletonParent = parentdb;
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        internal static string UrnSuffix
        {
            get
            {
                return "QueryStoreOptions";
            }
        }

        /// <summary>
        /// Parent, Database object.
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Database Parent
        {
            get
            {
                return singletonParent as Database;
            }
        }

        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            // When doing an alter when database is in restoring state, we do not want to script query store options
            // Querying for this options in a restoring database will cause a null reference exception
            if (this.Parent.Status == DatabaseStatus.Restoring)
            {
                // Make sure none of the properties are dirty though because if the user changed a value that would indicate that
                // they are intending to actually modify those values but we can't do that when it's restoring
                var desiredStateProp = Properties.Get("DesiredState");
                var actualStateProp = Properties.Get("ActualState");
                var staleQueryThresholdInDaysProp = Properties.Get("StaleQueryThresholdInDays");
                var dataFlushIntervalInSecondsProp = Properties.Get("DataFlushIntervalInSeconds");
                var statisticsCollectionIntervalInMinutesProp = Properties.Get("StatisticsCollectionIntervalInMinutes");
                var maxStorageSizeInMBProp = Properties.Get("MaxStorageSizeInMB");
                var queryCaptureModeProp = Properties.Get("QueryCaptureMode");
                var sizeBasedCleanupModeProp = Properties.Get("SizeBasedCleanupMode");
                var maxPlansPerQueryProp = IsSupportedProperty("MaxPlansPerQuery") ? Properties.Get("MaxPlansPerQuery") : null;
                var waitStatsCaptureModeProp = IsSupportedProperty("WaitStatsCaptureMode") ? Properties.Get("WaitStatsCaptureMode") : null;
                var capturePolicyExecutionCountProp = IsSupportedProperty("CapturePolicyExecutionCount") ? Properties.Get("CapturePolicyExecutionCount") : null;
                var capturePolicyTotalCompileCpuTimeInMSProp = IsSupportedProperty("CapturePolicyTotalCompileCpuTimeInMS") ? Properties.Get("CapturePolicyTotalCompileCpuTimeInMS") : null;
                var capturePolicyTotalExecutionCpuTimeInMSProp = IsSupportedProperty("CapturePolicyTotalExecutionCpuTimeInMS") ? Properties.Get("CapturePolicyTotalExecutionCpuTimeInMS") : null;
                var capturePolicyStaleThresholdInHrsProp = IsSupportedProperty("CapturePolicyStaleThresholdInHrs") ? Properties.Get("CapturePolicyStaleThresholdInHrs") : null;

                if ((desiredStateProp != null && desiredStateProp.Dirty) ||
                    (actualStateProp != null && actualStateProp.Dirty) ||
                    (staleQueryThresholdInDaysProp != null && staleQueryThresholdInDaysProp.Dirty) ||
                    (dataFlushIntervalInSecondsProp != null && dataFlushIntervalInSecondsProp.Dirty) ||
                    (statisticsCollectionIntervalInMinutesProp != null && statisticsCollectionIntervalInMinutesProp.Dirty) ||
                    (maxStorageSizeInMBProp != null && maxStorageSizeInMBProp.Dirty) ||
                    (queryCaptureModeProp != null && queryCaptureModeProp.Dirty) ||
                    (sizeBasedCleanupModeProp != null && sizeBasedCleanupModeProp.Dirty) ||
                    (maxPlansPerQueryProp != null && maxPlansPerQueryProp.Dirty) ||
                    (waitStatsCaptureModeProp != null && waitStatsCaptureModeProp.Dirty) ||
                    (capturePolicyExecutionCountProp != null && capturePolicyExecutionCountProp.Dirty) ||
                    (capturePolicyTotalCompileCpuTimeInMSProp != null && capturePolicyTotalCompileCpuTimeInMSProp.Dirty) ||
                    (capturePolicyTotalExecutionCpuTimeInMSProp != null && capturePolicyTotalExecutionCpuTimeInMSProp.Dirty) ||
                    (capturePolicyStaleThresholdInHrsProp != null && capturePolicyStaleThresholdInHrsProp.Dirty))
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.AlterQueryStorePropertyForRestoringDatabase);
                }

                return;
            }
            
            ScriptQueryStoreOptions(query, sp, false /* scriptAll */);
        }

        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            ScriptQueryStoreOptions(query, sp, true /* scriptAll */);
        }

        /// <summary>
        /// Script out the QDS options for enabling/disabling the feature. 
        /// 
        /// ALTER DATABASE [$(DatabaseName)] SET QUERY_STORE = ON|OFF
        /// 
        /// {
        ///     ALTER DATABASE [$(DatabaseName)] SET QUERY_STORE (
        ///         OPERATION_MODE = OFF|READ_ONLY|READ_WRITE ,
        ///         CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = ##) ,
        ///         DATA_FLUSH_INTERVALS_SECONDS = ## ,
        ///         INTERVAL LENGTH MINUTES = ## ,
        ///         MAX_STORAGE_SIZE_MB = ## ,
        ///         QUERY_CAPTURE_MODE = ALL|AUTO ,
        ///         SIZE_BASED_CLEANUP_MODE = OFF|AUTO )
        /// }
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        /// <param name="scriptAll"></param>
        private void ScriptQueryStoreOptions(StringCollection query, ScriptingPreferences sp, bool scriptAll)
        {
            CheckObjectState();

            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder();

            Property desiredStateProp = this.GetPropertyOptional("DesiredState");
            if (desiredStateProp.Value == null)
            {
                return;
            }

            Property actualStateProp = this.GetPropertyOptional("ActualState");
            Property staleQueryThresholdInDaysProp = this.GetPropertyOptional("StaleQueryThresholdInDays");
            Property dataFlushIntervalInSecondsProp = this.GetPropertyOptional("DataFlushIntervalInSeconds");
            Property statisticsCollectionIntervalInMinutesProp = this.GetPropertyOptional("StatisticsCollectionIntervalInMinutes");
            Property maxStorageSizeInMBProp = this.GetPropertyOptional("MaxStorageSizeInMB");
            Property queryCaptureModeProp = this.GetPropertyOptional("QueryCaptureMode");
            Property sizeBasedCleanupModeProp = this.GetPropertyOptional("SizeBasedCleanupMode");

            Property maxPlansPerQueryProp = IsSupportedProperty("MaxPlansPerQuery") ? this.GetPropertyOptional("MaxPlansPerQuery") : null;
            Property waitStatsCaptureModeProp = IsSupportedProperty("WaitStatsCaptureMode") ? this.GetPropertyOptional("WaitStatsCaptureMode") : null;
            Property capturePolicyExecutionCountProp = IsSupportedProperty("CapturePolicyExecutionCount") ? this.GetPropertyOptional("CapturePolicyExecutionCount") : null;
            Property capturePolicyTotalCompileCpuTimeInMSProp = IsSupportedProperty("CapturePolicyTotalCompileCpuTimeInMS") ? this.GetPropertyOptional("CapturePolicyTotalCompileCpuTimeInMS") : null;
            Property capturePolicyTotalExecutionCpuTimeInMSProp = IsSupportedProperty("CapturePolicyTotalExecutionCpuTimeInMS") ? this.GetPropertyOptional("CapturePolicyTotalExecutionCpuTimeInMS") : null;
            Property capturePolicyStaleThresholdInHrsProp = IsSupportedProperty("CapturePolicyStaleThresholdInHrs") ? this.GetPropertyOptional("CapturePolicyStaleThresholdInHrs") : null;

            TypeConverter queryStoreOperationModeConverter = SmoManagementUtil.GetTypeConverter(typeof(QueryStoreOperationMode));
            TypeConverter queryStoreCaptureModeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(QueryStoreCaptureMode));
            TypeConverter cleanupModeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(QueryStoreSizeBasedCleanupMode));
            TypeConverter waitStatsCaptureModeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(QueryStoreWaitStatsCaptureMode));

            QueryStoreOperationMode desiredState = (QueryStoreOperationMode)desiredStateProp.Value;

            QueryStoreOperationMode actualState = actualStateProp.IsNull == false ?
                (QueryStoreOperationMode)actualStateProp.Value :
                QueryStoreOperationMode.Off;
            
            // QueryStore Update
            if (scriptAll ||
                (desiredStateProp.Dirty &&
                 ((desiredState == QueryStoreOperationMode.Off) != (actualState == QueryStoreOperationMode.Off))))
            {                
                sb.Append(
                    string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET QUERY_STORE = {1}",
                        SqlBraket(this.Parent.Name),
                        desiredState != QueryStoreOperationMode.Off ? "ON" : "OFF"));
                query.Add(sb.ToString());
            }

            if (desiredState == QueryStoreOperationMode.Off)
            {
                return;
            }

            sb = new StringBuilder();

            if (scriptAll ||
                desiredStateProp.Dirty ||
                staleQueryThresholdInDaysProp.Dirty ||
                dataFlushIntervalInSecondsProp.Dirty ||
                statisticsCollectionIntervalInMinutesProp.Dirty ||
                maxStorageSizeInMBProp.Dirty ||
                queryCaptureModeProp.Dirty ||
                sizeBasedCleanupModeProp.Dirty ||
                (maxPlansPerQueryProp != null && maxPlansPerQueryProp.Dirty) ||
                (waitStatsCaptureModeProp != null && waitStatsCaptureModeProp.Dirty) ||
                (capturePolicyExecutionCountProp != null && capturePolicyExecutionCountProp.Dirty) ||
                (capturePolicyTotalCompileCpuTimeInMSProp != null && capturePolicyTotalCompileCpuTimeInMSProp.Dirty) ||
                (capturePolicyTotalExecutionCpuTimeInMSProp != null && capturePolicyTotalExecutionCpuTimeInMSProp.Dirty) ||
                (capturePolicyStaleThresholdInHrsProp != null && capturePolicyStaleThresholdInHrsProp.Dirty))
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET QUERY_STORE (",
                    SqlBraket(this.Parent.Name)));

                // we always include operation mode
                sb.Append(string.Format(SmoApplication.DefaultCulture, "OPERATION_MODE = {0}, ",
                    queryStoreOperationModeConverter.ConvertToInvariantString(desiredState)));

                if (scriptAll || staleQueryThresholdInDaysProp.Dirty)
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture,
                        "CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = {0}), ", (long)staleQueryThresholdInDaysProp.Value));
                }

                if (scriptAll || dataFlushIntervalInSecondsProp.Dirty)
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, "DATA_FLUSH_INTERVAL_SECONDS = {0}, ",
                        (long) dataFlushIntervalInSecondsProp.Value));
                }

                if (scriptAll || statisticsCollectionIntervalInMinutesProp.Dirty)
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, "INTERVAL_LENGTH_MINUTES = {0}, ",
                        (long) statisticsCollectionIntervalInMinutesProp.Value));
                }

                if (scriptAll || maxStorageSizeInMBProp.Dirty)
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, "MAX_STORAGE_SIZE_MB = {0}, ",
                        (long) maxStorageSizeInMBProp.Value));
                }

                QueryStoreCaptureMode newValue = (QueryStoreCaptureMode) queryCaptureModeProp.Value;

                if (newValue == QueryStoreCaptureMode.Custom && !IsSupportedProperty("CapturePolicyExecutionCount", sp))
                {
                    newValue = QueryStoreCaptureMode.Auto;
                }

                if (scriptAll || queryCaptureModeProp.Dirty)
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, "QUERY_CAPTURE_MODE = {0}, ",
                        queryStoreCaptureModeTypeConverter.ConvertToInvariantString(newValue)));
                }

                if (newValue == QueryStoreCaptureMode.Custom)
                {
                    StringBuilder capturePolicy = new StringBuilder();

                    if (scriptAll || capturePolicyStaleThresholdInHrsProp.Dirty)
                    {
                        capturePolicy.Append(string.Format(SmoApplication.DefaultCulture,
                            "STALE_CAPTURE_POLICY_THRESHOLD = {0} HOURS, ",
                            (int) capturePolicyStaleThresholdInHrsProp.Value));
                    }

                    if (scriptAll || capturePolicyExecutionCountProp.Dirty)
                    {
                        capturePolicy.Append(string.Format(SmoApplication.DefaultCulture, "EXECUTION_COUNT = {0}, ",
                            (int) capturePolicyExecutionCountProp.Value));
                    }

                    if (scriptAll || capturePolicyTotalCompileCpuTimeInMSProp.Dirty)
                    {
                        capturePolicy.Append(string.Format(SmoApplication.DefaultCulture, "TOTAL_COMPILE_CPU_TIME_MS = {0}, ",
                            (long) capturePolicyTotalCompileCpuTimeInMSProp.Value));
                    }

                    if (scriptAll || capturePolicyTotalExecutionCpuTimeInMSProp.Dirty)
                    {
                        capturePolicy.Append(string.Format(SmoApplication.DefaultCulture, "TOTAL_EXECUTION_CPU_TIME_MS = {0}, ",
                            (long) capturePolicyTotalExecutionCpuTimeInMSProp.Value));
                    }

                    if (capturePolicy.Length > 0)
                    {
                        // Remove last ','
                        capturePolicy.Length -= 2;
                        sb.Append(string.Format(SmoApplication.DefaultCulture, "QUERY_CAPTURE_POLICY = ("));
                        sb.Append(capturePolicy.ToString());
                        sb.Append("), ");
                    }
                }

                if (scriptAll || sizeBasedCleanupModeProp.Dirty)
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, "SIZE_BASED_CLEANUP_MODE = {0}, ",
                        cleanupModeTypeConverter.ConvertToInvariantString(sizeBasedCleanupModeProp.Value)));
                }

                if (IsSupportedProperty("MaxPlansPerQuery", sp) && (scriptAll || maxPlansPerQueryProp.Dirty))
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, "MAX_PLANS_PER_QUERY = {0}, ",
                        (long) maxPlansPerQueryProp.Value));
                }

                if (IsSupportedProperty("WaitStatsCaptureMode", sp) && (scriptAll || waitStatsCaptureModeProp.Dirty))
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, "WAIT_STATS_CAPTURE_MODE = {0}, ",
                        waitStatsCaptureModeTypeConverter.ConvertToInvariantString(waitStatsCaptureModeProp.Value)));
                }

                // Remove last ','
                sb.Length -= 2;

                sb.Append(")");
            }
            query.Add(sb.ToString());
        }

        #region IAlterable overrides

        public void Alter()
        {
            base.AlterImpl();
        }

        #endregion //IAlterable overrides

        #region IScriptable overrides

        public StringCollection Script()
        {
            return base.ScriptImpl();
        }

        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return base.ScriptImpl(scriptingOptions);
        }

        #endregion //IScriptable overrides

        /// <summary>
        /// Clears all query store data from the owning database
        /// </summary>
        public void PurgeQueryStoreData()
        {
            try
            {
                CheckObjectState();

                this.ThrowIfNotSupported(typeof(QueryStoreOptions));

                this.Parent.ExecuteNonQuery(
                    string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET QUERY_STORE CLEAR ALL",
                    SqlBraket(this.Parent.Name)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetQueryStoreOptions, this.Parent, e);
            }
        }

        /// <summary>
        /// currently this isn't actually called, because QueryStoreOptions is not an independently scripted object. 
        /// Database would need to initialize this as a child object which implies having a setter for it along with custom code in Database.InitChildLevelRec. 
        /// Since this is a singleton object within the database it has minimal impact on the overall performance of scripting so we'll not spend effort on that type of change.
        /// </summary>
        /// <param name="parentType"></param>
        /// <param name="version"></param>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <param name="defaultTextMode"></param>
        /// <returns></returns>

        internal static string[] GetScriptFields(Type parentType,
                                  Microsoft.SqlServer.Management.Common.ServerVersion version,
                                  Common.DatabaseEngineType databaseEngineType,
                                  Common.DatabaseEngineEdition databaseEngineEdition,
                                  bool defaultTextMode)
        {
            string[] fields = 
            {
                "ActualState",
                "DataFlushIntervalInSeconds",
                "DesiredState",
                "MaxStorageSizeInMB",
                "QueryCaptureMode",
                "SizeBasedCleanupMode",
                "StaleQueryThresholdInDays",
                "StatisticsCollectionIntervalInMinutes",
                "MaxPlansPerQuery",
                "WaitStatsCaptureMode",
                "CapturePolicyExecutionCount",
                "CapturePolicyTotalCompileCpuTimeInMS",
                "CapturePolicyTotalExecutionCpuTimeInMS",
                "CapturePolicyStaleThresholdInHrs"
            };
            var list = GetSupportedScriptFields(typeof(QueryStoreOptions.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


#pragma warning restore 1591