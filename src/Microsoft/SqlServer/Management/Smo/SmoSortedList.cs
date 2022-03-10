// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;

namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class SortedListCollectionBase : SmoCollectionBase
    {
        internal SortedListCollectionBase(SqlSmoObject parent) : base(parent)
        {
        }
        
        protected void AddImpl(SqlSmoObject obj)
        {
            if( null == obj )
            {
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException());
            }

            CheckCollectionLock();
            

            // check if the object already exists, and throw a meaningful exception 
            SqlSmoObject objLookup = GetObjectByKey(obj.key);
            
            if( null != objLookup )
            {
                throw new SmoException(ExceptionTemplates.CannotAddObject(obj.GetType().Name, obj.key.ToString()));
            }

            // we can add an object to a collection if it is in Creating state, or if 
            // it is in Pending state and its key has been set 
            if (null == obj.ParentColl)
            {
                obj.SetParentImpl(this.ParentInstance);
            }

            // if the object is in Pending state we should be throwing
            obj.CheckPendingState();

            ValidateParentObject(obj);

            ImplAddExisting(obj);
        }

        protected override void ImplAddExisting(SqlSmoObject obj) 
        {
            InternalStorage.Add(obj.key, obj);
            obj.objectInSpace = false;
            obj.ParentColl = this;
        }
    }
    
    

    internal class SmoSortedList : SmoInternalStorage
    {
        SortedList innerCollection = null;
        internal SmoSortedList(IComparer keyComparer) : base(keyComparer)
        {
            innerCollection = new SortedList(keyComparer);
        }

        internal override bool Contains(ObjectKeyBase key)
        {
            return innerCollection.Contains(key);
        }
        
        internal override Int32 LookUp(ObjectKeyBase key)
        {
            return this.Contains(key)?1:0;
        }

        internal override SqlSmoObject this[ObjectKeyBase key]
        { 
            get { return innerCollection[key] as SqlSmoObject;}
            set { innerCollection[key] = value as SqlSmoObject; }
        }
        
        internal override SqlSmoObject GetByIndex(Int32 index)
        {
            return innerCollection.GetByIndex(index) as SqlSmoObject;
        }

        public override Int32 Count 
        { 
            get { return innerCollection.Count;}
        }

        internal override void Add(ObjectKeyBase key, SqlSmoObject o)
        {
            innerCollection[key] = o;
            o.key.Writable = false;
        }
        
        internal override void Remove(ObjectKeyBase key)
        {
            innerCollection.Remove(key);
        }
        
        internal override void InsertAt(int position, SqlSmoObject o)
        {
            // this should never be called
            Diagnostics.TraceHelper.Assert(false);
        }
        
        internal override void RemoveAt(int position)
        {
            innerCollection.RemoveAt(position);
        }

        internal override void Clear()
        {
            innerCollection.Clear();
        }

        internal  override bool IsSynchronized 
        {
            get
            {
                return innerCollection.IsSynchronized;
            }
                
        }

        internal override object SyncRoot
        {
            get
            {
                return innerCollection.SyncRoot;
            }
        }

        public override IEnumerator GetEnumerator() 
        {
            return new SmoSortedListEnumerator(innerCollection.GetEnumerator());
        }

        // nested enumerator class
        // we need that to override the behaviour of SortedList
        // that exposes an IDictionaryEnumerator interface
        internal sealed class SmoSortedListEnumerator : IEnumerator 
        {
            private IEnumerator baseEnumerator;
            
            internal SmoSortedListEnumerator(IEnumerator enumerator) 
            {
                this.baseEnumerator = enumerator;
            }

            public object Current 
            {
                get 
                {
                    return ((DictionaryEntry)baseEnumerator.Current).Value;
                }
            }

            public bool MoveNext() 
            {
                return baseEnumerator.MoveNext();
            }
            
            public void Reset() 
            {
                baseEnumerator.Reset();
            }
        }
    }
}
