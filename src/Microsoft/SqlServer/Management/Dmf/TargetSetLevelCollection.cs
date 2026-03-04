// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for TargetSet.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class TargetSetLevelCollection : SfcCollatedDictionaryCollection<TargetSetLevel, TargetSetLevel.Key, TargetSet>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public TargetSetLevelCollection(TargetSet parent) : base(parent)
        {
            this.IgnoreCase = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="customComparer"></param>
        public TargetSetLevelCollection(TargetSet parent, IComparer<string> customComparer)
            : base(parent, customComparer)
        {
            this.IgnoreCase = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterTypeSkeleton"></param>
        /// <returns></returns>
        public TargetSetLevel this[string filterTypeSkeleton]
        {
            get { return this[new TargetSetLevel.Key (filterTypeSkeleton)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterTypeSkeleton"></param>
        /// <returns></returns>
        public bool Contains(string filterTypeSkeleton)
        {
            return Contains (new TargetSetLevel.Key (filterTypeSkeleton));
        }

        // Implemented on derived collection class since it refers to a static method
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return TargetSetLevel.GetObjectFactory ();
        }

    }

}
