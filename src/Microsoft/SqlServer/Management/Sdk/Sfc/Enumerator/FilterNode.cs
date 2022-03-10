// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// base class for elements in the filter
    /// </summary>
    [ComVisible(false)]
    public abstract class FilterNode
    {
        /// <summary>
        /// type of filter node
        /// </summary>
        public enum Type 
        { 
            /// <summary>
            /// attribute
            /// </summary>
            Attribute, 
            /// <summary>
            /// constant
            /// </summary>
            Constant, 
            /// <summary>
            /// operator
            /// </summary>
            Operator, 
            /// <summary>
            /// function
            /// </summary>
            Function, 
            /// <summary>
            /// group
            /// </summary>
            Group }

        /// <summary>
        /// type of the node
        /// </summary>
        /// <value></value>
        public abstract FilterNode.Type NodeType
        {
            get; 
        }

        /// <summary>
        /// compare two filter nodes
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <param name="compInfo"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        static public bool Compare(FilterNode f1, FilterNode f2, CompareOptions compInfo, CultureInfo cultureInfo)
        {
            if( f1 == null && f2 == null )
            {
                return true;
            }

            if ( f1 == null || f2 == null )
            {
                return false;
            }

            if ( f1.NodeType != f2.NodeType )
            {
                return false;
            }

            switch (f1.NodeType)
            {
                case Type.Attribute:return FilterNodeAttribute.Compare((FilterNodeAttribute)f1, (FilterNodeAttribute)f2);
                case Type.Constant:return FilterNodeConstant.Compare((FilterNodeConstant)f1, (FilterNodeConstant)f2, compInfo, cultureInfo);
                case Type.Operator:return FilterNodeOperator.Compare((FilterNodeOperator)f1, (FilterNodeOperator)f2, compInfo, cultureInfo);
                case Type.Function:return FilterNodeFunction.Compare((FilterNodeFunction)f1, (FilterNodeFunction)f2, compInfo, cultureInfo);
                case Type.Group:return FilterNodeGroup.Compare((FilterNodeGroup)f1, (FilterNodeGroup)f2, compInfo, cultureInfo);
                default:throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UnknowNodeType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
            return String.Empty;
        }
    }

    /// <summary>
    /// a node that encapsulates an attribute in the filter
    /// </summary>
    [ComVisible(false)]
    public class FilterNodeAttribute : FilterNode
    {
        String m_name;
        // There expected to be only a few unique attribute names for FilterNodeAttribute
        // By caching them globally we avoid having multiple copies of the same attribute name in memory
        private static Dictionary<string,string> CachedNames = new Dictionary<string,string>(StringComparer.Ordinal);
        // Lock for the CachedNames dictionary, used when inserting new names
        private static object CachedNamesLock = new object();

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="name">attribute name</param>
        public FilterNodeAttribute(String name)
        {
            if (!CachedNames.TryGetValue(name, out m_name))
            {
                // Need to add the name to the cache
                // Use a lock here to prevent race condition
                lock (CachedNamesLock)
                {
                    if (!CachedNames.TryGetValue(name, out m_name))
                    {
                        CachedNames.Add(name, name);
                        m_name = name;
                    }
                }
            }
        }

        /// <summary>
        /// type of the node
        /// </summary>
        /// <value></value>
        public override FilterNode.Type NodeType
        {
            get { return FilterNode.Type.Attribute; }
        }

        /// <summary>
        /// attribute name
        /// </summary>
        /// <value></value>
        public String Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// compare 2 FilterNodeAttribute
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        static internal bool Compare(FilterNodeAttribute a1, FilterNodeAttribute a2)
        {
            return 0 == String.Compare(a1.Name, a2.Name,StringComparison.Ordinal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
            return "@" + Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    /// <summary>
    /// a node that encapsulates a constant in the filter
    /// </summary>
    [ComVisible(false)]
    public class FilterNodeConstant : FilterNode
    {
        /// <summary>
        /// constant type
        /// </summary>
        public enum ObjectType 
        { 
            /// <summary>
            /// constant is a number
            /// </summary>
            Number, 
            /// <summary>
            /// constant is boolean
            /// </summary>
            Boolean, 
            /// <summary>
            /// constant is a string
            /// </summary>
            String 
        };

        Object m_value;
        ObjectType m_objtype;

        /// <summary>
        /// initalize with value and type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public FilterNodeConstant(Object value, ObjectType type)
        {
            m_value = value;
            m_objtype = type;
        }

        /// <summary>
        /// type of the node
        /// </summary>
        /// <value></value>
        public override FilterNode.Type NodeType
        {
            get { return FilterNode.Type.Constant; }
        }

        /// <summary>
        /// value of the constant
        /// </summary>
        /// <value></value>
        public Object Value
        {
            get { return m_value; }
        }

        /// <summary>
        /// type of the constant
        /// </summary>
        /// <value></value>
        public ObjectType ObjType
        {
            get { return m_objtype; }
        }

        /// <summary>
        /// get the raw constant value as string
        /// </summary>
        /// <value></value>
        public string ValueAsString
        {
            get { return this.Value.ToString(); }
        }

        /// <summary>
        /// implicit cast to string
        /// </summary>
        /// <param name="fnc"></param>
        /// <returns>An escaped string value</returns>
        public static implicit operator String(FilterNodeConstant fnc)
        {
            if( null == fnc )
            {
                return null;
            }
            return  Urn.EscapeString(fnc.ValueAsString);
        }

        /// <summary>
        /// compare two FilterNodeConstants
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <param name="compInfo"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        static internal bool Compare(FilterNodeConstant f1, FilterNodeConstant f2, CompareOptions compInfo, CultureInfo cultureInfo)
        {
            if( f1.ObjType != f2.ObjType )
            {
                return false;
            }

            switch (f1.ObjType)
            {
                case ObjectType.String:goto case ObjectType.Number;
                case ObjectType.Boolean:goto case ObjectType.Number;
                case ObjectType.Number:
                    return 0 == cultureInfo.CompareInfo.Compare(f1.Value.ToString(), f2.Value.ToString(), compInfo);
            }
            return false;
        }

        /// <summary>
        /// Converts the value to a string that can be used as a parameter in a SQL script.        
        /// </summary>
        /// <returns>For ObjectType.String, an escaped value surrounded by single quotes. For ObjectType.Boolean, true() or false().</returns>
        public override string ToString ()
        {
            if (ObjType == FilterNodeConstant.ObjectType.String)
            {
                return String.Format ("'{0}'", Urn.EscapeString (Value.ToString ()));
            }
            else if (ObjType == FilterNodeConstant.ObjectType.Boolean)
            {
                return (bool)this.m_value?"true()":"false()";
            }
            else
            {
                return Value.ToString ();
            }
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }
    }

    /// <summary>
    /// holds a list of subnodes
    /// </summary>
    [ComVisible(false)]
    public abstract class FilterNodeChildren : FilterNode
    {
        FilterNode[] children;

        /// <summary>
        /// list of nodes
        /// </summary>
        /// <value></value>
        internal FilterNode[] Children
        {
            get { return this.children ?? new FilterNode[] {}; }
        }

        /// <summary>
        /// initialize
        /// </summary>
        internal FilterNodeChildren()
        {
        }

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="children"></param>
        internal FilterNodeChildren(FilterNode[] children)
        {
            this.children = children;
        }

        /// <summary>
        /// add a node
        /// </summary>
        /// <param name="x"></param>
        internal void Add(FilterNode x)
        {
            if (this.children == null)
            {
                this.children = new FilterNode[] { x };
            }
            else
            {
                int oldCount = this.children.Length;
                FilterNode[] oldChildren = this.children;
                
                this.children = new FilterNode[oldCount + 1];
                Array.Copy(oldChildren, this.children, oldCount);
                this.children[oldCount] = x;
            }
        }

        /// <summary>
        /// compare two FilterNodeChildrens
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <param name="compInfo"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        static internal bool Compare(FilterNodeChildren f1, FilterNodeChildren f2, CompareOptions compInfo, CultureInfo cultureInfo)
        {
            if (f1.Children.Length != f2.Children.Length)
            {
                return false;
            }

            for (int i = f1.Children.Length - 1; i >= 0; i--)
            {
                if (!FilterNode.Compare(f1.Children[i], f2.Children[i], compInfo, cultureInfo))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = -1;
            for (int i = 0; i < this.children.Length; i++)
            {
                hashCode ^= this.children[i].GetHashCode();
            }
            return hashCode;
        }
    }

    /// <summary>
    /// pharantesis
    /// </summary>
    [ComVisible(false)]
    public class FilterNodeGroup : FilterNodeChildren
    {
        /// <summary>
        /// default constructor
        /// </summary>
        internal FilterNodeGroup()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public FilterNodeGroup (FilterNode node) : base (new FilterNode[] { node })
        {
        }

        /// <summary>
        /// type of the node
        /// </summary>
        /// <value></value>
        public override FilterNode.Type NodeType
        {
            get { return FilterNode.Type.Group; }
        }

        /// <summary>
        /// get the node
        /// </summary>
        /// <value></value>
        public FilterNode Node
        {
            get{ return this.Children[0]; }
        }

        /// <summary>
        /// compare
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <param name="compInfo"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        static internal bool Compare(FilterNodeGroup f1, FilterNodeGroup f2, CompareOptions compInfo, CultureInfo cultureInfo)
        {
            return FilterNodeChildren.Compare(f1, f2, compInfo, cultureInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
            string res = "(" + Node.ToString () + ")";

            return res;
        }
    }

    /// <summary>
    /// operator
    /// </summary>
    [ComVisible(false)]
    public class FilterNodeOperator : FilterNodeChildren
    {
        /// <summary>
        /// operator type
        /// </summary>
        public new enum Type 
        {  
            /// <summary>
            /// less then
            /// </summary>
            LT, 
            /// <summary>
            /// greater then
            /// </summary>
            GT, 
            /// <summary>
            /// less equal
            /// </summary>
            LE, 
            /// <summary>
            /// greater equal
            /// </summary>
            GE, 
            /// <summary>
            /// equal
            /// </summary>
            EQ, 
            /// <summary>
            /// not equal
            /// </summary>
            NE, 
            /// <summary>
            /// or
            /// </summary>
            OR, 
            /// <summary>
            /// and
            /// </summary>
            And,
            /// <summary>
            /// negate
            /// </summary>
            NEG
        }
        Type m_opType;

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="opType"></param>
        internal FilterNodeOperator(Type opType)
        {
            m_opType = opType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="opType"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public FilterNodeOperator (Type opType, FilterNode left, FilterNode right)
            : base (new FilterNode[] {left, right})
        {
            m_opType = opType;
        }

        /// <summary>
        /// type of the node
        /// </summary>
        /// <value></value>
        public override FilterNode.Type NodeType
        {
            get { return FilterNode.Type.Operator; }
        }

        /// <summary>
        /// operator type
        /// </summary>
        /// <value></value>
        public Type OpType
        {
            get { return m_opType; }
        }

        /// <summary>
        /// left node
        /// </summary>
        /// <value></value>
        public FilterNode Left
        {
            get { return this.Children[0]; }
        }

        /// <summary>
        /// right node
        /// </summary>
        /// <value></value>
        public FilterNode Right
        {
            get { return this.Children[1]; }
        }

        /// <summary>
        /// compare
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <param name="compInfo"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        static internal bool Compare(FilterNodeOperator f1, FilterNodeOperator f2, CompareOptions compInfo, CultureInfo cultureInfo)
        {
            if( f1.OpType != f2.OpType )
            {
                return false;
            }

            return FilterNodeChildren.Compare(f1, f2, compInfo, cultureInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string OpTypeToString (FilterNodeOperator.Type type)
        {
            switch (type)
            {
                case FilterNodeOperator.Type.And:
                    return " and ";
                case FilterNodeOperator.Type.EQ:
                    return "=";
                case FilterNodeOperator.Type.GE:
                    return ">=";
                case FilterNodeOperator.Type.GT:
                    return ">";
                case FilterNodeOperator.Type.LE:
                    return "<=";
                case FilterNodeOperator.Type.LT:
                    return "<";
                case FilterNodeOperator.Type.NE:
                    return "!=";
                case FilterNodeOperator.Type.OR:
                    return " or ";
                default:
                    return String.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
            string res = FilterNodeOperator.Type.NEG == OpType ?
                String.Format ("-{0}", Left.ToString ()):
                String.Format ("{0}{1}{2}", Left.ToString (), OpTypeToString (OpType), Right.ToString ());

            return res;
        }

        public override int GetHashCode()
        {
            return OpType.GetHashCode() ^ base.GetHashCode();
        }
    }

    /// <summary>
    /// a function in the filter
    /// </summary>
    [ComVisible(false)]
    public class FilterNodeFunction : FilterNodeChildren
    {
        /// <summary>
        /// function type
        /// </summary>
        public new enum Type 
        { 
            /// true() value true
            True, 
            /// false() value false
            False, 
            /// not supported
            String,
            /// contains() - same as sql LIKE but it adds % around the input pattern
            Contains, 
            /// placeholder for enumerator extension defined functions
            UserDefined, 
            /// not() - negate
            Not, 
            /// not supported
            Boolean,
            /// t-sql LIKE function
            Like,
            // t-sql IN function
            In
        }
        Type m_funcType;
        String m_name;

        /// <summary>
        /// initzlize with function type and name
        /// </summary>
        /// <param name="funcType"></param>
        /// <param name="name"></param>
        internal FilterNodeFunction(Type funcType, String name)
        {
            m_funcType = funcType;
            m_name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="funcType"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        public FilterNodeFunction (Type funcType, String name, params FilterNode[] args)
            : base (args)
        {
            m_funcType = funcType;
            m_name = name;
        }

        /// <summary>
        /// type of the node
        /// </summary>
        /// <value></value>
        public override FilterNode.Type NodeType
        {
            get { return FilterNode.Type.Function; }
        }

        /// <summary>
        /// function name
        /// </summary>
        /// <value></value>
        public string Name
        {
            get
            {
                return m_name;
            }
        }

        /// <summary>
        /// function type
        /// </summary>
        /// <value></value>
        public Type FunctionType
        {
            get { return m_funcType; }
        }

        /// <summary>
        /// number of function parameters
        /// </summary>
        /// <value></value>
        public int ParameterCount
        {
            get { return this.Children.Length; }
        }

        /// <summary>
        /// get function parameter by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public FilterNode GetParameter(int index)
        {
            return this.Children[index];
        }

        /// <summary>
        /// compare
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <param name="compInfo"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        static internal bool Compare(FilterNodeFunction f1, FilterNodeFunction f2, CompareOptions compInfo, CultureInfo cultureInfo)
        {
            if( f1.FunctionType != f2.FunctionType )
            {
                return false;
            }

            return FilterNodeChildren.Compare(f1, f2, compInfo, cultureInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FuncTypeToString (FilterNodeFunction.Type type)
        {
            return type.ToString ().ToLowerInvariant ();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (FuncTypeToString (FunctionType));
            sb.Append ("(");
            if (ParameterCount > 0)
            {
                sb.Append (GetParameter (0).ToString ());
            }
            for (int i = 1; i < ParameterCount; i++)
            {
                sb.Append (", ");
                sb.Append (GetParameter (i).ToString ());
            }
            sb.Append (")");

            return sb.ToString ();
        }

        public override int GetHashCode()
        {
            return FunctionType.GetHashCode() ^ base.GetHashCode();
        }
    }
}	
