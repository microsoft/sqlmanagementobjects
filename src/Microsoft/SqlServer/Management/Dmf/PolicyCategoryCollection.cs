// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for Policy categories.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyCategoryCollection : SfcCollatedDictionaryCollection<PolicyCategory, PolicyCategory.Key, PolicyStore>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public PolicyCategoryCollection(PolicyStore parent)
            : base(parent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="customComparer"></param>
        public PolicyCategoryCollection(PolicyStore parent, IComparer<string> customComparer)
            : base(parent, customComparer)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PolicyCategory this[string name]
        {
            get { return this[new PolicyCategory.Key(name)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return Contains(new PolicyCategory.Key(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return PolicyCategory.GetObjectFactory();
        }

    }
}
