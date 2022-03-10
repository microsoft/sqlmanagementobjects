// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Runtime.InteropServices;

    ///	<summary>
    ///class encapsulating a property_link
    ///used do indicate joins</summary>
    [ComVisible(false)]
    internal class SqlPropertyLink : ConditionedSql
    {
        enum JoinType { Classic, Inner, Left };
        String m_table;		//table necessary for property
        String m_filter;	// to be added in the where clause
        String m_alias;
        bool m_bExpressionIsForTableName;
        JoinType m_joinType;

        ///	<summary>
        ///initialize with reader from xml
        ///by reading a property_link tag</summary>
        public SqlPropertyLink(XmlReadPropertyLink xrpl)
        {
            SetFields(xrpl.Fields);
            m_table = xrpl.Table;
            m_alias = xrpl.TableAlias;
            m_bExpressionIsForTableName = xrpl.ExpressionIsForTableName;
            if( !m_bExpressionIsForTableName )
            {
                if( null != m_table  )
                {
                    m_joinType = JoinType.Classic;
                }
                else
                {
                    m_table = xrpl.InnerJoin;
                    if( null != m_table  )
                    {
                        m_joinType = JoinType.Inner;
                    }
                    else
                    {
                        m_table = xrpl.LeftJoin;
                        if( null != m_table  )
                        {
                            m_joinType = JoinType.Left;
                        }
                    }
                }
            }
            
            m_filter = xrpl.Filter;

            AddLinkMultiple(xrpl.MultipleLink);
        }

        ///	<summary>
        ///initialize by reading a property tag ( attribute table )</summary>
        public SqlPropertyLink(XmlReadProperty xrp)
        {
            StringCollection sc = new StringCollection();
            sc.Add(xrp.Name);
            SetFields(sc);
            m_table = xrp.Table;
            m_joinType = JoinType.Classic;
            m_filter = xrp.Link;
        }

        ///	<summary>
        ///initialize by reading a setting tag ( attribute main_table )</summary>
        public SqlPropertyLink(XmlReadSettings xrs)
        {
            SetFields(new StringCollection());
            m_table = xrs.MainTable;
            m_joinType = JoinType.Classic;
            m_filter = xrs.AdditionalFilter;
        }

        ///	<summary>
        ///add all property_link tags</summary>
        public static void AddAll(ConditionedSqlList list, XmlReadPropertyLink xrpl)
        {
            do
            {
                list.Add(new SqlPropertyLink(xrpl));
            }
            while( xrpl.Next() );
        }

        ///	<summary>
        ///add from a property tag</summary>
        public static void Add(ConditionedSqlList list, XmlReadProperty xrp)
        {
            if( xrp.HasPropertyLink )
            {
                list.Add(new SqlPropertyLink(xrp));
            }
        }

        ///	<summary>
        ///add from setting tag</summary>
        public static void Add(ConditionedSqlList list, XmlReadSettings xrs)
        {
            if( xrs.HasPropertyLink )
            {
                list.Add(new SqlPropertyLink(xrs));
            }
        }

        ///	<summary>
        ///get set the joined table name</summary>
        public String Table
        {
            get { return m_table; }
            set { m_table = value; }
        }

        ///	<summary>
        ///get the table name with alias in tsql format</summary>
        public String GetTableNameWithAlias(SqlObjectBase obj)
        {
            string tblName = m_bExpressionIsForTableName ? this.LinkMultiple.GetSqlExpression(obj) : m_table;
            if( null == m_alias )
            {
                return tblName;
            }
            return tblName + " AS " + m_alias;
        }

        ///	<summary>
        ///get the filter for the join</summary>
        public String GetFilter(SqlObjectBase obj)
        {
            if( null == this.LinkMultiple || m_bExpressionIsForTableName )
            {
                return m_filter;
            }

            return this.LinkMultiple.GetSqlExpression(obj);
        }

        ///	<summary>
        ///add hit for this field 
        ///update the StatementBuilder</summary>
        public override void AddHit(string field, SqlObjectBase obj, StatementBuilder sb)
        {
            obj.AddLinkProperties(LinkFieldType.Local, this.LinkFields);
            string s = this.GetFilter(obj);
            string tblName = this.GetTableNameWithAlias(obj);

            if( null == tblName || tblName.Length <= 0 )
            {
                sb.AddWhere(s);
                return;
            }
            
            //if it is the first join add also the parent link if exists
            if( sb.IsFirstJoin ) //is the first table for this object
            {
                if( null != obj.ParentLink )
                {
                    if( null == s || s.Length <= 0 )
                    {
                        s = obj.ParentLink.LinkMultiple.GetSqlExpression(obj);
                    }
                    else
                    {
                        s = "(" + s + ") AND (" + obj.ParentLink.LinkMultiple.GetSqlExpression(obj) + ")";
                    }
                }
                else
                {
                    //if requested calssic join and not on an intermediate level, do classic join
                    if( JoinType.Classic == m_joinType )
                    {
                        sb.AddFrom(tblName);
                        if( null != s && s.Length > 0 )
                        {
                            sb.AddWhere(s);
                        }
                        return;
                    }
                }
            }
            
            string prefix;
            if( JoinType.Left == m_joinType )
            {
                prefix = "LEFT OUTER JOIN ";
            }
            else
            {
                prefix = "INNER JOIN ";				
            }
            string join = prefix + tblName + 
                                        String.Format(CultureInfo.InvariantCulture, " ON {0}", s);

            sb.AddJoin(join);
        }
    }
}
