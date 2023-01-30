// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This interface is available on all Sfc collections and is passed to the non-generic SfcInstance class to perform necessary 
    /// collection operations such as child initialization.
    /// </summary>
    public interface ISfcCollection
    {
        string GetCollectionElementNameImpl();
        SfcObjectFactory GetElementFactory();
        SfcInstance Parent { get; }
        // Rename needs a way to really ensure the initialization from the server-side has occured, not
        // just flag the collection to pretend it is like the Initialized property does.
        void EnsureInitialized();
        // Deserialize needs to be able to Set the Initialized state to prevent unwanted Enumerator queries.
        bool Initialized { get; set; }
        int Count { get; }
        void Add(SfcInstance sfcInstance);
        void Remove(SfcInstance sfcInstance);
        void RemoveElement(SfcInstance sfcInstance);
        void Rename(SfcInstance sfcInstance, SfcKey newKey);
        bool GetExisting(SfcKey key, out SfcInstance sfcInstance);
        SfcInstance GetObjectByKey(SfcKey key);
        void PrepareMerge();
        bool AddShadow(SfcInstance sfcInstance);
        void FinishMerge();
    }

    /// <summary>
    /// The Sfc collection base for all domain collections. 
    /// It abstracts all the necesssary handshaking between the parent object, and the collection or element objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="ParentT"></typeparam>
    abstract public class SfcCollection<T, K, ParentT> : ICollection<T>, ICollection, ISfcCollection, IEnumerable<T>, IListSource
        where T : SfcInstance
        where K : SfcKey
        where ParentT : SfcInstance
    {

        #region Constructors
        protected SfcCollection(ParentT parent)
        {
            Parent = parent;
            InitInnerCollection();
        }
        #endregion

        #region Public API

        #region Abstract ICollection<T> Members

        /// <summary>
        ///   Adds the obj to the collection
        /// </summary>
        /// <param name="obj"></param>
        public virtual void Add(T obj)   
        {
            //Note: Ideally we want the derived classes to override only the AddImpl() and not the Add()
            //     However, leaving this method virtual for the backward compatibility -Sivasat


            //handle "Recreate" state
            this.PreprocessObjectForAdd(obj);

            //EnsureCollectionInitialized();

            //overridden by the dervived classes
            // uses the template method pattern
            this.AddImpl(obj);

            // The object must already be parented correctly
            TraceHelper.Assert(obj.Parent == this.Parent);
        }
        
        public abstract void Clear();
        public abstract bool Contains(T obj);
        public abstract void CopyTo(T[] array, int arrayIndex);
        public abstract int Count { get; }
        public abstract bool IsReadOnly { get; }
        public abstract bool Remove(T obj);
        public abstract IEnumerator<T> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

        #endregion


        #region Abstract collection key contains
        public abstract bool Contains(K key);
        #endregion

        #region Public collection key indexing
        public T this[K key]
        {
            get { return GetObjectByKey(key); }
        }
        #endregion

        #region Public collection refresh
        public void Refresh()
        {
            Refresh(false);
        }

        // TODO: work out state management
        public void Refresh(bool refreshChildObjects)
        {
            // TODO: InitializeChildCollection already ends up creating new/merging existing objects and properties
            // so I am unclear what this loop below would then do.
            InitializeChildCollection(true);
            if( refreshChildObjects )
            {
                IEnumerator ienum = this.GetEnumerator();

                if( null != ienum )
                {
                    while ( ienum.MoveNext() )
                    {
                        ((T)ienum.Current).Refresh();
                    }
                }
            }
        }
        #endregion

        #endregion

        #region Abstract protected methods implemented by derived classes

        // Get Urn suffix for elements in this collection. This belongs to TypeDescriptor, which is not yet implemented
        // Use .NET type info period, no overriding.
        // TODO: Change over all other usages like this to use .HNET type info and reflection
        protected virtual string GetCollectionElementNameImpl()
        {
            return typeof(T).Name;
        }

        // Get the factory for the element type. Collections uses the factory to instantiate objects.
        protected abstract SfcObjectFactory GetElementFactoryImpl();     

        // Insert the object into the collection
        //  handles implementation specific things for the ICollection<T>.Add()                
        protected abstract void AddImpl(T obj);

        // Remove the object from the collection
        protected abstract bool RemoveImpl(T obj);

        // Look up the object by key in the collection and init a child object if not found
        protected abstract T GetObjectByKey(K key);

        
        // Look up the object by key in the collection and return null if not found
        protected abstract T GetExistingObjectByKey(K key);

        // (Re)create an empty collection storage
        protected abstract void InitInnerCollection();

        // Add the object into the shadow collection. Only used during PrepareMerge()/FinishMerge() mode.
        protected abstract bool AddShadow(T obj);

        // Prepare for merging fresh query results into the existing collection state. Usually this is by creating a brand new internal
        // collection we will create or merge into, and eventually the current collection will be left behind
        // and this new one will become the inner collection oncer FinishMerge() is called later.
        protected abstract void PrepareMerge();

        // Finish the merge.
        // Swap the new collection for the old one. Each collection impl is responsible for doing whatever is needed
        // internally to put any merged changes/reinits into effect. Usually this is by swapping in a new temporary collection
        // it has been accumulating since PrepareMerge() was called, and deleting the old one.
        // Note that any dropped items are defacto removed by virtue of them not being present in the new collection anyway.
        protected abstract void FinishMerge();

#endregion

#region Private and internal implementation routines

        protected void EnsureCollectionInitialized()
        {
            if( !this.Parent.GetDomain().UseSfcStateManagement() || this.Parent.State == SfcObjectState.Existing || this.Parent.State == SfcObjectState.Recreate)
            {
                // Must check both init flags to prevent reentrancy, since some functions called in the InitializeChildCollection() stack
                // may end up using EnsureCollectionInitialized() as a guard.
                // we also have to not be in disconnected mode, otherwise whatever we have is good.
                if (!m_initialized && !m_initializing &&
                    this.Parent.GetDomain().ConnectionContext.Mode != SfcConnectionContextMode.Offline)
                {
                    InitializeChildCollection();
                }
            }
        }

        internal void AddExisting(T obj)
        {
            AddImpl(obj);
        }

        void InitializeChildCollection()
        {
            InitializeChildCollection(false);
        }

        void InitializeChildCollection(bool refresh)
        {
            // Initialized must not be set until we are done in here, since object population
            // in SfcInstance for the collection child via InitObjectsFromEnumResultsRec()
            // checks it.
            m_initializing = true;

            // VSTS 121305 (and probably other variations): We must avoid the Enumerator when disconnected.
            if (this.Parent != null && this.Parent.KeyChain != null && this.Parent.KeyChain.IsConnected)
            {
                // Prepare for merging fresh query results into the existing collection state. Usually this is by creating a brand new internal
                // collection we will create or merge into, and eventually the current collection will be left behind
                // and this new one will become the inner collection oncer FinishMerge() is called later.
                PrepareMerge();
                this.Parent.InitChildLevel((ISfcCollection)this);
                FinishMerge();
            }

            m_initializing = false;

            if (!m_initialized)
            {
                m_initialized = true;
            }

        }

        // This function instantiates a missing object and adds it to the collection
        // Initialize will throw if the child element Urn does not exist on the server side.
        protected T CreateAndInitializeChildObject(K key)
        {
            // If the domain is not connected do not try to retrieve the object
            // This allows collections to work disconnected.
            if (this.Parent.GetDomain().ConnectionContext.Mode == SfcConnectionContextMode.Offline)
            {
                return null;
            }

            try
            {
                SfcObjectFactory factory = this.GetElementFactory();
                T childobj = factory.Create(this.Parent, key, SfcObjectState.Existing) as T;
                childobj.Initialize();
                this.AddExisting(childobj);
                return childobj;
            }
            catch (SfcObjectInitializationException)
            {
                // it is possible that the child object being asked to create does not exist
                // if that is the case just return null
                return null;
            }
        }

        /// <summary>
        /// Remove the item given by the key.
        /// Existing object are marked with a state of ToBeDropped for dropping when their parent is Altered or Dropped.
        /// At that time the marked object will be removed fro the collection.
        /// Pending, None or Dropped objects are removed from the collection immediately.
        /// </summary>
        /// <param name="obj">The instance to remove.</param>
        /// <returns>If the object is successfully marked to be dropped it returns true, else false.</returns>
        internal protected bool RemoveInternal(T obj)
        {
            bool bFound = false;
            if (this.Contains(obj))
            {
                bFound = true;
                switch (obj.State)
                {
                    case SfcObjectState.Existing:
                    case SfcObjectState.Recreate:
                        //mark it for dropping on the back end
                        obj.State = SfcObjectState.ToBeDropped;
                        break;
                    case SfcObjectState.ToBeDropped:                    
                        // Nothing to do, it's already marked ToBeDropped
                        break;
                    default:
                        // All other states (pending, none, dropped) just remove it completely right now
                        obj.State = SfcObjectState.Dropped;
                        bFound = RemoveImpl(obj);
                        break;
                }
            }
            return bFound;
        }

        /// <summary>
        ///   Handles the preprocessing for the Add operation on the collection:
        ///     If the collection has a duplicate item in the ToBeDropped state, removes it from the collection
        ///     and changes the state of obj to "Recreate"
        /// </summary>
        /// <param name="obj"></param>
        internal void PreprocessObjectForAdd(T obj)
        {            
                T t = this.GetExistingObjectByKey(obj.AbstractIdentityKey as K);
                if (t != null && t.State == SfcObjectState.ToBeDropped)
                {
                    this.RemoveImpl(t);
                    t.State = SfcObjectState.Dropped; //the old object may no longer be in a valid state
                    obj.State = SfcObjectState.Recreate;
                }            
        }



        internal protected void Rename(T obj, K newKey)
        {
            // This guard keeps us cohesive inside this function, but you better be doing this even earlier before the server-side updates
            // if you want the initialize reuslts to match what you want to pretend you have on the client-side until you
            // are done renaming keys. ANother words, you do not want to perform a server query after you already updated the server-side
            // but before you have done proper client-side housekeeping in here.
            EnsureCollectionInitialized();

            // The object must exist in the collection
            if (!this.Contains(obj))
            {
                throw new SfcInvalidRenameException(SfcStrings.KeyNotFound((obj.AbstractIdentityKey as K).ToString()));
            }

            // The new key must not already be in collection
            if (this.Contains(newKey))
            {
                throw new SfcInvalidRenameException(SfcStrings.KeyExists((obj.AbstractIdentityKey as K).ToString()));
            }

            // Set all key properties in the objects from their values present in the new key
            SfcKeyChain newKeyChain = new SfcKeyChain(newKey, this.Parent.KeyChain.Parent);
            Urn newUrn = newKeyChain.Urn;
            XPathExpressionBlock leafBlock = newUrn.XPathExpression[newUrn.XPathExpression.Length - 1];

            // TODO: Need SfcMetadata to tell me the names of the key properties for this.GetType()
            string[] keyProps = new string[] { "Schema", "Name" };
            int keysFound = 0;

            SfcProperty keyProp;
            foreach (string keyPropName in keyProps)
            {
                keyProp = null;
                try
                {
                    keyProp = obj.Properties[keyPropName];
                    TraceHelper.Assert(keyProp != null);
                }
                catch
                {
                    // TODO: Enable this throw when we have SfcMetadata really telling us the key prop names
                    //throw new SSfcInvalidRenameException(SfcStrings.CannotRenameMissingProperty(this.Parent, keyPropName));
                    continue;
                }

                // This should never throw, if it does it means the Urn doesn't have all the right key property attributes in it
                keysFound++;
                keyProp.Value = leafBlock.GetAttributeFromFilter(keyProp.Name);
            }

            // Can't rename something with no key properties
            if (keysFound == 0)
            {
                throw new SfcInvalidRenameException(SfcStrings.CannotRenameNoProperties(this.Parent));
            }

            // Raw remove from collection (will not involve mark for drop, etc.)
            this.RemoveImpl(obj);

            // Regenerate the element's key by clearing it so it gets rebuilt on next access to it
            // $ISSUE: We cannot use ResetKey since we must directly mutate the m_ThisKey field inside the keychain.
            // This is due to how we currently effect a change in all descendants without having to visit them.
            // This strategy mey need to be changed if we see that not all descendants are pointing at the *exact* same instance of our keychain.
            //obj.ResetKey();
            obj.KeyChain.LeafKey = newKey;
            obj.AbstractIdentityKey = obj.CreateIdentityKey();


            // Raw add back into collection
            this.AddImpl(obj);
        }


        internal SfcObjectFactory GetElementFactory()
        {
            return GetElementFactoryImpl();
        }

        ParentT m_parent = null;
        internal protected ParentT Parent
        {
            get{ return m_parent; }
            set{ m_parent = value; }
        }

        bool m_initialized = false;
        bool m_initializing = false;

        // All collections that derive from the SfcCollection need to set this property in the constructor
        // As well other services within SFC may need access to this property.
        // Deserialize uses this up front to prevent any Enumerator calls.
        // TODO: The generic collection base may not need this at all (if we own the state of the collection).
        protected internal bool Initialized
        {
            get { return m_initialized; }
            set { m_initialized = value; }
        }

#endregion


        #region ISfcCollection Members

        string ISfcCollection.GetCollectionElementNameImpl()
        {
            return this.GetCollectionElementNameImpl();
        }

        SfcObjectFactory ISfcCollection.GetElementFactory()
        {
            return this.GetElementFactory();
        }

        SfcInstance ISfcCollection.Parent
        {
            get { return this.Parent as SfcInstance; }
        }

        void ISfcCollection.EnsureInitialized()
        {
            this.EnsureCollectionInitialized();
        }

        bool ISfcCollection.Initialized
        {
            get { return this.Initialized; }
            set { if (value)
                {
                    this.Initialized = value;
                }
            }
        }

        int ISfcCollection.Count
        {
            get { return this.Count; }
        }

        void ISfcCollection.Add(SfcInstance sfcInstance)
        {
            this.Add(sfcInstance as T);
        }

        void ISfcCollection.Remove(SfcInstance sfcInstance)
        {
            this.Remove(sfcInstance as T);
        }

        void ISfcCollection.RemoveElement(SfcInstance sfcInstance)
        {
            // Raw remove of element from internal collection, used by CRUD
            this.RemoveImpl(sfcInstance as T);
        }

        void ISfcCollection.Rename(SfcInstance sfcInstance, SfcKey newKey)
        {
            this.Rename(sfcInstance as T, newKey as K);
        }

        bool ISfcCollection.GetExisting(SfcKey key, out SfcInstance sfcInstance)
        {
            sfcInstance = this.GetExistingObjectByKey(key as K);
            return (object)sfcInstance != null;
        }

        SfcInstance ISfcCollection.GetObjectByKey(SfcKey key)
        {
            return GetObjectByKey(key as K);
        }

        void ISfcCollection.PrepareMerge()
        {
            this.PrepareMerge();
        }

        bool ISfcCollection.AddShadow(SfcInstance sfcInstance)
        {
            return this.AddShadow(sfcInstance as T);
        }

        void ISfcCollection.FinishMerge()
        {
            this.FinishMerge();
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            this.CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            set
            {
                //This class doesn't support syncronization, the correct fix is to remove the setter altogether, but due
                //to the fact that KJ assemblies carry the same version as Katmai ones, removes this will be changing the public
                //interface, and will be breaking applications that use Katmai assemblies because due to the same version, they 
                //will be using KJ without any knowledge of that.
                TraceHelper.Assert(false, "Setting IsSynchronized property is not supported");
            }
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            set
            {
                //This class doesn't support SyncRoot changing, the correct fix is to remove the setter altogether, but due
                //to the fact that KJ assemblies carry the same version as Katmai ones, removes this will be changing the public
                //interface, and will be breaking applications that use Katmai assemblies because due to the same version, they 
                //will be using KJ without any knowledge of that.
                TraceHelper.Assert(false, "Setting SyncRoot property is not supported");
            }
            get
            {
                return this;
            }
        }
        #endregion

        #region IListSource support

        bool IListSource.ContainsListCollection
        {
            get { return false; }
        }

        IList IListSource.GetList()
        {
            T[] list = new T[this.Count]; // access to .Count will ensure collection is initialized
            this.CopyTo(list,0);
            return list;
        }

        #endregion

    }

}
