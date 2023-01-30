// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// The Sfc collection base for all domain collections. 
    /// It abstracts all the necesssary handshaking between the parent object, and the collection or element objects.
    /// </summary>
    /// <typeparam name="S">The type of the collection owner.</typeparam>
    /// <typeparam name="T">The element type of the target collection.</typeparam>
    /// <typeparam name="K">The key type of the target collection.</typeparam>
    abstract public class SfcReferenceCollection<K, T, S> : IReadOnlyDictionary<K, T>, IListSource
        where K : IComparable<K>
        where T : SfcInstance
        where S : SfcInstance
    {
        private IComparer<K> comparer;
        private SortedDictionary<K, T> dict;
        private ISfcReferenceCollectionResolver<T, S> resolver;
        private S owner;

        /// <summary>
        /// Create a collection of instance references.
        /// </summary>
        /// <param name="owner">The parent of the collection.</param>
        /// <param name="resolver">The resolver used to determine the target references for collection initialization or refresh.</param>
        /// <param name="comparer">The key comparer for the collection.</param>
        protected SfcReferenceCollection(S owner, ISfcReferenceCollectionResolver<T, S> resolver, IComparer<K> comparer)
        {
            this.owner = owner;
            this.resolver = resolver;
            this.comparer = comparer;
            Reset();
        }

        public void Refresh()
        {
            Reset();
        }

        private void Reset()
        {
            // Renew the inner collection storage and resolve to fill it
            if (this.comparer == null)
            {
                this.dict = new SortedDictionary<K, T>(this.comparer);
            }
            else
            {
                this.dict = new SortedDictionary<K, T>();
            }

            // Currently we never have any params to pass (rely on the object instance to reach anything we need for context)
            foreach (T reference in this.resolver.ResolveCollection(this.owner, null))
            {
                // The derived class knows what K is and how it relates to T, since a referential collection could have
                // just about any scope it wants in terms of unique keying.
                this.dict.Add(this.GetKeyFromValue(reference), reference);
            }
        }

        /// <summary>
        /// Derived types implement this to obtain a key from the item value.
        /// </summary>
        /// <param name="value">The current item to use to obtain or calculate its key.</param>
        /// <returns>The key.</returns>
        protected abstract K GetKeyFromValue(T value);

        #region IListSource support

        bool IListSource.ContainsListCollection
        {
            get { return false; }
        }

        IList IListSource.GetList()
        {
            return this.dict.Values.ToList();
        }

        #endregion


        #region IReadOnlyDictionary<K,T> Members

        public bool ContainsKey(K key)
        {
            return this.dict.ContainsKey(key);
        }

        public bool TryGetValue(K key, out T value)
        {
            return this.dict.TryGetValue(key, out value);
        }

        public T this[K key]
        {
            get { return this.dict[key]; }
        }

        public IEnumerable<K> Keys
        {
            get { return this.dict.Keys; }
        }

        public IEnumerable<T> Values
        {
            get { return this.dict.Values; }
        }

        #endregion

        #region IReadOnlyCollection<T> Members

        public bool Contains(T item)
        {
            return this.dict.ContainsValue(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.dict.Values.CopyTo(array, arrayIndex);
        }

        #endregion

        #region IReadOnlyCollection Members

        public int Count
        {
            get { return this.dict.Count; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return this.dict.GetEnumerator();
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)this.dict).GetEnumerator();
        }

        #endregion
    }
}
