// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;


//
// Readonly collection interfaces
//
// .NET should have implemented their own collection interface bases this way via composition 
// of the IReadOnly... interfaces into larger IReadWrite... interfaces.
//
// The IReadWriteXxx interfaces are the same as the corresponding mutable .NET IXxx containers.
// The prefix avoids confusion with those.
//

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    #region Read-only collection interfaces

    // Note: IReadOnlyCollection<T> is general use, whereas
    // IReadOnlyCollection and IReadOnlyCollection<P,T> are derived mainly for MetadataStore use.

    /// <summary>
    /// The readonly collection minimal base interface.
    /// </summary>
    public interface IReadOnlyCollection : IEnumerable
    {
        /// <summary>
        /// Gets the number of items contained in the <see cref="IReadOnlyCollection"/>.
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// An interface to an immutable collection.
    /// </summary>
    /// <typeparam name="T">The item type of the collection.</typeparam>
    public interface IReadOnlyCollection<T> : IReadOnlyCollection, IEnumerable<T>
    {
        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>True if the item is found in the collection; otherwise false.</returns>
        bool Contains(T item);

        /// <summary>
        /// Copies the elements of the collection to an
        ///  array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements
        /// copied from the collection. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        void CopyTo(T[] array, int arrayIndex);
    }

    /// <summary>
    /// Represents a strongly typed list of objects that can be accessed by index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public interface IReadOnlyList<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        T this[int index] { get; }
    }

    /// <summary>
    /// Represents a generic read-only collection of key/value pairs.
    /// </summary>
    /// <typeparam name="K">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="T">The type of values in the dictionary.</typeparam>
    public interface IReadOnlyDictionary<K, T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// Determines whether the <see cref="IReadOnlyDictionary{K, T}"/> contains an 
        /// element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in this <see cref="IReadOnlyDictionary{K, T}"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="IReadOnlyDictionary{K, T}"/> contains an element 
        /// with the key; otherwise, <c>false</c>.
        /// </returns>
        bool ContainsKey(K key);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if 
        /// the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <c>true</c> if the object that implements <see cref="IReadOnlyDictionary{K, T}"/>
        /// contains an element with the specified key; otherwise, <c>false</c>.
        /// </returns>
        bool TryGetValue(K key, out T value);

        /// <summary>
        /// Gets the value with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key.</returns>
        T this[K key] { get; }

        /// <summary>
        /// Gets an <see cref="IEnumerable{K}"/> containing the keys of the 
        /// <see cref="IReadOnlyDictionary{K, T}"/>.
        /// </summary>
        IEnumerable<K> Keys { get; }

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> containing the values of the 
        /// <see cref="IReadOnlyDictionary{K, T}"/>.
        /// </summary>
        IEnumerable<T> Values { get; }
    }

    #endregion

    #region Read-only set interfaces

    /// <summary>
    /// An interface to an immutable set.
    /// </summary>
    public interface IReadOnlySet : IReadOnlyCollection
    {
        /// <summary>
        /// Check if this set is a subset of other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a subset of other; otherwise false.</returns>
        bool IsSubsetOf(IEnumerable other);
        /// <summary>
        /// Check if this set is a superset of other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a superset of other; otherwise false.</returns>
        bool IsSupersetOf(IEnumerable other);
        /// <summary>
        /// Check if this set is a subset of other, but not the same as it.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a proper subset of other; otherwise false.</returns>
        bool IsProperSubsetOf(IEnumerable other);
        /// <summary>
        /// Check if this set is a superset of other, but not the same as it.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a proper subperset of other; otherwise false.</returns>
        bool IsProperSupersetOf(IEnumerable other);
        /// <summary>
        /// Check if this set has any elements in common with other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set has any elements in common with other; otherwise false.</returns>
        bool Overlaps(IEnumerable other);
        /// <summary>
        /// Check if this set contains the same and only the same elements as other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set contains the same and only the same elements as other; otherwise false.</returns>
        bool SetEquals(IEnumerable other);
    }

    /// <summary>
    /// An interface to an immutable set.
    /// </summary>
    /// <typeparam name="T">The element type of the set.</typeparam>
    public interface IReadOnlySet<T> : IReadOnlySet, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Check if this set is a subset of other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a subset of other; otherwise false.</returns>
        bool IsSubsetOf(IEnumerable<T> other);
        /// <summary>
        /// Check if this set is a superset of other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a superset of other; otherwise false.</returns>
        bool IsSupersetOf(IEnumerable<T> other);
        /// <summary>
        /// Check if this set is a subset of other, but not the same as it.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a proper subset of other; otherwise false.</returns>
        bool IsProperSubsetOf(IEnumerable<T> other);
        /// <summary>
        /// Check if this set is a superset of other, but not the same as it.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set is a proper subperset of other; otherwise false.</returns>
        bool IsProperSupersetOf(IEnumerable<T> other);
        /// <summary>
        /// Check if this set has any elements in common with other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set has any elements in common with other; otherwise false.</returns>
        bool Overlaps(IEnumerable<T> other);
        /// <summary>
        /// Check if this set contains the same and only the same elements as other.
        /// </summary>
        /// <param name="other">The sequence to check against.</param>
        /// <returns>True if this set contains the same and only the same elements as other; otherwise false.</returns>
        bool SetEquals(IEnumerable<T> other);
    }

    #endregion
}
