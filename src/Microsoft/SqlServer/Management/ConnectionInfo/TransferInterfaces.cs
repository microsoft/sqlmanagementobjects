// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Runtime.Serialization;

// VBUMP: For v17 remove unused interfaces like ITransferMetadataProvider and move the rest to Smo.Extended
namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// This interface abstracts the metadata provider - which is
    /// the component that computes the metadata needed in the transfer process
    /// </summary>
    public interface ITransferMetadataProvider
    {
        /// <summary>
        /// Save the metadata in the paths provided by the variables in the input list
        /// </summary>
        void SaveMetadata();

        /// <summary>
        /// returns the options as a collection of named variables
        /// </summary>
        /// <returns></returns>
        SortedList GetOptions();
    }

    /// <summary>
    /// This interface abstracts the data transfer mechanism
    /// </summary>
    public interface IDataTransferProvider
    {
        void Configure(ITransferMetadataProvider metadataProvider);
        void ExecuteTransfer();

        event DataTransferEventHandler TransferEvent;
    }

    /// <summary>
    /// This is the exception that will be thrown by the transfer operation
    /// </summary>
    [Serializable]
    public class TransferException : SqlServerManagementException
    {
        public TransferException() : base() {}
        public TransferException(string message) : base(message) {}
        public TransferException(string message, Exception innerException) : base(message, innerException) {}

        protected TransferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    public delegate void DataTransferEventHandler(object sender, DataTransferEventArgs e);

    public class DataTransferEventArgs : EventArgs
    {
        public DataTransferEventArgs(DataTransferEventType eventType, string message)
        {
            this.eventType = eventType;
            this.message = message;
        }

        private DataTransferEventType eventType;
        public DataTransferEventType DataTransferEventType
        {
            get { return eventType; }
        }

        private string message;
        public string Message
        {
            get { return message; }
        }

    }

    public enum DataTransferEventType
    {
        Progress,
        Information,
        Warning
    }

    public delegate bool DataTransferProgressEventHandler(object sender, Microsoft.SqlServer.Management.Common.DataTransferProgressEventArgs e);

    public enum DataTransferProgressEventType
    {
        ExecuteNonTransactableSql,
        StartTransaction,
        AllowedToFailPrologueSql,
        ExecutePrologueSql,
        GenerateDataFlow,
        ExecutingDataFlow,
        TransferringRows,
        ExecuteEpilogueSql,
        CommitTransaction,
        RollbackTransaction,
        ExecuteCompensatingSql,
        CancelQuery,
        Error
    };

    public class DataTransferProgressEventArgs : System.EventArgs
    {
        public DataTransferProgressEventArgs(DataTransferProgressEventType eventType, string transferId, long progressCount, Exception ex)
        {
            this.eventType = eventType;
            this.transferId = transferId;
            this.progressCount = progressCount;
            this.ex = ex;
        }

        public DataTransferProgressEventType DataTransferProgressEventType
        {
            get
            {
                return this.eventType;
            }
        }

        public string TransferId
        {
            get
            {
                return this.transferId;
            }
        }

        public long ProgressCount
        {
            get
            {
                return this.progressCount;
            }
        }

        public Exception Exception
        {
            get
            {
                return this.ex;
            }
        }

        private DataTransferProgressEventType eventType;
        private long progressCount;
        private string transferId;
        private Exception ex;
    }


}
