// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;


namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class ArrayListCollectionBase<TObject, TParent> : SmoCollectionBase<TObject, TParent>
        where TObject : SqlSmoObject
        where TParent : SqlSmoObject
    {
        internal ArrayListCollectionBase(TParent parent) : base(parent)
        {
        }


        private void FixIDs(int startIdx)
        {
            var realID = startIdx;
            for (var i = startIdx; i < Count; i++)
            {
                var propID = InternalStorage.GetByIndex(i).Properties.Get("ID");
                if (!propID.Retrieved || Convert.ToInt32(propID.Value, SmoApplication.DefaultCulture) != 0)
                {
                    propID.SetRetrieved(true);
                    if (propID.Type.Equals(typeof(short)))
                    {
                        propID.SetValue((short)(++realID));
                    }
                    else if (propID.Type.Equals(typeof(byte)))
                    {
                        propID.SetValue((byte)(++realID));
                    }
                    else
                    {
                        propID.SetValue(++realID);
                    }
                }
            }
        }

        protected void AddImpl(TObject obj, int insertAtPosition)
        {
            CheckCollectionLock();

            if (null == obj)
            {
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException());
            }


            // we can add an object to a collection if it is in Creating state, or if 
            // it is in Pending state and its key has been set 
            if (null == obj.ParentColl)
            {
                obj.SetParentImpl(ParentInstance);
            }

            obj.CheckPendingState();
            ValidateParentObject(obj);

            InternalStorage.InsertAt(insertAtPosition, obj);
            obj.objectInSpace = false;
            obj.key.Writable = true;

            // if we can have duplicate names in the collection this means the ID's are
            // coming from the server and we don't need to rearrange them 
            if (!AcceptDuplicateNames)
            {
                FixIDs(insertAtPosition);
            }
        }

        internal void AddImpl(TObject obj, ObjectKeyBase insertAtKey)
        {
            CheckCollectionLock();

            if (null == obj)
            {
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException());
            }

            var pos = InternalStorage.LookUp(insertAtKey);
            if (-1 == pos)
            {
                throw new SmoException(ExceptionTemplates.ColumnBeforeNotExisting(insertAtKey.ToString()));
            }

            AddImpl(obj, pos);
        }

        internal void AddImpl(TObject obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException();
            }
            try
            {
                // Since we can have column objects upto 100k through sparse columns support, look up takes a huge amount of time in case of create time
                // Hence we removed the look up in case of columns. Then engine throws the exception in this case.
                if (!(obj is Column))
                {
                    var pos = InternalStorage.LookUp(obj.key);

                    if (-1 != pos)
                    {
                        throw new SmoException(ExceptionTemplates.CannotAddObject(typeof(TObject).Name, obj.ToString()));
                    }
                }

                AddImpl(obj, InternalStorage.Count);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, e);
            }
        }
    }

    internal class SmoArrayList<TObject, TParent> : SmoInternalStorage<TObject>
        where TObject : SqlSmoObject
        where TParent : SqlSmoObject
    {
        internal readonly List<TObject> innerCollection = new List<TObject>();
        private readonly SmoCollectionBase<TObject, TParent> parent;
        internal SmoArrayList(IComparer keyComparer, SmoCollectionBase<TObject, TParent> parent) : base(keyComparer)
        {
            this.parent = parent;
        }

        internal override bool Contains(ObjectKeyBase key) => LookUp(key) != -1;

        internal override int LookUp(ObjectKeyBase key)
        {
            for (var idx = 0; idx < innerCollection.Count; idx++)
            {
                if (0 == keyComparer.Compare(key, innerCollection[idx].key))
                {
                    return idx;
                }
            }

            return -1;
        }

        internal override TObject this[ObjectKeyBase key]
        {
            get
            {
                var pos = LookUp(key);
                return pos != -1 ? innerCollection[pos] : null;
            }
            set
            {
                var pos = LookUp(key);
                if (pos != -1)
                {
                    innerCollection[pos] = value;
                }
                else
                {
                    innerCollection.Add(value);
                }
            }
        }

        internal override TObject GetByIndex(int index) => innerCollection[index];

        public override int Count => innerCollection.Count;

        internal override void Add(ObjectKeyBase key, TObject o)
        {
            innerCollection.Add(o);
            o.key.Writable = false;
        }

        internal override void Remove(ObjectKeyBase key)
        {
            var pos = LookUp(key);
            if (pos != -1)
            {
                innerCollection[pos].key.Writable = true;
                innerCollection.RemoveAt(pos);
            }
            else
            {
                throw new InternalSmoErrorException(ExceptionTemplates.CouldNotFindKey(key.ToString()));
            }
        }

        internal override void Clear() => innerCollection.Clear();

        internal override void InsertAt(int position, TObject o) => innerCollection.Insert(position, o);

        internal override void RemoveAt(int position) => innerCollection.RemoveAt(position);

        public override IEnumerator<TObject> GetEnumerator() => innerCollection.GetEnumerator();

    }
}

