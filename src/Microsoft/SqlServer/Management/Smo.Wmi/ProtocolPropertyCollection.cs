
using System;
using System.Collections;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
    /// <summary>
    /// ProtocolProperty is a Property with two more internal members,
    /// which are the keys we have to use in order to access it while 
    /// dealing with the managed WMI provider
    /// </summary>
    public class ProtocolProperty : Property
    {
        internal ProtocolProperty(Property p) :
            base(p)		
        {
        }

        UInt32 propertyType;
        internal UInt32 PropertyType
        {
            get 	{return propertyType;	}
            set { propertyType = value; }
        }

    }

    public sealed class ClientProtocolProperty : ProtocolProperty
    {
        internal ClientProtocolProperty(Property p) :
            base(p)		
        {
        }
            
        UInt32 index;
        internal UInt32 Index
        {
            get 	{return index;	}
            set { index = value; }
        }
    }
    
    public sealed class ServerProtocolProperty : ProtocolProperty
    {
        internal ServerProtocolProperty(Property p) :
            base(p)		
        {
        }
            
    }
    
    public sealed class IPAddressProperty : ProtocolProperty
    {
        internal IPAddressProperty(Property p) :
            base(p)		
        {
        }
            
    }
    

    public sealed class ClientProtocolPropertyCollection : ProtocolPropertyCollection
    {
        internal ClientProtocolPropertyCollection() : base() {}

        internal override void Add(ProtocolProperty property) 
        {
            // the key for a property is a pair of index and type, not its name 
            // as you might expect (see WMI classes (S/P)ProtocolProperty)
            // so we make an uint64 out of those two, Index lying on the most 
            // significant 32 bits
            long key = ((long)((ClientProtocolProperty)property).Index << 32) + property.PropertyType;
            sl.Add(key , property);
        }

        
    }

    public sealed class ServerProtocolPropertyCollection : ProtocolPropertyCollection
    {
        internal ServerProtocolPropertyCollection() : base() {}

        internal override void Add(ProtocolProperty property) 
        {
            sl.Add(property.Name, property);
        }
    }
    
    public sealed class IPAddressPropertyCollection : ProtocolPropertyCollection
    {
        internal IPAddressPropertyCollection() : base() {}

        internal override void Add(ProtocolProperty property) 
        {
            sl.Add(property.Name, property);
        }
    }
    
    /// <summary>
    /// This class is a collection of properties that can be accessed by name
    /// The client has also access to the Property object in itself.
    /// </summary>
    public abstract class ProtocolPropertyCollection : ICollection
    {
        internal ProtocolPropertyCollection() 
        { 
            sl = new SortedList(); 
        }

        
        public void CopyTo(ProtocolProperty[] array,Int32 index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array,Int32 index)
        {
            int idx = index;
            foreach(DictionaryEntry de in sl)
            {
                array.SetValue( de.Value, idx++);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new ProtocolPropertyEnumerator(this);
        }

        internal abstract void Add(ProtocolProperty property);

        internal void Remove(string name) 
        {
            sl.Remove(name); 
        }
        
        public bool Contains(string name) 
        { 
            return (null != LookupName(name));
        }
        
        public Int32 Count 
        { 
            get 
            {
                return sl.Count;
            } 
        }

        public bool IsSynchronized
        {
            get
            {
                return sl.IsSynchronized;
            }
        }

        public object SyncRoot
        {
            get
            {
                return sl.SyncRoot;
            }
        }

        // sets the dirty flag for all objects in collection
        internal void SetAllDirty(bool bVal)
        {
            foreach(DictionaryEntry de in sl)
            {
                ProtocolProperty prop = de.Value as ProtocolProperty;
                if(null!=prop) 
                    prop.SetDirty(bVal);
            }
        }

        internal void SetAllRetrieved(bool bVal)
        {
            foreach(DictionaryEntry de in sl)
            {
                ProtocolProperty prop = de.Value as ProtocolProperty;
                if(null!=prop) 
                    prop.SetRetrieved(bVal);
            }
        }

        internal void Update(string propName, object value)
        {
            ProtocolProperty prop = this[propName];
            prop.SetValue(value);
            prop.SetRetrieved(true);
        }
        
        internal void Clear()
        {
            sl.Clear();
        }

        ///<summary>
        ///Checks if a property exists and is dirty
        ///</summary>
        internal bool ExistAndDirty(string propname)
        {
            ProtocolProperty prop = this[propname];
            if( null == prop.Value ) 
            {
                return false;
            }
            else
            {
                return prop.Dirty;
            }
        }

        ///<summary>
        ///returns a property if it exists and is dirty
        ///</summary>
        internal object GetIfDirty(string propname)
        {
            ProtocolProperty prop = this[propname];
            if( !prop.Dirty ) 
            {
                return null;
            }
            else
            {
                return prop.Value;
            }
        }

        internal ProtocolProperty LookupName(string name)
        {
            // we do a plain list traversal here, since the number 
            // of properties is small, we don't need a more sophisticated 
            // structure here
            foreach(DictionaryEntry ppo in sl)
            {
                ProtocolProperty pp = ppo.Value as ProtocolProperty;
                if( pp.Name == name )
                    return pp;
            }

            return null;
        }

        ///<summary>
        /// string indexer
        ///</summary>
        public ProtocolProperty this[string name] 
        { 
            get
            {
                ProtocolProperty prop = LookupName(name);
                // check if the property exists
                if (null==prop) 
                {
                    throw new UnknownPropertyException(name );
                }
                
                return prop;
            }
        }
        
        ///<summary>
        /// Integer indexer
        ///</summary>
        public ProtocolProperty this[Int32 index] 
        { 
            get
            {
                // we do boundaries checking because we want callers to see the 
                // exception coming from here, rather than from SortedList
                if( (0>index) || (index>=sl.Count) )
                    throw new IndexOutOfRangeException();
                
                ProtocolProperty prop = sl.GetByIndex(index) as ProtocolProperty;
                return prop;
            }
        }

        internal SortedList sl;	// collection holder

        ///<summary>
        /// nested enumerator class. It ses SortedList enumerations.
        ///</summary>
        internal sealed class ProtocolPropertyEnumerator : IEnumerator 
        {
            private IDictionaryEnumerator baseEnumerator;
            
            public ProtocolPropertyEnumerator(ProtocolPropertyCollection col) 
            {
                this.baseEnumerator = col.sl.GetEnumerator();
            }

            object IEnumerator.Current 
            { 
                get 
                {
                    return ((DictionaryEntry)(baseEnumerator.Current)).Value;
                }
            }
            
            ProtocolProperty Current 
            {
                get
                {
                    return baseEnumerator.Value as ProtocolProperty;
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


