// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public class ReuseExistingDatabaseHandler : DatabaseHandlerBase
    {
        public ReuseExistingDatabaseHandler(TestDescriptor descriptor)
        {
            if (descriptor == null || !(descriptor is TestServerDescriptor))
            {
                throw new ArgumentException($"The descriptor must be of type {nameof(TestServerDescriptor)}.", nameof(descriptor));
            }
            this.TestDescriptor = descriptor;

            // Initialize ServerContext
            var serverDescriptor = (TestServerDescriptor)this.TestDescriptor;
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(serverDescriptor.ConnectionString);
            this.ServerContext = InitializeServerContext(sqlConnectionStringBuilder);
        }

        public override Database HandleDatabaseCreation(DatabaseParameters dbParameters = null)
        {
            Trace.TraceInformation("Reusing user database " + ServerContext.ConnectionContext.SqlConnectionObject.Database);
            // For Azure databases, when connected directly to a user database specified in the
            // connection file, we just reuse it for every test and don't run any tests in parallel.
            
            var db = this.ServerContext.Databases.Cast<Database>().First(d => d.Name != "master");
            db.DropAllObjects();
            Trace.TraceInformation("Resetting database state for reuse");
            if (db.UserAccess != DatabaseUserAccess.Multiple || db.ReadOnly || !db.AutoUpdateStatisticsEnabled || db.ChangeTrackingEnabled)
            {
                db.UserAccess = DatabaseUserAccess.Multiple;
                db.ReadOnly = false;
                db.AutoUpdateStatisticsEnabled = true;
                if (db.ChangeTrackingEnabled)
                {
                    db.ChangeTrackingAutoCleanUp = false;
                    db.ChangeTrackingEnabled = false;
                }
                db.Alter();
            }

            db.ExecutionManager.ConnectionContext.Disconnect();
            db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
            this.ServerContext.Databases.ClearAndInitialize(null, null);
            // We return a fresh Database object because after DropAllObjects the object has some incorrect internal state.
            // It would take a long time to investigate the sources of inconsistency and that work would have little customer value.
            db = this.ServerContext.Databases[db.Name];
            this.DatabaseDisplayName = db.Name;

            return db;
        }

    }
}
