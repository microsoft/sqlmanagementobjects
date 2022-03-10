// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    ///     This custom implementaion is added 
    ///     as .NET 2.0 does not have a Genertic Set
    ///     TODO: Replace with more efficient implementation later
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class SmoSet<T> 
    {
        private Dictionary<T, object> namesList;

        public SmoSet()
        {
            this.namesList = new Dictionary<T, object>();
        }

        public SmoSet(SmoSet<T> set)
        {
            this.namesList = new Dictionary<T, object>(set.namesList);
        }

        public bool Add(T name)
        {
            bool added = true;
            try
            {
                this.namesList.Add(name, null);
            }
            catch (System.ArgumentException)
            {
                //The name is null or already exists
                added = false;
            }
            return added;
        }

        public bool Remove(T name)
        {
            bool removed = true;
            try
            {
                removed = this.namesList.Remove(name);
            }
            catch (System.ArgumentNullException)
            {
                //The name is null
                removed = false;
            }
            return removed;
        }

        public bool Contains(T name)
        {
            bool contains;
            try
            {
                contains = this.namesList.ContainsKey(name);
            }
            catch (System.ArgumentNullException)
            {
                //The name is null
                contains = false;
            }
            return contains;
        }

        #region IEnumerable Members

        public IEnumerator<T> GetEnumerator()
        {
            return this.namesList.Keys.GetEnumerator();
        }

        #endregion


    }
}
