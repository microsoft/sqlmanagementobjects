// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    /// <summary>
    /// represents an XPATH level
    /// </summary>
    [ComVisible(false)]
    public class XPathExpressionBlock
    {
        String m_name;
        FilterNode m_filter;
        KeyValuePair<string, object>[] m_fixedProperties;

        // There expected to be only a few unique XPathExpressionBlock names like Server, Table, etc
        // By caching them globally we avoid having multiple copies of the same name in memory
        private static Dictionary<string, string> CachedNames = new Dictionary<string, string>(StringComparer.Ordinal); 

        /// <summary>
        /// default constructor
        /// </summary>
        public XPathExpressionBlock()
        {
        }

        /// <summary>
        /// Name and FilterNode constructor
        /// </summary>
        public XPathExpressionBlock(String name, FilterNode filter)
        {
            if (!CachedNames.TryGetValue(name, out m_name))
            {
                CachedNames.Add(name, name);
                m_name = name;
            }
            this.Filter = filter;
        }
        
        /// <summary>
        /// make a shallow copy
        /// </summary>
        /// <returns></returns>
        public XPathExpressionBlock Copy()
        {
            XPathExpressionBlock x = new XPathExpressionBlock();
            x.m_name = this.m_name;
            x.m_filter = this.m_filter;
            x.m_fixedProperties = this.m_fixedProperties;
            return x;
        }
        
        /// <summary>
        /// level name
        /// </summary>
        /// <value></value>
        public String Name
        {
            get	{ return m_name; }
            set	{ m_name = value; }
        }

        /// <summary>
        /// syntactical tree representation of a filter block
        /// </summary>
        /// <value></value>
        public FilterNode Filter
        {
            get { return m_filter;}
            set { 
                    m_fixedProperties = null;
                    m_filter = value;
                }
        }

        /// <summary>
        /// Returns the string representation of this block.
        /// </summary>
        public override string ToString()
        {
            string ret = this.Name;
            if (null != this.Filter)
            {
                ret += String.Format("[{0}]", this.Filter);
            }
            return ret;
        }

        /// <summary>
        /// Overrides Object.GetHashCode
        /// </summary>
        public override int GetHashCode()
        {
            int hashCode = m_name.GetHashCode();
            if (m_filter != null) 
            {
                 hashCode ^= m_filter.GetHashCode();
            }
            return hashCode;
        }

        int IndexOfFixedProperty(string name)
        {
            for (int i = 0; i < FixedPropertiesInternal.Length; i++)
            {
                if (String.Compare(FixedPropertiesInternal[i].Key, name, StringComparison.Ordinal) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// add a fixed property to the internal list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fnc"></param>
        void AddFixedProperty(string name, Object fnc)
        {
            KeyValuePair<string, object> newPair = new KeyValuePair<string,object>(name, fnc);

            if (m_fixedProperties == null || m_fixedProperties.Length == 0)
            {
                m_fixedProperties = new KeyValuePair<string, object>[] { newPair };
            }
            else
            {
                if (IndexOfFixedProperty(name) == -1)
                {
                    int oldCount = m_fixedProperties.Length;
                    KeyValuePair<string, object>[] oldArray = m_fixedProperties;
                    m_fixedProperties = new KeyValuePair<string, object>[oldCount + 1];
                    Array.Copy(oldArray, m_fixedProperties, oldCount);
                    m_fixedProperties[oldCount] = newPair;
                }
            }
        }

        /// <summary>
        /// add fixed property if valid
        /// </summary>
        /// <param name="fno"></param>
        void AddFixedProperty(FilterNodeOperator fno)
        {
            FilterNodeAttribute fnaToSet;
            Object value = null;
            FilterNode.Type valueFilterType = FilterNode.Type.Operator;

            //check which side the attribute is
            if( FilterNode.Type.Attribute == fno.Left.NodeType )
            {
                fnaToSet = (FilterNodeAttribute)fno.Left;
                value = fno.Right;
                valueFilterType = fno.Right.NodeType;
            }
            else //if( FilterNode.Type.Attribute == fno.Right.NodeType )
            {
                fnaToSet = (FilterNodeAttribute)fno.Right;
                value = fno.Left;
                valueFilterType = fno.Left.NodeType;
            }

            //we accept constants or the functions: true(), false()
            if( FilterNode.Type.Constant == valueFilterType )
            {
                AddFixedProperty(fnaToSet.Name, value);
            }
            else if( FilterNode.Type.Function == valueFilterType )
            {
                FilterNodeFunction fnf = (FilterNodeFunction)value;
                if( fnf.FunctionType == FilterNodeFunction.Type.True || fnf.FunctionType == FilterNodeFunction.Type.False )
                {
                    AddFixedProperty(fnaToSet.Name, value);
                }
            }
        }

        /// <summary>
        /// parse the tree and indentify fixed properties
        /// ( in the form [@Name='ddd'] or [@Name = 'ddd' and ( ) ]
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        bool ComputeFixedProperties(FilterNode node)
        {
            if( null == node )
            {
                return true;
            }

            if ( FilterNode.Type.Operator == node.NodeType )
            {
                FilterNodeOperator fno = (FilterNodeOperator)node;
                if( FilterNodeOperator.Type.EQ == fno.OpType )
                {
                    AddFixedProperty(fno);
                }
                else if( FilterNodeOperator.Type.And == fno.OpType )
                {
                    if( !ComputeFixedProperties(fno.Left) )
                    {
                        return false;
                    }
                    if( !ComputeFixedProperties(fno.Right) )
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// get the value of an attribute for a filter of the form [@Attribute = ddd]
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        static public String GetUniqueAttribute(FilterNode filter)
        {
            if( null == filter )
            {
                return null;
            }
            FilterNodeOperator fno = (FilterNodeOperator)filter;
            if( FilterNodeOperator.Type.EQ == fno.OpType )
            {
                if( FilterNode.Type.Attribute == fno.Left.NodeType && FilterNode.Type.Constant == fno.Right.NodeType )
                {
                    return ((FilterNodeConstant)fno.Right).Value.ToString();
                }
                else if( FilterNode.Type.Attribute == fno.Right.NodeType && FilterNode.Type.Constant == fno.Left.NodeType )
                {
                    return ((FilterNodeConstant)fno.Left).Value.ToString();
                }
            }
            return null;
        }

        private KeyValuePair<string, object>[] FixedPropertiesInternal
        {
            get
            {
                if (m_fixedProperties == null)
                {
                    ComputeFixedProperties(this.Filter);
                    if (m_fixedProperties == null)
                    {
                        m_fixedProperties = new KeyValuePair<string, object>[] { };
                    }
                }
                return m_fixedProperties;
            }
        }

        /// <summary>
        /// get the list of fixed properties
        /// </summary>
        /// <value></value>
        public SortedList FixedProperties
        {
            get
            {
                // fixed properties are stored as simple array of structs but should be
                // returned as SortedArray to maintain compatibility with the existing code
                SortedList result = new SortedList(StringComparer.Ordinal, FixedPropertiesInternal.Length);
                foreach (KeyValuePair<string, object> pair in FixedPropertiesInternal)
                {
                    result.Add(pair.Key, pair.Value);
                }
                return result;
            }
        }

        /// <summary>
        /// get the attribute from filter if its value is specified
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public string GetAttributeFromFilter(String attributeName)
        {
            string s = null;
            int index = IndexOfFixedProperty(attributeName);
            if (index >= 0)
            {
                s = ((FilterNodeConstant)FixedPropertiesInternal[index].Value).Value.ToString();
            }
            return s;
        }
    }
}	
