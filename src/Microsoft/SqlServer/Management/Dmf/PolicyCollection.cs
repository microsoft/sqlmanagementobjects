// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for Policies.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyCollection : SfcCollatedDictionaryCollection<Policy, Policy.Key, PolicyStore>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public PolicyCollection(PolicyStore parent)
            : base(parent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="customComparer"></param>
        public PolicyCollection(PolicyStore parent, IComparer<string> customComparer)
            : base(parent, customComparer)
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Policy this[string name]
        {
            get { return this[new Policy.Key(name)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return Contains(new Policy.Key(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return Policy.GetObjectFactory();
        }

    }

}
