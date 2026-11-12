// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using NUnit.Framework;
using VSUT = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// SMO scripting QueryStore TestSuite.
    /// </summary>
    [VSUT.TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public partial class QueryStore_SmoTestSuite : SqlTestBase
    {
        /// <summary>
        /// Script Query Store settings.
        /// </summary>
        [VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void VerifyScriptingQueryStoreOptions()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    Assert.Multiple(() =>
                    {
                        //The Error mode isn't something we can set - it's only set by the engine as a possible value of ActualState
                        foreach (
                            QueryStoreOperationMode operationMode in
                                Enum.GetValues(typeof (QueryStoreOperationMode))
                                    .Cast<QueryStoreOperationMode>()
                                    .Except(new[] { QueryStoreOperationMode.Off, QueryStoreOperationMode.Error }))
                        {
                            SetAndValidateOption(database,
                                typeof (QueryStoreOptions).GetProperty("DesiredState"),
                                operationMode,
                                GenerateExpectedAlterScript("OPERATION_MODE", GetOperationModeString(operationMode)));
                        }

                        database.QueryStoreOptions.DesiredState = QueryStoreOperationMode.ReadWrite;
                        database.Alter();

                        foreach (QueryStoreCaptureMode captureMode in
                            Enum.GetValues(typeof(QueryStoreCaptureMode))
                                .Cast<QueryStoreCaptureMode>()
                                .Except(new[] { QueryStoreCaptureMode.Custom }))
                        {
                            SetAndValidateOption(database,
                                typeof(QueryStoreOptions).GetProperty("QueryCaptureMode"),
                                captureMode,
                                GenerateExpectedAlterScript("QUERY_CAPTURE_MODE", GetQueryCaptureModeString(captureMode)));
                        }

                        // Change Stale Query Threshold
                        const long expectedStaleQueryThreshold = 200;
                        SetAndValidateOption(database,
                            typeof (QueryStoreOptions).GetProperty("StaleQueryThresholdInDays"),
                            expectedStaleQueryThreshold,
                            GenerateExpectedAlterScript("STALE_QUERY_THRESHOLD_DAYS", expectedStaleQueryThreshold.ToString()));

                        // Change Data Flush Intervals
                        const long expectedDataFlushInterval = 300;
                        SetAndValidateOption(database,
                            typeof (QueryStoreOptions).GetProperty("DataFlushIntervalInSeconds"),
                            expectedDataFlushInterval,
                            GenerateExpectedAlterScript("DATA_FLUSH_INTERVAL_SECONDS", expectedDataFlushInterval.ToString()));

                        // Change Collection Interval Length
                        const long expectedIntervalLength = 30;
                        SetAndValidateOption(database,
                            typeof (QueryStoreOptions).GetProperty("StatisticsCollectionIntervalInMinutes"),
                            expectedIntervalLength,
                            GenerateExpectedAlterScript("INTERVAL_LENGTH_MINUTES", expectedIntervalLength.ToString()));

                        // Change Storage Size
                        const long expectedMaxStorageSize = 200;
                        SetAndValidateOption(database,
                            typeof (QueryStoreOptions).GetProperty("MaxStorageSizeInMB"),
                            expectedMaxStorageSize,
                            GenerateExpectedAlterScript("MAX_STORAGE_SIZE_MB", expectedMaxStorageSize.ToString()));

                        // Change Size Based Cleanup Mode
                        foreach (
                            QueryStoreSizeBasedCleanupMode cleanupMode in
                                Enum.GetValues(typeof (QueryStoreSizeBasedCleanupMode)))
                        {
                            SetAndValidateOption(database,
                                typeof (QueryStoreOptions).GetProperty("SizeBasedCleanupMode"),
                                cleanupMode,
                                GenerateExpectedAlterScript("SIZE_BASED_CLEANUP_MODE", GetQuerySizeBasedCleanupModeString(cleanupMode)));
                        }

                        // Change Max Plans Per Query
                        const long expectedMaxPlansPerQuery = 20000;
                        SetAndValidateOption(database,
                            typeof(QueryStoreOptions).GetProperty("MaxPlansPerQuery"),
                            expectedMaxPlansPerQuery,
                            GenerateExpectedAlterScript("MAX_PLANS_PER_QUERY", expectedMaxPlansPerQuery.ToString()));

                        if (ServerContext.VersionMajor >= 14)
                        {
                            // Change WaitStatsCaptureMode
                            foreach (
                                QueryStoreWaitStatsCaptureMode waitStatsCaptureMode in
                                    Enum.GetValues(typeof(QueryStoreWaitStatsCaptureMode)))
                            {
                                SetAndValidateOption(database,
                                    typeof(QueryStoreOptions).GetProperty("WaitStatsCaptureMode"),
                                    waitStatsCaptureMode,
                                    GenerateExpectedAlterScript("WAIT_STATS_CAPTURE_MODE", GetQueryWaitstatsCaptureModeString(waitStatsCaptureMode)));
                            }
                        }
                    });
                });
        }

        /// <summary>
        /// Test script Query Store settings for version >=15
        /// </summary>
        [VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void VerifyScriptingQueryStoreOptionsV15()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    Assert.Multiple(() =>
                    {
                        // update database desired state to read_write
                        database.QueryStoreOptions.DesiredState = QueryStoreOperationMode.ReadWrite;

                        // update database Query Capture Mode to Custom
                        database.QueryStoreOptions.QueryCaptureMode = QueryStoreCaptureMode.Custom;

                        // Change CapturePolicyStaleThresholdInHrs
                        const int expectedCapturePolicyStaleThresholdInHrs = 10;
                        SetAndValidateOption(database,
                            typeof(QueryStoreOptions).GetProperty("CapturePolicyStaleThresholdInHrs"),
                            expectedCapturePolicyStaleThresholdInHrs,
                            GenerateExpectedAlterScript("STALE_CAPTURE_POLICY_THRESHOLD", expectedCapturePolicyStaleThresholdInHrs.ToString()));

                        // Change CapturePolicyExecutionCount
                        const int expectedCapturePolicyExecutionCount = 3000;
                        SetAndValidateOption(database,
                            typeof(QueryStoreOptions).GetProperty("CapturePolicyExecutionCount"),
                            expectedCapturePolicyExecutionCount,
                            GenerateExpectedAlterScript("EXECUTION_COUNT", expectedCapturePolicyExecutionCount.ToString()));

                        // Change CapturePolicyTotalCompileCpuTime
                        const long expectedCapturePolicyTotalCompileCpuTime = 20000;
                        SetAndValidateOption(database,
                            typeof(QueryStoreOptions).GetProperty("CapturePolicyTotalCompileCpuTimeInMS"),
                            expectedCapturePolicyTotalCompileCpuTime,
                            GenerateExpectedAlterScript("TOTAL_COMPILE_CPU_TIME_MS", expectedCapturePolicyTotalCompileCpuTime.ToString()));

                        // Change CapturePolicyTotalExecutionCpuTime
                        const long expectedCapturePolicyTotalExecutionCpuTime = 20000;
                        SetAndValidateOption(database,
                            typeof(QueryStoreOptions).GetProperty("CapturePolicyTotalExecutionCpuTimeInMS"),
                            expectedCapturePolicyTotalExecutionCpuTime,
                            GenerateExpectedAlterScript("TOTAL_EXECUTION_CPU_TIME_MS", expectedCapturePolicyTotalExecutionCpuTime.ToString()));
                    });
                });
        }

        #region Helpers

        /// <summary>
        /// Helper method that will set the specified Query Store option to the
        /// given value and then verify that the generated script is correct.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="prop"></param>
        /// <param name="val"></param>
        /// <param name="expectedAlterScript"></param>
        private void SetAndValidateOption(Database db,
            PropertyInfo prop,
            object val,
            string expectedAlterScript)
        {
            TraceHelper.TraceInformation("Setting option '{0}' to '{1}'", prop.Name, val);
            prop.SetValue(db.QueryStoreOptions, val);
            db.Alter();
            //Refresh the options so we're sure to pick up the actual values from the DB
            db.QueryStoreOptions.Refresh();

            Assert.That(db.Script().ToSingleString(), Contains.Substring(expectedAlterScript), "Couldn't find expected script after setting '{0}' property", prop.Name);
        }

        /// <summary>
        /// Generates the expected query store options alter script
        /// based on the options provided and database version
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GenerateExpectedAlterScript(string propertyName, string value)
        {
            return string.Format(CultureInfo.InvariantCulture, @"{0} = {1}", propertyName, value);
        }

        private string GetOperationModeString(QueryStoreOperationMode mode)
        {
            switch(mode)
            {
                case QueryStoreOperationMode.Off: return "OFF";
                case QueryStoreOperationMode.ReadOnly: return "READ_ONLY";
                case QueryStoreOperationMode.ReadWrite: return "READ_WRITE";
                default: return string.Empty;
            }
        }

        private string GetQueryCaptureModeString(QueryStoreCaptureMode mode)
        {
            switch (mode)
            {
                case QueryStoreCaptureMode.All: return "ALL";
                case QueryStoreCaptureMode.Auto: return "AUTO";
                case QueryStoreCaptureMode.None: return "NONE";
                case QueryStoreCaptureMode.Custom: return "CUSTOM";
                default: return string.Empty;
            }
        }

        private string GetQuerySizeBasedCleanupModeString(QueryStoreSizeBasedCleanupMode mode)
        {
            switch (mode)
            {
                case QueryStoreSizeBasedCleanupMode.Off: return "OFF";
                case QueryStoreSizeBasedCleanupMode.Auto: return "AUTO";
                default: return string.Empty;
            }
        }

        private string GetQueryWaitstatsCaptureModeString(QueryStoreWaitStatsCaptureMode mode)
        {
            switch (mode)
            {
                case QueryStoreWaitStatsCaptureMode.Off: return "OFF";
                case QueryStoreWaitStatsCaptureMode.On: return "ON";
                default: return string.Empty;
            }
        }

        #endregion Helpers
    }
}