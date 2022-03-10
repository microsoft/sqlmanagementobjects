// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Used by WHIHelper for the Feature Installation State
    /// </summary>
    public enum FeatureInstallationState
    {
        /// <summary>
        /// unkown state
        /// </summary>
        Unknown,

        /// <summary>
        /// feature installed
        /// </summary>
        Installed,

        /// <summary>
        /// not installed
        /// </summary>
        NotInstalled
    }
}
