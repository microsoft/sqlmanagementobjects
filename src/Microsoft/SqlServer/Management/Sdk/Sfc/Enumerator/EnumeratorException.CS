// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;

    /// <summary>
    /// base class for all enumerator exceptions
    /// </summary>
    [ComVisible(false)]
    [Serializable]

    public class EnumeratorException : Microsoft.SqlServer.Management.Common.SqlServerManagementException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public EnumeratorException()
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        public EnumeratorException(String message) : base(message)
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        public EnumeratorException(String message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected EnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    
        /// <summary>
        /// TODO
        /// </summary>
        internal static void FilterException(Exception e)
        {
            if (e is OutOfMemoryException)
            {
                throw e;
            }
        }
    }

    /// <summary>
    /// thrown when the XPATH expression is invalid
    /// </summary>
    [Serializable]

    public sealed class InvalidQueryExpressionEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public InvalidQueryExpressionEnumeratorException()
        {
            this.HResult = 1;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidQueryExpressionEnumeratorException(String message) : base(message)
        {
            this.HResult = 1;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidQueryExpressionEnumeratorException(String message, Exception innerException) : base(message, innerException)
        {
            this.HResult = 1;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private InvalidQueryExpressionEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 1;
        }
    }

    /// <summary>
    /// thrown when an error is encountered reading the configuration file
    /// </summary>
    [Serializable]

    public sealed class InvalidConfigurationFileEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public InvalidConfigurationFileEnumeratorException()
        {
            this.HResult = 2;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidConfigurationFileEnumeratorException(String message) : base(message)
        {
            this.HResult = 2;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidConfigurationFileEnumeratorException(String message, Exception innerException) : base(message, innerException)
        {
            this.HResult = 2;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private InvalidConfigurationFileEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 2;
        }
    }

    /// <summary>
    /// thrown when a unknown property is requested
    /// </summary>
    [Serializable]

    public sealed class UnknownPropertyEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public UnknownPropertyEnumeratorException()
        {
            this.HResult = 3;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public UnknownPropertyEnumeratorException(String propertyName) : base(SfcStrings.UnknownProperty(propertyName))
        {
            this.HResult = 3;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public UnknownPropertyEnumeratorException(String propertyName, Exception innerException) : base(SfcStrings.UnknownProperty(propertyName), innerException)
        {
            this.HResult = 3;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private UnknownPropertyEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 3;
        }
    }

    /// <summary>
    /// thrown whe an unknown level is used
    /// </summary>
    [Serializable]

    public sealed class UnknownTypeEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public UnknownTypeEnumeratorException()
        {
            this.HResult = 4;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public UnknownTypeEnumeratorException(String typeName) : base(SfcStrings.UnknownType(typeName))
        {
            this.HResult = 4;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public UnknownTypeEnumeratorException(String typeName, Exception innerException) : base(SfcStrings.UnknownType(typeName), innerException)
        {
            this.HResult = 4;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private UnknownTypeEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 4;
        }
    }

    /// <summary>
    /// thown when an unsuported <see>ResultType</see> is requested
    /// </summary>
    [Serializable]
    
    public sealed class ResultTypeNotSupportedEnumeratorException : EnumeratorException
    {
        String m_type;

        /// <summary>
        /// TODO
        /// </summary>
        public ResultTypeNotSupportedEnumeratorException()
        {
            this.HResult = 5;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public ResultTypeNotSupportedEnumeratorException(string msg) : base(msg)
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        public ResultTypeNotSupportedEnumeratorException(string msg, Exception e) : base(msg, e)
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        public ResultTypeNotSupportedEnumeratorException(ResultType type) : base(SfcStrings.ResultNotSupported)
        {
            this.HResult = 5;
            m_type = type.ToString();
        }

        /// <summary>
        /// TODO
        /// </summary>
        public ResultTypeNotSupportedEnumeratorException(ResultType type, Exception innerException) : base(SfcStrings.ResultNotSupported, innerException)
        {
            this.HResult = 5;
            m_type = type.ToString();
        }

        /// <summary>
        /// TODO
        /// </summary>
        private ResultTypeNotSupportedEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 5;
            m_type = info.GetString("m_type");
        }
        
        /// <summary>
        /// TODO
        /// </summary>
        public String ResultType
        {
            get
            {
                return m_type;
            }
        }
    }

    /// <summary>
    /// internal exception - fatal error
    /// </summary>
    [Serializable]

    public sealed class InternalEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public InternalEnumeratorException()
        {
            this.HResult = 7;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InternalEnumeratorException(String message) : base(message)
        {
            this.HResult = 7;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InternalEnumeratorException(String message, Exception innerException) : base(message, innerException)
        {
            this.HResult = 7;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private InternalEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 7;
        }
    }

    /// <summary>
    /// a property has been used in a place which is not covered by its Usage property
    /// </summary>
    [Serializable]

    public sealed class InvalidPropertyUsageEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public InvalidPropertyUsageEnumeratorException()
        {
            this.HResult = 8;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidPropertyUsageEnumeratorException(String message) : base(message)
        {
            this.HResult = 8;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidPropertyUsageEnumeratorException(String message, Exception innerException) : base(message, innerException)
        {
            this.HResult = 8;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private InvalidPropertyUsageEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 8;
        }
        
        /// <summary>
        /// TODO
        /// </summary>
        static internal void Throw(String propertyName, ObjectPropertyUsages usage_not_resolved)
        {
            String sUsageInfo = String.Empty;
            if( 0 != ( ObjectPropertyUsages.Request & usage_not_resolved ) )
            {
                sUsageInfo = SfcStrings.UsageRequest + " ";
            }
            if( 0 != ( ObjectPropertyUsages.Filter & usage_not_resolved ) )
            {
                sUsageInfo += SfcStrings.UsageFilter + " ";
            }
            if( 0 != ( ObjectPropertyUsages.OrderBy & usage_not_resolved ) )
            {
                sUsageInfo += SfcStrings.UsageOrderBy + " ";
            }
            throw new InvalidPropertyUsageEnumeratorException(SfcStrings.PropertyUsageError(propertyName, sUsageInfo));
        }
    }

    /// <summary>
    /// a valid query but using features that are unsupported has been used
    /// </summary>
    [Serializable]

    public sealed class QueryNotSupportedEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public QueryNotSupportedEnumeratorException()
        {
            this.HResult = 9;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public QueryNotSupportedEnumeratorException(String message) : base(message)
        {
            this.HResult = 9;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public QueryNotSupportedEnumeratorException(String message, Exception innerException) : base(message, innerException)
        {
            this.HResult = 9;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private QueryNotSupportedEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 9;
        }
        
    }

    /// <summary>
    /// a level that is not supported on a particular version has been used
    /// </summary>
    [Serializable]

    public sealed class InvalidVersionEnumeratorException : EnumeratorException
    {
        /// <summary>
        /// TODO
        /// </summary>
        public InvalidVersionEnumeratorException()
        {
            this.HResult = 10;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidVersionEnumeratorException(String message) : base(message)
        {
            this.HResult = 10;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public InvalidVersionEnumeratorException(String message, Exception innerException) : base(message, innerException)
        {
            this.HResult = 10;
        }

        /// <summary>
        /// TODO
        /// </summary>
        private InvalidVersionEnumeratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.HResult = 10;
        }
    }
}
