// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// Abstract class for all Event classes.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")]
    public abstract class EventSetBase
    {
        private BitArray m_storage;

        /// <summary>
        ///bit storage</summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal protected BitArray Storage
        {
            get { return m_storage; }
            set { m_storage = value; }
        }
        
        /// <summary>
        ///default constructor</summary>
        //[SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EventSetBase()
        {
            m_storage = new BitArray(this.NumberOfElements);
        }

        /// <summary>
        ///copy constructor</summary>
        public EventSetBase(EventSetBase eventSetBase)
        {
            m_storage = eventSetBase.Storage;
        }

        /// <summary>
        ///number of elements</summary>
        public abstract int NumberOfElements
        {
            get;
        }

        /// <summary>
        ///copy</summary>
        public abstract EventSetBase Copy();

        /// <summary>
        ///set bit at idx with value</summary>
        internal void SetBitAt(int idx, bool value)
        {
            m_storage[idx] = value;
        }

        /// <summary>
        ///get teh value of bit at idx</summary>
        internal bool GetBitAt(int idx)
        {
            return m_storage[idx];
        }

        /// <summary>
        ///set value</summary>
        protected void SetValue(EventSetBase options, bool value)
        {
            if (value)
            {
                m_storage.Or(options.m_storage);	
            }
            else
            {
                BitArray clone = (BitArray) options.m_storage.Clone();
                m_storage.And(clone.Not());
            }
        }

        /// <summary>
        ///true if fits the mask</summary>
        protected bool FitsMask(EventSetBase mask)
        {
            for (int idx = 0; idx < mask.NumberOfElements; idx++)
            {
                if (mask.m_storage[idx] && !this.m_storage[idx])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// returns true if this and optCompare have common options that are set</summary>
        protected bool HasCommonBits(EventSetBase optionsCompare)
        {
            // if null, there is no filter
            if( null == optionsCompare )
            {
                return true;
            }

            // otherwise, clone the array, and check if we have anything in common
            BitArray clone = (BitArray)this.m_storage.Clone();
            BitArray mask = clone.And(optionsCompare.m_storage);
            
            for( int idx = 0; idx < NumberOfElements; idx++)
            {
                if (mask[idx] != optionsCompare.m_storage[idx])
                {
                    return false;
                }
            }

            return true;
        }
    }

}
