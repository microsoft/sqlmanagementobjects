// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;

    using System.Data;
    using System.Globalization;

    using System.Text;


    /// <summary>
    /// A class that post processes table properties    
    /// </summary>
    internal class PostProcessTable : PostProcess
    {
        private DataRow rowResults;
        private string databaseName;
        private string schemaName;
        private string tableName;
        private string query;

        // post processed table properties
        private const string rowCount = "RowCount";

        /// <summary>
        /// Default constructor
        /// </summary>
        public PostProcessTable()
        {
            this.rowResults = null;
            this.databaseName = string.Empty;
            this.schemaName = string.Empty;
            this.tableName = string.Empty;
            this.query = string.Empty;
        }

        /// <summary>
        /// Execute query to get values for table properties
        /// </summary>
        private void GetRowResults(DataProvider dp)
        {
            if (null == this.rowResults)
            {
                this.databaseName = GetTriggeredString(dp, 0);
                this.schemaName = GetTriggeredString(dp, 1);
                this.tableName = GetTriggeredString(dp, 2);   

                DataTable dt = null;
                this.BuildQuery();

                //Don't pool the connection since we're connecting directly to the DB
                dt = ExecuteSql.ExecuteWithResults(this.query, this.ConnectionInfo, this.databaseName, poolConnection:false);

                if ((dt != null) && (dt.Rows.Count > 0))
                {
                    this.rowResults = dt.Rows[0];
                }

            }            
        }

        public override void CleanRowData()
        {
            this.rowResults = null;
        }

        /// <summary>
        /// Build T-Sql queries to get values for table properties.        
        /// </summary>        
        private void BuildQuery()
        {
            StringBuilder sb = new StringBuilder();

            // build queries for table properties
            StatementBuilder selectQuery = new StatementBuilder();
            if (GetIsFieldHit(rowCount))
            {
                string queryRowCount = String.Format(CultureInfo.InvariantCulture, @"(CASE WHEN (tbl.is_memory_optimized=0) 
                            THEN ISNULL((SELECT SUM (spart.rows) FROM sys.partitions spart WHERE spart.object_id = tbl.object_id AND spart.index_id < 2), 0)
                            ELSE ISNULL((SELECT COUNT(*) FROM [{0}].[{1}]), 0) END)", Util.EscapeString(this.schemaName, ']'), Util.EscapeString(this.tableName, ']'));
                
                // add property to SELECT list
                selectQuery.AddProperty(rowCount, queryRowCount);
            }
            sb.Append(selectQuery.SqlStatement);
                                   
            string queryFromTable = String.Format(CultureInfo.InvariantCulture, "FROM sys.tables tbl WHERE SCHEMA_NAME(tbl.schema_id)=N'{0}' AND tbl.name=N'{1}'", 
                                    Util.EscapeString(this.schemaName, '\''), Util.EscapeString(this.tableName, '\''));
            sb.Append(queryFromTable);
            
            this.query = sb.ToString();
        }

        /// <summary>
        /// Returns the value of required property
        /// </summary>
        /// <param name="name">Name of a table property</param>
        /// <param name="data">data</param>
        /// <param name="dp">data provider</param>
        /// <returns>Value of the property</returns>
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            this.GetRowResults(dp);
            if (this.rowResults == null)
            {
                data = DBNull.Value;
            }
            else
            {
                switch (name)
                {
                    case rowCount:
                        data = Convert.ToInt64(this.rowResults[rowCount]);
                        break;
                }
            }
            return data;
        }
            
    }
}
