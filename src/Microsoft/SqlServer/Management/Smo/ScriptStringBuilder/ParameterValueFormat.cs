// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// How a parameter value in a SMO script should be formatted
    /// </summary>
    internal enum ParameterValueFormat
    {
        /// <summary>
        /// Not quoted, e.g. value = 2
        /// </summary>
        NotString,

        /// <summary>
        /// Quoted as a CHAR, e.g. value = '2'
        /// </summary>
        CharString,

        /// <summary>
        /// 
        /// </summary>
        NVarCharString
    }
}