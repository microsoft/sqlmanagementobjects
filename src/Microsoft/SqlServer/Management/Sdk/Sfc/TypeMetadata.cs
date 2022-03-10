// Copyright (c) Microsoft.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Provide type metadata for the object (information that doesn't change with every instance)
    /// </summary>
    public abstract class SfcTypeMetadata
    {
        public abstract bool IsCrudActionHandledByParent( SfcDependencyAction dependencyAction );
    }
}