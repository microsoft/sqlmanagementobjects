using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Diagnostics;
using Diagnostics = Microsoft.SqlServer.Management.Diagnostics;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
	
	internal class SqlExec : MarshalByRefObject
	{
		internal SqlExec()
		{
			m_command = null;
			m_connection = new SqlConnection();
			m_bConnectionPersisted = true;
			oldConnectionString = string.Empty;
		}
		
		internal ConnectionState ConnectionState
		{
			get
			{
				return m_connection.State;
			}
		}

		internal bool PersistConnection
		{
			get
			{
				return m_bConnectionPersisted;
			}
			set
			{
				if( m_bConnectionPersisted != value )
				{
					if( m_bConnectionPersisted == false )
					{
						// we need to update the connection
						m_connection = new SqlConnection();
						m_bConnectionPersisted = true;
					}
					else
					{
						m_bConnectionPersisted = false;
					}
				}
			}
		}

		/// <summary>
		/// Connects to the server specified in the connectionString.
		/// </summary>
		/// <param name="connectionString"></param>
		internal void Connect(string connectionString)
		{
			try
			{
				GetConnection(connectionString);
				ReleaseConnection();
			}
			catch(SqlException e)
			{
				throw new SmoException(ExceptionTemplates.SqlInnerException, e);
			}
		}

		/// <summary>
		/// Disconnects from the server, if we have a connection. If not
		/// it does not return any error
		/// </summary>
		internal void Disconnect()
		{
			m_connection.Close();
		}

		// returns an opened connection, either the persisted connection, or a new one
		internal SqlConnection GetConnection(string connectionString)
		{
			if( m_bConnectionPersisted )
			{
				// do we have the same connection string ?
				if( connectionString != oldConnectionString )
				{
					// if not, we have to create a new connection
					m_connection.Close();
					
					// first close the old connection
					m_connection.Close();
					// watch out, we go with connections that are not drawn from the pool
					m_connection.ConnectionString = connectionString + ";pooling=false";
					
					oldConnectionString = connectionString;
#if DEBUG
				Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, 
							"Getting a new connection with connection string =\"" + m_connection.ConnectionString + "\"");
#endif
				}

				// open the connection if necessary
				if( m_connection.State != ConnectionState.Open )
				{
					m_connection.Open();
				}
			}
			else
			{
				m_connection = new SqlConnection(connectionString);
				m_connection.Open();
#if DEBUG
				Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, 
							"Getting a new connection with connection string =\"" + m_connection.ConnectionString + "\"");
#endif
			}

			return m_connection;
		}

		// for unpersisted connections, we call Close() on them, releasing them to the pool
		void ReleaseConnection()
		{
			if( !m_bConnectionPersisted )
			{
				m_connection.Close();
			}
		}

		private SqlCommand m_command;
		private SqlConnection m_connection;
		private bool m_bConnectionPersisted;
		private string oldConnectionString;

		// aborts any executing command. Please note that it does not return a success status
		// since it is based on IDbCommand.Cancel() that has the same behavior
		internal void Abort()
		{
			if( m_command != null )
			{
				m_command.Cancel();
				if( !m_bConnectionPersisted )
				{
					// we also have to close the connection, since the 
					// meaning of Abort is to abort the execution of all the commands
					m_connection.Close();
				}
			}
		}

		/// <summary>
		/// Executes SQL commands, and has different types of return values
		/// </summary>
		/// <param name="ret">Type of the return value</param>
		/// <param name="sqlcommand"></param>
		/// <param name="connString"></param>
		/// <param name="msgHandler"></param>
		/// <param name="stateChangeHandler"></param>
		/// <returns></returns>
 		internal object ExecuteSql(SqlExecuteReturn ret, StringCollection sqlcommand, string connString, 
 									SqlInfoMessageEventHandler msgHandler, StateChangeEventHandler stateChangeHandler)
		{
			// allocate return object
			object returnObject = null;
			switch(ret )
			{
				case SqlExecuteReturn.DataSet:
					returnObject = new DataSet[sqlcommand.Count]; 
					((DataSet)returnObject).Locale = CultureInfo.InvariantCulture;
					break;
				case SqlExecuteReturn.DataReader:
					returnObject = new SqlDataReader[sqlcommand.Count]; 
					break;
				case SqlExecuteReturn.Scalar:
					returnObject = new object[sqlcommand.Count]; 
					break;
				case SqlExecuteReturn.NonQuery:
					returnObject = new int[sqlcommand.Count]; 
					break;
			}

			// if input is an empty collection, we return an empty array of DataSets			
			if(sqlcommand.Count==0) 
			{
				return returnObject;
			}

			// data adapter used ONLY by ret = SqlExecuteReturn.DataSet
			// we create it here for the sake of code simplicity 
			SqlDataAdapter sqlSrvDataSetCmd = new SqlDataAdapter();

#if DEBUG			
			DateTime now = DateTime.Now;
#endif
			SqlConnection connection = null;
			try{
				// obtain the connection, either the persisted of a new one
				connection = GetConnection(connString);

				// attach the information message handler
				if( null != msgHandler )
				{
					connection.InfoMessage += msgHandler;
				}

				if( null != stateChangeHandler )
				{
					connection.StateChange += stateChangeHandler;
				}
				
				m_command = new SqlCommand();
				m_command.Connection = connection;
				// set it default to this value, temporary untill we implement the new ServerConnection 
				m_command.CommandTimeout = 5000;
				int cmdno = 0;

#if DEBUG
				now = DateTime.Now;
#endif
				
				// execute all commands in the input collection, and collect the results
				foreach(string cmd in sqlcommand)
				{
					m_command.CommandText = cmd;
					sqlSrvDataSetCmd.SelectCommand = m_command;

					switch(ret )
					{
						case SqlExecuteReturn.DataSet:
							DataSet dataSet = new DataSet();
							dataSet.Locale = CultureInfo.InvariantCulture;
							sqlSrvDataSetCmd.Fill(dataSet);
							((DataSet[])returnObject)[cmdno++] = dataSet;
							break;
						case SqlExecuteReturn.DataReader:
							((SqlDataReader[])returnObject)[cmdno++] = m_command.ExecuteReader();
							break;
						case SqlExecuteReturn.Scalar:
							((object[])returnObject)[cmdno++] = m_command.ExecuteScalar();
							break;
						case SqlExecuteReturn.NonQuery:
							((int[])returnObject)[cmdno++] = m_command.ExecuteNonQuery();
							break;
					}
				}
			}
			catch(SqlException e)
			{
				// do the cleanup - close the connection
				if( null != connection )
					connection.Close();
				
				// wrap up SqlException into a SmoException and rethrow it
				throw new SmoException(ExceptionTemplates.SqlInnerException + e.Message, e);
			}
			finally
			{
#if DEBUG	
				if( PerformanceCounters.DoCount )
					PerformanceCounters.SqlExecutionDuration = PerformanceCounters.SqlExecutionDuration + (DateTime.Now - now);
#endif
				// clean m_command
				m_command = null;

				// remove the event handlers on connection
				if( null != msgHandler )
				{
					connection.InfoMessage -= msgHandler;
				}
				
				if( null != stateChangeHandler )
				{
					connection.StateChange -= stateChangeHandler;
				}

				// if the connection is not persisted, we need to return it to the pool
				ReleaseConnection();
			}

			return returnObject;
		}

 		internal DataSet[] ExecuteDataSet(StringCollection sqlcommand, string connString, 
 										SqlInfoMessageEventHandler msgHandler, 
 										StateChangeEventHandler stateChangeHandler)
		{
			return (DataSet[])ExecuteSql(SqlExecuteReturn.DataSet, sqlcommand, connString, 
										msgHandler, stateChangeHandler);
		}

		internal Int32[] ExecuteNonQuery(StringCollection sqlcommand, string connString, 
										SqlInfoMessageEventHandler msgHandler, 
										StateChangeEventHandler stateChangeHandler)
		{
			return (Int32[])ExecuteSql(SqlExecuteReturn.NonQuery, sqlcommand, connString, 
										msgHandler, stateChangeHandler);
		}

		internal object[] ExecuteScalar(StringCollection sqlcommand, string connString, 
										SqlInfoMessageEventHandler msgHandler, 
										StateChangeEventHandler stateChangeHandler)
		{
			return (object[])ExecuteSql(SqlExecuteReturn.Scalar, sqlcommand, connString, 
										msgHandler, stateChangeHandler);
		}

		internal SqlDataReader ExecuteDataReader(StringCollection sqlcommand, string connString, 
										SqlInfoMessageEventHandler msgHandler, 
										StateChangeEventHandler stateChangeHandler)
		{
			if( 0 == sqlcommand.Count ) 
			{
				return null;
			}
			
			//here we aggregate all the commands in one
			// we do this because we cannot return more than one valid reader
			StringBuilder sbSqlCmd = new StringBuilder(Globals.INIT_BUFFER_SIZE);
			foreach(string s in sqlcommand) 
			{ 
				sbSqlCmd.Append(s); 
				sbSqlCmd.Append("\r\n"); 
			}

			StringCollection cmds = new StringCollection();
			cmds.Add(sbSqlCmd.ToString());

			return ((SqlDataReader[])ExecuteSql(SqlExecuteReturn.DataReader, cmds, connString, 
										msgHandler, stateChangeHandler))[0];
			
		}

	}

	enum SqlExecuteReturn
	{
		DataSet, DataReader, NonQuery, Scalar
	}

}

