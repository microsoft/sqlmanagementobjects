// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Base class for collections whose enumeration order is based on ID instead of Name
    /// </summary>
    public abstract class ParameterCollectionBase<TObject, TParent> : ArrayListCollectionBase<TObject, TParent>, IOrderedCollection, IOrderedCollectionInternal
         where TObject : SqlSmoObject
         where TParent : SqlSmoObject
    {
        internal ParameterCollectionBase(SqlSmoObject parent) : base((TParent)parent)
        {
        }

        protected override void InitInnerCollection() => InternalStorage = new SmoArrayList<TObject, TParent>(new SimpleObjectComparer(StringComparer), this);

        /// <summary>
        /// Returns the object of the given name, or null if it's not in the collection
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TObject this[string name] => GetObjectByKey(new SimpleObjectKey(name)) as TObject;

        /// <summary>
        /// Returns the Parent of the collection
        /// </summary>
        public TParent Parent => ParentInstance as TParent;

        /// <summary>
        /// Returns whether the named object exists in the collection
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name) => Contains(new SimpleObjectKey(name));

        /// <summary>
        /// Adds an object to the collection
        /// </summary>
        /// <param name="obj"></param>
        public void Add(TObject obj) => AddImpl(obj);

        /// <summary>
        /// Adds an object to the collection after the object with a specific name
        /// </summary>
        /// <param name="obj">The column to add</param>
        /// <param name="insertAtObjectName">The name of the column already in the collection after which the column will be inserted</param>
        public void Add(TObject obj, string insertAtObjectName) => AddImpl(obj, new SimpleObjectKey(insertAtObjectName));

        /// <summary>
        /// Adds an object to the collection at particular position
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="insertAtPosition"></param>
        public void Add(TObject obj, int insertAtPosition) => AddImpl(obj, insertAtPosition);

        /// <summary>
        /// Removes an object from the collection
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="FailedOperationException"></exception>
        public void Remove(TObject obj)
        {
            if (null == obj)
            {
                throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException(nameof(obj)));
            }

            RemoveObj(obj, obj.key);
        }

        /// <summary>
        /// Attempts to remove the object with the given name from the collection.
        /// If the object is not in Creating state, a SmoException is thrown
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name)
        {
            CheckCollectionLock();
            Remove(new SimpleObjectKey(name));
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            var name = urn.GetAttribute("Name");
            if (string.IsNullOrEmpty(name))
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            return new SimpleObjectKey(name);
        }

        /// <summary>
        /// Inserts the object into the collection. Because the key is the name, we need 
        /// to insert in the order of ID
        /// </summary>
        /// <param name="obj"></param>
        protected override void ImplAddExisting(SqlSmoObject obj)
        {
            Debug.Assert(obj.Properties.Contains("ID"));

            // use the most generic version of the GetPropValueOptional because 
            // ID is int in most cases, but it's byte for some objects (IndexedColumn)
            var objId = Convert.ToInt32(obj.GetPropValueOptional<object>("ID", -1),
                                           SmoApplication.DefaultCulture);

            for (var i = 0; i < InternalStorage.Count; i++)
            {
                var currObj = InternalStorage.GetByIndex(i);
                Debug.Assert(currObj.Properties.Contains("ID"));

                var currObjId = Convert.ToInt32(currObj.GetPropValueOptional<object>("ID", -1),
                                                SmoApplication.DefaultCulture);
                if (-1 != currObjId)
                {
                    if (objId < currObjId)
                    {
                        InternalStorage.InsertAt(i, (TObject)obj);
                        return;
                    }
                }
            }

            // if we could not find a position then insert it at the end
            InternalStorage.InsertAt(InternalStorage.Count, (TObject)obj);
        }

        void IOrderedCollectionInternal.Append(SqlSmoObject obj)
        {
            InternalStorage.InsertAt(InternalStorage.Count, (TObject)obj);
        }
    }

    internal interface IOrderedCollectionInternal
    {
        void Append(SqlSmoObject obj);
    }
}

