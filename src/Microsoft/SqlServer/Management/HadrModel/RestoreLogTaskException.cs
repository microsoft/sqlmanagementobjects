// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when RestoreLogTask.perform fail
    /// </summary>
    public class RestoreLogTaskException : HadrTaskBaseException
    {
        /// <summary>
        /// Exception with DatabaseName and inner exception
        /// </summary>
        public RestoreLogTaskException(string DatabaseName,Exception inner)
            : base(Resource.FormatRestoreLogTaskException(DatabaseName), inner)
        {
        }
    }
}
