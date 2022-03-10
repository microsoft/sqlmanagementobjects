// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class ArrayListCollectionBase : SmoCollectionBase
	{
		internal ArrayListCollectionBase(SqlSmoObject parent) : base(parent)
		{
		}
		
		internal ArrayList InternalList 
		{
			get { return ((SmoArrayList)InternalStorage).innerCollection;}
		}
		
		private void FixIDs(int startIdx)
		{
			int realID = startIdx;
			for(int i = startIdx; i < InternalList.Count; i++)
			{
				Property propID = ((SqlSmoObject)InternalList[i]).Properties.Get("ID");
				if( !propID.Retrieved || Convert.ToInt32(propID.Value, SmoApplication.DefaultCulture) != 0 )
				{
					propID.SetRetrieved(true);
					if( propID.Type.Equals( typeof(System.Int16)) )
					{
						propID.SetValue((System.Int16)(++realID));
					}
					else if( propID.Type.Equals(typeof(System.Byte)) )
					{
						propID.SetValue((System.Byte)(++realID));
					}
					else
					{
						propID.SetValue(++realID);
					}
				}
			}
		}
		
		protected void AddImpl(SqlSmoObject obj, Int32 insertAtPosition)
		{
			CheckCollectionLock();

			if( null == obj )
            {
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException());
            }


            // we can add an object to a collection if it is in Creating state, or if 
            // it is in Pending state and its key has been set 
            if (null == obj.ParentColl)
            {
                obj.SetParentImpl(this.ParentInstance);
            }

            obj.CheckPendingState();
			ValidateParentObject(obj);

			InternalList.Insert( insertAtPosition, obj);
			obj.objectInSpace = false;
			obj.key.Writable = true;

			// if we can have duplicate names in the collection this means the ID's are
			// coming from the server and we don't need to rearrange them 
			if( !this.AcceptDuplicateNames )
            {
                FixIDs(insertAtPosition);
            }
        }

		internal void AddImpl(SqlSmoObject obj, ObjectKeyBase insertAtKey)
		{
			CheckCollectionLock();

			if( null == obj )
            {
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException());
            }

            int pos = InternalStorage.LookUp(insertAtKey);
            if (-1 == pos)
            {
                throw new SmoException(ExceptionTemplates.ColumnBeforeNotExisting(insertAtKey.ToString()));
            }

            AddImpl(obj, pos);
		}

		internal void AddImpl(SqlSmoObject obj)
		{
			try
			{
				if (null == obj)
                {
                    throw new ArgumentNullException();
                }

                // Since we can have column objects upto 100k through sparse columns support, look up takes a huge amount of time in case of create time
                // Hence we removed the look up in case of columns. Then engine throws the exception in this case.
                if (!(obj is Column))
                {
                    int pos = InternalStorage.LookUp(obj.key);

                    if (-1 != pos)
                    {
                        throw new SmoException(ExceptionTemplates.CannotAddObject(obj.GetType().Name, obj.ToString()));
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
	
	
	internal class SmoArrayList : SmoInternalStorage
	{
		internal ArrayList innerCollection = null;
		SmoCollectionBase parent = null;
		internal SmoArrayList(IComparer keyComparer, SmoCollectionBase parent) : base(keyComparer)
		{
			innerCollection = new ArrayList();
			this.parent = parent;
		}

		internal override bool Contains(ObjectKeyBase key)
		{
			return LookUp(key) != -1;
		}
		
		internal override Int32 LookUp(ObjectKeyBase key)
		{
			for( int idx = 0; idx < innerCollection.Count; idx++)
			{
				if( 0 == keyComparer.Compare(key, ((SqlSmoObject)innerCollection[idx]).key ))
                {
                    return idx;
                }
            }
			
			return -1;
		}

		internal override SqlSmoObject this[ObjectKeyBase key]
		{ 
			get 
			{ 
				int pos = LookUp(key);
				if( pos != -1 )
                {
                    return innerCollection[pos] as SqlSmoObject;
                }
                else
                {
                    return null;
                }
            }
			set 
			{ 
				int pos = LookUp(key);
				if( pos != -1 )
                {
                    innerCollection[pos] = value;
                }
                else
                {
                    innerCollection.Add(value);
                }
            }
		}
		
		internal override SqlSmoObject GetByIndex(Int32 index)
		{
			return innerCollection[index] as SqlSmoObject;
		}

		public override Int32 Count 
		{ 
			get { return innerCollection.Count;}
		}

		internal override void Add(ObjectKeyBase key, SqlSmoObject o)
		{
			innerCollection.Add(o);
			o.key.Writable = false;
		}
		
		internal override void Remove(ObjectKeyBase key)
		{
			int pos = LookUp(key);
			if (pos != -1)
			{
				((SqlSmoObject)innerCollection[pos]).key.Writable = true;
				innerCollection.RemoveAt(pos);
			}
			else
            {
                throw new InternalSmoErrorException(ExceptionTemplates.CouldNotFindKey(key.ToString()));
            }
        }
		
		internal override void Clear()
		{
			innerCollection.Clear();
		}

		internal override void InsertAt(int position, SqlSmoObject o)
		{
			innerCollection.Insert(position, o);
		}
		
		internal override void RemoveAt(int position)
		{
			innerCollection.RemoveAt(position);
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
			return innerCollection.GetEnumerator();
		}

	}
}

