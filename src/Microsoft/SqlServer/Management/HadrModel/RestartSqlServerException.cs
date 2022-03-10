// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Globalization;


namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Restart SQL Server Exception
    /// Thrown from AddDomainUserInSQLAdminTask.RestartSqlServer method
    /// </summary>
    public class RestartSqlServerException : HadrTaskBaseException
    {
        /// <summary>
        /// Standard RestartSqlServerException with Instance Name
        /// </summary>
        /// <param name="InstanceName"> target Sql Instance Name</param>
        public RestartSqlServerException(string InstanceName)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.RestartSqlServerException, InstanceName))
        {
        }
    }
}
