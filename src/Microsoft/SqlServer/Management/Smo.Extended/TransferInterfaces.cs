// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// This is the exception that will be thrown by the transfer operation
    /// </summary>
    [Serializable]
    public class TransferException : SqlServerManagementException
    {
        internal TransferException() : base() {}
        internal TransferException(string message) : base(message) {}
        internal TransferException(string message, Exception innerException) : base(message, innerException) {}
#if !NETCOREAPP

        protected TransferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    public delegate void DataTransferEventHandler(object sender, DataTransferEventArgs e);

    /// <summary>
    /// Arguments passed to Transfer.DatabaseTransferEvent
    /// </summary>
    public class DataTransferEventArgs : EventArgs
    {
        internal DataTransferEventArgs(DataTransferEventType eventType, string message)
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

    /// <summary>
    /// Indicate the type of Transfer event
    /// </summary>
    public enum DataTransferEventType
    {
        Progress,
        Information,
        Warning
    }

}
