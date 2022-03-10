// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    internal class EnumeratorContainer: IEnumerator<string>
    {
        #region ctor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// 
        internal EnumeratorContainer()
        {
        }
        #endregion

        #region internal methods

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
            if (stringCollection == null)
            {
                throw new ArgumentNullException("stringCollection");
            }

            this.listOfObjects.Add(stringCollection);
        }
        #endregion

        #region IEnumerator<string> Members

        /// <summary>
        /// Current implementation for IEnumerator<string> interface
        /// </summary>
        public string Current
        {
            get
            {
                if (this.state == EnumeratorState.NotStarted)
                {
                    // This string does not need to be localized
                    //
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                }
                else if (this.state == EnumeratorState.Finished)
                {
                    throw new InvalidOperationException("Enumeration already finished.");
                }

                Debug.Assert(this.currentEnumerator != null);

                return (string)this.currentEnumerator.Current;
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used by this method
        /// </summary>
        public void Dispose()
        {
            // nothing to dispose. Not changing the state of Enumerator
            // This behavior is inline with how string[] does it
            //
        }

        #endregion

        #region IEnumerator Members

        /// <summary>
        /// Current implementation for IEnumerator interface
        /// </summary>
        object System.Collections.IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        /// <summary>
        /// Moves the enumerator to the next element
        /// </summary>
        /// <returns>
        /// True if there is another value, else false
        /// </returns>
        /// 
        public bool MoveNext()
        {
            if (this.state == EnumeratorState.NotStarted)
            {
                this.state = EnumeratorState.InProgress;
            }

            // If the current enumertor is null or if it is over
            // then we need to create a new enumerator from the next element
            // Need to do this in a while loop to ensure that we skip over any
            // empty objects present in the list
            //
            while (this.currentEnumerator == null ||
                !this.currentEnumerator.MoveNext())
            {
                this.indexOfObject++;

                // If the next index is equal to the length of the 
                // objects in the list, it means that we are done
                //
                if (this.indexOfObject == this.listOfObjects.Count)
                {
                    this.state = EnumeratorState.Finished;
                    return false;
                }

                // If we are not done then get the next enumerator
                //
                this.currentEnumerator = this.listOfObjects[this.indexOfObject].GetEnumerator();
            }

            return true;
        }

        /// <summary>
        /// Resets the state
        /// </summary>
        /// 
        public void Reset()
        {
            this.indexOfObject = -1;
            this.currentEnumerator = null;
            this.state = EnumeratorState.NotStarted;
        }

        #endregion

        #region Fields

        private List<IEnumerable> listOfObjects = new List<IEnumerable>();
        private IEnumerator currentEnumerator = null;
        private int indexOfObject = -1;
        private EnumeratorState state = EnumeratorState.NotStarted;

        #endregion

        /// <summary>
        /// Enum used to track the state of the object
        /// </summary>
        private enum EnumeratorState
        {
            NotStarted,
            InProgress,
            Finished
        }
    }
}
