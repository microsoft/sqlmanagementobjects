using System;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// Summary description for RegisteredServerException.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1058:TypesShouldNotExtendCertainBaseTypes", MessageId = "System.ApplicationException")]
    public class RegisteredServerException : ApplicationException
    {
        /// <summary>
        /// An exception in a RegisteredServer object
        /// </summary>
        /// <param name="message"></param>
        /// <param name="previous"></param>
        public RegisteredServerException(string message, Exception previous) : base(message, previous)
        {
        }

        /// <summary>
        /// An exception in a RegisteredServer object
        /// </summary>
        /// <param name="message"></param>
        public RegisteredServerException(string message) : base(message)
        {
        }

        /// <summary>
        /// An exception in a RegisteredServer object
        /// </summary>
        public RegisteredServerException() : base()
        {
        }

#if !NETCOREAPP
        /// <summary>
        /// An exception in a RegisteredServer object
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected RegisteredServerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
