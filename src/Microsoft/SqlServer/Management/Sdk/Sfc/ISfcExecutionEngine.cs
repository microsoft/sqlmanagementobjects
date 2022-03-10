// Copyright (c) Microsoft.
// Licensed under the MIT license.
namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// ISfcExecutionEngine is an abstraction over a domain-provided component that is able to execute an ISfcScript
    /// </summary>
    public interface ISfcExecutionEngine
    {
        /// <summary>
        /// Execute a script
        /// </summary>
        /// <param name="script">Script to be executed</param>
        object Execute(ISfcScript script);
    }
}