// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.Common
{

    /// <summary>
    /// ConnectionException is the base class for most ConnectionInfo exceptions
    /// </summary>
    [Serializable]
    public class ConnectionException : Exception
    {
        
        /// <summary>
        ///
        /// </summary>
        public ConnectionException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ConnectionException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ConnectionException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        /// <summary>
        ///
        /// </summary>
        protected ConnectionException(SerializationInfo info, StreamingContext context): base(info, context)
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class ConnectionCannotBeChangedException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public ConnectionCannotBeChangedException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ConnectionCannotBeChangedException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ConnectionCannotBeChangedException(String message, Exception innerException) : base(message, innerException)
        {
        }
#if !NETCOREAPP
        private ConnectionCannotBeChangedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class InvalidPropertyValueException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public InvalidPropertyValueException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public InvalidPropertyValueException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public InvalidPropertyValueException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        private InvalidPropertyValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class ConnectionFailureException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public ConnectionFailureException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ConnectionFailureException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ConnectionFailureException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        private ConnectionFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class ExecutionFailureException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public ExecutionFailureException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ExecutionFailureException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ExecutionFailureException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        private ExecutionFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class NotInTransactionException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public NotInTransactionException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public NotInTransactionException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public NotInTransactionException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        private NotInTransactionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class InvalidArgumentException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public InvalidArgumentException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public InvalidArgumentException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public InvalidArgumentException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        private InvalidArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class PropertyNotSetException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public PropertyNotSetException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public PropertyNotSetException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public PropertyNotSetException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        private PropertyNotSetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class PropertyNotAvailableException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public PropertyNotAvailableException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public PropertyNotAvailableException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public PropertyNotAvailableException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="si"></param>
        /// <param name="sc"></param>
        private PropertyNotAvailableException(SerializationInfo si, StreamingContext sc) : base(si, sc) 
        {
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class ChangePasswordFailureException : ConnectionException
    {
        /// <summary>
        ///
        /// </summary>
        public ChangePasswordFailureException()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ChangePasswordFailureException(String message) : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ChangePasswordFailureException(String message, Exception innerException) : base(message, innerException)
        {
        }

#if !NETCOREAPP
        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="si"></param>
        /// <param name="sc"></param>
        private ChangePasswordFailureException(SerializationInfo si, StreamingContext sc) : base(si,sc)
        {
        }
#endif
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

#if !NETCOREAPP
        private DisconnectedConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }


}
