// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Text;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif

    public sealed class SfcTSqlExecutor
    {
        private SfcTSqlExecutor() { } // Static Holders should not have constructors

        //DC needs this visible to be able to correctly set the mode of execution, modified to public
        public enum ExecutionMode
        {
            Scalar,
            NonQuery,
            WithResults
        }


        internal static object Execute(Common.ServerConnection connection, string script, ExecutionMode mode)
        {
            return Execute(connection, script, mode, Common.ExecutionTypes.NoCommands);
        }

        //IS needs this to run multiple batches
        //Modified to internal for DC support
        internal static object Execute(Common.ServerConnection connection, string script, ExecutionMode mode, Common.ExecutionTypes type)
        {
            try
            {
                switch (mode)
                {
                    case ExecutionMode.Scalar:
                        object scalarResult = connection.ExecuteScalar(script);
                        return scalarResult;
                    case ExecutionMode.NonQuery:
                        connection.ExecuteNonQuery(script, type);
                        return null;
                    //Added for DC support
                    case ExecutionMode.WithResults:
                        object dataSet = connection.ExecuteWithResults(script);
                        return dataSet;
                    default:
                        TraceHelper.Assert(false, "Unknown ExecutionMode supplied");
                        return null;
                }
            }
            catch (Common.ExecutionFailureException efex)
            {
                if (efex.InnerException is SqlException)
                {
                    var sqlex = (SqlException)efex.InnerException;
                    if (sqlex.Number == 229)
                    {
                        throw new SfcSecurityException(SfcStrings.PermissionDenied, sqlex);
                    }
                }
                throw;
            }
        }

        public static object ExecuteScalar(Common.ServerConnection connection, string script)
        {
            object result = Execute(connection, script, ExecutionMode.Scalar);
            return result;
        }

        public static void ExecuteNonQuery(Common.ServerConnection connection, string script)
        {
            Execute(connection, script, ExecutionMode.NonQuery);
        }
    }

    public class SfcTSqlScript : ISfcScript
    {
        private StringCollection m_script;

        //This is added for the benefit of DC
        private SfcTSqlExecutor.ExecutionMode m_executionMode;

        //This is added for the benefit of IS
        private Common.ExecutionTypes m_executionType;

        private void Init()
        {
            m_script = new StringCollection();

            //Default to the ExecuteScalar mode as it was before (added for DC support)
            m_executionMode = SfcTSqlExecutor.ExecutionMode.Scalar;

            //Default to NoCommands as it was before (added for IS support)
            m_executionType = Common.ExecutionTypes.NoCommands;
        }

        public SfcTSqlScript()
        {
            Init();
        }

        public SfcTSqlScript(string batch)
        {
            Init();
            m_script.Add(batch);
        }

        public void AddBatch(string batch)
        {
            m_script.Add(batch);
        }

        void ISfcScript.Add(ISfcScript otherScript)
        {
            SfcTSqlScript sfcScript = (SfcTSqlScript)otherScript; // invalid cast here means domain is mixing batches from other domains

            foreach (string batch in sfcScript.GetScript())
            {
                this.AddBatch(batch);
            }
        }

        public StringCollection GetScript()
        {
            return m_script;
        }

        public override string ToString()
        {
            StringBuilder scriptSB = new StringBuilder();
            foreach (string stmt in m_script)
            {
                scriptSB.AppendLine(stmt);
            }
            return scriptSB.ToString();
        }

        /// <summary>
        /// Will set the execution mode of the script.
        /// DC needs this to set the execution mode to return a result set
        /// </summary>
        public SfcTSqlExecutor.ExecutionMode ExecutionMode
        {
            get
            {
                return this.m_executionMode;
            }
            set
            {
                this.m_executionMode = (SfcTSqlExecutor.ExecutionMode)value;
            }
        }

        /// <summary>
        /// Will set the execution type of the script.
        /// IS needs this to run multiple batches in one execution
        /// </summary>
        public Common.ExecutionTypes ExecutionType
        {
            get
            {
                return this.m_executionType;
            }
            set
            {
                this.m_executionType = (Common.ExecutionTypes)value;
            }
        }
    }

    public class SfcTSqlExecutionEngine : ISfcExecutionEngine
    {
        Common.ServerConnection m_connection;

        public SfcTSqlExecutionEngine(Common.ServerConnection connection)
        {
            m_connection = connection;
        }

        object ISfcExecutionEngine.Execute(ISfcScript script)
        {
            SfcTSqlScript sfcScript = (SfcTSqlScript)script; // we know what the type of script is, so this cast can't fail

            //Modified for IS support
            //Modified for DC support
            object retval = SfcTSqlExecutor.Execute(m_connection, sfcScript.ToString(), sfcScript.ExecutionMode, sfcScript.ExecutionType);
            return retval;
        }
    }
}