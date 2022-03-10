// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// A generic struct that only exposes the read-only interface of a dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    struct ReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dictionary;

        /// <summary>
        /// Initializes a new instance of <see cref="ReadOnlyDictionary{TKey, TValue}"/> that wraps the
        /// specified <see cref="IDictionary{TKey, TValue}"/> in a read-only interface.
        /// </summary>
        /// <param name="dictionary">A <see cref="IDictionary{TKey, TValue}"/> object to be wrapped in read-only interface</param>
        /// <remarks>If <paramref name="dictionary"/> is <c>null</c>, it is treated as an empty dictionary.</remarks>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        /// <summary>
        /// Gets the number of elements contained in the dictionary.
        /// </summary>
        public int Count
        {
            get { return this.dictionary != null ? this.dictionary.Count : 0; }
        }

        /// <summary>
        /// Gets the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <returns>The element with the specified key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                if (this.dictionary == null)
                {
                    if (key == null)
                    {
                        throw new ArgumentNullException("key");
                    }

                    throw new KeyNotFoundException();
                }

                return this.dictionary[key];
            }
        }

        /// <summary>
        /// Gets an IEnumerable{TValue} containing the keys of the dictionary.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                if (this.dictionary != null)
                {
                    foreach (TKey key in this.dictionary.Keys)
                    {
                        yield return key;
                    }
                }
            }
        }

        /// <summary>
        /// Gets an IEnumerable{TValue} containing the values of the dictionary.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                if (this.dictionary != null)
                {
                    foreach (TValue value in this.dictionary.Values)
                    {
                        yield return value;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the dictionary.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is found; otherwise, <c>false</c>.</returns>
        public bool Contains(TValue item)
        {
            return (this.dictionary != null) && this.dictionary.Values.Contains(item);
        }

        /// <summary>
        /// Determines whether the dictionary contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(TKey key)
        {
            return (this.dictionary != null) && this.dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; 
        /// otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (this.dictionary != null)
            {
                return this.dictionary.TryGetValue(key, out value);
            }

            value = default(TValue);
            return false;
        }

        #region IReadOnlyCollection<TValue> Members

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            this.dictionary.Values.CopyTo(array, arrayIndex);
        }

        #endregion

        #region IEnumerable<TValue> Members

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return this.dictionary.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the list of key/value pairs.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return (this.dictionary != null) ?
                this.dictionary.GetEnumerator() :
                Enumerable.Empty;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the list of values in the dictionary.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the list of values.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region Implicit Conversions

        /// <summary>
        /// Implicit conversion from <see cref="Dictionary{TKey, TValue}"/> to 
        /// <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="Dictionary{TKey, TValue}"/> object to convert.</param>
        /// <returns>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> that wraps the specified dictionary.</returns>
        public static implicit operator ReadOnlyDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            return new ReadOnlyDictionary<TKey, TValue>(dictionary);
        }

        /// <summary>
        /// Implicit conversion from <see cref="SortedList{TKey, TValue}"/> to 
        /// <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="sortedList">The <see cref="SortedList{TKey, TValue}"/> object to convert.</param>
        /// <returns>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> that wraps the specified sorted list.</returns>
        public static implicit operator ReadOnlyDictionary<TKey, TValue>(SortedList<TKey, TValue> sortedList)
        {
            return new ReadOnlyDictionary<TKey, TValue>(sortedList);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// An empty enumerable struct that provides an empty enumerator.
        /// </summary>
        private struct Enumerable
        {
            public static IEnumerator<KeyValuePair<TKey, TValue>> Empty
            {
                get { yield break; }
            }
        }

        #endregion

    }
}
