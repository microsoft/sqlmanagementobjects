// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class AlertCategory : CategoryBase
    {
        internal AlertCategory(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "AlertCategory";
            }
        }


        internal override string GetCategoryClassName()
        {
            return "ALERT";
        }

        internal override int GetCategoryClass()
        {
            return 2;
        }

    }
}
