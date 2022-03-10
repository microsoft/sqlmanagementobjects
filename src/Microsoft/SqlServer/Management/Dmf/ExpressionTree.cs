// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Diagnostics.STrace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Globalization;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Internal;
using SFC = Microsoft.SqlServer.Management.Sdk.Sfc;
#if MICROSOFTDATA
using ADO = Microsoft.Data.SqlClient;
#else
using ADO = System.Data.SqlClient;
#endif

using Microsoft.SqlServer.Management.Facets;


/*
 * To add new supported datatype:
 *  Add type definition to ExpressionNode.supportedTypes (in static ctor)
 *  Add new case to ExpressionNode.ConvertFromString
 *  Make sure ExpressionNodeConstant.ToString generates correct representation for the type
 *  Add new case to ExpressionNodeConstant.EqualProperties
 *  Make sure ExpressionNodeConstant serialization methods handle the type correctly
 *  Update ExpressionNodeOperator.SupportedOperators ot return an appropriate set for the type
 *  Update ExpressionNodeOperator.EvaluateObjectPair to handle the type
 *  Add new EvaluateSimple method if needed
 */


namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Enumerates operators used in Name Conditions (expression: @Name operator 'object_name')
    /// None used for all other expressions
    /// </summary>
    public enum NameConditionType
    {
        ///
        None = 0,
        ///
        Equal = 1,
        ///
        Like = 2,
        ///
        NotEqual = 3,
        ///
        NotLike = 4
    }

    /// <summary>
    /// Types of nodes in Expression Tree
    /// These are used for identification, not for creation of nodes
    /// </summary>
    public enum ExpressionNodeType
    {
        /// <summary>
        /// Base Type - for initialization only
        /// </summary>
        Base,
        /// <summary>
        /// Constant
        /// </summary>
        Constant,
        /// <summary>
        /// Attribute - Management Facet property
        /// </summary>
        Attribute,
        /// <summary>
        /// Operator - predefined boolean function with 2 arguments
        /// </summary>
        Operator,
        /// <summary>
        /// Function
        /// </summary>
        Function,
        /// <summary>
        /// Group - node enclosed in parentheses
        /// </summary>
        Group
    }

    internal sealed class AttributeOperatorPair
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "AttributeOperatorPair");
        string attribute;
        internal string Attribute { get { return this.attribute; } }

        OperatorType opType;
        internal OperatorType OpType { get { return this.opType; } }

        public AttributeOperatorPair(string attribute, OperatorType opType)
        {
            traceContext.TraceMethodEnter("AttributeOperatorPair");
            // Tracing Input Parameters
            traceContext.TraceParameters(attribute, opType);
            this.attribute = attribute;
            this.opType = opType;
            traceContext.TraceMethodExit("AttributeOperatorPair");
        }
    }

    /// <summary>
    /// Class representing configuration items - actions needed 
    /// to configure a target to meet a certain condition.
    /// </summary>
    internal sealed class ConfigurationItem
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ConfigurationItem");
        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="desiredValue"></param>
        internal ConfigurationItem(string property, object desiredValue)
        {
            traceContext.TraceMethodEnter("ConfigurationItem");
            // Tracing Input Parameters
            traceContext.TraceParameters(property, desiredValue);
            this.property = property;
            this.desiredValue = desiredValue;
            traceContext.TraceMethodExit("ConfigurationItem");
        }

        string property;
        /// <summary>
        /// 
        /// </summary>
        internal string Property
        {
            get { return property; }
            set
            {
                traceContext.TraceVerbose("Setting Property to: {0}", value);
                property = value;
            }
        }

        object desiredValue;
        /// <summary>
        /// 
        /// </summary>
        internal object DesiredValue
        {
            get { return desiredValue; }
            set
            {
                traceContext.TraceVerbose("Setting DesiredValue to: {0}", value);
                desiredValue = value;
            }
        }
    }

    internal static class TypeExtensions
    {
        public static bool IsBitmappedEnum(this Type t)
        {
            //Get the flags attribute, if it exists
            Object[] flagsAttribute = t.GetCustomAttributes(typeof(FlagsAttribute), true);
            
            return t.IsEnum && (flagsAttribute.Length > 0);
        }
    }

    /// <summary>
    /// Base node class, cannot be instantiated
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public abstract class ExpressionNode
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ExpressionNode");
        private const string cXmlTagResultObjType = "ResultObjType";
        private const string cXmlTagResultValue = "ResultValue";
        private const string cXmlTagCount = "Count";
        private const string cXmlTagTypeClass = "TypeClass";


        /// <summary>
        /// Type of the Node used for identification when called through base class
        /// (An alternative to GetType())
        /// </summary>
        private ExpressionNodeType nodeType = ExpressionNodeType.Base;

        private string tag = String.Empty;

        private TypeClass typeClass = TypeClass.Unsupported;

        private object lastEvaluationResult;

        private NameConditionType nameConditionType;

        private string objectName;

        private bool hasScript;

        private bool filterNodeCompatible;

        /// <summary>
        /// 
        /// </summary>
        protected object LastEvaluationResult
        {
            get
            {
                return lastEvaluationResult;
            }
            set
            {
                traceContext.TraceVerbose("Setting LastEvaluationResult to: {0}", value);
                lastEvaluationResult = value;
            }
        }

        /// <summary>
        /// Type of the Node (read-only)
        /// </summary>
        public ExpressionNodeType Type
        {
            get { return nodeType; }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void SetNodeType(ExpressionNodeType value)
        {
            traceContext.TraceMethodEnter("SetNodeType");
            // Tracing Input Parameters
            traceContext.TraceParameters(value);
            nodeType = value;
            traceContext.TraceMethodExit("SetNodeType");
        }

        /// <summary>
        /// Node's Tag 
        /// </summary>
        public string Tag
        {
            get { return tag; }
            set
            {
                traceContext.TraceVerbose("Setting Tag to: {0}", value);
                tag = value;
            }
        }

        /// <summary>
        /// Node's TypeClass
        /// </summary>
        public TypeClass TypeClass
        {
            get { return typeClass; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        protected void SetTypeClass(TypeClass value)
        {
            traceContext.TraceMethodEnter("SetTypeClass");
            // Tracing Input Parameters
            traceContext.TraceParameters(value);
            typeClass = value;
            traceContext.TraceMethodExit("SetTypeClass");
        }

        /// <summary>
        /// If condition has the following format {@Name operator 'object_name'}, returns type of operator
        /// otherwise returns None
        /// </summary>
        internal NameConditionType NameConditionType
        {
            get { return nameConditionType; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        protected void SetNameConditionType(NameConditionType value)
        {
            traceContext.TraceMethodEnter("SetNameConditionType");
            // Tracing Input Parameters
            traceContext.TraceParameters(value);
            nameConditionType = value;
            traceContext.TraceMethodExit("SetNameConditionType");
        }

        /// <summary>
        /// Name of object in NameCondition
        /// </summary>
        internal string ObjectName
        {
            get { return objectName; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        protected void SetObjectName(string value)
        {
            traceContext.TraceMethodEnter("SetObjectName");
            // Tracing Input Parameters
            traceContext.TraceParameters(value);
            objectName = value;
            traceContext.TraceMethodExit("SetObjectName");
        }

        /// <summary>
        /// Returns true if this ExpressionNode's evaluation will run a dynamic script, which is
        /// potentially dangerous.
        /// </summary>
        internal bool HasScript
        {
            get { return hasScript; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        protected void SetHasScript(bool value)
        {
            traceContext.TraceMethodEnter("SetHasScript");
            // Tracing Input Parameters
            traceContext.TraceParameters(value);
            hasScript = value;
            traceContext.TraceMethodExit("SetHasScript");
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool FilterNodeCompatible
        {
            get { return filterNodeCompatible; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        protected void SetFilterNodeCompatible(bool value)
        {
            traceContext.TraceMethodEnter("SetFilterNodeCompatible");
            // Tracing Input Parameters
            traceContext.TraceParameters(value);
            filterNodeCompatible = value;
            traceContext.TraceMethodExit("SetFilterNodeCompatible");
        }

        /// <summary>
        /// Sets class properties (HasScript, NameConditionType, ...)
        /// </summary>
        protected virtual void SetProperties() { }

        /// <summary>
        /// Creates a deep clone of the current expresion.
        /// The evaluation results are copied by reference, not by value.
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public virtual ExpressionNode DeepClone()
        {
            return null;
        }

#region EVALUATE

        /// <summary>
        /// 
        /// </summary>
        internal virtual void ResetResult()
        {
            LastEvaluationResult = null;
        }

        /// <summary>
        /// Evaluates the node (tree) using supplied Management Facet context
        /// </summary>
        /// <param name="context">Management Facet context</param>
        /// <returns></returns>
        public object Evaluate(FacetEvaluationContext context)
        {
            return Evaluate(context, false);
        }

        /// <summary>
        /// Evaluates the node (tree) using supplied Management Facet context
        /// </summary>
        /// <param name="context">Management Facet context</param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns></returns>
        public object Evaluate(FacetEvaluationContext context, bool checkSqlScriptAsProxy)
        {
            ResetResult();
            return DoEvaluate(context, checkSqlScriptAsProxy);
        }

        /// <summary>
        /// Does actual evaluatioin
        /// </summary>
        /// <param name="context">Management Facet context</param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal virtual object DoEvaluate(FacetEvaluationContext context, bool checkSqlScriptAsProxy)
        {
            return null;
        }


#endregion EVALUATE

#region ANALYZE
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationList"></param>
        internal virtual void AnalyzeForConfiguration(List<ConfigurationItem> configurationList)
        {
        }

        /// <summary>
        /// Result of the latest Evaluation
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public object GetResult()
        {
            return LastEvaluationResult;
        }

        /// <summary>
        /// Result of the latest Evaluation as a String
        /// </summary>
        /// <returns></returns>
        public string GetResultString()
        {
            if (null == LastEvaluationResult)
            {
                return String.Empty;
            }

            if (LastEvaluationResult is System.Array)
            {
                StringBuilder sb = new StringBuilder("{");
                foreach (object v in (LastEvaluationResult as System.Array))
                {
                    sb.Append(v.ToString() + ",");
                }
                if (((System.Array)LastEvaluationResult).Length > 0)
                {
                    // remove the last comma
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.Append("}");
                return sb.ToString();
            }
            else if (LastEvaluationResult.GetType() == typeof(string))
            {
                return "'" + LastEvaluationResult.ToString() + "'";
            }
            else
            {
                return LastEvaluationResult.ToString();
            }
        }

        /// <summary>
        /// Enumerates Children of the Node
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public virtual IEnumerable<ExpressionNode> EnumChildren()
        {
            List<ExpressionNode> list = new List<ExpressionNode>(0);
            return (IEnumerable<ExpressionNode>)list;
        }

        /// <summary>
        /// Enumerates Attributes in the tree
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<string> EnumAttributes()
        {
            List<string> list = new List<string>();
            DoEnumAttributes(list);
            return (IEnumerable<string>)list;
        }

        /// <summary>
        /// Actual tree enumerator
        /// </summary>
        /// <param name="list"></param>
        protected virtual void DoEnumAttributes(List<string> list)
        {
        }

        /// <summary>
        /// Enumerates Attributes in the tree along with thier Operators
        /// It ignores Attributes that are not direct children of Operators
        /// </summary>
        /// <returns></returns>
        internal List<AttributeOperatorPair> EnumAttributeOperatorPairs()
        {
            List<AttributeOperatorPair> list = new List<AttributeOperatorPair>();
            DoEnumAttributeOperatorPairs(list);
            return list;
        }

        /// <summary>
        /// Actual tree enumerator
        /// </summary>
        /// <param name="list"></param>
        internal virtual void DoEnumAttributeOperatorPairs(List<AttributeOperatorPair> list)
        {
        }

#endregion ANALYZE

        /// <summary>
        /// Represents Expression as a string in T-SQL like syntax
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Empty;
        }

        /// <summary>
        /// A special method to display some simple node in the UI in simplified form,
        /// which cannot always be parsed back
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public virtual string ToStringForDisplay()
        {
            return null;
        }

#region EQUALS
        /// <summary>
        /// Overriden to support overriden Equals
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Overriden Equals to support value comparison
        /// Inheritants implement EqualProperties method for type sepcific comparison
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override bool Equals(object obj)
        {
            bool ret = false;

            if (null == obj)
            {
                return false;
            }

            if (obj is ExpressionNode && this.Type == ((ExpressionNode)obj).Type)
            {
                ret = EqualProperties(obj);
            }

            return ret;
        }

        /// <summary>
        /// virtual method for descendants to implement type specific comparison
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected abstract bool EqualProperties(object obj);

#endregion EQUALS

#region SERIALIZATION
#region Instance methods
        /// <summary>
        /// Base serialization routine - creates start and end elements
        /// calls to virtual method SerializeProperties to output properties of particular node objectTypeName
        /// </summary>
        /// <param name="xmlWriter"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public void Serialize(XmlWriter xmlWriter)
        {
            if (xmlWriter == null)
            {
                throw new ArgumentNullException("xmlWriter");
            }
            xmlWriter.WriteStartElement(nodeType.ToString());
            xmlWriter.WriteElementString(cXmlTagTypeClass, typeClass.ToString());
            SerializeProperties(xmlWriter, false);
            xmlWriter.WriteEndElement();
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal void SerializeWithResult(XmlWriter xw)
        {
            xw.WriteStartElement(nodeType.ToString());
            xw.WriteElementString(cXmlTagTypeClass, typeClass.ToString());
            SerializeProperties(xw, true);
            xw.WriteEndElement();
        }

        /// <summary>
        /// Virtual method for children classes to serialize thier properties
        /// </summary>
        /// <param name="xw"></param>
        /// <param name="includeResult"></param>
        protected virtual void SerializeProperties(XmlWriter xw, bool includeResult)
        {
        }

        /// <summary>
        /// Includes Last Result into serialization output
        /// </summary>
        /// <param name="xw"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected void SerializeResult(XmlWriter xw)
        {
            if (null == LastEvaluationResult)
            {
                xw.WriteElementString(cXmlTagResultObjType, "NULL");
                xw.WriteElementString(cXmlTagResultValue, "NULL");
                return;
            }

            xw.WriteElementString(cXmlTagResultObjType, LastEvaluationResult.GetType().ToString());

            if (LastEvaluationResult is Array)
            {
                xw.WriteStartElement(cXmlTagResultValue);
                Array array = (Array)LastEvaluationResult;

                xw.WriteElementString(cXmlTagCount, array.Length.ToString());

                foreach (object obj in array)
                {
                    xw.WriteElementString(cXmlTagResultObjType, obj.GetType().ToString());
                    xw.WriteElementString(cXmlTagResultValue, ConvertToString(obj));
                }
                xw.WriteEndElement();
            }
            else
            {
                xw.WriteElementString(cXmlTagResultValue, ConvertToString(LastEvaluationResult));
            }
        }

        /// <summary>
        /// Deserializes Last Result 
        /// </summary>
        /// <param name="xr"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected void DeserializeResult(XmlReader xr)
        {
            string sObjType;
            string sValue;
            object value = null;

            sObjType = ReadElementWithCheck(xr, cXmlTagResultObjType);

            if (sObjType.EndsWith("[]", StringComparison.Ordinal))
            {
                ReadWithCheck(xr, XmlNodeType.Element, cXmlTagResultValue);
                sValue = ReadElementWithCheck(xr, cXmlTagCount);
                int count = Convert.ToInt32(sValue);
                object[] resultArray = new object[count];
                for (int i = 0; i < count; i++)
                {
                    List<string> vals = ReadNodeWithCheck(xr, cXmlTagResultObjType, cXmlTagResultValue);
                    sObjType = vals[0];
                    sValue = vals[1];

                    resultArray[i] = ConvertFromString(sObjType, sValue);
                }

                ReadWithCheck(xr, XmlNodeType.EndElement, cXmlTagResultValue);

                this.LastEvaluationResult = resultArray;
            }
            else
            {
                sValue = ReadElementWithCheck(xr, cXmlTagResultValue);
                value = ConvertFromString(sObjType, sValue);
                this.LastEvaluationResult = value;
            }
        }

        /// <summary>
        /// Virtual method for children classes to deserialize thier properties
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <param name="includeResult"></param>
        protected virtual void DeserializeProperties(XmlReader xr, bool includeResult)
        {
        }

        /// <summary>
        /// Reads and verifies instance specific end node element
        /// </summary>
        /// <param name="xr"></param>
        protected void ReadEndElement(XmlReader xr)
        {
            ReadWithCheck(xr, XmlNodeType.EndElement, Type.ToString());
        }
#endregion Instance mathods

#region Static Methods
        /// <summary>
        /// Reads the next xml node and verifies it has expected type and name (if supplied)
        /// </summary>
        /// <param name="xr"></param>
        /// <param name="nodeType">expected node type</param>
        /// <param name="name">expected node name (can be null)</param>
        /// <exception cref="ExpressionSerializationException">Thrown id node type and/or name don't match with provided type, name</exception>
        protected static void ReadWithCheck(XmlReader xr, XmlNodeType nodeType, string name)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ReadWithCheck"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(nodeType, name);
                xr.Read();

                if (false == (xr.NodeType == nodeType && (String.IsNullOrEmpty(name) || xr.Name == name)))
                {
                    throw methodTraceContext.TraceThrow(new ExpressionSerializationException(xr.NodeType.ToString(), xr.Name, nodeType.ToString(), name));
                }
            }
        }

        /// <summary>
        /// Moves to the node with specified type and name (if supplied)
        /// </summary>
        /// <param name="xr"></param>
        /// 
        /// <param name="name">node name (can be null)</param>
        /// <exception cref="ExpressionSerializationException">Thrown if no node found in the stream</exception>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected static void MoveToElementWithCheck(XmlReader xr, string name)
        {
            while (xr.Read())
            {
                if (xr.NodeType == XmlNodeType.Element)
                {
                    if (String.IsNullOrEmpty(name) || xr.Name == name)
                    {
                        return;
                    }
                }
                else
                {
                    throw traceContext.TraceThrow(new ExpressionSerializationException(xr.Name, name));
                }
            }

            throw traceContext.TraceThrow(new ExpressionSerializationException(string.Empty, name));
        }

        /// <summary>
        /// Reads and verifies named element in its entirety (Element - Text - EndElement)
        /// </summary>
        /// <param name="xr"></param>
        /// <param name="name">element's name</param>
        /// <returns>element's text</returns>
        protected static string ReadElementWithCheck(XmlReader xr, string name)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ReadElementWithCheck"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(xr, name);
                string s;

                MoveToElementWithCheck(xr, name);
                if (!xr.IsEmptyElement)
                {
                    ReadWithCheck(xr, XmlNodeType.Text, null);
                    s = xr.Value;
                    ReadWithCheck(xr, XmlNodeType.EndElement, name);
                }
                else
                {
                    s = xr.Value;
                }

                methodTraceContext.TraceParameterOut("returnVal", s);
                return s;
            }
        }

        /// <summary>
        /// Reads and veirifies specified ExpressionNode properties. Stops after reading the last requested element.
        /// Requested properties must be listed in the order they appear in the stream.
        /// </summary>
        /// <param name="xr"></param>
        /// <param name="elements">list of elements</param>
        /// <returns>list of element text values</returns>
        protected static List<string> ReadNodeWithCheck(XmlReader xr, params string[] elements)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ReadNodeWithCheck"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(xr, elements);
                string s;
                List<string> list = new List<string>();

                foreach (string elem in elements)
                {
                    s = ReadElementWithCheck(xr, elem);
                    list.Add(s);
                }

                methodTraceContext.TraceParameterOut("returnVal", list);
                return list;
            }
        }

        /// <summary>
        /// Reads and verifies simple (with no children) ExpressionNode  in its entirety, including end element.
        /// Requested properties must be listed in the order they appear in the stream.
        /// </summary>
        /// <param name="xr"></param>
        /// <param name="type">ExpressionNode's type</param>
        /// <param name="elements">list of properties</param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected static List<string> ReadSimpleNodeWithCheck(XmlReader xr, ExpressionNodeType type, params string[] elements)
        {
            string s;
            List<string> list = new List<string>();

            foreach (string elem in elements)
            {
                s = ReadElementWithCheck(xr, elem);
                list.Add(s);
            }

            ReadWithCheck(xr, XmlNodeType.EndElement, type.ToString());

            return list;
        }

        /// <summary>
        /// Deserialize from string - creates XmlReader and calls actual deserializer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ExpressionNode Deserialize(string value)
        {
            TextReader tr = new StringReader(value);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            XmlReader xr = XmlReader.Create(tr, settings);

            ExpressionNode node = ExpressionNode.Deserialize(xr);

            xr.Close();

            return node;
        }

        /// <summary>
        /// Deserialize from string - creates XmlReader and calls actual 
        /// deserializer. The ExpressionNode will contail the result of the 
        /// evaluation for the particular target that has generated this 
        /// serialized version of the node.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ExpressionNode DeserializeWithResult(string value)
        {
            TextReader tr = new StringReader(value);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            XmlReader xr = XmlReader.Create(tr, settings);

            ExpressionNode node = ExpressionNode.DeserializeWithResult(xr);

            xr.Close();

            return node;
        }

        /// <summary>
        /// Static method - provides generic way for deserializing nodes
        /// calls to virtual method DeserializeProperties to read properties 
        /// of particular node objectTypeName
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <returns></returns>
        public static ExpressionNode Deserialize(XmlReader xr)
        {
            return DoDeserialize(xr, false);
        }

        internal static ExpressionNode DeserializeWithResult(XmlReader xr)
        {
            return DoDeserialize(xr, true);
        }

        static ExpressionNode DoDeserialize(XmlReader xr, bool includeResult)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("DoDeserialize"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(xr, includeResult);
                ExpressionNode node = null;

                MoveToElementWithCheck(xr, null);

                string snode = xr.Name;
                ExpressionNodeType type = MatchType<ExpressionNodeType>(snode);

                switch (type)
                {
                    case ExpressionNodeType.Attribute:
                        node = new ExpressionNodeAttribute();
                        break;
                    case ExpressionNodeType.Constant:
                        node = new ExpressionNodeConstant();
                        break;
                    case ExpressionNodeType.Operator:
                        node = new ExpressionNodeOperator();
                        break;
                    case ExpressionNodeType.Group:
                        node = new ExpressionNodeGroup();
                        break;
                    case ExpressionNodeType.Function:
                        node = new ExpressionNodeFunction();
                        break;
                    default:
                        traceContext.DebugAssert(false, "ExpressionTree Deserialize - Unknown Node Type");
                        break;
                }

                if (null != node)
                {
                    string typeClass = ReadElementWithCheck(xr, cXmlTagTypeClass);
                    node.SetTypeClass(MatchType<TypeClass>(typeClass));
                    node.DeserializeProperties(xr, includeResult);
                    methodTraceContext.TraceParameterOut("returnVal", node);
                    return node;
                }

                methodTraceContext.TraceParameterOut("returnVal", null);
                return null;
            }
        }

        /// <summary>
        /// Serializes given Node to a string 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string SerializeNode(ExpressionNode node)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("SerializeNode", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(node);
                if (null == node)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("node"));
                }

                StringBuilder sb = new StringBuilder();

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                settings.NewLineOnAttributes = true;
                XmlWriter xw = XmlWriter.Create(sb, settings);

                node.Serialize(xw);

                xw.Close();

                return sb.ToString();
            }
        }

        /// <summary>
        /// Serializes given Node to a string
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static string SerializeNodeWithResult(ExpressionNode node)
        {
            if (null == node)
            {
                throw new ArgumentNullException("node");
            }

            StringBuilder sb = new StringBuilder();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            settings.NewLineOnAttributes = true;
            XmlWriter xw = XmlWriter.Create(sb, settings);

            node.SerializeWithResult(xw);

            xw.Close();

            return sb.ToString();
        }

        /// <summary>
        /// Static method - provides generic way of obtaining enum types from their string names
        /// </summary>
        /// <typeparam name="T">ObjectType to match</typeparam>
        /// <param name="value">name</param>
        /// 
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static T MatchType<T>(string value)
        {
            Type t = typeof(T);

            return (T)(t.GetField(value).GetValue(null));
        }

        /// <summary>
        /// Converts an object to its string representation
        /// understandable by ConvertFromString
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected static string ConvertToString(object value)
        {
            string str;

            if (null == value)
            {
                return "NULL";
            }

            Type type = value.GetType();

            if (type == typeof(DateTime))
            {
                str = SFC.SfcSecureString.XmlEscape(((DateTime)value).ToBinary().ToString());
            }
            else
            {
                str = SFC.SfcSecureString.XmlEscape(value.ToString());
            }

            return str;
        }

        /// <summary>
        /// Constructs Enum object from its string representation
        /// </summary>
        /// <param name="stringObjType"></param>
        /// <param name="stringValue"></param>
        /// <returns></returns>
        protected static object ResolveEnum(string stringObjType, string stringValue)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ResolveEnum"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(stringObjType, stringValue);
                // $FUTURE grigoryp 1/17/07
                // This code will have to be reviewed when we get to 3rd party assemblies
                // We may have troubles getting type info for enums defined in those assembles
                //

                object value = null;

                Type t = System.Type.GetType(stringObjType, false);
                if (null == t)
                {
                    Assembly assm = typeof(Microsoft.SqlServer.Management.Smo.Server).Assembly;
                    t = assm.GetType(stringObjType, false);
                }
                if (null == t)
                {
                    Assembly assm = typeof(Microsoft.SqlServer.Management.Smo.LoginType).Assembly;
                    t = assm.GetType(stringObjType, false);
                }
                if (null == t)
                {
                    // load this assembly through reflection as we don't want to take a 
                    // build time dependency on the ServiceBrokerEnum.dll 
                    // because it would mean we need to fork it for the *configuration* 
                    // set of assemblies
                    Assembly assm = typeof(Microsoft.SqlServer.Management.Smo.LoginType).Assembly;
                    string brokerAsmName = assm.FullName.Replace("SqlEnum", "ServiceBrokerEnum");
                    Assembly assmBroker = Assembly.Load(brokerAsmName);
                    t = assmBroker.GetType(stringObjType, false);
                }
                if (null == t)
                {
                    Assembly assm = typeof(Microsoft.SqlServer.Management.Common.DatabaseEngineType).Assembly;
                    t = assm.GetType(stringObjType, false);
                }
                if (null == t && !Microsoft.SqlServer.Server.SqlContext.IsAvailable)
                {
                    // Don't load Adapters in SqlClr

                    string[] nameparts = Assembly.GetExecutingAssembly().FullName.Split(new char[] { ',' }, 2);
                    // AS & RS SAC facets + ExtendedProtection
                    string aname = "Microsoft.SqlServer.Dmf.Adapters, " + nameparts[1];
                    Assembly adapters = Assembly.Load(aname);
                    t = adapters.GetType(stringObjType, false);
                }
                if (null != t && t.IsEnum)
                {
                    value = Enum.Parse(t, stringValue);
                }

                methodTraceContext.TraceParameterOut("returnVal", value);
                return value;
            }
        }

        /// <summary>
        /// Converts string to an object of specified type. 
        /// Type must be supported. UnsupportedTypeException thrown otherwise.
        /// Catches FormatException and throws TypeConversionException.
        /// </summary>
        /// <param name="stringObjType"></param>
        /// <param name="stringValue"></param>
        /// <returns></returns>
        /// <exception cref="TypeConversionException"></exception>
        /// <exception cref="UnsupportedTypeException"></exception>
        protected static object ConvertFromString(string stringObjType, string stringValue)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ConvertFromString"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(stringObjType, stringValue);
                object value = null;
                string str = SFC.SfcSecureString.XmlUnEscape(stringValue);

                try
                {
                    switch (stringObjType)
                    {
                        case "NULL":
                            break;
                        case "System.String":
                            value = str;
                            break;
                        case "System.Int32":
                            value = Convert.ToInt32(str);
                            break;
                        case "System.Boolean":
                            value = Convert.ToBoolean(str);
                            break;
                        case "System.Double":
                            value = Convert.ToDouble(str);
                            break;
                        case "System.Int64":
                            value = Convert.ToInt64(str);
                            break;
                        case "System.Int16":
                            value = Convert.ToInt16(str);
                            break;
                        case "System.Byte":
                            value = Convert.ToByte(str);
                            break;
                        case "System.DateTime":
                            value = DateTime.FromBinary((Convert.ToInt64(str)));
                            break;
                        case "System.Guid":
                            value = new Guid(str);
                            break;
                        case "System.Decimal":
                            value = Convert.ToDecimal(str);
                            break;
                        case "System.Char":
                            value = Convert.ToChar(str);
                            break;
                        case "System.Single":
                            value = Convert.ToSingle(str);
                            break;
                        default:
                            value = ResolveEnum(stringObjType, str);
                            if (null == value)
                            {
                                throw methodTraceContext.TraceThrow(new UnsupportedTypeException(typeof(ExpressionNodeConstant).Name, stringObjType));
                            }
                            break;
                    }
                }
                catch (System.FormatException fex)
                {
                    methodTraceContext.TraceCatch(fex);
                    throw methodTraceContext.TraceThrow(new TypeConversionException(str, stringObjType, fex));
                }

                methodTraceContext.TraceParameterOut("returnVal", value);
                return value;
            }
        }

        /// <summary>
        /// Converts string to integer. 
        /// Catches FormatException and throws TypeConversionException.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TypeConversionException"></exception>
        protected static int ConvertToIntWithCheck(string value)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ConvertToIntWithCheck"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(value);
                int res;

                try
                {
                    res = Convert.ToInt32(value);
                }
                catch (System.FormatException fex)
                {
                    methodTraceContext.TraceCatch(fex);
                    throw methodTraceContext.TraceThrow(new TypeConversionException(value, typeof(int).ToString(), fex));
                }

                methodTraceContext.TraceParameterOut("returnVal", res);
                return res;
            }
        }

#endregion Static Methods

#endregion SERIALIZATION

#region FilterNode conversion
        /// <summary>
        /// Parses input string into ExpressionNode
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static ExpressionNode Parse(string input)
        {
            return Parse(input, null);
        }

        /// <summary>
        /// Parses input string into ExpressionNode and verifies against given Facet
        /// </summary>
        /// <param name="input"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static ExpressionNode Parse(string input, Type facet)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Parse", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(input, facet);
                if (null == input)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("input"));
                }

                // This will thow if input is null
                if (String.IsNullOrEmpty(input.Trim()))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("input")));
                }

                // Parsing is done using XPath parser
                // The expression is the Filter of the created URN

                // Create fake URN
                string urnStr = String.Format("FakeLevel[{0}]", input);
                SFC.Urn urn = new SFC.Urn(urnStr);

                // Induce parsing and catch exceptions
                try
                {
                    SFC.XPathExpression xpe = urn.XPathExpression;
                }
                catch (SFC.XPathException xpe)
                {
                    methodTraceContext.TraceCatch(xpe);
                    CheckForDateFunctions (input);
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ParsingError(input), xpe));
                }
                catch (SFC.InvalidQueryExpressionEnumeratorException iqe)
                {
                    methodTraceContext.TraceCatch(iqe);
                    CheckForDateFunctions (input);
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ParsingError(input), iqe));
                }

                // Create ExpressionNode
                ExpressionNode node = ExpressionNode.ConvertFromFilterNode(urn.XPathExpression[0].Filter, facet);

                methodTraceContext.TraceParameterOut("returnVal", node);
                return node;
            }
        }

        /// <summary>
        /// Verifies if parsing failed due to unquoted datepart argument
        /// Throws specific Exception if the pattern detected
        /// </summary>
        /// <param name="input"></param>
        static void CheckForDateFunctions (string input)
        {
            // VSTS #214321
            // T-SQL DateAdd and DatePart have datepart arg as keyword
            // PBM expressions require datepart arg as a string
            // We try to identify situation when parsing failed due to the user using T-Sql syntax
            // This is no 100% reliable as we don't parse, but rather textually match to a pattern
            // This is a workaround to having parser understand keywords (which may not be desirable)
        
            const string rxpattern = 
@"(DATEADD|DATEPART)\(\s*(YEAR|Y|YY|YYYY|MONTH|MM|M|DAYOFYEAR|DY|DAY|DD|D|WEEKDAY|DW|HOUR|HH|MINUTE|MI|N|SECOND|SS|S|MILLISECOND|MS)\s*\,";

            Regex rx = new Regex (rxpattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            if (rx.IsMatch (input))
            {
                throw new DmfException (ExceptionTemplatesSR.ParsingUnquotedDatePart (input));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterNode"></param>
        /// <returns></returns>
        public static ExpressionNode ConvertFromFilterNode(SFC.FilterNode filterNode)
        {
            return ConvertFromFilterNode(filterNode, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterNode"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static ExpressionNode ConvertFromFilterNode(SFC.FilterNode filterNode, Type facet)
        {
            ExpressionNode node = null;

            if (null == filterNode)
            {
                return null;
            }

            switch (filterNode.NodeType)
            {
                case SFC.FilterNode.Type.Attribute:
                    SFC.FilterNodeAttribute fa = (SFC.FilterNodeAttribute)filterNode;
                    if (null == facet)
                    {
                        node = new ExpressionNodeAttribute(fa.Name);
                    }
                    else
                    {
                        node = new ExpressionNodeAttribute(fa.Name, facet);
                    }
                    break;
                case SFC.FilterNode.Type.Constant:
                    SFC.FilterNodeConstant fc = (SFC.FilterNodeConstant)filterNode;
                    node = new ExpressionNodeConstant(fc.Value);
                    break;
                case SFC.FilterNode.Type.Function:
                    SFC.FilterNodeFunction ff = (SFC.FilterNodeFunction)filterNode;
                    switch (ff.FunctionType)
                    {
                        case SFC.FilterNodeFunction.Type.True:
                            node = new ExpressionNodeFunction(ExpressionNodeFunction.Function.True);
                            break;
                        case SFC.FilterNodeFunction.Type.False:
                            node = new ExpressionNodeFunction(ExpressionNodeFunction.Function.False);
                            break;
                        case SFC.FilterNodeFunction.Type.Like:
                            ExpressionNode left = ExpressionNodeOperator.ConvertFromFilterNode(ff.GetParameter(0), facet);
                            ExpressionNode right = ExpressionNodeOperator.ConvertFromFilterNode(ff.GetParameter(1), facet);
                            node = new ExpressionNodeOperator(OperatorType.LIKE, left, right);
                            break;
                        case SFC.FilterNodeFunction.Type.String:
                            ExpressionNode arg = ExpressionNode.ConvertFromFilterNode(ff.GetParameter(0), facet);
                            node = new ExpressionNodeFunction(ExpressionNodeFunction.Function.String, arg);
                            break;
                        case SFC.FilterNodeFunction.Type.UserDefined:
                            string fname = ff.Name;
                            int nargs = ff.ParameterCount;
                            ExpressionNode[] args = new ExpressionNode[nargs];

                            ExpressionNodeFunction.Function ftype = (ExpressionNodeFunction.Function)Enum.Parse(typeof(ExpressionNodeFunction.Function), fname, true);

                            for (int i = 0; i < nargs; i++)
                            {
                                args[i] = ExpressionNode.ConvertFromFilterNode(ff.GetParameter(i), facet);
                            }

                            node = new ExpressionNodeFunction(ftype, args);
                            break;
                        default:
                            throw traceContext.TraceThrow(new ConversionNotSupportedException(ExceptionTemplatesSR.Function, ff.FunctionType.ToString()));
                    }
                    break;
                case SFC.FilterNode.Type.Operator:
                    SFC.FilterNodeOperator fo = (SFC.FilterNodeOperator)filterNode;
                    if (fo.OpType == SFC.FilterNodeOperator.Type.NEG)
                    {
                        ExpressionNode left = ExpressionNodeOperator.ConvertFromFilterNode(fo.Left, facet);
                        if (left.Type == ExpressionNodeType.Constant && left.TypeClass == TypeClass.Numeric)
                        {
                            double v = -1 * (double)left.LastEvaluationResult;
                            node = new ExpressionNodeConstant(v);
                        }
                        else
                        {
                            throw traceContext.TraceThrow(new ConversionNotSupportedException(ExceptionTemplatesSR.Operator, fo.OpType.ToString()));
                        }
                    }
                    else
                    {
                        ExpressionNode left = ExpressionNodeOperator.ConvertFromFilterNode(fo.Left, facet);
                        ExpressionNode right = ExpressionNodeOperator.ConvertFromFilterNode(fo.Right, facet);
                        OperatorType otype = ExpressionNodeOperator.ConvertType(fo.OpType);
                        if (null == left || null == right || otype == OperatorType.NONE)
                        {
                            break;
                        }
                        node = new ExpressionNodeOperator(otype, left, right);
                    }
                    break;
                case SFC.FilterNode.Type.Group:
                    SFC.FilterNodeGroup fg = (SFC.FilterNodeGroup)filterNode;
                    ExpressionNode group = ExpressionNode.ConvertFromFilterNode(fg.Node, facet);
                    node = new ExpressionNodeGroup(group);
                    break;
                default:
                    traceContext.DebugAssert(false, String.Format("Converting FilterNode to ExpressionNode - Unsupported FilterNode type '{0}'", filterNode.NodeType.ToString()));
                    break;
            }

            return node;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SFC.FilterNode ConvertToFilterNode()
        {
            return DoConvertToFilterNode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected virtual SFC.FilterNode DoConvertToFilterNode()
        {
            traceContext.DebugAssert(false, String.Format("Converting ExpressionNode to FilterNode - conversion of {0} is not supported", this.Type.ToString()));
            return null;
        }

#endregion FilterNode conversion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static ExpressionNode ConstructNode(object obj)
        {
            if (obj == null)
            {
                // This function requires the ability to "GetType" on obj
                // There is no way to tell the type of a null object.
                throw traceContext.TraceThrow(new ArgumentNullException("obj"));
            }

            if (!EvaluationFactory.IsTypeSupported(obj.GetType()))
            {
                throw traceContext.TraceThrow(new UnsupportedObjectTypeException(obj.GetType().ToString(), typeof(ExpressionNode).ToString()));
            }

            TypeClass typeClass = EvaluationFactory.ClassifyType(obj);
            switch (typeClass)
            {
                case TypeClass.Numeric:
                    if (obj.GetType().IsEnum)
                    {
                        return new ExpressionNodeFunction(ExpressionNodeFunction.Function.Enum,
                            new ExpressionNodeConstant(obj.GetType().ToString()),
                            new ExpressionNodeConstant(obj.ToString()));
                    }
                    else
                    {
                        return new ExpressionNodeConstant(obj);
                    }
                case TypeClass.String:
                    return new ExpressionNodeConstant(obj);
                case TypeClass.DateTime:
                    return new ExpressionNodeFunction(ExpressionNodeFunction.Function.DateTime,
                        new ExpressionNodeConstant(((DateTime)obj).ToString("o")));
                case TypeClass.Bool:
                    return Evaluator.ConvertToBool(obj) ?
                        new ExpressionNodeFunction(ExpressionNodeFunction.Function.True) :
                        new ExpressionNodeFunction(ExpressionNodeFunction.Function.False);
                case TypeClass.Guid:
                    return new ExpressionNodeFunction(ExpressionNodeFunction.Function.Guid,
                        new ExpressionNodeConstant(obj.ToString()));
                case TypeClass.Array:
                    Array array = (Array)obj;
                    ExpressionNode[] args = new ExpressionNode[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        args[i] = new ExpressionNodeConstant(array.GetValue(i));
                    }
                    return new ExpressionNodeFunction(ExpressionNodeFunction.Function.Array, args);
                case TypeClass.BitmappedEnum:
                    return new ExpressionNodeFunction(ExpressionNodeFunction.Function.Enum,
                        new ExpressionNodeConstant(obj.GetType().ToString()),
                        new ExpressionNodeConstant(obj.ToString()));
            }

            traceContext.DebugAssert(false, "Unable construct node!");
            return null;
        }

        /// <summary>
        /// Represents Expression as a string in T-SQL like syntax, which can be used in URN
        /// </summary>
        /// <returns></returns>
        public string ToStringForUrn()
        {
            return this.DoConvertToFilterNode().ToString();
        }

    }

    /// <summary>
    /// Node representing Constants
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeConstant : ExpressionNode
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ExpressionNodeConstant");
        private const string cXmlTagObjType = "ObjType";
        private const string cXmlTagValue = "Value";

        private object mObject;

        /// <summary>
        /// Constant Value
        /// </summary>
        public object Value { get { return mObject; } }

        /// <summary>
        /// Default constructor for deserialization
        /// </summary>
        internal ExpressionNodeConstant()
        {
            SetNodeType(ExpressionNodeType.Constant);
        }

        /// <summary>
        /// Creates Constant node
        /// </summary>
        /// <param name="obj">Constant value</param>
        public ExpressionNodeConstant(object obj)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ExpressionNodeConstant", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(obj);
                if (obj == null)
                {
                    // This function attempts to derive the type of the object.  If it is null, it cannot proceed.
                    // Adding this check to prevent NULL constants
                    // until we figure out how to support NULL in UI
                    // All other NULL handling code remains in place
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("obj"));
                }

                Type type = obj.GetType();

                // Special case - SqlSecureString
                // SqlSecureString is not a supported type
                // However some SMO facets returns SqlSecureString from their context GetProperty 
                // (accessed directly through their PropertyBug)
                // while the actual property is String
                //
                if (false == (EvaluationFactory.IsTypeSupportedForConstant(type) || obj is SqlSecureString))
                {
                    throw methodTraceContext.TraceThrow(new UnsupportedTypeException(this.GetType().Name, type.ToString()));
                }

                // Special case - SqlSecureString
                // Substitute with string (we cannot deserialize it back as SqlSecureString anyway)
                //
                if (obj is SqlSecureString)
                {
                    obj = obj.ToString();
                }

                SetNodeType(ExpressionNodeType.Constant);
                mObject = obj;
                LastEvaluationResult = obj;
                SetTypeClass(EvaluationFactory.ClassifyType(obj));
            }
        }

        /// <summary>
        /// Represents Expression as a string in T-SQL like syntax
        /// </summary>
        /// <returns></returns>
        /// <remarks>If the Expression is a string object it will be quoted with '</remarks>
        public override string ToString()
        {
            string str;

            if (null == Value)
            {
                return "NULL";
            }

            if (Value.GetType() == typeof(string))
            {
                str = String.Format("'{0}'", SFC.Urn.EscapeString(Value.ToString()));
            }
            else
            {
                str = Value.ToString();
            }

            return str;
        }

        /// <summary>
        /// A special method to display some simple node in the UI in simplified form
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override string ToStringForDisplay()
        {
            return this.ToString();
        }

#region EQUALS
        /// <summary>
        /// Constant specific type comparison
        /// type and nullability of comparison object checked by caller (Equals)
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override bool EqualProperties(object obj)
        {
            bool ret;
            object cmpObject = ((ExpressionNodeConstant)obj).mObject;

            try
            {
                ret = EvaluationFactory.Evaluate(this.mObject, cmpObject, OperatorType.EQ);
            }
            catch (ExpressionTypeMistmatchException)
            {
                traceContext.TraceError("Caught a general Exception of type ExpressionTypeMistmatchException");
                ret = false;
            }

            return ret;
        }
#endregion EQUALS

#region EVALUATE
        /// <summary>
        /// 
        /// </summary>
        internal override void ResetResult()
        {
            //LastEvaluationResult = mObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal override object DoEvaluate(FacetEvaluationContext context, bool checkSqlScriptAsProxy)
        {
            return mObject;
        }

#endregion EVALUATE

#region SERIALIZE
        /// <summary>
        /// Serializes Constant node properties
        /// </summary>
        /// <param name="xw"></param>
        /// <param name="includeResult"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override void SerializeProperties(XmlWriter xw, bool includeResult)
        {
            string str;

            // ignore includeResult
            if (null != mObject)
            {
                xw.WriteElementString(cXmlTagObjType, mObject.GetType().ToString());
                //String constant values in the XML tree are expected to be escaped
                str = SFC.Urn.EscapeString(ConvertToString(mObject));
                xw.WriteElementString(cXmlTagValue, str);
            }
            else
            {
                xw.WriteElementString(cXmlTagObjType, "NULL");
                xw.WriteElementString(cXmlTagValue, "NULL");
            }
        }

        /// <summary>
        /// Deserializes Constant node properties
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <param name="includeResult"></param>
        protected override void DeserializeProperties(XmlReader xr, bool includeResult)
        {
            traceContext.TraceMethodEnter("DeserializeProperties");
            // Tracing Input Parameters
            traceContext.TraceParameters(xr, includeResult);
            // ignore includeResult

            string sObjType;
            string sValue;
            object value = null;

            List<string> vals = ReadSimpleNodeWithCheck(xr, Type, cXmlTagObjType, cXmlTagValue);
            sObjType = vals[0];
            sValue = vals[1];

            if (sObjType != "NULL")
            {
                //String constant values in the XML tree are escaped
                value = ConvertFromString(sObjType, SFC.Urn.UnEscapeString(sValue));
                this.mObject = value;
                this.LastEvaluationResult = value;
            }
            else
            {
                this.mObject = null;
                this.LastEvaluationResult = null;
            }

            traceContext.TraceMethodExit("DeserializeProperties");
        }
#endregion SERIALIZE

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SFC.FilterNode DoConvertToFilterNode()
        {
            SFC.FilterNode fnode = null;

            Type type = Value.GetType();

            if (ExpressionNodeOperator.numericTypes.Contains(type))
            {
                fnode = new SFC.FilterNodeConstant(Value, SFC.FilterNodeConstant.ObjectType.Number);
            }
            else if (ExpressionNodeOperator.stringTypes.Contains(type))
            {
                fnode = new SFC.FilterNodeConstant(Value.ToString(), SFC.FilterNodeConstant.ObjectType.String);
            }
            else if (type == typeof(bool))
            {
                fnode = new SFC.FilterNodeFunction(
                    ((bool)Value) ? SFC.FilterNodeFunction.Type.True : SFC.FilterNodeFunction.Type.False,
                    ((bool)Value) ? "TRUE" : "FALSE",
                    new SFC.FilterNode[] { });
            }
            else if (type.IsSubclassOf(typeof(Enum)))
            {
                fnode = new SFC.FilterNodeConstant(Convert.ToInt32(Value), SFC.FilterNodeConstant.ObjectType.Number);
            }
            else
            {
                // conversion of the value type failed. see vsts#217413 for details. -anchals
                throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.TypeNotSupported(type.ToString())));
            }


            return fnode;
        }

        /// <summary>
        /// Deep clone of the current node.
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override ExpressionNode DeepClone()
        {
            ExpressionNode clone = new ExpressionNodeConstant(this.Value);
            return clone;
        }
    }

    /// <summary>
    /// Node representing Attributes - properties of Management Facets
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeAttribute : ExpressionNode
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ExpressionNodeAttribute");
        private const string cXmlTagName = "Name";

        private string mName;

        /// <summary>
        /// Attribute Name - property of Management Facet
        /// </summary>
        public string Name { get { return mName; } }

        /// <summary>
        /// Default constructor for deserialization
        /// </summary>
        internal ExpressionNodeAttribute()
        {
            SetNodeType(ExpressionNodeType.Attribute);
        }

        /// <summary>
        /// Creates Attribute node
        /// </summary>
        /// <param name="name">Attribute name</param>
        public ExpressionNodeAttribute(string name)
        {
            // $FUTURE This constructor should be made internal
            // VSTS# 122143 

            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(name);
            }

            SetNodeType(ExpressionNodeType.Attribute);
            mName = name;
        }

        /// <summary>
        /// Creates Attribute node and verifies it against Facet
        /// </summary>
        /// <param name="name"></param>
        /// <param name="facet"></param>
        public ExpressionNodeAttribute(string name, Type facet)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ExpressionNodeAttribute", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(name, facet);
                if (String.IsNullOrEmpty(name))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("name"));
                }
                if (null == facet)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("facet"));
                }

                PropertyInfo[] props = FacetRepository.GetFacetProperties(facet);
                foreach (PropertyInfo pi in props)
                {
                    if (pi.Name == name)
                    {
                        SetNodeType(ExpressionNodeType.Attribute);
                        mName = name;
                        SetTypeClass(EvaluationFactory.ClassifyType(pi.PropertyType));
                        return;
                    }
                }

                throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Property, name, new MissingPropertyException(name)));
            }
        }

        /// <summary>
        /// Represents Expression as a string in T-SQL like syntax
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "@" + Name;
        }

        /// <summary>
        /// A special method to display some simple node in the UI in simplified form
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override string ToStringForDisplay()
        {
            return this.ToString();
        }

#region EQUALS
        /// <summary>
        /// Attribute specific type comparison
        /// type and nullability of comparison object checked by caller (Equals)
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override bool EqualProperties(object obj)
        {
            bool ret = (this.Name == ((ExpressionNodeAttribute)obj).Name && this.TypeClass == ((ExpressionNodeAttribute)obj).TypeClass);

            return ret;
        }
#endregion EQUALS

#region EVALUATE
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns></returns>
        internal override object DoEvaluate(FacetEvaluationContext context, bool checkSqlScriptAsProxy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("DoEvaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(context, checkSqlScriptAsProxy);
                if (null == context)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("context"));
                }

                LastEvaluationResult = context.GetPropertyValue(mName);

                // Special case - SqlSecureString
                // Substitute with string (we cannot deserialize it back as SqlSecureString anyway)
                //
                if (LastEvaluationResult is SqlSecureString)
                {
                    LastEvaluationResult = LastEvaluationResult.ToString();
                }

                methodTraceContext.TraceParameterOut("returnVal", LastEvaluationResult);
                return LastEvaluationResult;
            }
        }

#endregion EVALUATE

#region SERIALIZE
        /// <summary>
        /// Serializes Attribute node properties
        /// </summary>
        /// <param name="xw"></param>
        /// <param name="includeResult"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override void SerializeProperties(XmlWriter xw, bool includeResult)
        {
            xw.WriteElementString(cXmlTagName, mName);
            if (includeResult)
            {
                SerializeResult(xw);
            }
        }

        /// <summary>
        /// Deserializes Attribute node properties
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <param name="includeResult"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override void DeserializeProperties(XmlReader xr, bool includeResult)
        {
            List<string> vals;


            if (includeResult)
            {
                vals = ReadNodeWithCheck(xr, cXmlTagName);
                DeserializeResult(xr);
                ReadEndElement(xr);
            }
            else
            {
                vals = ReadSimpleNodeWithCheck(xr, Type, cXmlTagName);
            }

            this.mName = vals[0];
        }
#endregion SERIALIZE

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SFC.FilterNode DoConvertToFilterNode()
        {
            SFC.FilterNodeAttribute fnode = new SFC.FilterNodeAttribute(Name);

            return fnode;
        }

        /// <summary>
        /// Deep clone of the current node.
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override ExpressionNode DeepClone()
        {
            ExpressionNodeAttribute clone = new ExpressionNodeAttribute(this.Name);
            clone.LastEvaluationResult = this.LastEvaluationResult;
            clone.SetTypeClass(this.TypeClass);
            return clone;
        }

        /// <summary>
        /// This override is implemented in case somebody calls EnumAttribues directly on Attribute node.
        /// </summary>
        /// <param name="list"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override void DoEnumAttributes(List<string> list)
        {
            // This method shouldn't ba called in normal situation, 
            // but we want to be constistent. 
            // A tree made of a single Attribute node makes an invalid condition, but it's a valid tree
            // And it's always a valid subtree

            list.Add(Name);
        }
    }

    /// <summary>
    /// Base class for nodes, having children; cannot be instantiated
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public abstract class ExpressionNodeChildren : ExpressionNode
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ExpressionNodeChildren");
        private const string cXmlTagCount = "Count";

        /// <summary>
        /// List of children nodes
        /// </summary>
        private List<ExpressionNode> childrenList;

        /// <summary>
        /// 
        /// </summary>
        protected List<ExpressionNode> ChildrenList
        {
            get
            {
                return childrenList;
            }
            set
            {
                traceContext.TraceVerbose("Setting ChildrenList to: {0}", value);
                childrenList = value;
            }
        }

        /// <summary>
        /// List of children nodes
        /// </summary>
        public IEnumerable<ExpressionNode> EnumerableChildrenList
        {
            get
            {
                return (IEnumerable<ExpressionNode>)ChildrenList;
            }
        }

        /// <summary>
        /// Enumerates Children of the Node
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override IEnumerable<ExpressionNode> EnumChildren()
        {
            return EnumerableChildrenList;
        }

        /// <summary>
        /// Count of children nodes
        /// </summary>
        public int Count { get { return ChildrenList.Count; } }

        /// <summary>
        /// Adds node to the list
        /// Does NOT check if the node added has the same Type as previously added nodes
        /// </summary>
        /// <param name="node"></param>
        protected virtual void Add(ExpressionNode node)
        {
            ChildrenList.Add(node);
        }

#region internal utilities

#endregion

#region EQUALS
        /// <summary>
        /// Children node specific type comparison
        /// type and nullability of comparison object checked by caller (Equals)
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override bool EqualProperties(object obj)
        {
            bool ret = false;

            if (this.Count == ((ExpressionNodeChildren)obj).Count)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (false == this.ChildrenList[i].Equals(((ExpressionNodeChildren)obj).ChildrenList[i]))
                    {
                        return false;
                    }
                }

                ret = true;
            }

            return ret;
        }
#endregion EQUALS

        /// <summary>
        /// 
        /// </summary>
        internal override void ResetResult()
        {
            foreach (ExpressionNode node in EnumerableChildrenList)
            {
                node.ResetResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationList"></param>
        internal override void AnalyzeForConfiguration(List<ConfigurationItem> configurationList)
        {
            foreach (ExpressionNode node in EnumerableChildrenList)
            {
                node.AnalyzeForConfiguration(configurationList);
            }
        }

        /// <summary>
        /// Recursively enumerates Attributes
        /// </summary>
        /// <param name="list"></param>
        protected override void DoEnumAttributes(List<string> list)
        {
            foreach (ExpressionNode node in ChildrenList)
            {
                if (node is ExpressionNodeChildren)
                {
                    ((ExpressionNodeChildren)node).DoEnumAttributes(list);
                }
                else if (node.Type == ExpressionNodeType.Attribute)
                {
                    list.Add(((ExpressionNodeAttribute)node).Name);
                }
            }
        }

        internal override void DoEnumAttributeOperatorPairs(List<AttributeOperatorPair> list)
        {
            foreach (ExpressionNode node in ChildrenList)
            {
                if (node is ExpressionNodeChildren)
                {
                    ((ExpressionNodeChildren)node).DoEnumAttributeOperatorPairs(list);
                }
                else if (node.Type == ExpressionNodeType.Attribute && this.Type == ExpressionNodeType.Operator)
                {
                    list.Add(new AttributeOperatorPair(((ExpressionNodeAttribute)node).Name, ((ExpressionNodeOperator)this).OpType));
                }
            }
            base.DoEnumAttributeOperatorPairs(list);
        }

        /// <summary>
        /// Represents Expression as a string in T-SQL like syntax
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            bool firstChild = true;

            StringBuilder sb = new StringBuilder();

            sb.Append("(");

            foreach (ExpressionNode node in EnumerableChildrenList)
            {
                if (firstChild)
                {
                    firstChild = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(node.ToString());
            }

            sb.Append(")");

            return sb.ToString();
        }

#region SERIALIZE
        /// <summary>
        /// Serializes Children nodes 
        /// </summary>
        /// <param name="xw"></param>
        /// <param name="includeResult"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override void SerializeProperties(XmlWriter xw, bool includeResult)
        {
            xw.WriteElementString(cXmlTagCount, Count.ToString());

            foreach (ExpressionNode node in EnumerableChildrenList)
            {
                if (includeResult)
                {
                    node.SerializeWithResult(xw);
                }
                else
                {
                    node.Serialize(xw);
                }
            }
        }

        /// <summary>
        /// Deserializes Children nodes
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <param name="includeResult"></param>
        protected override void DeserializeProperties(XmlReader xr, bool includeResult)
        {
            traceContext.TraceMethodEnter("DeserializeProperties");
            // Tracing Input Parameters
            traceContext.TraceParameters(xr, includeResult);
            int count;

            // Read node variables - count
            List<string> vals = ReadNodeWithCheck(xr, cXmlTagCount);
            count = ConvertToIntWithCheck(vals[0]);

            // Deserialize children nodes
            for (int i = 0; i < count; i++)
            {
                ExpressionNode node = includeResult ?
                    ExpressionNode.DeserializeWithResult(xr) :
                    ExpressionNode.Deserialize(xr);
                Add(node);
            }

            ReadEndElement(xr);
            traceContext.TraceMethodExit("DeserializeProperties");
        }
#endregion SERIALIZE
    }

    /// <summary>
    /// Node, representing a Group - node in parentheses
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeGroup : ExpressionNodeChildren
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ExpressionNodeGroup");
        /// <summary>
        /// Default constructor for deserialization
        /// </summary>
        internal ExpressionNodeGroup()
        {
            SetNodeType(ExpressionNodeType.Group);
            ChildrenList = new List<ExpressionNode>(1);
        }

        /// <summary>
        /// Creates Group node
        /// </summary>
        /// <param name="node">Group node</param>
        public ExpressionNodeGroup(ExpressionNode node)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ExpressionNodeGroup", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(node);
                if (null == node)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException());
                }

                SetNodeType(ExpressionNodeType.Group);
                SetTypeClass(node.TypeClass);
                ChildrenList = new List<ExpressionNode>(1);
                Add(node);

                SetProperties();
            }
        }

        /// <summary>
        /// Group node - node inside paretheses - usually operator
        /// </summary>
        public ExpressionNode Group
        {
            get { return ChildrenList[0]; }
            set
            {
                traceContext.TraceVerbose("Setting Group to: {0}", value);
                ChildrenList[0] = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void SetProperties()
        {
            SetHasScript(Group.HasScript);
            SetNameConditionType(Group.NameConditionType);
            SetFilterNodeCompatible(Group.FilterNodeCompatible);
        }
        /// <summary>
        /// 
        /// </summary>
        internal override void ResetResult()
        {
            LastEvaluationResult = null;
            base.ResetResult();
        }

        /// <summary>
        /// Evaluates Group node (usually operator)
        /// </summary>
        /// <param name="context">Management Facet context</param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns>object</returns>
        internal override object DoEvaluate(FacetEvaluationContext context, bool checkSqlScriptAsProxy)
        {
            LastEvaluationResult = Group.DoEvaluate(context, checkSqlScriptAsProxy);

            return LastEvaluationResult;
        }

#region SERIALIZE
        /// <summary>
        /// Deserializes Group node properties
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <param name="includeResult"></param>
        protected override void DeserializeProperties(XmlReader xr, bool includeResult)
        {
            base.DeserializeProperties(xr, includeResult);

            SetProperties();
        }
#endregion SERIALIZE

        /// <summary>
        /// Deep clone of the current node.
        /// </summary>
        /// <returns></returns>
        public override ExpressionNode DeepClone()
        {
            ExpressionNodeGroup clone = new ExpressionNodeGroup();
            clone.Add(this.Group.DeepClone());
            clone.SetTypeClass(this.TypeClass);
            return clone;
        }

        /// <summary>
        /// Converts Group to FilterNodeGrop
        /// </summary>
        /// <returns></returns>
        protected override SFC.FilterNode DoConvertToFilterNode()
        {
            SFC.FilterNode node = Group.ConvertToFilterNode();

            SFC.FilterNodeGroup fnode = new SFC.FilterNodeGroup(node);
            return fnode;
        }

    }

    /// <summary>
    /// Operator node - boolean function with 2 arguments
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeOperator : ExpressionNodeChildren
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ExpressionNodeOperator");
        private const string cXmlTagOpType = "OpType";

        private OperatorType mOpType;

        /// <summary>
        /// Operator objectTypeName
        /// </summary>
        public OperatorType OpType
        {
            get { return mOpType; }
        }

        /// <summary>
        /// Default constructor for deserialization
        /// </summary>
        internal ExpressionNodeOperator()
        {
            SetNodeType(ExpressionNodeType.Operator);
            ChildrenList = new List<ExpressionNode>(2);
        }

        /// <summary>
        /// Deep clone of the current node.
        /// </summary>
        /// <returns></returns>
        public override ExpressionNode DeepClone()
        {
            ExpressionNodeOperator clone = new ExpressionNodeOperator();
            clone.mOpType = this.mOpType;
            clone.LastEvaluationResult = this.LastEvaluationResult;
            clone.Tag = this.Tag;
            clone.SetTypeClass(this.TypeClass);

            clone.Add(this.Left.DeepClone());
            clone.Add(this.Right.DeepClone());

            return clone;
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        private bool ValidOperandNode(ExpressionNode node)
        {
            return (node.Type == ExpressionNodeType.Constant
                || node.Type == ExpressionNodeType.Function
                || node.Type == ExpressionNodeType.Attribute);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void SetProperties()
        {
            // Scripting
            SetHasScript(Left.HasScript | Right.HasScript);


            //FilterNode compatibility

            // Expression is compatible if Operator is compatible and its left and right branches are compatible
            // We only need to check complex nodes - operators, groups and functions
            // simple nodes alwasy return false as they can't constitute filter node by themselves

            SetFilterNodeCompatible(operatorsFilterNode.Contains(this.OpType));

            if (FilterNodeCompatible)
            {
                if (Left.Type == ExpressionNodeType.Group || Left.Type == ExpressionNodeType.Operator || Left.Type == ExpressionNodeType.Function)
                {
                    SetFilterNodeCompatible(Left.FilterNodeCompatible);
                }

                if (FilterNodeCompatible && (Right.Type == ExpressionNodeType.Group || Right.Type == ExpressionNodeType.Operator || Right.Type == ExpressionNodeType.Function))
                {
                    SetFilterNodeCompatible(Right.FilterNodeCompatible);
                }
            }


            // NameConditionType
            NameConditionType nType = NameConditionType.None;

            switch (OpType)
            {
                case OperatorType.EQ:
                    nType = NameConditionType.Equal;
                    break;
                case OperatorType.LIKE:
                    nType = NameConditionType.Like;
                    break;
                case OperatorType.NE:
                    nType = NameConditionType.NotEqual;
                    break;
                case OperatorType.NOT_LIKE:
                    nType = NameConditionType.NotLike;
                    break;
                default:
                    // nothing to set - keep default values for NameType and ObjectName
                    return;
            }

            if (Left.Type == ExpressionNodeType.Attribute)
            {
                if (((ExpressionNodeAttribute)Left).Name == "Name"
                    && Right.Type == ExpressionNodeType.Constant)
                {
                    if (((ExpressionNodeConstant)Right).Value.GetType() == typeof(string))
                    {
                        SetObjectName((String)((ExpressionNodeConstant)Right).Value);
                        SetNameConditionType(nType);
                    }
                }
            }
        }



        /// <summary>
        /// Creates Operator node
        /// </summary>
        /// <param name="type">Operator type</param>
        /// <param name="left">Left node</param>
        /// <param name="right">Right node</param>
        public ExpressionNodeOperator(OperatorType type, ExpressionNode left, ExpressionNode right)
        {
            traceContext.TraceMethodEnter("ExpressionNodeOperator");
            traceContext.TraceParameters(type);

            if (type == OperatorType.NONE)
            {
                throw new ArgumentException(ExceptionTemplatesSR.InvalidArgument(type.ToString()));
            }

            if (null == left || null == right)
            {
                throw new ArgumentNullException();
            }

            if (type == OperatorType.AND || type == OperatorType.OR)
            {
                // Logical operators are only allowed between operators

                if ((left.Type != ExpressionNodeType.Operator && left.Type != ExpressionNodeType.Group)
                    || (right.Type != ExpressionNodeType.Operator && right.Type != ExpressionNodeType.Group))
                {
                    throw traceContext.TraceThrow(new OperatorNotApplicableException(type.ToString(), left.GetType().ToString()));
                }
            }
            else if (type == OperatorType.IN || type == OperatorType.NOT_IN)
            {
                // left could be Property, Function or Constant
                // right has to be an Array Function or TypeClass Array Attribute

                if (!((right.Type == ExpressionNodeType.Attribute && right.TypeClass == TypeClass.Array) || (right.Type == ExpressionNodeType.Function)))
                {
                    throw traceContext.TraceThrow(new InvalidInOperatorException(OperatorTypeToString(type)));
                }

                if (!ValidOperandNode(left))
                {
                    throw traceContext.TraceThrow(new InvalidOperandException(left.Type.ToString(), ExceptionTemplatesSR.LeftOperand));
                }

                if (right.Type == ExpressionNodeType.Function)
                {
                    ExpressionNodeFunction fn = (ExpressionNodeFunction)right;
                    if (fn.FunctionType != ExpressionNodeFunction.Function.Array)
                    {
                        throw traceContext.TraceThrow(new InvalidInOperatorException(OperatorTypeToString(type)));
                    }

                    // Try to match left type with array members type
                    foreach (ExpressionNode child in fn.EnumChildren())
                    {
                        // see if we can reason about type compatibility 
                        if (child.TypeClass != TypeClass.Unsupported && left.TypeClass != TypeClass.Unsupported)
                        {
                            if (child.TypeClass != left.TypeClass)
                            {
                                throw traceContext.TraceThrow(new ExpressionTypeMistmatchException(left.TypeClass.ToString(), child.TypeClass.ToString()));
                            }
                        }

                        // Only need to check the first child
                        break;
                    }
                }
            }
            else
            {
                // Everything else could have Property, Function or Constant on either side

                if (!ValidOperandNode(left))
                {
                    throw traceContext.TraceThrow(new InvalidOperandException(left.GetType().Name, ExceptionTemplatesSR.LeftOperand));
                }
                if (!ValidOperandNode(right))
                {
                    throw traceContext.TraceThrow(new InvalidOperandException(right.GetType().Name, ExceptionTemplatesSR.RightOperand));
                }

                if (left.TypeClass != TypeClass.Unsupported)
                {
                    if (right.TypeClass != TypeClass.Unsupported && right.TypeClass != TypeClass.Variant && left.TypeClass != TypeClass.Variant && right.TypeClass != left.TypeClass)
                    {
                        throw traceContext.TraceThrow(new ExpressionTypeMistmatchException(left.TypeClass.ToString(), right.TypeClass.ToString()));
                    }

                    if (!EvaluationFactory.SupportedOperators(left.TypeClass).Contains(type))
                    {
                        throw traceContext.TraceThrow(new OperatorNotApplicableException(type.ToString(), left.TypeClass.ToString()));
                    }
                }
                else if (right.TypeClass != TypeClass.Unsupported)
                {
                    if (!EvaluationFactory.SupportedOperators(right.TypeClass).Contains(type))
                    {
                        throw traceContext.TraceThrow(new OperatorNotApplicableException(type.ToString(), right.TypeClass.ToString()));
                    }
                }
            }

            SetNodeType(ExpressionNodeType.Operator);
            mOpType = type;
            SetTypeClass(TypeClass.Bool);    // Operators are always bool
            ChildrenList = new List<ExpressionNode>(2);
            ChildrenList.Add(left);
            ChildrenList.Add(right);

            SetProperties();

            traceContext.TraceMethodExit("ExpressionNodeOperator");
        }

        /// <summary>
        /// Left operand node
        /// </summary>
        public ExpressionNode Left
        {
            get { return ChildrenList[0]; }
            set
            {
                traceContext.TraceVerbose("Setting Left to: {0}", value);
                ChildrenList[0] = value;
            }
        }

        /// <summary>
        /// Right operand node
        /// </summary>
        public ExpressionNode Right
        {
            get { return ChildrenList[1]; }
            set
            {
                traceContext.TraceVerbose("Setting Right to: {0}", value);
                ChildrenList[1] = value;
            }
        }

#region TOSTRING
        /// <summary>
        /// Symbolic repesentation of OperatorType (T-Sql style)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static string OperatorTypeToString(OperatorType type)
        {
            switch (type)
            {
                case OperatorType.AND:
                    return "AND";
                case OperatorType.EQ:
                    return "=";
                case OperatorType.GE:
                    return ">=";
                case OperatorType.GT:
                    return ">";
                case OperatorType.IN:
                    return "IN";
                case OperatorType.LE:
                    return "<=";
                case OperatorType.LIKE:
                    return "LIKE";
                case OperatorType.LT:
                    return "<";
                case OperatorType.NE:
                    return "!=";
                case OperatorType.OR:
                    return "OR";
                case OperatorType.NOT_IN:
                    return "NOT IN";
                case OperatorType.NOT_LIKE:
                    return "NOT LIKE";
                default:
                    return String.Empty;
            }
        }

        /// <summary>
        /// Returns OperatorType for given string representation (opposite to OperatorTypeToString)
        /// </summary>
        /// <param name="opType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">No match is found for given string</exception>
        public static OperatorType OperatorTypeFromString(string opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("OperatorTypeFromString", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                switch (opType)
                {
                    case "AND":
                        return OperatorType.AND;
                    case "=":
                        return OperatorType.EQ;
                    case ">=":
                        return OperatorType.GE;
                    case ">":
                        return OperatorType.GT;
                    case "IN":
                        return OperatorType.IN;
                    case "<=":
                        return OperatorType.LE;
                    case "LIKE":
                        return OperatorType.LIKE;
                    case "<":
                        return OperatorType.LT;
                    case "!=":
                        return OperatorType.NE;
                    case "OR":
                        return OperatorType.OR;
                    case "NOT IN":
                        return OperatorType.NOT_IN;
                    case "NOT LIKE":
                        return OperatorType.NOT_LIKE;
                    default:
                        throw methodTraceContext.TraceThrow(new ArgumentException("opType"));
                }
            }
        }

        /// <summary>
        /// Represents Expression as a string in T-SQL like syntax
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Left.ToString());
            sb.Append(" ");
            sb.Append(OperatorTypeToString(OpType));
            sb.Append(" ");
            sb.Append(Right.ToString());

            return sb.ToString();
        }

#endregion TOSTRING

#region EQUALS
        /// <summary>
        /// Operator specific type comparison
        /// type and nullability of comparison object checked by caller (Equals)
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override bool EqualProperties(object obj)
        {
            bool ret = false;

            ret = (this.OpType == ((ExpressionNodeOperator)obj).OpType &&
                this.Left.Equals(((ExpressionNodeOperator)obj).Left) &&
                this.Right.Equals(((ExpressionNodeOperator)obj).Right));

            return ret;
        }
#endregion EQUALS

#region EVALUATE

        /// <summary>
        /// 
        /// </summary>
        internal override void ResetResult()
        {
            LastEvaluationResult = null;
            base.ResetResult();
        }

        /// <summary>
        /// Compares left and right nodes according to operator's type
        /// </summary>
        /// <param name="context">Management Facet context</param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns>boolean result</returns>
        internal override object DoEvaluate(FacetEvaluationContext context, bool checkSqlScriptAsProxy)
        {
            if (mOpType == OperatorType.NONE)
            {
                throw traceContext.TraceThrow(new BadExpressionTreeException(ExceptionTemplatesSR.EvaluatingOperatorNone));
            }

            bool ret = false;
            object left, right;

            left = Left.DoEvaluate(context, checkSqlScriptAsProxy);
            right = Right.DoEvaluate(context, checkSqlScriptAsProxy);

            // Check if we have an enumeration on either side
            // Bitmapped enumeration operations require special handling.
            // The bitmapped enum comparison is a bitwise AND followed by the requested operations:
            //      a & const == const
            // or   a & const != const
            // So, since we need to know which side is the enum constant to compare to and which 
            // is the side we are checking (i.e., even though the operation is EQ/NEQ, it is not commutative)
            //
            // If there are no constant operands, or if both are constants then the bitwise operation is unnecessary and we
            // will let the standard evaluation continue
            if (left.GetType().IsBitmappedEnum() || right.GetType().IsBitmappedEnum())
            {
                // Now, if an enum constant exists, change the operation type, and make 
                // sure the constant is the right operand
                bool leftIsConstant = false;
                bool rightIsConstant = false;

                // Find the constant
                // This can be an outright constant node, or the result of the Enum() function
                if (Left.Type == ExpressionNodeType.Constant ||
                    (Left.Type == ExpressionNodeType.Function &&
                     ((ExpressionNodeFunction)Left).FunctionType == ExpressionNodeFunction.Function.Enum))
                {
                    leftIsConstant = true;
                }
                if (Right.Type == ExpressionNodeType.Constant ||
                    (Right.Type == ExpressionNodeType.Function &&
                     ((ExpressionNodeFunction)Right).FunctionType == ExpressionNodeFunction.Function.Enum))
                {
                    rightIsConstant = true;
                }

                // Check if exactly one of the operands is a constant
                if (leftIsConstant ^ rightIsConstant)
                { 
                    //we will have a bitmapped enum comparison,
                    //switch the operation type
                    switch (mOpType)
                    {
                        case OperatorType.EQ:
                            mOpType = OperatorType.BEQ;
                            break;

                        case OperatorType.NE:
                            mOpType = OperatorType.BNE;
                            break;

                        default:
                            break;
                    }

                    if (leftIsConstant)
                    {
                        //swap the operands to put the constant on the right
                        ExpressionNode temp = Right;
                        Right = Left;
                        Left = temp;

                    }
                }
            }

            ret = EvaluationFactory.Evaluate(left, right, mOpType, context);

            LastEvaluationResult = ret;
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationList"></param>
        internal override void AnalyzeForConfiguration(List<ConfigurationItem> configurationList)
        {
            if (null == GetResult() || true == (bool)GetResult())
            {
                // If operator succeeded or never evaluated, there is nothing to configure below it
                return;
            }

            if (mOpType == OperatorType.OR)
            {
                throw traceContext.TraceThrow(new ExpressionNodeNotConfigurableException(OperatorTypeToString(mOpType)));
            }

            // We can set left-hand Attribute to a right-hand value (from Attribute, Function or Constant)

            if (Left.Type == ExpressionNodeType.Attribute)
            {
                if (mOpType != OperatorType.EQ)
                {
                    throw traceContext.TraceThrow(new ExpressionNodeNotConfigurableOperatorException(((ExpressionNodeAttribute)Left).Name, this.ToString()));
                }

                traceContext.DebugAssert(Right.Type == ExpressionNodeType.Constant || Right.Type == ExpressionNodeType.Function || Right.Type == ExpressionNodeType.Attribute,
                    "Unsupported node structure");

                configurationList.Add(new ConfigurationItem(((ExpressionNodeAttribute)Left).Name, Right.GetResult()));

                return;
            }
            else if ((Left.Type == ExpressionNodeType.Operator || Left.Type == ExpressionNodeType.Group))
            {
                Left.AnalyzeForConfiguration(configurationList);

                traceContext.DebugAssert(Right.Type == ExpressionNodeType.Operator || Right.Type == ExpressionNodeType.Group,
                    "Unsupported node structure");

                Right.AnalyzeForConfiguration(configurationList);
            }
            else
            {
                // Left is Function or Constant - can't configure
                throw traceContext.TraceThrow(new ExpressionNodeNotConfigurableException());
            }
        }

#endregion EVALUATE

#region SERIALIZE
        /// <summary>
        /// Serializes Operator node properties
        /// </summary>
        /// <param name="xw"></param>
        /// <param name="includeResult"></param>
        protected override void SerializeProperties(XmlWriter xw, bool includeResult)
        {
            xw.WriteElementString(cXmlTagOpType, mOpType.ToString());

            if (includeResult)
            {
                SerializeResult(xw);
            }

            base.SerializeProperties(xw, includeResult);
        }

        /// <summary>
        /// Deserializes Operator node properties
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <param name="includeResult"></param>
        protected override void DeserializeProperties(XmlReader xr, bool includeResult)
        {
            List<string> vals = ReadNodeWithCheck(xr, cXmlTagOpType);

            OperatorType type = MatchType<OperatorType>(vals[0]);
            this.mOpType = type;

            if (includeResult)
            {
                DeserializeResult(xr);
            }

            base.DeserializeProperties(xr, includeResult);

            SetProperties();
        }
#endregion SERIALIZE

#region FilterNode conversion
        // These list are intended for FilterNode conversion
        // They include only types supported by FilterNode
        internal static readonly List<Type> numericTypes = new List<Type>(7);
        internal static readonly List<Type> stringTypes = new List<Type>(4);

        internal static readonly List<OperatorType> operatorsFilterNode = new List<OperatorType>(8);

        static ExpressionNodeOperator()
        {
            numericTypes.Add(typeof(int));
            numericTypes.Add(typeof(double));
            numericTypes.Add(typeof(byte));
            numericTypes.Add(typeof(long));
            numericTypes.Add(typeof(short));
            numericTypes.Add(typeof(float));
            numericTypes.Add(typeof(decimal)); // decimal and float should be treated the same as Numeric type see vsts#217413 for details. -anchals

            stringTypes.Add(typeof(string));
            stringTypes.Add(typeof(char));
            stringTypes.Add(typeof(DateTime));
            stringTypes.Add(typeof(Guid));

            operatorsFilterNode.Add(OperatorType.EQ);
            operatorsFilterNode.Add(OperatorType.GE);
            operatorsFilterNode.Add(OperatorType.GT);
            operatorsFilterNode.Add(OperatorType.LE);
            operatorsFilterNode.Add(OperatorType.LT);
            operatorsFilterNode.Add(OperatorType.NE);
            operatorsFilterNode.Add(OperatorType.AND);
            operatorsFilterNode.Add(OperatorType.OR);
            operatorsFilterNode.Add(OperatorType.LIKE);
            operatorsFilterNode.Add(OperatorType.NOT_LIKE);
        }

        /// <summary>
        /// Returns a list of operators supported for the given type in Filters
        /// (Filters are more restrictive than Condition expressions)
        /// Empty list if evaluation for the type is not supported
        /// </summary>
        /// <param name="type"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static List<OperatorType> SupportedFilterOperators(Type type, AutomatedPolicyEvaluationMode mode)
        {
            // $FUTURE
            // Leaving signature of the method as it is
            // but always returning EQ only regardless of the mode 
            // Will address this in the future

            List<OperatorType> list = new List<OperatorType>(1);
            list.Add(OperatorType.EQ);
            return list;
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static OperatorType ConvertType(SFC.FilterNodeOperator.Type ftype)
        {
            switch (ftype)
            {
                case SFC.FilterNodeOperator.Type.And:
                    return OperatorType.AND;
                case SFC.FilterNodeOperator.Type.EQ:
                    return OperatorType.EQ;
                case SFC.FilterNodeOperator.Type.GE:
                    return OperatorType.GE;
                case SFC.FilterNodeOperator.Type.GT:
                    return OperatorType.GT;
                case SFC.FilterNodeOperator.Type.LE:
                    return OperatorType.LE;
                case SFC.FilterNodeOperator.Type.LT:
                    return OperatorType.LT;
                case SFC.FilterNodeOperator.Type.NE:
                    return OperatorType.NE;
                case SFC.FilterNodeOperator.Type.OR:
                    return OperatorType.OR;
                default:
                    traceContext.DebugAssert(false, String.Format("Converting FilterNode to ExpressionNode - Unrecognized OperatorType '{0}'", ftype.ToString()));
                    break;
            }

            return OperatorType.NONE;
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static SFC.FilterNodeOperator.Type ConvertType(OperatorType type)
        {
            switch (type)
            {
                case OperatorType.AND:
                    return SFC.FilterNodeOperator.Type.And;
                case OperatorType.EQ:
                    return SFC.FilterNodeOperator.Type.EQ;
                case OperatorType.GE:
                    return SFC.FilterNodeOperator.Type.GE;
                case OperatorType.GT:
                    return SFC.FilterNodeOperator.Type.GT;
                case OperatorType.LE:
                    return SFC.FilterNodeOperator.Type.LE;
                case OperatorType.LT:
                    return SFC.FilterNodeOperator.Type.LT;
                case OperatorType.NE:
                    return SFC.FilterNodeOperator.Type.NE;
                case OperatorType.OR:
                    return SFC.FilterNodeOperator.Type.OR;
                default:
                    throw traceContext.TraceThrow(new ConversionNotSupportedException(ExceptionTemplatesSR.Operator, type.ToString()));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SFC.FilterNode DoConvertToFilterNode()
        {
            if (false == operatorsFilterNode.Contains(OpType))
            {
                throw traceContext.TraceThrow(new ConversionNotSupportedException(ExceptionTemplatesSR.Operator, OpType.ToString()));
            }

            // deal with operators that are converted into functions here
            switch (OpType)
            {
                case OperatorType.LIKE:
                    return new Microsoft.SqlServer.Management.Sdk.Sfc.FilterNodeFunction(
                        Microsoft.SqlServer.Management.Sdk.Sfc.FilterNodeFunction.Type.Like,
                        "like",
                        Left.ConvertToFilterNode(),
                        Right.ConvertToFilterNode());
                case OperatorType.NOT_LIKE:
                    return new Microsoft.SqlServer.Management.Sdk.Sfc.FilterNodeFunction(
                        Microsoft.SqlServer.Management.Sdk.Sfc.FilterNodeFunction.Type.Not,
                        "not",
                        new Microsoft.SqlServer.Management.Sdk.Sfc.FilterNodeFunction(
                            Microsoft.SqlServer.Management.Sdk.Sfc.FilterNodeFunction.Type.Like,
                                "like",
                                Left.ConvertToFilterNode(),
                                Right.ConvertToFilterNode()));
            }

            SFC.FilterNodeOperator.Type fOpType = ConvertType(OpType);
            SFC.FilterNode left = Left.ConvertToFilterNode();
            SFC.FilterNode right = Right.ConvertToFilterNode();

            SFC.FilterNodeOperator fnode = new SFC.FilterNodeOperator(fOpType, left, right);
            return fnode;
        }
#endregion FilterNode conversion
    }

    // Proxy class to provide access to SMO's GetServerObject method
    // TODO:  Replace this with the appropriate SFC mechanism when available
    [STraceConfigurationAttribute(SkipAutoTrace = true)]
    internal sealed class DmfSqlSmoObjectProxy : SMO.SqlSmoObject
    {
        internal static SMO.Server GetServerObj(SMO.SqlSmoObject smoObj)
        {
            return smoObj.GetServerObject();
        }
    }

    /// <summary>
    /// Function - returns object for given set of arguments
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeFunction : ExpressionNodeChildren
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ExpressionNodeFunction");
        private const string cXmlTagFuncType = "FunctionType";
        private const string cXmlTagReturnType = "ReturnType";

        /// <summary>
        /// Type of Function
        /// </summary>
        public enum Function
        {
            /// <summary>
            /// Execute scalar SQL
            /// </summary>
            ExecuteSql,
            /// <summary>
            /// Execute WMI query
            /// </summary>
            ExecuteWql,
            /// <summary>
            /// Get current date
            /// </summary>
            GetDate,
            /// <summary>
            /// Add a number to a date
            /// </summary>
            DateAdd,
            /// <summary>
            /// Extract a part of a date
            /// </summary>
            DatePart,
            /// <summary>
            /// Sum a series of values
            /// </summary>
            Sum,
            /// <summary>
            /// Average a series of values
            /// </summary>
            Avg,
            /// <summary>
            /// Count a series of values
            /// </summary>
            Count,
            /// <summary>
            /// Get the length of a string
            /// </summary>
            Len,
            /// <summary>
            /// Substitute a value for null 
            /// </summary>
            IsNull,
            /// <summary>
            /// Return an array of values
            /// </summary>
            Array,
            /// <summary>
            /// Add two values
            /// </summary>
            Add,
            /// <summary>
            /// Substract two values
            /// </summary>
            Subtract,
            /// <summary>
            /// Multiple two values
            /// </summary>
            Multiply,
            /// <summary>
            /// Divide one value by another
            /// </summary>
            Divide,
            /// <summary>
            /// Bitwise AND
            /// </summary>
            BitwiseAnd,
            /// <summary>
            /// Bitwise OR
            /// </summary>
            BitwiseOr,
            /// <summary>
            /// Raise a value to an exponential power
            /// </summary>
            Power,
            /// <summary>
            /// Return the modulus of one number divided by another
            /// </summary>
            Mod,
            /// <summary>
            /// Round a number
            /// </summary>
            Round,
            /// <summary>
            /// Return the textual description for an enum
            /// </summary>
            Enum,
            /// <summary>
            /// Return a datetime from a string
            /// </summary>
            DateTime,
            /// <summary>
            /// Convert a value to a string
            /// </summary>
            String,
            /// <summary>
            /// Logical true
            /// </summary>
            True,
            /// <summary>
            /// Logical false
            /// </summary>
            False,
            /// <summary>
            /// Return guid from string
            /// </summary>
            Guid,
            /// <summary>
            /// Return upper-case string
            /// </summary>
            Upper,
            /// <summary>
            /// Return lower-case string
            /// </summary>
            Lower,
            /// <summary>
            /// Concatenate strings
            /// </summary>
            /// Defect 247787, Ability to pass dynamic Arguments to ExecuteWQL
            Concatenate,
            /// <summary>
            /// Escape string
            /// Defect 247787, Ability to pass dynamic Arguments to ExecuteWQL
            /// </summary>
            Escape,
        }

        private Function mFunctionType;
        private TypeClass mReturnType;
        private List<object> mArgs = new List<object>();

        /// <summary>
        /// Type of Function
        /// </summary>
        public Function FunctionType { get { return mFunctionType; } }

        /// <summary>
        /// Return type
        /// </summary>
        public TypeClass ReturnType { get { return mReturnType; } }

        // Function definitions: function type, return type, parameters
        // Return type is first type in type array. Parameters (if any) follow it
        static readonly Dictionary<ExpressionNodeFunction.Function, TypeClass[]> functionDefs =
            new Dictionary<ExpressionNodeFunction.Function, TypeClass[]>(25);

        static ExpressionNodeFunction()
        {
            // If a function that return Variant (return type depends on the arguments)
            // has its first argument signature as String, that argument is a return type - example: ExecuteSql
            // If return type is derived from the type of the argument, the signature of the argument is Variant - example: IsNull

            functionDefs.Add(Function.ExecuteSql, new TypeClass[] { TypeClass.Variant, TypeClass.String, TypeClass.String });
            functionDefs.Add(Function.ExecuteWql, new TypeClass[] { TypeClass.Variant, TypeClass.String, TypeClass.String, TypeClass.String });
            functionDefs.Add(Function.GetDate, new TypeClass[] { TypeClass.DateTime });
            functionDefs.Add(Function.DateAdd, new TypeClass[] { TypeClass.DateTime, TypeClass.String, TypeClass.Numeric, TypeClass.DateTime });
            functionDefs.Add(Function.DatePart, new TypeClass[] { TypeClass.Numeric, TypeClass.String, TypeClass.DateTime });
            functionDefs.Add(Function.Len, new TypeClass[] { TypeClass.Numeric, TypeClass.String });
            functionDefs.Add(Function.Add, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.Subtract, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.Multiply, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.Divide, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.BitwiseAnd, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.BitwiseOr, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.Power, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.Mod, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.Round, new TypeClass[] { TypeClass.Numeric, TypeClass.Numeric, TypeClass.Numeric });
            functionDefs.Add(Function.Array, new TypeClass[] { TypeClass.Array, TypeClass.VarArgs });
            functionDefs.Add(Function.Sum, new TypeClass[] { TypeClass.Numeric, TypeClass.VarArgs });
            functionDefs.Add(Function.Avg, new TypeClass[] { TypeClass.Numeric, TypeClass.VarArgs });
            functionDefs.Add(Function.Count, new TypeClass[] { TypeClass.Numeric, TypeClass.VarArgs });
            functionDefs.Add(Function.IsNull, new TypeClass[] { TypeClass.Variant, TypeClass.Variant, TypeClass.Variant });
            functionDefs.Add(Function.Enum, new TypeClass[] { TypeClass.Numeric, TypeClass.String, TypeClass.String });
            functionDefs.Add(Function.False, new TypeClass[] { TypeClass.Bool });
            functionDefs.Add(Function.True, new TypeClass[] { TypeClass.Bool });
            functionDefs.Add(Function.DateTime, new TypeClass[] { TypeClass.DateTime, TypeClass.String });
            functionDefs.Add(Function.String, new TypeClass[] { TypeClass.String, TypeClass.Variant });
            functionDefs.Add(Function.Guid, new TypeClass[] { TypeClass.Guid, TypeClass.String });
            functionDefs.Add(Function.Upper, new TypeClass[] { TypeClass.String, TypeClass.String });
            functionDefs.Add(Function.Lower, new TypeClass[] { TypeClass.String, TypeClass.String });
            functionDefs.Add(Function.Concatenate, new TypeClass[] { TypeClass.String, TypeClass.String, TypeClass.String });
            functionDefs.Add(Function.Escape, new TypeClass[] { TypeClass.String, TypeClass.String, TypeClass.String, TypeClass.String });
        }

        /// <summary>
        /// Function definition dictionary for consumption by the GUI 
        /// </summary>
        public static Dictionary<ExpressionNodeFunction.Function, TypeClass[]> FunctionsDefinitions
        {
            get
            {
                return functionDefs;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void SetProperties()
        {
            // FilterNodeCompatibility
            //
            // Four functions are filternode compatible: 
            // True, False, Enum and String
            // All of them have simple arguments, except the string function, for which an argument
            // can be any expression that results in a variant convertible to a string.
            // So, we need to confirm that the argument of the function is also filternode compatible.
            // Of the possible parameters to string(), only a constant and an attribute are compatible
            bool compatibleStringNode = this.FunctionType == Function.String &&
                                        (this.ChildrenList[0].Type == ExpressionNodeType.Constant ||
                                         this.ChildrenList[0].Type == ExpressionNodeType.Attribute);
            SetFilterNodeCompatible(this.FunctionType == Function.True || this.FunctionType == Function.False || 
                                    this.FunctionType == Function.Enum || compatibleStringNode);
                                    
            // Scripting
            if (mFunctionType == Function.ExecuteSql || mFunctionType == Function.ExecuteWql)
            {
                SetHasScript(true);
            }
            else
            {
                foreach (ExpressionNode node in EnumerableChildrenList)
                {
                    if (node.HasScript)
                    {
                        SetHasScript(node.HasScript);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Default constructor for deserialization
        /// </summary>
        internal ExpressionNodeFunction()
        {
            SetNodeType(ExpressionNodeType.Function);
            mReturnType = TypeClass.Unsupported;
            ChildrenList = new List<ExpressionNode>();
        }

        /// <summary>
        /// Creates Function node
        /// </summary>
        /// <param name="functionType">Function type</param>
        /// <param name="args">Function arguments</param>
        public ExpressionNodeFunction(Function functionType, params ExpressionNode[] args)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ExpressionNodeFunction", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(functionType, args);
                if (args == null)
                {
                    // This function asks for the length of args, thus it should not be null
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("args"));
                }

                int signatureArgs = functionDefs[mFunctionType].Length - 1;

                // Initialize the object
                SetNodeType(ExpressionNodeType.Function);
                mFunctionType = functionType;
                mReturnType = functionDefs[mFunctionType][0];
                ChildrenList = new List<ExpressionNode>(args.Length);

                // if return type is not set in the defs table, assume it is passed as the first arg
                if (mReturnType == TypeClass.Variant)
                {
                    if (args.Length == 0)
                    {
                        throw methodTraceContext.TraceThrow(new FunctionWrongArgumentsNumberException(mFunctionType.ToString(), args.Length, signatureArgs));
                    }

                    // if the first agrument of a variant function is string it is its return type
                    if (functionDefs[mFunctionType][1] == TypeClass.String)
                    {
                        if (args[0].Type != ExpressionNodeType.Constant || args[0].TypeClass != TypeClass.String)
                        {
                            throw methodTraceContext.TraceThrow(new FunctionWrongArgumentTypeException(mFunctionType.ToString(), args[0].TypeClass.ToString(), TypeClass.String.ToString()));
                        }

                        string typeName = (string)((ExpressionNodeConstant)args[0]).Value;
                        mReturnType = (TypeClass)Enum.Parse(typeof(TypeClass), typeName, true);
                    }
                }

                if (signatureArgs > 0 && args.Length > 0)
                {
                    bool isVarArgs = (functionDefs[mFunctionType][1] == TypeClass.VarArgs);
                    if (!isVarArgs && args.Length != functionDefs[mFunctionType].Length - 1)
                    {
                        throw methodTraceContext.TraceThrow(new FunctionWrongArgumentsNumberException(mFunctionType.ToString(), args.Length, functionDefs[mFunctionType].Length - 1));
                    }

                    TypeClass varArgClass = TypeClass.Unsupported;

                    for (int i = 0; i < args.Length; ++i)
                    {
                        ExpressionNode node = args[i] as ExpressionNode;
                        if (null == node)
                        {
                            throw methodTraceContext.TraceThrow(new ArgumentNullException());
                        }

                        // Verify signature
                        // We have to account for Unsupported in case we get unverified Attribute
                        //
                        if (isVarArgs || functionDefs[mFunctionType][i + 1] == TypeClass.Variant)
                        {
                            // This is an array or the functions accepts different types
                            // we only can make sure that all arguments are of the same type

                            if (varArgClass == TypeClass.Unsupported)
                            {
                                if (node.TypeClass != TypeClass.Unsupported)
                                {
                                    varArgClass = node.TypeClass;

                                    if (mReturnType == TypeClass.Variant)
                                    {
                                        // If function accetps variant and return variant derive return type from the first known type argument
                                        mReturnType = varArgClass;
                                    }
                                }
                            }
                            else
                            {
                                if (node.TypeClass != TypeClass.Unsupported && node.TypeClass != varArgClass)
                                {
                                    throw methodTraceContext.TraceThrow(new FunctionWrongArgumentTypeException(mFunctionType.ToString(), node.TypeClass.ToString(), varArgClass.ToString()));
                                }
                            }
                        }
                        else
                        {
                            if (node.TypeClass != TypeClass.Unsupported && node.TypeClass != functionDefs[mFunctionType][i + 1])
                            {
                                throw methodTraceContext.TraceThrow(new FunctionWrongArgumentTypeException(mFunctionType.ToString(), node.TypeClass.ToString(), (functionDefs[mFunctionType][i + 1]).ToString()));
                            }
                        }

                        Add(node);
                    }
                }
                else if (signatureArgs == 0 && args.Length > 0)
                {
                    throw methodTraceContext.TraceThrow(new FunctionWrongArgumentsNumberException(mFunctionType.ToString(), args.Length, signatureArgs));
                }

                SetTypeClass(mReturnType);

                SetProperties();
            }
        }

        /// <summary>
        /// Represents Expression as a string in T-SQL like syntax
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = FunctionType.ToString() + base.ToString();

            return str;
        }

        /// <summary>
        /// A special method to display some simple functions in UI in simplified form,
        /// cannot be parsed back
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override string ToStringForDisplay()
        {
            // Verify that children are constants only
            //
            foreach (ExpressionNode node in EnumChildren())
            {
                if (node.Type != ExpressionNodeType.Constant)
                {
                    return null;
                }
            }

            switch (this.FunctionType)
            {
                case Function.True:
                    return "True";
                case Function.False:
                    return "False";
                case Function.Enum:
                    // return second arg - value name
                    return ((ExpressionNodeConstant)ChildrenList[1]).Value.ToString();
                case Function.DateTime:
                    return ((DateTime)Evaluate (null, false)).ToString ("o", CultureInfo.CurrentCulture);
                case Function.Guid:
                    return ((ExpressionNodeConstant)ChildrenList[0]).Value.ToString();
                default:
                    return null;
            }
        }

#region internal utilities

#endregion
#region EQUALS
        /// <summary>
        /// Function specific type comparison
        /// type and nullability of comparison object checked by caller (Equals)
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns></returns>
        protected override bool EqualProperties(object obj)
        {
            ExpressionNodeFunction fn = (ExpressionNodeFunction)obj;

            return (this.FunctionType == fn.FunctionType &&
                this.TypeClass == fn.TypeClass &&
                base.EqualProperties(fn));

        }
#endregion EQUALS

#region EVALUATE
        /// <summary>
        /// 
        /// </summary>
        internal override void ResetResult()
        {
            LastEvaluationResult = null;
            base.ResetResult();
        }

        private void GetParameters(FacetEvaluationContext context, Function funcType, bool checkSqlScriptAsProxy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetParameters"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(context, funcType, checkSqlScriptAsProxy);
                if (mArgs.Count > 0)
                {
                    mArgs.Clear();
                }

                // Check the number of arguments
                if ((functionDefs[funcType].Length == 1) // Only return type is specified, no parameters in the signature
                    || (functionDefs[funcType][1] != TypeClass.VarArgs))
                {
                    if (Count != functionDefs[funcType].Length - 1)
                    {
                        throw methodTraceContext.TraceThrow(new FunctionWrongArgumentsNumberException(funcType.ToString(), Count, functionDefs[funcType].Length - 1));
                    }

                    // Check the argument types (return type is offset 0, so start at 1)
                    for (int i = 1; i < functionDefs[funcType].Length; ++i)
                    {
                        // Cache the parameter value for later use
                        mArgs.Add(ChildrenList[i - 1].DoEvaluate(context, checkSqlScriptAsProxy));

                        // Check the parameter type against its expected type
                        if ((mArgs[i - 1] != null) && (functionDefs[funcType][i] != TypeClass.Variant) && (EvaluationFactory.ClassifyType(mArgs[i - 1])) != (functionDefs[funcType][i]))
                        {
                            throw methodTraceContext.TraceThrow(new FunctionWrongArgumentTypeException(funcType.ToString(), EvaluationFactory.ClassifyType(mArgs[i - 1].GetType()).ToString(), functionDefs[funcType][i].ToString()));
                        }
                    }
                }
                else //VarArgs
                {
                    TypeClass argType = TypeClass.Unsupported;

                    // Evaluate each parameter and cache it for later use
                    for (int i = 0; i < ChildrenList.Count; ++i)
                    {
                        object arg = ChildrenList[i].DoEvaluate(context, checkSqlScriptAsProxy);

                        // Cache the parameter value for later use
                        mArgs.Add(arg);

                        // For non-null args, check their type to be sure they match the other args
                        if (arg != null)
                        {
                            // Cache first arg type for comparison with the rest
                            if (argType == TypeClass.Unsupported)
                            {
                                argType = EvaluationFactory.ClassifyType(arg);
                            }

                            // Make sure the arg's type matches that of the first non-null arg--all arg types must be the same
                            if ((argType != TypeClass.Unsupported) && (argType != EvaluationFactory.ClassifyType(arg)))
                            {
                                throw methodTraceContext.TraceThrow(new FunctionWrongArgumentTypeException(funcType.ToString(), EvaluationFactory.ClassifyType(arg).ToString(), argType.ToString()));
                            }
                        }
                    }
                }
            }
        }

        FacetEvaluationContext mcontext;

        /// <summary>
        /// Calculates result of the function
        /// </summary>
        /// <param name="context">Management Facet context</param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns></returns>
        internal override object DoEvaluate(FacetEvaluationContext context, bool checkSqlScriptAsProxy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("DoEvaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(context, checkSqlScriptAsProxy);
                mcontext = context;

                object ret = null;

                GetParameters(mcontext, mFunctionType, checkSqlScriptAsProxy);

                switch (mFunctionType)
                {
                    case Function.ExecuteSql:
                        ret = EvaluateExecuteSqlScalar(checkSqlScriptAsProxy);
                        break;
                    case Function.ExecuteWql:
                        ret = EvaluateExecuteWqlScalar();
                        break;
                    case Function.GetDate:
                        ret = EvaluateGetDate();
                        break;
                    case Function.DateAdd:
                        ret = EvaluateDateAdd();
                        break;
                    case Function.DatePart:
                        ret = EvaluateDatePart();
                        break;
                    case Function.Len:
                        ret = EvaluateLen();
                        break;
                    case Function.Add:
                        ret = EvaluateAdd();
                        break;
                    case Function.Subtract:
                        ret = EvaluateSubtract();
                        break;
                    case Function.Multiply:
                        ret = EvaluateMultiply();
                        break;
                    case Function.Divide:
                        ret = EvaluateDivide();
                        break;
                    case Function.BitwiseAnd:
                        ret = EvaluateBitwiseAnd();
                        break;
                    case Function.BitwiseOr:
                        ret = EvaluateBitwiseOr();
                        break;
                    case Function.Power:
                        ret = EvaluatePower();
                        break;
                    case Function.Mod:
                        ret = EvaluateModulus();
                        break;
                    case Function.Round:
                        ret = EvaluateRound();
                        break;
                    case Function.Array:
                        ret = EvaluateArray();
                        break;
                    case Function.Sum:
                        ret = EvaluateSum();
                        break;
                    case Function.Avg:
                        ret = EvaluateAvg();
                        break;
                    case Function.Count:
                        ret = EvaluateCount();
                        break;
                    case Function.IsNull:
                        ret = EvaluateIsNull();
                        break;
                    case Function.Enum:
                        ret = EvaluateEnum();
                        break;
                    case Function.False:
                        ret = EvaluateFalse();
                        break;
                    case Function.True:
                        ret = EvaluateTrue();
                        break;
                    case Function.DateTime:
                        ret = EvaluateDateTime();
                        break;
                    case Function.String:
                        ret = EvaluateString();
                        break;
                    case Function.Guid:
                        ret = EvaluateGuid();
                        break;
                    case Function.Lower:
                        ret = EvaluateLower();
                        break;
                    case Function.Upper:
                        ret = EvaluateUpper();
                        break;
                    case Function.Concatenate:
                        ret = EvaluateConcatenate ();
                        break;
                    case Function.Escape:
                        ret = EvaluateEscape();
                        break;

                    default:
                        throw methodTraceContext.TraceThrow(new UnsupportedTypeException(typeof(Function).Name, mFunctionType.ToString()));
                }

                LastEvaluationResult = ret;
                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }

        /// <summary>
        /// To evaluate an ExecuteSql() expression we can have one of two modes:
        ///     1- The OnDemand mode, in which case the policy is executed in the caller's context.
        ///     2- The OnSchedule mode, in which case agent executes the policy in the checkSqlScriptAsProxy mode.
        ///        In this mode, we are supposed to do the evaluation in the context of the policy LPU ##MS_PolicyTsqlExecutionLogin##.
        /// </summary>
        /// <param name="checkSqlScriptAsProxy">true if we will impersonate the LPU ##MS_PolicyTsqlExecutionLogin## for evaluation.</param>
        /// <returns></returns>
        private object EvaluateExecuteSqlScalar(bool checkSqlScriptAsProxy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EvaluateExecuteSqlScalar"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(checkSqlScriptAsProxy);
                const int cTypeArgPos = 0;  // Position of arg containing type
                const int cSQLArgPos = 1;  // Position of arg containing SQL command to run

                // Be sure we have a SMO object - we need a Server to run the query
                SMO.SqlSmoObject smoobj = mcontext.PhysicalTarget as SMO.SqlSmoObject;

                if (null == smoobj)
                {
                    throw methodTraceContext.TraceThrow(new FunctionNotASmoObjectException(mcontext.Target.GetType().Name));
                }

                // Walk the object hierarchy as necessary to get the Server
                SMO.Server server = DmfSqlSmoObjectProxy.GetServerObj(smoobj);

                if (null == server)
                {
                    throw methodTraceContext.TraceThrow(new FunctionNoServerException());
                }

                ADO.SqlConnection conn = server.ConnectionContext.SqlConnectionObject;

                bool bOpened = false;

                if (conn.State != System.Data.ConnectionState.Open)
                {
                    conn.Open();
                    bOpened = true;  // We opened the connection; we need to close it
                }

                object ret = null;

                try
                {
                    string sqlstr = Evaluator.ConvertToString(mArgs[cSQLArgPos]);
                    //replace every double quote with a single one, this is necessary since the SFC expression parser treats single quotes as string delimiters also.
                    sqlstr = sqlstr.Replace("''", "'"); 
                    
                    //Then surround the whole script with a sql string quotation and escape single quotes remaining in the script.
                    sqlstr = SFC.SfcTsqlProcFormatter.MakeSqlString(sqlstr);

                    string paramDefinitionAndAssignment = null;

                    // See if the physical target is a SMO scriptable object
                    // because this is the common ancestor for all schema-based
                    // objects in SMO
                    SMO.ScriptSchemaObjectBase schemaobj = smoobj as SMO.ScriptSchemaObjectBase;

                    if (schemaobj != null)
                    {
                        //Set up the magic params.  Use two @s to distinguish them from regular variables and params
                        //We will pass these parameters to the sp_executesql call inside the script
                        paramDefinitionAndAssignment = ", N'@@ObjectName sysname, @@SchemaName sysname', @@ObjectName = " + SFC.SfcTsqlProcFormatter.MakeSqlString(schemaobj.Name) + ", @@SchemaName = " + SFC.SfcTsqlProcFormatter.MakeSqlString(schemaobj.Schema);
                    }

                    //we have to check the script length. Sql 2000 and below does not allow 
                    string userScriptDeclaration;
                    string serverVersion = conn.ServerVersion;
                    int versionMajor = Int32.Parse(serverVersion.Split(new char[] { '.' }, 2)[0]); //get the first number before the '.' in the version, this is the major version

                    if (versionMajor <= 8)
                    {
                        if (sqlstr.Length > 4000) //This is the maximum size of a script in an nvarchar variable.
                        {
                            traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.CantExecuteLongSqlScriptOn2000));
                        }

                        userScriptDeclaration = "DECLARE @@UserScript nvarchar(4000); ";
                    }
                    else
                    {
                        userScriptDeclaration = "DECLARE @@UserScript nvarchar(max); ";
                    }

                    //prepare the sp_executesql statement to execute the user's script
                    //if we don't have a schema bound object, the "paramDefinitionAndAssignment" will be null will not affect the final script called.
                    string executeSqlScript = userScriptDeclaration +
                                              "SET @@UserScript = " + sqlstr + "; " +
                                              "EXEC sp_executesql @@UserScript" + paramDefinitionAndAssignment + "; ";


                    ADO.SqlCommand cmd = new ADO.SqlCommand();
                    // Bugfix: 9855019 We don't have a way to propagate the command timeout from the UIConnectionInfo here,
                    // and we can't use a 0 timeout because we have no way to cancel an infinitely long
                    // running query. We are picking a reasonable command timeout instead.
                    cmd.CommandTimeout = 300;
                    cmd.Connection = conn;
                    if (checkSqlScriptAsProxy)
                    {
                        //If we are impersonating the policy non-priviledged user, we will wrap
                        //the script in an EXECUTE AS ... REVERT block
                        //To guard against the user doing a REVERT in the sql provided we will use 
                        //the WITH COOKIE construct, and generate the name of the cookie variable
                        //randomly in the emitted script
                        string executeSqlWithProxyScript =
                                @"DECLARE @cookie varbinary(100);
                                  EXECUTE AS LOGIN = N'##MS_PolicyTsqlExecutionLogin##' WITH COOKIE INTO @cookie;
                                  BEGIN TRY " +
                                executeSqlScript +
                                @"   REVERT WITH COOKIE = @cookie; 
                                  END TRY
                                  BEGIN CATCH 
                                     REVERT WITH COOKIE = @cookie; 
                                     DECLARE @ErrorMessage   NVARCHAR(4000);
                                     DECLARE @ErrorSeverity  INT;
                                     DECLARE @ErrorState     INT;
                                     DECLARE @ErrorNumber    INT;
                                     DECLARE @ErrorLine      INT;
                                     DECLARE @ErrorProcedure NVARCHAR(200);
                                     SELECT @ErrorLine = ERROR_LINE(),
                                            @ErrorSeverity = ERROR_SEVERITY(),
                                            @ErrorState = ERROR_STATE(),
                                            @ErrorNumber = ERROR_NUMBER(),
                                            @ErrorMessage = ERROR_MESSAGE(),
                                            @ErrorProcedure = ISNULL(ERROR_PROCEDURE(), '-');
                                     RAISERROR (14684, @ErrorSeverity, -1 , @ErrorNumber, @ErrorSeverity, @ErrorState, @ErrorProcedure, @ErrorLine, @ErrorMessage);
                                 END CATCH";
                        cmd.CommandText = executeSqlWithProxyScript;
                    }
                    else
                    {
                        cmd.CommandText = executeSqlScript;
                    }

                    if (!String.IsNullOrEmpty(smoobj.GetDBName()))
                    {
                        // If the object related to DB
                        // we need to switch context
                        cmd.CommandText = string.Format("use {0}; ",
                            SFC.SfcTsqlProcFormatter.MakeSqlBracket(smoobj.GetDBName())) + cmd.CommandText;
                    }

                    // Run the query
                    ret = cmd.ExecuteScalar();
                    if (ret == null)
                    {
                        //If we get a null back, it means the query returned an empty set
                        //In this case, treat it as if it returned a NULL value (i.e., condition will always evaluate to false)
                        ret = DBNull.Value;
                    }
                    // Set the return value to the user-specified type
                    mReturnType = (TypeClass)Enum.Parse(typeof(TypeClass), Evaluator.ConvertToString(mArgs[cTypeArgPos]), true);

                    // Cast to the specified type, unless you are:
                    // 1- Unsupported: we will throw somewhere down the line
                    // 2- Null: This will happen if the query returns an empty result set, we will have a special evaluation for that value.
                    // 3- DBNull: we will also have a special evaluator for this case
                    if (!(ret is DBNull) && (mReturnType != TypeClass.Unsupported))
                    {
                        ret = Evaluator.ConvertToAny(ret, mReturnType);
                    }
                }
                finally
                {
                    if (bOpened)
                    {
                        conn.Close();
                    }

                }

                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }

        private object EvaluateExecuteWqlScalar ()
        {
            // 8/21/08 GrigoryP - added this for later use; cannot use now due to loc freeze in CU
            //bool callRemote = false;
            string machineName = String.Empty;

            int cTypeArgPos = 0;  // Position of arg containing type
            int cWMINameSpaceArgPos = 1;  // Position of arg containing WMI namespace to use
            int cWQLArgPos = 2;  // Position of arg containing WQL query to run

            string cMgmtAssemName = "System.Management, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

            object ret = null;
            string wminamespace = Evaluator.ConvertToString (mArgs[cWMINameSpaceArgPos]);
            string wqlstr = Evaluator.ConvertToString (mArgs[cWQLArgPos]);
            //replace every double quote with a single one, this is necessary since the SFC expression parser treats single quotes as string delimiters also.
            wqlstr = wqlstr.Replace("''", "'"); 

            if (!wminamespace.StartsWith (@"\\", StringComparison.Ordinal))
            {
                // If namespace doesn't start with "\\" it doesn't have machine name there

                if (mcontext.Target is IDmfObjectInfo)
                {
                    //This will be the case if the target is not an SMO object, as in an RS or AS object
                    machineName = (mcontext.Target as IDmfObjectInfo).RootPath;
                }
                else if (mcontext.Target is SMO.SqlSmoObject)
                {
                    // Be sure we have a SMO object - we need a Server to derive machine name
                    SMO.SqlSmoObject smoobj = mcontext.Target as SMO.SqlSmoObject;

                    // Walk the object hierarchy as necessary to get the Server
                    SMO.Server server = DmfSqlSmoObjectProxy.GetServerObj (smoobj);

                    if (null == server)
                    {
                        throw new FunctionNoServerException ();
                    }

                    machineName = server.Information.NetName;
                }
                // we can be evaluating an adapter, in which case, try to get the root from its corresponding physical target
                else if (mcontext.Target is IDmfAdapter && mcontext.PhysicalTarget != null && mcontext.PhysicalTarget is SMO.SqlSmoObject)
                {
                    // Be sure we have a SMO object - we need a Server to derive machine name
                    SMO.SqlSmoObject smoobj = mcontext.PhysicalTarget as SMO.SqlSmoObject;

                    // Walk the object hierarchy as necessary to get the Server
                    SMO.Server server = DmfSqlSmoObjectProxy.GetServerObj(smoobj);

                    if (null == server)
                    {
                        throw new FunctionNoServerException();
                    }

                    machineName = server.Information.NetName;
                }
                else
                {
                    throw new FunctionNotASmoObjectException(mcontext.Target.GetType().Name);
                }

                if (!Environment.MachineName.Equals (machineName, StringComparison.OrdinalIgnoreCase))
                {
                    //callRemote = true;
                    wminamespace = @"\\" + machineName +
                        (wminamespace.StartsWith (@"\", StringComparison.Ordinal) ? String.Empty : @"\") +
                        wminamespace;
                }
            }

            try
            {
                // Use the file name to load the assembly into the current application domain.
                Assembly assem = Assembly.Load (cMgmtAssemName);

                traceContext.DebugAssert (assem != null, "Unable to find 'System.Management' assembly");

                //Get the type to use
                Type wmiMosType = assem.GetType ("System.Management.ManagementObjectSearcher", true, true);

                traceContext.DebugAssert (wmiMosType != null, "Unable to find 'System.Management.ManagementObjectSearcher' type");

                //Get the method to call
                Type[] fparams = new Type[0];
                MethodInfo wmiMethod = wmiMosType.GetMethod ("Get", fparams);

                traceContext.DebugAssert (wmiMethod != null, "Unable to find 'ManagementObjectSearcher.Get' method");

                //Create an instance
                Object wmiMosObj = Activator.CreateInstance (wmiMosType, wminamespace, wqlstr);

                traceContext.DebugAssert (wmiMosObj != null, "Unable to create 'System.Management.ManagementObjectSearcher' object");

                //Execute the Get method
                IEnumerable searcher = (IEnumerable)wmiMethod.Invoke (wmiMosObj, null);

                traceContext.DebugAssert (searcher != null, "Unable to invoke 'ManagementObjectSearcher.Get' method");

                foreach (object obj in (searcher))
                {
                    PropertyInfo srchProperties = obj.GetType ().GetProperty ("Properties");
                    ICollection properties = (ICollection)srchProperties.GetValue (obj, null);

                    if (properties.Count > 1)
                    {
                        throw traceContext.TraceThrow(new FunctionTooManyColumnsException());
                    }

                    foreach (object property in properties)
                    {

                        PropertyInfo srchProperty = property.GetType ().GetProperty ("Value");

                        traceContext.DebugAssert (srchProperty != null, "Unable to find 'ManagementObject' Value property");

                        ret = srchProperty.GetValue (property, null);
                        break;  // Bail after getting the first property
                    }
                    break;  // Bail after getting the first object
                }

                // Set the return value to the user-specified type
                mReturnType = (TypeClass)Enum.Parse (typeof (TypeClass), Evaluator.ConvertToString (mArgs[cTypeArgPos]), true);

                // Cast to the specified type
                if ((ret != null) && (mReturnType != TypeClass.Unsupported))
                {
                    ret = Evaluator.ConvertToAny (ret, mReturnType);
                }
            }
            catch (Exception ex)
            {
                traceContext.TraceCatch(ex);
                if (ex is FunctionTooManyColumnsException)
                {
                    throw;
                }
                else
                {
                    // 8/21/08 GrigoryP - cannot make loc-affecting changes in CU
                    // Using the same error message, but replacing exception message with namespace.
                    // There is no value in having exception message there, 
                    // as you can look it up in Inner Exception, and UI does that.
                    // Namespace is the only hint we can provide to indicate remote access
                    //
                    // Idially I would like to provide message explicitly indicating local or remote access
                    // with machine name. All that information is available here. (callRemote and machineName)

                    throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.WmiException(ex.Message), ex));
                }
            }

            return ret;
        }

        private object EvaluateGetDate()
        {
            return DateTime.Now;
        }

        private object EvaluateDateAdd()
        {
            int cDatePartPos = 0;
            int cDateIncPos = 1;
            int cDatePos = 2;

            object ret = null;

            string datePart = Evaluator.ConvertToString(mArgs[cDatePartPos]);
            int dateInc = Convert.ToInt32(Evaluator.ConvertToLong(mArgs[cDateIncPos]));
            DateTime date = Evaluator.ConvertToDateTime(mArgs[cDatePos]);

            switch (datePart.ToUpperInvariant())
            {
                case "YEAR":
                case "YY":
                case "YYYY":
                    ret = date.AddYears(dateInc);
                    break;
                case "MONTH":
                case "MM":
                case "M":
                    ret = date.AddMonths(dateInc);
                    break;
                case "DAYOFYEAR":
                case "DY":
                case "Y":
                case "DAY":
                case "DD":
                case "D":
                case "WEEKDAY":
                case "DW":
                    ret = date.AddDays(dateInc);
                    break;
                case "HOUR":
                case "HH":
                    ret = date.AddHours(dateInc);
                    break;
                case "MINUTE":
                case "MI":
                case "N":
                    ret = date.AddMinutes(dateInc);
                    break;
                case "SECOND":
                case "SS":
                case "S":
                    ret = date.AddSeconds(dateInc);
                    break;
                case "MILLISECOND":
                case "MS":
                    ret = date.AddMilliseconds(dateInc);
                    break;
                default:
                    throw traceContext.TraceThrow(new FunctionBadDatePartException());
            }

            return ret;

        }

        private object EvaluateDatePart()
        {
            int cDatePartPos = 0;
            int cDatePos = 1;

            object ret = null;

            string datePart = Evaluator.ConvertToString(mArgs[cDatePartPos]);
            DateTime date = Evaluator.ConvertToDateTime(mArgs[cDatePos]);

            switch (datePart.ToUpperInvariant())
            {
                case "YEAR":
                case "YY":
                case "YYYY":
                    ret = date.Year;
                    break;
                case "MONTH":
                case "MM":
                case "M":
                    ret = date.Month;
                    break;
                case "DAYOFYEAR":
                case "DY":
                case "Y":
                    ret = date.DayOfYear;
                    break;
                case "DAY":
                case "DD":
                case "D":
                    ret = date.Day;
                    break;
                case "WEEKDAY":
                case "DW":
                    ret = (int)date.DayOfWeek + 1;  //Add one for consistency with SQL Server
                    break;
                case "HOUR":
                case "HH":
                    ret = date.Hour;
                    break;
                case "MINUTE":
                case "MI":
                case "N":
                    ret = date.Minute;
                    break;
                case "SECOND":
                case "SS":
                case "S":
                    ret = date.Second;
                    break;
                case "MILLISECOND":
                case "MS":
                    ret = date.Millisecond;
                    break;
                default:
                    throw traceContext.TraceThrow(new FunctionBadDatePartException());
            }

            return ret;

        }

        private object EvaluateLen()
        {
            int cStringPos = 0;

            return Evaluator.ConvertToString(mArgs[cStringPos]).Length;
        }

        private object EvaluateUpper()
        {
            int cStringPos = 0;
            string ret = String.Empty;

            string value = Evaluator.ConvertToString(mArgs[cStringPos]);

            // Be sure we have a SMO object - we need the culture info
            // TODO: VSTS 160654 when freeing DMF of SMO dependency, we need an interface that 
            // will retrieve culture info from a target object
            SMO.SqlSmoObject smoobj = mcontext.PhysicalTarget as SMO.SqlSmoObject;

            CultureInfo culture = CultureInfo.InvariantCulture;
            if (null != smoobj)
            {
                culture = smoobj.StringComparer.CultureInfo;
            }

            ret = value.ToUpper(culture);

            return ret;

        }

        private object EvaluateLower()
        {
            int cStringPos = 0;
            string ret = String.Empty;

            string value = Evaluator.ConvertToString(mArgs[cStringPos]);

            // Be sure we have a SMO object - we need the culture info
            // TODO: VSTS 160654 when freeing DMF of SMO dependency, we need an interface that 
            // will retrieve culture info from a target object
            SMO.SqlSmoObject smoobj = mcontext.PhysicalTarget as SMO.SqlSmoObject;

            CultureInfo culture = CultureInfo.InvariantCulture;
            if (null != smoobj)
            {
                culture = smoobj.StringComparer.CultureInfo;
            }

            ret = value.ToLower(culture);

            return ret;

        }

        private object EvaluateAdd()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            double arg1 = Evaluator.ConvertToDouble(mArgs[cArg1Pos]);
            double arg2 = Evaluator.ConvertToDouble(mArgs[cArg2Pos]);

            return (arg1 + arg2);
        }

        private object EvaluateSubtract()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            double arg1 = Evaluator.ConvertToDouble(mArgs[cArg1Pos]);
            double arg2 = Evaluator.ConvertToDouble(mArgs[cArg2Pos]);

            return (arg1 - arg2);

        }

        private object EvaluateMultiply()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            double arg1 = Evaluator.ConvertToDouble(mArgs[cArg1Pos]);
            double arg2 = Evaluator.ConvertToDouble(mArgs[cArg2Pos]);

            return (arg1 * arg2);

        }

        private object EvaluateDivide()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            double arg1 = Evaluator.ConvertToDouble(mArgs[cArg1Pos]);
            double arg2 = Evaluator.ConvertToDouble(mArgs[cArg2Pos]);

            return (arg1 / arg2);

        }

        private object EvaluatePower()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            double arg1 = Evaluator.ConvertToDouble(mArgs[cArg1Pos]);
            double arg2 = Evaluator.ConvertToDouble(mArgs[cArg2Pos]);

            return Math.Pow(arg1, arg2);

        }

        private object EvaluateModulus()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            double arg1 = Evaluator.ConvertToDouble(mArgs[cArg1Pos]);
            double arg2 = Evaluator.ConvertToDouble(mArgs[cArg2Pos]);

            return (arg1 % arg2);

        }

        private object EvaluateRound()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            double arg1 = Evaluator.ConvertToDouble(mArgs[cArg1Pos]);
            int arg2 = Convert.ToInt32(Evaluator.ConvertToDouble(mArgs[cArg2Pos]));

            return Math.Round(arg1, arg2);

        }

        private object EvaluateBitwiseAnd()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            Int64 arg1 = Evaluator.ConvertToLong(mArgs[cArg1Pos]);
            Int64 arg2 = Evaluator.ConvertToLong(mArgs[cArg2Pos]);

            return (arg1 & arg2);

        }

        private object EvaluateBitwiseOr()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            Int64 arg1 = Evaluator.ConvertToLong(mArgs[cArg1Pos]);
            Int64 arg2 = Evaluator.ConvertToLong(mArgs[cArg2Pos]);

            return (arg1 | arg2);

        }

        private object[] EvaluateArray()
        {
            object[] ret = new object[mArgs.Count];
            for (int i = 0; i < mArgs.Count; ++i)
            {
                ret[i] = mArgs[i];
            }
            return ret;
        }

        private double EvaluateSum()
        {

            double ret = 0;
            foreach (object obj in mArgs)
            {
                ret += Evaluator.ConvertToDouble(obj);
            }
            return ret;

        }

        private double EvaluateAvg()
        {

            double ret = 0;
            foreach (object obj in mArgs)
            {
                ret += Evaluator.ConvertToDouble(obj);
            }
            return ret / mArgs.Count;
        }

        private double EvaluateCount()
        {
            return mArgs.Count;
        }

        private object EvaluateIsNull()
        {
            int cArg1Pos = 0;
            int cArg2Pos = 1;

            if ((mArgs[cArg1Pos] == null) || (mArgs[cArg1Pos] is DBNull))
            {
                return mArgs[cArg2Pos];
            }
            else
            {
                return mArgs[cArg1Pos];
            }
        }

        private object EvaluateEnum()
        {
            int cEnumPos = 0;
            int cEnumValPos = 1;

            object value = ResolveEnum(Evaluator.ConvertToString(mArgs[cEnumPos]), Evaluator.ConvertToString(mArgs[cEnumValPos]));
            if (null == value)
            {
                throw traceContext.TraceThrow(new UnsupportedObjectTypeException(Evaluator.ConvertToString(mArgs[cEnumPos]), this.GetType().ToString()));
            }

            return value;
        }

        private object EvaluateTrue()
        {
            return true;
        }

        private object EvaluateFalse()
        {
            return false;
        }

        private object EvaluateDateTime()
        {
            int cDateStrPos = 0;
            string datestr = Evaluator.ConvertToString(mArgs[cDateStrPos]);
            return Convert.ToDateTime(datestr, CultureInfo.InvariantCulture);
        }

        private object EvaluateString()
        {
            int cStrPos = 0;
            return Evaluator.ConvertToString(mArgs[cStrPos]);
        }

        private object EvaluateConcatenate ()
        {
            int cStr1Pos = 0;
            int cStr2Pos = 1;
            string string1 = Evaluator.ConvertToString(mArgs[cStr1Pos]);
            string string2 = Evaluator.ConvertToString(mArgs[cStr2Pos]);

            return String.Concat (string1, string2);
        }

        private object EvaluateEscape()
        {
            int cStringPos = 0;
            int cStringToEscapePos = 1;
            int cNewStringPos = 2;

            string replacedString = Evaluator.ConvertToString(mArgs[cStringPos]);
            string stringToEscape = Evaluator.ConvertToString(mArgs[cStringToEscapePos]);
            string newString = Evaluator.ConvertToString(mArgs[cNewStringPos]);

            if (string.IsNullOrEmpty(stringToEscape))
            {
                throw new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("StringToEscape"));
            }

            return replacedString.Replace(stringToEscape, newString + stringToEscape);
        }

        private object EvaluateGuid()
        {
            int cGuidStrPos = 0;
            string guidstr = Evaluator.ConvertToString(mArgs[cGuidStrPos]);
            return new Guid(guidstr);
        }

#endregion EVALUATE

#region SERIALIZE
        /// <summary>
        /// Serializes Operator node properties
        /// </summary>
        /// <param name="xw"></param>
        /// <param name="includeResult"></param>
        protected override void SerializeProperties(XmlWriter xw, bool includeResult)
        {
            xw.WriteElementString(cXmlTagFuncType, mFunctionType.ToString());
            xw.WriteElementString(cXmlTagReturnType, mReturnType.ToString());

            if (includeResult)
            {
                SerializeResult(xw);
            }

            base.SerializeProperties(xw, includeResult);
        }

        /// <summary>
        /// Deserializes Operator node properties
        /// </summary>
        /// <param name="xr">XmlReader - must ignore whitespaces</param>
        /// <param name="includeResult"></param>
        protected override void DeserializeProperties(XmlReader xr, bool includeResult)
        {
            List<string> vals = ReadNodeWithCheck(xr, cXmlTagFuncType);

            Function type = MatchType<Function>(vals[0]);
            this.mFunctionType = type;

            List<string> retVals = ReadNodeWithCheck(xr, cXmlTagReturnType);
            this.mReturnType = (TypeClass)Enum.Parse(typeof(TypeClass), retVals[0]);


            if (includeResult)
            {
                DeserializeResult(xr);
            }

            base.DeserializeProperties(xr, includeResult);

            SetProperties();
        }
#endregion SERIALIZE

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SFC.FilterNode DoConvertToFilterNode()
        {
            SFC.FilterNodeFunction.Type fFuncType = SFC.FilterNodeFunction.Type.UserDefined;
            string name = this.FunctionType.ToString();

            switch (this.FunctionType)
            {
                case Function.True:
                    name = "true";
                    fFuncType = SFC.FilterNodeFunction.Type.True;
                    break;
                case Function.False:
                    name = "false";
                    fFuncType = SFC.FilterNodeFunction.Type.False;
                    break;
                case Function.String:
                    name = "string";
                    fFuncType = SFC.FilterNodeFunction.Type.String;
                    break;
                case Function.Enum:
                    // an enum is transformed in a numerical constant
                    {
                        int cEnumPos = 0;
                        int cEnumValPos = 1;

                        // special care here because the enum is specified via a string,
                        // and we need to transform it. this is not always straightforward 
                        // because the enum might be defined in a different assembly.
                        return new SFC.FilterNodeConstant(
                            Convert.ToInt32(ResolveEnum(
                                ((ExpressionNodeConstant)ChildrenList[cEnumPos]).Value.ToString(),
                                ((ExpressionNodeConstant)ChildrenList[cEnumValPos]).Value.ToString())),
                            SFC.FilterNodeConstant.ObjectType.Number);
                    }
            }

            int nargs = this.ChildrenList.Count;
            SFC.FilterNode[] args = new SFC.FilterNode[nargs];
            for (int i = 0; i < nargs; i++)
            {
                args[i] = this.ChildrenList[i].ConvertToFilterNode();
            }
            SFC.FilterNodeFunction fnode = new SFC.FilterNodeFunction(fFuncType, name, args);
            return fnode;
        }

        /// <summary>
        /// Deep clone of the current node.
        /// </summary>
        /// <returns></returns>
        public override ExpressionNode DeepClone()
        {
            ExpressionNodeFunction clone = new ExpressionNodeFunction();
            clone.mFunctionType = this.FunctionType;
            clone.mReturnType = this.ReturnType;
            clone.LastEvaluationResult = this.LastEvaluationResult;
            clone.Tag = this.Tag;
            clone.SetTypeClass(this.TypeClass);

            foreach (ExpressionNode node in EnumChildren())
            {
                clone.Add(node.DeepClone());
            }

            return clone;
        }
    }
}
