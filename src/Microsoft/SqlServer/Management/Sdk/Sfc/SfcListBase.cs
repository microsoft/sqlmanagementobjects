// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    abstract public class SfcListCollection<T, K, ParentT> : SfcCollection<T, K, ParentT>, IEnumerable<T>
        where T : SfcInstance
        where K : SfcKey
        where ParentT : SfcInstance
    {
        // This generic collection base class uses an internal List<T>
        List<T> m_list = null;

        // Temporary copy used as shadow to merge/create into when refreshing the entire collection
        List<T> m_shadow = null;

        #region Constructors
        protected SfcListCollection(ParentT parent) : base(parent)
        {
        }
        #endregion

        #region Public ICollection<T> overrides

        protected override void AddImpl(T obj)
        {
            m_list.Add(obj);            
        }

        public override void Clear()
        {
            EnsureCollectionInitialized();
            m_list.Clear();
        }

        public override bool Contains(T obj)
        {
            // TODO: Do we show objects as Contained that are in a Dropped state?
            EnsureCollectionInitialized();
            // Linear search
            int i = m_list.IndexOf(obj);
            return i != -1 && obj.State != SfcObjectState.Dropped;
        }
        
        public override void CopyTo(T[] array, int arrayIndex)
        {
            EnsureCollectionInitialized();
            for (int i = 0; i < Count; i++)
            {
                array.SetValue(m_list[i], i + arrayIndex);
            }
        }

        public override int Count
        {
            get
            {
                EnsureCollectionInitialized();
                return m_list.Count;
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
            return m_list.GetEnumerator();
        }

        #endregion

        // This returns the inner collection without retrieving any objects
        // This is an implementation method and is intentionally non-virtual
        protected ICollection<T> GetInternalCollectionImpl()
        {
            return m_list;
        }

        #region Public overrides
        public override bool Contains(K key)
        {
            EnsureCollectionInitialized();

            // Linear search
            foreach (T loopObj in m_list)
            {
                K loopKey = loopObj.AbstractIdentityKey as K;
                if (loopKey.Equals(key))
                {
                    TraceHelper.Assert(loopObj.State != SfcObjectState.Dropped); // drop should remove objects from collections, so this should never happen
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Protected SfcCollection<T, K, ParentT> method overrides

        protected override bool RemoveImpl(T obj)
        {
            return m_list.Remove(obj);
        }

        protected override T GetObjectByKey(K key)
        {
            T obj = GetExistingObjectByKey(key);

            if (obj != null)
            {
                return obj;
            }

            return CreateAndInitializeChildObject(key);
        }

        protected override T GetExistingObjectByKey(K key)
        {
            // Linear search
            foreach (T loopObj in m_list)
            {
                K loopKey = loopObj.AbstractIdentityKey as K;
                if (loopKey.Equals(key) && loopObj.State != SfcObjectState.Dropped)
                {
                    return loopObj;
                }
            }

            return null;
        }

        protected override void InitInnerCollection()
        {
            m_list = new List<T>();
        }

        protected override void PrepareMerge()
        {
            m_shadow = new List<T>();
        }

        protected override bool AddShadow(T obj)
        {
            if (m_shadow != null)
            {
                m_shadow.Add(obj);
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
                m_list = m_shadow;
                m_shadow = null;
            }
        }

        #endregion
    }

}


