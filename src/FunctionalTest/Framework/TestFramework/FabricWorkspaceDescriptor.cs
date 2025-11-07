// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Exposes Fabric workspace properties for test cases.
    /// </summary>
    public class FabricWorkspaceDescriptor : TestDescriptor
    {
        /// <summary>
        /// Constructs a FabricWorkspaceDescriptor for the given environment.
        /// </summary>
        /// <param name="environment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FabricWorkspaceDescriptor(string environment)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            FabricDatabaseManager = new FabricDatabaseManager(environment);
        }

        private FabricDatabaseManager FabricDatabaseManager { get; set; }
        /// <summary>
        /// The name of the environment hosting the workspace (msit/daily etc)
        /// </summary>
        public string Environment { get; }
        /// <summary>
        /// Name of the workspace
        /// </summary>
        public string WorkspaceName { get; set; }
        /// <summary>
        /// Prefix for the database names created in this workspace
        /// </summary>
        public string DbNamePrefix { get; set; }

        /// <summary>
        /// Creates a Fabric DB or Fabric DW instance depending on the DatabaseEngineEdition
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public string CreateDatabase(string databaseName) => DatabaseEngineEdition == Management.Common.DatabaseEngineEdition.SqlOnDemand ?
            FabricDatabaseManager.CreateWarehouse(WorkspaceName, databaseName) :
            FabricDatabaseManager.CreateDatabase(WorkspaceName, databaseName);

        /// <summary>
        /// Drops the Fabric DB or Fabric DW instance depending on the DatabaseEngineEdition
        /// </summary>
        /// <param name="databaseName"></param>
        public void DropDatabase(string databaseName)
        {
            if (DatabaseEngineEdition == Management.Common.DatabaseEngineEdition.SqlOnDemand)
            {
                FabricDatabaseManager.DropWarehouse(WorkspaceName, databaseName);
            }
            else
            {
                FabricDatabaseManager.DropDatabase(WorkspaceName, databaseName);
            }
        }
    }
}
