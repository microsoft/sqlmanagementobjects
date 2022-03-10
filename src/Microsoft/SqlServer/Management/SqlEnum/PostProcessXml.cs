// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System.Collections.Specialized;
    using System.Xml;

    /// <summary>
    /// Given a Xml string of the format <root><row a="x"/><row a="y"/></root>
    /// return a List containing the strings "x" and "y"
    /// </summary>
    internal class PostProcessXmlToList : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            StringCollection result = new StringCollection();
            string xml = GetTriggeredString(dp, 0);
            if (string.IsNullOrEmpty(xml))
            {
                return result;
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode root = doc.ChildNodes[0];
            if (null != root)
            {
                foreach (XmlNode node in root.ChildNodes)
                {
                    // processing row
                    XmlAttribute a = node.Attributes[0];
                    if (null != a)
                    {
                        result.Add(a.Value);
                    }
                }
            }
            return result;
        }
    }
}