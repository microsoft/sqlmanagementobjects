// Copyright (c) Microsoft.
// Licensed under the MIT license.

// PURPOSE: This is the base class for all of the Predicate expression. Three
// kinds of expressions exist: compare expression, function expression and logical
// expression.

using System.Globalization;

using System.Text;
using System.Xml;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Base class for all of the predicate expression.
    /// </summary>
    public abstract class PredExpr: Predicate
    {
        // Here's an example of predicateXml
        // <and><leaf>
        //          <comparator name="equal_i_sql_unicode_string" package="sqlserver"></comparator>
        //          <global name="client_app_name" package="sqlserver"></global>
        //          <value><![CDATA[profiler]]></value></leaf>
        //      <leaf>
        //          <comparator name="equal_uint64" package="package0"></comparator>
        //          <global name="database_id" package="sqlserver"></global>
        //          <value>7</value>
        // </leaf></and>
        internal static PredExpr ParsePredicateXml(BaseXEStore store, string predicateXml)
        {
            if (predicateXml == null || predicateXml.Trim() == string.Empty)
            {
                return null;
            }

            xeStore = store;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(predicateXml);

            XmlElement root = (XmlElement)doc.FirstChild;
            return RecursiveTraverse(root);
        }

        private static PredExpr RecursiveTraverse(XmlElement element)
        {
            if (element.Name == "leaf")
            {
                return ParseLeaf(element);
            }
            else
            {
                PredExpr expr1 = RecursiveTraverse((XmlElement)element.FirstChild);
                if (element.Name == "not")
                {
                    return new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Not, expr1, null);
                }
                PredExpr expr2 = RecursiveTraverse((XmlElement)element.ChildNodes[1]);
                if (element.Name == "and")
                {
                    return new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.And, expr1, expr2);
                }
                return new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Or, expr1, expr2);
            }
        }

        private static PredExpr ParseLeaf(XmlElement leaf)
        {
            XmlElement comparatorElem = (XmlElement)leaf.FirstChild;
            string pkgName = comparatorElem.Attributes["package"].Value;
            string objName =  comparatorElem.Attributes["name"].Value;
            PredCompareInfo pci = xeStore.ObjectInfoSet.Get<PredCompareInfo>(pkgName, objName);

            XmlElement operandElem = (XmlElement)leaf.ChildNodes[1];
            pkgName = operandElem.Attributes["package"].Value;
            objName = operandElem.Attributes["name"].Value;
            PredOperand operand = null;
            if (operandElem.Name == "event")
            {
                string eventField = operandElem.Attributes["field"].Value;
                operand = new PredOperand(xeStore.ObjectInfoSet.Get<EventInfo>(pkgName, objName).DataEventColumnInfoSet[eventField]);
            }
            else
            {
                operand = new PredOperand(xeStore.ObjectInfoSet.Get<PredSourceInfo>(pkgName, objName));
            }

            PredValue value = null;
            XmlElement valueElem = (XmlElement)leaf.ChildNodes[2];
            string valueText = valueElem.InnerText;
            value = new PredValue(valueText); // when parsing, always create as a string

            return new PredFunctionExpr(pci, operand, value);
        }

        private static BaseXEStore xeStore;
    }
}