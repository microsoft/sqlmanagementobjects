// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    internal class EnumerableContainer: IEnumerable<string>
    {

        #region internal methods

        internal void Insert(int index, StringCollection stringCollection)
        {
            if (stringCollection == null)
            {
                throw new ArgumentNullException("stringCollection");
            }

            this.listOfObjects.Insert(index, stringCollection);
        }

        /// <Summar>
        /// Removes all elements from the list
        /// </Summary>
        internal void Clear()
        {
            this.listOfObjects.Clear();
        }

        /// <summary>
        /// Adds the passed IEnumerable object to the list of objects being maintained
        /// by this class. The IEnumerable is added to the end of the list
        /// </summary>
        /// <param name="stringEnumerable">
        /// The IEnumerable to add
        /// </param>
        /// 
        internal void Add(IEnumerable<string> stringEnumerable)
        {
            if (stringEnumerable == null)
            {
                throw new ArgumentNullException("stringEnumerable");
            }

            this.listOfObjects.Add(stringEnumerable);
        }

        /// <summary>
        /// Adds the passed StringCollection object to the list of objects.
        /// The stringCollection is added to the end of the list.
        /// </summary>
        /// <param name="stringCollection"></param>
        /// 
        internal void Add(StringCollection stringCollection)
        {
            Insert(this.listOfObjects.Count, stringCollection);
        }

        /// <summary>
        /// Adds the string in the passed IEnumerable to a stringCollection
        /// and returns it.
        /// </summary>
        internal static StringCollection IEnumerableToStringCollection(IEnumerable<string> enumerable)
        {
            StringCollection stringCollection = new StringCollection();
            foreach (string str in enumerable)
            {
                stringCollection.Add(str);
            }
            return stringCollection;
        }
        
        #endregion

        #region IEnumerable<string> Members

        /// <summary>
        /// Returns an IEnumerataor<string> object
        /// </summary>
        public IEnumerator<string> GetEnumerator()
        {
            EnumeratorContainer enumerator = new EnumeratorContainer();
            foreach (IEnumerable element in this.listOfObjects)
            {
                StringCollection stringCollection = element as StringCollection;
                if (stringCollection != null)
                {
                    enumerator.Add(stringCollection);
                }
                else
                {
                    enumerator.Add((IEnumerable<string>)element);
                }
            }

            return enumerator;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an IEnumerataor object
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Fields

        private List<IEnumerable> listOfObjects = new List<IEnumerable>();

        #endregion
    }
}
