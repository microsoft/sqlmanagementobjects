// Copyright (c) Microsoft.
// Licensed under the MIT license.

#define TRACE

namespace Microsoft.SqlServer.Management.Sdk.Sfc.Diagnostics
{
    using System;
    using System.Collections;
    using System.Diagnostics;


    internal class SQLToolsTraceListenerCollection : IList 
    {
        ArrayList list;

        public SQLToolsTraceListenerCollection() 
        {
            list = new ArrayList(1);
        }

        public TraceListener this[int i] 
        {
            get 
            {
                return (TraceListener)list[i];
            }

            set 
            {
                list[i] = value;
            }            
        }

        public TraceListener this[string name] 
        {
            get 
            {
                foreach (TraceListener listener in this) 
                {
                    if (listener.Name == name)
                    {
                        return listener;
                    }
                }
                return null;
            }
        }

        public int Count 
        { 
            get 
            {
                return list.Count;
            }
        }

        public int Add(TraceListener listener) 
        {                
            return ((IList)this).Add(listener);
        }

        public void AddRange(TraceListener[] value) 
        {
            for (int i = 0; ((i) < (value.Length)); i = ((i) + (1))) 
            {
                this.Add(value[i]);
            }
        }        
        
        public void AddRange(SQLToolsTraceListenerCollection value) 
        {
            for (int i = 0; ((i) < (value.Count)); i = ((i) + (1))) 
            {
                this.Add(value[i]);
            }
        }
        
        public void Clear() 
        {
            list = new ArrayList();
        }        

        public bool Contains(TraceListener listener) 
        {
            return ((IList)this).Contains(listener);
        }

        public void CopyTo(TraceListener[] listeners, int index) 
        {
            ((ICollection)this).CopyTo((Array) listeners, index);                   
        }
        public IEnumerator GetEnumerator() 
        {
            return list.GetEnumerator();
        }


        public int IndexOf(TraceListener listener) 
        {
            return ((IList)this).IndexOf(listener);
        }

        public void Insert(int index, TraceListener listener) 
        {
            ((IList)this).Insert(index, listener);
        }

        public void Remove(TraceListener listener) 
        {
            ((IList)this).Remove(listener);
        }

        public void Remove(string name) 
        {
            TraceListener listener = this[name];
            if (listener != null)
            {
                ((IList)this).Remove(listener);
            }
        }

        public void RemoveAt(int index) 
        {
            ArrayList newList = new ArrayList(list.Count);
            lock (this) 
            {
                newList.AddRange(list);
                newList.RemoveAt(index);
                list = newList;
            }
        }

       object IList.this[int index] 
        {
            get 
            {
                return list[index];
            }

            set 
            {
                list[index] = value;
            }
        }
        
        bool IList.IsReadOnly 
        {
            get 
            {
                return false;
            }
        }

        bool IList.IsFixedSize 
        {
            get 
            {
                return false;
            }
        }
        
        int IList.Add(object value) 
        {
            int i;            
            ArrayList newList = new ArrayList(list.Count + 1);
            lock (this) 
            {
                newList.AddRange(list);
                i = newList.Add(value);
                list = newList;
            }        
            return i;
        }
        
        bool IList.Contains(object value) 
        {
            return list.Contains(value);
        }
        
        int IList.IndexOf(object value) 
        {
            return list.IndexOf(value);
        }
        
        void IList.Insert(int index, object value) 
        {
            ArrayList newList = new ArrayList(list.Count + 1);
            lock (this) 
            {
                newList.AddRange(list);        
                newList.Insert(index, value);
                list = newList;
            }            
        }
        
        void IList.Remove(object value) 
        {
            ArrayList newList = new ArrayList(list.Count);
            lock (this) 
            {
                newList.AddRange(list);
                newList.Remove(value);
                list = newList;
            }
        }
        
        object ICollection.SyncRoot 
        {
            get {
                return this;
            }
        }
                                                  
        bool ICollection.IsSynchronized 
        {
            get 
            {
                return true;
            }
        }
        
        void ICollection.CopyTo(Array array, int index) 
         {
            ArrayList newList = new ArrayList(list.Count + array.Length);
            lock (this) 
            {
                newList.AddRange(list);
                newList.CopyTo(array, index);
                list = newList;
            }
        }
    }

}
