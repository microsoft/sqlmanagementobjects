// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public static class DatabaseHandlerFactory
    {
        public static IDatabaseHandler GetDatabaseHandler(TestDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            switch (descriptor)
            {
                case TestServerDescriptor serverDescriptor:
                    if (serverDescriptor.ReuseExistingDatabase)
                    {
                        return new ReuseExistingDatabaseHandler(serverDescriptor);
                    }
                    return new RegularDatabaseHandler(serverDescriptor);

                case FabricWorkspaceDescriptor fabricWorkspaceDescriptor:
                    return new FabricDatabaseHandler(fabricWorkspaceDescriptor);

                default:
                    throw new ArgumentException($"Unsupported TestDescriptor type: {descriptor.GetType().Name}");
            }
        }
    }
}
