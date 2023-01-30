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

        protected SfcMetadataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
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

        protected SfcObjectNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
