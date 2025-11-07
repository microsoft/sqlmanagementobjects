// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// A collection class that stores items sorted by key
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TParent"></typeparam>
    public abstract class SortedListCollectionBase<TObject, TParent> : SmoCollectionBase<TObject, TParent>, ISortedListCollection
        where TObject : SqlSmoObject
        where TParent : SqlSmoObject
    {
        internal SortedListCollectionBase(TParent parent) : base(parent)
        {
        }
        
        protected void AddImpl(TObject obj)
        {
            if( null == obj )
            {
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException());
            }

            CheckCollectionLock();
            

            // check if the object already exists, and throw a meaningful exception 
            var objLookup = GetObjectByKey(obj.key);
            
            if( null != objLookup )
            {
                throw new SmoException(ExceptionTemplates.CannotAddObject(typeof(TObject).Name, obj.key.ToString()));
            }

            // we can add an object to a collection if it is in Creating state, or if 
            // it is in Pending state and its key has been set 
            if (null == obj.ParentColl)
            {
                obj.SetParentImpl(ParentInstance);
            }

            // if the object is in Pending state we should be throwing
            obj.CheckPendingState();

            ValidateParentObject(obj);

            ImplAddExisting(obj);
        }

        protected override void ImplAddExisting(SqlSmoObject obj) 
        {
            InternalStorage.Add(obj.key, (TObject)obj);
            obj.objectInSpace = false;
            obj.ParentColl = this;
        }
    }

    internal interface ISortedListCollection
    {

    }
    internal class SmoSortedList<T> : SmoInternalStorage<T>
        where T : SqlSmoObject
    {
        private readonly SortedList innerCollection = null;
        internal SmoSortedList(IComparer keyComparer) : base(keyComparer)
        {
            innerCollection = new SortedList(keyComparer);
        }

        internal override bool Contains(ObjectKeyBase key) => innerCollection.Contains(key);

        internal override int LookUp(ObjectKeyBase key) => Contains(key) ? 1 : 0;

        internal override T this[ObjectKeyBase key]
        { 
            get { return innerCollection[key] as T;}
            set { innerCollection[key] = value as T; }
        }

        internal override T GetByIndex(int index) => innerCollection.GetByIndex(index) as T;

        public override int Count => innerCollection.Count;

        internal override void Add(ObjectKeyBase key, T o)
        {
            innerCollection[key] = o;
            o.key.Writable = false;
        }

        internal override void Remove(ObjectKeyBase key) => innerCollection.Remove(key);

        internal override void InsertAt(int position, T o) =>
            // this should never be called
            Diagnostics.TraceHelper.Assert(false);

        internal override void RemoveAt(int position) => innerCollection.RemoveAt(position);

        internal override void Clear() => innerCollection.Clear();

        public override IEnumerator<T> GetEnumerator() => new SmoSortedListEnumerator(innerCollection.GetEnumerator());

        // nested enumerator class
        // we need that to override the behaviour of SortedList
        // that exposes an IDictionaryEnumerator interface
        // TODO: https://github.com/microsoft/sqlmanagementobjects/issues/140
        internal sealed class SmoSortedListEnumerator : IEnumerator<T>
        {
            private readonly IEnumerator baseEnumerator;
            
            internal SmoSortedListEnumerator(IEnumerator enumerator) 
            {
                baseEnumerator = enumerator;
            }

            public T Current => ((DictionaryEntry)baseEnumerator.Current).Value as T;

            object IEnumerator.Current => Current;

            public bool MoveNext() => baseEnumerator.MoveNext();

            public void Reset() => baseEnumerator.Reset();

            void IDisposable.Dispose()
            {
            }
        }
    }
}
