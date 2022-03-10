// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;

    /// <summary>
    /// TODO
    /// </summary>
    public enum XPathExceptionCode {
        /// <summary>
        /// TODO
        /// </summary>
        Success,
        /// <summary>
        /// TODO
        /// </summary>
        UnclosedString,
        /// <summary>
        /// TODO
        /// </summary>
        TokenExpected,
        /// <summary>
        /// TODO
        /// </summary>
        NodeTestExpected,
        /// <summary>
        /// TODO
        /// </summary>
        ExpressionExpected,
        /// <summary>
        /// TODO
        /// </summary>
        NumberExpected,
        /// <summary>
        /// TODO
        /// </summary>
        BooleanExpected,
        /// <summary>
        /// TODO
        /// </summary>
        QueryExpected,
        /// <summary>
        /// TODO
        /// </summary>
        UnknownMethod,
        /// <summary>
        /// TODO
        /// </summary>
        TestExpected,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidArgument,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidNumArgs,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidName,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidNodeType,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidToken,
        /// <summary>
        /// TODO
        /// </summary>
        FunctionExpected,
        /// <summary>
        /// TODO
        /// </summary>
        NodeSetExpected,
        /// <summary>
        /// TODO
        /// </summary>
        NoXPathActive,
        /// <summary>
        /// TODO
        /// </summary>
        NotSupported,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidPattern,
        /// <summary>
        /// TODO
        /// </summary>
        BadQueryObject,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidDataRecordFilter,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidPrefix,
        /// <summary>
        /// TODO
        /// </summary>
        NoSelectedSet,
        /// <summary>
        /// TODO
        /// </summary>
        MovedFromSelection,
        /// <summary>
        /// TODO
        /// </summary>
        ConstantExpected,
        /// <summary>
        /// TODO
        /// </summary>
        InvalidVariable,
        /// <summary>
        /// TODO
        /// </summary>
        UndefinedXsltContext,
        /// <summary>
        /// TODO
        /// </summary>
        BadContext,
        /// <summary>
        /// TODO
        /// </summary>
        Last
    }

    /// <summary>
    /// exception denoting a syntax error in the xpath
    /// </summary>
    [ComVisible(false)]
    [Serializable]

    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    public class XPathException : EnumeratorException 
    {
        /// <summary>
        /// TODO
        /// </summary>
        public XPathException() 
        {
            HResult = (int)XPathExceptionCode.Success;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public XPathException(string msg) : base(msg)
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        public XPathException(string msg, Exception e) : base(msg, e)
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected XPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        
        /// <summary>
        /// TODO
        /// </summary>
        internal XPathException(XPathExceptionCode hr) {
            HResult = (int)hr;
        }

        /// <summary>
        /// TODO
        /// </summary>
        internal XPathException(XPathExceptionCode hr, string[] args) {
            _pArgs = args;
            HResult = (int)hr;
        }

        /// <summary>
        /// TODO
        /// </summary>
        internal XPathException(XPathExceptionCode hr, string arg) {
            _pArgs = new string[1];
            _pArgs[0] = arg;
            HResult = (int)hr;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public XPathExceptionCode ErrorCode 
        {
            get 
            { 
                return (XPathExceptionCode)HResult; 
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public override string Message 
        { 
            get 
            {
                string s = " (";
                if (_pArgs != null)
                {
                    foreach (string t in _pArgs)
                    {
                        s += t;
                        s += ';';
                    }
                }
                if( ';' == s[s.Length - 1] )
                {
                    s = s.Remove(s.Length - 1, 1);
                }
                s += ')';
                XPathExceptionCode hr = this.ErrorCode;
                if( hr == XPathExceptionCode.UnclosedString )
                {
                    return SfcStrings.XPathUnclosedString + s;
                }
                return SfcStrings.XPathSyntaxError + s;
            }
        }

        private string[] _pArgs;
    }
}

