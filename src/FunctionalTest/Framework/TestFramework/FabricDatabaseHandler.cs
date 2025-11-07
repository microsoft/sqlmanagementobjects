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
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public class FabricDatabaseHandler : DatabaseHandlerBase
    {
        public FabricDatabaseHandler(TestDescriptor descriptor)
        {
            if (descriptor == null || !(descriptor is FabricWorkspaceDescriptor))
            {
                throw new ArgumentException($"The descriptor must be of type {nameof(FabricWorkspaceDescriptor)}.", nameof(descriptor));
            }

            TestDescriptor = descriptor;
        }
        public override Database HandleDatabaseCreation(DatabaseParameters dbParameters)
        {
            var fabricWorkspaceDescriptor = (FabricWorkspaceDescriptor)TestDescriptor;
            string fabricDbName;
            var currentUtcTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var dbNamePrefix = $"{fabricWorkspaceDescriptor.DbNamePrefix}{currentUtcTime}-";
            if (dbParameters == null)
            {
                fabricDbName = SmoObjectHelpers.GenerateUniqueObjectName(dbNamePrefix);
            }
            else
            {
                fabricDbName = SmoObjectHelpers.GenerateUniqueObjectName(dbNamePrefix,
                    includeClosingBracket: dbParameters.UseEscapedCharacters,
                    includeDoubleClosingBracket: dbParameters.UseEscapedCharacters,
                    includeSingleQuote: dbParameters.UseEscapedCharacters,
                    includeDoubleSingleQuote: dbParameters.UseEscapedCharacters);
            }
            //create fabric database using fabric-cli
            var connectionString = fabricWorkspaceDescriptor.CreateDatabase(fabricDbName);
            Trace.TraceInformation($"Created fabric database {fabricDbName}");
            Database db;
            try
            {
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                ServerContext = InitializeServerContext(sqlConnectionStringBuilder);
                // Fabric database InitialCatalog is slightly different from the database display name
                // e.g. "SmoTestFabric-'']]]'{e2e334e4-043a-4f31-ad6c-b9649f886d2a}" is the display name
                // but the InitialCatalog is "SmoTestFabric-'']]]'{e2e334e4-043a-4f31-ad6c-b9649f886d2a}-46ecae46-6627-43db-8c0d-53ae916a0a23"
                var dbName = string.IsNullOrEmpty(sqlConnectionStringBuilder.InitialCatalog) ? fabricDbName : sqlConnectionStringBuilder.InitialCatalog;
                db = ServerContext.Databases[dbName];
                DatabaseDisplayName = fabricDbName;
                if (!string.IsNullOrEmpty(dbParameters?.BackupFile))
                {
                    // Restore DB from the specified backup file
                    db = ServerContext.RestoreDatabaseFromBackup(dbParameters.BackupFile, dbName, ConnectionHelpers.GetAzureKeyVaultHelper().StorageHelper);
                }
            }
            catch
            {
                //clean up fabric database if we failed to connect to it
                fabricWorkspaceDescriptor.DropDatabase(fabricDbName);
                throw;
            }
            return db;
        }

        public override void HandleDatabaseDrop()
        {
            var fabricWorkspaceDescriptor = TestDescriptor as FabricWorkspaceDescriptor;
            if (!string.IsNullOrEmpty(DatabaseDisplayName))
            {
                fabricWorkspaceDescriptor.DropDatabase(DatabaseDisplayName);
            }
        }
    }
}
