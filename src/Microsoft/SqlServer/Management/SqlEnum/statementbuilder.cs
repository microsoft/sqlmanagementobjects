// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

    ///	<summary>
    ///constructs the tsql that will get the data	</summary>
    [ComVisible(false)]
    internal class StatementBuilder
    {
        StringBuilder m_urn;
        StringBuilder m_prefix;
        StringBuilder m_fields;
        StringBuilder m_from;
        StringBuilder m_where;
        StringBuilder m_postfix;
        StringBuilder m_orderBy;
        StringBuilder m_optionHint;
        ArrayList m_ParentProps;
        int m_NonTriggeredProps;
        StringBuilder m_condition;
        SortedList m_postProcess;
        bool m_bDistinct;
        int m_topN;
        bool m_bStoredPropsAdded;
        StringBuilder m_InternalSelect;
        bool bFirstJoinIsClassic;

        ///	<summary>
        ///default constructor</summary>
        public StatementBuilder()
        {
            m_urn = new StringBuilder();
            m_prefix = new StringBuilder();
            m_fields = new StringBuilder();
            m_from = new StringBuilder();
            m_where = new StringBuilder();
            m_postfix = new StringBuilder();
            m_orderBy = new StringBuilder();
            m_condition = new StringBuilder();
            m_optionHint = new StringBuilder();
            m_ParentProps = new ArrayList();
            m_postProcess = new SortedList(System.StringComparer.Ordinal);
            m_NonTriggeredProps = 0;
            Distinct = false;
            m_topN = -1;
            m_bStoredPropsAdded = false;
            m_InternalSelect = null;
            bFirstJoinIsClassic = false;
        }

        ///	<summary>
        ///list of properties selected</summary>
        internal ArrayList ParentProperties
        {
            get	{ return m_ParentProps; }
        }

        ///	<summary>
        ///set the select statement</summary>
        internal void SetInternalSelect(StringBuilder sql)
        {
            m_InternalSelect = sql;
        }

        ///	<summary>
        ///position from where the triggered ( not requested by user ) 
        /// properties start</summary>
        internal int NonTriggeredProperties
        {
            get { return m_NonTriggeredProps; }
        }

        ///	<summary>
        ///list of prost processes that must be run</summary>
        internal SortedList PostProcessList
        {
            get	{ return m_postProcess; }
        }

        ///	<summary>
        ///the select must be distinct</summary>
        public bool Distinct
        {
            get	{ return m_bDistinct; }
            set	{ m_bDistinct = value; }
        }

        ///	<summary>
        ///the select must return Top N rows
        ///</summary>
        public int TopN
        {
            get { return m_topN; }
            set { m_topN = value; }
        }

        ///	<summary>
        ///this is the first table added to the from clause</summary>
        public bool IsFirstJoin
        {
            get	{ return IsEmpty(m_from); }
        }

        ///	<summary>
        ///returns true if s is empty</summary>
        public bool IsEmpty(StringBuilder s)
        {
            return null == s || s.Length <= 0;
        }

        ///	<summary>
        ///returns the FROM clause</summary>
        public StringBuilder From
        {
            get
            {
                return m_from;
            }
            set
            {
                m_from = value;
            }
        }

        ///	<summary>
        ///adds value to a string builder using the specified delimiters</summary>
        internal protected static void AddElement(StringBuilder str, String value, String delimStart, String delimEnd, String delimElems)
        {
            if( str.Length > 0 )
            {
                str.Append(delimElems);
            }

            str.Append(delimStart);
            str.Append(value);
            str.Append(delimEnd);
        }

        ///	<summary>
        ///add to URN</summary>
        public void AddUrn(String value)
        {
            AddElement(m_urn, value, string.Empty, string.Empty, "+'/'+");
        }

        ///	<summary>
        /// add to PREFIX</summary>
        public void AddPrefix(String value)
        {
            AddElement(m_prefix, value, string.Empty, "\n", "\n");
        }

        ///	<summary>
        /// add a CONDITION which if is true will make the result set be empty</summary>
        public void AddCondition(string value)
        {
            AddElement(m_condition, value, "(", ")", "or");
        }

        ///	<summary>
        ///add to POSTFIX</summary>
        public void AddPostfix(String value)
        {
            AddElement(m_postfix, value, string.Empty, "\n", "\n");
        }

        ///	<summary>
        ///add to the SELECT fields list</summary>
        public void AddFields(String value)
        {
            AddElement(m_fields, value, string.Empty, string.Empty, ",\n");
        }

        ///	<summary>
        ///add a clasic join to the FROM clause</summary>
        public void AddFrom(String value)
        {
            if( IsFirstJoin )
            {
                bFirstJoinIsClassic = true;
            }
            AddElement(m_from, value, string.Empty, string.Empty, ",\n");
        }

        ///	<summary>
        ///add a new syntax join to the FROM clause</summary>
        public void AddJoin(String value)
        {
            AddElement(m_from, value, string.Empty, string.Empty, "\n");
        }

        ///	<summary>
        ///add to the WHERE clause using AND</summary>
        public void AddWhere(String value)
        {
            AddElement(m_where, value, "(", ")", "and");
        }

        ///	<summary>
        ///add to the ORDER BY clause</summary>
        public void AddOrderBy(String str)
        {
            AddElement(m_orderBy, str, string.Empty, string.Empty, ",");
        }

        ///	<summary>
        ///add to the ORDER BY clause spcifying direction</summary>
        private void AddOrderBy(String orderByValue, OrderBy.Direction dir)
        {
            if( OrderBy.Direction.Asc == dir )
            {
                orderByValue += " ASC";
            }
            else
            {
                orderByValue += " DESC";
            }

            AddOrderBy(orderByValue);
        }

        ///	<summary>
        ///add to ODER BY clause by name if the property apears in the SELECT list
        /// if not add it by value</summary>
        public void AddOrderBy(String prop, String orderByValue, OrderBy.Direction dir)
        {
            foreach(SqlObjectProperty o in this.ParentProperties)
            {
                if( prop == o.Name )
                {
                    AddOrderBy(String.Format(CultureInfo.InvariantCulture, "[{0}]", Util.EscapeString(o.Alias, ']')), dir);
                    return;
                }
            }
            AddOrderBy(orderByValue, dir);
        }

        ///	<summary>
        ///add property to the SELECT list with alias</summary>
        public void AddProperty(String name, String value)
        {
            if(  null != value )
            {
                if( null != name )
                {
                    AddFields(String.Format(CultureInfo.InvariantCulture, "{1} AS [{0}]", Util.EscapeString(name, ']'), value));
                }
                else
                {
                    AddFields(value);
                }
            }
        }

        ///	<summary>
        ///add a post process needed to resolve the field property</summary>
        internal void AddPostProcess(string field, PostProcess postProcess)
        {
            m_postProcess[field] = postProcess;
        }

        ///	<summary>
        ///add a query hint</summary>
        internal void AddQueryHint(string hint)
        {
            m_optionHint = new StringBuilder(hint);
        }

        ///	<summary>
        ///merge two StatementBuilder instances</summary>
        public void Merge(StatementBuilder sb)
        {
            if( !IsEmpty(m_prefix) )
            {
                sb.AddPrefix(m_prefix.ToString());
            }
            m_prefix = sb.m_prefix;

            if( !IsEmpty(sb.m_fields) )
            {
                StringBuilder sbtmp = m_fields;
                m_fields = sb.m_fields;
                if( !IsEmpty(sbtmp) )
                {
                    AddFields(sbtmp.ToString());
                }
            }

            int triggeredProps = sb.m_ParentProps.Count - sb.m_NonTriggeredProps;
            m_NonTriggeredProps += sb.NonTriggeredProperties;
            if( triggeredProps > 0 )
            {
                m_ParentProps.InsertRange(0, sb.m_ParentProps.GetRange(0, sb.m_NonTriggeredProps));
                m_ParentProps.InsertRange(m_NonTriggeredProps, sb.m_ParentProps.GetRange(sb.m_NonTriggeredProps, triggeredProps));
            }
            else
            {
                m_ParentProps.InsertRange(0, sb.m_ParentProps);
            }

            foreach(DictionaryEntry de in sb.m_postProcess)
            {
                m_postProcess[de.Key] = de.Value;
            }

            StringBuilder from = new StringBuilder();
            if( !IsEmpty(sb.m_from) )
            {
                from.Append(sb.m_from);
                if( bFirstJoinIsClassic )
                {
                    from.Append(",");
                }
                from.Append("\n");
            }
            if( !IsEmpty(m_from) )
            {
                from.Append(m_from);
            }
            m_from = from;
            bFirstJoinIsClassic = sb.bFirstJoinIsClassic;

            if( !IsEmpty(sb.m_where) )
            {
                if( IsEmpty(m_where) )
                {
                    m_where.Append(sb.m_where);
                }
                else
                {
                    AddWhere(sb.m_where.ToString());
                }
            }

            if( !IsEmpty(sb.m_orderBy) )
            {
                StringBuilder sbtmp = m_orderBy;
                m_orderBy = sb.m_orderBy;
                if( !IsEmpty(sbtmp) )
                {
                    AddOrderBy(sbtmp.ToString());
                }
            }

            if( !IsEmpty(sb.m_postfix) )
            {
                AddPostfix(sb.m_postfix.ToString());
            }
            m_urn.Append(sb.m_urn);

            if( !IsEmpty(sb.m_condition) )
            {
                AddCondition(sb.m_condition.ToString());
            }

            if ( !IsEmpty(sb.m_optionHint) )
            {
                AddQueryHint(sb.m_optionHint.ToString());
            }
        }

        ///	<summary>
        ///compute the select statement</summary>
        internal StringBuilder InternalSelect()
        {
            if( null != m_InternalSelect )
            {
                return m_InternalSelect;
            }
            StringBuilder sql = new StringBuilder();
            if( IsEmpty(m_fields) )
            {
                return sql;
            }

            sql.Append("SELECT\n");
            if( true == m_bDistinct )
            {
                sql.Append("distinct ");
            }

            if (m_topN > 0)
            {
                sql.Append(string.Format(CultureInfo.InvariantCulture, "TOP ({0}) ", m_topN));
            }
            sql.Append(m_fields);
            if( !IsEmpty(m_from) )
            {
                sql.Append("\nFROM\n");
                sql.Append(m_from);
            }
            if( !IsEmpty(m_where) )
            {
                sql.Append("\nWHERE\n");
                sql.Append(m_where);
            }
            if( !IsEmpty(m_orderBy) )
            {
                sql.Append("\nORDER BY\n");
                sql.Append(m_orderBy);
            }
            if (!IsEmpty(m_optionHint))
            {
                sql.AppendFormat("\nOPTION ({0})\n", m_optionHint.ToString());
            }
            return sql;
        }

        ///	<summary>
        ///compute the whole sql statement ( including prefix / postfix )</summary>
        public String SqlStatement
        {
            get
            {
                StringBuilder sql = new StringBuilder();
                if (!IsEmpty(m_condition))
                {
                    sql.Append("if ");
                    sql.Append(m_condition);
                    sql.Append("\nbegin\n");
                    sql.Append(GetCreateTemporaryTableSqlConnect("#empty_result"));
                    sql.Append("\n");
                    sql.Append(SelectAndDrop("#empty_result", string.Empty));
                    sql.Append("\nreturn\nend\n");
                }
                if (!IsEmpty(m_prefix))
                {
                    sql.Append(m_prefix);
                    sql.Append("\n");
                }
                sql.Append(InternalSelect());
                if (!IsEmpty(m_postfix))
                {
                    sql.Append("\n");
                    sql.Append(m_postfix);
                }

                var statement = sql.ToString();

                // Test Hook: makes it easier for test cases to get the desired results
                //
                foreach (var fragment in SqlEnumStatementBuilderTestHook.SqlStatementFragmentsToBeReplaced)
                {
                    statement = statement.Replace(fragment.Key, fragment.Value);
                }

                return statement;
            }
        }

        ///	<summary>
        ///get the postfix statement</summary>
        public String SqlPostfix
        {
            get
            {
                return m_postfix.ToString();
            }
        }

        ///	<summary>
        ///clear the prefix and the postfix statements</summary>
        internal void ClearPrefixPostfix()
        {
            m_prefix = null;
            m_postfix = null;
            m_condition = null;
        }

        ///	<summary>
        ///make a copy of this StatementBuilder</summary>
        public StatementBuilder MakeCopy()
        {
            StatementBuilder sb = new StatementBuilder();

            sb.m_urn.Append(m_urn);
            sb.m_prefix.Append(m_prefix);
            sb.m_fields.Append(m_fields);
            sb.bFirstJoinIsClassic = bFirstJoinIsClassic;

            int triggeredProps = m_ParentProps.Count - m_NonTriggeredProps;
            if( triggeredProps > 0 )
            {
                sb.m_ParentProps.InsertRange(sb.m_NonTriggeredProps, m_ParentProps.GetRange(0, m_NonTriggeredProps));
                sb.m_ParentProps.AddRange(m_ParentProps.GetRange(m_NonTriggeredProps, triggeredProps));
            }
            else
            {
                sb.m_ParentProps.AddRange(m_ParentProps);
            }

            sb.m_NonTriggeredProps += this.NonTriggeredProperties;

            foreach(DictionaryEntry de in m_postProcess)
            {
                sb.m_postProcess[de.Key] = de.Value;
            }

            sb.m_from.Append(m_from);
            sb.m_where.Append(m_where);
            sb.m_orderBy.Append(m_orderBy);
            sb.m_postfix.Append(m_postfix);
            return sb;
        }

        ///	<summary>
        ///add a property that is requested in the SELECT
        /// if bTriggered is true thanm the property is not directly requested by the user
        /// but is needed to resolve one of the properties requested by the user</summary>
        internal void StoreParentProperty(SqlObjectProperty sop, bool bTriggered)
        {
            m_ParentProps.Add(sop);
            if( !bTriggered )
            {
                m_NonTriggeredProps++;
            }
        }

        ///	<summary>
        /// clear the prefix and postfix and get the remaining sql statement</summary>
        public string GetSqlNoPrefixPostfix()
        {
            AddStoredProperties();
            ClearPrefixPostfix();
            return SqlStatement;
        }

        ///	<summary>
        ///add the internally stored properties in the SELECT clause</summary>
        internal void AddStoredProperties()
        {
            if( !m_bStoredPropsAdded )
            {
                m_bStoredPropsAdded = true;
                foreach(SqlObjectProperty p in m_ParentProps)
                {
                    this.AddProperty(p.Alias, p.SessionValue);
                }
            }
        }

        ///	<summary>
        ///get the order by statement</summary>
        internal string GetOrderBy()
        {
            return this.m_orderBy.ToString();
        }

        ///	<summary>
        ///clear the order by statement</summary>
        internal void ClearOrderBy()
        {
            this.m_orderBy.Length = 0;
        }

        ///	<summary>
        ///sql that creates a temporary table that can hold the requested properties for 
        /// this object-> it can only be asked for the last object in xpath</summary>
        internal String GetCreateTemporaryTableSqlConnect(String tableName)
        {
            StringBuilder sqlCreate = new StringBuilder();
            sqlCreate.Append("\ncreate table ");
            sqlCreate.Append(tableName);
            sqlCreate.Append("(");
            bool bFirst = true;

            if( null != this.ParentProperties )
            {
                foreach(SqlObjectProperty p in this.ParentProperties)
                {
                    AddColumn(sqlCreate, p, ref bFirst, true);
                }
            }

            sqlCreate.Append(")\n");
            return sqlCreate.ToString();
        }

        ///	<summary>
        ///add a column in the create temporary table statement</summary>
        private void AddColumn(StringBuilder sqlCreate, SqlObjectProperty prop, ref bool bFirst, bool bUseAlias)
        {
            if( true == bFirst )
            {
                bFirst = false;
            }
            else
            {
                sqlCreate.Append(",");
            }

            string name = prop.Name;
            if( bUseAlias )
            {
                name = prop.Alias;
            }

            if( null == prop.Size )
            {
                sqlCreate.Append(String.Format(CultureInfo.InvariantCulture, "[{0}] {1} NULL", name, prop.DBType));
            }
            else
            {
                sqlCreate.Append(String.Format(CultureInfo.InvariantCulture, "[{0}] {1}({2}) NULL", name, prop.DBType, prop.Size));
            }
        }

        ///	<summary>
        ///generate tsql to select from the specified temporary table and then drop it</summary>
        static internal string SelectAndDrop(string tableName, string sOrderBy)
        {
            StatementBuilder sb = new StatementBuilder();
            sb.AddFields("*");
            sb.AddFrom(tableName);
            sb.AddPostfix("drop table " + tableName);
            sb.AddOrderBy(sOrderBy);

            return sb.SqlStatement;
        }

        ///	<summary>
        ///clear the CONDITION which if true makes the result be enpty</summary>
        internal void ClearFailCondition()
        {
            m_condition = new StringBuilder();
        }
    }
}
