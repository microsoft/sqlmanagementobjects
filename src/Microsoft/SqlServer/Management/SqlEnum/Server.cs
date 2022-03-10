// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using Common;
    using Microsoft.SqlServer.Management.Sdk.Sfc;


    internal class SqlServer : SqlObject, ISupportVersions, ISupportDatabaseEngineTypes, ISupportDatabaseEngineEditions
    {
        public override EnumResult GetData(EnumResult erParent)
        {
            //don't take sever's filter into consideration
            this.Filter = null;
            if( null == Request ) //it is in an intermediate possition
            {
                this.StatementBuilder = new StatementBuilder();
                this.ConditionedSqlList.AddHits(this, string.Empty, this.StatementBuilder);
                DatabaseEngineType dbType = ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo);
                return new SqlEnumResult(this.StatementBuilder, ResultType.Reserved1 ,dbType );
            }

            return base.GetData(erParent);
        }

        public ServerVersion GetServerVersion(Object conn)
        {
            return ExecuteSql.GetServerVersion(conn);
        }

        public DatabaseEngineType GetDatabaseEngineType(Object conn)
        {
            return ExecuteSql.GetDatabaseEngineType(conn);
        }

        public DatabaseEngineEdition GetDatabaseEngineEdition(Object conn)
        {
            return ExecuteSql.GetDatabaseEngineEdition(conn);
        }
    }

    
}
