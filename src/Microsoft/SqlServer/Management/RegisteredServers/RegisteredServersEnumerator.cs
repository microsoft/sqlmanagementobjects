// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Reflection;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    internal sealed class RegisteredServersEnumerator : SqlObject, ISupportVersions
    {
        public override System.Reflection.Assembly ResourceAssembly
        {
            get
            {
                return Assembly.GetExecutingAssembly();
            }
        }

        #region ISupportVersions Members

        public ServerVersion GetServerVersion(object conn)
        {
            return ExecuteSql.GetServerVersion(conn);
        }

        #endregion

        #region SqlObjectBase overrides

		///	<summary>
		///	Allow subclasses to add anything to the statement
		///	</summary>
		protected override void BeforeStatementExecuted(string levelName)
		{
            // Unless this is the root store object where we're not
            // doing a real query, we add some query hints in order to
            // improve performance of the set of Join queries that the
            // Enumerator is producing for each repeated ServerGroup
            // in the xpath query. ServerGroup is a recursive type, so
            // it can appear any number of times, each time adding a
            // new Join to the query. Since each join enforces a
            // parent check in order from the top of the query to the
            // bottom, we use 'force order' so the optimizer doesn't
            // try to reorder anything in the query, and we use 'hash
            // join' to make sure that it uses our indexes when it
            // doesn't have table statistics.
            if (!levelName.Equals("RegisteredServersStore"))
            {
                if (String.IsNullOrEmpty(this.StatementBuilder.SqlPostfix))
                {
                    this.StatementBuilder.AddPostfix("option (hash join, force order)");
                }
            }
		}
        
        #endregion
    }
}
