// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;

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
            : base(string.Format(CultureInfo.InvariantCulture, Resource.AddDomainUserInAdminGroupTaskException, VMIPAddress))
        {
        }

    }
}
