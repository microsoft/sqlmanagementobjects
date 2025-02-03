// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    using Microsoft.SqlServer.Management.Smo.Agent;

    public abstract class SmoCollectionBase : AbstractCollectionBase, ICollection
    {

        private SmoInternalStorage internalStorage = null;
        internal SmoInternalStorage InternalStorage
        {
            get
            {
                if (null == internalStorage)
                {
                    InitInnerCollection();
                }
                return internalStorage;
            }
            set { internalStorage = value; }
        }


        internal SmoCollectionBase(SqlSmoObject parent)
            : base(parent)
        {
        }

        protected abstract void InitInnerCollection();

        protected virtual Type GetCollectionElementType()
        {
            return null;
        }

        string m_lockReason = null;
        internal void LockCollection(string lockReason)
        {
            m_lockReason = lockReason;
        }

        internal void UnlockCollection()
        {
            m_lockReason = null;
        }

        ///<summary>
        /// return true if the collection is locked for updates ( it gets locked
        /// when the parent text object in in text mode )
        ///</summary>
        internal bool IsCollectionLocked
        {
            get
            {
                return null != m_lockReason;
            }
        }

        internal void CheckCollectionLock()
        {
        if (ParentInstance.IsDesignMode)
            {
                return;
            }

            if (null != m_lockReason)
            {
                throw new FailedOperationException(ExceptionTemplates.CollectionCannotBeModified + m_lockReason).SetHelpContext("CollectionCannotBeModified");
            }
        }

        internal virtual SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return null;
        }

        public bool IsSynchronized
        {
            get
            {
                return InternalStorage.IsSynchronized;
            }

        }

        public object SyncRoot
        {
            get
            {
                return InternalStorage.SyncRoot;
            }
        }

        internal override void ImplRemove(ObjectKeyBase key)
        {
            InternalStorage.Remove(key);
        }

        internal void Remove(ObjectKeyBase key)
        {
            RemoveObj((SqlSmoObject)InternalStorage[key], key);
        }

    internal void RemoveObj(SqlSmoObject obj, ObjectKeyBase key)
    {
        CheckCollectionLock();
        if( null != obj )
        {
            if( obj.State == SqlSmoState.Creating ||
                ((obj is Column) && (obj.ParentColl.ParentInstance is View )) 
            )
            {
                InternalStorage.Remove(key);
                obj.objectInSpace = true;
            }
            else
            {
                throw new InvalidSmoOperationException("Remove", obj.State);
            }

        }
        else
        {
            if( !key.IsNull )
                {
                    throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist(GetCollectionElementType().Name, key.ToString()));
                }
                else
                {
                    throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException());
                }
            }
    }



        internal SqlSmoObject GetNewObject(ObjectKeyBase key)
        {
            key.Validate(GetCollectionElementType());

            bool needsValidation = (null == ParentInstance || !ParentInstance.IsObjectInSpace());
            if ((true == InternalStorage.Contains(key) ||
                (needsValidation && (null != InitializeChildObject(key)))) &&
                !this.AcceptDuplicateNames)
            {
                throw new SmoException(ExceptionTemplates.CannotAddObject(GetCollectionElementType().ToString(), key.ToString()));
            }

            // instantiate a new child object
            return GetCollectionElementInstance(key, SqlSmoState.Creating);
        }

        // returns wrapped object
        protected SqlSmoObject GetObjectByIndex(Int32 index)
        {
            if (!initialized && ParentInstance.State == SqlSmoState.Existing)
            {
                InitializeChildCollection();
            }

            return InternalStorage.GetByIndex(index) as SqlSmoObject;
        }


        // returns wrapped object
        internal virtual SqlSmoObject GetObjectByKey(ObjectKeyBase key)
        {
            object instanceObject = InternalStorage[key];

            bool needsValidation = (null == ParentInstance || !ParentInstance.IsObjectInSpace()); ;

            if ((null == instanceObject) &&
                needsValidation &&
                !initialized &&
                ParentInstance.State == SqlSmoState.Existing)
            {
                instanceObject = InitializeChildObject(key);
            }

            return instanceObject as SqlSmoObject;
        }

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
        public void ClearAndInitialize(string filterQuery, IEnumerable<string> extraFields)
        {
            this.InternalStorage.Clear();
            InitializeChildCollection(false, null, filterQuery: filterQuery, extraFields: extraFields);
        }

        /// <summary>
        /// Empties the collection but doesn't attempt to retrieve any data
        /// </summary>
        public void ResetCollection()
        {
            InternalStorage.Clear();
            UnlockCollection();
        }

        private void InitializeChildCollection(bool refresh, ScriptingPreferences sp)
        {
            InitializeChildCollection(refresh, sp, filterQuery: null, extraFields: null);
        }

        /// <summary>
        /// Initialize the child collection
        /// </summary>
        protected void InitializeChildCollection()
        {
            InitializeChildCollection(false);
        }

        /// <summary>
        /// Initializes the child collection, optionally keeping all the old objects
        /// </summary>
        /// <param name="refresh">directs if we discard the old objects</param>
        protected void InitializeChildCollection(bool refresh)
        {
            InitializeChildCollection(refresh, null);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods",
            MessageId = "System.Type.InvokeMember")]
        private void InitializeChildCollection(bool refresh, ScriptingPreferences sp, string filterQuery, IEnumerable<string> extraFields)
        {
        // In design mode the objects are not retrieved, but are added by the client
            if (this.ParentInstance.IsDesignMode)
            {
                this.initialized = true;
                return;
            }
            // keep the old collection, because we'll append all the objects to the new one
            SmoInternalStorage oldColl = InternalStorage;
            InitInnerCollection();
            
            string urnsuffix = null;
            if( this.GetCollectionElementType().GetBaseType() == typeof(Parameter) )
            {
                urnsuffix = this.GetCollectionElementType().GetBaseType().GetBaseType().InvokeMember("UrnSuffix",
                    SqlSmoObject.UrnSuffixBindingFlags,
                    null, null, new object[] {}, SmoApplication.DefaultCulture ) as string;
            }
            else
            {
                urnsuffix = this.GetCollectionElementType().InvokeMember("UrnSuffix",
                    SqlSmoObject.UrnSuffixBindingFlags,
                    null, null, new object[] {}, SmoApplication.DefaultCulture ) as string;
            }
            
            urnsuffix += (string.IsNullOrEmpty(filterQuery) ? string.Empty : filterQuery);
            // init the collection with objects
            this.ParentInstance.InitChildLevel(urnsuffix, sp ?? new ScriptingPreferences(), forScripting: sp != null, extraFields: extraFields);
            
            // now merge the old collection into the new one
            foreach (SqlSmoObject oldObj in oldColl)
            {
                ObjectKeyBase sok = oldObj.key.Clone();
                SqlSmoObject obj = InternalStorage[sok];
                if (null != obj)
                {
                    InternalStorage[sok] = oldObj;
                }
                else
                {
                    if (oldObj.State == SqlSmoState.Creating)
                    {
                        if (!refresh)
                        {
                            InternalStorage[sok] = oldObj;
                        }
                    }
                    else
                    {
                        oldObj.SetState(SqlSmoState.Dropped);
                    }
                }
            }

            // update the state flag
            this.initialized = true;
        }

            
        // this function tries to instantiate a missing object
        // and if it exists we add it to the collection
        internal object InitializeChildObject(ObjectKeyBase key)
        {
            if (this.ParentInstance.IsDesignMode)
            {
                // Do not try to get the object while in design mode.
                return null;
            }

            SqlSmoObject childobj = GetCollectionElementInstance(key, SqlSmoState.Creating);
            
            if( childobj.Initialize() )
            {
                // update object's state and add it to the collection
                childobj.SetState(SqlSmoState.Existing);
                this.AddExisting(childobj);
                return childobj;
            }
            else
            {
                return null;
            }
        }

            

        

        // behaves like this[string].get 
        internal bool Contains(ObjectKeyBase key)
        {
            return null != GetObjectByKey(key);
        }

        internal bool ContainsKey(ObjectKeyBase key)
        {
            return null != InternalStorage[key];
        }

        public Int32 Count
        {
            get
            {
                if (!initialized && ParentInstance.State == SqlSmoState.Existing)
                {
                    InitializeChildCollection();
                }
                return InternalStorage.Count;
            }
        }

        public void Refresh()
        {
            Refresh(false);
        }

        public void Refresh(bool refreshChildObjects)
        {
            InitializeChildCollection(true);
            if (refreshChildObjects)
            {
                IEnumerator ienum = this.GetEnumerator();

                if (null != ienum)
                {
                    while (ienum.MoveNext())
                    {
                        ((SqlSmoObject)ienum.Current).Refresh();
                    }
                }
            }
        }

        internal void Clear()
        {
            MarkAllDropped();
            InternalStorage.Clear();
        }

        protected SqlSmoObject GetItemById(int id)
        {
            return GetItemById(id, "ID");
        }

        protected SqlSmoObject GetItemById(int id, string idPropName)
        {
            IEnumerator ie = ((IEnumerable)this).GetEnumerator();
            while (ie.MoveNext())
            {
                SqlSmoObject c = (SqlSmoObject)ie.Current;
                Property p = c.Properties.Get(idPropName);

                if (null == p.Value && c.State != SqlSmoState.Creating)
                {
                    c.Initialize(true); // initialize the object to get the property value
                }

                if (null != p.Value && id == Convert.ToInt32(p.Value, SmoApplication.DefaultCulture)) // found object with the right id
                {
                    return c;
                }
            }
            return null;
        }


        internal void MarkAllDropped()
        {
            IEnumerator collenum = this.GetEnumerator();
            while (collenum != null && collenum.MoveNext())
            {
                ((SqlSmoObject)collenum.Current).MarkDroppedInternal();
            }
        }

        /// <summary>
        /// Returns an enumerator after making sure the collection is initialized
        /// </summary>
        /// <param name="sp">The optional scripting settings to pass along to each SqlSmoObject.</param>
        /// <returns></returns>
        internal IEnumerator GetEnumerator(ScriptingPreferences sp)
        {
            //If our parent is dropped we can't get an enumerator 
            //since we depend on its properties (such as DB collation)
            if (ParentInstance.State == SqlSmoState.Dropped)
            {
                return null;
            }

            if (!initialized && ParentInstance.State == SqlSmoState.Existing)
            {
                InitializeChildCollection(false, sp);
            }

            return InternalStorage.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator after making sure the collection is initialized with the default properties
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator GetEnumerator()
        {
            return GetEnumerator(null);
        }

        internal override SqlSmoObject NoFaultLookup(ObjectKeyBase key)
        {
            return InternalStorage[key] as SqlSmoObject;
        }

        internal override Int32 NoFaultCount { get { return InternalStorage.Count; } }

        protected override void ImplAddExisting(SqlSmoObject obj)
        {
            InternalStorage.Add(obj.key, obj);
        }

        // implementing ICollection
        void ICollection.CopyTo(Array array, Int32 index)
        {
            int idx = index;
            foreach (SqlSmoObject sp in InternalStorage)
            {
                array.SetValue(sp, idx++);
            }
        }

        bool acceptDuplicateNames = false;
        internal bool AcceptDuplicateNames
        {
            get { return acceptDuplicateNames; }
            set { acceptDuplicateNames = value; }
        }

        internal bool CanHaveEmptyName(Urn urn)
        {
            //all agent objects can have empty names
            if (urn.Value.IndexOf("/JobServer", StringComparison.Ordinal) > 0)
            {
                return true;
            }
            if ((urn.Type == "Login" && urn.Parent.Type == "LinkedServer"))
            {
                return true;
            }

            return false;
        }

        protected void ValidateParentObject(SqlSmoObject obj)
        {
            // check to see if the object being added belongs to a 
            // different collection
            if (obj.ParentColl != this)
            {
                throw new FailedOperationException(ExceptionTemplates.WrongParent(obj.ToString()));
            }
        }


    }

    // base class for all comparers
    // its implementation of IComparer will compare strings
    internal class ObjectComparerBase : IComparer
    {
        protected IComparer stringComparer;

        internal ObjectComparerBase(IComparer stringComparer)
        {
            this.stringComparer = stringComparer;
        }

        public virtual int Compare(object obj1, object obj2) { return 0; }
    }

    internal class ObjectKeyBase
    {
        public ObjectKeyBase()
        {
        }

        // this function will throw an exception if the key cannot be in the collection
        // eg we won't allow objetcs with empty names
        internal virtual void Validate(Type objectType)
        {
        }

        public virtual string UrnFilter
        {
            get { return string.Empty; }
        }

        public virtual StringCollection GetFieldNames()
        {
            return new StringCollection();
        }

        public virtual bool IsNull
        {
            get { return false; }
        }

        bool writable = true;
        internal bool Writable
        {
            get { return writable; }
            set { writable = value; }
        }

        public virtual ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new ObjectComparerBase(stringComparer);
        }

        public virtual ObjectKeyBase Clone()
        {
            return new ObjectKeyBase();
        }

        /// <summary>
        /// Returns the name that we will use while displaying an exception 
        /// that involves this object. We need this function because some 
        /// exception boxes should not show a ToString() formatted name
        /// e.g. [dbo].[t1] will be shown as 'dbo.t1'
        /// </summary>
        /// <returns></returns>
        public virtual string GetExceptionName()
        {
            // the stock implementation is to return ToString()
            return ToString();
        }

        internal static StringCollection GetFieldNames(Type t)
        {
            if (t.IsSubclassOf(typeof(ScriptSchemaObjectBase)))
            {
                return SchemaObjectKey.schemaFields;
            }
            else if (t.IsSubclassOf(typeof(MessageObjectBase)))
            {
                return MessageObjectKey.fields;
            }
            else if (t.IsSubclassOf(typeof(SoapMethodObject)))
            {
                return SoapMethodKey.soapMethodFields;
            }
            else if (t == typeof(NumberedStoredProcedure))
            {
                return NumberedObjectKey.fields;
            }
            else if (t == typeof(PhysicalPartition))
            {
                return PartitionNumberedObjectKey.fields;
            }
            else if (t == typeof(ScheduleBase))
            {
                return ScheduleObjectKey.fields;
            }
            else if (t == typeof(Job))
            {
                return JobObjectKey.jobKeyFields;
            }
            else if (t == typeof(DatabaseReplicaState))
            {
                return DatabaseReplicaStateObjectKey.fields;
            }
            else if (t == typeof(AvailabilityGroupListenerIPAddress))
            {
                return AvailabilityGroupListenerIPAddressObjectKey.fields;
            }
            else if (t == typeof(SecurityPredicate))
            {
                return SecurityPredicateObjectKey.fields;
            }
            else if (t == typeof(ColumnEncryptionKeyValue))
            {
                return ColumnEncryptionKeyValueObjectKey.fields;
            }
            else if (t == typeof(ExternalStream))
            {
                return ExternalStream.RequiredFields;
            }
            else if (t == typeof(ExternalStreamingJob))
            {
                return ExternalStreamingJob.RequiredFields;
            }
            else
            {
                return SimpleObjectKey.fields;
            }
        }

        internal static ObjectKeyBase CreateKeyOffset(Type t, System.Data.IDataReader reader, int columnOffset)
        {
            // TODO: FIX_IN_KATMAI: just say no to special cases

            if (t.IsSubclassOf(typeof(ScriptSchemaObjectBase)))
            {
                // schema is reversed
                return new SchemaObjectKey(reader.GetString(columnOffset + 1),
                                            reader.GetString(columnOffset));
            }
            else if (t.IsSubclassOf(typeof(MessageObjectBase)))
            {
                return new MessageObjectKey(reader.GetInt32(columnOffset),
                                            reader.GetString(columnOffset + 1));
            }
            else if (t == typeof(NumberedStoredProcedure))
            {
                return new NumberedObjectKey(reader.GetInt16(columnOffset));
            }
            else if (t == typeof(PhysicalPartition))
            {
                return new PartitionNumberedObjectKey((short)reader.GetInt32(columnOffset));
            }
            else if (SqlSmoObject.IsOrderedByID(t))
            {
                return new SimpleObjectKey(reader.GetString(columnOffset + 1));
            }
            else if (t.IsSubclassOf(typeof(SoapMethodObject)))
            {
                //  Name, Namespace
                return new SoapMethodKey(reader.GetString(columnOffset + 1),
                                            reader.GetString(columnOffset));
            }
            else if (t.IsSubclassOf(typeof(ScheduleBase)))
            {
                //  Name, Namespace
                return new ScheduleObjectKey(reader.GetString(columnOffset),
                                                reader.GetInt32(columnOffset + 1));
            }
            else if (t == typeof(Job))
            {
                //  Name, CategoryID
                return new JobObjectKey(reader.GetString(columnOffset), reader.GetInt32(columnOffset + 1));
            }
            else if (t == typeof(DatabaseReplicaState))
            {
                return new DatabaseReplicaStateObjectKey(reader.GetString(columnOffset), reader.GetString(columnOffset + 1));
            }
            else if (t == typeof(AvailabilityGroupListenerIPAddress))
            {
                return new AvailabilityGroupListenerIPAddressObjectKey(reader.GetString(columnOffset), reader.GetString(columnOffset + 1), reader.GetString(columnOffset + 2));
            }
            else if(t == typeof(SecurityPredicate))
            {
                return new SecurityPredicateObjectKey(reader.GetInt32(columnOffset));
            }
            else if (t == typeof(ColumnEncryptionKeyValue))
            {
                return new ColumnEncryptionKeyValueObjectKey(reader.GetInt32(columnOffset));
            }
            else
            {
                return new SimpleObjectKey(reader.GetString(columnOffset));
            }
        }

    }


    internal abstract class SmoInternalStorage : IEnumerable
    {
        protected IComparer keyComparer = null;
        internal SmoInternalStorage(IComparer keyComparer)
        {
            this.keyComparer = keyComparer;
        }

        internal abstract bool Contains(ObjectKeyBase key);
        internal abstract Int32 LookUp(ObjectKeyBase key);

        internal abstract SqlSmoObject this[ObjectKeyBase key]
        {
            get;
            set;
        }

        internal abstract SqlSmoObject GetByIndex(Int32 index);

        public abstract Int32 Count { get; }

        internal abstract void Add(ObjectKeyBase key, SqlSmoObject o);
        internal abstract void Remove(ObjectKeyBase key);

        internal abstract void InsertAt(int position, SqlSmoObject o);
        internal abstract void RemoveAt(int position);

        internal abstract bool IsSynchronized { get; }

        internal abstract object SyncRoot { get; }

        public abstract IEnumerator GetEnumerator();

        internal abstract void Clear();
    }


}


