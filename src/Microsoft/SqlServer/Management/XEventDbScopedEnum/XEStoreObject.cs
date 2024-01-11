// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Data;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEventDbScoped
{
    /// <summary>
    /// This is the Enumerator object for XEvnet object model. It derived from SqlObject, the
    /// base class for all enumerator in SFC enabled object model.Override the ResourceAssembly
    /// to provide the correct assembly that contains the resources.
    /// </summary>
    internal sealed class XEStoreObject : SqlObject, ISupportVersions, ISupportDatabaseEngineTypes
    {
        /// <summary>
        /// Return the assebmly that contains the resources.
        /// </summary>
        public override Assembly ResourceAssembly
        {
            get
            {
                return Assembly.GetExecutingAssembly();
            }
        }

        /// <summary>
        /// Return the server version for the given connection.
        /// </summary>
        /// <param name="conn">connetion to the server we want to know the version</param>
        /// <returns>server version on the connection</returns>
        public ServerVersion GetServerVersion(object conn)
        {
            return ExecuteSql.GetServerVersion(conn);
        }

        /// <summary>
        /// Return the databse engine type for the given connection.
        /// </summary>
        /// <param name="conn">connetion to the server we want to know the type</param>
        /// <returns>engine type of the server on the connection</returns>
        public DatabaseEngineType GetDatabaseEngineType(object conn)
        {
            return ExecuteSql.GetDatabaseEngineType(conn);
        }

        public override EnumResult GetData(EnumResult parentResult)
        {
            string database = this.GetFixedStringProperty("Name", true /* remove escapes */);

            // Nullify the filter. 
            this.Filter = null;

            if (database != null)
            {
                this.StatementBuilder = new StatementBuilder();
                DatabaseEngineType databaseEngineType = ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo);
                SqlEnumResult ser = new SqlEnumResult(this.StatementBuilder, ResultType.Reserved1, databaseEngineType);

                DataTable dt = new DataTable();
                dt.Locale = System.Globalization.CultureInfo.InvariantCulture;
                DataColumn dc = dt.Columns.Add("Name", System.Type.GetType("System.String"));
                DataRow dr = dt.NewRow();
                dr[0] = Urn.UnEscapeString(database);
                dt.Rows.Add(dr);

                ser.Databases = dt;
                ser.NameProperties.Add("Name");

                return base.GetData(ser);
            }

            return base.GetData(parentResult);
        }
    }
}
