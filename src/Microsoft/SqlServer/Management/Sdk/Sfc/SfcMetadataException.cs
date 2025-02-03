// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    [Serializable]

    public class SfcMetadataException : SfcException
    {
        public SfcMetadataException()
            : base()
        {
        }
        public SfcMetadataException(string message)
            : base(message)
        {
        }
        public SfcMetadataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
#if !NETCOREAPP
        protected SfcMetadataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
namespace Microsoft.SqlServer.Management.Sdk.Sfc.Metadata
{
    [Serializable]

    public class SfcObjectNotFoundException : SfcException
    {
        public SfcObjectNotFoundException()
            : base()
        {
        }
        public SfcObjectNotFoundException(string message)
            : base(message)
        {
        }
        public SfcObjectNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
#if !NETCOREAPP
        protected SfcObjectNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
