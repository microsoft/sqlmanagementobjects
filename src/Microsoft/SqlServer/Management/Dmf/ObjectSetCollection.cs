// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for ObjectSets.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ObjectSetCollection : SfcDictionaryCollection<ObjectSet, ObjectSet.Key, PolicyStore>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public ObjectSetCollection(PolicyStore parent) : base(parent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ObjectSet this[string name]
        {
            get { return this[new ObjectSet.Key(name)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return Contains(new ObjectSet.Key(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return ObjectSet.GetObjectFactory();
        }

    }

}
