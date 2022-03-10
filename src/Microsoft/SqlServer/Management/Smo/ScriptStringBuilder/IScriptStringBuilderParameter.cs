// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Interface used for parameters of ScriptStringBuilder
    /// </summary>
    internal interface IScriptStringBuilderParameter
    {
        /// <summary>
        /// Gets the parameter's key
        /// </summary>
        /// <returns>Parameter's key as a string</returns>
        string GetKey();

        /// <summary>
        /// Gets the TSQL script representation of this parameter
        /// </summary>
        /// <returns>This parameter as TSQL script</returns>
        string ToScript();
    }
}