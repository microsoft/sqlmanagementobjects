// Copyright (c) Microsoft.
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
    public sealed class TargetSetCollection : SfcCollatedDictionaryCollection<TargetSet, TargetSet.Key, ObjectSet>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public TargetSetCollection(ObjectSet parent) : base(parent)
        {
            this.IgnoreCase = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="customComparer"></param>
        public TargetSetCollection(ObjectSet parent, IComparer<string> customComparer)
            : base(parent, customComparer)
        {
            this.IgnoreCase = true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterTypeSkeleton"></param>
        /// <returns></returns>
        public TargetSet this[string filterTypeSkeleton]
        {
            get { return this[new TargetSet.Key(filterTypeSkeleton)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterTypeSkeleton"></param>
        /// <returns></returns>
        public bool Contains(string filterTypeSkeleton)
        {
            return Contains(new TargetSet.Key(filterTypeSkeleton));
        }

        // Implemented on derived collection class since it refers to a static method
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return TargetSet.GetObjectFactory();
        }

        // TODO: Check if there is some way to prevent customers from manipulating the collection, if not then you'll have to catch it
        // in validation
        /*
        // External customers cannot add and remove TargetSets. They are automatically constructed
        // by the ObjectSet
        private new void ISfcCollection.Add(TargetSet ts)
        {
            base.Add(ts);
        }

        // External customers cannot add and remove TargetSets. They are automatically constructed
        // by the ObjectSet
        private new bool Remove(TargetSet obj)
        {
            return base.Remove(obj);
        }

        // External customers cannot add and remove TargetSets. They are automatically constructed
        // by the ObjectSet
        private new void Clear()
        {
            base.Clear();
        }
         */
    }

}
