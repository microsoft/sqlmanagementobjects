// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public class ConnectionException : SystemException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionException(String message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected ConnectionException(SerializationInfo info, StreamingContext context): base(info, context)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ConnectionCannotBeChangedException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionCannotBeChangedException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionCannotBeChangedException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionCannotBeChangedException(String message, Exception innerException) : base(message, innerException)
        {
        }
        private ConnectionCannotBeChangedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class InvalidPropertyValueException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public InvalidPropertyValueException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public InvalidPropertyValueException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public InvalidPropertyValueException(String message, Exception innerException) : base(message, innerException)
        {
        }

        private InvalidPropertyValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ConnectionFailureException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionFailureException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionFailureException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ConnectionFailureException(String message, Exception innerException) : base(message, innerException)
        {
        }

        private ConnectionFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ExecutionFailureException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public ExecutionFailureException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ExecutionFailureException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ExecutionFailureException(String message, Exception innerException) : base(message, innerException)
        {
        }

        private ExecutionFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class NotInTransactionException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public NotInTransactionException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public NotInTransactionException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public NotInTransactionException(String message, Exception innerException) : base(message, innerException)
        {
        }

        private NotInTransactionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class InvalidArgumentException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public InvalidArgumentException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public InvalidArgumentException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public InvalidArgumentException(String message, Exception innerException) : base(message, innerException)
        {
        }

        private InvalidArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class PropertyNotSetException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public PropertyNotSetException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public PropertyNotSetException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public PropertyNotSetException(String message, Exception innerException) : base(message, innerException)
        {
        }

        private PropertyNotSetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class PropertyNotAvailableException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public PropertyNotAvailableException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public PropertyNotAvailableException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public PropertyNotAvailableException(String message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="si"></param>
        /// <param name="sc"></param>
        private PropertyNotAvailableException(SerializationInfo si, StreamingContext sc)
        {
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ChangePasswordFailureException : ConnectionException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public ChangePasswordFailureException()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ChangePasswordFailureException(String message) : base(message)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ChangePasswordFailureException(String message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="si"></param>
        /// <param name="sc"></param>
        private ChangePasswordFailureException(SerializationInfo si, StreamingContext sc) : base(si,sc)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when an attempt is made to use a connected that is forced in disconnected mode
    /// </summary>
    [Serializable]
    public sealed class DisconnectedConnectionException : ConnectionException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DisconnectedConnectionException()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DisconnectedConnectionException(String message) : base(message)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DisconnectedConnectionException(String message, Exception innerException) : base(message, innerException)
        {
        }

        private DisconnectedConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


}
