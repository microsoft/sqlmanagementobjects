// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    ///  Extension to base PostProcessing that adds row caching for the results,
    ///  so that a row is retrieved only once when multiple properties are set
    ///  from that data
    ///</summary>
    internal abstract class PostProcessWithRowCaching : PostProcess
    {
        /// <summary>
        /// The cached row results
        /// </summary>
        protected DataRowCollection rowResults = null;

        /// <summary>
        /// Whether the database we're querying is accessible to the user
        /// </summary>
        /// <remarks>Note we use a case-sensitive key comparer even though the database names may or may
        /// not be case-sensitive depending on the collation of the server. This is because SMO will use
        /// the same key for the same DB regardless so the case-sensitivity isn't going to change for a
        /// single DB within the context of SMO</remarks>
        private Dictionary<string, bool> dbIsAccessible = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// Whether we've retrieved the results already
        /// </summary>
        protected bool rowsRetrieved = false;

        /// <summary>
        /// Execute the query against the specified database, then caches the result until
        /// CleanRowData is called
        /// </summary>
        /// <param name="dp">The DataProvider for this request</param>
        /// <param name="databaseName">The database to execute the query against</param>
        protected void GetCachedRowResultsForDatabase(DataProvider dp, string databaseName)
        {
            var sc = new StringCollection();
            DataTable dt = null;

            if(!this.dbIsAccessible.ContainsKey(databaseName))
            {
                dbIsAccessible[databaseName] = true;
            }

            if (this.dbIsAccessible[databaseName] && !this.rowsRetrieved)
            {
                try
                {
                    //Refresh isAccessible in case it's changed since we last tried (note that this won't happen
                    //if we actually retrieve the results - so we won't be querying this for every property since
                    //we cache the whole row)
                    this.dbIsAccessible[databaseName] = ExecuteSql.GetIsDatabaseAccessibleNoThrow(this.ConnectionInfo, databaseName);

                    //Don't try running the query if we don't have access - since we're connecting directly to the DB this
                    //may fail (if the DB is in the restoring state for example) which may stop responding until the timeout period
                    //(default 30sec)
                    if (this.dbIsAccessible[databaseName])
                    {
                        sc.Add(this.SqlQuery);
                        dt = ExecuteSql.ExecuteWithResults(sc, this.ConnectionInfo, databaseName, false);
                    }
                }
                catch (Exception e)
                {
                    TraceHelper.Trace("PostProcess", "Exception in GetCachedRowResultsForDatabase: {0}", e.GetBaseException());
                    if (e is ConnectionFailureException || e is ExecutionFailureException)
                    {
                        //In case of a connection/execution failure we don't want to keep retrying since
                        //the row data is already going to be wrong (and retrying could get expensive if
                        //we have to wait for the timeout each time). So just clear the data and skip
                        //processing the rest
                        this.rowsRetrieved = false;
                        this.rowResults = null;
                        this.dbIsAccessible[databaseName] = false;
                        return;
                    }
                    throw;
                }

                if (dt != null)
                {
                    this.rowResults = dt.Rows;
                    this.rowsRetrieved = true;
                }
            }
        }

        protected abstract string SqlQuery { get; }

        /// <summary>
        /// Clear cached results
        /// </summary>
        public override void CleanRowData()
        {
            this.rowResults = null;
            this.rowsRetrieved = false;
            this.dbIsAccessible.Clear();
        }
    }
}
