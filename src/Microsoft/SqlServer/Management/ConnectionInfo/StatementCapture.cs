// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System.Collections.Specialized;

    /// <summary>
    /// 
    /// </summary>
    public sealed class CapturedSql
    {
        private StringCollection m_CapturedSql;

        /// <summary>
        /// constructor
        /// </summary>
        internal CapturedSql()
        {
            m_CapturedSql = new StringCollection();
        }

        /// <summary>
        /// Returns a copy of the string collection that contains the captured SQL statements.
        /// The buffer has to be explicitly cleared with Clear.
        /// NOTE: According to Dima, no memory will be copied over; will be handled by URT
        /// and therefore there is not added overhead.
        /// </summary>
        public StringCollection Text
        {
            get
            {
                StringCollection col = new StringCollection();
                foreach(string s in this.m_CapturedSql)
                {
                    col.Add(s);
                }
                return col;
            }
        }

        /// <summary>
        /// Adds the string to the capture buffer.
        /// </summary>
        public void Add(string sqlStatement)
        {
            this.m_CapturedSql.Add(sqlStatement);
        }

        /// <summary>
        /// Clears the capture buffer.
        /// </summary>
        public void Clear()
        {
            this.m_CapturedSql.Clear();
        }
    }
}