// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    ///a conditioned statement that inserts a chunck of tsql outside the main select</summary>
    [ComVisible(false)]
    internal abstract class SqlConditionedStatement : ConditionedSql
    {
        String m_sql;	//sql to be added

        /// <summary>
        ///initialize with xml reader</summary>
        protected SqlConditionedStatement(XmlReadConditionedStatement xrcs)
        {
            SetFields(xrcs.Fields);
            m_sql = xrcs.Sql;

            AddLinkMultiple(xrcs.MultipleLink);
        }

        /// <summary>
        ///get the tsql witl link_mulitple resolved</summary>
        public String GetLocalSql(SqlObjectBase obj)
        {
            if( null != m_sql )
            {
                return m_sql;
            }
            return this.LinkMultiple.GetSqlExpression(obj);
        }
    }

    /// <summary>
    ///encapsulates prefix</summary>
    [ComVisible(false)]
    internal class SqlConditionedStatementPrefix : SqlConditionedStatement
    {
        /// <summary>	
        ///initialize with xml reader</summary>
        public SqlConditionedStatementPrefix(XmlReadConditionedStatement xrcs) : base(xrcs)
        {
        }

        /// <summary>
        ///read all prefixes</summary>
        public static void AddAll(ConditionedSqlList list, XmlReadConditionedStatementPrefix xrcs)
        {
            if( null != xrcs )
            {
                do
                {
                    list.Add(new SqlConditionedStatementPrefix(xrcs));
                }
                while( xrcs.Next() );
            }
        }

        /// <summary>
        ///add hit for the field</summary>
        public override void AddHit(string field, SqlObjectBase obj, StatementBuilder sb)
        {
            sb.AddPrefix(this.GetLocalSql(obj));
        }
    }


    /// <summary>
    ///encapsulates postfix</summary>
    [ComVisible(false)]
    internal class SqlConditionedStatementPostfix : SqlConditionedStatement
    {
        /// <summary>	
        ///initialize from xml reader</summary>
        public SqlConditionedStatementPostfix(XmlReadConditionedStatement xrcs) : base(xrcs)
        {
        }

        /// <summary>
        ///read all postfixes</summary>
        public static void AddAll(ConditionedSqlList list, XmlReadConditionedStatementPostfix xrcs)
        {
            if( null != xrcs )
            {
                do
                {
                    list.Add(new SqlConditionedStatementPostfix(xrcs));
                }
                while( xrcs.Next() );
            }
        }

        /// <summary>
        ///add hit for field</summary>
        public override void AddHit(string field, SqlObjectBase obj, StatementBuilder sb)
        {
            sb.AddPostfix(this.GetLocalSql(obj));
        }
    }


    /// <summary>
    ///encapsulates a failed condition statement</summary>
    [ComVisible(false)]
    internal class SqlConditionedStatementFailCondition : SqlConditionedStatement
    {
        /// <summary>	
        ///initialize with xml reader</summary>
        public SqlConditionedStatementFailCondition(XmlReadConditionedStatement xrcs) : base(xrcs)
        {
        }

        /// <summary>
        ///add all fail conditions</summary>
        public static void AddAll(ConditionedSqlList list, XmlReadConditionedStatementFailCondition xrcs)
        {
            if( null != xrcs )
            {
                do
                {
                    list.Add(new SqlConditionedStatementFailCondition(xrcs));
                }
                while( xrcs.Next() );
            }
        }

        /// <summary>
        ///add hit for field</summary>
        public override void AddHit(string field, SqlObjectBase obj, StatementBuilder sb)
        {
            sb.AddCondition(this.GetLocalSql(obj));
        }
    }

    internal class SqlConditionedStatementWhereClause : SqlConditionedStatement
    {
        public SqlConditionedStatementWhereClause(XmlReadSpecialQuery xrcs) : base(xrcs)
        {
        }

        public static void AddAll(ConditionedSqlList list, XmlReadSpecialQuery xrcs)
        {
            if (null != xrcs)
            {
                do
                {
                    list.Add(new SqlConditionedStatementWhereClause(xrcs));
                }
                while (xrcs.Next());
            }
        }
        /// <summary>
        ///add hit for field</summary>
        public override void AddHit(string field, SqlObjectBase obj, StatementBuilder sb)
        {
            sb.AddWhere(this.GetLocalSql(obj));
        }
    }
}
