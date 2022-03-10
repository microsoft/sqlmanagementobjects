// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.SqlServer.Management.Common;

    /// <summary>
    ///	Interface that must be implemebted by the user of FilterDecoder</summary>
    [ComVisible(false)]
    public interface ISqlFilterDecoderCallback
    {
        /// <summary>	
        /// FilterDecoder reports that the property name is used in filter
        /// and requests its physical name</summary>
        String AddPropertyForFilter(String name);

        /// <summary>	
        /// FilterDecoder reports that a constant is used in filter
        /// gives client a chance to modify it</summary>
        String AddConstantForFilter(String constantValue);

        /// <summary>
        /// Indicates whether the client support SQL-style query parameterization
        /// </summary>
        bool SupportsParameterization { get; }

    }

    /// <summary>
    ///	parses the syntactical tree to build the tsql where clause</summary>
    [ComVisible(false)]
    public class FilterDecoder
    {
        ISqlFilterDecoderCallback m_isfdc;
        StringBuilder m_sql;
        bool m_bInFuncContains;
        bool m_bInFuncLike;
        string m_strPrefix;

        /// <summary>
        ///	the construnctor receives the callback where to report fields find and get their tsql representation</summary>
        public FilterDecoder(ISqlFilterDecoderCallback isfdc)
        {
            m_isfdc = isfdc;
            m_strPrefix = "N";
        }

        /// <summary>
        ///	prefix for string. default N - unicode</summary>
        public string StringPrefix
        {
            get
            {
                return m_strPrefix;
            }

            set
            {
                m_strPrefix = value;
            }
        }

        /// <summary>
        ///	get the where clause for this sintactical tree</summary>
        public String GetSql(FilterNode node)
        {
            m_sql = new StringBuilder();
            m_bInFuncContains = false;
            m_bInFuncLike = false;
            decode(node);
            return m_sql.ToString();
        }

        String XPathOpToSqOp(FilterNodeOperator.Type op)
        {
            switch (op)
            {
                case FilterNodeOperator.Type.LT: return "<";
                case FilterNodeOperator.Type.GT: return ">";
                case FilterNodeOperator.Type.LE: return "<=";
                case FilterNodeOperator.Type.GE: return ">=";
                case FilterNodeOperator.Type.EQ: return "=";
                case FilterNodeOperator.Type.NE: return "<>";
                case FilterNodeOperator.Type.OR: return " or ";
                case FilterNodeOperator.Type.And: return " and ";
                default: throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UnknownOperator);
            }
        }

        void decode(FilterNode node)
        {
            if (null == node)
            {
                return;
            }
            else if (FilterNode.Type.Operator == node.NodeType)
            {
                FilterNodeOperator op = (FilterNodeOperator)node;
                decode(op.Left);
                m_sql.Append(XPathOpToSqOp(op.OpType));
                decode(op.Right);
            }
            else if (FilterNode.Type.Constant == node.NodeType)
            {
                FilterNodeConstant opd = (FilterNodeConstant)node;
                string val = m_isfdc.AddConstantForFilter(opd.Value.ToString());

                if (!m_isfdc.SupportsParameterization)
                {
                    if (!m_bInFuncContains && !m_bInFuncLike &&
                        FilterNodeConstant.ObjectType.String == opd.ObjType)
                    {
                        m_sql.Append(m_strPrefix + "'");
                    }

                    // Need to escape significant characters in case we are in contains
                    if (m_bInFuncContains)
                    {
                        val = Util.EscapeLikePattern(val);
                    }

                    //add the constant value after it was filtered by the client
                    m_sql.Append(Util.EscapeString(val, '\''));

                    if (!m_bInFuncContains && !m_bInFuncLike &&
                        FilterNodeConstant.ObjectType.String == opd.ObjType)
                    {
                        m_sql.Append('\'');
                    }

                }
                else
                {
                    // add the constant value wrapped in delimiters so we can
                    // parameterize the query later
                    m_sql.AppendFormat("<msparam>{0}</msparam>", val);
                }

            }
            else if (FilterNode.Type.Group == node.NodeType)
            {
                FilterNodeGroup gp = (FilterNodeGroup)node;
                m_sql.Append('(');
                decode(gp.Node);
                m_sql.Append(')');
            }
            else if (FilterNode.Type.Attribute == node.NodeType)
            {
                decode((FilterNodeAttribute)node);
            }
            else if (FilterNode.Type.Function == node.NodeType)
            {
                decode((FilterNodeFunction)node);
            }
        }

        void decode(FilterNodeFunction func)
        {
            QueryParameterizationMode oldParamMode = ServerConnection.ParameterizationMode;

            switch (func.FunctionType)
            {
                case FilterNodeFunction.Type.True:
                    m_sql.Append('1');
                    break;
                case FilterNodeFunction.Type.False:
                    m_sql.Append('0');
                    break;
                case FilterNodeFunction.Type.String:
                    decode((FilterNode)func.GetParameter(0));
                    break;
                case FilterNodeFunction.Type.Contains:
                    try
                    {
                        // turn off parametrization because it is not designed 
                        // to be used inside functions
                        ServerConnection.ParameterizationMode = QueryParameterizationMode.None;
                        decode((FilterNode)func.GetParameter(0));
                        m_sql.Append(" like N'%");
                        m_bInFuncContains = true;
                        decode((FilterNode)func.GetParameter(1));
                        m_bInFuncContains = false;
                        m_sql.Append("%'");
                    }
                    finally
                    {
                        ServerConnection.ParameterizationMode = oldParamMode;
                    }
                    break;
                case FilterNodeFunction.Type.Like:
                    try
                    {
                        // turn off parametrization because it is not designed 
                        // to be used inside functions
                        ServerConnection.ParameterizationMode = QueryParameterizationMode.None;
                        decode((FilterNode)func.GetParameter(0));
                        m_sql.Append(" like N'");
                        m_bInFuncLike = true;
                        decode((FilterNode)func.GetParameter(1));
                        m_bInFuncLike = false;
                        m_sql.Append("'");
                    }
                    finally
                    {
                        ServerConnection.ParameterizationMode = oldParamMode;
                    }
                    break;
                case FilterNodeFunction.Type.In:
                    try
                    {
                        // turn off parametrization because it is not designed 
                        // to be used inside functions
                        ServerConnection.ParameterizationMode = QueryParameterizationMode.None;
                        decode((FilterNode)func.GetParameter(0));
                        m_sql.Append(" in (");
                        // For now only a comma separated list of integers (object IDs) is expected to be use with in function
                        string value = ((FilterNodeConstant)func.GetParameter(1)).ValueAsString;
                        
                        // Validate value before passing it down to the query to avoid any SQL injections
                        foreach (string part in value.Split(new char[] { ',' }))
                        {
                            // This will trow if any of the parts is not a string
                            Int32.Parse(part.Trim());
                        }

                        m_sql.Append(value);
                        m_sql.Append(")");
                    }
                    finally
                    {
                        ServerConnection.ParameterizationMode = oldParamMode;
                    }
                    break;
                case FilterNodeFunction.Type.Not:
                    m_sql.Append("not(");
                    decode((FilterNode)func.GetParameter(0));
                    m_sql.Append(")");
                    break;
                case FilterNodeFunction.Type.Boolean:
                    decode((FilterNode)func.GetParameter(0));
                    break;
                case FilterNodeFunction.Type.UserDefined:
                    {
                        switch (func.Name)
                        {
                            case "BitWiseAnd":
                                m_sql.Append("(");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(")");
                                m_sql.Append(" & ");
                                m_sql.Append("(");
                                decode((FilterNode)func.GetParameter(1));
                                m_sql.Append(")");
                                break;
                            case "is_null":
                                m_sql.Append("(");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(")");
                                m_sql.Append(" is null");
                                break;
                            case "datetime":
                                // Convert from "yyyy-mm-dd hh:mi:ss[.mmm]" ODBC canonical format
                                // (why isn't 126 used instead, or just a cast to datetime for that matter, Legacy probably, and 3 fractional seconds digits max.)
                                m_sql.Append("convert(datetime, ");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(", 121)");
                                break;
                            case "datetime2":
                                // Convert from "yyyy-mm-dd hh:mi:ss[.mmmmmmm]" ISO 8601 string format
                                m_sql.Append("cast(");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(" AS datetime2)");
                                break;
                            case "datetimeoffset":
                                // Convert from "yyyy-mm-ddThh:mi:ss.mmm-08:00" ISO 8601 string format
                                m_sql.Append("cast(");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(" AS datetimeoffset)");
                                break;
                            case "timespan":
                                // Convert from "nnnnnnn" in ticks to a T-SQL bigint
                                // (note: you could also make another one that does "ss[.nnnnnnn]" to seconds (not ticks) using a T-SQL decimal type)
                                m_sql.Append("cast(");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(" AS bigint)");
                                break;
                            case "date":
                                // Convert from "yyyy-mm-dd" ISO 8601 string format (extra time or timezone info is ignored if present)
                                m_sql.Append("cast(");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(" AS date)");
                                break;
                            case "time":
                                // Convert from "hh:mi:ss[.mmmmmmm]" ISO 8601 string format (date or time zone info is ignored if present)
                                // (note: this is NOT a .NET TimeSpan equivalent, as T-SQL currently has no built-in type for that notion
                                //  this ia a simple 24-hour clock representation which just strips off the time portion of a datetime rep)
                                m_sql.Append("cast(");
                                decode((FilterNode)func.GetParameter(0));
                                m_sql.Append(" AS time)");
                                break;
                            default: //silently ignore unknown function
                                break;
                        }
                    }
                    break;
            }
        }

        void decode(FilterNodeAttribute ax)
        {
            m_sql.Append(m_isfdc.AddPropertyForFilter(ax.Name));
        }
    }
}
