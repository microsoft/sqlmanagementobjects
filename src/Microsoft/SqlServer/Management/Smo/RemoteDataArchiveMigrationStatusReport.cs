// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Status report for each batch of rows migrated to Remote Data Archive
    /// </summary>
    public class RemoteDataArchiveMigrationStatusReport
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="databaseName">The ID of the database from which rows were migrated.</param>
        /// <param name="tableName">The ID of the table from which rows were migrated.</param>
        /// <param name="migratedRows">The number of rows migrated in this batch.</param>
        /// <param name="statusReportStartTimeInUtc">The UTC time at which the batch started.</param>
        /// <param name="statusReportEndTimeInUtc">The UTC time at which the batch finished.</param>
        internal RemoteDataArchiveMigrationStatusReport(string databaseName, 
                                                        string tableName, 
                                                        long migratedRows,                                     
                                                        DateTime statusReportStartTimeInUtc, 
                                                        DateTime statusReportEndTimeInUtc)
        {
            this.DatabaseName = databaseName;
            this.TableName = tableName;
            this.MigratedRows = migratedRows;
            this.StatusReportStartTimeInUtc = statusReportStartTimeInUtc;
            this.StatusReportEndTimeInUtc = statusReportEndTimeInUtc;
            this.ErrorNumber = 0;
            this.ErrorSeverity = 0;
            this.ErrorState = 0;
            this.Details = string.Empty;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="databaseName">The ID of the database from which rows were migrated.</param>
        /// <param name="tableName">The ID of the table from which rows were migrated.</param>
        /// <param name="migratedRows">The number of rows migrated in this batch.</param>
        /// <param name="statusReportStartTimeInUtc">The UTC time at which the batch started.</param>
        /// <param name="statusReportEndTimeInUtc">The UTC time at which the batch finished.</param>
        /// <param name="errorNumber">If the batch fails, the error number of the error that occurred; otherwise, null.</param>
        /// <param name="errorSeverity">If the batch fails, the severity of the error that occurred; otherwise, null.</param>
        /// <param name="errorState">If the batch fails, the state of the error that occurred; otherwise, null.</param>
        /// <param name="details">Details of the error message.</param>
        internal RemoteDataArchiveMigrationStatusReport(string databaseName,
                                                        string tableName, 
                                                        long migratedRows,
                                                        DateTime statusReportStartTimeInUtc,
                                                        DateTime statusReportEndTimeInUtc,                                  
                                                        int? errorNumber,
                                                        int? errorSeverity,
                                                        int? errorState,
                                                        string details)
        {
            this.DatabaseName = databaseName;
            this.TableName = tableName;
            this.MigratedRows = migratedRows;
            this.StatusReportStartTimeInUtc = statusReportStartTimeInUtc;
            this.StatusReportEndTimeInUtc = statusReportEndTimeInUtc;
            this.ErrorNumber = errorNumber.HasValue ? errorNumber.Value : 0;
            this.ErrorSeverity = errorSeverity.HasValue ? errorSeverity.Value : 0;
            this.ErrorState = errorState.HasValue ? errorState.Value : 0;
            this.Details = (details == null) ? string.Empty : details;
        }

        /// <summary>
        /// The name of the database from which rows were migrated.
        /// </summary>
        public string DatabaseName
        {
            get;
            private set;
        }

        /// <summary>
        /// The name of the table from which rows were migrated.
        /// </summary>
        public string TableName
        {
            get;
            private set;
        }

        /// <summary>
        /// The number of rows migrated in this batch.
        /// </summary>
        public long MigratedRows
        {
            get;
            private set;
        }

        /// <summary>
        /// The UTC time at which the batch started.
        /// </summary>
        public DateTime StatusReportStartTimeInUtc
        {
            get;
            private set;
        }

        /// <summary>
        /// The UTC time at which the batch finished.
        /// </summary>
        public DateTime StatusReportEndTimeInUtc
        {
            get;
            private set;
        }

        /// <summary>
        /// If the batch fails, the error number of the error that occurred; otherwise, 0.
        /// </summary>
        public int ErrorNumber
        {
            get;
            private set;
        }

        /// <summary>
        /// If the batch fails, the severity of the error that occurred; otherwise, 0.
        /// </summary>
        public int ErrorSeverity
        {
            get;
            private set;
        }

        /// <summary>
        /// If the batch fails, the state of the error that occurred; otherwise, 0.
        /// The error state indicates the condition or location where the error occurred.
        /// </summary>
        public int ErrorState
        {
            get;
            private set;
        }

        /// <summary>
        /// Details of the error message.
        /// </summary>
        public string Details
        {
            get;
            private set;
        }
    }
}
