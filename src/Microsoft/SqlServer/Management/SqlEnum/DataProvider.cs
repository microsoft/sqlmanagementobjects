// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Data;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif
    using Microsoft.SqlServer.Management.Smo.SqlEnum;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;

    ///<summary>
    ///exeposes the results as a tsql as a DataTable or a data reader
    ///executes necessary postprocesses and type conversions</summary>
    internal class DataProvider : IDataReader
    {
        ///<summary>	
        ///describes how the data will be retrived</summary>
        public enum RetriveMode 
        { 
            ///<summary>		
            ///as a DataReadet</summary>
            RetriveDataReader, 
            ///<summary>			
            ///as a DataTable</summary>
            RetriveDataTable 
        };

        ExecuteSql m_execSql;
        DataTable m_table;
        SqlDataReader m_dataReader;
        DataTable m_schemaTable;
        int nonTriggeredPropertiesCount;
        Object [] rowData;
        ColumnDataManipulation [] rowDataManipulation;
        bool m_bHasTypeCast;
        bool m_bHasPostProcess;
        RetriveMode m_RetriveMode;
        int m_nCurentCachePos;
        SqlCommand m_command;

        struct ColumnDataManipulation
        {
            public string name;
            public Type targetType;
            public Type sourceType;
            public PostProcess postProcess;
        }

        ///<summary>
        ///initialize with SatatementBuilder, default retrive mode is DataReader</summary>
        public DataProvider(StatementBuilder sb)
        {
            Init(sb, RetriveMode.RetriveDataReader);
        }

        ///<summary>
        ///initialize with SatatementBuilder, and retrive mode</summary>
        public DataProvider(StatementBuilder sb, RetriveMode rm)
        {
            Init(sb, rm);
        }

        ///<summary>
        ///initialize with StatementBuilder, and retrive mode
        /// </summary>
        private void Init(StatementBuilder sb, RetriveMode rm)
        {
            m_RetriveMode = rm;
            m_dataReader = null;
            m_schemaTable = null;
            m_command = null;

            nonTriggeredPropertiesCount = sb.NonTriggeredProperties;

            if (RetriveMode.RetriveDataTable == m_RetriveMode)
            {
                rowData = new Object[sb.ParentProperties.Count];
            }
            else
            {
                rowData = new Object[nonTriggeredPropertiesCount];
            }

            InitRowDataManipulation(sb.ParentProperties, sb.PostProcessList);
            InitSchemaTable(sb.ParentProperties);
        }

        ///<summary>
        ///the means to execute the query ( execSql ) and the query ( query )
        ///it executes the query and gets a data reader
        ///if retrive mode is DataTable it proceeds to fill it</summary>
        public void SetConnectionAndQuery(ExecuteSql execSql, string query)
        {
            m_execSql = execSql;
            m_dataReader = m_execSql.GetDataReader(query, out m_command);
            m_schemaTable = null;

            //get all the rows right now
            if ( RetriveMode.RetriveDataTable == m_RetriveMode )
            {
                try
                {
                    while( ReadInternal() )
                    {
                        ManipulateRowDataType();
                        DataRow row = m_table.NewRow();
                        for(int i = 0; i < rowData.Length; i++)
                        {
                            if( null == rowData[i] )
                            {
                                row[i] = System.DBNull.Value;
                            }
                            else
                            {
                                row[i] = rowData[i];
                            }
                        }
                        m_table.Rows.Add(row);
                    }

                    // Nothing to cancel when we drop into the finally block since we finished without error
                    m_command.Dispose();
                    m_command = null;
                }
                finally
                {
                    rowData = null;
                    if (m_command != null)
                    {
                        m_command.Cancel();
                        m_command.Dispose();
                        m_command = null;
                    }
                    m_dataReader.Dispose();
                    m_dataReader = null;
                    m_schemaTable = null;
                    m_execSql.Disconnect();
                    m_execSql = null;
                }
            }
        }

#region InitDataStructures

        ///<summary>
        ///init data structures so that row manipulation is done eficiently</summary>
        public void InitRowDataManipulation(ArrayList parentProperties, SortedList postProcessList)
        {
            rowDataManipulation = new ColumnDataManipulation[nonTriggeredPropertiesCount];
            m_bHasTypeCast = false;
            m_bHasPostProcess = null != postProcessList && postProcessList.Count > 0;

            //make temporary triggered column to ordinal list
            SortedList triggeredColumnsLookup = null;
            if( m_bHasPostProcess )
            {
                triggeredColumnsLookup = new SortedList(System.StringComparer.Ordinal);
                for(int j = nonTriggeredPropertiesCount; j < parentProperties.Count; j++)
                {
                    triggeredColumnsLookup[((SqlObjectProperty)parentProperties[j]).Alias] = j;
                }
            }

            int i = 0;
            foreach(SqlObjectProperty p in parentProperties)
            {
                //handle type casts
                if( Util.DbTypeToClrType(p.DBType) != p.Type )
                {
                    Type targetType;
                    try
                    {
                        targetType = Type.GetType(p.Type);
                    }
                    catch (Exception e)
                    {
                        //Type.GetType throws exceptions or returns null for the types that it cannnot find in the calling assembly.
                        //This will handle Exception case. Eg: Microsoft.SqlServer.Management.Smo.ServiceStartMode is defined in Microsoft.SqlServer.Smo.dll
                        //while Microsoft.SqlServer.Management.Smo.MirroringRole is defined in the current assembly(Microsoft.SqlServer.SqlEnum.dll).
                        //GetType of MirroringRole will succeed while the ServiceStartMode will throw exception. For details see the Type.GetType msdn info.
                        //Hence call the GetType a second time with the SMO assembly name - since we know that rest of the types are in SMO assembly. 
                        //If second call fails - for anyreason, just throw the original exception.
                        try
                        {
                            string typename = string.Format("{0}, {1}", p.Type, "Microsoft.SqlServer.Smo");
                            targetType = Type.GetType(typename);
                        }
                        catch (Exception)
                        {
                            throw e;
                        }
                    }
                    rowDataManipulation[i].targetType = targetType;
                    rowDataManipulation[i].sourceType = Type.GetType(Util.DbTypeToClrType(p.DBType));
                    m_bHasTypeCast = true;
                }
                //handle post process
                if( m_bHasPostProcess )
                {
                    PostProcess pp = (PostProcess)postProcessList[p.Alias];
                    if( null != pp )
                    {
                        rowDataManipulation[i].name = p.Name;
                        rowDataManipulation[i].postProcess = pp;

                        //fix the lookup table for triggered columns from
                        //name( actual name of the property)->alias( coresponding column name in the result set ) relationship
                        //to name->ordinal
                        pp.UpdateFromNameBasedToOrdinalLookup(triggeredColumnsLookup);						
                    }
                }
                if( ++i >= nonTriggeredPropertiesCount )
                {
                    break;
                }
            }
        }

        ///<summary>
        /// Creates a empty DataTable that reflects the schema of the retrieved data.
        /// </summary>
        public void InitSchemaTable(ArrayList parentProperties)
        {
            m_table = new DataTable();
            m_table.Locale = CultureInfo.InvariantCulture;

            int i = 0;
            foreach(SqlObjectProperty p in parentProperties)
            {
                string sType = p.Type;

                // BUG: VSTS 69514

                // An Extended type is a type that uses the attribute "report_type" in the SMO xml 
                // All of the types that use "report_type" currently are base type int or tinyint in the server EXCEPT for SqlSecureString in LinkedServer
                // LinkedServer uses SqlSecureString, so we need to make an exception for that type.
                if( p.ExtendedType )
                {
                    if (p.Type.Equals("Microsoft.SqlServer.Management.Smo.Internal.SqlSecureString"))
                    {
                        sType = "System.String";
                    }
                    else  // the reference type is the other 99.9%
                    {
                        sType = "System.Int32";
                    }
                }


                if (string.Compare(sType, "Microsoft.SqlServer.Management.Common.DatabaseEngineType", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    m_table.Columns.Add(new DataColumn(p.Alias, typeof(Microsoft.SqlServer.Management.Common.DatabaseEngineType)));
                }
                else
                {
                    m_table.Columns.Add(new DataColumn(p.Alias, Type.GetType(sType, true)));
                }

                if( RetriveMode.RetriveDataReader == m_RetriveMode && ++i >= nonTriggeredPropertiesCount )
                {
                    break;
                }
            }
        }
#endregion

#region DataManipulation

        ///<summary>
        ///get the value for the ordinal column i
        ///the column is triggered rather than requested by user</summary>
        internal object GetTrigeredValue(int i)
        {
            if( RetriveMode.RetriveDataTable == m_RetriveMode )
            {
                return m_table.Rows[m_nCurentCachePos][i];
            }
            else //DataReader
            {
                return m_dataReader.GetValue(i);
            }
        }

        ///<summary>
        ///get the value for ordinal column i
        ///the column was directly requested by the user</summary>
        internal object GetDataFromStorage(int i)
        {
            if( RetriveMode.RetriveDataTable == m_RetriveMode )
            {
                return m_table.Rows[m_nCurentCachePos][i];
            }
            else
            {
                return rowData[i];
            }
        }

        ///<summary>
        ///update the data at ordinal i</summary>
        internal void SetDataInStorage(int i, object data)
        {
            if( RetriveMode.RetriveDataTable == m_RetriveMode )
            {
                m_table.Rows[m_nCurentCachePos][i] = data;
            }
            else
            {
                rowData[i] = data;
            }
        }

        ///<summary>
        ///advance one row</summary>
        private bool ReadInternal()
        {
            //else use DataReader
            bool b = m_dataReader.Read();
            if( b )
            {
                try
                {
                    m_dataReader.GetValues(rowData);
                }
                catch (OverflowException)
                {
                    for (int i = 0; i < rowData.Length; i++)
                    {
                        try
                        {
                            rowData[i] = m_dataReader.GetValue(i);
                        }
                        catch(OverflowException)
                        {
                            //GetSqlValue returns the data value in the specified column as a SQL Server type. 
                            //This is needed if the values in the database that cannot be represented with CLR builtin types.
                            //example:-  .NET Decimal type cannot handle a value as large as the SQL Server Decimal type.
                            rowData[i] = m_dataReader.GetSqlValue(i);
                        }
                    }
                }
            }
            return b;
        }

        ///<summary>
        ///executes post process for this row</summary>
        private void ManipulateRowDataPostProcess()
        {
            if( !m_bHasPostProcess )
            {
                return;
            }

            for(int i = 0; i < nonTriggeredPropertiesCount; i++)
            {
                //check post process manipulation
                if( null != rowDataManipulation[i].postProcess )
                {
                    object o = rowDataManipulation[i].postProcess.GetColumnData
                        (rowDataManipulation[i].name, GetDataFromStorage(i), this/* for triggered data access*/);
                    SetDataInStorage(i, o);
                }
            }

            //row done: cleanup post process for any row specific data that it may have cached
            for(int i = 0; i < nonTriggeredPropertiesCount; i++)
            {
                if( null != rowDataManipulation[i].postProcess )
                {
                    rowDataManipulation[i].postProcess.CleanRowData();
                }
            }
        }

        ///<summary>
        ///execute type conversions for this row</summary>
        private void ManipulateRowDataType()
        {
            if( !m_bHasTypeCast )
            {
                return;
            }
            for(int i = 0; i < nonTriggeredPropertiesCount; i++)
            {
                //check type manipulation
                if( null != rowDataManipulation[i].targetType )
                {
                    //if data is DBNull then do nothing
                    if( rowData[i] is DBNull )
                    {
                        continue;
                    }
                    //2. if target is enum
                    if( rowDataManipulation[i].targetType.GetIsEnum() )
                    {
                        rowData[i] = Enum.ToObject(rowDataManipulation[i].targetType, rowData[i]);
                        continue;
                    }
                    //3. try the IConvertible interface
                    IConvertible intfConv = rowData[i] as IConvertible;
                    //we only use IConvertible to convert to primitive types
                    if( null != intfConv && rowDataManipulation[i].targetType.GetIsPrimitive() )
                    {
                        rowData[i] = intfConv.ToType(rowDataManipulation[i].targetType, CultureInfo.CurrentCulture);
                        //we're done with this property
                        continue;
                    }

                    //4. use contructor to convert from result set type to desired type
                    Type[] types = new Type[] {rowDataManipulation[i].sourceType };
                    ConstructorInfo constructorInfoObj = rowDataManipulation[i].targetType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public, null,
                        CallingConventions.HasThis, types, null);
                    if( null != constructorInfoObj )
                    {
                        rowData[i] = constructorInfoObj.Invoke(new Object[] { rowData[i] } );
                        continue;
                    }
                    //5. if the source is a string try to use the Parse method.
                    if (typeof(System.String) == rowDataManipulation[i].sourceType)
                    {
                        //if it has a default constructer we can proceed
                        constructorInfoObj = rowDataManipulation[i].targetType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[] {}, null);
                        if (null != constructorInfoObj)
                        {
                            Object o = constructorInfoObj.Invoke(new Object[] { });

                            if (null != o)
                            {
                                MethodInfo methodInfoObj = rowDataManipulation[i].targetType.GetMethod("Parse", new Type[] { typeof(System.String) });

                                if (null != methodInfoObj)
                                {
                                    methodInfoObj.Invoke(o, new Object[] { rowData[i] });
                                    rowData[i] = o;
                                }
                            }
                        }
                    }
                }
            }
        }

        ///<summary>
        ///manipulates row data: post process + type conversions</summary>
        private void ManipulateRowData()
        {
            ManipulateRowDataType();
            ManipulateRowDataPostProcess();
        }
#endregion

        //IDataReader
#region IDataReader

        ///<summary>
        ///always 1
        /// </summary>
        public int Depth 
        {
            get 
            { 
                return m_dataReader.Depth; 
            }
        }

        ///<summary>
        ///is DataReader closed
        /// </summary>
        public bool IsClosed 
        {
            get 
            { 
                return m_dataReader.IsClosed; 
            }
        }

        ///<summary>
        /// Returns the number of records affected. Will always be -1.
        /// </summary>
        public int RecordsAffected 
        {
            get 
            {
                // we do not want to expose here the internal workings of DataProvider
                // so we return -1, which indicates that no records have been inserted
                // or deleted
                return -1;
            }
        }

        /// <summary>
        /// clear internal data, cancel any data reader pipe, close reader, disconnect </summary>
        public void Close()
        {
            m_table = null;

            if( null != m_dataReader && !m_dataReader.IsClosed )
            {
                if (null != m_command)
                {
                    // Try to cancel the outstanding data reader pipe via the SQL command before closing
                    // to prevent long-running queries from continuing to pump data form the server even after the reader.Close().
                    m_command.Cancel();
                }
                m_dataReader.Dispose();
                m_schemaTable = null;

            }
            if( null != m_execSql )
            {
                m_execSql.Disconnect();
                m_execSql = null;
            }
        }

        ///<summary>
        ///get empty DataTable describing the schema
        /// </summary>
        public DataTable GetSchemaTable()
        {
            // get the table from the reader
            if (m_schemaTable == null)
            {
                m_schemaTable = m_dataReader.GetSchemaTable();

                // remove the last columns if they are hidden fields
                while (nonTriggeredPropertiesCount >= 1 &&
                    m_schemaTable.Rows.Count > nonTriggeredPropertiesCount)
                {
                    m_schemaTable.Rows.RemoveAt(m_schemaTable.Rows.Count - 1);
                }
            }
            return m_schemaTable;
        }

        ///<summary>
        ///nop, always returns false</summary>
        public bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// advances to next row, closes in case of failure</summary>
        public bool Read()
        {
            bool b = ReadInternal();
            if( b )
            {
                ManipulateRowData();
            }
            return b;
        }
#endregion

        ///<summary>
        ///get the DataTable filled with the result data</summary>
        internal DataTable GetTable()
        {
            int nCount = m_table.Rows.Count;
            for(m_nCurentCachePos = 0; m_nCurentCachePos < nCount; m_nCurentCachePos++)
            {
                ManipulateRowDataPostProcess();
            }
            int nCols = m_table.Columns.Count;
            for(int i = nCols - 1; i >= nonTriggeredPropertiesCount; i--)
            {
                m_table.Columns.RemoveAt(i);
            }
            return m_table;
        }

        /// <summary>
        /// Returns the number of rows in the table.
        /// </summary>
        internal int TableRowCount
        {
            get
            {
                return m_table.Rows.Count;
            }
        }

#region IDisposable
        ///<summary>
        ///dispose the object</summary>
        public void Dispose()
        {
            if( m_table != null ) 
            {
                m_table.Dispose();
            }
            if (m_command != null)
            {
                m_command.Dispose();
            }
            if (m_dataReader != null)
            {
                m_dataReader.Dispose();
            }

            m_schemaTable = null;
        }
#endregion

#region IDataRecord
        ///<summary>		
        ///number of columns</summary>
        public int FieldCount 
        {
            get { return nonTriggeredPropertiesCount; }
        }

        ///<summary>
        ///makes shure any access outside the user requested properties
        ///results in an out of range exception
        ///triggered properties should not be available</summary>
        private int AdjustIndex(int i)
        {
            if( nonTriggeredPropertiesCount > 0 && i > nonTriggeredPropertiesCount)
            {
                i = m_dataReader.FieldCount + 1;//ensure out of range
            }
            return i;
        }

        ///<summary>
        ///int indexer</summary>
        public object this[int idx]
        {
            get 
            { 
                return rowData[idx];
            }
        }

        ///<summary>
        ///string indexer</summary>
        public object this[string name]
        {
            get	{ return this[m_dataReader.GetOrdinal(name)]; }
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public bool GetBoolean(int i)
        {
            return (bool)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public byte GetByte(int i)
        {
            return (byte)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public long GetBytes(int i,long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            byte [] data = (byte [])this[i];
            int size = length <= data.Length ? length : data.Length;
            Array.Copy(buffer, bufferoffset, data, fieldOffset, size);
            return size;
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public char GetChar(int i)
        {
            return (char)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            char [] data = (char [])this[i];
            int size = length <= data.Length ? length : data.Length;
            Array.Copy(buffer, bufferoffset, data, (int)fieldoffset, size);
            return size;
        }

        ///<summary>
        ///not supported, always null</summary>
        public IDataReader GetData(int i)
        {
            return null;//not supported
        }

        ///<summary>
        ///get type name for column at ordinal i</summary>
        public string GetDataTypeName(int i)
        {
            i = AdjustIndex(i);
            return m_dataReader.GetDataTypeName(i);
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public DateTime GetDateTime(int i)
        {
            return (DateTime)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public decimal GetDecimal(int i)
        {
            return (decimal)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public double GetDouble(int i)
        {
            return (double)this[i];
        }

        ///<summary>
        ///get type for column at ordinal i</summary>
        public Type GetFieldType(int i)
        {
            i = AdjustIndex(i);
            return GetSchemaTable().Columns[i].DataType;
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public float GetFloat(int i)
        {
            return (float)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public Guid GetGuid(int i)
        {
            return (Guid)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public short GetInt16(int i)
        {
            return (short)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public int GetInt32(int i)
        {
            return (Int32)this[i];
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public long GetInt64(int i)
        {
            return (Int64)this[i];
        }

        ///<summary>
        ///get column name for ordinal i</summary>
        public string GetName(int i)
        {
            i = AdjustIndex(i);
            return m_dataReader.GetName(i);
        }

        ///<summary>
        ///get ordinal for column name</summary>
        public int GetOrdinal(string name)
        {
            return AdjustIndex(m_dataReader.GetOrdinal(name));
        }

        ///<summary>
        ///get data for column i as the given type</summary>
        public string GetString(int i)
        {
            return (string)this[i];
        }

        ///<summary>
        ///get data for column i as Object</summary>
        public object GetValue(int i)
        {
            return this[i];
        }

        ///<summary>
        ///get data for the row as an array of Object</summary>
        public int GetValues(object[] values)
        {
            int nColsRequested = values.Length;
            int nColsReturnedFinal = nonTriggeredPropertiesCount < nColsRequested ? nonTriggeredPropertiesCount : nColsRequested;
            for(int i = 0; i < nColsReturnedFinal; i++)
            {
                values[i] = this[i];
            }
            for(int i = nColsReturnedFinal; i < nonTriggeredPropertiesCount; i++)
            {
                values[i] = null;
            }
            return nColsReturnedFinal;
        }

        ///<summary>
        ///true if the data for ordinal i is null</summary>
        public bool IsDBNull(int i)
        {
            i = AdjustIndex(i);
            return m_dataReader.IsDBNull(i);
        }

#endregion
    }
}
