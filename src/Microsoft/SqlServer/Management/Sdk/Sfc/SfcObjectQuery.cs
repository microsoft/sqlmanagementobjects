// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Data;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    public class SfcObjectQuery
    {
        // The query to execute. It may return a single SfcInstance object, a live or cached SfcObjectIterator, or a DataTable.
        private SfcQueryExpression query = null;

        // The object tree root we are to access and populate
        private ISfcDomain domain = null;

        // The main connection to use. This may change for each ExecuteIterator if multiple active queries support is desired.
        private ISfcConnection domainConn = null;

        // Keep the SMO Server explicitly for strongly-typed checking and access to its methods
        private IAlienRoot nonSfcRoot = null;

        // The fields to retrieve. If null then default inexpensive fields are the default to get.
        private string[] fields = null;

        // The fields on which the result set will be ordered.
        private OrderBy[] orderByFields = null;

        // Cached, single or multiple active queries?
        //
        // The default is Cached which simply caches any Iterator queries in a DataTable and drives from that so the underlying
        // IDataReader is not tied up further.
        //
        // For either Single or Multiple modes, we need to pass this info to get the right connection from the domain root.
        // It is up to them to answer us correctly and do what is deemed necessary to give us a valid connection to use.
        // If multiple queries is indicated, then the connection returned must be capable of performing a query and getting results
        // assuming the main connection is busy with an already-active query. It may not be at the moment but it will be during some point while
        // the iterator still has the IDataReader open. That is why the onus is on the iterator now to produce a cloned or compatible connection
        // so the future IDataReader invoker can be blissfully unaware this happened.
        private SfcObjectQueryMode activeQueriesMode = SfcObjectQueryMode.CachedQuery;

        /// <summary>
        /// Create an object query for a particular domain instance.
        /// The query mode determines how requests to return an open iterator are handled and the particular connection to use.
        /// </summary>
        /// <param name="domain">The root of the SfcInstance object tree to query.</param>
        /// <param name="activeQueriesMode">CachedQueries avoids any issues with more than one running query by first caching the query results and closing the query data reader.
        /// SingleActive
        /// MultipleActiveQueries indicated that any provided connection via the domain's GetConnection method should assume that the main connection is busy with an already-active query and
        /// an alternate connection may be more suitable to use.</param>
        public SfcObjectQuery(ISfcDomain domain, SfcObjectQueryMode activeQueriesMode)
        {
            Init(domain, activeQueriesMode);
        }

        /// <summary>
        /// Create an object query for a particular domain instance object hierarchy and connection.
        /// If you use ExecuteIterator and want to make another query before closing it, consider using the overload
        /// which indicates support for multiple active queries, or simply use ExecuteCollection to avoid it.
        /// </summary>
        /// <param name="domain">The domain of the SfcInstance object tree to query.</param>
        public SfcObjectQuery(ISfcDomain domain)
        {
            Init(domain, SfcObjectQueryMode.CachedQuery);
        }

        /// This function should only exist as long as the SMO.Server
        /// class is not an SfcInstance. The given object must be
        /// an instance of SMO.Server.
        public SfcObjectQuery(IAlienRoot root)
        {
            this.nonSfcRoot = root;
        }

        /// <summary>
        /// The most recent query string processed. This is readonly since each query execution is passed the query to perform.
        /// </summary>
        public SfcQueryExpression SfcQueryExpression
        {
            get
            {
                return this.query;
            }
        }

        /// <summary>
        /// Single or multiple active queries?
        /// If multiple queries is indicated, then the connection returned must be capable of performing a query and getting results
        /// assuming the main connection is busy with an already-active query.
        /// </summary>
        public SfcObjectQueryMode ActiveQueriesMode
        {
            get
            {
                return this.activeQueriesMode;
            }
            set
            {
                this.activeQueriesMode = value;
            }
        }
		
        /// <summary>
        /// Execute the query string to retrieve the specified fields and return a fully populated DataTable.
        /// </summary>
        /// <param name="query">The query string to process.</param>
        /// <param name="fields">The field names to retrieve. If null, all default inexpensive fields are retrieved.</param>
        /// <param name="orderByFields">The field names that we need to order on. If null no order will be used.</param>
        /// <returns>The data table of results.</returns>
        public DataTable ExecuteDataTable(SfcQueryExpression query, string[] fields, OrderBy[] orderByFields)
        {
            this.query = query;
            this.fields = fields;
            this.orderByFields = orderByFields;
            ValidateQueryExpression(this.query);

            if (nonSfcRoot != null)
            {
                return nonSfcRoot.SfcHelper_GetDataTable(nonSfcRoot.ConnectionContext, query.ToString(), fields, orderByFields);
            }
            else
            {
                if (this.domain.ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    // The caller should be resilient to getting a completely empty DataTable.
                    return new DataTable();
                }
                else
                {
                    return Enumerator.GetData(domainConn.ToEnumeratorObject(), query.ToString(), fields, orderByFields);
                }
            }
        }

        /// <summary>
        /// Execute the query string and return an SfcObjectIterator to enumerate the results without caching them.
        /// If MultipleActiveQueries is true, then you must either provide a suitable connection when requests by GetConnection
        /// or use the ExecuteCachedIterator instead to avoid this issue.
        /// </summary>
        /// <param name="query">The query string to process.</param>
        /// <param name="fields">The field names to retrieve. If null, all default inexpensive fields are retrieved.</param>
        /// <param name="orderByFields"></param>
        /// <returns>A SfcObjectIterator to enumerate the results. This is suitable for IEnumerable use and must be disposed when done.</returns>
        public IEnumerable ExecuteIterator(SfcQueryExpression query, string[] fields, OrderBy[] orderByFields)
        {
            this.query = query;
            this.fields = fields;
            this.orderByFields = orderByFields;
            ValidateQueryExpression(this.query);

            if (null != this.nonSfcRoot)
            {
                return new NonSfcObjectIterator(this.nonSfcRoot, this.activeQueriesMode, this.query, this.fields, this.orderByFields);
            }

            // The guards for disconnected access via iterator are in the SfcObjectIterator class.
            return new SfcObjectIterator(this.domain, this.activeQueriesMode, this.query, this.fields, this.orderByFields);
        }

        /// <summary>
        /// Common constructor init.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="mode"></param>
        private void Init(ISfcDomain domain, SfcObjectQueryMode mode)
        {
            this.domain = domain;
            this.activeQueriesMode = mode;

            // Any connection sent into OQ must be a SfcConnection-derived one
            ISfcHasConnection hasConn = domain as ISfcHasConnection;
            TraceHelper.Assert(null != hasConn);

            // The connection can be null if we are Offline (disconnected)
            this.domainConn = hasConn.GetConnection();
            TraceHelper.Assert(null != domainConn || this.domain.ConnectionContext.Mode == SfcConnectionContextMode.Offline);
        }

        // Validate the query string.
        private void ValidateQueryExpression (SfcQueryExpression query)
        {
            // The OQ was created with Server in the ctor but the query does not start with "Server"
            if (nonSfcRoot != null && !query.ToString ().StartsWith ("Server", StringComparison.OrdinalIgnoreCase))
            {
                throw new SfcInvalidQueryExpressionException (SfcStrings.InvalidSMOQuery (query.ToString ()));
            }
        }

    }
}
