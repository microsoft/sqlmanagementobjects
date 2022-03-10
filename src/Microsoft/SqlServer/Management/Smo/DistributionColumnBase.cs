using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class DistributionColumn : NamedSmoObject
    {
        internal DistributionColumn(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        internal static string UrnSuffix
        {
            get 
            {
                return "DistributionColumn";
            }
        }

    }
}

