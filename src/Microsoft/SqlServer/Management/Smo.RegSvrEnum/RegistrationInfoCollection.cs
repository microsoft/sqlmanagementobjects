using System;
using System.Collections;


namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
	public class RegistrationInfoCollection : IEnumerable, ICollection
	{
		#region Private Data
		protected object syncObject;
		protected System.Collections.ArrayList entries;
		#endregion
	
		public RegistrationInfoCollection()
		{
			this.entries = new System.Collections.ArrayList();
			this.syncObject = new object();
		}
		public RegistrationInfoCollection(RegistrationInfo[] entries)	
			:this()
		{
			this.entries.AddRange(entries);
		}

		internal void Clear()
		{
			this.entries.Clear();
		}

		internal int Add( RegistrationInfo reg )
		{
			return entries.Add( reg );
		}

		internal void Remove( RegistrationInfo reg )
		{
			entries.Remove(reg);
		}

        public void Sort()
        {
            entries.Sort();
        }

        public int IndexOf(string index)
        {
            if (index == null)
            {
                return -1;
            }
            int c = Count;
            RegistrationInfo cur;
            for (int i = 0; i < c; i++)
            {
                cur = this[i];
                if (0 == String.Compare(cur.FriendlyName, index, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }

            //nothing found
            return -1;
        }
		
        public RegistrationInfo this[int index]
		{
			get
			{
				return (RegistrationInfo)this.entries[index];
			}
		}


        /// <summary>
        /// returns RegistrationInfo with specified name. 
        /// </summary>
        public RegistrationInfo this[string index]
        {
            get
            {
                if (index == null)
                {
                    return null;
                }
                int c = Count;
                RegistrationInfo cur;
                for (int i = 0; i < c; i++)
                {
                    cur = this[i];
                    if (0 == String.Compare(cur.FriendlyName, index, false, System.Globalization.CultureInfo.CurrentCulture))
                    {
                        return cur;
                    }
                }

                //nothing found
                return null;
            }
        }


        /// <summary>
        /// checks whether registrationinfo with given name already exists.
        /// NOTE: comparison is case insensitive
        /// </summary>
        /// <param name="childName"></param>
        /// <returns></returns>
        public bool Contains(string childName)
        {
            RegistrationInfo info = this[childName];
            return (info != null);
        }

        /// <summary>
        /// Strongly  typed version of ICollection.CopyTo(...)
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(RegistrationInfo[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }


		#region ICollection Implementation
        public virtual void CopyTo(System.Array array, int index)
        {			
			if(array == null)
			{
				throw new ArgumentNullException("array");
			}

			if(index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			if(array.Rank != 1)
			{
                throw new ArgumentException(SRError.ArrayDimensionInvalid, "array");
            }

			if(this.entries.Count >= array.Length - index)
			{
                throw new ArgumentException(SRError.ArraySizeError, "array");
            }

			this.entries.CopyTo(array, index);
		}

		public virtual int Count
		{			
			get
			{
				return this.entries.Count;
			}
		}
		public virtual object SyncRoot
		{
			get
			{
				return this.syncObject;
			}
		}
		public virtual bool IsSynchronized
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region IEnumerable Implementation
		public System.Collections.IEnumerator GetEnumerator()
		{
			return (System.Collections.IEnumerator) new RegistrationInfoCollectionEnumerator( this );
		}
		class RegistrationInfoCollectionEnumerator : System.Collections.IEnumerator
		{
			private RegistrationInfoCollection current;
			private int index;
			public RegistrationInfoCollectionEnumerator( RegistrationInfoCollection aic)
			{
				current = aic;
				index = -1;
			}
			public bool MoveNext()
			{
				index++;
				if( index >= current.entries.Count )
				{
					return false;
				}
				else
				{
					return true;
				}
			}
			public void Reset()
			{
				index = -1;
			}
			public object Current
			{
				get
				{
					return current.entries[index];
				}
			}
		}
		#endregion
	}
}