// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Interface of adapter that serializes and deserializes object of a some non SFC type for SfcSerializer.
    /// </summary>
    public interface IXmlSerializationAdapter
    {
        /// <summary>
        /// Deserializes object from its xml representation (which will a valid xml). Implementation of this interface should unescape
        /// invalid xml characters that were escaped in WriteXml(), using SfcSecureString.XmlUnEscape()
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="deserializedObject"></param>
        void ReadXml(XmlReader reader, out object deserializedObject);

        /// <summary>
        /// Serialize an object to its xml representation. Implementation of this interface should:
        /// 1. produce a valid xml document(not just a fragment), just as XmlSerializer does.
        /// 2. escape invalid xml characters in the string representation of the given object, if any, using SfcSecureString.XmlEscape()
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="objectToSerialize"></param>
        void WriteXml(XmlWriter writer, object objectToSerialize);
    }
}
