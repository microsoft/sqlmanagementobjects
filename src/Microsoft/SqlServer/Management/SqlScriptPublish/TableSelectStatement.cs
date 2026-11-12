// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// Gets select statement for a table
    /// </summary>
    internal class TableSelectStatement
    {
        #region Private Fields
        private Table table;
        private bool hasWritableColumns;
        private bool hasUserDefinedType;
        private string columnNames;
        private string tableName;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an instance of TableSelect
        /// </summary>
        /// <param name="table">Table whose data is to be enumerated as INSERT strings</param>
        public TableSelectStatement(Table table)
        {
            this.table = table;

            this.tableName = table.FullQualifiedName;

            this.hasWritableColumns = false;

            StringBuilder columnNameSQL = new StringBuilder();

            bool firstColumn = true;

            foreach (Column col in this.table.Columns)
            {
                // we need to ignore timestamp values because it gets
                // automatically populated when a row is inserted or updated
                if (col.DataType.SqlDataType == SqlDataType.Timestamp ||
                    col.Computed)
                {
                    continue;
                }

                if (firstColumn == false)
                {
                    // Append the commas after existing columnName and selectSql statements
                    columnNameSQL.Append(", ");
                }
                firstColumn = false;

                //we need to know if table has a UDT column because these kind of column cannot be published 
                //inside a dataset. (Passing a dataset with table and UDT column over a web service will failed.)
                if (col.DataType.SqlDataType == SqlDataType.UserDefinedType)
                {
                    this.hasUserDefinedType = true;
                }

                columnNameSQL.Append(String.Format(CultureInfo.InvariantCulture, "[{0}]", col.Name));
            }


            // If there are no columns which can be read then set hasWritableColumns to false
            //
            this.hasWritableColumns = (columnNameSQL.Length > 0);
            if (this.hasWritableColumns)
            {
                this.columnNames = columnNameSQL.ToString();
            }

        }
        #endregion

        #region Internal Methods

        /// <summary>
        /// Returns whether or not we have a user defined type.
        /// </summary>
        internal bool HasUserDefinedType
        {
            get
            {
                return this.hasUserDefinedType;
            }
        }

        /// <summary>
        /// Returns whether or not there's anything to be scripted from this table.
        /// </summary>
        internal bool HasWritableColumns
        {
            get
            {
                return this.hasWritableColumns;
            }
        }

        /// <summary>
        /// Returns the table name
        /// </summary>
        internal string TableName
        {
            get
            {
                return this.tableName;
            }
        }

        /// <summary>
        /// Returns a SqlBulkCopy object representing the data
        /// for the table.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// If there are no Writable column in the table
        /// </exception>
        /// 
        internal string GetSelectStatement()
        {
            if (!this.HasWritableColumns)
            {
                Debug.Assert(false, "This method should not have been called when there is no writable column");
                throw new InvalidOperationException();
            }

            return string.Format(CultureInfo.InvariantCulture,
                    "SELECT {0} FROM {1}",
                    this.columnNames,
                    this.tableName);
        }

        #endregion
    }

}

