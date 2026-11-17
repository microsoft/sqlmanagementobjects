// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for PolicyCategorySubscriptions.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyCategorySubscriptionCollection : SfcDictionaryCollection<PolicyCategorySubscription, PolicyCategorySubscription.Key, PolicyStore>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public PolicyCategorySubscriptionCollection(PolicyStore parent) : base(parent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PolicyCategorySubscription this[int id]
        {
            get { return this[new PolicyCategorySubscription.Key(id)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(int id)
        {
            return Contains(new PolicyCategorySubscription.Key(id));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl ()
        {
            return PolicyCategorySubscription.GetObjectFactory ();
        }


        internal IEnumerable<string> EnumCategorySubscribers (string category, string targetType)
        {
            foreach (PolicyCategorySubscription pcs in this)
            {
                if (pcs.PolicyCategory == category && pcs.TargetType == targetType)
                {
                    yield return pcs.Target;
                }
            }
        }
    }

}
