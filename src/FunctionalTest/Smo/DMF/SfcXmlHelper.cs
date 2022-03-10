// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Xml;

namespace Microsoft.SqlServer.Test.SMO.DMF
{
    /// <summary>
    /// Helper methods for SFC XML functionality
    /// </summary>
    public static class SfcXmlHelper
    {
        // Uris for schema namespaces in the DMF xml
        private static string DmfUri = "http://schemas.microsoft.com/sqlserver/DMF/2007/08";
        private static string SfcUri = "http://schemas.microsoft.com/sqlserver/sfc/serialization/2007/08";
        private static string SmlUri = "http://schemas.serviceml.org/sml/2007/02";
        private static string SmlifUri = "http://schemas.serviceml.org/smlif/2007/02";
        private static string XsUri = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// The namespace manager is required for the XPathNavigator to find XML elements that are in a namespace.
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public static XmlNamespaceManager GetXmlNsManager(XmlDocument xmlDoc)
        {
            // The namespace prefix defined here does not have to be the same as the prefix that is actually used in the file
            // For smlif we will use 's' because it makes the Xpath queries shorter and easier to read
            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            xmlnsManager.AddNamespace("dmf", SfcXmlHelper.DmfUri);
            xmlnsManager.AddNamespace("DMF", SfcXmlHelper.DmfUri);
            xmlnsManager.AddNamespace("sfc", SfcXmlHelper.SfcUri);
            xmlnsManager.AddNamespace("sml", SfcXmlHelper.SmlUri);
            xmlnsManager.AddNamespace("s", SfcXmlHelper.SmlifUri);
            xmlnsManager.AddNamespace("xs", SfcXmlHelper.XsUri);

            return xmlnsManager;
        }
    }
}
