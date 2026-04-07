// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Add Domain User In AdminGroup Task Exception
    /// </summary>
    public class SqlServerConnectionException : HadrTaskBaseException
    {
        /// <summary>
        /// Standard SqlServerConnectionException with domain user name
        /// </summary>
        /// <param name="VMIPAddress"></param>
        public SqlServerConnectionException(string VMIPAddress)
            : base(Resource.FormatAddDomainUserInAdminGroupTaskException(VMIPAddress))
        {
        }

    }
}
