// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class OperatorCategory : CategoryBase
    {
        internal OperatorCategory(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "OperatorCategory";
            }
        }


        internal override string GetCategoryClassName()
        {
            return "OPERATOR";
        }

        internal override int GetCategoryClass()
        {
            return 3;
        }

    }
}
