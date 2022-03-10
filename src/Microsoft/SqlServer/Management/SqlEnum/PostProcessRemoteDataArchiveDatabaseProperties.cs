// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// A class that post processes the RemoteDataArchive properties for a database.
    /// </summary>
    /// <remarks>Post-Processing is necessary in this case because we need to run a query against the DB itself.
    /// The query generated from the Database.xml file runs in the context of the master DB so will not work.</remarks>
    internal class PostProcessRemoteDataArchiveDatabaseProperties : PostProcessWithRowCaching
    {
        /// <summary>
        /// Name of RemoteDataArchiveEndpoint property
        /// </summary>
        private const string remoteDataArchiveEndpoint = "RemoteDataArchiveEndpoint";

        /// <summary>
        /// Name of RemoteDataArchiveDatabaseName property
        /// </summary>
        private const string remoteDataArchiveDatabaseName = "RemoteDatabaseName";

        /// <summary>
        /// Name of RemoteDataArchiveLinkedServer property
        /// </summary>
        private const string remoteDataArchiveLinkedServer = "RemoteDataArchiveLinkedServer";

        /// <summary>
        /// Name of FederatedServiceAccount property
        /// </summary>
        private const string remoteDataArchiveFederatedServiceAccount = "RemoteDataArchiveUseFederatedServiceAccount";

        /// <summary>
        /// Name of RemoteDataArchiveCredential property
        /// </summary>
        private const string remoteDataArchiveCredential = "RemoteDataArchiveCredential";

        /// <summary>
        /// Build T-Sql queries to get values for PostProcess RemoteDataArchiveEndpoint, RemoteDataArchiveLinkedServer, RemoteDataArchiveDatabaseName
        /// </summary>
        /// <returns></returns>
        protected override string SqlQuery
        {
            get
            {
                var selectQuery = new StatementBuilder();

                // add property to SELECT list
                selectQuery.AddProperty(remoteDataArchiveEndpoint, @"eds.location");
                selectQuery.AddProperty(remoteDataArchiveLinkedServer, @"eds.name");
                selectQuery.AddProperty(remoteDataArchiveDatabaseName, @"rdad.remote_database_name");
                selectQuery.AddProperty(remoteDataArchiveFederatedServiceAccount, @"rdad.federated_service_account");
                selectQuery.AddProperty(remoteDataArchiveCredential, @"case when rdad.federated_service_account = 1 then null else cred.name end");
                selectQuery.AddFrom(@"sys.remote_data_archive_databases rdad");
                selectQuery.AddJoin(@"INNER JOIN sys.external_data_sources eds ON rdad.data_source_id = eds.data_source_id");
                selectQuery.AddJoin(@"LEFT OUTER JOIN sys.database_scoped_credentials cred ON eds.credential_id = cred.credential_id");
                return selectQuery.SqlStatement;
            }

        }

        /// <summary>
        /// Returns a boolean indicating if the current stretch queries
        /// will work on metadata views on the target server.
        /// 
        /// The check is based on a version check of the server.
        /// The metaviews related to stretch were changed between ctp2.4 
        /// and ctp3.0
        /// 
        /// The new queries added in ctp3.0 smo should not be allowed
        /// to run on ctp2.4 or lower server.
        /// </summary>
        /// <param name="sqlServerVersion"></param>
        /// <returns></returns>
        private bool IsStretchSmoSupportedOnVersion(Version sqlServerVersion)
        {
            // Smo can only query stretch related properties
            // for server versions higher or equal than 13.0.700.
            // This is because considerable changes have happened
            // in the DMVs between versions prior to 13.0.700 and 13.0.700.
            if (sqlServerVersion < new Version(13, 0, 700))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the value of required property
        /// </summary>
        /// <param name="name">Name of a table property</param>
        /// <param name="data">data</param>
        /// <param name="dp">data provider</param>
        /// <returns>Value of the property</returns>
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if(!string.IsNullOrEmpty(name))
            {
                ServerVersion targetSqlServerVersion = ExecuteSql.GetServerVersion(this.ConnectionInfo);
                bool isValidSqlServerVersion = this.IsStretchSmoSupportedOnVersion(new Version(targetSqlServerVersion.Major, targetSqlServerVersion.Minor, targetSqlServerVersion.BuildNumber));

                if (isValidSqlServerVersion)
                {
                    bool isRemoteDataArchivePropertyName = (name.Equals(remoteDataArchiveEndpoint, StringComparison.InvariantCultureIgnoreCase) ||
                         name.Equals(remoteDataArchiveDatabaseName, StringComparison.InvariantCultureIgnoreCase) ||
                         name.Equals(remoteDataArchiveLinkedServer, StringComparison.InvariantCultureIgnoreCase) ||
                         name.Equals(remoteDataArchiveFederatedServiceAccount, StringComparison.InvariantCultureIgnoreCase) ||
                         name.Equals(remoteDataArchiveCredential, StringComparison.InvariantCultureIgnoreCase));
                    if (isRemoteDataArchivePropertyName)
                    {
                        this.GetCachedRowResultsForDatabase(dp, databaseName : GetTriggeredString(dp, 0));
                    }
                    
                }

                data = DBNull.Value;
                switch (name)
                {
                    case remoteDataArchiveEndpoint:
                    case remoteDataArchiveLinkedServer:
                    case remoteDataArchiveDatabaseName:
                    case remoteDataArchiveFederatedServiceAccount:
                    case remoteDataArchiveCredential:
                        if (this.rowResults != null && this.rowResults.Count > 0)
                        {
                            data = this.rowResults[0][name];
                        }
                        break;
                    default:
                        TraceHelper.Assert(false,
                            string.Format(CultureInfo.InvariantCulture,
                                        "PostProcessRemoteDataArchiveDatabaseProperties - Unknown property {0}",
                                        name));
                        break;
                }

                return data;
            }
            else
            {
                TraceHelper.Assert(false,
                           string.Format(CultureInfo.InvariantCulture,
                           "PostProcessRemoteDataArchiveDatabaseProperties - Column name is null"));
                return null;
            }
        }

    }
}
