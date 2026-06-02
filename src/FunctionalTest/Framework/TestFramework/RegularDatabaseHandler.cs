// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public class RegularDatabaseHandler : DatabaseHandlerBase
    {
       
        public RegularDatabaseHandler(TestDescriptor descriptor)
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
        public override Database HandleDatabaseCreation(DatabaseParameters dbParameters)
        {
            if (dbParameters == null)
            {
                return this.ServerContext.CreateDatabaseWithRetry();
            }

            var db = this.ServerContext.CreateDatabaseWithRetry(
                            dbParameters);
            this.DatabaseDisplayName = db.Name;

            return db;
        }

        public override void HandleDatabaseDrop()
        {
            if (this.ServerContext != null && !string.IsNullOrEmpty(this.DatabaseDisplayName))
            {
                this.ServerContext.DropKillDatabaseNoThrow(this.DatabaseDisplayName);
            }
        }
    }
}
