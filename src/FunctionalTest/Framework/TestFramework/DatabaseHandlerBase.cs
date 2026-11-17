// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public abstract class DatabaseHandlerBase : IDatabaseHandler
    {
        public SMO.Server ServerContext { get; set; }
        public TestDescriptor TestDescriptor { get; set; }
        public string DatabaseDisplayName { get; set; }

        public abstract Database HandleDatabaseCreation(DatabaseParameters dbParameters = null);
        
        public virtual void HandleDatabaseDrop()
        {
            // Default behavior: skip dropping the database
            Trace.TraceInformation("Skipping database drop.");
        }

        protected SMO.Server InitializeServerContext(SqlConnectionStringBuilder sqlConnectionStringBuilder)
        {
            // 10 minute minimum for statement timeouts
#if MICROSOFTDATA
            var commandTimeout = sqlConnectionStringBuilder.CommandTimeout > 0 ? Math.Max(600, sqlConnectionStringBuilder.CommandTimeout) : 0;
#else
                            var commandTimeout = 600;
#endif
            var serverConnection = new ServerConnection(new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
            {
                StatementTimeout = commandTimeout,
            };

            return new SMO.Server(serverConnection);
        }
    }

}
