// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public class FabricWorkspaceDescriptor : TestDescriptor
    {
        public string Environment { get; set; }
        public string WorkspaceName { get; set; }
        public string DbNamePrefix { get; set; }

        public string CreateDatabase(string databaseName) => FabricDatabaseManager.CreateDatabase(WorkspaceName, databaseName);
        public void DropDatabase(string databaseName) => FabricDatabaseManager.DropDatabase(WorkspaceName, databaseName);

    }

}
