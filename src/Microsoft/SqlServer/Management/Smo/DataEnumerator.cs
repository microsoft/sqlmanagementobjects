// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Data.SqlTypes;
using System.Data;
using System.Diagnostics;
using System.Globalization;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// DataEnumerator manages the lazy load of the SQLDataReader that will be iterated through
    /// for table data INSERT script generation
    /// </summary>
    internal class DataEnumerator : IEnumerator<string>, IDisposable
    {
        #region Private Fields
        private SqlDataReader reader;
        private SqlConnection conn;
        private Database database;
        private Dictionary<string, SqlDataType> columnDataType;
        private Dictionary<string, int> columnNumericPrecision;
        private Dictionary<string, int> columnNumericScale;
        private Dictionary<string, string> columnCollation;
        private string tableName;
        private string schemaQualifiedTableName;
        private ScriptingPreferences options;
        private string insertPrefix;
        private string selectCommand;
        private string columnNames;
        private bool hasIdentity;
        private bool hasPersisted;
        private bool hasWritableColumns;
        private string currentScriptString;
        private EnumeratorState state = EnumeratorState.NotStarted;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an instance of DataEnumerator
        /// </summary>
        /// <param name="table">Table whose data is to be enumerated as INSERT strings</param>
        /// <param name="options">Scripting Options</param>
        internal DataEnumerator(Table table, ScriptingPreferences options)
        {
            this.database = table.Parent;
            this.columnNumericScale = new Dictionary<string, int>(this.database.StringComparer);
            this.columnNumericPrecision = new Dictionary<string, int>(this.database.StringComparer);
            this.columnCollation = new Dictionary<string, string>(this.database.StringComparer);
            this.columnDataType = new Dictionary<string, SqlDataType>(this.database.StringComparer);
            this.options = options;
            this.tableName = table.FormatFullNameForScripting(options);

            ScriptingPreferences optionWithSchemaQualify = (ScriptingPreferences)options.Clone();
            optionWithSchemaQualify.IncludeScripts.SchemaQualify = true;
            this.schemaQualifiedTableName = table.FormatFullNameForScripting(optionWithSchemaQualify);

            //// Load the column Names for the table and the select statement
            ////

            StringBuilder columnNameSQL;
            StringBuilder selectSQL;
            this.hasPersisted = false;
            GetColumnNamesAndSelectSQL(out columnNameSQL, out selectSQL, options, table);

            // If there are no columns which can be read then set hasWritableColumns to false
            //
            this.hasWritableColumns = (columnNameSQL.Length > 0);
            if (this.hasWritableColumns)
            {
                this.columnNames = columnNameSQL.ToString();

                this.insertPrefix = String.Format(CultureInfo.InvariantCulture,
                                        "INSERT {0} ({1}) VALUES (",
                                        this.tableName,
                                        columnNameSQL.ToString());

                // In Hekaton M5, READ COMMITTED is not supported for SELECT statement of a memory optimized table, therefore we provide SNAPSHOT hint. 
                if (table.IsSupportedProperty("IsMemoryOptimized") && table.IsMemoryOptimized)
                {
                    this.selectCommand = String.Format(CultureInfo.InvariantCulture,
                     "SELECT {0} FROM {1} WITH (SNAPSHOT)", selectSQL, this.schemaQualifiedTableName);
                }
                else
                {
                    this.selectCommand = String.Format(CultureInfo.InvariantCulture,
                     "SELECT {0} FROM {1}", selectSQL, this.schemaQualifiedTableName);
                }
            }
        }
        #endregion


        #region IEnumerator Members


        public object Current
        {
            get
            {
                if (state == EnumeratorState.NotStarted ||
                    state == EnumeratorState.Finished)
                {
                    throw new InvalidOperationException();
                }

                return this.currentScriptString;
            }
        }

        /// <summary>
        /// Retrieves the current INSERT statement string
        /// </summary>
        string IEnumerator<string>.Current
        {
            get
            {
                if (state == EnumeratorState.NotStarted ||
                    state == EnumeratorState.Finished)
                {
                    throw new InvalidOperationException();
                }

                return this.currentScriptString;
            }
        }

        /// <summary>
        /// Moves to the next row of the table
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            bool? moveNext = null;
            UserOptions userOptions = null;
            switch (state)
            {
                case EnumeratorState.NotStarted:

                    bool hasData = false;

                    // If there are writable columns then establish a connection and 
                    // check if there is anything to write
                    //
                    if (hasWritableColumns == true)
                    {
                        SqlCommand cmd = new SqlCommand(this.selectCommand, this.Connection);
                        this.reader = cmd.ExecuteReader();

                        hasData = reader.Read();
                    }

                    // If there is nothing to write then
                    // return false and set the state to finished
                    //
                    if (hasData == false)
                    {
                        currentScriptString = null;
                        state = EnumeratorState.Finished;
                        moveNext = false;
                    }
                    else
                    {
                        // If ANSI_PADDING need to be set on due to persisted column
                        if (this.hasPersisted)
                        {
                            currentScriptString =
                                String.Format(CultureInfo.InvariantCulture,
                                    "SET ANSI_PADDING ON");
                            state = EnumeratorState.PersistedON;
                            moveNext = true;
                        }
                        else
                        {
                            // If IdentityOn statement is needed then set it as the 
                            // current string else read the data
                            //
                            if (this.hasIdentity)
                            {
                                currentScriptString = String.Format(CultureInfo.InvariantCulture,
                                        "SET IDENTITY_INSERT {0} ON {1}",
                                        this.tableName,
                                        System.Environment.NewLine);
                                state = EnumeratorState.IdentityON;
                                moveNext = true;
                            }
                            else
                            {
                                currentScriptString = GetNextInsertStatement();
                                state = EnumeratorState.Data;
                                moveNext = true;
                            }
                        }
                    }
                    break;

                case EnumeratorState.PersistedON:

                    // At this point we dont need to read from the Reader
                    // because we will come into IdentityON state from 
                    // TruncateStatement state and in the code for TruncateStatement
                    // we are calling Reader.Read but not processing it if we transition into 
                    // IdentityON
                    //
                    if (this.hasIdentity)
                    {
                        currentScriptString = String.Format(CultureInfo.InvariantCulture,
                                "SET IDENTITY_INSERT {0} ON {1}",
                                this.tableName,
                                System.Environment.NewLine);
                        state = EnumeratorState.IdentityON;
                        moveNext = true;
                    }
                    else
                    {
                        currentScriptString = GetNextInsertStatement();
                        state = EnumeratorState.Data;
                        moveNext = true;
                    }
                    break;

                case EnumeratorState.IdentityON:

                    // At this point we dont need to read from the Reader
                    // because we will come into IdentityON state from 
                    // TruncateStatement state and in the code for TruncateStatement
                    // we are calling Reader.Read but not processing it if we transition into 
                    // IdentityON
                    //
                    currentScriptString = GetNextInsertStatement();
                    state = EnumeratorState.Data;

                    moveNext = true;
                    break;

                case EnumeratorState.Data:

                    if (this.reader.Read())
                    {
                        currentScriptString = GetNextInsertStatement();
                        state = EnumeratorState.Data;
                        moveNext = true;
                    }
                    else
                    {
                        if (this.hasIdentity)
                        {
                            currentScriptString =
                                String.Format(CultureInfo.InvariantCulture,
                                    "SET IDENTITY_INSERT {0} OFF",
                                    this.tableName
                                    );
                            state = EnumeratorState.IdentityOFF;
                            moveNext = true;
                        }
                        else
                        {
                            //setting ANSI_PADDING OFF if it was off before it was set on by us and the
                            //server supports it
                            if (this.database.GetServerObject().IsSupportedObject<UserOptions>())
                            {
                                userOptions = this.database.GetServerObject().UserOptions;
                            }

                            if (this.hasPersisted && 
                                userOptions != null && 
                                userOptions.IsSupportedProperty("AnsiPadding") && 
                                !userOptions.AnsiPadding)
                            {
                                currentScriptString =
                                    String.Format(CultureInfo.InvariantCulture,
                                        "SET ANSI_PADDING OFF");
                                state = EnumeratorState.PersistedOFF;
                                moveNext = true;
                            }
                            else
                            {
                                currentScriptString = null;
                                state = EnumeratorState.Finished;
                                moveNext = false;
                            }
                        }
                    }
                    break;

                case EnumeratorState.IdentityOFF:
                    if (this.database.GetServerObject().IsSupportedObject<UserOptions>())
                    {
                        userOptions = this.database.GetServerObject().UserOptions;
                    }
                    if (this.hasPersisted && 
                        userOptions != null &&
                        userOptions.IsSupportedProperty("AnsiPadding") 
                        && !userOptions.AnsiPadding)
                    {
                        //setting ANSI_PADDING OFF if it was off before set on by us
                        currentScriptString =
                            String.Format(CultureInfo.InvariantCulture,
                                "SET ANSI_PADDING OFF");
                        state = EnumeratorState.PersistedOFF;
                        moveNext = true;
                    }
                    else
                    {
                        currentScriptString = null;
                        state = EnumeratorState.Finished;
                        moveNext = false;
                    }
                    break;
                case EnumeratorState.PersistedOFF:
                    currentScriptString = null;
                    state = EnumeratorState.Finished;
                    moveNext = false;
                    break;
                case EnumeratorState.Finished:
                    moveNext = false;
                    break;
                default:
                    Diagnostics.TraceHelper.Assert(false, "Bug in dev code");
                    throw new Exception("Unknown state");
            }

            if (state == EnumeratorState.Finished)
            {
                this.CleanUp();
            }

            if (moveNext == null)
            {
                Diagnostics.TraceHelper.Assert(false, "MoveNext not initialized. Bug in code");
                throw new Exception("MoveNext not initialized. Bug in code");
            }
            return moveNext.Value;

        }

        /// <summary>
        /// Resets the collection so it can be reiterated.
        /// </summary>
        public void Reset()
        {
            this.CleanUp();

            this.reader = null;
            this.hasPersisted = false;
            this.hasIdentity = false;
            this.state = EnumeratorState.NotStarted;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Clean up all resources.
        /// </summary>
        public void Dispose()
        {
            this.CleanUp();
        }

        #endregion

        #region Private Methods


        /// <summary>
        /// Iterates over the columns and populates the columnNames for the columns 
        /// which are not computed and are not of type TimeStamp.
        /// Also, generates the select statement that should used to read the data
        /// for generating insert statements
        /// </summary>
        /// <param name="columnNameSQL"></param>
        /// <param name="selectSQL"></param>
        /// <param name="options"></param>
        /// <param name="table"></param>
        private void GetColumnNamesAndSelectSQL(out StringBuilder columnNameSQL, out StringBuilder selectSQL, ScriptingPreferences options, Table table)
        {

            columnNameSQL = new StringBuilder();
            selectSQL = new StringBuilder();

            bool firstColumn = true;

            foreach (Column col in table.Columns)
            {
                if (col.IsSupportedProperty("GraphType"))
                {
                    // Skip adding internal graph columns here.
                    // Note: Computed graph columns are made available
                    // to the user for querying.
                    //
                    GraphType currentColGraphType = col.GetPropValueOptional("GraphType", GraphType.None);
                    switch (currentColGraphType)
                    {
                        case GraphType.GraphFromId:
                        case GraphType.GraphFromObjId:
                        case GraphType.GraphId:
                        case GraphType.GraphToId:
                        case GraphType.GraphToObjId:
                            continue;
                    }
                }

                // make sure we can script it
                col.VersionValidate(options);

                StoreDataTypeInformation(col);

                // we need to ignore timestamp values because it gets
                // automatically populated when a row is inserted or updated
                if (col.UnderlyingSqlDataType == SqlDataType.Timestamp ||
                    col.Computed)
                {
                    //Check if it has persisted computed column so that ansi-padding setting statement is generated
                    if (this.options.IncludeScripts.AnsiPadding && col.ServerVersion.Major > 8 && col.IsPersisted)
                    {
                        this.hasPersisted = true;
                    }
                    continue;
                }

                if (options.Table.Identities && col.Identity)
                {
                    this.hasIdentity = true;
                }

                if (firstColumn == false)
                {
                    // Append the commas after existing columnName and selectSql statements
                    columnNameSQL.Append(", ");
                    selectSQL.Append(", ");
                }
                firstColumn = false;

                columnNameSQL.Append(String.Format(CultureInfo.InvariantCulture, "[{0}]", col.Name));
                selectSQL.Append(FormatValueByTypeForSelect(col));
            }
        }

        private void StoreDataTypeInformation(Column col)
        {
            //this stores information which later used while insert statement generation
            this.columnDataType.Add(col.Name, col.UnderlyingSqlDataType);

            switch (col.UnderlyingSqlDataType)
            {
                case SqlDataType.Decimal:
                case SqlDataType.Numeric:
                    this.columnNumericPrecision.Add(col.Name, col.DataType.NumericPrecision);
                    this.columnNumericScale.Add(col.Name, col.DataType.NumericScale);
                    break;
                case SqlDataType.Char:
                case SqlDataType.VarChar:
                case SqlDataType.VarCharMax:
                case SqlDataType.Text:
                    this.columnCollation.Add(col.Name, col.Collation);
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Returns the current INSERT statement string for the current row in the
        /// SQLDataReader
        /// </summary>
        /// <returns></returns>
        private String GetNextInsertStatement()
        {
            StringBuilder nextInsert = new StringBuilder();

            string formattedValue = string.Empty;

            // reset string concatenation helper
            bool firstColumn = true;

            // get a list of the all column headers from the data reader
            DataTable dt = this.reader.GetSchemaTable();
            int columnIndex = 0;

            // loop through all columns to create 'VALUES' clause for INSERT statement
            foreach (DataRow row in dt.Rows)
            {
                formattedValue = string.Empty;

                string columnName = row[0].ToString();

                // should only be empty in the case of sql_variants,
                // where the extra column to describe the underlying
                // type has no column name
                //
                if (!string.IsNullOrEmpty(columnName))
                {

                    // row[0] gives us back the column name of the SqlDataReader,
                    // which will correspond to the name in the Table


                    // should only be null in the case of sql_variants, where
                    // we use extra columns for describing the underlying type
                    //
                    if (this.reader.IsDBNull(columnIndex))
                    {
                        formattedValue = "NULL";
                    }
                    else
                    {
                        SqlDataType dataType = this.columnDataType[columnName];
                        if (dataType == SqlDataType.Timestamp)
                        {
                            columnIndex++;
                            continue;
                        }
                        else
                        {
                            formattedValue = FormatValueByType(columnName, columnIndex);
                        }
                    }

                    // concatenating values string
                    if (!string.IsNullOrEmpty(formattedValue))
                    {
                        if (firstColumn)
                        {
                            nextInsert.Append(formattedValue);
                            firstColumn = false;
                        }
                        else
                        {
                            nextInsert.Append(String.Format(CultureInfo.InvariantCulture, ", {0}", formattedValue));
                        }
                    }
                }
                columnIndex++;
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}{1})", this.insertPrefix, nextInsert.ToString());

        }


        /// <summary>
        /// Generates T-SQL script for the SELECT statement to script out table data
        /// </summary>
        /// <param name="col">Column to be scripted</param>
        /// <returns></returns>
        private string FormatValueByTypeForSelect(Column col)
        {
            string formattedValue = string.Empty;

            SqlDataType sqlDataType = col.UnderlyingSqlDataType;

            switch (sqlDataType)
            {
                case SqlDataType.DateTime:
                case SqlDataType.DateTime2:
                case SqlDataType.Time:
                case SqlDataType.DateTimeOffset:
                case SqlDataType.Date:
                case SqlDataType.SmallDateTime:
                    formattedValue = String.Format(CultureInfo.InvariantCulture, "[{0}]", col.Name);
                    break;

                case SqlDataType.Variant:
                    // NOTE: we're going to return three columns in the case of sql_variants.
                    // the first column will return the value of the column
                    // the second column will return the underlying type of the column, which we'll use
                    // later for explicit casting, so we make sure underlying types across databases are preserved
                    // the third column is the collation that is used when scripting out char,varchar, and text data
                    //
                    formattedValue = String.Format(CultureInfo.InvariantCulture,
                        "[{0}], SQL_VARIANT_PROPERTY([{0}], N'basetype'), SQL_VARIANT_PROPERTY([{0}], N'Collation')", col.Name);
                    break;

                default:
                    formattedValue = String.Format(CultureInfo.InvariantCulture, "[{0}]", col.Name);
                    break;
            }

            return formattedValue;
        }

        /// <summary>
        /// Creates the proper T-SQL syntaxt to cast the value to the proper sql_variant
        /// underlying type.  We are relegated to doing this because not all types that 
        /// are possible for an underlying sql_variant are exposed System.Data.SqlTypes.
        /// </summary>
        /// <param name="readerItem"></param>
        /// <returns></returns>
        private string FormatSqlVariantValue(int columnIndex)
        {
            string formattedValue = string.Empty;

            // get value of the sql_variant
            object variantValue = this.reader.GetProviderSpecificValue(columnIndex);

            // get underlying type
            // and collation
            string underlyingType = this.reader[columnIndex + 1].ToString().ToLowerInvariant();
            string collation = string.Empty;
            if (!this.reader.IsDBNull(columnIndex + 2))
            {
                collation = this.reader[columnIndex + 2].ToString();
            }

            switch (underlyingType)
            {
                case "bit":
                    string bit = "0";

                    if ((SqlBoolean)variantValue)
                    {
                        bit = "1";
                    }

                    formattedValue = String.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1})", bit, underlyingType);
                    break;

                case "decimal":
                case "numeric":

                    SqlDecimal d = (SqlDecimal)variantValue;

                    formattedValue = String.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1}({2},{3}))",
                        variantValue.ToString(),
                        underlyingType,
                        d.Precision,
                        d.Scale);

                    break;

                case "bigint":

                case "int":
                case "money":
                case "smalldatetime":
                case "smallint":
                case "smallmoney":
                case "tinyint":
                    formattedValue = String.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1})", variantValue.ToString(), underlyingType);
                    break;

                case "float":
                    SqlDouble vDouble = (SqlDouble)variantValue;

                    // The "R" formatting means "Round Trip", which preserves fidelity
                    formattedValue = String.Format("CAST({0} AS {1})", vDouble.Value.ToString("R", GetUsCultureInfo()), underlyingType);
                    break;

                case "real":
                    SqlSingle vSingle = (SqlSingle)variantValue;

                    // The "R" formatting means "Round Trip", which preserves fidelity
                    formattedValue = String.Format("CAST({0} AS {1})", vSingle.Value.ToString("R", GetUsCultureInfo()), underlyingType);
                    break;

                case "nchar":
                case "nvarchar":
                    // if we want a collation and we have one, we'll use it
                    if (this.options.IncludeScripts.Collation && !string.IsNullOrEmpty(collation))
                    {
                        formattedValue = String.Format(
                            CultureInfo.InvariantCulture,
                            "CAST({0} AS {1}({2})) COLLATE {3}",
                            SqlSmoObject.MakeSqlString(variantValue.ToString()),
                            underlyingType,
                            variantValue.ToString().Length,
                            collation);
                    }
                    else
                    {
                        formattedValue = String.Format(
                            CultureInfo.InvariantCulture,
                            "CAST({0} AS {1}({2}))",
                            SqlSmoObject.MakeSqlString(variantValue.ToString()),
                            underlyingType,
                            variantValue.ToString().Length);
                    }

                    break;

                case "varchar":
                case "char":
                    // If collation is not there then script data without it
                    // Else convert to the right collation. This conversion is needed both on 2000 and 2005 
                    // because otherwise the data will stored using the collation for the Database
                    //
                    string inputData;
                    if (!this.options.IncludeScripts.Collation || string.IsNullOrEmpty(collation))
                    {
                        if (string.IsNullOrEmpty(collation))
                        {
                            Debug.Assert(false, "Collation was null or empty for sql_variant data type " + underlyingType);
                        }

                        inputData = string.Format(CultureInfo.InvariantCulture,
                            "Convert(text, {0})",
                            SqlSmoObject.MakeSqlString(variantValue.ToString()));
                    }
                    else
                    {
                        inputData = string.Format(CultureInfo.InvariantCulture,
                            "Convert(text, {0} collate {1})",
                            SqlSmoObject.MakeSqlString(variantValue.ToString()),
                            collation);
                    }

                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS {1}({2}))",
                        inputData,
                        underlyingType,
                        variantValue.ToString().Length);
                    break;
                case "uniqueidentifier":
                    formattedValue = String.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1})", SqlSmoObject.MakeSqlString(variantValue.ToString()), underlyingType);
                    break;

                case "binary":
                case "varbinary":
                    formattedValue = String.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1})", ByteArrayToHexString((byte[])variantValue), underlyingType);
                    break;
                default:
                    Debug.Assert(false, "Not handling one of the types supported by sql_variant. Bug in code");
                    throw new NotSupportedException(
                        string.Format(CultureInfo.CurrentCulture,
                            "sql_variant type {0} is not supported",
                            underlyingType));
            }

            return formattedValue;
        }


        /// <summary>
        /// Generates T-SQL script for inserting values into target database
        /// </summary>
        /// <param name="columnName">Column Name of source to be scripted</param>
        /// <param name="columnIndex">Index of the column</param>
        /// <returns></returns>
        private string FormatValueByType(string columnName, int columnIndex)
        {
            string formattedValue = string.Empty;

            SqlDataType dataType = this.columnDataType[columnName]; ;

            switch (dataType)
            {

#if NETSTANDARD2_0
                // CLR UDTs are not supported on .NET Core, so we cannot output the human readable string value 
                // of the hierarchy id.  Instead, we treat the hierarchy id as a binary, the same we treat all 
                // other other CLR UDTs on .NET Core.
                case SqlDataType.HierarchyId:
#endif
                case SqlDataType.UserDefinedType:
                case SqlDataType.Geometry:
                case SqlDataType.Geography:
                    // Get the bytes and write them as hex string
                    //
                    SqlBinary sqlBinary = this.reader.GetSqlBinary(columnIndex);
                    formattedValue = ByteArrayToHexString(sqlBinary.Value);
                    break;
                case SqlDataType.BigInt:
                case SqlDataType.Int:
                case SqlDataType.SmallInt:
                case SqlDataType.TinyInt:
                    formattedValue = this.reader.GetProviderSpecificValue(columnIndex).ToString();
                    break;

                case SqlDataType.Money:
                case SqlDataType.SmallMoney:
                    formattedValue = ((SqlMoney)this.reader.GetProviderSpecificValue(columnIndex)).Value.ToString(GetUsCultureInfo());
                    break;

                case SqlDataType.Float:
                    // The "R" formatting means "Round Trip", which preserves fidelity
                    formattedValue = ((SqlDouble)this.reader.GetProviderSpecificValue(columnIndex)).Value.ToString("R", GetUsCultureInfo());
                    break;

                case SqlDataType.Real:
                    // The "R" formatting means "Round Trip", which preserves fidelity
                    formattedValue = ((SqlSingle)this.reader.GetProviderSpecificValue(columnIndex)).Value.ToString("R", GetUsCultureInfo());
                    break;

                case SqlDataType.Bit:
                    formattedValue = "0";

                    if ((SqlBoolean)this.reader.GetProviderSpecificValue(columnIndex))
                    {
                        formattedValue = "1";
                    }
                    break;

                case SqlDataType.Decimal:
                    // we have to manually format the string by ToStringing the value first, and then converting 
                    // the potential (European formatted) comma to a period.
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS Decimal({1}, {2}))",
                        String.Format(
                            GetUsCultureInfo(),
                            "{0}",
                            this.reader.GetProviderSpecificValue(columnIndex).ToString()),
                        this.columnNumericPrecision[columnName],
                        this.columnNumericScale[columnName]);
                    break;

                case SqlDataType.Numeric:
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS Numeric({1}, {2}))",
                        String.Format(
                            GetUsCultureInfo(),
                            "{0}",
                            this.reader.GetProviderSpecificValue(columnIndex).ToString()),
                        this.columnNumericPrecision[columnName],
                        this.columnNumericScale[columnName]);
                    break;

                case SqlDataType.DateTime:
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS DateTime)", SqlSmoObject.MakeSqlStringForInsert(this.reader.GetDateTime(columnIndex).ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture)));

                    break;

                case SqlDataType.SmallDateTime:
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS SmallDateTime)", SqlSmoObject.MakeSqlStringForInsert(this.reader.GetDateTime(columnIndex).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)));
                    break;

                case SqlDataType.DateTime2:
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS DateTime2)", SqlSmoObject.MakeSqlStringForInsert(this.reader.GetDateTime(columnIndex).ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)));
                    break;

                case SqlDataType.Date:
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS Date)", SqlSmoObject.MakeSqlStringForInsert(this.reader.GetDateTime(columnIndex).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    break;

                case SqlDataType.Time:
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS Time)", SqlSmoObject.MakeSqlStringForInsert(this.reader.GetTimeSpan(columnIndex).ToString()));
                    break;

                case SqlDataType.DateTimeOffset:
                    formattedValue = String.Format(
                        CultureInfo.InvariantCulture,
                        "CAST({0} AS DateTimeOffset)", SqlSmoObject.MakeSqlStringForInsert(this.reader.GetDateTimeOffset(columnIndex).ToString("o", CultureInfo.InvariantCulture)));
                    break;

                case SqlDataType.Char:
                case SqlDataType.VarChar:
                case SqlDataType.VarCharMax:
                case SqlDataType.Text:
                    // The value is generated in the following format for Sql Server 2000 since inserting 
                    // the unicode value directly when column size is more than 4000 chars does not 
                    // work for char and varchar types on 2000
                    // convert(text, N'Data' collate Collation)
                    //
                    string providerValue = this.reader.GetProviderSpecificValue(columnIndex).ToString();
                    if (this.options.TargetServerVersion == SqlServerVersion.Version80)
                    {
                        if (!this.options.IncludeScripts.Collation)
                        {
                            formattedValue = string.Format(CultureInfo.InvariantCulture,
                                "CONVERT(TEXT, {0})",
                                SqlSmoObject.MakeSqlStringForInsert(providerValue));
                        }
                        else
                        {
                            formattedValue = string.Format(CultureInfo.InvariantCulture,
                                "CONVERT(TEXT, {0} COLLATE {1})",
                                SqlSmoObject.MakeSqlStringForInsert(providerValue),
                                this.columnCollation[columnName]);
                        }
                    }
                    else
                    {
                        formattedValue = SqlSmoObject.MakeSqlStringForInsert(providerValue);
                    }
                    break;

                case SqlDataType.NVarCharMax:
                case SqlDataType.NChar:
                case SqlDataType.NVarChar:
                case SqlDataType.NText:
                case SqlDataType.SysName:
                    formattedValue = SqlSmoObject.MakeSqlStringForInsert(this.reader.GetProviderSpecificValue(columnIndex).ToString());
                    break;

                case SqlDataType.UniqueIdentifier:
#if !NETSTANDARD2_0
                // For the full .NET Framework, instantiate the CLR UDT to output the human readable string 
                // value of the hierarchy id.
                case SqlDataType.HierarchyId:
#endif
                    formattedValue = SqlSmoObject.MakeSqlString(this.reader.GetProviderSpecificValue(columnIndex).ToString());
                    break;
                case SqlDataType.Xml:
                    formattedValue = SqlSmoObject.MakeSqlString(((SqlXml)this.reader.GetProviderSpecificValue(columnIndex)).Value);
                    break;

                case SqlDataType.VarBinary:
                case SqlDataType.VarBinaryMax:
                case SqlDataType.Binary:
                case SqlDataType.Image:
                    formattedValue = ByteArrayToHexString((byte[])this.reader[columnIndex]);
                    break;

                case SqlDataType.Variant:
                    formattedValue = this.FormatSqlVariantValue(columnIndex);
                    break;

                default:
                    // We are explictly handling all types that we support. We will not attempt
                    // to support types that we don't understand.
                    // In any case, this code should never be hit because TableScriptCommand
                    // does an explict check for all types that we support and throws
                    // if it sees a type that we don't understand. If we ever hit this code
                    // then there is a bug either in TableScriptCommand where we are checking for supported
                    // types or we are not explictly handling scripting data for the said type
                    //
                    Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, "ERROR: Attempting to script data for type " + dataType);

                    throw new InvalidSmoOperationException(
                        ExceptionTemplates.DataScriptingUnsupportedDataTypeException(
                            this.tableName,
                            columnName,
                            dataType.ToString()));

            }

            return formattedValue;

        }

        /// <summary>
        /// Returns a US specific CultureInfo object
        /// </summary>
        /// <returns></returns>
        private static CultureInfo GetUsCultureInfo()
        {
            return new CultureInfo("en-US");
        }

        /// <summary>
        /// Converts byte array to hex string (used for SQL Server binary types)
        /// </summary>
        /// <param name="binValue">Input byte array</param>
        /// <returns></returns>
        private static string ByteArrayToHexString(Byte[] binValue)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("0x");

            foreach (byte byteValue in binValue)
            {
                if (byteValue < 16)
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture,
                        "0{0:X}", byteValue));
                }
                else
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture,
                        "{0:X}", byteValue));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Releases all resources
        /// </summary>
        private void CleanUp()
        {

            // release resources
            if (this.reader != null)
            {
                if (!this.reader.IsClosed)
                {
                    this.reader.Close();
                }

                this.reader = null;
            }

            if (this.conn != null)
            {
                this.conn.Close();
            }

        }

#endregion

#region Enum
        /// <summary>
        /// The enumeration for the different states of the Enumerator
        /// </summary>
        private enum EnumeratorState
        {
            NotStarted,
            PersistedON,
            IdentityON,
            Data,
            IdentityOFF,
            PersistedOFF,
            Finished
        }
#endregion

#region Private Properties
        private SqlConnection Connection
        {
            get
            {
                if (this.conn == null)
                {
                    // need to add database to the connection string
                    var connection =
                        database.ExecutionManager.ConnectionContext.GetDatabaseConnection(database.Name,
                            poolConnection: false).SqlConnectionObject;

                    if (connection.State == System.Data.ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    this.conn = connection;
                }

                return this.conn;
            }
        }
#endregion
    }
}


