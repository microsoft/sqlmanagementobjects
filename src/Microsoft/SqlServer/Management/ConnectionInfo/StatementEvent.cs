// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif

    /// <summary>
    /// Event Handler for Statements
    /// </summary>
    public delegate void StatementEventHandler(object sender, StatementEventArgs e);

    /// <summary>
    /// This class contains the details of an executed Sql statement.
    /// </summary>
    public class StatementEventArgs : EventArgs 
    {
        string m_sqlStatement;
        DateTime m_timeStamp;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="sqlStatement">statement executes</param>
        /// <param name="timeStamp">Execution time</param>
        public StatementEventArgs(string sqlStatement, DateTime timeStamp)
        {
            m_sqlStatement = sqlStatement;
            m_timeStamp = timeStamp;
        }

        /// <summary>
        /// statement executed
        /// </summary>
        public string SqlStatement
        {
            get
            {
                return m_sqlStatement;
            }
        }

        /// <summary>
        /// execution time
        /// </summary>
        public DateTime TimeStamp
        {
            get
            {
                return m_timeStamp;
            }
        }

        /// <summary>
        /// string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.SqlStatement;
        }
    }

    /// <summary>
    /// the prototype of the callback method
    /// </summary>
    public delegate void ServerMessageEventHandler(object sender, ServerMessageEventArgs e);

    /// <summary>
    /// Arguments for the event handler 
    /// </summary>
    public class ServerMessageEventArgs : EventArgs
    {
        private SqlError error;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlError"></param>
        public ServerMessageEventArgs(SqlError sqlError)
        {
            this.error = sqlError;
        }

        /// <summary>
        /// 
        /// </summary>
        public SqlError Error 
        { 
            get 
            { 
                return error;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return error.ToString();
        }
    }
}