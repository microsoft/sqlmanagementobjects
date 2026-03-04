// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Smo;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public interface IDatabaseHandler
    {
        SMO.Server ServerContext { get; set; }
        TestDescriptor TestDescriptor { get; set; }
        string DatabaseDisplayName { get; set; }
        Database HandleDatabaseCreation(DatabaseParameters dbParameters = null);
        void HandleDatabaseDrop();
    }
}
