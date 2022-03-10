// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    // This enum represents the state in which an object
    public enum SfcObjectState
    {
        /// <summary>
        ///   the object not yet set or unknown. It is just there as a completeness enum value. No code has ever seem to have used it (in SFC or externally)
        /// </summary>
        None,

        /// <summary>
        ///   the object is just instantiated, not yet committed to the backend.
        /// </summary>
        Pending,

        /// <summary>
        ///  the object corresponds to an existing object in the backend.
        /// </summary>
        Existing,

        /// <summary>
        ///   the object is dropped in the backend and/or is no longer in a valid state.
        /// </summary>
        Dropped,

        /// <summary>
        ///   the Object is marked for Deletion. Actual deletion is deferred till a commit operation like Create() etc. is called.  Added for DC support.
        /// </summary>
        ToBeDropped,

        
        /// <summary>
        ///  the object is to be dropped and then created again. Required to enable dropping/adding again the same object to the collections.
        /// </summary>
        Recreate
    }

    // This interface is used to populate object's property bag during creation. In SFC, only ObjectFactory knows how
    // create objects correctly, setting Parent, SfcKeyChain etc. The rest of the SFC cannot instantiate objects. Instead,
    // they implement IPropertyCollectionPopulator and pass it to ObjectFactory during creation.
    interface IPropertyCollectionPopulator
    {
        void Populate(SfcPropertyCollection properties);
    }

    // TODO: Put this in a better location as a public interface
    /// <summary>
    /// The ISfcDiscoverObject interface is implemented on all SfcInstanced-derived objects and is used to ask an individual object 
    /// to report all known relationships in or out of the object for the dependency discovery engine. The object will automatically be marked 
    /// as discovered by the dependency system upon return.
    /// </summary>
    public interface ISfcDiscoverObject
    {
        /// <summary>
        /// Allow the object to indicate relationships for dependency discovery requests via the passed sink.
        /// </summary>
        /// <param name="sink"></param>
        void Discover(ISfcDependencyDiscoveryObjectSink sink);
    }

    /// <summary>
    /// The base class for all Sfc object types in a domain.</summary>
    abstract public class SfcInstance : ISfcDiscoverObject, ISfcPropertyProvider
    {

        #region Public API
        // TODO: I need to figure out a way by which I can enforce a public default constructor
        // on all the classes through the need of serialization

        /// <summary>
        /// Create a new Urn string on each request and return it.
        /// </summary>
        public Urn Urn
        {
            get { return (null == KeyChain) ? null : KeyChain.Urn; }
        }

        /// <summary>
        /// Returns the state of the object
        /// </summary>
        SfcObjectState m_state = SfcObjectState.Pending;
        protected internal SfcObjectState State
        {
            get { return m_state; }
            internal set { m_state = value; }
        }

        [System.Xml.Serialization.XmlIgnore()]
        public SfcPropertyCollection Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new SfcPropertyCollection(new PropertyDataDispatcher(this));
                }

                return m_properties;
            }
        }

        [System.Xml.Serialization.XmlIgnore()]
        public virtual SfcMetadataDiscovery Metadata
        {
            get
            {
                return new SfcMetadataDiscovery(this.GetType());
            }
        }

        // Initializes the object, by reading its properties from the enumerator
        internal void Initialize()
        {
            // Guard disconnected state from attempting to hit Enumerator
            if (this.GetConnectionContextMode() == SfcConnectionContextMode.Offline)
            {
                return;
            }

            // Let's see what's supported by this type
            // TODO: we should cache this data
            ResultType[] resultTypesSupported = GetSupportedResultTypes(this.GetConnectionContext(), this.Urn);

            bool supportsDataReader = false;

            foreach (ResultType result in resultTypesSupported)
            {
                if (result == ResultType.IDataReader)
                {
                    supportsDataReader = true;
                }
            }

            if( this.Properties.Count == 0 )
            {
                // VSTS:146996. Object has no properties, don't bother running a query. This is very common
                // for root objects like PolicyStore, thus it deserves a special case.
                return;
            }

            if (supportsDataReader)
            {
                // TODO: provide fields and orderby -- currently both are nulls (in SMO: GetServerObject().GetDefaultInitFieldsInternal(childType)), GetOrderByList(childType)))
                using (IDataReader reader = GetInitDataReader(this.GetConnectionContext(), this.Urn, null, null))
                {
                    if (!reader.Read())
                    {
                        reader.Close();
                        // Failed to Initialize urn
                        // This means we don't have a server-side match and should throw so the collection client will not accidentally
                        // add a non-existent element into the collection.
                        throw (new SfcObjectInitializationException(this.Urn));
                    }

                    FillPropertyCollectionFromDataReader(this.Properties,reader);
                }
                return;
            }

            // Enumerator object does not support any of the return types.
            // Note: if you get this, see change in this file made by arturl in Jan/2008
            throw new SfcObjectInitializationException(this.Urn);

        }

        ///<summary>
        /// refreshes the object's properties by reading them from the server
        ///</summary>
        public virtual void Refresh()
        {
            if (this.State == SfcObjectState.Recreate)
            {
                this.State = SfcObjectState.Existing;
            }

            CheckObjectCreated();

            // if we aren't in a dropped state we might really have been dropped 
            // on the server

            // No fancy error-handling here. An error here means the object is in bad state, user error
            Initialize();
        }

        public override string ToString()
        {
            return this.AbstractIdentityKey.ToString();
        }

        public void Serialize(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException ("writer");
            }

            SfcSerializer serializer = new SfcSerializer();
            serializer.Serialize(this);
            serializer.Write(writer);
        }
        #endregion

        #region ISfcPropertyProvider Methods and Helpers
        [CLSCompliant(false)]
        protected event EventHandler<SfcPropertyMetadataChangedEventArgs> propertyMetadataChanged;
        [CLSCompliant(false)]
        protected event PropertyChangedEventHandler propertyChanged;

        public virtual ISfcPropertySet GetPropertySet()
        {
            return this.Properties;
        }
        public event EventHandler<SfcPropertyMetadataChangedEventArgs> PropertyMetadataChanged
        {
            add
            {
                propertyMetadataChanged += value;
                //Initialize the states as the user is interested in knowing the states
                InitializeUIPropertyState();
            }
            remove
            {
                propertyMetadataChanged -= value;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                propertyChanged += value;
            }
            remove
            {
                propertyChanged -= value;
            }
        }

        //Prevent un-nessesary calls if no one registered to the events or no one is using the "Enabled" property
        //So don't reserve the m_bEnabled array until it's really needed
        internal void InternalOnPropertyValueChanges(PropertyChangedEventArgs args)
        {
            if (propertyChanged != null || this.Properties.DynamicMetaDataEnabled)
            {
                UpdateUIPropertyState();
                OnPropertyValueChanges(args);
            }
        }

        //Prevent un-nessesary calls if no one registered to the events.
        internal void InternalOnPropertyMetadataChanges(SfcPropertyMetadataChangedEventArgs args)
        {
            if (propertyMetadataChanged != null)
            {
                OnPropertyMetadataChanges(args);
            }
        }

        internal protected virtual void OnPropertyValueChanges(PropertyChangedEventArgs args)
        {
            if (propertyChanged != null)
            {
                propertyChanged(this, args);
            }
        }

        internal protected virtual void OnPropertyMetadataChanges(SfcPropertyMetadataChangedEventArgs args)
        {
            if (propertyMetadataChanged != null)
            {
                propertyMetadataChanged(this, args);
            }
        }
        #endregion

        #region Abstract methods implemented by derived classes

        /// <summary>
        /// Get the child collection in this instance for the given element name string.
        /// </summary>
        /// <param name="elementType">The element name string. Note that it is singular not plural like the collection name often is.</param>
        /// <returns>The child collection instance.</returns>
        protected internal abstract ISfcCollection GetChildCollection(string elementType);

        ISfcPropertyStorageProvider propertiesStorage = null;
        /// <summary>
        /// This property returns the default implementation of SFC for ISfcPropertyStorageProvider interface, it can be overriden in the
        /// child classes to return another storage provider (i.e. flat properties list)
        /// </summary>
        protected virtual ISfcPropertyStorageProvider PropertyStorageProvider
        {
            get
            {
                if (propertiesStorage == null)
                {
                    propertiesStorage = new SfcDefaultStorage(this);
                }
                return propertiesStorage;
            }
        }
        // The lowest level getter and setter of property values. These methods are called only from
        // SfcPropertyCollection, never by the user (thus protected)
        internal object GetPropertyValueImpl(string propertyName)
        {
            return PropertyStorageProvider.GetPropertyValueImpl(propertyName);
        }
        internal void SetPropertyValueImpl(string propertyName, object value)
        {
            PropertyStorageProvider.SetPropertyValueImpl(propertyName, value);
        }

        // Each class knows how to create its own Key.
        protected internal abstract SfcKey CreateIdentityKey();

        #endregion

        #region Virtual Protected Methods
        /// <summary>
        /// Basic child object's validation
        /// </summary>
        /// <returns>The validation state of the object to be validated</returns>
        protected virtual ValidationState Validate()
        {
            ValidationState state = new ValidationState();
            foreach (SfcProperty property in Properties)
            {
                if (property.Required)
                {
                    Exception exception = null;
                    object value = null;
                    string message = null;

                    //Try to read the value
                    value = property.Value;
                    //If the value is null or DBNull then we have an error of value is not set
                    if (value == null || value == DBNull.Value)
                    {
                        message = SfcStrings.PropertyNotSet(property.Name);
                        state.AddError(message, exception, property.Name);
                    }
                }
            }
            return state;
        }

        /// <summary>
        /// Overridable from the child objects who care about initializing their states (dynamic metadata which is currently
        /// the ".Enabled" property)
        /// </summary>
        internal protected virtual void InitializeUIPropertyState()
        {
        }

        protected virtual void UpdateUIPropertyState()
        {
        }
        #endregion

        #region Private and internal implementation routines

        internal protected void ResetKey()
        {
            m_key = this.CreateIdentityKey();
            // $ISSUE: If this is being used for a deep rename, we have a problem. This will not affect descendant keychains.
            // YOu either have to (a) visit every descendant and explicitly force re-evaluate each one's keychain by
            // setting them all to null like we do here, or (b) mutate the internal m_keychain.m_ThisKey much like we do
            // for Move's that mutate the internal m_keychain.m_Parent.
            // (b) is dangerous, see MoveImpl() for more details that also apply to Rename.
            m_keychain = null; // force re-evaluation
        }

        // This should be a strongly-typed XXX.Key if SfcInstance were a generic base class.
        // For now, manually implement a derived class strongly-typed public property called Key IdentityKey.
        private SfcKey m_key = null;
        protected internal SfcKey AbstractIdentityKey
        {
            get
            {
                if (m_key == null)
                {                    
                    m_key = this.CreateIdentityKey();
                    // if we got a key it must hash
                    if (m_key == null)
                    {
                        // This would be an InvalidKey exception but we don't want to throw here.
                        return null;
                    }

                    // there is no real check here other than to try and get a hashcode. Every key must hash.
                    // Note: Removed TraceHelper.Assert(m_key.GetHashCode() != 0), since 0 is a perfectly valid hash value!
                }
                return m_key;
            }
            set
            {
                m_key = value;
            }
        }


        private SfcKeyChain m_keychain;

        /// <summary>
        /// Returns the identity path of the object
        /// </summary>
        [System.Xml.Serialization.XmlIgnore()]
        public SfcKeyChain KeyChain
        {
            get
            {
                // if we don't have a keychain yet we might be able to make it
                // we can make a keychain if we have a parent, or are the root, and we have a key
                if (m_keychain == null)
                {
                    // We have to have a key or we can't continue.
                    // Likely from a domain type's inability to CreateIdentityKey() due to missing key data.
                     if (this.AbstractIdentityKey == null)
                     {
                         return null;
                     }

                    // if we are a domain root we have a special keychain constructor
                    if (this is ISfcDomain)
                    {
                        // Better not have a parent if we are the root
                        TraceHelper.Assert(m_parent == null);

                        // This would be an InvalidCast exception but we don't want to throw, just return null.
                        // The key from a ISfcDomain root object should always be a DomainRootKey.
                        DomainRootKey rootKey = this.AbstractIdentityKey as DomainRootKey;
                        if (rootKey == null)
                        {
                            return null;
                        }
                        m_keychain = new SfcKeyChain(rootKey);
                    }
                    // if we already have a parent and it has a SfcKeyChain then we can make a keychain
                    else if (m_parent != null && m_parent.KeyChain != null)
                    {
                        m_keychain = new SfcKeyChain(this.AbstractIdentityKey, m_parent.KeyChain);
                    }
                    // otherwise we fall out and m_keychain is still null,
                    // we don't have enough to make one yet
                }

                return m_keychain;
            }
            internal set // setting a keychain is now done in very few places and should be avoided altogether
            {
                TraceHelper.Assert(value != null); // don't try setting a null keychain

                // When we set the keychain we have to make sure that we aren't stepping on a 
                // preset parent 
                if (m_parent != null)
                {
                    if (value.Parent.GetObject() != Parent)
                    {
                        throw new SfcInvalidKeyChainException();
                    }
                }

                // if we have a key we shouldn't be setting the keychain
                if (m_key != null)
                {
                    throw new InvalidOperationException(SfcStrings.KeyAlreadySet);
                }
                // if we make it here, we are good to go
                m_keychain = value; 
            }
        }

        SfcInstance m_parent;
        /// <summary>
        /// Parent is not something that is kept local, it is implied from the keychain
        /// The concept of a parent is really about the hiearchy up and under the root
        /// if we only do this via parents then we will not be able to instantiate objects with
        /// only lightweight SfcKeyChain sets
        /// 
        /// Setting the parent is therefor a helper operation to set a keychain
        /// </summary>
        [System.Xml.Serialization.XmlIgnore()]
        public SfcInstance Parent
        {
            // getting the parent is really moving up the SfcKeyChain and getting the parent 
            // from there. We do cache the value for effiency
            get
            {
                // if the parent is null see if we can go make it
                if (m_parent == null)
                {
                    // we can only get the parent if we have a keychain, otherwise we aren't really parented
                    if (this.KeyChain == null)
                    {
                        return null;
                    }

                    // The parent Keychain will be null if we are already a domain root instance 
                    SfcKeyChain kc = this.KeyChain.Parent;
                    if (kc != null)
                    {
                        m_parent = kc.GetObject();
                    }
                }
                return m_parent;
            }
            // Setter is not available to domain clients, but domains can expose it.
            // SFC only calls it from ObjectFactory (and from Serlializer, but it will be fixed)
            protected internal set
            {
                // setting a parent can only be done if a keychain does not exist yet
                // since it can be used in making a keychain.

                // Setting the parent if the keychain is set will be allowed if the parent of the 
                // keychain is the same as the parent being passed in
                if (m_keychain != null)
                {
                    if (State != SfcObjectState.Pending)
                    {
                        // we don't have a cache yet so if we compare parent objects
                        // it will fail. We can compare keychains though
                        if (m_keychain.Parent != value.KeyChain)
                        {
                            throw new InvalidOperationException(SfcStrings.KeyChainAlreadySet);
                        }
                    }
                    else
                    {
                        // Only pending objects are allowed to change a parent that's already set.
                        // We may re-visit for Rename/Move Event generation purposes

                        m_keychain.Parent = value.KeyChain;
                    }
                }


                //Ideally, we would want to disallow setting a root-level parent without a connection
                //But a deserialized root (being an ISfcDomain) does not have a connection. So, we don't check for that condition

                m_parent = value;

            }
        }

        ISfcConnection GetConnectionContext()
        {
            // drill down to the root and get the connection from it
            return ((ISfcHasConnection)this.KeyChain.Domain).GetConnection();
        }

        SfcConnectionContextMode GetConnectionContextMode()
        {
            // drill down to the root and get the connection context mode from it
            return ((ISfcHasConnection)this.KeyChain.Domain).ConnectionContext.Mode;
        }

        SfcPropertyCollection m_properties;

        #region Object Query stuff

        // Populates the object's property bag from the current row of the DataReader
        // Arguably this could be a member of SfcPropertyCollection, but I don't want to make properties aware of DataReaders
        static private void FillPropertyCollectionFromDataReader( SfcPropertyCollection properties, IDataReader reader )
        {
            DataTable schemaTable = reader.GetSchemaTable();
            int colNameIdx = schemaTable.Columns.IndexOf("ColumnName");

            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                string columnName = schemaTable.Rows[i][colNameIdx] as string;

                int propIndex = properties.LookupID(columnName);

                if (propIndex >= 0)
                {
                    SfcProperty property = properties[columnName];
                    property.Retrieved = true;
                    object colValue = reader.GetValue(i);
                    property.Value = colValue;
                    property.Dirty = false;
                }
            }
        }

        // This populator populates objects from IDataReader. It is important that this class is private in SfcInstance,
        // and is only exposed outside via IPropertyCollectionPopulator interface. This ensures that only SfcInstance can instantiate
        // it so it can be passed to the factory for creation.
        // Serialization will use a similar class to populate object from XML but delegate the rest of object creation
        // mechanics to the factory.
        internal class PopulatorFromDataReader : IPropertyCollectionPopulator
        {
            IDataReader m_reader;

            public PopulatorFromDataReader(IDataReader reader)
            {
                m_reader = reader;
            }

            void IPropertyCollectionPopulator.Populate(SfcPropertyCollection properties)
            {
                FillPropertyCollectionFromDataReader(properties,m_reader);
            }
        }

        // Merges the object's property bag with the given property collection
        // TODO: This is a temporary solution in v1 since we really just want to merge the properties directly from the enum results grid.
        internal void MergeObjectPropsFromPropertyCollection(SfcPropertyCollection mergeProperties)
        {
            foreach (SfcProperty property in mergeProperties)
            {
                // it is possible that the property already exists and has been modified
                // if that is the case don't update it
                if (this.Properties[property.Name].Dirty != true)
                {
                    this.Properties[property.Name].Retrieved = true;
                    this.Properties[property.Name].Value = mergeProperties[property.Name].Value;
                    // Reset the dirty bit since a side effect of the Value assignment above is to set it
                    this.Properties[property.Name].Dirty = false;
                }
            }
        }

        private static IDataReader GetInitDataReader(ISfcConnection connection, Urn urn, string[] fields, OrderBy[] orderby)
        {
            Request request = new Request(urn);
            request.ResultType = ResultType.IDataReader;
            request.Fields = fields;
            request.OrderByList = orderby;

            IDataReader reader = EnumResult.ConvertToDataReader(Enumerator.GetData(connection.ToEnumeratorObject(), request));

            return reader;
        }

        /// <summary>
        /// Returns the result types supported by the URN level
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="urn"></param>
        /// <returns></returns>
        private static ResultType[] GetSupportedResultTypes(ISfcConnection connection, Urn urn)
        {
            // We want to know about the result
            RequestObjectInfo req = new RequestObjectInfo(urn, RequestObjectInfo.Flags.ResultTypes);
            // Instantiate an Enumerator.
            Enumerator enumerator = new Enumerator();
            // Request the metadata about this object
            ObjectInfo info = enumerator.Process(connection.ToEnumeratorObject(), req);
            return info.ResultTypes;
        }

        /// <summary>
        /// Initialize a referential collection from the containing parent object via the regular initialization mechanism.
        /// </summary>
        internal void InitReferenceLevel(ISfcCollection refColl)
        {
            // execute the query
            Urn levelFilter = refColl.GetCollectionElementNameImpl();
            Urn urn = new Urn(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", this.Urn, levelFilter));
            // TODO: provide fields and orderby -- currently both are nulls (in SMO: GetServerObject().GetDefaultInitFieldsInternal(childType)), GetOrderByList(childType)))
            using (IDataReader reader = GetInitDataReader(this.GetConnectionContext(), urn, null, null))
            {
                // init all child objects from the query results
                InitObjectsFromEnumResults(refColl, reader);
            }
        }

        /// <summary>
        /// This function that receives a generic urn initializes for scripting all the objects of a 
        /// certain type that are returned by a query with the urn, e.g. for an urn that ends in 
        /// /Table/Column the function would initialize all the columns from all the tables in that
        /// database. The enumerator currently does not support this feature
        /// accross databases, but we do not need this for transfer anyway
        /// The function can also initialize a child collection via the regular initialization mechanism 
        /// </summary>
        internal void InitChildLevel(ISfcCollection childColl)
        {
            // execute the query
            Urn levelFilter = childColl.GetCollectionElementNameImpl();
            Urn urn = new Urn(string.Format("{0}/{1}", this.Urn, levelFilter));
            // TODO: provide fields and orderby -- currently both are nulls (in SMO: GetServerObject().GetDefaultInitFieldsInternal(childType)), GetOrderByList(childType)))
            using (IDataReader reader = GetInitDataReader(this.GetConnectionContext(), urn, null, null))
            {
                // init all child objects from the query results
                InitObjectsFromEnumResults(childColl, reader);
            }
        }

        private void InitObjectsFromEnumResults(ISfcCollection childColl, IDataReader reader)
        {
            if (!reader.Read())
            {
                reader.Close();
                return;
            }

            // if all fields are null, the enum result is invalid, since at least the key field can't be null.
            bool allFieldsNull = true;
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                if (!(reader[i] is DBNull))
                {
                    allFieldsNull = false;
                    break;
                }
            }
            if (allFieldsNull)
            {
                reader.Close();
                return;
            }

            object[] parentRow = new object[reader.FieldCount];
            reader.GetValues(parentRow);
            InitObjectsFromEnumResultsRec(childColl, reader, 0, parentRow);
        }

        /// <summary>
        /// the general philosophy of this function is like this:
        /// we are doing a query that returns a potentially large table, and we would 
        /// like to minimize the lookups in the collections; we have arranged 
        /// for the results to be ordered in the same order as our collections, 
        /// so dumping the results into the objects' properties becomes something 
        /// like a merge operation between two data sets that have the same ordering
        /// </summary>
        private void InitObjectsFromEnumResultsRec(
            ISfcCollection childColl,
            IDataReader reader,
            int columnIdx,
            object[] parentRow)
        {
            // have we finished the table already?
            TraceHelper.Assert(!reader.IsClosed);

            // Check for merging via key only if collection is not empty,
            // otherwise assume we can just add new items in via the reader.
            // Obviously this would need to always check for potential merging if we cannot guarantee enum results to be unique.
            SfcInstance currObj = null;
            if (childColl.Count > 0)
            {
                // TODO: merge existing initialized collection with the data from enumerator,
                // carefully following SMO semantics
                // return if there are no more rows
                while (CompareRows(reader, parentRow, 0, columnIdx))
                {
                    currObj = MergeOrCreateObjectFromRow(childColl, reader);

                    if (!reader.Read())
                    {
                        reader.Close();
                        return;
                    }
                }
            }
            else
            {
                while (CompareRows(reader, parentRow, 0, columnIdx))
                {
                    // Here SMO tries to look up the object in the uninitilized collection
                    // based on NoFaultCount flag. This should not be applicable to SFC

                    currObj = CreateNewObjectFromRow(childColl, reader);

                    if (!reader.Read())
                    {
                        reader.Close();
                        return;
                    }
                }
            }
        }

        private static SfcInstance MergeOrCreateObjectFromRow(ISfcCollection childColl, IDataReader reader)
        {
            // TODO: This temporary solution goes away in v2 when we have a real static metadata factory hanger for asking for the key
            // of a SfcType to be constructed from a property set so we don't need to construct a dummy instance first.
            SfcInstance dummyObj = childColl.GetElementFactory().Create(childColl.Parent,new PopulatorFromDataReader(reader),SfcObjectState.Existing);

            SfcInstance currObj;

            // Merge properties into the existing object reference if it already exists in the current incarnation of the inner collection
            if (childColl.GetExisting(dummyObj.AbstractIdentityKey, out currObj))
            {
                // The object already exists in the collection so merge the properties from the dummy instance
                currObj.MergeObjectPropsFromPropertyCollection(dummyObj.Properties);
            }
            else
            {
                // The object truly is new and is added to the collection as-is
                currObj = dummyObj;

                // // SMO: update state
                // ((SqlSmoObject)currObj).SetState(PropertyBagState.Lazy);
            }

            // Whether we have an existing object that was already in the collection, or a brand new one, we need to now
            // add it to the shadow copy collection we are building to replace the current collection.
            childColl.AddShadow(currObj);

            return currObj;
        }

        private static SfcInstance CreateNewObjectFromRow(ISfcCollection childColl, IDataReader reader)
        {
            SfcInstance currObj = childColl.GetElementFactory().Create(childColl.Parent, new PopulatorFromDataReader(reader),SfcObjectState.Existing);

            // Whether we have an existing object that was already in the collection, or a brand new one, we need to now
            // add it to the shadow copy collection we are building to replace the current collection.
            childColl.AddShadow(currObj);

            return currObj;
        }

        /// <summary>
        /// move one row down if we are on the last level of the Urn
        /// otherwise it move one level to the right into the Urn
        /// </summary>
        /// <returns>true if there are still records to read </returns>
        private bool AdvanceInitRec(
            IDataReader reader,
            int columnIdx)
        {
            // set the object's properties from the current row
            if (reader.FieldCount - columnIdx > 1)
            {
                FillPropertyCollectionFromDataReader(this.Properties,reader);
            }

            // move forward in the result table
            if (!reader.Read())
            {
                reader.Close();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the current row of the reader and the parent row are equal 
        /// up to the specified column
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="parentRow"></param>
        /// <param name="columnStartIdx"></param>
        /// <param name="columnStopIdx"></param>
        /// <returns></returns>
        private static bool CompareRows(IDataReader reader, object[] parentRow, int columnStartIdx, int columnStopIdx)
        {
            TraceHelper.Assert(!reader.IsClosed);

            for (int i = columnStartIdx; i < columnStopIdx; i++)
            {
                if (!reader.GetValue(i).Equals(parentRow[i]))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #endregion

        #region ISfcDiscoverObject Members

        // Default impl of Discover method. Classes that don't override this method won't be asked to provide their dep. information
        public virtual void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            return;
        }

        #endregion

        public ISfcDomain GetDomain()
        {
            SfcInstance root = this;
            while( root.Parent != null )
            {
                root = root.Parent;
            }

            ISfcDomain domain = root as ISfcDomain;
            if( domain == null )
            {
                throw new SfcMissingParentException(SfcStrings.MissingParent);
            }

            return domain;
        }

        // Domain types can override by returning the right TypeMetadata class directly
        protected internal virtual SfcTypeMetadata GetTypeMetadataImpl()
        {
            return this.GetDomain().GetTypeMetadata(this.GetType().Name);
        }

#region Script-CRUD support

        // This is called to "light up" the root object when it receives a connection
        protected void MarkRootAsConnected()
        {
            if( !this.GetDomain().UseSfcStateManagement() )
            {
                return;
            }

            TraceHelper.Assert(this is ISfcDomain);  // not supposed to be used for other things
            this.State = SfcObjectState.Existing;
            this.Initialize();
        }

        private ISfcCollection GetParentCollection()
        {
            return this.Parent.GetChildCollection(this.GetType().Name);
        }

        /// <summary>
        /// To be called from domain for any access to the object
        /// </summary>
        protected void CheckObjectState()
        {
            if( !this.GetDomain().UseSfcStateManagement() )
            {
                return; // this domain doesn't want SFC state management
            }

            //This check is needed for Deserialization: Root of the deserialized tree might not be able to access parent
            TraceHelper.Assert(this.KeyChain != null);

            if (this.State == SfcObjectState.Dropped)
            {
                    throw new SfcInvalidStateException(SfcStrings.InvalidState(this.State, SfcObjectState.Existing));
            }
        }

        /// <summary>
        /// To be called from domain for when an API requires the object to be Created
        /// Stronger than CheckObjectState
        /// </summary>
        protected void CheckObjectCreated()
        {
            CheckObjectState();
            CheckObjectStateAndParent(SfcObjectState.Existing);
        }

        private void CheckObjectStateAndParent( SfcObjectState required_state )
        {
            // Unreachable, because object with no parent set throws above in GetDomain
            if( this.Parent == null && !(this is ISfcDomain) )
            {
                throw new SfcMissingParentException(SfcStrings.MissingParent);
            }

            if( !this.GetDomain().UseSfcStateManagement() )
            {
                return; // this domain doesn't want SFC state management
            }

            if( this.State != required_state )
            {
                throw new SfcInvalidStateException(SfcStrings.InvalidState(this.State, required_state));
            }
        }

        private List<SfcInstance> GetDependentObjects( SfcDependencyAction action )
        {
            SfcDependencyEngine dependencyEngine = new SfcDependencyEngine(SfcDependencyDiscoveryMode.Children, action);
            SfcDependencyRootList dependencyRootList = new SfcDependencyRootList();
            dependencyRootList.Add(this);
            dependencyEngine.SfcDependencyRootList = dependencyRootList;
            dependencyEngine.Discover();

            SfcDependencyEngine.DependencyListEnumerator dependencyIterator = new SfcDependencyEngine.DependencyListEnumerator(dependencyEngine);
            List<SfcInstance> list = new List<SfcInstance>();
            foreach (SfcDependencyNode depNode in dependencyIterator)
            {
                list.Add(depNode.Instance);
            }
            dependencyIterator.Dispose();

            return list;
        }

        // Perform some post-CRUD action given the result of execution
        // The order is:
        // 0. Ping UI (virtual)
        // 1. Fire BeforeAction housekeeping event (Rename and Move only)
        // 2. Perform Sfc housekeeping
        // 3. Perform User housekeeping (virtual)
        // 4. Fire AfterAction housekeeping event (Rename and Move only)
        private void PostCrud(SfcDependencyAction depAction, SfcKeyChain oldKeyChain, object extraParam, object executionResult)
        {
            UpdateUIPropertyState();
            switch(depAction)
            {
                case SfcDependencyAction.Create:
                    {
                        // User housekeeping
                        // $ISSUE: This mimics the order we had before moving all this followign housekeeping down
                        // into this level in PostCRUD processing. DMF depends on this being before SFC's housekeeping
                        // so it can get the ID.
                        // We may have to add POstBEforeCreate and PostAfterCreate similar to events if anyone
                        // ever needs the "other side" of the hooking.
                        PostCreate(executionResult);

                        // Sfc Housekeeping
                        ISfcCollection collection = GetParentCollection();
                        SfcInstance existingObj;
                        if (collection.GetExisting(this.AbstractIdentityKey, out existingObj))
                        {
                            // This is temporary solution to support DMF's deep creation. The object already exists, don't add it to the collection
                            TraceHelper.Assert(Object.ReferenceEquals(existingObj, this));
                        }
                        else
                        {
                            GetParentCollection().Add(this);
                        }
                        //Revert all the dirty bits in the properties bag to not be dirty anymore
                        foreach (SfcProperty property in this.Properties)
                        {
                            property.Dirty = false;
                        }
                    }
                    break;

                case SfcDependencyAction.Rename:
                    {
                        // Before housekeeping event
                        // pass New keychain, New key
                        TraceHelper.Assert(extraParam is SfcKey);
                        SfcKey newKey = (extraParam as SfcKey);
                        SfcKeyChain newKeyChain = new SfcKeyChain(newKey, this.KeyChain.Parent);
                        SfcApplication.Events.OnBeforeObjectRenamed(this, new SfcBeforeObjectRenamedEventArgs(this.KeyChain.Urn, this, newKeyChain.Urn, newKey));

                        // Sfc housekeeping
                        SfcKey oldKey = this.KeyChain.LeafKey;
                        ISfcCollection collection = GetParentCollection();
                        SfcInstance existingObj;
                        TraceHelper.Assert(collection.GetExisting(this.AbstractIdentityKey, out existingObj)); // can't remove if it doesn't exist
                        TraceHelper.Assert(object.ReferenceEquals(this, existingObj)); // must refer to us
                        collection.Rename(this, newKey);
                        TraceHelper.Assert(!collection.GetExisting(oldKey, out existingObj)); // old key must not exist after removal
                        TraceHelper.Assert(collection.GetExisting(this.AbstractIdentityKey, out existingObj)); // new key must now exist
                        TraceHelper.Assert(object.ReferenceEquals(this, existingObj)); // must still refer to us

                        // User housekeeping
                        PostRename(executionResult);

                        // After housekeeping event
                        // pass Old keychain, Old key
                        SfcApplication.Events.OnAfterObjectRenamed(this, new SfcAfterObjectRenamedEventArgs(this.KeyChain.Urn, this, oldKeyChain.Urn, oldKey));
                    }
                    break;

                case SfcDependencyAction.Move:
                    {
                        // Assumes that before calling MoveImpl(), or in response to a BeforeMove event:
                        //      Caller updates its own housekeeping (like Dictionary<SfcKeyChain, ...>) either by visiting descendants
                        //          or blanket examination. Before calling this method, all descendant keychains are still at
                        //          their old (pre-move) values.
                        //
                        // Once we mutate the internal parent keychain reference of our keychain (which is *only* done for moves),
                        // and reset our own parent, all descendants' keychains immediately radiate the new ancestry in their keychains.
                        // Any data structure (like hash tables or dictionaries) which depend on immutable K values will break if any of
                        // these keychains are left in them once you exit this method. The caller has to have already cleaned them
                        // out in preparation for MoveImpl(). Once we exit this method, the caller can then re-add them under their
                        // now-changed keychain values.
                        //
                        // Any cached object references pointing outside the descendants cone meant to be relative will need to be
                        // determined by the caller and adjusted/recached once we exit here. Otherwise you may have some object references
                        // "stretch" to old locations and not the new relative-to-the-cone ones. There is no Sfc support for adjusting
                        // anything for this issue; the caller must somehow know if anythign like this exists and needs accomodation.

                        // Before housekeeping event
                        // pass New keychain, New parent
                        TraceHelper.Assert(extraParam is SfcInstance);
                        SfcInstance newParent = (extraParam as SfcInstance);
                        SfcKeyChain newKeyChain = new SfcKeyChain(this.AbstractIdentityKey, newParent.KeyChain);
                        SfcApplication.Events.OnBeforeObjectMoved(this, new SfcBeforeObjectMovedEventArgs(this.KeyChain.Urn, this, newKeyChain.Urn, newParent));

                        // Sfc housekeeping
                        SfcInstance oldParent = this.Parent;

                        // Remove from old (current) collection
                        ISfcCollection oldCollection = GetParentCollection();
                        oldCollection.RemoveElement(this);

                        // DANGEROUS: Doing this immediately invalidates any existing uses of this keychain or any other keychain
                        // with this one as an ancestor, where the use is supposed to be immutable.
                        // Mutate our keychain's parent keychain internal field directly, so all descendants immediately "switch over".
                        // A better way would be to always create new truly immutable keychains, and visit all descendants 
                        // when a Move occurs and ResetKeychain(). Mutation avoids visitation for now...
                        this.m_keychain.Parent = newParent.m_keychain;

                        // Set the parent directly.
                        // Since the .Parent setter normally only works for Pending state, we have to do it this way.
                        this.m_parent = newParent;

                        // Finally, add to our new collection home
                        ISfcCollection collection = GetParentCollection();
                        SfcInstance existingObj;
                        collection.Add(this);
                        TraceHelper.Assert(!oldCollection.GetExisting(this.AbstractIdentityKey, out existingObj)); // old collection entry for us must not exist
                        TraceHelper.Assert(collection.GetExisting(this.AbstractIdentityKey, out existingObj)); // new collection entry must now exist
                        TraceHelper.Assert(object.ReferenceEquals(this, existingObj)); // must still refer to us

                        // User housekeeping
                        PostMove(executionResult);

                        // After housekeeping event
                        // pass Old keychain, Old parent
                        SfcApplication.Events.OnAfterObjectMoved(this, new SfcAfterObjectMovedEventArgs(this.KeyChain.Urn, this, oldKeyChain.Urn, oldParent));
                    }
                    break;

                case SfcDependencyAction.Alter:
                    {
                        // User housekeeping
                        PostAlter(executionResult);

                        // Sfc Housekeeping
                        //Revert all the dirty bits in the properties bag to not be dirty anymore
                        foreach (SfcProperty property in this.Properties)
                        {
                            property.Dirty = false;
                        }
                    }
                    break;

                case SfcDependencyAction.Drop:
                    {
                        // User housekeeping
                        PostDrop(executionResult);

                        // Sfc Housekeeping
                        GetParentCollection().RemoveElement(this);
                    }
                    break;

                default:
                    TraceHelper.Assert(false); // Unknown action
                    break;
            }
        }

        // Note: all implementors are aware that the executionResult is always null if the connection context mode is Offline.
        // They should check for that to tell the difference between an Offline null result and an executed null result.
        protected virtual void PostCreate(object executionResult) { /* default impl does nothing */ }
        protected virtual void PostRename(object executionResult) { /* default impl does nothing */ }
        protected virtual void PostMove(object executionResult) { /* default impl does nothing */ }
        protected virtual void PostAlter (object executionResult) { /* default impl does nothing */ }
        protected virtual void PostDrop  (object executionResult) { /* default impl does nothing */ }

        private ISfcScript ScriptCRUD(SfcDependencyAction depAction,object extraParam)
        {
            try
            {
                switch(depAction)
                {
                    // InvalidCast here is domain error
                    case SfcDependencyAction.Create: return ((ISfcCreatable)(this)).ScriptCreate();
                    case SfcDependencyAction.Rename: return ((ISfcRenamable)(this)).ScriptRename((SfcKey)extraParam);
                    case SfcDependencyAction.Move: return ((ISfcMovable)(this)).ScriptMove((SfcInstance)extraParam);
                    case SfcDependencyAction.Alter:  return ((ISfcAlterable)(this)).ScriptAlter();
                    case SfcDependencyAction.Drop:   return ((ISfcDroppable)(this)).ScriptDrop();
                }
            }
            catch(InvalidCastException)
            {
                throw new SfcObjectNotScriptableException(SfcStrings.ObjectNotScriptabe(this.ToString(),this.GetType().Name));
            }
            return null;
        }

        private ISfcScript  AccumulateScript(List<SfcInstance> objList, SfcDependencyAction depAction, object extraParam)
        {
            ISfcScript whole_script = null;
            foreach(SfcInstance currentObject in objList)
            {
                // currentObject: Is my parent in the list? If it is, is it going to take care of me?
                if( objList.Contains(currentObject.Parent) )
                {
                    SfcTypeMetadata typeMetadata = currentObject.GetTypeMetadataImpl();
                    // "typeMetadata == null" means domain doesn't implement type metadata (yet). They can get away with it
                    // now (in v1), and we will assume that in this case every object is responsible for its own scripting
                    if( typeMetadata != null && typeMetadata.IsCrudActionHandledByParent(depAction) )
                    {
                        continue;
                    }
                }

                ISfcScript partial_script = currentObject.ScriptCRUD(depAction,extraParam);

                if( partial_script != null )
                {
                    if( whole_script == null )
                    {
                        whole_script = partial_script;
                    }
                    else
                    {
                        whole_script.Add(partial_script);
                    }
                }
            }

            return whole_script;
        }

        private void CRUDImpl( string operationName, SfcObjectState requiredState, SfcDependencyAction depAction, SfcObjectState finalState )
        {
            CRUDImplWorker( operationName, requiredState, depAction, finalState, null );
        }

        private void CRUDImplWorker( string operationName, SfcObjectState requiredState, SfcDependencyAction depAction, SfcObjectState finalState, object extraParam )
        {
            // Check state. Note we don't check state of all children (SMO doesn't either)
            CheckObjectStateAndParent(requiredState);

            List<SfcInstance> depObjects = GetDependentObjects(depAction);
            TraceHelper.Assert(depObjects.Count > 0); // could not get any objects to script

            // Save a copy of the original Keychain in case this is a rename or move.
            // Eitehr operation will mutate the original, so make a COPY.
            // We will need it for the event notification.
            SfcKeyChain oldKeyChain = new SfcKeyChain(this.KeyChain.LeafKey, this.KeyChain.Parent);

            // Guard against execution engine running anything if we are disconnected (Offline).
            // We can only pretend the executionResult is null, since many different kinds of things are normally possible.
            // Note: client code using an EE directly to do things like ExecuteNonQuery will have to guard their own
            // execution attmepts by looking at the Offline mode themselves just like we do here.
            object executionResult = null;
            if (this.GetDomain().ConnectionContext.Mode == SfcConnectionContextMode.Offline)
            {
                if( depAction == SfcDependencyAction.Create && this.Parent != null )
                {
                    // We can't just use this.AbstractIdentityKey here, since underlying properties might have changed,
                    // causing it to be stale. Instead, ask for the guaranteed up-to-date key
                    SfcKey mostCurrentKey = this.CreateIdentityKey();

                    if( !mostCurrentKey.Equals(this.AbstractIdentityKey) )
                    {
                        // A property that is part of a key (like 'Name') has changed, but the key has not been updated.
                        // Do it now, and pray that nobody nowhere still refers to this object by its old key
                        ResetKey();

                        // Better be equal now...
                        TraceHelper.Assert(mostCurrentKey.Equals(this.AbstractIdentityKey));
                    }

                    SfcInstance existingObj;
                    if (this.GetParentCollection().GetExisting(mostCurrentKey, out existingObj))
                    {
                        throw new SfcCRUDOperationFailedException(SfcStrings.CannotCreateDestinationHasDuplicate(this));
                    }
                }
            }
            else
            {
                ISfcScript script = AccumulateScript(depObjects, depAction, extraParam);
                ISfcExecutionEngine ee = this.GetDomain().GetExecutionEngine();

                try
                {
                    executionResult = ee.Execute(script);
                }
                catch (Exception ex)
                {
                    throw new SfcCRUDOperationFailedException(SfcStrings.CRUDOperationFailed(operationName, this.ToString()), ex);
                }
            }

            // Execution was successful, so propagate final state to all objects
            if( this.GetDomain().UseSfcStateManagement() )
            {
                foreach(SfcInstance obj in depObjects)
                {

                    //We need this special case for DC
                    if (SfcObjectState.ToBeDropped != obj.State)
                    {
                        obj.State = finalState;
                    }
                    else //if you are marked for drop, you should be dropped no matter what the final state given is
                    {
                        obj.State = SfcObjectState.Dropped;

                        // Remove the now-dropped item if it's parent is not itself dropped, since the PostCRUD
                        // processing will not drop his parent (effectively dropping this item too).
                        // You always have to remove any Dropped items that are not part of a larger Dropped subtree.
                        if (obj.Parent.State != SfcObjectState.Dropped)
                        {
                            // Raw remove from collection
                            // These objects are still valid if the caller happens to have a handle to them, but they
                            // can no longer be accessed via collection as of this next line.
                            obj.GetParentCollection().RemoveElement(obj);
                        }
                    }

                    // Clean up object internals
                    if( depAction == SfcDependencyAction.Create || depAction == SfcDependencyAction.Alter )
                    {
                        // Nothing is dirty to start off
                        foreach( SfcProperty prop in obj.Properties ) { prop.Dirty=false; }
                    }

                    // Fire CRUD events which occur on individual instances affected
                    switch (depAction)
                    {
                        case SfcDependencyAction.Create:
                            SfcApplication.Events.OnObjectCreated(this, new SfcObjectCreatedEventArgs(obj.Urn, obj));
                            break;
                        case SfcDependencyAction.Alter:
                            SfcApplication.Events.OnObjectAltered(this, new SfcObjectAlteredEventArgs(obj.Urn, obj));
                            break;
                        case SfcDependencyAction.Drop:
                            SfcApplication.Events.OnObjectDropped(this, new SfcObjectDroppedEventArgs(obj.Urn, obj));
                            break;
                    }
                }
            }

            // Handle OM housekeeping and overall Before/After events
            PostCrud(depAction, oldKeyChain, extraParam, executionResult);
        }

        protected void CreateImpl()
        {
            CRUDImpl(SfcStrings.opCreate, SfcObjectState.Pending, SfcDependencyAction.Create, SfcObjectState.Existing );
        }

        protected void RenameImpl(SfcKey newKey)

        {
            // The new key must not be null
            if (newKey == null)
            {
                throw new SfcInvalidRenameException(SfcStrings.CannotRenameNoKey(this));
            }

            // We MUST make sure the collection is initialized before we start talking to it. Since some of these
            // functions, like GetExisting() do not init it we make sure it happens.
            // You do not want to enter the CRUDImplWorker with the collection uninitialized, because you want it
            // avoid server-side fetches an initialize would cause to happen between the time you start updating
            // the server-side and when you finish up client-side houskeeping to match.
            //this.GetParentCollection().EnsureInitialized();
            ISfcCollection collection = this.GetParentCollection();
            bool realInitializedState = collection.Initialized;
            try
            {
                // We have to protect from init by push/pop state, so the collection can go back to acting normally
                // after the rename attempt is over. Hence the finally block.
                // $ISSUE: This architecture is plain bad. We need to have better consistent way to keep hands off
                // Enumerator init/refresh than this flag business.
                collection.Initialized = true;

                // The new key must not already exist in the same collection
                SfcInstance existingObj;
                if (this.GetParentCollection().GetExisting(newKey, out existingObj))
                {
                    throw new SfcInvalidRenameException(SfcStrings.CannotRenameDestinationHasDuplicate(this, newKey));
                }

                CRUDImplWorker(SfcStrings.opRename, SfcObjectState.Existing, SfcDependencyAction.Rename, SfcObjectState.Existing, newKey);
            }
            finally
            {
                // If we were not init'd before, we won't be again so I hope noone did anything in CRUDImplWorker
                // that would have really init'd this or it will get done again on the next access attempt.
                collection.Initialized = realInitializedState;
            }
        }

        protected void MoveImpl(SfcInstance newParent)
        {
            // The new parent object must not be null
            if (newParent == null)
            {
                throw new SfcInvalidMoveException(SfcStrings.CannotMoveNoDestination(this));
            }

            // The new parent object cannot be inside us
            if (this.KeyChain.IsClientAncestorOf(newParent.KeyChain))
            {
                throw new SfcInvalidMoveException(SfcStrings.CannotMoveDestinationIsDescendant(this, newParent));
            }

            // The new parent object must not have an existing child in the same collection with the same key as us
            SfcInstance existingObj;
            if (newParent.GetChildCollection(this.GetType().Name).GetExisting(this.AbstractIdentityKey, out existingObj))
            {
                throw new SfcInvalidMoveException(SfcStrings.CannotMoveDestinationHasDuplicate(this, newParent));
            }

            // Any state is allowed, so use the object's current state as bothrequire state and final state
            CRUDImplWorker(SfcStrings.opMove, newParent.State, SfcDependencyAction.Move, newParent.State, newParent);
        }

        protected void AlterImpl()
        {
            CRUDImpl(SfcStrings.opAlter, SfcObjectState.Existing, SfcDependencyAction.Alter, SfcObjectState.Existing );
        }

        protected void DropImpl()
        {
            CRUDImpl(SfcStrings.opDrop, SfcObjectState.Existing, SfcDependencyAction.Drop, SfcObjectState.Dropped );
        }

        protected void MarkForDropImpl(bool dropOnAlter)
        {
            CheckObjectState();
            if (this.State != SfcObjectState.Existing && this.State != SfcObjectState.ToBeDropped)
            {
                throw new SfcInvalidStateException(SfcStrings.InvalidState(this.State, SfcObjectState.Existing));
            }

            if (dropOnAlter)
            {
                this.State = SfcObjectState.ToBeDropped;
            }
            else if (this.State == SfcObjectState.ToBeDropped)
            {
                this.State = SfcObjectState.Existing;
            }
        }

#endregion
    }

    /// <summary>
    /// The generic base class for all Sfc object types in a domain.
    /// </summary>
    /// <typeparam name="K">The type of the key for the instance class.</typeparam>
    /// <typeparam name="T">The type of the instance class.</typeparam>
    public abstract class SfcInstance<K, T> : SfcInstance
        where K : SfcKey
        where T : SfcInstance, new()
    {
        public SfcInstance()
            : base()
        {
        }

        /// <summary>
        /// Always create an object factory per class type.
        /// This is not generic since the SFC core that uses factories is still non-generic and weakly-typed.
        /// </summary>
        private sealed class ObjectFactory : SfcObjectFactory
        {
            static readonly ObjectFactory instance = new ObjectFactory();

            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static ObjectFactory() { }

            ObjectFactory() { }

            public static ObjectFactory Instance
            {
                get { return instance; }
            }

            protected override SfcInstance CreateImpl()
            {
                return new T();
            }
        }

        #region TypeMetadata support

        /// <summary>
        /// Always create a type metadata per class type.
        /// The only thing a derived class may want to do is override any of the nested partial class's virtual methods it does differently.
        /// </summary>
        private sealed class TypeMetadata : SfcTypeMetadata
        {
            string typeName = typeof(T).Name;
            static readonly TypeMetadata instance = new TypeMetadata();

            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static TypeMetadata() { }

            TypeMetadata() { }

            public static TypeMetadata Instance
            {
                get { return instance; }
            }

            /// <summary>
            /// The default handling of CRUD dependency based on which action is performed.
            /// All actions assume they are performed explicitly except dropping, which assumes the parent takes care of it
            /// since in most providers, deleting an object naturally deletes everything "under" it.
            /// </summary>
            /// <param name="depAction">The CRUD action.</param>
            /// <returns>True if the parent handles the action for this objet; false if the object handles itself.</returns> 
            public override bool IsCrudActionHandledByParent(SfcDependencyAction depAction)
            {
                switch (depAction)
                {
                    case SfcDependencyAction.Create:
                    case SfcDependencyAction.Rename:
                    case SfcDependencyAction.Move:
                    case SfcDependencyAction.Alter:
                        return false;
                    case SfcDependencyAction.Drop:
                        return true;
                    default:
                        throw new InvalidOperationException(SfcStrings.UnsupportedAction(depAction.ToString(), this.typeName));
                }
            }
        }

        /// <summary>
        /// Internal static class type metadata access.
        /// Usually called from the domain root instance via switch.
        /// To customize a type's TypeMetadata, skip calling this method and return your own SfcTypeMetadata-derived object for the class.
        /// </summary>
        /// <returns></returns>
        static public SfcTypeMetadata GetTypeMetadata()
        {
            return TypeMetadata.Instance;
        }

        /// <summary>
        /// Internal instance class type metadata access.
        /// This returns the default static implementation of a type's SfcTypeMetadata.
        /// To customize a type's TypeMetadata, override this method in the derived type class to point to your own SfcTypeMetadata-derived object singleton for that class.
        /// </summary>
        /// <returns></returns>
        protected internal override SfcTypeMetadata GetTypeMetadataImpl()
        {
            return GetTypeMetadata();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static SfcObjectFactory GetObjectFactory()
        {
            return ObjectFactory.Instance;
        }

        /// <summary>
        /// Derived classes must implement this to create a key for a class instance from its properties.
        /// </summary>
        /// <returns>The key.</returns>
        protected internal abstract K CreateKey();

        /// <summary>
        /// Wrapper for our strongly-typed version. The non-generic SfcInstance base needs this.
        /// </summary>
        /// <returns>The key.</returns>
        protected internal override SfcKey CreateIdentityKey()
        {
            return this.CreateKey();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [SfcIgnore]
        public K IdentityKey
        {
            get { return (K)this.AbstractIdentityKey; }
        }
    }

    /// <summary>
    /// A proxy class to allow SfcPropertyCollection set and get property data in SFC instance class
    /// </summary>
    sealed class PropertyDataDispatcher
    {
        SfcInstance m_owner; // the instance object that owns the dispatcher

        internal PropertyDataDispatcher(SfcInstance owner)
        {
            m_owner = owner;
        }

        internal SfcInstance GetParent()
        {
            return m_owner;
        }

        internal object GetPropertyValue(string propertyName)
        {
            return m_owner.GetPropertyValueImpl(propertyName);
        }

        internal void SetPropertyValue(string propertyName, object value)
        {
            m_owner.SetPropertyValueImpl(propertyName, value);
        }

        internal void InitializeState()
        {
            m_owner.InitializeUIPropertyState();
        }
    }

    /// <summary>
    /// The generic base class for a proxy to another target instance.
    /// The properties available are the proxy instance properties plus plus the target instance properties.
    /// If both the proxy and target instances have the same name for a property, then the proxy property is the one that is exposed.
    /// All target instance properties can always be obtained by explicitly accessing the proxy.Reference property which is the target instance.
    /// </summary>
    /// <typeparam name="K">The type of the key for the proxy instance class.</typeparam>
    /// <typeparam name="T">The type of the proxy instance class.</typeparam>
    /// <typeparam name="TRef">The type of the target reference instance class.</typeparam>
    public abstract class SfcProxyInstance<K, T, TRef> : SfcInstance<K, T>
        where K : SfcKey
        where T : SfcInstance, new()
        where TRef : SfcInstance
    {
        private TRef reference;

        /// <summary>
        /// Construct a new proxy instance to an unknown target reference.
        /// The reference will be resolved upon first access to the Reference property.
        /// </summary>
        public SfcProxyInstance()
        {
        }

        /// <summary>
        /// Construct a new proxy instance to a known target reference.
        /// </summary>
        /// <param name="reference">The target reference instance.</param>
        public SfcProxyInstance(TRef reference)
        {
            this.reference = reference;
        }

        /// <summary>
        /// The instance that this proxy refers to.
        /// This is only set once when this property is first accessed.
        /// </summary>
        public TRef Reference
        {
            get
            {
                if (this.reference == null)
                {
                    this.reference = this.GetReferenceImpl();
                }
                return this.reference;
            }
        }

        /// <summary>
        /// Get the referenced object based on the proxy.
        /// Must be implemented.
        /// </summary>
        /// <returns></returns>
        protected abstract TRef GetReferenceImpl();

    }

}

