// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// ISfcScript interface is an abstraction over a script concept. Most domains would implement
    /// this interface via text
    /// </summary>
    public interface ISfcScript
    {
        /// <summary>
        /// Add a "batch" to the script to be executed individually
        /// </summary>
        /// <param name="script">Partial Script</param>
        void Add(ISfcScript script);
    }
}