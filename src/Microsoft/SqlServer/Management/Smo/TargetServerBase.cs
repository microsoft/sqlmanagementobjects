// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class TargetServer : AgentObjectBase
    {
        internal TargetServer(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "TargetServer";
            }
        }
    }

    
}

