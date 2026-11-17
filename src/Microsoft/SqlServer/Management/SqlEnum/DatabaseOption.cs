// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using Microsoft.SqlServer.Management.Sdk.Sfc;


    internal class DatabaseOption : SqlObject
    {
        //override RetrieveParentRequest
        //the only change is to set ResolveDatabases to false
        //this will have the effect treating DatabaseOption as if is not inside a Database
        //that means that a "use" clause will not be generated and it will join with the parent table
        // ( in this case sys.database ) without a special treatment of entering a database.
        public override Request RetrieveParentRequest()
        {
            SqlRequest sr = (SqlRequest)base.RetrieveParentRequest();
            if( null == sr )
            {
                sr = new SqlRequest();
            }
            sr.ResolveDatabases = false;
            return sr;
        }
    }
}
