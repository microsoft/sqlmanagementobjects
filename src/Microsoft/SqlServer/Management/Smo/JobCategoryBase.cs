// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class JobCategory : CategoryBase
    {
        internal JobCategory(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "JobCategory";
            }
        }

        internal override string GetCategoryClassName()
        {
            return "JOB";
        }

        internal override int GetCategoryClass()
        {
            return 1;
        }

        internal override string GetCategoryTypeName()
        {
            Property propType = Properties.Get("CategoryType");
            string categoryTypeName = string.Empty;
            if (null != propType.Value)
            {
                categoryTypeName = GetCatTypeName((CategoryType)propType.Value);
            }
            else
            {
                categoryTypeName = GetCatTypeName(CategoryType.LocalJob);
            }

            return categoryTypeName;


        }
    }
}
