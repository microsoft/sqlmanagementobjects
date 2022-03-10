// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Xml;

    /// <summary>
    /// container for the result received from the enumerator
    /// </summary>
    [ComVisible(false)]
    public class EnumResult
    {
        ResultType m_resultType;
        Object m_data;

        /// <summary>
        /// in what kind of storage is the data in the result
        /// </summary>
        /// <value></value>
        public ResultType Type
        {
            get { return m_resultType; }
        }

        /// <summary>
        /// used to set the result type on the data is filled
        /// </summary>
        /// <param name="type"></param>
        protected void SetType(ResultType type)
        {
            m_resultType = type;
        }

        /// <summary>
        /// result data 
        /// </summary>
        /// <value></value>
        public Object Data
        {
            get	{ return m_data; }
            set	{ m_data = value; }
        }

        /// <summary>
        /// The text of the command that was used to generate the result. Can be null.
        /// </summary>
        public string CommandText { get; set; }

        /// <summary>
        /// The time spent executing the command. Not valid if CommandText is empty.
        /// </summary>
        public TimeSpan CommandElapsedTime { get; set; }

        /// <summary>
        /// initialize an EnumResult
        /// </summary>
        /// <param name="ob">data</param>
        /// <param name="resultType">type of data</param>
        public EnumResult(Object ob, ResultType resultType)
        {
            m_data = ob;
            m_resultType = resultType;
        }

        /// <summary>
        /// default constructor
        /// </summary>
        public EnumResult()
        {
        }

        /// <summary>
        /// implicit cast to DataSet if possible
        /// </summary>
        /// <param name="er"></param>
        /// <returns></returns>
        public static implicit operator DataSet(EnumResult er)
        {
            if( er.m_resultType == ResultType.DataSet )
            {
                return (DataSet)er.m_data;
            }
            else if( er.m_resultType == ResultType.DataTable )
            {
                DataSet ds = new DataSet();
                ds.Locale = CultureInfo.InvariantCulture;
                ds.Tables.Add((DataTable)er.m_data);
                return ds;
            }
            throw new ResultTypeNotSupportedEnumeratorException(ResultType.DataSet);
        }

        /// <summary>
        /// convert to DataSet if possible
        /// </summary>
        /// <param name="er"></param>
        /// <returns></returns>
        public static DataSet ConvertToDataSet(EnumResult er)
        {
            return (DataSet)er;
        }

        /// <summary>
        /// implicit cast to DataSet if possible
        /// </summary>
        /// <param name="er"></param>
        /// <returns></returns>
        public static implicit operator DataTable(EnumResult er)
        {
            if( er.m_resultType == ResultType.DataTable )
            {
                return (DataTable)er.m_data;
            }
            else if( er.m_resultType == ResultType.DataSet )
            {
                if( null != ((DataSet)er.Data).Tables[0] )
                {
                    return ((DataSet)er.Data).Tables[0];
                }
                return null;
            }
            throw new ResultTypeNotSupportedEnumeratorException(ResultType.DataTable);
        }

        /// <summary>
        /// implicit cast to DataTable if possible
        /// </summary>
        /// <param name="er"></param>
        /// <returns></returns>
        public static DataTable ConvertToDataTable(EnumResult er)
        {
            return (DataTable)er;
        }

        /// <summary>
        /// implicit cast to XmlDocument if possible
        /// </summary>
        /// <param name="er"></param>
        /// <returns></returns>
        public static implicit operator XmlDocument(EnumResult er)
        {
            if( er.m_resultType != ResultType.XmlDocument )
            {
                throw new ResultTypeNotSupportedEnumeratorException(ResultType.XmlDocument);
            }

            return (XmlDocument)er.m_data;
        }

        /// <summary>
        /// convert to XmlDocument if possible
        /// </summary>
        /// <param name="er"></param>
        /// <returns></returns>
        public static XmlDocument ConvertToXmlDocument(EnumResult er)
        {
            return (XmlDocument)er;
        }

        /// <summary>
        /// Converts the enumeration result to IDataReader
        /// </summary>
        /// <param name="er"></param>
        /// <returns></returns>
        public static IDataReader ConvertToDataReader(EnumResult er)
        {
            switch (er.m_resultType)
            {
                case ResultType.DataTable:
                    return ((DataTable)er.Data).CreateDataReader();
                case ResultType.IDataReader:
                    return (IDataReader)er.Data;
                default:
                    throw new ResultTypeNotSupportedEnumeratorException(ResultType.IDataReader);
            }
        }
    }
}
            
