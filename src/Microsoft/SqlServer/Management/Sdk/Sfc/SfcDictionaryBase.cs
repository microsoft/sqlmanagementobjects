// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    abstract public class SfcDictionaryCollection<T, K, ParentT> : SfcCollection<T, K, ParentT>, IEnumerable<T>
        where T : SfcInstance
        where K : SfcKey
        where ParentT : SfcInstance
    {
        // This generic collection base class uses an internal Dictionary<K, T>
        Dictionary<K, T> m_collection = null;

        // Temporary copy used as shadow to merge/create into when refreshing the entire collection
        Dictionary<K, T> m_shadow = null;

        #region Constructors
        protected SfcDictionaryCollection(ParentT parent) : base(parent)
        {
        }
        #endregion

        #region Public ICollection<T> overrides

        protected override void AddImpl(T obj)
        {
            m_collection.Add(obj.AbstractIdentityKey as K, obj);
        }

        public override void Clear()
        {
            EnsureCollectionInitialized();
            m_collection.Clear();
        }

        public override bool Contains(T obj)
        {
            // TODO: Do we show objects as Contained that are in a Dropped state?
            EnsureCollectionInitialized();
            return Contains(obj.AbstractIdentityKey as K) && obj.State != SfcObjectState.Dropped;
        }
        
        public override void CopyTo(T[] array, int arrayIndex)
        {
            EnsureCollectionInitialized();
            int i=0;
            foreach( T obj in m_collection.Values )
            {
                array.SetValue( obj,i +arrayIndex );
                i++;
            }
        }

        public override int Count
        {
            get
            {
                EnsureCollectionInitialized();
                return m_collection.Count;
            }
        }

        public override bool  IsReadOnly
        {
            get { return false; }
        }

        public override bool Remove(T obj)
        {
            EnsureCollectionInitialized();
            return base.RemoveInternal(obj);
        }

        public override IEnumerator<T> GetEnumerator()
        {
            EnsureCollectionInitialized();
            return m_collection.Values.GetEnumerator();
        }

        #endregion

        #region Public overrides
        public override bool Contains(K key)
        {
            EnsureCollectionInitialized();
            return m_collection.ContainsKey(key);

        }

        public bool TryGetValue(K key, out T obj)
        {
            EnsureCollectionInitialized();
            return m_collection.TryGetValue(key, out obj);
        }
        #endregion

        #region Protected SfcCollection<T, K, ParentT> method overrides

        protected override bool RemoveImpl(T obj)
        {
            return m_collection.Remove(obj.AbstractIdentityKey as K);
        }

        protected override T GetObjectByKey(K key)
        {
            T obj;

            if(m_collection.TryGetValue(key, out obj) )
            {
                return obj;
            }

            return CreateAndInitializeChildObject(key);
        }

        protected override T GetExistingObjectByKey(K key)
        {
            T obj;

            if (m_collection.TryGetValue(key, out obj))
            {
                return obj;
            }

            return null;
        }

        protected override void InitInnerCollection()
        {
            m_collection = new Dictionary<K,T>();
        }

        protected override void PrepareMerge()
        {
            m_shadow = new Dictionary<K, T>();
        }

        protected override bool AddShadow(T obj)
        {
            if (m_shadow != null)
            {
                m_shadow.Add(obj.AbstractIdentityKey as K, obj);
                // The object must already be parented correctly
                TraceHelper.Assert(obj.Parent == this.Parent);
                return true;
            }
            return false;
        }

        protected override void FinishMerge()
        {
            if (m_shadow != null)
            {
                m_collection = m_shadow;
                m_shadow = null;
            }
        }

        #endregion
    }

}

