// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
namespace Microsoft.SqlServer.Management.Smo
{
    public sealed partial class DatabaseCollection : SimpleObjectCollectionBase<Database, Server>
    {
        internal override SqlSmoObject GetObjectByName(string name)
        {
            try
            {
                return  base.GetObjectByName(name);
            }
            catch (Microsoft.SqlServer.Management.Common.ConnectionFailureException cfe) 
                   when (cfe.InnerException is SqlException ex && ex.Number == 4060)
            {                  
                Microsoft.SqlServer.Management.Diagnostics.TraceHelper.LogExCatch(cfe);
                // this exception occurs if the user doesn't have access to 
                //  the database with the input name
                //  in such a case the expected behavior is to return null  
                return null;
            }
        }
    }
}
