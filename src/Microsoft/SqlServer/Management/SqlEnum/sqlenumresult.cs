// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;


    /// <summary>
    ///derives from the enumerator EnumResult too add sql extension specific functionality</summary>
    [ComVisible(false)]
    internal class SqlEnumResult : EnumResult
    {
        DataTable m_databases;
        // This variable and related code is used specifically by the Notification Services team and should be looped in on any changes.
        DataTable m_SchemaPrefixes;
        StringCollection m_NameProperties;
        StringCollection m_SchemaPrefixProperties;
        bool m_LastDbLevelSet;
        SortedList m_SpecialQuery;
        String m_QueryHint;
        StringCollection m_PostProcessFields;
        DatabaseEngineType dbType;
        

        /// <summary>
        ///constructor, receives a StatementBuilder and a ResultType</summary>
        public SqlEnumResult(Object ob, ResultType resultType , DatabaseEngineType dbType) : base(ob, resultType)
        {
            m_LastDbLevelSet = false;
            this.dbType = dbType;
        }

        /// <summary>
        ///unused ????</summary>
        public StringCollection PostProcessFields
        {
            get
            {
                if( null == m_PostProcessFields )
                {
                    m_PostProcessFields = new StringCollection();
                }
                return m_PostProcessFields;
            }
        }

        /// <summary>
        ///property name for database used in DatabaseLevel</summary>
        public StringCollection NameProperties
        {
            get
            {
                if( null == m_NameProperties )
                {
                    m_NameProperties = new StringCollection();
                }
                return m_NameProperties;
            }
            set
            {
                m_NameProperties = value;
            }
        }

        /// <summary>
        /// property name for schema used in the DatabaseLevel</summary>
        public StringCollection SchemaPrefixProperties
        {
            get
            {
                if (null == m_SchemaPrefixProperties)
                {
                    m_SchemaPrefixProperties = new StringCollection ();
                }
                return m_SchemaPrefixProperties;
            }
            set
            {
                m_SchemaPrefixProperties = value;
            }
        }

        /// <summary>
        ///special query</summary>
        internal SortedList SpecialQuery
        {
            get 
            { 
                return m_SpecialQuery; 
            }
        }

        /// <summary>
        ///special query</summary>
        internal String QueryHint
        {
            get
            {
                return m_QueryHint;
            }
        }

        /// <summary>
        ///no further procesing is necessary for the database level
        ///set in DatabaseLevel</summary>
        public bool LastDbLevelSet
        {
            get
            {
                return m_LastDbLevelSet;
            }
            set
            {
                m_LastDbLevelSet = value;
            }
        }

        /// <summary>
        ///get/set the StatementBuilder</summary>
        public StatementBuilder StatementBuilder
        {
            get
            {
                return (StatementBuilder)this.Data;
            }
            set
            {
                this.Data = value;
            }
        }

        /// <summary>
        ///How many databases deep are we</summary>
        public int Level
        {
            get
            {
                if( null == m_databases )
                {
                    return 0;
                }
                if( 0 >= m_databases.Rows.Count )
                {
                    return 1;
                }
                return m_databases.Columns.Count;
            }
        }

        /// <summary>
        ///list of databases trough which the query 
        ///must be executed can have multiple database levels</summary>
        public DataTable Databases
        {
            get
            {
                return m_databases;
            }
            set
            {
                m_databases = value;
            }
        }

        /// <summary>
        /// list of schemas that must be substituted into the query
        /// </summary>
        // This property and related code is used specifically by the Notification Services team and should be looped in on any changes.
        public DataTable SchemaPrefixes
        {
            get
            {
                return m_SchemaPrefixes;
            }
            set
            {
                m_SchemaPrefixes = value;
            }
        }

        /// <summary>
        ///add the special query for the specified database</summary>
        internal void AddSpecialQuery(string database, string query)
        {
            if( null == m_SpecialQuery )
            {
                m_SpecialQuery = new SortedList(System.StringComparer.Ordinal);
            }
            m_SpecialQuery.Add(database, query);
        }

        /// <summary>
        ///add the special query for the specified database</summary>
        internal void AddQueryHint(string hint)
        {
            m_QueryHint = hint;
        }

        /// <summary>
        ///build the tsql for a database</summary>
        private string GetSql(DataRow dbs, string sql)
        {
            if( !this.LastDbLevelSet )
            {
                switch(this.Level)
                {
                    case 1:
                        return String.Format(CultureInfo.InvariantCulture, sql, "db_name()");
                    case 2:
                        return String.Format(CultureInfo.InvariantCulture, sql, 
                            "'" + Util.EscapeString(dbs[0].ToString(), '\'') + "'", 
                            Util.EscapeString(dbs[0].ToString(), ']'), 
                            "db_name()");
                    default:
                        throw new InternalEnumeratorException(StringSqlEnumerator.TooManyDbLevels);
                }
            }
            else
            {
                switch(this.Level)
                {
                    case 1:
                        return sql;
                    case 2:
                        return String.Format(CultureInfo.InvariantCulture, sql, 
                            "'" + Util.EscapeString(dbs[0].ToString(), '\'') + "'", 
                            Util.EscapeString(dbs[0].ToString(), ']'));
                    default:
                        throw new InternalEnumeratorException(StringSqlEnumerator.TooManyDbLevels);
                }
            }
        }

        /// <summary>
        ///get the use statement for this database list row
        /// ( its a use in the rightmost database name )</summary>
        private string GetUse(DataRow dbs)
        {
            return "use " + String.Format(CultureInfo.InvariantCulture, "[{0}]", Util.EscapeString(dbs[this.Level - 1].ToString(), ']'));
        }

        // This function and related code is used specifically by the Notification Services team and should be looped in on any changes.
        private string SubstituteSchemaPrefix(DataRow dbs, string sql)
        {
            if (dbs == null)
            {
                return sql;
            }

            return sql.Replace ("[SchemaPrefix]", String.Format(CultureInfo.InvariantCulture, "[{0}]", Util.EscapeString(dbs[this.Level - 1].ToString(), ']')));
        }

        /// <summary>
        ///true if the tsql will have to be run in more than one database</summary>
        internal bool MultipleDatabases
        {
            get
            {
                return null != this.m_databases && this.m_databases.Rows.Count != 1;
            }
        }

        /// <summary>
        ///compare all but the last element
        ///comparisons are made in the most restricted way , without collation because 
        ///the database names are all from sysdatabases , so they must mach exactly in order to be equal.</summary>
        private bool IsDatabaseListEqual(DataRow db1, DataRow db2)
        {
            for(int i = 0; i < this.Level - 1; i++)
            {
                if( db1[i].ToString() != db2[i].ToString() )
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///return the tsql that will provide the result for the user</summary>
        public StringCollection BuildSql()
        {
            this.StatementBuilder.AddStoredProperties();
            StringCollection scQuery = new StringCollection();
            // If the user has specified a desired isolation level for SMO queries, let's set that before the main query
            var isolationStatement = QueryIsolation.GetQueryPrefix();
            if (!string.IsNullOrEmpty(isolationStatement))
            {
                scQuery.Add(isolationStatement);
            }
            if (0 >= this.Level)
            {
                scQuery.Add(this.StatementBuilder.SqlStatement);
            }
            else
            {

                String sOrderBy = String.Empty;
                string unifTable = null;
                if (this.MultipleDatabases)
                {
                    unifTable = "[#unify_temptbl" +
                                (DateTime.Now - DateTime.MinValue).TotalMilliseconds.ToString(CultureInfo.InvariantCulture) +
                                "]";
                    scQuery.Add(this.StatementBuilder.GetCreateTemporaryTableSqlConnect(unifTable));
                    this.StatementBuilder.AddPrefix(" insert into " + unifTable);
                    sOrderBy = this.StatementBuilder.GetOrderBy();
                    this.StatementBuilder.ClearOrderBy();
                }

                if (!String.IsNullOrEmpty(m_QueryHint))
                {
                    this.StatementBuilder.AddQueryHint(m_QueryHint);
                }

                String sQuery = this.StatementBuilder.SqlStatement;

                DataRow prevRow = null;
                String prevSql = null;
                bool bSpecialCase = false;
                //foreach(DataRow dbs in m_databases.Rows)
                for (int rowIndex = 0; rowIndex < m_databases.Rows.Count; ++ rowIndex)
                {
                    DataRow dbs = m_databases.Rows[rowIndex];
                    // Code related to Schema Prefixes is owned by the Notification Services team and should be looped in on any changes.
                    DataRow schemaPrefix = null;
                    if ((m_SchemaPrefixes != null) && (rowIndex < m_SchemaPrefixes.Rows.Count))
                    {
                        schemaPrefix = m_SchemaPrefixes.Rows[rowIndex];
                    }

                    //Check if database in not cloud , only then add Use Dbs
                    if (this.dbType != DatabaseEngineType.SqlAzureDatabase)
                    {
                        scQuery.Add(GetUse(dbs));
                    }

                    //we cover only one special case on Tables right now
                    //we search case insenzitive, we might get into more databases, but it will not hurt
                    //for our only supported special case on Tables.
                    if (null != m_SpecialQuery && 1 == m_SpecialQuery.Count && 1 == this.Level &&
                        0 ==
                        String.Compare((string) m_SpecialQuery.GetKey(0), (string) dbs[this.Level - 1],
                            StringComparison.OrdinalIgnoreCase))
                    {
                        StatementBuilder b = this.StatementBuilder.MakeCopy();
                        b.AddWhere((string) m_SpecialQuery[dbs[this.Level - 1]]);
                        prevSql = GetSql(dbs, b.SqlStatement);
                        prevRow = dbs;
                        bSpecialCase = true;
                    }
                    else if (null == prevRow || true == bSpecialCase || !IsDatabaseListEqual(prevRow, dbs))
                    {
                        prevSql = GetSql(dbs, sQuery);
                        prevRow = dbs;
                        bSpecialCase = false;
                    }
                    // Code related to Schema Prefixes is owned by the Notification Services team and should be looped in on any changes.
                    prevSql = SubstituteSchemaPrefix(schemaPrefix, prevSql);
                    scQuery.Add(prevSql);
                }

                if (this.MultipleDatabases)
                {
                    scQuery.Add(StatementBuilder.SelectAndDrop(unifTable, sOrderBy));
                }
            }
            // Append the reset of our isolation level to the query. 
            // Note that we have to append it to the last string in the collection, 
            // since the last string is executed as a results fetch.
            isolationStatement = QueryIsolation.GetQueryPostfix();
            if (!string.IsNullOrEmpty(isolationStatement))
            {
                scQuery[scQuery.Count - 1] = string.Format("{0}\n{1}", scQuery[scQuery.Count - 1], isolationStatement);
            }
            return scQuery;
        }

        /// <summary>
        ///get the tsql the would be run in a single database, without 'use'</summary>
        internal string GetSingleDatabaseSql()
        {
            if( null == m_databases )
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.NotDbObject);
            }
            if( 1 != m_databases.Rows.Count )
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.NotSingleDb);
            }
            return GetSql(m_databases.Rows[0], this.StatementBuilder.SqlStatement);
        }
    }
}
            
