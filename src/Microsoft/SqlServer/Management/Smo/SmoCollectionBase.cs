// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Base implementation of ICollection for SMO objects
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TParent"></typeparam>
    public abstract class SmoCollectionBase<TObject, TParent> : AbstractCollectionBase, ICollection, IEnumerable<TObject>, ILockableCollection, ISmoInternalCollection, ISmoCollection
        where TObject : SqlSmoObject
        where TParent : SqlSmoObject
    {

        private SmoInternalStorage<TObject> internalStorage = null;
        internal SmoInternalStorage<TObject> InternalStorage
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


        internal SmoCollectionBase(TParent parent)
            : base(parent)
        {
        }

        /// <summary>
        /// Returns the object at the given index after ensuring the collection is initialized
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TObject this[int index] => GetObjectByIndex(index);

        protected abstract void InitInnerCollection();


        string m_lockReason = null;
        internal void LockCollection(string lockReason) => m_lockReason = lockReason;

        internal void UnlockCollection() => m_lockReason = null;

        ///<summary>
        /// return true if the collection is locked for updates ( it gets locked
        /// when the parent text object in in text mode )
        ///</summary>
        internal bool IsCollectionLocked => null != m_lockReason;

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

        internal abstract TObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state);

        internal override void ImplRemove(ObjectKeyBase key) => InternalStorage.Remove(key);

        internal void Remove(ObjectKeyBase key) => RemoveObj(InternalStorage[key], key);

        internal void RemoveObj(TObject obj, ObjectKeyBase key)
        {
            CheckCollectionLock();
            if (null != obj)
            {
                if (obj.State == SqlSmoState.Creating ||
                    ((obj is Column) && (obj.ParentColl.ParentInstance is View))
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
                if (!key.IsNull)
                {
                    throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist(typeof(TObject).Name, key.ToString()));
                }
                else
                {
                    throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException());
                }
            }
        }

        internal TObject GetNewObject(ObjectKeyBase key)
        {
            key.Validate(typeof(TObject));

            var needsValidation = null == ParentInstance || !ParentInstance.IsObjectInSpace();
            if ((true == InternalStorage.Contains(key) ||
                (needsValidation && (null != InitializeChildObject(key)))) &&
                !AcceptDuplicateNames)
            {
                throw new SmoException(ExceptionTemplates.CannotAddObject(typeof(TObject).ToString(), key.ToString()));
            }

            // instantiate a new child object
            return GetCollectionElementInstance(key, SqlSmoState.Creating);
        }

        internal virtual SqlSmoObject GetObjectByName(string name) => GetObjectByKey(new SimpleObjectKey(name));

        // returns wrapped object
        protected TObject GetObjectByIndex(int index)
        {
            if (!initialized && ParentInstance.State == SqlSmoState.Existing)
            {
                InitializeChildCollection();
            }

            return InternalStorage.GetByIndex(index) as TObject;
        }


        // returns wrapped object
        internal virtual TObject GetObjectByKey(ObjectKeyBase key)
        {
            object instanceObject = InternalStorage[key];

            var needsValidation = null == ParentInstance || !ParentInstance.IsObjectInSpace(); ;

            if ((null == instanceObject) &&
                needsValidation &&
                !initialized &&
                ParentInstance.State == SqlSmoState.Existing)
            {
                instanceObject = InitializeChildObject(key);
            }

            return instanceObject as TObject;
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
            InternalStorage.Clear();
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

        private void InitializeChildCollection(bool refresh, ScriptingPreferences sp) => InitializeChildCollection(refresh, sp, filterQuery: null, extraFields: null);

        /// <summary>
        /// Initialize the child collection
        /// </summary>
        protected void InitializeChildCollection() => InitializeChildCollection(false);

        /// <summary>
        /// Initializes the child collection, optionally keeping all the old objects
        /// </summary>
        /// <param name="refresh">directs if we discard the old objects</param>
        protected void InitializeChildCollection(bool refresh) => InitializeChildCollection(refresh, null);

        protected abstract string UrnSuffix { get; }

        private void InitializeChildCollection(bool refresh, ScriptingPreferences sp, string filterQuery, IEnumerable<string> extraFields)
        {
            // In design mode the objects are not retrieved, but are added by the client
            if (ParentInstance.IsDesignMode)
            {
                initialized = true;
                return;
            }
            // keep the old collection, because we'll append all the objects to the new one
            var oldColl = InternalStorage;
            InitInnerCollection();

            var urnsuffix = UrnSuffix + (string.IsNullOrEmpty(filterQuery) ? string.Empty : filterQuery);

            // init the collection with objects
            ParentInstance.InitChildLevel(urnsuffix, sp ?? new ScriptingPreferences(), forScripting: sp != null, extraFields: extraFields);

            // now merge the old collection into the new one
            foreach (var oldObj in oldColl)
            {
                var sok = oldObj.key.Clone();
                var obj = InternalStorage[sok];
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
            initialized = true;
        }


        // this function tries to instantiate a missing object
        // and if it exists we add it to the collection
        internal object InitializeChildObject(ObjectKeyBase key)
        {
            if (ParentInstance.IsDesignMode)
            {
                // Do not try to get the object while in design mode.
                return null;
            }

            var childobj = GetCollectionElementInstance(key, SqlSmoState.Creating);

            if (childobj.Initialize())
            {
                // update object's state and add it to the collection
                childobj.SetState(SqlSmoState.Existing);
                AddExisting(childobj);
                return childobj;
            }
            else
            {
                return null;
            }
        }


        // behaves like this[string].get 
        internal bool Contains(ObjectKeyBase key) => null != GetObjectByKey(key);

        internal bool ContainsKey(ObjectKeyBase key) => null != InternalStorage[key];

        /// <summary>
        /// Returns the number of objects in the collection after ensuring the collection is initialized
        /// </summary>
        public int Count
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

        /// <summary>
        /// Recreates the collection contents from the database without refreshing properties of any existing objects in the collection.
        /// </summary>
        public void Refresh() => Refresh(false);

        /// <summary>
        /// Refreshes the contents of the collection and refreshes the properties of existing objects in the collection
        /// </summary>
        /// <param name="refreshChildObjects"></param>
        public void Refresh(bool refreshChildObjects)
        {
            InitializeChildCollection(true);
            if (refreshChildObjects)
            {
                foreach (var obj in this)
                {
                    obj.Refresh();
                }
            }
        }

        /// <summary>
        /// Returns the item whose numeric ID property matches id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual TObject ItemById(int id) => GetItemById(id);

        internal void Clear()
        {
            MarkAllDropped();
            InternalStorage.Clear();
        }

        protected TObject GetItemById(int id) => GetItemById(id, "ID");

        protected TObject GetItemById(int id, string idPropName)
        {
            foreach (var c in this)
            {
                var p = c.Properties.Get(idPropName);

                if (null == p.Value && c.State != SqlSmoState.Creating)
                {
                    _ = c.Initialize(true); // initialize the object to get the property value
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
            // GetEnumerator returns null if the parent is already marked as Dropped
            // So we can't use foreach.
            var collenum = GetEnumerator();
            while (collenum != null && collenum.MoveNext())
            { 
                collenum.Current.MarkDroppedInternal();
            }
        }

        /// <summary>
        /// Returns an enumerator after making sure the collection is initialized
        /// </summary>
        /// <param name="sp">The optional scripting settings to pass along to each SqlSmoObject.</param>
        /// <returns></returns>
        internal IEnumerator<TObject> GetEnumerator(ScriptingPreferences sp)
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


        internal override SqlSmoObject NoFaultLookup(ObjectKeyBase key) => InternalStorage[key];

        internal override int NoFaultCount => InternalStorage.Count;

        protected override void ImplAddExisting(SqlSmoObject obj) => InternalStorage.Add(obj.key, (TObject)obj);

        // implementing ICollection
        public void CopyTo(TObject[] array, int index)
        {
            foreach (var obj in InternalStorage)
            {
                array[index++] = obj;
            }
        }

        int ICollection.Count => Count;
        object ICollection.SyncRoot => null;
        bool ICollection.IsSynchronized => false;
        void ICollection.CopyTo(Array array, int index)
        {
            var idx = index;
            foreach (var obj in InternalStorage)
            {
                array.SetValue(obj, idx++);
            }
        }
        internal bool AcceptDuplicateNames { get; set; } = false;
        bool ILockableCollection.IsCollectionLocked => IsCollectionLocked;

        internal virtual bool CanHaveEmptyName(Urn urn) => false;

        protected void ValidateParentObject(TObject obj)
        {
            // check to see if the object being added belongs to a 
            // different collection
            if (obj.ParentColl != this)
            {
                throw new FailedOperationException(ExceptionTemplates.WrongParent(obj.ToString()));
            }
        }

        /// <summary>
        /// Ensures the collection is initialized and returns a strongly typed IEnumerator using default scripting preferences
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TObject> GetEnumerator() => GetEnumerator(null);

        /// <summary>
        /// Ensures the collection is initialized and returns an IEnumerator using default scripting preferences
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ILockableCollection.LockCollection(string lockReason) => LockCollection(lockReason);

        void ILockableCollection.UnlockCollection() => UnlockCollection();

        void ILockableCollection.CheckCollectionLock() => CheckCollectionLock();

        SqlSmoObject ISmoInternalCollection.GetObjectByKey(ObjectKeyBase key) => GetObjectByKey(key);

        IEnumerable<SqlSmoObject> ISmoInternalCollection.InternalStorage => InternalStorage;
        IEnumerator<SqlSmoObject> ISmoInternalCollection.GetEnumerator(ScriptingPreferences sp) => GetEnumerator(sp);
    }

    internal interface ISmoInternalCollection
    {
        SqlSmoObject GetObjectByKey(ObjectKeyBase key);
        IEnumerable<SqlSmoObject> InternalStorage { get; }
        IEnumerator<SqlSmoObject> GetEnumerator(ScriptingPreferences sp);
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

        public virtual int Compare(object obj1, object obj2) => 0;
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

        public virtual string UrnFilter => string.Empty;

        public virtual StringCollection GetFieldNames() => new StringCollection();

        public virtual bool IsNull => false;

        bool writable = true;
        internal bool Writable
        {
            get { return writable; }
            set { writable = value; }
        }

        public virtual ObjectComparerBase GetComparer(IComparer stringComparer) => new ObjectComparerBase(stringComparer);

        public virtual ObjectKeyBase Clone() => new ObjectKeyBase();

        /// <summary>
        /// Returns the name that we will use while displaying an exception 
        /// that involves this object. We need this function because some 
        /// exception boxes should not show a ToString() formatted name
        /// e.g. [dbo].[t1] will be shown as 'dbo.t1'
        /// </summary>
        /// <returns></returns>
        public virtual string GetExceptionName() =>
            // the stock implementation is to return ToString()
            ToString();

        // TODO: https://github.com/microsoft/sqlmanagementobjects/issues/139
        // Replace these static factories with instance methods on the collection classes
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
            else if (t == typeof(IndexedJsonPath))
            {
                return IndexedJsonPathObjectKey.fields;
            }
            else if (t == typeof(ExternalStream))
            {
                return ExternalStream.RequiredFields;
            }
            else if (t == typeof(ExternalStreamingJob))
            {
                return ExternalStreamingJob.RequiredFields;
            }
            return SimpleObjectKey.fields;
        }

        internal static ObjectKeyBase CreateKeyOffset(Type t, System.Data.IDataReader reader, int columnOffset)
        {

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
            else if (t == typeof(SecurityPredicate))
            {
                return new SecurityPredicateObjectKey(reader.GetInt32(columnOffset));
            }
            else if (t == typeof(ColumnEncryptionKeyValue))
            {
                return new ColumnEncryptionKeyValueObjectKey(reader.GetInt32(columnOffset));
            }
            else if (t == typeof(IndexedJsonPath))
            {
                return new IndexedJsonPathObjectKey(reader.GetString(columnOffset));
            }
            return new SimpleObjectKey(reader.GetString(columnOffset));
        }

    }


    internal abstract class SmoInternalStorage<T> : IEnumerable<T>
        where T : SqlSmoObject
    {
        protected readonly IComparer keyComparer;
        internal SmoInternalStorage(IComparer keyComparer)
        {
            this.keyComparer = keyComparer;
        }

        internal abstract bool Contains(ObjectKeyBase key);
        internal abstract int LookUp(ObjectKeyBase key);

        internal abstract T this[ObjectKeyBase key]
        {
            get;
            set;
        }

        internal abstract T GetByIndex(int index);

        public abstract int Count { get; }

        internal abstract void Add(ObjectKeyBase key, T o);
        internal abstract void Remove(ObjectKeyBase key);

        internal abstract void InsertAt(int position, T o);
        internal abstract void RemoveAt(int position);

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        internal abstract void Clear();
    }

    
}
