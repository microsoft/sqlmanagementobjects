// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Provide an access to the Script Collector instance
    /// </summary>
    public interface ISfcScriptCollector
    {
        /// <summary>
        /// Opens writer of the supplied type
        /// </summary>
        /// <typeparam name="T">type of the writer</typeparam>
        /// <returns></returns>
        T OpenWriter<T>();

        /// <summary>
        /// Opens or Reopens writer of the supplied type 
        /// </summary>
        /// <typeparam name="T">type of the writer</typeparam>
        /// <param name="append">indicates that previous writer of the same type should be reused</param>
        /// <returns></returns>
        T OpenWriter<T>(bool append);
    }
}
