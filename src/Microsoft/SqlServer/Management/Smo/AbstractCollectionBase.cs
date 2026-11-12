// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Base class for all SMO collections
    /// </summary>
    public abstract class AbstractCollectionBase
    {
        internal AbstractCollectionBase(SqlSmoObject parentInstance)
        {
            ParentInstance = parentInstance;
            initialized = false;
            IsDirty = false;
        }

        /// <summary>
        /// Returns the parent of the collection instance
        /// </summary>
        public SqlSmoObject ParentInstance { get; private set; }

        protected internal bool initialized;

        internal void MarkOutOfSync()
        {
            initialized = false;
        }

        internal virtual StringComparer StringComparer
        {
            get 
            {
                return ParentInstance.StringComparer;
            }
        }

        // we need this abstract method in order to support children collection 
        // enumeration automatically
        protected internal void AddExisting(SqlSmoObject smoObj)
        {
            ImplAddExisting(smoObj);
        }

        protected abstract void ImplAddExisting(SqlSmoObject smoObj);

        internal void RemoveObject(ObjectKeyBase key)
        {
            ImplRemove(key);
        }
        internal abstract void ImplRemove(ObjectKeyBase key);

        internal abstract SqlSmoObject NoFaultLookup(ObjectKeyBase key);
        internal abstract int NoFaultCount { get; }
        internal bool IsDirty // is collection dirty. ex: a Remove() has been performed
        { get; set; }

        internal abstract ObjectKeyBase CreateKeyFromUrn(Urn urn);
    }
}

