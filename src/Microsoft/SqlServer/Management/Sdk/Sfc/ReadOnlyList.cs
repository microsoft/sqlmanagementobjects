// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// A generic struct that exposes the read-only interface of a list while hides
    /// its mutable interface.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public struct ReadOnlyList<T> : IEnumerable<T>, IReadOnlyList<T>
    {
        private readonly IList<T> list;

        /// <summary>
        /// Initializes a new instance of <see cref="ReadOnlyList{T}"/> that wraps the
        /// specified <see cref="IList{T}"/> in a read-only interface.
        /// </summary>
        /// <param name="list">A <see cref="IList{T}"/> object to be wrapped in read-only interface</param>
        /// <remarks>If <paramref name="list"/> is <c>null</c>, it is treated as an empty list.</remarks>
        public ReadOnlyList(IList<T> list)
        {
            this.list = list;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ReadOnlyList{T}"/>.
        /// </summary>
        public int Count
        {
            get { return this.list != null ? this.list.Count : 0; }
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                if (this.list == null)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return this.list[index];
            }
        }

        /// <summary>
        /// Determines whether the <see cref="ReadOnlyList{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns><c>true</c> if item is found in the list; otherwise, <c>false</c>.</returns>
        public bool Contains(T item)
        {
            return (this.list != null) && this.list.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="ReadOnlyList{T}"/> to an <see cref="Array"/>, 
        /// starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="ReadOnlyList{T}"/>.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (this.list != null)
            {
                this.list.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="ReadOnlyList{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="ReadOnlyList{T}"/>.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            return (this.list != null) ? this.list.IndexOf(item) : -1;
        }

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the list.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return (this.list != null) ?
                this.list.GetEnumerator() :
                Enumerable.Empty;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the list.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region Implicit Conversions

        /// <summary>
        /// Implicit conversion from <see cref="Array"/> to <see cref="ReadOnlyList{t}"/>.
        /// </summary>
        /// <param name="array">The <see cref="Array"/> object to convert.</param>
        /// <returns>A <see cref="ReadOnlyList{T}"/> that wraps the specified array.</returns>
        public static implicit operator ReadOnlyList<T>(T[] array)
        {
            return new ReadOnlyList<T>(array);
        }

        /// <summary>
        /// Implicit conversion from <see cref="List{T}"/> to <see cref="ReadOnlyList{t}"/>.
        /// </summary>
        /// <param name="list">The <see cref="List{T}"/> object to convert.</param>
        /// <returns>A <see cref="ReadOnlyList{T}"/> that wraps the specified list.</returns>
        public static implicit operator ReadOnlyList<T>(List<T> list)
        {
            return new ReadOnlyList<T>(list);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// An empty enumerable struct that provides an empty enumerator.
        /// </summary>
        private struct Enumerable
        {
            public static IEnumerator<T> Empty
            {
                get { yield break; }
            }
        }

        #endregion
    }
}
