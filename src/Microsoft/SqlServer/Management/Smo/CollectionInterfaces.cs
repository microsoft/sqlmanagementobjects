// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Base interface for SMO collections
    /// </summary>
    public interface ISmoCollection : System.Collections.ICollection
    {
        /// <summary>
        /// Refreshes the content of the collection without refreshing existing objects in the collection
        /// </summary>
        void Refresh();

        /// <summary>
        /// Refreshes the contents of the collection and refreshes the properties of existing objects in the collection
        /// </summary>
        /// <param name="refreshChildObjects"></param>
        void Refresh(bool refreshChildObjects);

        /// <summary>
        /// Clears old objects and initializes the collection. Unlike Refresh(), any objects already listed in the collection will be replaced with new versions.
        /// Use this method to assure all the objects in the collection have the complete set of properties you want. 
        /// </summary>
        /// <param name="filterQuery">the xpath to filter the objects by properties 
        /// (e.g. setting the filter to [(@IsSystemObject = 0)] will exclude the system objects from the result. 
        /// By setting the parameter to null or empty string, no filter will be applied to the result</param>        
        /// <param name="extraFields">the list of fields to be loaded in each object. 
        /// (e.g. setting the extraFields to "new string[] { "IsSystemVersioned" })" when calling this method for TableCollection 
        /// will include "IsSystemVersioned" property for each table object. 
        /// By setting the parameter to null or empty array, only the default fields will be included in the result</param>
        void ClearAndInitialize(string filterQuery, IEnumerable<string> extraFields);
        
        /// <summary>
        /// Empties the collection but doesn't attempt to retrieve any data
        /// </summary>
        void ResetCollection();
    }

    /// <summary>
    /// Base interface for SMO collections of named objects
    /// </summary>
    public interface INamedObjectCollection : ISmoCollection
    {
        /// <summary>
        /// Returns whether the collection contains an object with the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Contains(string name);
    }

    /// <summary>
    /// Interface with methods that describe characteristics of schema objects in a collection
    /// </summary>
    public interface ISchemaObjectCollection : INamedObjectCollection
    {
        /// <summary>
        /// Returns the default schema of the database associated with this collection
        /// </summary>
        /// <returns></returns>
        string GetDefaultSchema();
        /// <summary>
        /// Returns whether the collection contains an object with the given name and schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        bool Contains(string name, string schema);
    }

    /// <summary>
    /// Implemented by collections whose sort order is by ID
    /// </summary>
    public interface IOrderedCollection : INamedObjectCollection
    {
    }

    /// <summary>
    /// Provides methods to lock and unlock the contents of a collection
    /// </summary>
    public interface ILockableCollection
    {
        void LockCollection(string lockReason);

        void UnlockCollection();

        bool IsCollectionLocked { get; }

        void CheckCollectionLock();

    }
}

