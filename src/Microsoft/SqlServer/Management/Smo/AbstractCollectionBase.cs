// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Base class for all SMO collections
    /// </summary>
    public abstract class AbstractCollectionBase 
    {
        // the object that holds this collection
        private SqlSmoObject parentInstance;

        internal AbstractCollectionBase(SqlSmoObject parentInstance)
        {
            this.parentInstance = parentInstance;
            initialized = false;
            m_bIsDirty = false;
        }
        
        // we have this so that contained objects can ask for their parent object
        public SqlSmoObject ParentInstance
        {
            get
            {
                return parentInstance;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        internal protected bool initialized;

        internal void MarkOutOfSync()
        {
            initialized = false;
        }

        virtual internal StringComparer StringComparer
        {
            get 
            {
                return parentInstance.StringComparer;
            }
        }

        // we need this abstract method in order to support childred collection 
        // enumeration automatically
        internal protected void AddExisting(SqlSmoObject smoObj)
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
        internal abstract Int32 NoFaultCount { get; }

        bool m_bIsDirty;
        internal bool IsDirty // is collection dirty. ex: a Remove() has been performed
        {
            get	{ return m_bIsDirty; }
            set { m_bIsDirty = value; }
        }

        internal abstract ObjectKeyBase CreateKeyFromUrn(Urn urn);
    }
}

