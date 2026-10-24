// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    abstract public class SfcCollatedDictionaryCollection<T, K, ParentT> : SfcCollection<T, K, ParentT>, IComparer<K>, IEnumerable<T>
        where T : SfcInstance
        where K : SfcKey
        where ParentT : SfcInstance
    {
        // This generic collection base class uses an internal Dictionary<K, T>
        SortedDictionary<K, T> m_collection = null;

        // Temporary copy used as shadow to merge/create into when refreshing the entire collection
        SortedDictionary<K, T> m_shadow = null;

        // Internal dirty state flag if anything affecting collation changes, necessitating an internal SortedDictionary reset
        bool m_dirty = false;

        // The culture to use for sorting
        CultureInfo m_cultureInfo = CultureInfo.InvariantCulture;

        // Ignore case
        bool m_ignoreCase = false;

        // Ascending order
        bool m_ascending = true;

        IComparer<string> comparer = null;

        #region Constructors
        protected SfcCollatedDictionaryCollection(ParentT parent) : this(parent, null)
        {
        }

        protected SfcCollatedDictionaryCollection(ParentT parent, IComparer<string> customComparer)
            : base(parent)
        {
            comparer = customComparer;
        }
        #endregion

        #region Protected methods
        /// <summary>
        /// Sorting the collection will use this .NET culture info.
        /// </summary>
        protected CultureInfo CultureInfo
        {
            get { return m_cultureInfo; }
            set
            {
                if (m_cultureInfo != value)
                {
                    m_dirty = true;
                }
                m_cultureInfo = value;
            }
        }

        /// <summary>
        /// Sort in a case-insensitive manner for the current collection culture.
        /// </summary>
        protected bool IgnoreCase
        {
            get { return m_ignoreCase; }
            set
            {
                if (m_ignoreCase != value)
                {
                    m_dirty = true;
                }
                m_ignoreCase = value;
            }
        }

        /// <summary>
        /// Call this if any of the CultureInfo, IgnoreCase, or Ascending properties change after Initialzation.
        /// The collection is reset to honor the new settings.
        /// TODO: The user is responsible for calling this after changing whatever properties they desire, it should be automated.
        /// </summary>
        protected void ResetInnerCollection()
        {
            if (m_dirty)
            {
                m_dirty = false;

                if (m_collection != null)
                {
                    // We can already use the internal Comparer again for the new settings since the old collection no longer needs them
                    // just to act as the copy source (I think :). If it does, we may need a temporary Comparer.
                    SortedDictionary<K, T> newColl = new SortedDictionary<K, T>(m_collection, (IComparer<K>)this);
                    m_collection.Clear();
                    m_collection = newColl;
                }
            }
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Sort in an ascending order manner for the current collection culture.
        /// </summary>
        public bool Ascending
        {
            get { return m_ascending; }
            set
            {
                if (m_ascending != value)
                {
                    m_dirty = true;
                }
                m_ascending = value;
            }
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

            if (m_collection.TryGetValue(key, out obj))
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
            m_collection = new SortedDictionary<K,T>((IComparer<K>)this);
            m_dirty = false;
        }

        protected override void PrepareMerge()
        {
            m_shadow = new SortedDictionary<K, T>((IComparer<K>)this);
            m_dirty = false;
        }

        protected override bool AddShadow(T obj)
        {
            if (m_shadow != null)
            {
                m_shadow.Add(obj.AbstractIdentityKey as K, obj);
                // The object must already be parented correctly
                Debug.Assert(obj.Parent == this.Parent);
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

        #region IComparer<K> Members
        /// <summary>
        /// Compare keys based on our current CultureInfo and IgnoreCase properties
        /// unless a custom comparer has been passed in.
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        int  IComparer<K>.Compare(K key1, K key2)
        {
            if (comparer == null)
            {
                // TODO: We have to either have Keys provide a dedicated GetComparerString(), or make them compare based on our Comparer passed to them.
                // Currently in v1, TOString() is usually the Urn fragment which is not good or efficient.
                return key1.ToString().Compare(key2.ToString(), m_ignoreCase, m_cultureInfo);
            }
            else // Use custom comparer
            {
                return comparer.Compare(key1.ToString(), key2.ToString());
            }
        }
        #endregion

    }

}

