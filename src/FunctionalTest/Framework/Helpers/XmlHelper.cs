// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml;
using System.Xml.XPath;
using NUnit.Framework;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Methods that assert on structure of the input XML based on an xpath
    /// </summary>
    public class XmlHelper
    {
        /// <summary>
        /// Return XML nodes matching the given xpath
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="xmlNode"></param>
        /// <param name="xmlnsManager"></param>
        /// <returns></returns>
        public static XPathNodeIterator SelectNodes(string xPath, IXPathNavigable xmlNode, XmlNamespaceManager xmlnsManager)
        {
           TraceHelper.TraceInformation("Evaluating " + xPath);

            XPathNavigator nav = xmlNode.CreateNavigator();
            XPathExpression expr = nav.Compile(xPath);
            expr.SetContext(xmlnsManager);
            XPathNodeIterator xpni = nav.Select(expr);

            return xpni;
        }

        /// <summary>
        /// Asserts that the given path returns a single node
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="xmlNode"></param>
        /// <param name="xmlnsManager"></param>
        /// <returns></returns>
        public static XPathNavigator SelectFirstAndOnlyNode(string xPath, IXPathNavigable xmlNode, XmlNamespaceManager xmlnsManager)
        {
            XPathNodeIterator xPathNodeIterator = SelectNodes(xPath, xmlNode, xmlnsManager);
            Assert.That(xPathNodeIterator.Count, Is.EqualTo(1), "There should be one result from xpath query: " + xPath);
            xPathNodeIterator.MoveNext();
           TraceHelper.TraceInformation("Value: " + xPathNodeIterator.Current.Value);
            return xPathNodeIterator.Current;
        }

        /// <summary>
        /// Asserts that the given path returns zero nodes
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="xmlNode"></param>
        /// <param name="xmlnsManager"></param>
        public static void SelectZeroNodes(string xPath, IXPathNavigable xmlNode, XmlNamespaceManager xmlnsManager)
        {
            XPathNodeIterator xPathNodeIterator = SelectNodes(xPath, xmlNode, xmlnsManager);
            Assert.That(xPathNodeIterator.Count, Is.EqualTo(0), "There should be zero results from xpath query: " + xPath);
        }
    }
}
