// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when RestoreDatabaseTask.perform fail
    /// </summary>
    public class RestoreDatabaseTaskException : HadrTaskBaseException
    {
        /// <summary>
        /// Exception with DatabaseName and inner exception
        /// </summary>
        public RestoreDatabaseTaskException(string DatabaseName, Exception inner)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.RestoreDatabaseTaskExcption, DatabaseName), inner)
        {
        }
    }
}
