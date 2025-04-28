// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Collections;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
using System.Data.SqlTypes;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // Given index and value, get/set the appropriate data member in XSchema
    internal interface IPropertyDataDispatch
    {
        object GetPropertyValue(int index);
        void SetPropertyValue(int index, object value);
    }
}

namespace Microsoft.SqlServer.Management.Smo
{
    public class NamedSmoObject : SqlSmoObject
    {
        internal NamedSmoObject(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
        }

        // this default constructor has to be called by objects that do not know their parent
        // because they don't live inside a collection
        internal NamedSmoObject(ObjectKeyBase key, SqlSmoState state)
            : base(key, state)
        {
        }

        // this constructor called by objects thet are created in space
        protected internal NamedSmoObject()
            : base()
        {
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone)]
        public virtual string Name
        {
            get
            {
                return ((SimpleObjectKey)key).Name;
            }
            set
            {
                try
                {
                    ValidateName(value);
                    if (ShouldNotifyPropertyChange)
                    {
                        if (this.Name != value)
                        {
                            ((SimpleObjectKey)key).Name = value;
                            OnPropertyChanged("Name");
                        }
                    }
                    else
                    {
                        ((SimpleObjectKey)key).Name = value;
                    }
                    UpdateObjectState();
                }
                catch (Exception e)
                {
                    FilterException(e);

                    throw new FailedOperationException(ExceptionTemplates.SetName, this, e);
                }

            }
        }

        internal virtual void ValidateName(string name)
        {
            if (null == name)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Name"));
            }

            if (
                (State != SqlSmoState.Pending) &&
                (State != SqlSmoState.Creating)
                )
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationOnlyInPendingState);
        }
        }

        internal override string FullQualifiedName
        {
            get
            {
                return string.Format(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(this.Name));
            }
        }

        internal override string InternalName
        {
            get { return string.Format(SmoApplication.DefaultCulture, "{0}", this.Name); }
        }


        ///<summary>
        /// change object name
        ///</summary>
        protected void RenameImpl(string newName)
        {
            try
            {
                CheckObjectState();
                string oldName = this.Name;
                string oldUrn = this.Urn;
                RenameImplWorker(newName);

                if (!this.ExecutionManager.Recording)
                {
                    // generate internal events
                    if (!SmoApplication.eventsSingleton.IsNullObjectRenamed())
                    {
                        SmoApplication.eventsSingleton.CallObjectRenamed(GetServerObject(),
                            // Only new Urn (this.Urn) and old Urn (oldUrn) are used now by SSMS
                            // Other parameters are left for backwards compatibility with old
                            // receivers of the event who might use them
                            new ObjectRenamedEventArgs(this.Urn, this, oldName, newName, oldUrn));
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Rename, this, e);
            }
        }

        protected void RenameImplWorker(string newName)
        {
            if (null == newName)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("newName"));
            }

            ObjectKeyBase oldKey = this.key;

            ExecuteRenameQuery(newName);

            if (!this.ExecutionManager.Recording)
            {
                ((SimpleObjectKey)this.key).Name = newName;

                if (null != ParentColl)
                {
                    ParentColl.RemoveObject(oldKey);
                    ParentColl.AddExisting(this);
                }
            }
        }

        /// <summary>
        /// Creates the Rename query for a SqlSmoObject and Executes it on the Server.
        /// </summary>
        /// <param name="newName"></param>
        protected virtual void ExecuteRenameQuery(string newName)
        {
            // builds the t-sql for filegroup rename
            StringCollection renameQuery = new StringCollection();
            ScriptingPreferences sp = new ScriptingPreferences();
            sp.SetTargetServerInfo(this);
            ScriptRename(renameQuery, sp, newName);

            // execute t-sql
            if (renameQuery.Count > 0 && !this.IsDesignMode)
            {
                // don't include database context while renaming a database
                this.ExecuteNonQuery(renameQuery, !(this is Database), executeForAlter: false);
            }
        }

        internal virtual void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            throw new InvalidOperationException();
        }

        internal override ObjectKeyBase GetEmptyKey()
        {
            return new SimpleObjectKey(null);
        }

        /// <summary>
        /// This is the prefix that is added to the permission DDL for the object
        /// The list is extracted from file 'secmgr.cpp' in the array:
        /// static const SECPermMappings::SECClassNameStrings x_sps_Class[] =
        /// Objects whose securable class_desc is OBJECT can omit the "object::" qualifier
        /// </summary>
        internal string PermissionPrefix
        {
            get
            {
                string prefix = null;

                switch (this.GetType().Name)
                {
                    case nameof(ExternalLanguage):
                        prefix = "EXTERNAL LANGUAGE"; break;

                    case nameof(ExternalLibrary):
                        prefix = "EXTERNAL LIBRARY"; break;
                    
                    case nameof(SqlAssembly):
                        prefix = "ASSEMBLY"; break;
                    
                    case nameof(UserDefinedDataType):                        
                    case nameof(UserDefinedTableType):                        
                    case nameof(UserDefinedType):
                        prefix = "TYPE"; break;
                    
                    case nameof(FullTextCatalog):
                        prefix = "FULLTEXT CATALOG"; break;

                    case nameof(Login):
                        prefix = "LOGIN"; break;

                    case nameof(ServerRole):
                        prefix = "SERVER ROLE"; break;

                    case nameof(Schema):
                        prefix = "SCHEMA"; break;

                    case nameof(Endpoint):
                    case "HttpEndpoint":
                        prefix = "ENDPOINT"; break;

                    case nameof(XmlSchemaCollection):
                        prefix = "XML SCHEMA COLLECTION"; break;

                    case nameof(Certificate):
                        prefix = "CERTIFICATE"; break;

                    case nameof(ApplicationRole):
                        prefix = "APPLICATION ROLE"; break;

                    case nameof(User):
                        prefix = "USER"; break;

                    case nameof(DatabaseRole):
                        prefix = "ROLE"; break;

                    case nameof(SymmetricKey):
                        prefix = "SYMMETRIC KEY"; break;

                    case nameof(AsymmetricKey):
                        prefix = "ASYMMETRIC KEY"; break;                    

                    // service broker related objects
                    case "MessageType":
                        prefix = "MESSAGE TYPE"; break;

                    case "ServiceContract":
                        prefix = "CONTRACT"; break;

                    case "BrokerService":
                        prefix = "SERVICE"; break;

                    case "ServiceRoute":
                        prefix = "ROUTE"; break;

                    case "RemoteServiceBinding":
                        prefix = "REMOTE SERVICE BINDING"; break;

                    case nameof(FullTextStopList):
                        prefix = "FULLTEXT STOPLIST"; break;

                    case nameof(SearchPropertyList):
                        prefix = "SEARCH PROPERTY LIST"; break;

                    case nameof(Database):
                        prefix = "DATABASE"; break;

                    case nameof(AvailabilityGroup):
                        prefix = AvailabilityGroup.AvailabilityGroupScript; break;

                    case nameof(DatabaseScopedCredential):
                        prefix = "DATABASE SCOPED CREDENTIAL"; break;
                }
                if (null != prefix)
                {
                    return prefix + "::";
                }

                return string.Empty;
            }
        }

        internal virtual string FormatFullNameForScripting(ScriptingPreferences sp)
        {
            return FormatFullNameForScripting(sp, true);
        }

        /// <summary>
        /// format full object name for scripting
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="nameIsIndentifier"></param>
        /// <returns></returns>
        internal string FormatFullNameForScripting(ScriptingPreferences sp, bool nameIsIndentifier)
        {
            CheckObjectState();

            if (nameIsIndentifier)
            {
                return MakeSqlBraket(GetName(sp));
            }
            else
            {
                return MakeSqlString(GetName(sp));
            }
        }

        /// <summary>
        /// Returns the name that will be used for scripting, in case we allow
        /// users to script the object with a different name.
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        internal virtual string GetName(ScriptingPreferences sp)
        {
            return this.Name;
        }

        internal void ScriptOwner(StringCollection queries, ScriptingPreferences sp)
        {
            this.ScriptChangeOwner(queries,sp);
        }

        protected void SetSchemaOwned()
        {
            if (this.ExecutionManager.Recording || !this.IsDesignMode || !this.IsVersion90AndAbove())
            {
                return;
            }

            string owner = this.Properties.Get("Owner").Value as string;
            bool schemaOwned = false;

            if (string.IsNullOrEmpty(owner))
            {
                schemaOwned = true;
            }
            //lookup the property ordinal from name
            int isSchemaOwnedSet = this.Properties.LookupID("IsSchemaOwned", PropertyAccessPurpose.Write);
            //set the new value
            this.Properties.SetValue(isSchemaOwnedSet, schemaOwned);
            //mark the property as retrived, that means that it is
            //in sync with value on the server
            this.Properties.SetRetrieved(isSchemaOwnedSet, true);
        }

         internal virtual void ScriptChangeOwner(StringCollection queries, ScriptingPreferences sp)
        {
            Property prop = this.GetPropertyOptional("Owner");

            if (!prop.IsNull && (prop.Dirty || !sp.ScriptForAlter))
            {
                ScriptChangeOwner(queries, (string)prop.Value, sp);
            }
        }

        /// <summary>
        /// Generate the script statements to change the owner of this object to the specified
        /// owner name.
        /// </summary>
        /// <param name="queries">Query collection to add the statements to</param>
        /// <param name="newOwner">The name of the new owner</param>
        /// <param name="sp">The scripting preferences</param>
        internal virtual void ScriptChangeOwner(StringCollection queries, string newOwner, ScriptingPreferences sp = null)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if ((sp != null && sp.TargetServerVersion > SqlServerVersion.Version80) ||
                (sp == null && this.IsVersion90AndAbove()))
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER AUTHORIZATION ON {0}", this.PermissionPrefix);
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", FormatFullNameForScripting(sp));
                sb.AppendFormat(SmoApplication.DefaultCulture, " TO ");
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(newOwner));
            }
            else
            {
                this.ScriptOwnerForShiloh(sb, sp, newOwner);
            }

            if (sb.Length > 0)
            {
                queries.Add(sb.ToString());
            }
        }

         /// <summary>
         /// Scripting the owner for shiloh (2005) which doesn't support the ALTER AUTHORIZATION statement
         /// </summary>
        /// <param name="sb">Builder to add the statements to</param>
        /// <param name="newOwner">The name of the new owner</param>
        /// <param name="sp">The scripting preferences</param>
         /// <returns></returns>
         internal virtual void ScriptOwnerForShiloh(StringBuilder sb, ScriptingPreferences sp, string newOwner)
         {
             sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC sp_changeobjectowner {0} , {1} ", MakeSqlString(FormatFullNameForScripting(sp)), MakeSqlString(newOwner));
         }
    }

    ///<summary>
    /// Contains common functionality for all the instance classes
    ///</summary>
    [TypeConverter(typeof(LocalizableTypeConverter))]
    public abstract class SqlSmoObject : SmoObjectBase
                                       , Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertyProvider
                                       , Microsoft.SqlServer.Management.Common.IRefreshable
                                       , IAlienObject
                                       , ISqlSmoObjectInitialize
    {
        internal const BindingFlags UrnSuffixBindingFlags =
            BindingFlags.Default | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public |
            BindingFlags.GetProperty | BindingFlags.FlattenHierarchy;
        /// <summary>
        /// Event that is raised when a property fetch is made after object initialization
        /// and the object needs to issue a SQL query to retrieve the value.
        /// This event is raised synchronously, so the fetch is blocked until all handlers of the event return.
        /// </summary>
        public static event EventHandler<PropertyMissingEventArgs> PropertyMissing = delegate { };
        internal SqlSmoObject(AbstractCollectionBase parentColl,
                                                ObjectKeyBase key, SqlSmoState state)
        {
#if DEBUG
            if (null == parentColl)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("parentColl"));
            }

            if (null == key)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("key"));
            }
#endif


            SetObjectKey(key);
            this.parentColl = parentColl;
            Init();
            SetState(state);
        }

        internal virtual SqlPropertyMetadataProvider GetPropertyMetadataProvider()
        {
            return null;
        }


        // this default constructor has to be called by objects that do not know their parent
        // because they don't live inside a collection
        internal SqlSmoObject(ObjectKeyBase key, SqlSmoState state)
        {
            SetObjectKey(key);
            Init();
            SetState(state);
        }

        // this constructor called by objects thet are created in space
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected internal SqlSmoObject()
        {
            Init();
            SetState(SqlSmoState.Pending);
            objectInSpace = true;
            key = GetEmptyKey();
        }

        internal virtual ObjectKeyBase GetEmptyKey()
        {
            return new ObjectKeyBase();
        }

        // some initialization calls
        private void Init()
        {
            // sets initial state
            propertyBagState = PropertyBagState.Empty;

            // inits the properties to null, we will populate the property
            // collection with enumerator metadata when the user asks for it
            properties = null;

            // we assume every object will be scripted
            m_bIgnoreForScripting = false;

            m_comparer = null;

            m_ExtendedProperties = null;
        }

        // Cache the lookup of the property name in the parent object based on the singleton child type
        // Used for SMO Object Query processing. Default capacity is 20 since there are abotu 20 singleton classes
        // in SMO in Katmai.
        private static Dictionary<Type, string> s_SingletonTypeToProperty = new Dictionary<Type, string>(20);

        // Cache the lookup of the property name in the parent object based on the singleton child type
        // Used for SMO Object Query processing. Default capacity is 150 since there are about 132 object classes
        // in SMO in Katmai.
        private static Dictionary<Type, string[]> s_TypeToKeyFields = new Dictionary<Type, string[]>(150);

        bool initializedForScripting = false;
        internal bool InitializedForScripting
        {
            get { return initializedForScripting; }
            set { initializedForScripting = value; }
        }

        internal bool objectInSpace = false;
        protected bool ObjectInSpace
        {
            get { return objectInSpace; }
        }

        // this function returns true if the object is in space, of one of its parents is in space
        internal protected bool IsObjectInSpace()
        {
            if (this.State == SqlSmoState.Pending)
            {
                return true;
            }

            // climb up the tree to the server object
            SqlSmoObject current = this;

            while ((null != current) && !(current is Server))
            {
                if (current.ObjectInSpace)
                {
                    return true;
                }

                if (null == current.ParentColl || null == current.ParentColl.ParentInstance)
                {
                    PropertyInfo mi = current.GetType().GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public);
                    if (null == mi)
                    {
                        throw new InternalSmoErrorException(ExceptionTemplates.GetParentFailed);
                    }

                    current = mi.GetValue(current, null) as SqlSmoObject;
                }
                else
                {
                    current = current.ParentColl.ParentInstance;
                }
            }

            return false;
        }

        // Not all SqlSmoObjects have ExtendedProperties, but we need to make sure that
        // when an object is refreshed, that its ExtendedProperties are also refreshed.
        // To do this, we push this into the base class and clear extended properties in
        // SqlSmoObject.Refresh()

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected ExtendedPropertyCollection m_ExtendedProperties;

        protected virtual void MarkDropped()
        {
            // mark the object itself as dropped
            SetState(SqlSmoState.Dropped);

            if (null != userPermissions)
            {
                userPermissions.MarkAllDropped();
            }
        }

        // we have this function so that we can call MarkDropped internally
        // internal virtual ... is forbidden by the compiler; to be fixed by Everett
        internal void MarkDroppedInternal()
        {
            MarkDropped();
        }

        protected void MarkForDropImpl(bool dropOnAlter)
        {
            CheckObjectState();
            if (this.State != SqlSmoState.Existing && this.State != SqlSmoState.ToBeDropped)
            {
                throw new InvalidSmoOperationException("MarkForDrop", this.State);
            }

            if (dropOnAlter)
            {
                SetState(SqlSmoState.ToBeDropped);
            }
            else if (this.State == SqlSmoState.ToBeDropped)
            {
                SetState(SqlSmoState.Existing);
            }
        }

        // this function will be the first thing called in EVERY public method
        // and property of ANY SMO object. It checks if the object is still
        // alive, meaning that it has not been dropped yet. If the object
        // has been dropped, the method throws an exception. There will
        // be some exception from this rule, eg methods that do not do this
        // check are mainly methods and properties that go up the chain
        // Methods not checked : Name, State, Parent, class constructors
        // Also, note that the checks will not apply to objects that cannot be
        // dropped, like Languages, or Backup
        // For properties that are accessed through the property bag, we will
        // check the property bag.
        protected void CheckObjectState()
        {
            CheckObjectState(false);
        }

        /// <summary>
        /// This is a virtual function, so that derived classes can
        /// override it if they want to do additional checks on the state of the object
        /// </summary>
        /// <param name="throwIfNotCreated"></param>
        protected virtual void CheckObjectState(bool throwIfNotCreated)
        {
            CheckObjectStateImpl(throwIfNotCreated);
        }

        /// <summary>
        /// Checks object state
        /// Because it is not recusrive, this function can be called directly and
        /// which means derived classes can't supply their own validation
        /// </summary>
        /// <param name="throwIfNotCreated"></param>
        protected void CheckObjectStateImpl(bool throwIfNotCreated)
        {
            CheckPendingState();

            if (this.State == SqlSmoState.Dropped)
            {
                throw new SmoException(ExceptionTemplates.ObjectDroppedExceptionText(this.GetType().ToString(), this.key.ToString()));
            }

            if (throwIfNotCreated && this.State == SqlSmoState.Creating)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.ErrorInCreatingState);
            }
        }

        AbstractCollectionBase parentColl;
        /// <summary>
        /// Pointer to the collection that holds the object, if any
        /// </summary>
        internal AbstractCollectionBase ParentColl
        {
            get
            {
                return parentColl;
            }
            set
            {
                parentColl = value;
            }
        }

        /// <summary>
        /// Returns the collection that contains the object. May be null.
        /// </summary>
        public AbstractCollectionBase ParentCollection
        {
            get { return ParentColl; }
        }

        internal virtual string FullQualifiedName
        {
            get
            {
                return key.ToString();
            }
        }

        internal virtual string InternalName
        {
            get { return key.ToString(); }
        }

        public override string ToString()
        {

            if (key.GetType().Name == "ObjectKeyBase")
            {
                return base.ToString();
            }

            return key.ToString();
        }

        // the Skeleton of the Urn is consists of an Urn devoided of filters
        // we need it in order to get the list of properties that an object has
        // we do not want to pass the whole Urn to the enumerator for that
        internal string UrnSkeleton
        {
            get
            {
                CheckObjectState();
#if INCLUDE_PERF_COUNT
                if( PerformanceCounters.DoCount )
                    PerformanceCounters.UrnSkelCallsCount++;
#endif
                StringBuilder urnbuilder = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                GetUrnShellRecursive(urnbuilder);
                return urnbuilder.ToString();
            }
        }

        /// This function returns the Type matching the given URN
        /// skeleton. It is the inverse of GetUrnSkeletonFromType.
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
        public static Type GetTypeFromUrnSkeleton(Urn urn)
        {
            XPathExpression skeleton = urn.XPathExpression;
            Type childType = null;
            string parentName = null;
            for (int i = 0; i < skeleton.Length; i++)
            {
                childType = GetChildType(skeleton[i].Name, parentName);
                if (null == childType)
                {
                    break;
                }
                parentName = childType.Name;
            }
            return childType;
        }

        // go up the chain recursively to get the skeleton
        // we have this recursive function in the event some levels would have
        // different rules, otherwise we could have avoided recursivity

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
        internal void GetUrnShellRecursive(StringBuilder urnbuilder)
        {
            // determine the suffix, which is static member of the class
            string urnsuffix = SqlSmoObject.GetUrnSuffix(this.GetType());

            // if this is an empty string, we are in RootObject
            if (urnsuffix.Length == 0)
            {
                return;
            }

            // the recursive call to get the parent's skeleton
            if (null != ParentColl)
            {
                ParentColl.ParentInstance.GetUrnShellRecursive(urnbuilder);
            }
            else if (!(this is Server))
            {
                SqlSmoObject parent = (SqlSmoObject)this.GetType().InvokeMember("Parent",
                    BindingFlags.Default | BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.GetProperty,
                    null,
                    this,
                    new object[] { }, SmoApplication.DefaultCulture);

                parent.GetUrnShellRecursive(urnbuilder);
            }


            if (urnbuilder.Length != 0)
            {
                urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", urnsuffix);
            }
            else
            {
                // if the parentSkeleton is empty we are in Server object,
                // and we do not append any prefix
                urnbuilder.Append("Server");
            }
        }

        internal void GetUrnRecImpl(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            GetUrnRecursive(urnbuilder, idOption);
        }

        /// <summary>
        /// Computes the Urn for the object.
        /// </summary>
        /// <param name="urnbuilder"></param>
        protected virtual void GetUrnRecursive(StringBuilder urnbuilder)
        {
            GetUrnRecursive(urnbuilder, UrnIdOption.NoId);
        }

        /// <summary>
        /// Computes the Urn for the object, potentially including other fields in
        /// the definition besides the key fields.
        /// </summary>
        /// <param name="urnbuilder">holds the Urn</param>
        /// <param name="useIdAsKey">Us ID as key instead of the regular key
        /// fields. If the object does not have this property the regular key
        /// fields will still be used.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
        protected virtual void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            // determine the suffix, which is static member of the class
            string urnsuffix = SqlSmoObject.GetUrnSuffix(this.GetType());

            // if this is an empty string, we are in RootObject
            if (urnsuffix.Length == 0)
            {
                return;
            }

            // the recursive call
            if (null != ParentColl)
            {
                ParentColl.ParentInstance.GetUrnRecursive(urnbuilder, idOption);
            }

            if (urnbuilder.Length != 0)
            {
                switch (idOption)
                {
                    case UrnIdOption.WithId:
                        // this could be a bug but I keep the old behavior
                        if (!this.Properties.Contains("ID"))
                        {
                            goto case UrnIdOption.NoId;
                        }

                        urnbuilder.AppendFormat(SmoApplication.DefaultCulture,
                            "/{0}[{1} and @ID={2}]", urnsuffix, key.UrnFilter, GetPropValueOptional("ID", 0).ToString(SmoApplication.DefaultCulture));
                        break;
                    case UrnIdOption.OnlyId:
                        // this could be a bug but I keep the old behavior
                        if (!this.Properties.Contains("ID"))
                        {
                            goto case UrnIdOption.NoId;
                        }

                        urnbuilder.AppendFormat(SmoApplication.DefaultCulture,
                            "/{0}[@ID={1}]", urnsuffix, GetPropValueOptional("ID", 0).ToString(SmoApplication.DefaultCulture));
                        break;
                    case UrnIdOption.NoId:
                        urnbuilder.AppendFormat(SmoApplication.DefaultCulture,
                            "/{0}[{1}]", urnsuffix, key.UrnFilter);
                        break;
                }

            }
            else
            {
                // if the parenturn is empty we are in Server object, and we
                // do not append any prefix

                //null if in capture mode and we didn't take the name yet
                if (null == this.GetServerObject().ExecutionManager.TrueServerName)
                {
                    urnbuilder.Append(urnsuffix);
                }
                else
                {
                    urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "{0}[@Name='{1}']", urnsuffix,
                        Urn.EscapeString(this.GetServerObject().ExecutionManager.TrueServerName));
                }
                return;
            }
        }

        /// <summary>
        /// Gets the UrnSuffix from the specified type - or an empty string if the type
        /// does not define a static property named UrnSuffix.
        /// </summary>
        /// <param name="type"></param>
        public static string GetUrnSuffix(Type type)
        {
            PropertyInfo pi = type.GetProperty("UrnSuffix", UrnSuffixBindingFlags);
            if (pi == null)
            {
                return string.Empty;
            }
            return pi.GetValue(null, null) as string;
        }

        /// <summary>
        /// Returns the Urn of the object, computed on the fly
        /// </summary>
        public Urn Urn
        {
            get
            {
                CheckObjectStateImpl(false);
#if INCLUDE_PERF_COUNT
                if( PerformanceCounters.DoCount )
                    PerformanceCounters.UrnCallsCount++;
#endif
                StringBuilder urnbuilder = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                GetUrnRecursive(urnbuilder);
                return new Urn(urnbuilder.ToString());
            }
        }

        // Get Urn where each fragment *contains* the ID, as in [@Name='foo' and @ID='100']
        internal Urn UrnWithId
        {
            get
            {
#if INCLUDE_PERF_COUNT
                if( PerformanceCounters.DoCount )
                    PerformanceCounters.UrnCallsCount++;
#endif
                StringBuilder urnbuilder = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                GetUrnRecursive(urnbuilder, UrnIdOption.WithId);
                return new Urn(urnbuilder.ToString());
            }
        }

        // Get Urn where each fragment *is* the ID, as in [@ID='100']
        // This is only called by DMF
        internal Urn UrnOnlyId
        {
            get
            {
#if INCLUDE_PERF_COUNT
                if( PerformanceCounters.DoCount )
                    PerformanceCounters.UrnCallsCount++;
#endif
                StringBuilder urnbuilder = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                GetUrnRecursive(urnbuilder, UrnIdOption.OnlyId);
                return new Urn(urnbuilder.ToString());
            }
        }

        // this is the key that identifies the object in the collection
        // for the regular objects it will be name
        internal ObjectKeyBase key = null;
        internal void SetObjectKey(ObjectKeyBase key)
        {
            this.key = key;
        }

        /// <summary>
        /// Regular SMO objects access the parent class reference through parentColl (corresponding collection in parent class).
        /// Singleton class has no collection in parent.
        /// </summary>
        protected SqlSmoObject singletonParent = null;


        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
        internal virtual void ValidateParent(SqlSmoObject newParent)
        {
            // we are going to use the parent to get the child collection where this object is
            // going to sit. This is also a validation to make sure newParent can be a
            // parent of this object
            string urnsuffix = SqlSmoObject.GetUrnSuffix(this.GetType());

            if (null == urnsuffix || urnsuffix.Length == 0)
            {
                throw new InternalSmoErrorException(ExceptionTemplates.NoUrnSuffix);
            }

            try
            {
                parentColl = GetChildCollection(newParent, urnsuffix, null, newParent.ServerVersion);
            }
            catch(ArgumentException)
            {
                PropertyInfo childProperty;
                try
                {
                    childProperty = newParent.GetType().GetProperty(this.GetType().Name);
                }
                catch (MissingMethodException)
                {
                    throw new ArgumentException(ExceptionTemplates.InvalidPathChildCollectionNotFound(urnsuffix, newParent.GetType().Name));
                }
                if (null != childProperty)
                {
                    singletonParent = newParent;
                    return;
                }
            }
            if ((null == parentColl)&& (null == singletonParent))
            {
                throw new FailedOperationException(ExceptionTemplates.SetParent, this, null, ExceptionTemplates.InvalidType(newParent.GetType().ToString()));
        }
        }

        internal protected void SetParentImpl(SqlSmoObject newParent)
        {
            try
            {
                if (null == newParent)
                {
                    throw new ArgumentNullException("newParent");
                }

                // if the object is not in pending state, setting the parent is useless
                if (State != SqlSmoState.Pending)
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.OperationOnlyInPendingState);
                }

                // if parent is a pending object, we have to throw because we have no link up,
                // so we can't get metadata
                if (newParent.State == SqlSmoState.Pending)
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.OperationOnlyInPendingState);
                }

                // make sure newParent can be a parent for this object
                ValidateParent(newParent);

                // if the object has schema, then set it here
                ScriptSchemaObjectBase schemaObj = this as ScriptSchemaObjectBase;
                if (schemaObj != null)
                {
                    if (this.key != null && parentColl != null && (null == schemaObj.Schema || schemaObj.Schema.Length == 0))
                    {
                        schemaObj.ChangeSchema(((SchemaCollectionBase)parentColl).GetDefaultSchema(), false);
                    }
                }

                UpdateObjectState();
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.SetParent, this, e);
            }
        }

        internal virtual void UpdateObjectState()
        {
            if (this.State == SqlSmoState.Pending && !key.IsNull && null != parentColl)
            {
                SetState(SqlSmoState.Creating);
        }
        }

        private ScriptingPreferences GetScriptingPreferencesForAlter()
        {
            ScriptingPreferences sp = new ScriptingPreferences();
            sp.SuppressDirtyCheck = false;

            // pass the target version
            sp.SetTargetServerInfo(this);

            sp.ScriptForAlter = true;
            // using this option will avoid any script name/owner substitutions
            sp.ForDirectExecution = true;

            sp.IncludeScripts.Associations = true;
            sp.OldOptions.Bindings = true;
            sp.Data.ChangeTracking = true;
            sp.IncludeScripts.Owner = true;
            return sp;
        }

        /// <summary>
        /// method get called from the create script related method (from derived classes like Table, Index etc..)
        /// </summary>
        /// <param name="queries"></param>
        internal void AddDatabaseContext(StringCollection queries, ScriptingPreferences sp)
        {
            if (DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType)
            {
                return;
            }

            string dbName = this.GetDBName();

            if (string.IsNullOrEmpty(dbName))
            {
                if (this.parentColl != null && this.ParentColl.ParentInstance is Server)
                {
                    dbName = "master";
                }
            }

            if (0 == dbName.Length)
            {
                return;
            }

            string useDbScript = string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlSmoObject.SqlBraket(dbName));

            if ((0 == queries.Count) || (0 != string.Compare(queries[0], useDbScript, StringComparison.Ordinal)))
            {
                queries.Add(useDbScript);
            }
        }

        /// <summary>
        /// method get called from the create script related method (from derived classes like Table, Index etc..)
        /// </summary>
        /// <param name="queries"></param>
        protected void AddDatabaseContext(StringCollection queries)
        {
            AddDatabaseContext(queries, new ScriptingPreferences(this));
        }

        ///<summary>
        /// changes the object according to the modification of its members
        ///</sumary>
        protected void AlterImplWorker()
        {
            CheckObjectState();

            StringCollection alterQuery;
            ScriptingPreferences sp;

            AlterImplInit(out alterQuery, out sp);
            sp.IncludeScripts.DatabaseContext = true;
            ScriptAlterInternal(alterQuery, sp);
            AlterImplFinish(alterQuery, sp);
        }

        private void CheckNonAlterableProperties()
        {
            // check if the user has set any of the properties that are read-only
            // after the object has been created
            foreach (string propName in GetNonAlterableProperties())
            {
                if (Properties.Contains(propName))
                {
                    Property prop = Properties.Get(propName);
                    if (null != prop.Value && prop.Dirty)
                    {
                        // the user has modified this property, so we have to throw
                        throw new SmoException(ExceptionTemplates.PropNotModifiable(propName, this.GetType().Name));
                    }
                }
            }
        }

        internal void AlterImplInit(out StringCollection alterQuery, out ScriptingPreferences sp)
        {
            if (!this.ExecutionManager.Recording)
            {
                if (this.State != SqlSmoState.Existing)
                {
                    throw new InvalidSmoOperationException("Alter", this.State);
            }
            }

            // check to see if the user has set properties that cannot
            // be altered. Do that only if we are in execution mode
            if (!this.ExecutionManager.Recording)
            {
                CheckNonAlterableProperties();
            }

            InitializeKeepDirtyValues();

            sp = GetScriptingPreferencesForAlter();
            alterQuery = new StringCollection();
        }

        internal void AlterImplFinish(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (!this.IsDesignMode)
            {
                // execute the script
                ExecuteNonQuery(alterQuery, executeForAlter: true);
            }

            PostAlter();

            // update object state to only if we are in execution mode
            if (!this.ExecutionManager.Recording)
            {
                // mark all properties as non dirty, since we propagated the changes to the real object
                CleanObject();

                //propagate to the children collection state update and cleanup after alter
                PropagateStateAndCleanUp(alterQuery, sp, PropagateAction.Alter);
            }
        }

        ///<summary>
        /// changes the object according to the modification of its members
        ///</sumary>
        protected void AlterImpl()
        {
            try
            {
                AlterImplWorker();

                this.GenerateAlterEvent();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
            }
        }

        protected void GenerateAlterEvent(Urn urn, object innerObject)
        {
            if (!this.ExecutionManager.Recording)
            {
                // generate internal events
                if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                {
                    SmoApplication.eventsSingleton.CallObjectAltered(GetServerObject(),
                        new ObjectAlteredEventArgs(urn, innerObject));
                }
            }
        }

        protected void GenerateAlterEvent()
        {
            this.GenerateAlterEvent(this.Urn, this);
        }

        // ScriptAlter does nothing here, the function must be overriden by derived classes
        internal virtual void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
        }

        internal void ScriptAlterInternal(StringCollection alterQuery, ScriptingPreferences sp)
        {
            ScriptAlter(alterQuery, sp);

            //propagate alter to children collections
            PropagateScript(alterQuery, sp, PropagateAction.Alter);
        }

        ///<summary>
        /// refreshes the object's properties by reading them from the server
        ///</summary>
        public virtual void Refresh()
        {
            try
            {
                CheckObjectStateImpl(false);

                // Make object state transitions
                if (this.State == SqlSmoState.Creating && this.Initialize())
                {
                    SetState(SqlSmoState.Existing);
                }
                else if (this.State == SqlSmoState.Existing)
                {
                    // verify that the object has not been dropped
                    //                    try
                    //                    {
                    System.Data.IDataReader reader = null;
                    try
                    {
                        // limit the request to fields composing the key if we have any
                        // if there are no fields in the key this is a singleton and
                        // it does not make sense to check for its existance
                        Diagnostics.TraceHelper.Assert(null != key, "null == key");
                        StringCollection keyFields = key.GetFieldNames();
                        if (keyFields.Count > 0)
                        {
                            string[] fields = new string[keyFields.Count];
                            keyFields.CopyTo(fields, 0);
                            reader = GetInitDataReader(fields, null);
                            if (reader == null)
                            {
                                SetState(SqlSmoState.Dropped);
                            }
                        }
                    }
                    finally
                    {
                        if (null != reader)
                        {
                            reader.Close();
                            reader = null;
                        }
                    }
                    //                    }
                    //                    catch (FailedOperationException)
                    //                    {
                    //                        SetState(SqlSmoState.Dropped);
                    //                    }
                }

                // implement Refresh by clearing the property bag
                properties = null;
                propertyBagState = PropertyBagState.Empty;
                initializedForScripting = false;


                // clean the permissions cache. We need to do it here as opposed to
                // the other collections because this is not exposed publicly.
                userPermissions = null;

                m_ExtendedProperties = null;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Refresh, this, e);
            }
        }

        internal void ReCompile(string name, string schema)
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                if (String.IsNullOrEmpty(schema))
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_recompile @objname=N'[{0}]'", SqlString(name)));
                }
                else
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_recompile @objname=N'[{0}].[{1}]'", SqlString(schema), SqlString(name)));
                }

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReCompileReferences, this, e);
            }
        }

        internal override object GetPropertyDefaultValue(string propname)
        {
            if (IsDesignMode && this.State == SqlSmoState.Existing)
            {
                // Retrieve the default value using SFC metadata
                Property property = Properties.Get(propname);
                if (null != property)
                {
                    object defaultValue = null;
                    SfcMetadataDiscovery metadata = new SfcMetadataDiscovery(this.GetType());
                    SfcMetadataRelation propertyMetadata = metadata.FindProperty(propname);
                    if (null != propertyMetadata)
                    {
                        defaultValue = propertyMetadata.PropertyDefaultValue;
                        if (defaultValue != null && defaultValue.ToString() == "string.empty")
                        {
                            defaultValue = string.Empty;
                        }
                    }

                    if (null == defaultValue)
                    {
                        // There is no default value in metadata for this property
                        // See if we can create a default value based on property type
                        switch (property.Type.FullName)
                        {
                            case "System.Boolean":
                                defaultValue = false;
                                break;
                            case "System.DateTime":
                                defaultValue = DateTime.MinValue;
                                break;
                            case "System.DateTimeOffset":
                                defaultValue = DateTimeOffset.MinValue;
                                break;
                            case "System.TimeSpan":
                                defaultValue = TimeSpan.MinValue;
                                break;
                            case "System.Int32":
                                defaultValue = 0;
                                break;
                            case "System.Int64":
                                defaultValue = 0L;
                                break;
                            case "System.UInt32":
                                defaultValue = 0U;
                                break;
                            case "System.UInt64":
                                defaultValue = 0UL;
                                break;
                            case "System.Single":
                                defaultValue = 0.0F;
                                break;
                            case "System.Double":
                                defaultValue = 0.0;
                                break;
                        }
                    }

                    if (null != defaultValue)
                    {
                        // If we have obtained a non-null value, return it
                        // Otherwise continue down to the base class handler
                        return defaultValue;
                    }
                }

                Trace("DesignMode Missing " +
                        ((Properties.Get(propname).Expensive) ? "expensive" : "regular") +
                        " property " + propname + " for type " + this.GetType().Name);
            }

            return base.GetPropertyDefaultValue(propname);
        }

        ///<summary>
        ///Called when one of the properties is missing from the property collection
        ///</summary>
        internal override object OnPropertyMissing(string propname, bool useDefaultValue)
        {
            if (useDefaultValue)
            {
                switch (this.State)
                {
                    case SqlSmoState.Pending:
                    case SqlSmoState.Creating:
                        return GetPropertyDefaultValue(propname);

                    case SqlSmoState.Existing:
                        if (this.IsDesignMode) // Expected to use Existing State only in Design Mode
                        {
                            return GetPropertyDefaultValue(propname);
                        }
                        break;
                }
            }
            else
            {
                // we shouldn't try to get properties for an object that does not exist
                switch (this.State)
                {
                    case SqlSmoState.Pending:
                        Diagnostics.TraceHelper.Assert(false); // can't happen through user code, intercepted earlier
                        break;
                    case SqlSmoState.Creating:
                        throw new PropertyNotSetException(propname);
                }
            }

            System.Diagnostics.Trace.TraceWarning("Missing " +
                ((Properties.Get(propname).Expensive) ? "expensive" : "regular") +
                " property " + propname + " property bag state " + propertyBagState +
                " for type " + this.GetType().Name);
            var missingPropertyArgs = new PropertyMissingEventArgs(propname, this.GetType().Name);
            //treat first the expensive properties
            Property prop = Properties.Get(propname);
            if (prop.Expensive)
            {
                String[] fields = new String[1];
                fields[0] = propname;
                if (!this.IsDesignMode)
                {

                    PropertyMissing(this, missingPropertyArgs);
                    // Skip the retrieval when in Design Mode
                    ImplInitialize(fields, null);
                }
                return prop.Value;
            }


            switch (propertyBagState)
            {
                case PropertyBagState.Empty:
                    // if the field is in the default list, do a lazy initialization
                    // otherwise do a full initialization
                    var fullInit = !GetServerObject().IsInitField(this.GetType(), propname);
                    if (fullInit)
                    {
                        PropertyMissing(this, missingPropertyArgs);
                    }
                    Initialize(fullInit);
                    return prop.Value;

                case PropertyBagState.Lazy:
                    PropertyMissing(this, missingPropertyArgs);
                    Initialize(true);
                    return prop.Value;

                case PropertyBagState.Full:
                    throw new InternalSmoErrorException(ExceptionTemplates.FullPropertyBag(propname));
            }


            return null;
        }


        // initializes an object based on urn
        // Object in Lazy mode, with a subset of its properties
        public bool Initialize()
        {
            return Initialize(false);
        }

        /// <summary>
        /// Initializes the object, by reading its properties from the enumerator
        /// </summary>
        /// <param name="fullPropList">If false, only a specified subset of the properties
        /// are retrieved with this call</param>
        /// <returns></returns>
        public bool Initialize(bool allProperties)
        {
            CheckObjectState();
#if INCLUDE_PERF_COUNT
            if( PerformanceCounters.DoCount )
                PerformanceCounters.InitializeCallsCount++;
#endif
            // if the object does not exist or it has already been initialized there is
            // no point in initializing the object.
            // In Design Mode the property bag is never read from the backend
            if (this.IsDesignMode || (allProperties && IsObjectInitialized()))
            {
                return false;
            }

            string[] fields = null;
            OrderBy[] orderby = null;
            if (!allProperties)
            {
                // lazy initialization
                fields = GetServerObject().GetDefaultInitFieldsInternal(this.GetType(), this.DatabaseEngineEdition);
                //orderby = new OrderBy[1] { new OrderBy("Name", OrderBy.Direction.Asc)};
            }

            bool bInit = ImplInitialize(fields, orderby);

            // change object state according to the result of the initialization
            if (allProperties)
            {
                if (bInit)
                {
                    propertyBagState = PropertyBagState.Full;
            }
            }
            else
            {
                if (bInit)
                {
                    if (propertyBagState != PropertyBagState.Full)
                    {
                        propertyBagState = PropertyBagState.Lazy;
                    }
                }
            }

            return bInit;
        }

        internal protected bool IsObjectInitialized()
        {
            if ((this.State != SqlSmoState.Existing && this.State != SqlSmoState.ToBeDropped) ||
                propertyBagState == PropertyBagState.Full)
            {
                return true;
            }

            return false;
        }

        // this function is initializing properties for an object before scripting
        // and does not override properties that are set by the user
        internal bool InitializeKeepDirtyValues()
        {
            CheckObjectState();
            if (IsObjectInitialized() || InitializedForScripting
                // In design mode we cannot retrieve data, so we work with the properties that have already been set
                || this.IsDesignMode)
            {
                return false;
            }

            //there are objects which have only one property, the Urn.
            //don't attempt to initialize those.
            if (this.Properties.Count <= 0 && parentColl == null)
            {
                return true;
            }

#if INCLUDE_PERF_COUNT
            if( PerformanceCounters.DoCount )
                PerformanceCounters.InitializeCallsCount++;
#endif

            try
            {
                System.Data.IDataReader reader = null;
                try
                {
                    reader = GetInitDataReader(null, null);
                    AddObjectPropsFromDataReader(reader, true);
#if DEBUG
                    if (reader.Read())
                    {
                        throw new InternalSmoErrorException(ExceptionTemplates.MultipleRowsForUrn(this.Urn));
                    }
#endif
                }
                finally
                {
                    if (null != reader)
                    {
                        reader.Close();
                    }
                }
            }
            catch (FailedOperationException)
            {
                return false;
            }

            propertyBagState = PropertyBagState.Full;
            return true;
        }


        /// <summary>
        /// Retrieves a System.Data.IDataReader object that will contain the result of the query
        /// to obtain the object's properties.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="orderby"></param>
        /// <returns></returns>
        private System.Data.IDataReader GetInitDataReader(string[] fields, OrderBy[] orderby)
        {
            Urn urn = this.Urn;

            // build the request object
            Request req = new Request();
            req.Urn = urn;

            // speed up enumerator queries : do not request Urn when all the properties are
            // requested, we are ignoring it anyway
            if (fields == null)
            {
                // accept all non expensive properties besides Urn
                req.Fields = GetRejectFields();
                req.RequestFieldsTypes = RequestFieldsTypes.Reject;
            }
            else
            {
                req.Fields = fields;
            }
            req.OrderByList = orderby;

            // retrieve the data into the property collection
            // This query should return just one row !
            System.Data.IDataReader reader = this.ExecutionManager.GetEnumeratorDataReader(req);
            Diagnostics.TraceHelper.Assert(null != reader, "reader == null");

            // if the table has no rows this means that initialization of the object has failed
            if (!reader.Read())
            {
                reader.Close();
                System.Diagnostics.Trace.TraceWarning("Failed to Initialize urn " + urn);
                //throw new FailedOperationException(ExceptionTemplates.FailedtoInitialize(urn));
                return null; // this seems to be "normal", so why throw?
            }

            return reader;
        }

        /// <summary>
        /// Returns fields we will not bring while initializing the object.
        /// Instance classes can customize behavior by overriding this.
        /// </summary>
        /// <returns></returns>
        internal virtual string[] GetRejectFields()
        {
            return new string[] { "Urn" };
        }

        // initializes an object with a list of properties
        protected virtual bool ImplInitialize(string[] fields, OrderBy[] orderby)
        {
            //there are objects which have only one property, the Urn.
            //don't attempt to initialize those.
            if ((this.Properties.Count <= 0 && parentColl == null) ||
                (null != fields && fields.Length == 0))
            {
                //initialization succedded, there was nothing to initialize
                return true;
            }

            System.Data.IDataReader reader = null;
            try
            {
                reader = GetInitDataReader(fields, orderby);
                if (reader == null)
                {
                    return false;
                }

                AddObjectPropsFromDataReader(reader, true);
#if DEBUG
                if (reader.Read())
                {
                    throw new InternalSmoErrorException(ExceptionTemplates.MultipleRowsForUrn(this.Urn));
                }
#endif
            }
            finally
            {
                if (null != reader)
                {
                    reader.Close();
                }
            }

            return true;
        }

        /// <summary>
        /// Populates the object's property bag from the current row of the DataReader
        /// </summary>
        /// <param name="reader"></param>
        void ISqlSmoObjectInitialize.InitializeFromDataReader(System.Data.IDataReader reader)
        {
            AddObjectPropsFromDataReader(reader, false);
        }

        /// <summary>
        /// Populates the object's property bag from the current row of the DataReader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="skipIfDirty">If true do not initialize the property if it has
        /// been changed by the user</param>
        internal void AddObjectPropsFromDataReader(System.Data.IDataReader reader, bool skipIfDirty)
        {
            AddObjectPropsFromDataReader(reader, skipIfDirty, 0, -1);
        }

        /// <summary>
        /// Populates the object's property bag from the current row of the DataReader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="skipIfDirty">If true do not initialize the property if it has
        /// been changed by the user</param>
        /// <param name="startColIdx">Index of the first column</param>
        /// <param name="endColIdx">Index of the last column. If -1 then go to the end.</param>
        internal virtual void AddObjectPropsFromDataReader(System.Data.IDataReader reader, bool skipIfDirty,
            int startColIdx, int endColIdx)
        {
            var schemaTable = reader.GetSchemaTable();
            Diagnostics.TraceHelper.Assert(null != schemaTable, "reader.GetSchemaTable()==null");
            Diagnostics.TraceHelper.Assert(schemaTable.Rows.Count == reader.FieldCount, "schemaTable.Rows.Count != reader.FieldCount");

            int colNameIdx = schemaTable.Columns.IndexOf("ColumnName");
            Diagnostics.TraceHelper.Assert(colNameIdx > -1, "IndexOf(\"ColumnName\")==-1");

            for (int i = startColIdx; i < (endColIdx >= 0 ? endColIdx : schemaTable.Rows.Count); i++)
            {
                string columnName = schemaTable.Rows[i][colNameIdx] as string;
                Diagnostics.TraceHelper.Assert(null != columnName, "schemaTable.Rows[i][\"ColumnName\"]==null");

                object colValue = reader.GetValue(i);

                Property prop;

                int propIndex = this.Properties.PropertiesMetadata.PropertyNameToIDLookup(columnName);

                if (propIndex >= 0)
                {
                    prop = Properties.Get(propIndex);
                }
                else
                {
                    continue;
                }

                // At this point prop must exist and be non-null
                prop.SetRetrieved(true);
                if (skipIfDirty && prop.Dirty)
                {
                    continue;
                }
                else if (prop.Enumeration)
                {
                    // if the property is an enumeration then we have to
                    // change its type from Int32 to the specific enum

                    if (DBNull.Value.Equals(colValue))
                    {
                        prop.SetValue(null);
                    }
                    else if (prop.Type.Equals(typeof(System.Guid)))
                    {
                        // GUID values come as strings, we need to convert explicitly
                        prop.SetValue(new Guid((string)colValue));
                    }
                    else
                    {
                        // enumeration values come as Int32s, so we need to convert
                        // explicitly when populating the property bag.
                        prop.SetValue(Enum.ToObject(prop.Type,
                            Convert.ToInt32(colValue, SmoApplication.DefaultCulture)));
                    }
                }
                else
                {
                    if (DBNull.Value.Equals(colValue))
                    {
                        if (prop.Type.Equals(typeof(DateTime)))
                        {
                            prop.SetValue(DateTime.MinValue);
                        }
                        else if (prop.Type.Equals(typeof(DateTimeOffset)))
                        {
                            prop.SetValue(DateTimeOffset.MinValue);
                        }
                        else if (prop.Type.Equals(typeof(TimeSpan)))
                        {
                            prop.SetValue(TimeSpan.MinValue);
                        }
                        else
                        {
                            prop.SetValue(null);
                        }
                    }
                    else
                    {
                        prop.SetValue(colValue);
                    }
                }
            }
        }

        internal SqlPropertyCollection properties;
        // property collection
        public SqlPropertyCollection Properties
        {
            get
            {
                // call the non virtual method
                CheckObjectStateImpl(false);

                //check if properties collection is not initialized yet
                if (properties == null)
                {
                    //initialize properties collection
                    this.properties = new SqlPropertyCollection(this, this.GetPropertyMetadataProvider());
                }
                return properties;
            }
        }

        internal virtual string[] GetNonAlterableProperties()
        {
            return new string[] { };
        }

        /// <summary>
        /// Retrieve the property value from :
        ///     - The property bag directly if property is available (dirty or retrieved)
        ///     - A call to our OnPropertyMissing method otherwise
        ///
        /// Will throw an exception if the property value is NULL in either case.
        /// </summary>
        /// <param name="propName"></param>
        /// <returns></returns>
        internal protected object GetPropValue(string propName)
        {
            // Fetch value directly from property bag (no backend retrieval)
            if (State == SqlSmoState.Creating && null == Properties.Get(propName).Value)
            {
                throw new PropertyNotSetException(propName);
            }

            //Will call our OnPropertyMissing method if the property is available
            //Will throw an exception on a NULL value
            return Properties[propName].Value;
        }

        /// <summary>
        /// Retrieve the property value from :
        ///     - The property bag if the state is creating or we're in Design Mode (possible NULL value)
        ///     - A call to our OnPropertyMissing method otherwise (will throw exception if value is NULL)
        /// </summary>
        /// <param name="propName"></param>
        /// <returns></returns>
        internal protected object GetPropValueOptional(string propName)
        {
            return GetPropertyOptional(propName).Value;
        }

        /// <summary>
        /// Retrieve the property value from :
        ///     - The property bag if the state is creating or we're in Design Mode (possible NULL value)
        ///     - A call to our OnPropertyMissing method otherwise (will throw exception if value is NULL)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propName"></param>
        /// <returns></returns>
        internal Nullable<T> GetPropValueOptional<T>(string propName) where T : struct
        {
            object propVal = GetPropValueOptional(propName);
            if (null == propVal)
            {
                return new Nullable<T>();
            }
                return (Nullable<T>)(T)propVal;
        }

        /// <summary>
        /// Returns the value of the named property.
        /// If the object is in the Creating state and the property has not yet been set the defaultValue is returned.
        /// If the object exists the defaultValue is ignored and the actual value is returned.
        /// </summary>
        /// <param name="propName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetPropValueOptional<T>(string propName, T defaultValue)
        {
            object retVal = GetPropValueOptional(propName);
            if (null != retVal)
            {
                return (T)retVal;
            }
            return defaultValue;
        }

        /// <summary>
        /// Retrieve the property value from :
        ///     - The property bag if the state is creating or we're in Design Mode (possible NULL)
        ///     - A call to our OnPropertyMissing method otherwise (possible NULL)
        /// </summary>
        /// <param name="propName"></param>
        /// <returns></returns>
        internal protected object GetPropValueOptionalAllowNull(string propName)
        {
            if (State == SqlSmoState.Creating)
            {
                // Fetch value directly from property bag (no backend retrieval)
                // Will NOT throw exception on NULL value
                return Properties.Get(propName).Value;
            }

            //Will call our OnPropertyMissing method if the property is available
            //Will NOT throw exception on NULL value
            return Properties.GetPropertyObjectAllowNull(propName).Value;
        }

        /// <summary>
        /// Retrieves the value of the specified property, the exact value is determined by the supportability of the source
        /// and target server:
        ///    - If property is NOT supported on either source or target server(won't check target if 'sp' is NULL)
        ///      returns the default value
        ///    - Otherwise returns the value of the property (default value if the actual value is NULL)
        /// </summary>
        /// <typeparam name="T">The type of the property's value.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default value if actual value is null.</param>
        /// <param name="sp">The scripting preferences (must be provided for target server check!)</param>
        /// <returns>The property or default value depending on if the property is supported.</returns>
        internal T GetPropValueIfSupported<T>(string propertyName, T defaultValue, ScriptingPreferences sp = null)
        {
            if (IsSupportedProperty(propertyName))
            {
                T value = GetPropValueOptional<T>(propertyName, defaultValue);

                if (sp != null && !IsSupportedProperty(propertyName, sp))
                {
                    return defaultValue;
                }

                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Retrieves the value of the specified property, the exact value is determined by the supportability of the source
        /// and target server(possible to throw):
        ///    - If property is NOT supported on source server returns the default value
        ///    - If property is supported on source server and it's value is not the specified default value,
        ///      throw if NOT supported on target server(note if 'sp' is NULL this check is skipped)
        ///    - Otherwise returns the value of the property (default value if the actual value is NULL)
        /// </summary>
        /// <typeparam name="T">The type of the property's value.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default value if actual value is null.</param>
        /// <param name="sp">The scripting preferences (must be provided for target server check!)</param>
        /// <returns>The property or default value depending on if the property is supported.</returns>
        internal T GetPropValueIfSupportedWithThrowOnTarget<T>(string propertyName, T defaultValue, ScriptingPreferences sp = null)
        {
            if (IsSupportedProperty(propertyName))
            {
                T value = GetPropValueOptional<T>(propertyName, defaultValue);

                if (sp != null && !value.Equals(defaultValue))
                {
                    ThrowIfPropertyNotSupported(propertyName, sp);
                }

                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Retrieve the property from :
        ///     - The property bag if the state is creating or we're in Design Mode (property may have NULL value)
        ///     - A call to our OnPropertyMissing method otherwise (will throw exception if property value is NULL)
        /// </summary>
        /// <param name="propName"></param>
        /// <returns></returns>
        internal Property GetPropertyOptional(string propName)
        {
            if (this.State == SqlSmoState.Creating)
            {
                // Fetch value directly from property bag (no backend retrieval)
                // Will NOT throw exception on NULL value
                return this.Properties.Get(propName);
            }

            if (this.IsDesignMode)
            {
                // Fetch value directly from property bag (no backend retrieval)
                // Will NOT throw exception on NULL value
                return this.Properties.GetPropertyObject(propName, true);
            }

            //Will call our OnPropertyMissing method if the property is available
            //Will throw an exception on a NULL value
            return this.Properties[propName];
        }

        /// <summary>
        /// Returns the real value for the property.
        /// </summary>
        /// <param name="prop">The property object</param>
        /// <param name="value">Old value. If it is null the function queries the
        /// database for the old value.</param>
        /// <returns></returns>
        protected object GetRealValue(Property prop, object oldValue)
        {
            if (null != oldValue)
            {
                return oldValue;
            }
            else
            {
                Request reqValue = new Request(this.Urn, new string[] { prop.Name });
                DataTable dt = this.ExecutionManager.GetEnumeratorData(reqValue);
                if (dt.Rows.Count == 0 || dt.Columns.Count == 0)
                {
                    return prop.Value;
                }
                else
                {
                    return dt.Rows[0][0];
            }
        }
        }

        virtual protected string GetServerName()
        {
            return GetServerObject().Name;
        }

        // this would be the server object that sits at the root of the local tree
        // we cache it, since there is going to be a lot of references to it
        private Server m_server = null;

        private Server TryGetServerObject()
        {
            if (null == m_server)
            {
                // climb up the tree to the server object
                SqlSmoObject current = this;

                m_server = current as Server;

                if (null == m_server) //ask the parent
                {
                    if (null != current.ParentColl && null != current.ParentColl.ParentInstance)
                    {
                        //if it is not the server it must have a parent
                        m_server = current.ParentColl.ParentInstance.TryGetServerObject();
                    }
                    else if (null == current.ParentColl)
                    {
                        //When getting the parent object we don't want to throw if it's in the creating state
                        //since we don't care about the intermediate objects as long as they have a parent
                        //(and the server should never been in a non-Existing state)
                        m_server = current.GetParentObject(false).TryGetServerObject();
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return m_server;
        }

        protected internal Server GetServerObject()
        {
            Server server = TryGetServerObject();
            if (server != null)
            {
                return server;
            }
            throw new InternalSmoErrorException(ExceptionTemplates.ObjectNotUnderServer(this.GetType().ToString()));
        }

        internal void SetServerObject(Server server)
        {
            m_server = server;
        }

        internal protected virtual string GetDBName()
        {
            SqlSmoObject current = this;
            while (null != current.ParentColl && !(current is Database))
            {
                current = current.ParentColl.ParentInstance;
            }

            if (null == current.ParentColl)
            {
                if (current is ServiceBroker)
                {
                    return ((ServiceBroker)current).Parent.GetDBName();
                }
                else
                {
                    if (current is FullTextIndex)
                    {
                        return ((FullTextIndex)current).Parent.GetDBName();
                }
                }
                return string.Empty;
            }
            else
            {
                return ((Database)current).Name;
            }
        }

        internal protected Database GetContextDB()
        {
            SqlSmoObject current = this;
            while (null != current.ParentColl && !(current is Database))
            {
                current = current.ParentColl.ParentInstance;
            }

            if (null != current.ParentColl)
            {
                return (Database)current;
            }

            return null;
        }


        // tracing stuff
        internal protected static void Trace(string traceText)
        {
            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, "{0}", traceText);
        }


        /// <summary>
        /// Controls how CreateImpl executes a query. true means a single record is fetched after the query executes and the
        /// value is stored in the property ScalarResult
        /// </summary>
        bool executeForScalar = false;
        protected bool ExecuteForScalar
        {
            get { return executeForScalar; }
            set { executeForScalar = value; }
        }

        /// <summary>
        /// This is the result from a scalar query execution
        /// </summary>
        object[] scalarResult = null;
        protected object[] ScalarResult
        {
            get { return scalarResult; }
        }

        // this function is going to filter exceptions, and in some cases will
        // decide not to wrap exceptions in an SmoException but to just throw
        // it unchanged
        internal static void FilterException(Exception e)
        {
            if (e is OutOfMemoryException)
            {
                throw e;
        }
        }

        protected void CreateImpl()
        {
            try
            {
                StringCollection createQuery;
                ScriptingPreferences sp;

                //
                // create initialize.
                // add any pre-create activity in CreateImplInit
                //

                CreateImplInit(out createQuery, out sp);

                //
                // get the script
                //


                ScriptCreateInternal(createQuery, sp);

                //
                // create execute, cleanup and state update.
                // add any post-create activity in CreateImplFinish
                //

                CreateImplFinish(createQuery, sp);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
        }

        protected void CreateOrAlterImpl()
        {
            try
            {
                StringCollection createOrAlterQuery;
                ScriptingPreferences sp;

                //
                // create or alter initialize
                // add any pre activity in CreateOrAlterImplInit

                CreateOrAlterImplInit(out createOrAlterQuery, out sp);

                //
                // get the script
                //

                ScriptCreateOrAlterInternal(createOrAlterQuery, sp);

                //
                // create or alter execute, cleanup and state update
                // add any post activity in CreateOrAlterImplFinish

                CreateOrAlterImplFinish(createOrAlterQuery, sp);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CreateOrAlter, this, e);
            }
        }

        internal virtual void ScriptCreateOrAlter(StringCollection query, ScriptingPreferences sp)
        {
            throw new NotSupportedException(ExceptionTemplates.CreateOrAlterNotSupported(this.GetType().Name));
        }

        internal virtual void ScriptCreateOrAlterInternal(StringCollection query, ScriptingPreferences sp)
        {
            ScriptCreateOrAlterInternal(query, sp, skipPropagateScript: false);
        }

        internal virtual void ScriptCreateOrAlterInternal(StringCollection query, ScriptingPreferences sp, bool skipPropagateScript)
        {
            ScriptCreateOrAlter(query, sp);

            if (this.State != SqlSmoState.Existing)
            {
                // script permissions if needed
                if (sp.IncludeScripts.Permissions)
                {
                    AddScriptPermission(query, sp);
                }
            }

            if (!skipPropagateScript)
            {
                //propagate create to children collections
                PropagateScript(query, sp, PropagateAction.CreateOrAlter);
            }
        }


        // set some default scripting options
        internal ScriptingPreferences GetScriptingPreferencesForCreate()
        {
            // set some default scripting options
            ScriptingPreferences sp = new ScriptingPreferences();
            sp.SuppressDirtyCheck = false;

            sp.IncludeScripts.Associations = true;
            sp.OldOptions.Bindings = true;


            sp.ScriptForCreateDrop = true;
            // using this option will avoid any script name/owner substitutions
            sp.ForDirectExecution = true;

            // Do not emit IF NOT EXISTS clause when creating an object directly
            sp.IncludeScripts.ExistenceCheck = false;
            sp.Data.ChangeTracking = true;
            sp.IncludeScripts.Owner = true;

            // initialize the target server version
            sp.SetTargetServerInfo(this);
            return sp;
        }

        /// <summary>
        /// Gets the parent object or collection of this object.
        /// </summary>
        /// <param name="throwIfParentIsCreating">If TRUE exception will be thrown if parent exists but its state is Creating</param>
        /// <param name="throwIfParentNotExist">If TRUE return null if a parent doesn't exist, if FALSE an exception will be thrown</param>
        /// <returns></returns>
        /// <remarks>Will throw FailedOperationException if Parent does not exist</remarks>
        private SqlSmoObject GetParentObject(bool throwIfParentIsCreating = true, bool throwIfParentNotExist = true)
        {
            // if the object belongs to a collection, then get the parent walking up the tree
            AbstractCollectionBase parentColl = this.parentColl;
            SqlSmoObject objParent = null;

            if (null != parentColl)
            {
                objParent = parentColl.ParentInstance;
            }
            else if (null != singletonParent)
            {
                objParent = singletonParent;
            }

            if (null != objParent)
            {
                if (objParent.State == SqlSmoState.Creating && throwIfParentIsCreating)
                {
                    throw new FailedOperationException(ExceptionTemplates.ParentMustExist(this.GetType().Name, this.FullQualifiedName));
            }
            }
            else if(throwIfParentNotExist)
            {
                throw new FailedOperationException(ExceptionTemplates.NeedToSetParent);
            }


            return objParent;
        }

        /// <summary>
        /// executes sql statements
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="executeForAlter"></param>
        private void ExecuteNonQuery(StringCollection queries, bool executeForAlter)
        {
            ExecuteNonQuery(queries, true, executeForAlter);
        }

        /// <summary>
        /// Executes sql statements
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="includeDbContext">if true the statements are prefixed with
        /// setting the database context, if false they are not</param>
        /// <param name="executeForAlter">Whether this execution is for an alter.</param>
        protected void ExecuteNonQuery(StringCollection queries, bool includeDbContext, bool executeForAlter)
        {
            var retry = true;
            if (queries.Count <= 0)
            {
                return;
            }
            // we are generally using the same database during the operation
            string dbName = GetDBName();
            if (null != dbName &&
                this.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
            {
                string useDbName = string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(dbName));
                if ((includeDbContext) && !(queries[0].StartsWith(useDbName) ||
                                                    queries[0].StartsWith(Scripts.USEMASTER)))
                {
                    if (dbName.Length > 0 && !typeof(Database).Equals(this.GetType()))
                    {
                        queries.Insert(0, useDbName);
                    }
                    else
                    {
                        queries.Insert(0, Scripts.USEMASTER);
                    }
                }
            }

            ExecutionManager executionManager = this.ExecutionManager;
            if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase && typeof(Database).Equals(this.GetType()) && !executeForAlter)
            {
                // Create/Drop etc. database methods in the Database object
                //     need a connection to the master (i.e Server's ExecutionManager)
                //     Alter requires a connection to the DB.
                executionManager = this.GetServerObject().ExecutionManager;
                retry = false; // Drop queries on the user database connection will close the connection
            }

            if (executeForScalar)
            {
                scalarResult = executionManager.ExecuteScalar(queries);
            }
            else
            {
                executionManager.ExecuteNonQuery(queries, retry);
            }

        }

        internal void CreateImplInit(out StringCollection createQuery, out ScriptingPreferences sp)
        {
            //Init ScalarResult property
            scalarResult = null;

            if (this.State == SqlSmoState.Pending)
            {
                throw new FailedOperationException(ExceptionTemplates.InvalidOperationInDisconnectedMode);
            }

            if (!this.ExecutionManager.Recording)
            {
                CheckObjectState();
                // if the object is not in the empty state, a previous call to the
                // enumerator returned the object, which means that the object
                // already exists, and we throw
                if (this.State != SqlSmoState.Creating)
                {
                    throw new FailedOperationException(ExceptionTemplates.ObjectAlreadyExists(this.Urn.Type, this.key.ToString()));
                }

                //check parent object exists
                GetParentObject();
            }

            createQuery = new StringCollection();
            sp = GetScriptingPreferencesForCreate();
        }

        internal void CreateImplFinish(StringCollection createQuery, ScriptingPreferences sp)
        {
            if (createQuery.Count <= 0)
            {
                Urn urn = this.Urn;
                throw new FailedOperationException(ExceptionTemplates.NoSqlGen(urn.ToString()));
            }

            if (!this.IsDesignMode)
            {
                // don't include database context when creating database
                ExecuteNonQuery(createQuery, !(this is Database), executeForAlter: false);
            }

            PostCreate();

            // update object state to only if we are in execution mode
            if (!this.ExecutionManager.Recording)
            {
                SetState(SqlSmoState.Existing);

                // if the object was in space, we need to add it to the collection.
                if (this.objectInSpace)
                {
                    if (null != ParentColl)
                    {
                        ParentColl.AddExisting(this);
                    }

                    objectInSpace = false;
                }

                // reset all properties
                CleanObject();

                //propagate to the children collection state update and cleanup after create
                PropagateStateAndCleanUp(createQuery, sp, PropagateAction.Create);

                PostPropagate();
            }

            if (!this.ExecutionManager.Recording)
            {
                // generate internal events
                if (!SmoApplication.eventsSingleton.IsNullObjectCreated())
                {
                    SmoApplication.eventsSingleton.CallObjectCreated(GetServerObject(),
                        new ObjectCreatedEventArgs(this.Urn, this));
                }
            }

        }

        internal void CreateOrAlterImplInit(out StringCollection createOrAlterQuery, out ScriptingPreferences sp)
        {
            CheckObjectState();

            if (this.State != SqlSmoState.Existing)
            {
                CreateImplInit(out createOrAlterQuery, out sp);
            }
            else
            {
                AlterImplInit(out createOrAlterQuery, out sp);
                sp.IncludeScripts.DatabaseContext = true;
            }
        }

        internal void CreateOrAlterImplFinish(StringCollection createOrAlterQuery, ScriptingPreferences sp)
        {
            CheckObjectState();

            if (this.State != SqlSmoState.Existing)
            {
                CreateImplFinish(createOrAlterQuery, sp);
            }
            else
            {
                AlterImplFinish(createOrAlterQuery, sp);
                this.GenerateAlterEvent();
            }
        }

        /// <summary>
        /// this function is meant to be overriden by derived classes,
        /// if they have to do supplimentary actions after object creation
        /// </summary>
        protected virtual void PostCreate()
        {
        }

        /// <summary>
        /// this function is meant to be overriden by derived classes,
        /// if they have to do supplimentary actions after altering the object
        /// </summary>
        protected virtual void PostAlter()
        {
        }

        /// <summary>
        /// this function is meant to be overriden by derived classes,
        /// if they have to do supplimentary actions after dropping the object
        /// </summary>
        protected virtual void PostDrop()
        {
        }


        /// <summary>
        /// this function is meant to be overriden by derived classes,
        /// if they have to do supplimentary actions after propagating the
        /// object state to subobjects
        /// </summary>
        internal virtual void PostPropagate()
        {
        }

        internal virtual void ScriptDdl(StringCollection query, ScriptingPreferences sp)
        {
            throw new InvalidOperationException();
        }

        internal void ScriptDdlInternal(StringCollection query, ScriptingPreferences sp)
        {
            ScriptDdl(query, sp);
        }

        internal virtual void ScriptAssociations(StringCollection query, ScriptingPreferences sp)
        {
            throw new InvalidOperationException();
        }

        internal void ScriptAssociationsInternal(StringCollection query, ScriptingPreferences sp)
        {
            ScriptAssociations(query, sp);
        }

        internal virtual void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            throw new InvalidOperationException();
        }

        internal virtual void ScriptCreateInternal(StringCollection query, ScriptingPreferences sp)
        {
            ScriptCreateInternal(query, sp, false);
        }

        internal virtual void ScriptCreateInternal(StringCollection query, ScriptingPreferences sp, bool skipPropagateScript)
        {
            ScriptCreate(query, sp);

            // script permissions if needed
            if (sp.IncludeScripts.Permissions)
            {
                AddScriptPermission(query, sp);
            }

            if (!skipPropagateScript)
            {
                //propagate create to children collections
                PropagateScript(query, sp, PropagateAction.Create);
            }
        }


        /// <summary>
        /// Scripts permissions for this object. The default implementation scripts only the
        /// object-level permissions if the object implements IObjectPermission
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal virtual void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // by default exit if the object does not support object permissions
            if (!(this is IObjectPermission))
            {
                return;
            }

            AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Object, sp);
        }



        /// <summary>
        /// Returns a list of permissions that exist for the current object. If the query has been done
        /// before the permissions are returned from the cached UserPermissionsCollection
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        PermissionInfo[] GetPermissionsFromCache(PermissionWorker.PermissionEnumKind kind)
        {
            // initialize the permission collection. We will rely on the standard SMO
            // mechanism of initializing collections here, that is we will just get the
            // collection if it is initialized, otherwise we will fault later on the
            // foreach and retrieve the permissions
            UserPermissionCollection perms = GetUserPermissions();

            // figure out what kind of permissions we will create
            PermissionInfo[] retArr = PermissionWorker.GetPermissionInfoArray(kind, perms.Count);

            PermissionInfo.ObjIdent objIdent = null;
            string columnName = null;
            int arrIdx = 0;
            // iterate through the permissions in the collection and build the
            // structure that can be used by PermissionWorker to generate scripts.
            foreach (UserPermission perm in perms)
            {
                if (null == objIdent)
                {
                    Diagnostics.TraceHelper.Assert(null == columnName, "null == columnName");
                    objIdent = new PermissionInfo.ObjIdent(perm.ObjectClass);
                    if (kind == PermissionWorker.PermissionEnumKind.Column)
                    {
                        // in the case of columns we need to set the info
                        // of the parent
                        Diagnostics.TraceHelper.Assert(this is Column, "this is Column");
                        objIdent.SetData(((Column)this).Parent);
                        columnName = ((Column)this).Name;
                    }
                    else
                    {
                        objIdent.SetData(this);
                        columnName = null;
                    }
                }

                // create the PermissionInfo structure
                PermissionInfo p = PermissionWorker.GetPermissionInfo(kind);
                p.SetPermissionInfoData(
                    perm.Grantee,
                    perm.GranteeType,
                    perm.Grantor,
                    perm.GrantorType,
                    perm.PermissionState,
                    PermissionWorker.GetPermissionSetBase(kind, (int)perm.Code),
                    columnName,
                    objIdent);
                retArr[arrIdx++] = p;
            }

            return retArr;

        }

        /// <summary>
        /// The collection of UserPermission objects
        /// </summary>
        internal virtual UserPermissionCollection Permissions
        {
            get
            {
                // always return null unless overridden by instance classes
                // that support permissions. The overrides are autogenerated
                // for objects that implement IObjectPermissions
                return null;
            }
        }

        private UserPermissionCollection userPermissions = null;
        /// <summary>
        /// Returns the object's permissions. Will be called by overrides of
        /// Permissions property.
        /// </summary>
        /// <returns></returns>
        internal UserPermissionCollection GetUserPermissions()
        {
            CheckObjectState();
            if (null == userPermissions)
            {
                userPermissions = new UserPermissionCollection(this);
            }
            return userPermissions;
        }

        /// <summary>
        /// Clear the user permissions.
        /// </summary>
        internal void ClearUserPemissions()
        {
            userPermissions = null;
        }

        /// <summary>
        /// Scripts the permission DDL statements for this object.
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="kind"></param>
        /// <param name="ver"></param>
        /// <param name="sp"></param>
        internal void AddScriptPermissions(StringCollection sc,
                                    PermissionWorker.PermissionEnumKind kind,
                                    ScriptingPreferences sp)
        {
            PermissionInfo[] plist = null;
            try
            {
                plist = GetPermissionsFromCache(kind);
            }
            catch (InvalidVersionEnumeratorException)
            {
                return;
            }
            catch (EnumeratorException e) when (e.InnerException is InvalidVersionEnumeratorException)
            {
                //permissions have been requested but the object does not support permissions on this server version
                //then we ignore the error, there is nothing to script
                return;
            }

            //generate script for each of the objects permissions
            //skip invalid permissions for the script target version
            foreach (PermissionInfo pi in plist)
            {
                //7.0, 8.0 only support permissions on database, object and column
                //also verify that the permission code exists on the repective server version
                if (sp.TargetServerVersion < SqlServerVersion.Version90 &&
                    ((pi.ObjectClass != ObjectClass.ObjectOrColumn && pi.ObjectClass != ObjectClass.Database) ||
                    !pi.PermissionTypeInternal.IsValidPermissionForVersion(sp.TargetServerVersion)))
                {
                    continue;
                }

                // Cannot grant, deny, or revoke permissions to dbo, information_schema, sys
                if (pi.Grantee == "dbo" || pi.Grantee == "information_schema" || pi.Grantee == "sys")
                {
                    continue;
                }

                // we are done with all the filtering, generate the t-sql
                // for this permission. Note that the call passes in the target object, which might
                // not be the 'this' pointer.
                sc.Add(this.ScriptPermissionInfo(pi, sp));
            }
        }

        /// <summary>
        /// Returns permission script corresponding to the permission info passed as parameter.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pi"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        internal virtual string ScriptPermissionInfo(PermissionInfo pi, ScriptingPreferences sp)
        {
            return (PermissionWorker.ScriptPermissionInfo(GetPermTargetObject(), pi, sp));
        }

        /// <summary>
        /// Overrides will return a parent object instead of the this pointer.
        /// </summary>
        /// <returns></returns>
        internal virtual SqlSmoObject GetPermTargetObject()
        {
            return this;
        }

        /// <summary>
        /// For drop calls on the user database in Azure, the connection will be closed as the database is dropped
        /// The ExecutionManager will normally attempt to retry such calls by reopening the connection
        /// We want to avoid this retry when handleSevereError is true.
        /// </summary>
        /// <param name="isDropIfExists"></param>
        /// <param name="handleSevereError">Indicates whether we should ignore the internal service error raised by management service (42019)</param>
        internal protected void DropImpl(bool isDropIfExists, bool handleSevereError)
        {
            if (isDropIfExists)
            {
                if (this.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
                {
                    ThrowIfBelowVersion130();
                }

                bool fIsInvalidForDrop = (this.State == SqlSmoState.Dropped) ||
                                         (this.State == SqlSmoState.Creating && !this.ExecutionManager.Recording) ||
                                         (this.State == SqlSmoState.Pending && !this.IsDesignMode);

                if (fIsInvalidForDrop)
                {
                    return;
                }
            }

            try
            {
                Urn urn = null;
                if (!handleSevereError)
                {
                    // avoiding the extra layer of try/catch/throw if not needed
                    DropImplWorker(ref urn, isDropIfExists);
                }
                else
                {
                    try
                    {
                        DropImplWorker(ref urn, isDropIfExists);
                    }
                    catch (Exception ex)
                    {
                        SqlException sqlException = ex as SqlException;
                        while (ex.InnerException != null && sqlException == null)
                        {
                            ex = ex.InnerException;
                            sqlException = ex as SqlException;
                        }
                        // 42019 is the management service failed operation error code
                        if (sqlException == null || sqlException.Number != 42019)
                        {
                            throw;
                        }
                    }
                }
                if (!this.ExecutionManager.Recording)
                {
                    // generate internal events
                    if (!SmoApplication.eventsSingleton.IsNullObjectDropped())
                    {
                        SmoApplication.eventsSingleton.CallObjectDropped(GetServerObject(),
                            new ObjectDroppedEventArgs(urn));
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.Drop, this, e);
            }
        }

        ///<summary>
        /// drops the object
        ///</summary>
        /// <param name="isDropIfExists">If true, function will not do anything if object is not in valid state
        /// for dropping.</param>
        protected void DropImpl(bool isDropIfExists = false)
        {
            DropImpl(isDropIfExists, handleSevereError: false);
        }

        ///<summary>
        /// drops the object
        ///</summary>
        /// <param name="urn"></param>
        /// <param name="isDropIfExists">If true, drop will be called with existence check.</param>

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#")]
        protected void DropImplWorker(ref Urn urn, bool isDropIfExists = false)
        {
            if (!this.ExecutionManager.Recording)
            {
                CheckObjectState(true);
            }

            urn = this.Urn;

            StringCollection dropQuery = new StringCollection();


            // setting default script options
            ScriptingPreferences sp = new ScriptingPreferences();

            sp.IncludeScripts.Header = true;

            sp.Behavior = ScriptBehavior.Drop;

            // using this option will avoid any script name/owner substitutions
            sp.ForDirectExecution = true;

            sp.ScriptForCreateDrop = true;

            // pass target version
            sp.SetTargetServerInfo(this);

            // add IF EXISTS clause when executing drop with fIsDropIfExists set
            sp.IncludeScripts.ExistenceCheck = isDropIfExists;

            // call into this virtual function to get the script we have to execute
            ScriptDrop(dropQuery, sp);

            // Script is not executed in Design Mode
            if (!this.IsDesignMode)
            {
                // execute generated script
                if (dropQuery.Count > 0)
                {
                    ExecuteNonQuery(dropQuery, executeForAlter: false);
                }
            }

            // remove the object from the parent collection
            // TODO: this actually requires more actions, since we need to know
            // if there's somebody else holding refs to child objects
            if (null != parentColl)
            {
                parentColl.RemoveObject(this.key);
            }

            // update object state to only if we are in execution mode
            if (!this.ExecutionManager.Recording)
            {
                // mark the object as being dropped
                this.MarkDropped();
            }

            //propagate to the children collection state update and cleanup after drop
            PropagateStateAndCleanUp(dropQuery, sp, PropagateAction.Drop);

            PostDrop();
        }

        internal virtual void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            throw new InvalidOperationException();
        }


        internal void ScriptDropInternal(StringCollection dropQuery, ScriptingPreferences sp)
        {
            ScriptDrop(dropQuery, sp);

            //don't propagate script drop to children collections it is done automatically
        }

        internal void ScriptIncludeHeaders(StringBuilder sb, ScriptingPreferences sp, string objectType)
        {
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    objectType, MakeSqlBraketNoEscape(InternalName), DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }
        }

        /// <summary>
        /// Returns the comparer used by all child object collections to perform comparison of object names.
        /// The comparer of a Server is based on the collation of master
        /// The comparer of a Database and its children is based on the database collation
        /// </summary>
        /// <returns></returns>
        public IComparer<string> GetStringComparer()
        {
            return StringComparer;
        }

        // this is the comparer that will be used by all child object collections
        // to perform comparison between object names
        internal StringComparer m_comparer;
        internal StringComparer StringComparer
        {
            get
            {
                InitializeStringComparer();
                Diagnostics.TraceHelper.Assert(m_comparer != null);
                return m_comparer;
            }
        }

        internal void InitializeStringComparer()
        {
            if (null == m_comparer)
            {
                // TODO: replace "this is" with a virtual function call
                if (this is Server || this is Settings)
                {
                    // get the collation of the master database
                    m_comparer = GetDbComparer(true);
                }
                else if (this is Database)
                {
                    m_comparer = GetDbComparer(false);
                }
                else if (null == ParentColl || null == ParentColl.ParentInstance)
                {
                    m_comparer = GetDbComparer(true);
                }
                else
                {
                    // in all other cases, get the parent's comparer
                    m_comparer = ParentColl.ParentInstance.StringComparer;
                }
            }
        }

        private ObjectComparerBase keyComparer = null;
        internal ObjectComparerBase KeyComparer
        {
            get
            {
                if (null == keyComparer)
                {
                    StringComparer parentComparer = null;
                    if (null != ParentColl && null != ParentColl.ParentInstance)
                    {
                        // if we can reach it grab the parent's comparer
                        parentComparer = ParentColl.ParentInstance.StringComparer;
                    }
                    else if (this is Database)
                    {
                        // for Database, we know for sure we need Server's comparer
                        parentComparer = GetDbComparer(true);
                    }
                    else
                    {
                        // in all other situations the comparers are identical
                        parentComparer = this.StringComparer;
                    }

                    // we build the comparer using the parent's comparer because
                    // the parent's comparer should be used to compare child objects
                    keyComparer = key.GetComparer(parentComparer);
                }
                return keyComparer;
            }
        }

        private bool TryGetProperty<T>(string propertyName, ref T value)
        {
            Property property = null;
            if (IsSupportedProperty(propertyName) && Properties.Contains(propertyName))
            {
                property = Properties.Get(propertyName);
            }

            if (null != property && null != property.Value && property.Retrieved)
            {
                value = (T)property.Value;
                return true;
            }

            return false;
        }

        // The collation value used for in-database comparisons depends on a combination of 3 properties:
        // ContainmentType, CatalogCollation, and Collation. This method fetches the subset of those 3 properties
        // that is valid for the current database in one fetch at most, avoiding a full Database object fetch
        // Only valid if this object is of type Database
        private string GetCollationRelatedProperties(string dbName, out ContainmentType containmentType, out CatalogCollationType catalogCollation)
        {
            var propertiesToFetch = new List<string>();
            var localContainmentType = ContainmentType.None;
            CatalogCollationType localCatalogCollationType = CatalogCollationType.DatabaseDefault;
            string collation = null;

            if (!TryGetProperty("ContainmentType", ref localContainmentType))
            {
                if (dbName != "master" && dbName != "msdb" && IsSupportedProperty("ContainmentType"))
                {
                    propertiesToFetch.Add("ContainmentType");
                }
            }

            if (!TryGetProperty("CatalogCollation", ref localCatalogCollationType) && IsSupportedProperty("CatalogCollation"))
            {
                propertiesToFetch.Add("CatalogCollation");
            }

            if (!TryGetProperty("Collation", ref collation))
            {
                // Azure SQL database masters are fixed around the world to the same collation
                if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase &&
                    string.Compare(dbName, "master", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    collation = "SQL_Latin1_General_CP1_CI_AS";
                }
                else
                {
                    propertiesToFetch.Add("Collation");
                }
            }

            if (propertiesToFetch.Any())
            {
                // Use an SFC request to fetch needed properties
                var request = new Request(string.Format(SmoApplication.DefaultCulture,
                        "Server/Database[@Name='{0}']",
                        Urn.EscapeString(dbName)),
                    propertiesToFetch.ToArray());

                // handles the cloud DB case too (using Server's Execution Manager)
                var dataTable = this.GetServerObject().ExecutionManager.GetEnumeratorData(request);

                if (dataTable.Rows.Count > 0)
                {
                    if (dataTable.Columns.Contains("Collation") && dataTable.Rows[0]["Collation"] != DBNull.Value)
                    {
                        collation = (string) dataTable.Rows[0]["Collation"];
                    }

                    if (dataTable.Columns.Contains("CatalogCollation") &&
                        dataTable.Rows[0]["CatalogCollation"] != DBNull.Value)
                    {
                        localCatalogCollationType = (CatalogCollationType) dataTable.Rows[0]["CatalogCollation"];
                    }

                    if (dataTable.Columns.Contains("ContainmentType") &&
                        dataTable.Rows[0]["ContainmentType"] != DBNull.Value)
                    {
                        localContainmentType = (ContainmentType) dataTable.Rows[0]["ContainmentType"];
                    }
                }
            }

            catalogCollation = localCatalogCollationType;
            containmentType = localContainmentType;
            return collation ?? String.Empty;
        }

        internal protected virtual string CollationDatabaseInServer => "master";

        internal StringComparer GetDbComparer(bool inServer)
        {
            if (ServerVersion.Major <= 7 || IsDesignMode)
            {
                return new StringComparer(string.Empty, 1033);
            }

            var dbName = inServer ? CollationDatabaseInServer : ((SimpleObjectKey) this.key).Name;
            ContainmentType containmentType;
            CatalogCollationType catalogCollationType;
            var dbcoll = GetCollationRelatedProperties(dbName, out containmentType, out catalogCollationType);

            if (containmentType != ContainmentType.None) //for a contained database catalog_collation is always fixed.
            {
                dbcoll = "Latin1_General_100_CI_AS_KS_WS";
            }
            else if (Enum.IsDefined(typeof(CatalogCollationType), catalogCollationType) &&
                     catalogCollationType != CatalogCollationType.DatabaseDefault)
            {
                TypeConverter catalogCollationTypeConverter =
                    SmoManagementUtil.GetTypeConverter(typeof(CatalogCollationType));

                // If the CatalogCollationType is set to something other than DatabaseDefault, then catalog_collation is fixed.
                //
                dbcoll = catalogCollationTypeConverter.ConvertToInvariantString(catalogCollationType);
            }

            //Not a contained database, and no fixed catalog collation.
            if (dbcoll == string.Empty)
            {
                //Couldn't get the collation from the database
                //so fall back to the server's
                dbcoll = this.GetServerObject().Collation;
                Trace(string.Format(
                    "Got null/empty DB Collation for DB {0}, falling back to using server collation {1}", dbName,
                    dbcoll));
            }

            return GetComparerFromCollation(dbcoll);
        }

        //given a collation name, reads the collation atributes from server
        //and constructs a StringComparer
        internal StringComparer GetComparerFromCollation(string collationName)
        {
            StringComparer comparer = null;

            try
            {
                comparer = new StringComparer(collationName, this.GetServerObject().GetLCIDCollation(collationName));
            }
            catch (DisconnectedConnectionException)
            {
                // Fall back to 1033 LCID in Design Mode
                comparer = new StringComparer(collationName, 1033);
            }

            return comparer;
        }

        /// <summary>
        /// Returns the CultureInfo object to be used when formatting culture
        /// sensitive strings related to this object (e.g. DateTime etc.)
        /// </summary>
        /// <returns></returns>
        internal CultureInfo GetDbCulture()
        {
            //Used for Scripting header only and returns OS culture
            return System.Threading.Thread.CurrentThread.CurrentCulture;
        }

        private bool m_bIgnoreForScripting;
        internal bool IgnoreForScripting
        {
            get
            {
                CheckObjectState();
                return m_bIgnoreForScripting;
            }
            set
            {
                CheckObjectState();
                m_bIgnoreForScripting = value;
            }
        }

        /// <summary>
        /// Returns the ServerVersion of the Server that contains the object.
        /// If the object is not associated with a connected Server, the highest known
        /// server version is returned.
        /// </summary>
        public ServerVersion ServerVersion
        {
            get
            {
                if (this.TryGetServerObject() != null)
                {
                    // handles the cloud DB case too (using Server's Execution Manager)

                    return this.GetServerObject().ExecutionManager.GetServerVersion();
                }
                else
                {
                    // when we don't have a server connected,
                    // we assume the highest known version
                    // This is used in schenario, when we need
                    // to get a metadata without connection the server

                    return VersionUtils.HighestKnownServerVersion;
                }
            }
        }

        /// <summary>
        /// Returns the DatabaseEngineType of the SMO object
        /// </summary>
        public virtual DatabaseEngineType DatabaseEngineType
        {
            get
            {
                Server server = this.TryGetServerObject();
                if (server != null)
                {
                    //The EngineType is a server-level property so
                    //we can query the server directly (which lets us
                    //get this property when this object is being created)
                    return server.ExecutionManager.GetDatabaseEngineType();

                }
                else
                {
                    //Not having a server object is normal if we're
                    //designing an object, so just return unknown
                    //until we get a server connection to use
                    return DatabaseEngineType.Unknown;
                }
            }
        }

        /// <summary>
        /// Returns the DatabaseEngineEdition of the SMO object
        /// </summary>
        public virtual DatabaseEngineEdition DatabaseEngineEdition
        {
            get
            {
                //Bubble up to the parent until we get to the root - at
                //which point we'll fetch the value from the ExecutionManager.
                //This allows anything along the hierarchy to insert its own
                //implementation (the key one being DB which is special on
                //Azure since we need to use the DB-specific connection)
                SqlSmoObject parent = this.GetParentObject(throwIfParentIsCreating:false, throwIfParentNotExist:false);
                if (parent != null)
                {
                    return parent.DatabaseEngineEdition;
                }
                else
                {
                    return this.ExecutionManager.GetDatabaseEngineEdition();
                }
            }
        }

        private IRenewableToken accessToken = null;

        /// <summary>
        /// Set the accessToken for connection.
        /// </summary>
        public void SetAccessToken(IRenewableToken token)
        {
            accessToken = token;
            if (this.ExecutionManager != null)
            {
                this.ExecutionManager.ConnectionContext.AccessToken = token;
            }
        }

        /// <summary>
        /// Will return if Cloud is not supported
        /// </summary>
        internal virtual bool IsCloudSupported
        {
            get
            {
                return IsSupportedOnSqlAzure(this.GetType());
            }
        }

        /// <summary>
        /// Returns whether a smo object type is supported on Microsoft Azure SQL Database.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <remarks>This is for internal SQL usage and may not work correctly if used otherwise</remarks>
        public static bool IsSupportedOnSqlAzure(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            foreach (object o in type.GetCustomAttributes(typeof(SfcElementAttribute), true))
            {
                SfcElementAttribute attribute = o as SfcElementAttribute;
                if (attribute != null)
                {
                    return attribute.SqlAzureDatabase;
                }
            }
            return false;
        }

        internal ServerInformation ServerInfo
        {
            get
            {
                var server = TryGetServerObject();
                // The current assumption is that HostPlatform is only relevant for on-premise servers and would be irrelevant if Azure SQL Database replats
                // on Linux for some instances. If that changes, we may need to add such information to the executionmanager so we can have database-specific values
                return new ServerInformation(this.ExecutionManager.GetServerVersion(),
                    this.ExecutionManager.GetProductVersion(),
                    this.ExecutionManager.GetDatabaseEngineType(),
                    this.ExecutionManager.GetDatabaseEngineEdition(),
                    hostPlatform: server == null ? HostPlatformNames.Windows : server.HostPlatform,
                    connectionProtocol: this.ExecutionManager.GetConnectionProtocol());
            }
        }

        internal static String EscapeString(String s, char cEsc)
        {
            if (null == s)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                sb.Append(c);
                if (cEsc == c)
                {
                    sb.Append(c);
            }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Escapes all single-quotes in a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static String SqlString(String s)
        {
            return EscapeString(s, '\'');
        }

        /// <summary>
        /// Returns a fully-escaped SQL string wrapped within the SQL string single-quotes
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static String MakeSqlString(String s)
        {
            return String.Format(SmoApplication.DefaultCulture, "N'{0}'", EscapeString(s, '\''));
        }

        /// <summary>
        /// Return name enclosing with delimiters.
        /// </summary>
        /// <param name="name">name to enclose</param>
        /// <param name="cStart">starting delimiter character for enclosing</param>
        /// <param name="cEnd">ending delimiter character for enclosing</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static String QuoteString(String name, char cStart = '\'', char cEnd = '\'')
        {
            return string.Format(SmoApplication.DefaultCulture, "{1}{0}{2}", EscapeString(name, cEnd), cStart, cEnd);
        }

        /// <summary>
        /// Handles additional processing required for scripting insert statements for data
        /// </summary>
        /// <param name="s"> the column value to be processed</param>
        /// <returns></returns>
        internal static String MakeSqlStringForInsert(String s)
        {
            Diagnostics.TraceHelper.Assert(s != null);
            return MakeSqlString(s).Replace("\\" + System.Environment.NewLine,
                "\\\' + N'" + System.Environment.NewLine);
        }

        internal static String SqlBraket(String s)
        {
            return EscapeString(s, ']');
        }

        internal static String MakeSqlBraket(String s)
        {
            return string.Format(SmoApplication.DefaultCulture, "[{0}]", EscapeString(s, ']'));
        }

        internal static String SqlStringBraket(String s)
        {
            return SqlBraket(SqlString(s));
        }

        internal static String MakeSqlBraketNoEscape(String s)
        {
            return string.Format(SmoApplication.DefaultCulture, "[{0}]", s);
        }

        protected virtual bool IsObjectDirty()
        {
            return (this.Properties.Dirty || this.isTouched);
        }

        internal bool InternalIsObjectDirty
        {
            get { return IsObjectDirty(); }
        }

        protected virtual void CleanObject()
        {
            if (this.IsDesignMode)
            {
                this.Properties.SetAllDirtyAsRetrieved(true);
            }

            this.Properties.SetAllDirty(false);
            this.isTouched = false;
        }

        private bool isTouched = false;
        /// <summary>
        /// Whether the object has been touched for unconditional scripting of Alter
        /// </summary>
        protected bool IsTouched
        {
            get
            {
                return this.isTouched;
            }
        }

        /// <summary>
        /// Mark the object "touched" for unconditional scripting of Alter.
        /// </summary>
        /// <remarks>
        /// Properties are not marked dirty, so properties beyond the text definition
        /// will not be scripted for Alter.
        /// </remarks>
        public void Touch()
        {
            this.isTouched = true;
            this.TouchImpl();
        }

        /// <summary>
        /// Virtual method to allow derived classes to do additional
        /// work when touched.
        /// </summary>
        protected virtual void TouchImpl()
        {
            // do nothing
        }

        static protected bool IsCollectionDirty(ICollection col)
        {
            AbstractCollectionBase baseCol = col as AbstractCollectionBase;
            if (null != baseCol && true == baseCol.IsDirty)
            {
                return true;
            }
            foreach (SqlSmoObject obj in col)
            {
                if (obj.State == SqlSmoState.Creating)
                {
                    return true;
                }
                else if (obj.State == SqlSmoState.Existing)
                {
                    if (obj.IsObjectDirty())
                    {
                        return true;
                    }
                }
                else if (obj.State == SqlSmoState.ToBeDropped)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// holds information that specifies to what child objects is going
        /// to be propagated  a create, alter or drop action
        /// </summary>
        internal class PropagateInfo
        {
            public ICollection col;
            public SqlSmoObject obj;
            public bool bWithScript;
            //only has effect when bWithScript is false
            public bool bPropagateScriptToChildLevel;

            private void Init(ICollection col, bool bWithScript, string urnTypeKey, bool bPropagateScriptToChildLevel)
            {
                this.col = col;
                this.bWithScript = bWithScript;
                this.urnTypeKey = urnTypeKey;
                this.bPropagateScriptToChildLevel = bPropagateScriptToChildLevel;
            }

            private void Init(SqlSmoObject obj, bool bWithScript, string urnTypeKey, bool bPropagateScriptToChildLevel)
            {
                this.obj = obj;
                this.bWithScript = bWithScript;
                this.urnTypeKey = urnTypeKey;
                this.bPropagateScriptToChildLevel = bPropagateScriptToChildLevel;
            }

            internal PropagateInfo(ICollection col)
            {
                Init(col, true, null, true);
            }
            internal PropagateInfo(ICollection col, bool bWithScript)
            {
                Init(col, bWithScript, null, true);
            }

            internal PropagateInfo(ICollection col, bool bWithScript, bool bPropagateScriptToChildLevel)
            {
                Init(col, bWithScript, null, bPropagateScriptToChildLevel);
            }

            internal PropagateInfo(ICollection col, bool bWithScript, string urnTypeKey)
            {
                Init(col, bWithScript, urnTypeKey, true);
            }

            internal PropagateInfo(SqlSmoObject obj)
            {
                Init(obj, true, null, true);
            }
            internal PropagateInfo(SqlSmoObject obj, bool bWithScript)
            {
                Init(obj, bWithScript, null, true);
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="obj">The child object to propagate to</param>
            /// <param name="bWithScript">true if the child object should be scripted
            ///                         false if only it's state should be updated</param>
            /// <param name="urnTypeKey">Propagation ocurs only if the specified type is not in filter</param>
            internal PropagateInfo(SqlSmoObject obj, bool bWithScript, string urnTypeKey)
            {
                Init(obj, bWithScript, urnTypeKey, true);
            }

            /// <summary>
            /// if any of these is not specified, we do not propagate the action
            /// </summary>
            private string urnTypeKey;
            internal string UrnTypeKey
            {
                get { return this.urnTypeKey; }
                set { this.urnTypeKey = value; }
            }
        }

        /// <summary>
        /// represents the action on the parent object that need
        /// to be propagated to the child collections
        /// </summary>
        internal enum PropagateAction { Create, Alter, Drop, CreateOrAlter };

        /// <summary>
        /// to be overridden by objects that have to propagate actions to child
        /// collections
        /// </summary>
        /// <param name="action"></param>
        /// <returns>array of actions that are propagated</returns>
        internal virtual PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return null;
        }

        /// <summary>
        /// to be overridden by objects that have to propagate actions to child
        /// collections
        /// </summary>
        /// <param name="action"></param>
        /// <returns>array of actions that are propagated</returns>
        internal virtual PropagateInfo[] GetPropagateInfoForDiscovery(PropagateAction action)
        {
            return this.GetPropagateInfo(action);
        }

        /// <summary>
        /// Control loop for propagation of actions, works recursively
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        /// <param name="action"></param>
        internal void PropagateScript(StringCollection query, ScriptingPreferences sp, PropagateAction action)
        {
            if (PropagateAction.Drop == action)
            {
                return;//no script -> it is dropped automatically with father
            }
            PropagateInfo[] listPropagateInfo = GetPropagateInfo(action);
            if (null == listPropagateInfo)
            {
                return;
            }

            // propagate the action to all the objects that need to be updated
            // in order to complete this action
            foreach (PropagateInfo pi in listPropagateInfo)
            {
                ICollection smoObjCol = null;
                if (null != pi.col)
                {
                    smoObjCol = pi.col;
                }
                else if (null != pi.obj)
                {
                    smoObjCol = new SqlSmoObject[] { pi.obj };
                }
                else
                {
                    continue;
                }

                //saving the expensive initialization process of
                //collection class (esp PhyisicalPartitionCollection) inside next foreach loop
                if (pi.bWithScript || pi.bPropagateScriptToChildLevel)
                {


                    foreach (SqlSmoObject obj in smoObjCol)
                    {
                        if (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase && !(obj.IsCloudSupported))
                        {
                            continue;
                        }
                        if (true == pi.bWithScript)
                        {
                            if (PropagateAction.Create == action && !sp.ScriptForCreateDrop && obj is ICreatable) //scripting
                            {
                                obj.ScriptCreateInternal(query, sp);
                            }
                            else if (PropagateAction.Create == action || PropagateAction.Alter == action ||
                                     PropagateAction.CreateOrAlter == action)
                            {
                                // if the objects needs to be altered, its dependents
                                // have to be altered, or created
                                if (obj.State == SqlSmoState.Existing)
                                {
                                    bool b = sp.ScriptForAlter;
                                    sp.ScriptForAlter = true;
                                    obj.ScriptAlterInternal(query, sp);
                                    sp.ScriptForAlter = b;
                                }
                                else if (obj.State == SqlSmoState.Creating)
                                {
                                    bool b1 = sp.ScriptForAlter;
                                    bool b2 = sp.ScriptForCreateDrop;
                                    sp.ScriptForAlter = false;
                                    sp.ScriptForCreateDrop = true;
                                    obj.ScriptCreateInternal(query, sp);
                                    sp.ScriptForAlter = b1;
                                    sp.ScriptForCreateDrop = b2;
                                }
                                else if (obj.State == SqlSmoState.ToBeDropped)
                                {
                                    obj.ScriptDropInternal(query, sp);
                            }
                        }
                        }
                        else if (pi.bPropagateScriptToChildLevel)
                        {
                            obj.PropagateScript(query, sp, action);
                        }
                    }
                }
            }
        }

        private void PropagateStateAndCleanUp(StringCollection query, ScriptingPreferences sp, PropagateAction action)
        {
            if (false == sp.ScriptForCreateDrop) //should happen only for alter
            {
                if ((PropagateAction.Create == action) || (PropagateAction.Drop == action))
                {
                    return;
                }
            }
            else if (false == sp.ScriptForAlter && PropagateAction.Alter == action)
            {
                return; //should nver happen
            }
            if (PropagateAction.Drop == action)
            {
                return; //done by MarkDropped
            }
            if (true == this.ExecutionManager.Recording)
            {
                return; //should never happen
            }
            PropagateInfo[] listPropagateInfo = GetPropagateInfo(action);
            if (null == listPropagateInfo)
            {
                return;
            }
            foreach (PropagateInfo pi in listPropagateInfo)
            {
                ArrayList listObjectsToBeDropped = new ArrayList();

                // we can either propagate to a collection or to a single object
                ICollection smoObjCol;
                if (null != pi.col)
                {
                    AbstractCollectionBase baseCol = pi.col as AbstractCollectionBase;
                    // if there are no objects in the collection we don't want the
                    // propagation to trigger an initialization of the collection
                    if (null != baseCol && baseCol.NoFaultCount > 0)
                    {
                        smoObjCol = pi.col;
                        baseCol.IsDirty = false;
                    }
                    else if (pi.col is List<SqlSmoObject>)
                    {
                        smoObjCol = pi.col;
                    }
                    else
                    {
                        smoObjCol = new SqlSmoObject[] { };
                    }
                }
                else if (null != pi.obj)
                {
                    smoObjCol = new SqlSmoObject[] { pi.obj };
                }
                else
                {
                    continue;
                }

                // Whenever an initialized child collection is called through enumerator at create time, it
                // regenerates the new collection and merges the old collection and new collection and returns the old objects by setting initialized to true
                // Since we are returning the old collection itself, we can directly set the initialized to true at create time so that enumerator regenerate the new collection again.
                if (PropagateAction.Create == action)
                {
                    AbstractCollectionBase baseCollection = smoObjCol as AbstractCollectionBase;
                    if (baseCollection != null)
                    {
                        baseCollection.initialized = true;
                    }
                }
                foreach (SqlSmoObject obj in smoObjCol)
                {
                    if (PropagateAction.Create == action)
                    {
                        if (obj.State != SqlSmoState.Existing)
                        {
                            obj.PostCreate();
                        }

                        // do the same for the object's children
                        obj.PropagateStateAndCleanUp(query, sp, action);

                        // update object state
                        obj.SetState(SqlSmoState.Existing);

                        // reset all properties
                        obj.CleanObject();
                    }
                    else if (PropagateAction.Alter == action)
                    {
                        if (obj.State == SqlSmoState.ToBeDropped)
                        {
                            listObjectsToBeDropped.Add(obj);
                        }
                        else
                        {
                            if (obj.State == SqlSmoState.Creating)
                            {
                                obj.PostCreate();

                                obj.SetState(SqlSmoState.Existing);
                            }

                            // reset all properties
                            obj.CleanObject();

                            // do the same for the object's children
                            obj.PropagateStateAndCleanUp(query, sp, action);
                        }
                    }
                }
                foreach (SqlSmoObject obj in listObjectsToBeDropped)
                {
                    obj.SetState(SqlSmoState.Dropped);
                    if (null != obj.parentColl)
                    {
                        obj.parentColl.RemoveObject(obj.key);
                    }
                }
            }
        }

        // this function is called after a Create() or Alter() succeeded for an object
        // that holds a collection of children objects that are modified during Create()
        // or Alter() call. Its purpose is to update the state of the objects in the collection
        static protected internal void UpdateCollectionState2(ICollection col)
        {
            foreach (SqlSmoObject obj in col)
            {
                if (obj.State == SqlSmoState.Creating)
                {
                    obj.SetState(SqlSmoState.Existing);
                }
                else if (obj.State == SqlSmoState.ToBeDropped)
                {
                    obj.SetState(SqlSmoState.Dropped);
                    obj.ParentColl.RemoveObject(obj.key);
                }
            }
        }

        /// <summary>
        /// Classes that need to initialize properties needed during the prefetch
        /// operation should override this function. Because we are using System.Data.IDataReader
        /// and we don't have MARS this means we will throw an error if a property is
        /// faulted and that generates a query to the server
        /// </summary>
        internal virtual void PreInitChildLevel()
        {
        }

        /// <summary>
        /// Init all the objects in the query so subsequent GetSmoObject calls are fast and client-side.
        /// Also return the list of Urns from the query, in order, so an iterator can be layered over all this.
        /// Code lifted from InitChildLevel.
        /// </summary>
        /// <param name="queryUrn"></param>
        /// <param name="fields"></param>
        /// <param name="orderByFields"></param>
        /// <returns>The list of string Urn paths that matched the query.</returns>
        internal List<string> InitQueryUrns(Urn levelFilter,
            string[] queryFields, OrderBy[] orderByFields, string[] infrastructureFields)
        {
            return InitQueryUrns(levelFilter, queryFields, orderByFields, infrastructureFields, null, null, this.DatabaseEngineEdition);
        }

        /// <summary>
        /// Init all the objects in the query so subsequent GetSmoObject calls are fast and client-side.
        /// Also return the list of Urns from the query, in order, so an iterator can be layered over all this.
        /// Code lifted from InitChildLevel.
        /// We pass the edition as a parameter because prefetch calls this method on a Server object and the edition is tied to the database.
        /// </summary>
        /// <param name="queryUrn"></param>
        /// <param name="fields"></param>
        /// <param name="orderByFields"></param>
        /// <param name="edition">Engine edition of the database being scripted</param>
        /// <returns>The list of string Urn paths that matched the query.</returns>
        internal List<string> InitQueryUrns(Urn levelFilter,
            string[] queryFields, OrderBy[] orderByFields, string[] infrastructureFields, ScriptingPreferences sp, Urn initializeCollectionsFilter, DatabaseEngineEdition edition)
        {
            bool forScripting = (sp != null);

            // make sure all the infrastructure properties are present before
            // doing the prefetch query
            PreInitChildLevel();

            // Things to grab to avoid open DataReader collisions deeper:
            // 1. server.TrueName, can prime this by asking for any Urn
            // 2. server's StringComparer
            // 3. database.CompatiblityLevel, used in CheckDbCompatLevel for queries
            // Since we always start from a server instance, we grab (1) and (2) now, and (3) will have to come from
            // recusring on this function at yet a higher level and hope noone adds a database in between the time the S/D query
            // and the S/D/T one under it.
            Urn dummyUrn = this.Urn;
            InitializeStringComparer();

            // build the XPathExpression for this urn
            XPathExpression parsedUrn = levelFilter.XPathExpression;

            // servers collection does not exist, so we assume here that the Urn
            // contains at least one more level under the server
            int nodeCount = parsedUrn.Length;
            if (nodeCount < 1)
            {
                throw new InternalSmoErrorException(ExceptionTemplates.CallingInitQueryUrnsWithWrongUrn(levelFilter));
            }

            // figure out what is the children's type
            Type childType = GetChildType(parsedUrn[nodeCount - 1].Name,
                (nodeCount > 1) ? parsedUrn[nodeCount - 2].Name : this.GetType().Name);

            // this is the request we're making to the enumerator
            Request levelQuery = new Request(levelFilter, queryFields, orderByFields);

            Type parentType = null;
            int startLeafIdx = 0;

            // if we have more than one level in the filter, we will need to add properties
            // for the parent objects, so that we can be able to identify objetcs in the result rows
            if (nodeCount >= 2)
            {
                Type currType = childType;

                int reverseIdx = nodeCount - 2;
                levelQuery.ParentPropertiesRequests = new PropertiesRequest[nodeCount - 1];

                // go backwards through the Urn and figure out if we need to add
                // parent fields to the enumerator request. For instance, if the Urn is
                // like Server[@Name='name']/Database[@Name='name']/Table/Column
                // we would like to add Name and Schema for the table, and stop at the Database
                // level if we are calling this method from a Database object
                while (reverseIdx >= 0)
                {
                    currType = GetChildType(parsedUrn[reverseIdx].Name,
                        (reverseIdx > 0) ? parsedUrn[reverseIdx - 1].Name : this.GetType().Name);
                    Diagnostics.TraceHelper.Assert(null != currType, "currType == null");

                    if (null == parentType)
                    {
                        parentType = currType;
                    }

                    // now look at the current level to see what properties we'll need
                    // for this parent in the result set. We'll need Name, and if the object
                    // has Schema we'll also need schema
                    PropertiesRequest currProps = new PropertiesRequest();

                    currProps.Fields = GetQueryTypeKeyFields(currType);

                    currProps.OrderByList = GetOrderByList(currType);
                    //  No ordering if a singleton like Server/Setting, since GetOrderByList always defaults to "Name".
                    if (currProps.OrderByList.Length == 1 &&
                        currProps.OrderByList[0].Field == "Name" &&
                        !currType.IsSubclassOf(typeof(NamedSmoObject)))
                    {
                        // Force no ordering from us
                        currProps.OrderByList = null;
                    }

                    levelQuery.ParentPropertiesRequests[nodeCount - 2 - reverseIdx] = currProps;

                    // This is the column of the first leaf-related field (key or data), so we can freely
                    // create the parent Urn from all the column values up to this column.
                    startLeafIdx += (currProps.Fields != null ? currProps.Fields.Length : 0);

                    reverseIdx--;
                }
            }

            // If no ParentType was found just use the current type. This should only be needed for relative Urns
            parentType = parentType ?? this.GetType();

            string[] defaultFields;
            if (forScripting)
            {
                defaultFields = GetServerObject().GetScriptInitFieldsInternal(childType, parentType, sp, edition);
            }
            else
            {
                defaultFields = GetServerObject().GetDefaultInitFieldsInternal(childType, edition);
            }
            // Use the default init fields for the leaf type if none given
            if (levelQuery.Fields == null || levelQuery.Fields.Length == 0)
            {
                levelQuery.Fields = defaultFields;
            }
            // Make sure the key field(s) are included and are first in the list
            // We have to use the check for ID,Name being the key since GetFieldNames would only list the Name.
            // And we must have ID back in the query or risk yet another avenue to overlap the DataReader when it come time to need it.
            StringCollection childKeyFields = new StringCollection();

            // If the following is empty, then this type apparently has no key fields,
            // like FileStreamSettings, or DatabaseOptions.
            // Most likely the type is a singleton under another NamedSmoObject.
            childKeyFields.AddRange(GetQueryTypeKeyFields(childType));

            string[] childFields = new string[childKeyFields.Count +
                ((levelQuery.Fields != null) ? levelQuery.Fields.Length : 0) +
                ((infrastructureFields != null) ? infrastructureFields.Length : 0)];
            if (childKeyFields.Count > 0)
            {
                childKeyFields.CopyTo(childFields, 0);
            }
            int count = childKeyFields.Count;

            // These are the fields the caller requested, to add in after we ensured the key field(s) are in the list first
            if (levelQuery.Fields != null)
            {
                foreach (string field in levelQuery.Fields)
                {
                    // Check each field and skip duplicates since we added the key fields up-front in the list.
                    // This also catches any duplicates in general the Request may have; otherwise the Enumerator will throw.
                    bool bFound = false;
                    foreach (string keyField in childFields)
                    {
                        if (string.CompareOrdinal(field, keyField) == 0)
                        {
                            // The field is already in the list so skip it
                            bFound = true;
                            break;
                        }
                    }

                    // Append the field to our list if it isn't already in it.
                    if (!bFound)
                    {
                        childFields[count] = field;
                        count++;
                    }
                }
            }

            // These are the extra fields we internally add knowing they will be needed. This avoids extra queries.
            if (infrastructureFields != null)
            {
                foreach (string field in infrastructureFields)
                {
                    // Check each field and skip duplicates since we added the key fields up-front in the list.
                    // This also catches any duplicates in general the Request may have; otherwise the Enumerator will throw.
                    bool bFound = false;
                    foreach (string keyField in childFields)
                    {
                        if (string.CompareOrdinal(field, keyField) == 0)
                        {
                            // The field is already in the list so skip it
                            bFound = true;
                            break;
                        }
                    }

                    // Append the field to our list if it isn't already in it.
                    if (!bFound)
                    {
                        childFields[count] = field;
                        count++;
                    }
                }
            }

            // Replace the request fields array with the one with keys in front, extra fields added and no dupes
            if (count == 0)
            {
                levelQuery.Fields = null;
            }
            else
            {
                levelQuery.Fields = new string[count];
                for (int i = 0; i < count; i++)
                {
                    levelQuery.Fields[i] = childFields[i];
                }
            }

            // Default ordering if none given.
            if (levelQuery.OrderByList == null || levelQuery.OrderByList.Length == 0)
            {
                levelQuery.OrderByList = GetOrderByList(childType);

                //  No ordering if a singleton leaf like DatabaseOptions, GetOrderByList always defaults to "Name".
                if (levelQuery.OrderByList.Length == 1 &&
                    levelQuery.OrderByList[0].Field == "Name" &&
                    !childType.IsSubclassOf(typeof(NamedSmoObject)))
                {
                    // Force no ordering from us
                    levelQuery.OrderByList = null;
                }
            }

            // do this special case for initializing Database with default init fields
            var urnList = new List<string>();
            if (IsDatabaseSpecialCase(forScripting, parsedUrn, levelQuery.Fields, defaultFields))
            {
                DoDatabaseSpecialCase(levelQuery, levelFilter, forScripting, urnList, startLeafIdx, defaultFields);
            }
            else
            {
                // execute the query
                //using (System.Data.IDataReader reader = this.ExecutionManager.GetEnumeratorDataReader(levelQuery))
                using (DataTable dt = this.ExecutionManager.GetEnumeratorData(levelQuery))
                {
                    using (DataTableReader reader = dt.CreateDataReader())
                    {
                        // init all child objects from the query results
                        InitObjectsFromEnumResults(levelFilter, reader, forScripting, urnList, startLeafIdx, true);
                    }
                }

                string[] fields;
                if (forScripting
                    && this.GetServerObject().GetScriptInitExpensiveFieldsInternal(childType, parentType, sp, out fields, edition))
                {
                    levelQuery.Fields = fields;
                    // execute the query
                    using (System.Data.IDataReader reader = this.ExecutionManager.GetEnumeratorDataReader(levelQuery))
                    {
                        // init all child objects from the query results
                        InitObjectsFromEnumResults(levelFilter, reader, forScripting, urnList, startLeafIdx, true);
                    }
                }

                // make sure all the child collections have the proper retrieved status
                // EDDUDE: Arbitrary Object Query expressions cannot be imitated here.
                //         What does this do that the InitObjectFromEnumResultsRec code doesn't already do anyway?
                //         If it just to delay setting the initialized bit on collections which are
                //         uninitialized coming in to the query, and which have filters on them, then it would
                //         be better to just keep a queue of all those collections as we descended and touched them
                //         than to try to in-memory traverse all the collections afterwards, pretending to evaluate
                //         the entire query in the client again (which as I said we cannot acurately do).
                // The next line is the client-side XPath emulation to hit all the same collections (supposedly) that we already
                // visited while populating from the query. This worked good enough for InitChildLevel,
                // but cannot work for arbitrary Object Queries. We either have to just keep a queue of all the
                // collections we touched as we processed the query, or continue to process a cascading query
                // by each level (like GetSmoObjectQueryRec does which feeds us individual Urns to process)
                // so each level gets a chance at being the leaf and not having to queue up anything.

                // STANISC: added a workaround that should allow initialize all collections when using complex Urn queries
                // like the ones containing IN operator. This second URN should be a stripped out levelFilter Urn without
                // the actual filter
                if (initializeCollectionsFilter != null)
                {
                    MarkChildCollRetrieved(initializeCollectionsFilter, 1);
                }
            }

            return urnList;
        }

        /// <summary>
        /// Provides an enumerable of properties that are explicitly disabled for specific server types or editions.
        /// This is not a list of all properties that don't work for the specified target.
        /// </summary>
        /// <param name="sp">Scripting preferences to get server info from. If null, it will default to server info of the current object.</param>
        /// <returns></returns>
        public IEnumerable<string> GetDisabledProperties(ScriptingPreferences sp = null)
        {            
            return GetDisabledProperties(GetType(), sp == null ? DatabaseEngineEdition : sp.TargetDatabaseEngineEdition);
        }

        internal static IEnumerable<string> GetDisabledProperties(Type type, DatabaseEngineEdition databaseEngineEdition)
        {
            switch (type.Name)
            {
                case nameof(Database):
                    {
                        if (databaseEngineEdition != DatabaseEngineEdition.SqlDatabaseEdge)
                        {
                            yield return nameof(Database.DataRetentionEnabled);
                        }
                        if (databaseEngineEdition == DatabaseEngineEdition.SqlOnDemand)
                        {
                            yield return nameof(Database.AutoClose);
                            yield return nameof(Database.AutoCreateIncrementalStatisticsEnabled);
                            yield return nameof(Database.AutoCreateStatisticsEnabled);
                            yield return nameof(Database.AutoShrink);
                            yield return nameof(Database.AutoUpdateStatisticsAsync);
                            yield return nameof(Database.AutoUpdateStatisticsEnabled);
                            yield return nameof(Database.BrokerEnabled);
                            yield return nameof(Database.CatalogCollation);
                            yield return nameof(Database.CloseCursorsOnCommitEnabled);
                            yield return nameof(Database.CompatibilityLevel);
                            yield return nameof(Database.ContainmentType);
                            yield return nameof(Database.DatabaseOwnershipChaining);
                            yield return nameof(Database.DatabaseScopedConfigurations);
                            yield return nameof(Database.DatabaseScopedCredentials);
                            yield return nameof(Database.DateCorrelationOptimization);
                            yield return nameof(Database.DelayedDurability);
                            yield return nameof(Database.EncryptionEnabled);
                            yield return nameof(Database.FileGroups);
                            yield return nameof(Database.FilestreamDirectoryName);
                            yield return nameof(Database.FilestreamNonTransactedAccess);
                            yield return nameof(Database.HasMemoryOptimizedObjects);
                            yield return nameof(Database.HonorBrokerPriority);
                            yield return nameof(Database.IsFullTextEnabled);
                            yield return nameof(Database.IsLedger);
                            yield return nameof(Database.IsParameterizationForced);
                            yield return nameof(Database.IsReadCommittedSnapshotOn);
                            yield return nameof(Database.IsSqlDw);
                            yield return nameof(Database.IsSqlDwEdition);
                            yield return nameof(Database.IsVarDecimalStorageFormatEnabled);
                            yield return nameof(Database.IsVarDecimalStorageFormatSupported);
                            yield return nameof(Database.LegacyCardinalityEstimation);
                            yield return nameof(Database.LegacyCardinalityEstimationForSecondary);
                            yield return nameof(Database.LocalCursorsDefault);
                            yield return nameof(Database.MaxDop);
                            yield return nameof(Database.MaxDopForSecondary);
                            yield return nameof(Database.MemoryAllocatedToMemoryOptimizedObjectsInKB);
                            yield return nameof(Database.MemoryUsedByMemoryOptimizedObjectsInKB);
                            yield return nameof(Database.PageVerify);
                            yield return nameof(Database.ParameterSniffing);
                            yield return nameof(Database.ParameterSniffingForSecondary);
                            yield return nameof(Database.QueryOptimizerHotfixes);
                            yield return nameof(Database.QueryOptimizerHotfixesForSecondary);
                            yield return nameof(Database.ReadOnly);
                            yield return nameof(Database.RecoveryModel);
                            yield return nameof(Database.RecursiveTriggersEnabled);
                            yield return nameof(Database.ServiceBroker);
                            yield return nameof(Database.ServiceBrokerGuid);
                            yield return nameof(Database.SnapshotIsolationState);
                            yield return nameof(Database.TargetRecoveryTime);
                            yield return nameof(Database.Trustworthy);
                            yield return nameof(Database.UserAccess);
                            // Used by SqlManagerUI to disable GUI elements named differently than corresponding properties
                            yield return "AllowSnapshotIsolation";
                            yield return "Automatic";
                            yield return "ContainedDatabases";
                            yield return "Cursor";
                            yield return "DBChaining";
                            yield return "DatabaseState";
                            yield return "FileStream";
                            yield return "Parameterization";
                            yield return "RestrictAccess";
                            yield return "ServiceBrokerGUID";
                            yield return "VarDecimalEnabled";
                        }
                        if (databaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                        {
                            yield return nameof(Database.RemoteDataArchiveEnabled);
                        }
                    }
                    break;
                case nameof(Table):
                    {
                        if (databaseEngineEdition != DatabaseEngineEdition.SqlDatabaseEdge)
                        {
                            yield return nameof(Table.DataRetentionEnabled);
                            yield return nameof(Table.DataRetentionPeriod);
                            yield return nameof(Table.DataRetentionPeriodUnit);
                            yield return nameof(Table.DataRetentionFilterColumnName);
                        }
                        if (databaseEngineEdition == DatabaseEngineEdition.SqlOnDemand)
                        {
                            yield return nameof(Table.IndexSpaceUsed);
                            yield return nameof(Table.IsDroppedLedgerTable);
                            yield return nameof(Table.LedgerType);
                            yield return nameof(Table.LedgerViewName);
                            yield return nameof(Table.LedgerViewOperationTypeColumnName);
                            yield return nameof(Table.LedgerViewSchema);
                            yield return nameof(Table.LedgerViewSequenceNumberColumnName);
                            yield return nameof(Table.LedgerViewTransactionIdColumnName);                
                        }
                    }
                    break;
                case nameof(Index):
                    {
                        if (databaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
                        {
                            yield return nameof(Index.SpatialIndexType);
                            yield return nameof(Index.IsSpatialIndex);
                        }
                    }
                    break;
                case nameof(Audit):
                    {
                        if (databaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                        {
                            yield return nameof(Audit.IsOperator);
                        }
                    }
                    break;
                case nameof(DataFile):
                    {
                        if (databaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                        {

                            yield return nameof(DataFile.VolumeFreeSpace);
                        }
                    }
                    break;
                case nameof(Configuration):
                    {
                        if (databaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
                        {
                            yield return nameof(Server.Configuration.ContainmentEnabled);

                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Used by IsSupportedProperty().
        /// Ensures certain properties are disabled for specific server types or editions.
        /// </summary>
        /// <param name="propertyName">Property to check</param>
        /// <param name="sp">Scripting preferences of the targeted server</param>
        /// <returns>True if unsupported, false otherwise</returns>
        private bool IsDisabledProperty(string propertyName, ScriptingPreferences sp = null)
        {
            return GetDisabledProperties(sp).Contains(propertyName);
        }

        /// <summary>
        /// Validates whether the specified property is supported both in current server and target
        /// scripting environment.
        /// </summary>
        /// <param name="propertyName">Property to check</param>
        /// <param name="sp">Scripting preference, which specify parameters to use for scripting</param>
        /// <returns>True if both supported, false otherwise</returns>
        internal bool IsSupportedProperty(string propertyName, ScriptingPreferences sp)
        {
            if (IsSupportedProperty(propertyName) && !IsDisabledProperty(propertyName, sp))
            {
                return PropertyMetadataProvider.CheckPropertyValid(
                    this.GetPropertyMetadataProvider().GetType(),
                    propertyName,
                    ScriptingOptions.ConvertToServerVersion(sp.TargetServerVersion),
                    sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition);
            }
            return false;
        }

        /// <summary>
        /// Validate whether the specified property is supported in current server environment
        /// </summary>
        /// <param name="propertyName">Property to check</param>
        /// <returns>True if supported, false otherwise</returns>
        /// <remarks>
        /// This is for internal SQL usage and may not work correctly if used otherwise
        /// </remarks>
        public bool IsSupportedProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException("propertyName");
            }
            if (this.IsDisabledProperty(propertyName) || !this.Properties.Contains(propertyName))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Throws an exception if the property is not supported by current server environment. If scripting
        /// preferences is not null, also throws an exception if the property is not supported by the target
        /// scripting environment.
        /// </summary>
        /// <param name="propertyName">Property to check</param>
        /// <param name="sp">Scripting preference, which specify parameters to use for scripting</param>
        internal void ThrowIfPropertyNotSupported(string propertyName, ScriptingPreferences sp = null)
        {
            if(!IsSupportedProperty(propertyName))
            {
                throw new UnsupportedVersionException(
                    ExceptionTemplates.PropertyNotSupportedWithDetails(
                        propertyName,
                        this.DatabaseEngineType.ToString(),
                        this.ServerVersion.ToString(),
                        this.DatabaseEngineEdition.ToString()));
            }

            if(sp != null && !IsSupportedProperty(propertyName, sp))
            {
                throw new UnsupportedVersionException(
                    ExceptionTemplates.PropertyNotSupportedWithDetails(
                        propertyName,
                        sp.TargetDatabaseEngineType.ToString(),
                        ScriptingOptions.ConvertToServerVersion(sp.TargetServerVersion).ToString(),
                        sp.TargetDatabaseEngineEdition.ToString()));
            }
        }

        internal static List<string> GetSupportedScriptFields(Type type,string[] fields, ServerVersion serverVersion, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            //TODO change the return type to IEnumerable<string> instead of returning List<string>
            // VSTS 341622
            List<string> listFields = new List<string>();
            foreach (string s in fields)
            {
                if (PropertyMetadataProvider.CheckPropertyValid(type,s,serverVersion,databaseEngineType, databaseEngineEdition))
                {
                    listFields.Add(s);
                }
            }
            return listFields;
        }

        internal void InitChildLevel(Urn levelDescription, ScriptingPreferences sp)
        {
            InitChildLevel(levelDescription, sp, false);
        }

        /// <summary>
        /// Initializes the child object collection associated with the given type.
        /// For example, the name Column would initialize the Columns collection.
        /// </summary>
        /// <param name="childType">The type of child object collection to initialize. </param>
        /// <param name="forScripting">When true, the set of properties needed to script the objects in the collection will be fetched in the query. When false, only the default properties will be fetched. Use Server.SetDefaultInitFields to control the property set.</param>
        /// <remarks>If the childType Urn specifies multiple levels like "PartitionFunction/PartitionFunctionParameter", the application should have already initialized the first level in the Urn before initializing the second level.</remarks>
        public void InitChildCollection(Urn childType, bool forScripting)
        {
            InitChildLevel(childType, new ScriptingPreferences(), forScripting);
        }

        /// <summary>
        /// This function that receives a generic urn initializes for scripting all the objects of a
        /// certain type that are returned by a query with the urn, e.g. for an urn that ends in
        /// /Table/Column the function would initialize all the columns from all the tables in that
        /// database. The enumerator currently does not support this feature
        /// across databases, but we do not need this for transfer anyway
        /// The function can also initialize a child collection via the regular initialization mechanism
        /// </summary>
        /// <param name="levelFilter"></param>
        /// <param name="so"></param>
        /// <param name="forScripting"></param>
        internal void InitChildLevel(Urn levelFilter, ScriptingPreferences sp, bool forScripting)
        {
            InitChildLevel(levelFilter, sp, forScripting, null);
        }

        /// <summary>
        /// This function that receives a generic urn initializes for scripting all the objects of a
        /// certain type that are returned by a query with the urn, e.g. for an urn that ends in
        /// /Table/Column the function would initialize all the columns from all the tables in that
        /// database. The enumerator currently does not support this feature
        /// across databases, but we do not need this for transfer anyway
        /// The function can also initialize a child collection via the regular initialization mechanism.
        /// It loads the fields the default for each object and includes the fields passed as extraFields if there are not default fields
        /// </summary>
        /// <param name="levelFilter"></param>
        /// <param name="so"></param>
        /// <param name="forScripting"></param>
        /// <param name="extraFields"></param>
        internal void InitChildLevel(Urn levelFilter, ScriptingPreferences sp, bool forScripting, IEnumerable<string> extraFields)
        {
            // make sure all the infrastructure properties are present before
            // doing the prefetch query
            PreInitChildLevel();

            // build the XPathExpression for this urn
            XPathExpression parsedUrn = levelFilter.XPathExpression;

            // servers collection does not exist, so we assume here that the Urn
            // contains at least one more level under the server
            int nodeCount = parsedUrn.Length;
            if (nodeCount < 1)
            {
                throw new InternalSmoErrorException(ExceptionTemplates.CallingInitChildLevelWithWrongUrn(levelFilter));
            }

            // figure out what is the children's type
            Type childType = GetChildType(parsedUrn[nodeCount - 1].Name,
                (nodeCount > 1) ? parsedUrn[nodeCount - 2].Name : this.GetType().Name);

            // this is the request we're making to the enumerator
            Request levelQuery = new Request(string.Format(SmoApplication.DefaultCulture, "{0}/{1}", this.Urn, levelFilter));

            Type parentType = null;

            // if we have more than one level in the filter, we will need to add properties
            // for the parent objects, so that we can be able to identify objetcs in the result rows
            if (nodeCount >= 2)
            {
                Type currType = childType;

                int reverseIdx = nodeCount - 2;
                levelQuery.ParentPropertiesRequests = new PropertiesRequest[nodeCount - 1];

                // go backwards through the Urn and figure out if we need to add
                // parent fields to the enumerator request. For instance, if the Urn is
                // like Server[@Name='name']/Database[@Name='name']/Table/Column
                // we would like to add Name and Schema for the table, and stop at the Database
                // level if we are calling this method from a Database object
                while (reverseIdx >= 0)
                {
                    currType = GetChildType(parsedUrn[reverseIdx].Name,
                        (reverseIdx > 0) ? parsedUrn[reverseIdx - 1].Name : this.GetType().Name);
                    Diagnostics.TraceHelper.Assert(null != currType, "currType == null");

                    if (null == parentType)
                    {
                        parentType = currType;
                    }

                    // now look at the current level to see what properties we'll need
                    // for this parent in the result set. We'll need Name, and if the object
                    // has Schema we'll also need schema
                    PropertiesRequest currProps = new PropertiesRequest();

                    if (IsOrderedByID(currType))
                    {
                        currProps.Fields = new String[] { "ID", "Name" };
                    }
                    else
                    {
                        StringCollection fields = ObjectKeyBase.GetFieldNames(currType);
                        currProps.Fields = new String[fields.Count];
                        fields.CopyTo(currProps.Fields, 0);
                    }

                    currProps.OrderByList = GetOrderByList(currType);
                    levelQuery.ParentPropertiesRequests[nodeCount - 2 - reverseIdx] = currProps;

                    reverseIdx--;
                }
            }

            // If no ParentType was found just use the current type. This should only be needed for relative Urns
            parentType = parentType ?? this.GetType();

            var defaultFields = forScripting
                ? GetServerObject()
                    .GetScriptInitFieldsInternal(childType, parentType, sp, DatabaseEngineEdition)
                : GetServerObject()
                    .GetDefaultInitFieldsInternal(childType, DatabaseEngineEdition);
            // if the initialization is done with the goal of scripting the object, the bring the
            // fields required for scripting, otherwise get the default init fields
            levelQuery.Fields = defaultFields;
            if (extraFields != null && extraFields.Count() > 0)
            {
                levelQuery.Fields = levelQuery.Fields.Union(extraFields).ToArray();
            }

            // we know for sure that the last level of the urn has this ordering,
            // regardless of the usage of default init fields
            levelQuery.OrderByList = GetOrderByList(childType);

            // do this special case for initializing Database with default init fields
            if (IsDatabaseSpecialCase(forScripting, parsedUrn, levelQuery.Fields, defaultFields))
            {
                DoDatabaseSpecialCase(levelQuery, levelFilter, forScripting, null, 0, defaultFields);
            }
            else
            {
                //InitObjectsFromEnumResults uses StringComparer and the data reader in it will be still open
                //at the point when another datareader tries to execute query for collection. there is a chance for collation.
                //So StringComparer must be Initialized before executing the query.
                InitializeStringComparer();
                // execute the query
                using (var reader = ExecutionManager.GetEnumeratorDataReader(levelQuery))
                {
                    // init all child objects from the query results
                    InitObjectsFromEnumResults(levelFilter, reader, forScripting, null, 0, false);
                }

                string[] fields;
                if (forScripting
                    && this.GetServerObject().GetScriptInitExpensiveFieldsInternal(childType, parentType, sp, out fields, sp.TargetDatabaseEngineEdition))
                {
                    levelQuery.Fields = fields;
                    // execute the query
                    using (System.Data.IDataReader reader = this.ExecutionManager.GetEnumeratorDataReader(levelQuery))
                    {
                        // init all child objects from the query results
                        InitObjectsFromEnumResults(levelFilter, reader, forScripting, null, 0, false);
                    }
                }

                // make sure all the child collections have the proper retrieved status
                MarkChildCollRetrieved(levelFilter, 0);
            }
        }

        /// <summary>
        /// When enumerating Database instances, we can't successfully fetch extra properties for unavailable databases.
        /// When clients ask for "Status" in the property list explicitly, we will proactively exclude databases with a non-1 
        /// status from full property population when such extra properties are requested.
        /// </summary>
        /// <param name="forScripting"></param>
        /// <param name="parsedUrn"></param>
        /// <param name="fields"></param>
        /// <param name="defaultFields"></param>
        /// <returns></returns>
        private bool IsDatabaseSpecialCase(bool forScripting, XPathExpression parsedUrn, string[] fields, IList<string> defaultFields)
        {
            if (!forScripting && parsedUrn[parsedUrn.Length - 1].Name == Database.UrnSuffix)
            {
                var extraFields = fields.Except(defaultFields).ToList();
                if (extraFields.Count > 1 && extraFields.Contains(nameof(Database.Status)))
                {
                    return true;
                }
            }
            return false;
        }

        private void DoDatabaseSpecialCase(Request levelQuery, Urn levelFilter, bool forScripting, List<string> urnList, int startLeafIdx, IList<string> defaultFields)
        {
            var origFields = levelQuery.Fields;

            // first query for Name and Status
            levelQuery.Fields = defaultFields.Union(new[] { nameof(Database.Status) }).ToArray();

            // execute the query
            using (var statusReader = ExecutionManager.GetEnumeratorDataReader(levelQuery))
            {
                // init all child objects from the query results
                InitObjectsFromEnumResults(levelFilter, statusReader, forScripting, null, 0, urnList != null);
            }

            // make sure all the child collections have the proper retrieved status

            // EDDUDE: What does this do that the Init'ing code doesn't already do
            // (it already sets all the collections touched that have no filter at their level to "true")
            // so I will skip this when InitQueryUrns() is the caller.
            if (urnList == null)
            {
                MarkChildCollRetrieved(levelFilter, 0);
            }

            // now that we have Name and Status for every Database, initialize the
            // rest of the properties for databases that are accessible
            levelQuery.Fields = origFields;

            string stringUrn = levelQuery.Urn.ToString();
            if (stringUrn.EndsWith("]", StringComparison.Ordinal))
            {
                levelQuery.Urn = stringUrn.Insert(stringUrn.Length - 1, " and @Status=1");
            }
            else
            {
                levelQuery.Urn += "[@Status=1]";
            }

            stringUrn = levelFilter.ToString();
            if (stringUrn.EndsWith("]", StringComparison.Ordinal))
            {
                levelFilter = stringUrn.Insert(stringUrn.Length - 1, " and @Status=1");
            }
            else
            {
                levelFilter = levelFilter + "[@Status=1]";
            }


            // execute the query
            using (System.Data.IDataReader reader = this.ExecutionManager.GetEnumeratorDataReader(levelQuery))
            {
                // init all child objects from the query results
                InitObjectsFromEnumResults(levelFilter, reader, forScripting, urnList, startLeafIdx, (urnList != null));
            }
        }

        private void MarkChildCollRetrieved(Urn levelFilter, int filterIdx)
        {
            MarkChildCollRetrievedRec(this, levelFilter.XPathExpression, filterIdx);
        }

        // for the moment, this function works only for Urn's that have no level filters
        private void MarkChildCollRetrievedRec(SqlSmoObject currentSmoObject,
            XPathExpression levelFilter, int filterIdx)
        {
            // special case for DefaultConstraint
            if (currentSmoObject is Column && levelFilter[filterIdx].Name == "Default")
            {
                if (filterIdx == levelFilter.Length - 1)
                {
                    ((Column)currentSmoObject).m_bDefaultInitialized = true;
                }
                else
                {
                    DefaultConstraint dc = ((Column)currentSmoObject).DefaultConstraint;
                    if (null != dc)
                    {
                        MarkChildCollRetrievedRec(dc, levelFilter, filterIdx + 1);
                }
                }
                return;
            }

            // special case for FullTextIndex
            if (currentSmoObject is TableViewBase && levelFilter[filterIdx].Name == "FullTextIndex")
            {
                if (filterIdx == levelFilter.Length - 1)
                {
                    ((TableViewBase)currentSmoObject).m_bFullTextIndexInitialized = true;
                }
                else
                {
                    FullTextIndex fti = ((TableViewBase)currentSmoObject).FullTextIndex;
                    if (null != fti)
                    {
                        MarkChildCollRetrievedRec(fti, levelFilter, filterIdx + 1);
                }
                }
                return;
            }

            // special case for Endpoint classes
            if (currentSmoObject is Endpoint)
            {
                if (filterIdx == levelFilter.Length - 1)
                {
                    //we don't mark it as initialized because we use the Payload and Protocol enums for that
                }
                else
                {
                    SqlSmoObject obj = null;
                    Endpoint ep = (Endpoint)currentSmoObject;
                    switch (levelFilter[filterIdx].Name)
                    {
                        case "Soap": obj = ep.Payload.Soap; break;
                        case "DatabaseMirroring": obj = ep.Payload.DatabaseMirroring; break;
                        case "ServiceBroker": obj = ep.Payload.ServiceBroker; break;
                        case "Http": obj = ep.Protocol.Http; break;
                        case "Tcp": obj = ep.Protocol.Tcp; break;
                    }
                    if (null != obj)
                    {
                        MarkChildCollRetrievedRec(obj, levelFilter, filterIdx + 1);
                    }
                }
                return;
            }

            AbstractCollectionBase childColl = GetChildCollection(currentSmoObject, levelFilter,
                filterIdx, GetServerObject().ServerVersion);

            if (filterIdx == levelFilter.Length - 1)
            {
                // mark collection retrived only if there was no filter
                if (null == levelFilter[filterIdx].Filter)
                {
                    childColl.initialized = true;
                }
            }
            else
            {
                HashSet<string> idSet = new HashSet<string>();
                //Special case for when the URN is for an object under a database,
                //in that case as we're walking down the URN we don't want to retrieve
                //the entire DB collection since this can be very expensive (especially
                //for Azure) and the caller might not even have permissions for the other DBs
                //Note this will not change the case where the caller specifically wants to
                //populate the DB collection - since in that case they would not be settting
                //a name attribute and thus it would fall into the else.
                string nameAttribute = levelFilter[filterIdx].GetAttributeFromFilter("Name");
                if (childColl is DatabaseCollection && !string.IsNullOrEmpty(nameAttribute))
                {
                    MarkChildCollRetrievedRec(GetServerObject().Databases.GetObjectByName(nameAttribute), levelFilter, filterIdx + 1);
                }
                else
                {
                    IEnumerator enumColl = ((IEnumerable)childColl).GetEnumerator();
                    while (enumColl.MoveNext())
                    {
                        // don't advance unless the object is in the filter
                        // otherwise we'll move on branches of the tree that will not be present
                        // in the result set
                        if (ObjectInFilter((SqlSmoObject) enumColl.Current, levelFilter[filterIdx], idSet))
                        {
                            MarkChildCollRetrievedRec((SqlSmoObject) enumColl.Current, levelFilter, filterIdx + 1);
                        }
                    }
                }

            }
        }

        private bool ObjectInFilter(SqlSmoObject current, XPathExpressionBlock levelFilterBlock, HashSet<string> idSet)
        {
            FilterNode filter = levelFilterBlock.Filter;
            if (null == filter)
            {
                return true;
            }

            if (filter.NodeType == FilterNode.Type.Operator)
            {
                FilterNodeOperator opNode = (FilterNodeOperator)filter;
                return ObjectInFilterRec(current, opNode);
            }
            else if (filter.NodeType == FilterNode.Type.Function)
            {
                FilterNodeFunction functionNode = (FilterNodeFunction)filter;

                if (functionNode.FunctionType == FilterNodeFunction.Type.In)
                {
                    if (idSet.Count == 0)
                    {
                        string ids = ((FilterNodeConstant)functionNode.GetParameter(1)).ValueAsString;
                        idSet.UnionWith(ids.Split(','));
                    }

                    int? id = current.GetPropValueOptional<int>("ID");
                    if (id.HasValue && idSet.Contains(id.ToString()))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            throw new SmoException(ExceptionTemplates.UnknownFilter(levelFilterBlock.ToString()));
        }

        private bool ObjectInFilterRec(SqlSmoObject current, FilterNodeOperator opNode)
        {
            if (opNode.OpType == FilterNodeOperator.Type.OR)
            {
                return ObjectInFilterRec(current, (FilterNodeOperator)opNode.Left) ||
                        ObjectInFilterRec(current, (FilterNodeOperator)opNode.Right);
            }
            else if (opNode.OpType == FilterNodeOperator.Type.And)
            {
                return (CompareAttributeToObject(current, (FilterNodeOperator)opNode.Left) &&
                        CompareAttributeToObject(current, (FilterNodeOperator)opNode.Right));
            }
            else if (opNode.OpType == FilterNodeOperator.Type.EQ ||
                     opNode.OpType == FilterNodeOperator.Type.LT ||
                     opNode.OpType == FilterNodeOperator.Type.GT)
            {
                return CompareAttributeToObject(current, opNode);
            }

            throw new SmoException(ExceptionTemplates.UnknownFilter(opNode.ToString()));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="current"></param>
        /// <param name="oper"></param>
        /// <returns>true if the operator fits the object</returns>
        private bool CompareAttributeToObject(SqlSmoObject current, FilterNodeOperator oper)
        {
            FilterNodeAttribute attr = (FilterNodeAttribute)oper.Left;
            object cnstVal = null;
            if (oper.Right.NodeType == FilterNode.Type.Constant)
            {
                FilterNodeConstant cnst = (FilterNodeConstant)oper.Right;
                cnstVal = cnst.Value;
            }

            //Use Parent's String Comparer to Compare Keys
            StringComparer keyStringComparer = GetParentStringComparer(current);

            switch (oper.OpType)
            {
                case FilterNodeOperator.Type.EQ:
                    switch (oper.Right.NodeType)
                    {
                        case FilterNode.Type.Constant:
                            switch (attr.Name)
                            {
                                case "Name":
                                    return 0 == keyStringComparer.Compare(((SimpleObjectKey)current.key).Name,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                case "Schema":
                                    return 0 == keyStringComparer.Compare(((SchemaObjectKey)current.key).Schema,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                case "ID":
                                    return ((MessageObjectKey)current.key).ID == (Int32)cnstVal;
                                case "Language":
                                    return 0 == keyStringComparer.Compare(((MessageObjectKey)current.key).Language,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                default:
                                    throw new UnknownPropertyException(attr.Name);
                            }

                        case FilterNode.Type.Function:
                            if (attr.Name == "IsSystemObject")
                            {
                                FilterNodeFunction fun = (FilterNodeFunction)oper.Right;
                                if (fun.FunctionType != FilterNodeFunction.Type.True &&
                                    fun.FunctionType != FilterNodeFunction.Type.False)
                                {
                                    throw new InternalSmoErrorException(ExceptionTemplates.UnsupportedUrnFilter(attr.Name, fun.FunctionType.ToString()));
                                }
                                bool getsystemobject = (fun.FunctionType == FilterNodeFunction.Type.True);
                                return (getsystemobject == (bool)current.GetPropValue("IsSystemObject"));
                            }
                            else
                            {
                                throw new InternalSmoErrorException(ExceptionTemplates.UnsupportedUrnAttrib(attr.Name));
                            }
                    }
                    break;

                case FilterNodeOperator.Type.LT:
                    switch (oper.Right.NodeType)
                    {
                        case FilterNode.Type.Constant:
                            switch (attr.Name)
                            {
                                case "Name":
                                    return -1 == keyStringComparer.Compare(((SimpleObjectKey)current.key).Name,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                case "Schema":
                                    return -1 == keyStringComparer.Compare(((SchemaObjectKey)current.key).Schema,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                case "ID":
                                    return ((MessageObjectKey)current.key).ID < (Int32)cnstVal;
                                case "Language":
                                    return -1 == keyStringComparer.Compare(((MessageObjectKey)current.key).Language,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                default:
                                    throw new UnknownPropertyException(attr.Name);
                            }
                    }
                    break;

                case FilterNodeOperator.Type.GT:
                    switch (oper.Right.NodeType)
                    {
                        case FilterNode.Type.Constant:
                            switch (attr.Name)
                            {
                                case "Name":
                                    return 1 == keyStringComparer.Compare(((SimpleObjectKey)current.key).Name,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                case "Schema":
                                    return 1 == keyStringComparer.Compare(((SchemaObjectKey)current.key).Schema,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                case "ID":
                                    return ((MessageObjectKey)current.key).ID > (Int32)cnstVal;
                                case "Language":
                                    return 1 == keyStringComparer.Compare(((MessageObjectKey)current.key).Language,
                                                                    Urn.UnEscapeString((string)cnstVal));
                                default:
                                    throw new UnknownPropertyException(attr.Name);
                            }
                    }
                    break;
            }

            // If we get to here, we didn't know what to do with this particular operator combo
            throw new SmoException(ExceptionTemplates.UnknownFilter(oper.ToString()));
        }

        /// <summary>
        /// Returns Parent's String Comparer
        /// </summary>
        /// <param name="Object"></param>
        /// <returns></returns>
        private StringComparer GetParentStringComparer(SqlSmoObject Object)
        {
            SqlSmoObject Parent;

            if (Object.ParentColl == null || Object.ParentColl.ParentInstance == null)
            {
                Parent = Object;
            }
            else
            {
                Parent = Object.ParentColl.ParentInstance;
            }

            return Parent.StringComparer;
        }

        private void InitObjectsFromEnumResults(Urn levelFilter, System.Data.IDataReader reader, bool forScripting, List<string> urnList, int startLeafIdx, bool skipServer)
        {
            if (!reader.Read())
            {
                reader.Close();
                return;
            }
            object[] parentRow = new object[reader.FieldCount];
            reader.GetValues(parentRow);

            // For Object Queries, skip the Server top-level field advancing unless this query is *for* a Server.
            // It is a waste of time doing the Server since you already know you have it anyhow,
            // except when it is the true leaf and you might have some fields to populate into it.
            bool skipOverServerLevel = (skipServer && levelFilter.XPathExpression.Length > 1);
            int filterIdx = skipOverServerLevel ? 1 : 0;
            int columnIdx = skipOverServerLevel ? 1 : 0;
            InitObjectsFromEnumResultsRec(this, levelFilter.XPathExpression, filterIdx,
                reader, columnIdx, parentRow, forScripting, urnList, startLeafIdx);
        }

        /// <summary>
        /// the general philosophy of this function is like this:
        /// we are doing a query that returns a potentially large table, and we would
        /// like to minimize the lookups in the collections; we have arranged
        /// for the results to be ordered in the same order as our collections,
        /// so dumping the results into the objects' properties becomes something
        /// like a merge operation between two data sets that have the same ordering
        /// </summary>
        /// <param name="currentSmoObject">the current object</param>
        /// <param name="levelFilter">the Urn as a parsed XPath</param>
        /// <param name="filterIdx">the current level in the Urn</param>
        /// <param name="reader">the query results that we are
        /// transferring to the Smo objects in the tree</param>
        /// <param name="columnIdx">column index in the result table</param>
        /// <param name="rowIdx">row index in the result table</param>
        private void InitObjectsFromEnumResultsRec(SqlSmoObject currentSmoObject,
            XPathExpression levelFilter,
            int filterIdx,
            System.Data.IDataReader reader,
            int columnIdx,
            object[] parentRow,
            bool forScripting,
            List<string> urnList,
            int startLeafIdx)
        {
            // have we finished the table already?
            if (reader.IsClosed)
            {
                return;
            }

            Type childType;
            if (urnList != null)
            {
                // We have to special case Server here, since we normally skip over it for Object Query
                // except when it is all by itself (i.e. the query is for just "Server").
                int nodeCount = levelFilter.Length;
                childType = GetChildType((
                    nodeCount > filterIdx) ? levelFilter[filterIdx].Name : currentSmoObject.GetType().Name,
                    currentSmoObject.GetType().Name);
            }
            else
            {
                childType = GetChildType(levelFilter[filterIdx].Name, currentSmoObject.GetType().Name);
            }

            //
            // EDDUDE: All this specific type checking below is to avoid hitting the code that follows
            // which would try to get the collection base for whatever the next level is.
            // Things which are singletons or special in some way are just doing the advnace and OM creation
            // their own way.
            //
            // The code added to help support more generalized Object Querying does this in a more general way
            // inside GetChildSingleton. I am not removing this code though due to the fragility of the whole stack,
            // and existing callers of InitChildLevel work fine with just this in place. Also, some types like FTI seem
            // to know to allow for one column for a key to skip (passing columnIdx+1),
            // whereas any singleton to me has no key, its all data to grab.
            //

            // special case for DefaultConstraint
            if (childType.Equals(typeof(DefaultConstraint)))
            {
                if (filterIdx == levelFilter.Length - 1)
                {
                    // need to decide here if we can create the constraint for scripting or not
                    ((Column)currentSmoObject).InitializeDefault(reader, columnIdx, forScripting);

                    if (urnList != null)
                    {
                        urnList.Add(((Column)currentSmoObject).DefaultConstraint.Urn.ToString());
                    }

                    if (!reader.Read())
                    {
                        reader.Close();
                        return;
                    }
                }
                else
                {
                    DefaultConstraint dc = ((Column)currentSmoObject).DefaultConstraint;
                    if (null != dc)
                    {
                        InitObjectsFromEnumResultsRec(dc, levelFilter,
                            //next level
                            filterIdx + 1,
                            reader,
                            //ocupies one column = name
                            columnIdx + 1,
                            parentRow, forScripting, urnList, startLeafIdx);
                }
                }
                return;
            }

            // special case for FullTextIndex
            if (childType.Equals(typeof(FullTextIndex)))
            {
                if (filterIdx == levelFilter.Length - 1)
                {
                    currentSmoObject = ((TableViewBase)currentSmoObject).InitializeFullTextIndexNoEnum();

                    // set the full text index's properties from the current row
                    if (reader.FieldCount - columnIdx > 1)
                    {
                        currentSmoObject.AddObjectPropsFromDataReader(reader, true, columnIdx, -1);
                    }

                    // mark the object as having all the necessary properties for scripting
                    if (forScripting)
                    {
                        currentSmoObject.InitializedForScripting = true;
                    }

                    if (urnList != null)
                    {
                        urnList.Add(currentSmoObject.Urn.ToString());
                    }

                    if (!reader.Read())
                    {
                        reader.Close();
                        return;
                    }
                }
                else
                {
                    FullTextIndex fti = ((TableViewBase)currentSmoObject).FullTextIndex;
                    if (null != fti)
                    {
                        InitObjectsFromEnumResultsRec(fti, levelFilter,
                            filterIdx + 1, reader, columnIdx + 1,
                            parentRow, forScripting, urnList, startLeafIdx);
                }
                }
                return;
            }

            // special case for Endpoint objects
            if (currentSmoObject is Endpoint)
            {
                if (filterIdx == levelFilter.Length - 1)
                {
                    if (!reader.Read())
                    {
                        reader.Close();
                        return;
                    }
                }
                else
                {
                    SqlSmoObject obj = null;
                    Endpoint ep = (Endpoint)currentSmoObject;
                    if (childType.Equals(typeof(SoapPayload)))
                    {
                        obj = ep.Payload.Soap;
                    }
                    else if (childType.Equals(typeof(DatabaseMirroringPayload)))
                    {
                        obj = ep.Payload.DatabaseMirroring;
                    }
                    else if (childType.Equals(typeof(ServiceBrokerPayload)))
                    {
                        obj = ep.Payload.ServiceBroker;
                    }
                    else if (childType.Equals(typeof(HttpProtocol)))
                    {
                        obj = ep.Protocol.Http;
                    }
                    else if (childType.Equals(typeof(TcpProtocol)))
                    {
                        obj = ep.Protocol.Tcp;
                    }
                    if (null != obj)
                    {
                        InitObjectsFromEnumResultsRec(obj, levelFilter,
                            // next filtering level
                            filterIdx + 1,
                            reader,
                            // no key, so we don't advance the column index
                            columnIdx,
                            parentRow,
                            forScripting,
                            urnList,
                            startLeafIdx);
                    }
                }
                return;
            }

            // Handle Object population for non-collections, such as DatabaseOptions.
            // To support that we first try to get the collection base, and if that fails we resort to
            // doing a similar check for a singleton child of the parent object.

            AbstractCollectionBase childColl = null;
            bool isNonCollection = false;

            try
            {
                childColl = GetChildCollection(currentSmoObject, levelFilter,
                    filterIdx, GetServerObject().ServerVersion);
            }
            catch (Exception e)
            {
                // The old InitChildLevel caller just wants to throw here. It never handled singletons here anyhow.
                if (urnList == null)
                {
                    throw;
                }

                if (!(e is ArgumentException || e is InvalidCastException))
                {
                    throw;
                }

                // We come here if we ask for a child collection for a level that isn't really a collection like singletons.
                // Since we still want to get into AdvanceInitRec with them, we need to process them differently.
                // Only do this for the Object Query case (urnList != null), not the old InitChildLevel cases.

                isNonCollection = true;
            }

            // This represents the number of data columns occupied by the current object's keys.
            // It can return 0 if the Type really doesn't have any key fields.
            int columnOffset = GetQueryTypeKeyFieldsCount(childType);

            // For singletons, we know we are on the leaf level and just need to absorb properties
            if (isNonCollection)
            {
                SqlSmoObject currObj = GetChildSingleton(currentSmoObject, levelFilter,
                    filterIdx, GetServerObject().ServerVersion);

                if (!AdvanceInitRec(currObj, levelFilter, filterIdx, reader, columnIdx,
                    columnOffset, parentRow, forScripting, urnList, startLeafIdx))
                {
                    return;
                }
            }
            else
            {
                // Perform collection merging/adding
                bool isOrderedByID = IsOrderedByID(childType);

                if (childColl.initialized)
                {
                    IEnumerator childCollEnum = ((IEnumerable)childColl).GetEnumerator();
                    while (childCollEnum.MoveNext())
                    {
                        // did we finish the table already?
                        if (reader.IsClosed)
                        {
                            return;
                        }

                        SqlSmoObject currObj = (SqlSmoObject)childCollEnum.Current;

                        // does the parent stay the same?
                        if (CompareRows(reader, parentRow, 0, columnIdx))
                        {

                            int relativeOrder = CompareObjectToRow(currObj,
                                reader,
                                columnIdx,
                                isOrderedByID,
                                levelFilter,
                                filterIdx);
                            // is the current object in the current row ?
                            if (0 == relativeOrder)
                            {
                                if (!AdvanceInitRec(currObj, levelFilter, filterIdx, reader, columnIdx,
                                    columnOffset, parentRow, forScripting, urnList, startLeafIdx))
                                {
                                    return;
                                }

                            }
                            else if (relativeOrder > 0)
                            {
                                // OK, the row is not the object that we have a pointer to
                                // there can be two reasons for that
                                // 1. the result set does not contain all the objects in the collection,
                                // because the last level of filtration in Urn might filter objects on the deeper
                                // levels, eg if we're asking for the /Table/Column/ExtendedProperties
                                // the not all the tables and columns might show in the result set if they
                                // do not have extended properties
                                // 2. the collection is out of sync with the server because a new object was
                                // added on the server
                                // 3. the sorting order of engine and .net is not matching for the same culture
                                // Number 1 is the case we are optimizing for, so there we will just loop
                                // through the collection and try to find the object. If we can't find it, then we
                                // are in case 2, and we have to add the object to the collection, and move the
                                // enumerator to point to the newly added object
                                // case 1 - relativeOrder > 0, skip this object
                                // case 2 - relativeOrder < 0 - add the object to the collection

                                // reset the enumerator, so that we can add objects to the collection

                                SqlSmoObject newObject = GetExistingOrCreateNewObject(reader, columnIdx, childType, childColl, isOrderedByID);

                                childCollEnum = ((IEnumerable)childColl).GetEnumerator();

                                // roll the enumerator the the new object
                                while (childCollEnum.MoveNext() && childCollEnum.Current != newObject)
                                {
                                    ;
                                }


                                // at this point, the enumerator points out to the object, so just process it
                                // in the usual manner, either go down one level or update properties
                                currObj = (SqlSmoObject)childCollEnum.Current;

                                if (!AdvanceInitRec(newObject, levelFilter, filterIdx, reader, columnIdx,
                                    columnOffset, parentRow, forScripting, urnList, startLeafIdx))
                                {
                                    return;
                                }

                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    // don't attempt to do any comparison if we finished reading
                    if (reader.IsClosed)
                    {
                        return;
                    }

                    // if we have rows that should be in the collection, but have been added to the end
                    // so we could not discover them
                    while (CompareRows(reader, parentRow, 0, columnIdx))
                    {

                        SqlSmoObject newObject = GetExistingOrCreateNewObject(reader, columnIdx, childType, childColl, isOrderedByID);

                        if (!AdvanceInitRec(newObject, levelFilter, filterIdx, reader, columnIdx,
                            columnOffset, parentRow, forScripting, urnList, startLeafIdx))
                        {
                            return;
                        }

                    }
                }
                else
                {
                    // child collection is not initialized, so we'll have to add the objects manually
                    // if they are not in the collection

                    // if the collection was initially empty, need to remember this
                    // as we can skip looking up the row in the collection
                    bool mustLookup = (0 != childColl.NoFaultCount);

                    //if collection is initially empty and this is the final level we can
                    //safely keep on adding objects at the end as they will come ordered in dataset
                    bool skipOrderChecking = (isOrderedByID &&
                        (0 == childColl.NoFaultCount) &&
                        ((null == levelFilter[filterIdx].Filter) && (filterIdx == levelFilter.Length - 1)));

                    // return if there are no more rows
                    if (!reader.IsClosed)
                    {

                        SqlSmoObject currObj = null;
                        while (CompareRows(reader, parentRow, 0, columnIdx))
                        {
                            // try to find if the object exists in the collection; since the collection
                            // is unitialized, then we need to use a special lookup function
                            // because the regular one does an enumerator call to try to find
                            // the object
                            if (mustLookup)
                            {
                                ObjectKeyBase childObjectKey = ObjectKeyBase.CreateKeyOffset(childType, reader, columnIdx);
                                currObj = childColl.NoFaultLookup(childObjectKey);
                            }
                            else
                            {
                                currObj = null;
                            }

                            // if lookup failed, we have to add the object to the collection
                            if (null == currObj)
                            {
                                currObj = CreateNewObjectFromRow(childColl, childType,
                                     reader, columnIdx, isOrderedByID, skipOrderChecking);
                            }

                            if (!AdvanceInitRec(currObj, levelFilter, filterIdx, reader, columnIdx,
                                columnOffset, parentRow, forScripting, urnList, startLeafIdx))
                            {
                                break;
                            }
                        }
                    }
                }

                // does the last level have any filtering? If not, this means the
                // collection is going to be initialized with all its members
                if ((null == levelFilter[filterIdx].Filter) && (filterIdx == levelFilter.Length - 1))
                {
                    childColl.initialized = true;
            }
        }
        }

        // creates a new object from the current row, and adds it to the child collection
        private SqlSmoObject CreateNewObjectFromRow(AbstractCollectionBase childColl,
            Type childType,
            System.Data.IDataReader reader,
            int columnIdx,
            bool isOrderedByID,
            bool skipOrderChecking)
        {
            ObjectKeyBase childObjectKey = ObjectKeyBase.CreateKeyOffset(childType, reader, columnIdx);

            object[] args = new object[] { childColl, childObjectKey, SqlSmoState.Existing };
            SqlSmoObject currObj = (SqlSmoObject)Activator.CreateInstance(childType,
                BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.CreateInstance,
                null, args, null);
            var initializer = (ISqlSmoObjectInitialize) currObj;
            initializer.InitializeFromDataReader(reader);

            // update state
            ((SqlSmoObject)currObj).SetState(PropertyBagState.Lazy);

            if (isOrderedByID)
            {
                currObj.Properties.Get("ID").SetValue(reader.GetValue(columnIdx));
                currObj.Properties.Get("ID").SetRetrieved(true);

                if (skipOrderChecking)
                {
                    //Since we need to skip order checking we will add objects at the end of collection
                    ParameterCollectionBase orderedCollection = childColl as ParameterCollectionBase;
                    if (orderedCollection != null)
                    {
                        orderedCollection.InternalStorage.InsertAt(orderedCollection.InternalStorage.Count, currObj);
                        return currObj;
                    }
                }
            }

            childColl.AddExisting(currObj);

            return currObj;
        }

        /// <summary>
        /// Gets existing object from child collection if possible
        /// otherwise create new object from the current row and add it to the child collection
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnIdx"></param>
        /// <param name="childType"></param>
        /// <param name="childColl"></param>
        /// <param name="isOrderedByID"></param>
        /// <returns></returns>
        private SqlSmoObject GetExistingOrCreateNewObject(System.Data.IDataReader reader, int columnIdx, Type childType, AbstractCollectionBase childColl, bool isOrderedByID)
        {
            SqlSmoObject sqlSmoObject = null;
            if (!isOrderedByID && (childColl is SortedListCollectionBase))
            {
                //Check if object is already in collection
                sqlSmoObject = ((SortedListCollectionBase)childColl).NoFaultLookup(ObjectKeyBase.CreateKeyOffset(childType, reader, columnIdx));
            }

            if (sqlSmoObject == null)
            {
                sqlSmoObject = CreateNewObjectFromRow(childColl, childType,
                    reader, columnIdx, isOrderedByID, false);
            }
            return sqlSmoObject;
        }

        /// <summary>
        /// move one row down if we are on the last level of the Urn
        /// otherwise it move one level to the right into the Urn
        /// </summary>
        /// <param name="currentSmoObject"></param>
        /// <param name="levelFilter"></param>
        /// <param name="filterIdx"></param>
        /// <param name="reader"></param>
        /// <param name="columnIdx"></param>
        /// <param name="columnOffset"></param>
        /// <param name="parentRow"></param>
        /// <param name="forScripting"></param>
        /// <returns>true if there are still records to read </returns>
        private bool AdvanceInitRec(SqlSmoObject currentSmoObject,
            XPathExpression levelFilter,
            int filterIdx,
            System.Data.IDataReader reader,
            int columnIdx,
            int columnOffset,
            object[] parentRow,
            bool forScripting,
            List<string> urnList,
            int startLeafIdx)
        {
            // verify that we have received a valid parent row
            Diagnostics.TraceHelper.Assert(null != parentRow, "parentRow == null");
            Diagnostics.TraceHelper.Assert(parentRow.Length == reader.FieldCount,
                            "parentRow.Length != reader.FieldCount");

            // if we are on the last level of the Urn, the eat the current row
            if (filterIdx == levelFilter.Length - 1)
            {
                // set the object's properties from the current row
                if (reader.FieldCount - columnIdx > 1)
                {
                    currentSmoObject.AddObjectPropsFromDataReader(reader, true, columnIdx, -1);
                }

                // mark the object as having all the necessary properties for scripting
                if (forScripting)
                {
                    currentSmoObject.InitializedForScripting = true;
                }

                // Add to Urn list if we are asked to keep one
                if (urnList != null)
                {
                    urnList.Add(currentSmoObject.Urn.ToString());
                }

                // move forward in the result table
                if (!reader.Read())
                {
                    reader.Close();
                    return false;
                }
            }
            else
            {
                object[] newParentRow = new object[reader.FieldCount];
                reader.GetValues(newParentRow);

                // move to the next level of the Urn
                InitObjectsFromEnumResultsRec(currentSmoObject, levelFilter, filterIdx + 1,
                    reader, columnIdx + columnOffset, newParentRow, forScripting, urnList, startLeafIdx);
            }

            return true;
        }

        // get child collection. This should not fail, since the collection
        // name is hardcoded in internal prefetch calls
        internal static AbstractCollectionBase GetChildCollection(SqlSmoObject parent,
            XPathExpression levelFilter, int filterIdx, ServerVersion srvVer)
        {
            return GetChildCollection(
                parent,
                levelFilter[filterIdx].Name,
                levelFilter[filterIdx].GetAttributeFromFilter("CategoryClass"),
                srvVer);
        }

        // Given a single type name, return plural name for collection of these types
        internal static string GetPluralName(string name, SqlSmoObject parent)
        {
            switch (name)
            {
                case "Index": return "Indexes";
                case "Numbered": return "NumberedStoredProcedures";
                case "Method": return "SoapPayloadMethods";
                case "Param": return "Parameters";
                case "ExtendedProperty": return "ExtendedProperties";
                case "JobCategory": return "JobCategories";
                case "AlertCategory": return "AlertCategories";
                case "OperatorCategory": return "OperatorCategories";
                case "Column":
                    if (parent is Statistic)
                    {
                        return "StatisticColumns";
                    }
                    else
                    {
                        return "Columns";
                    }

                case "Step": return "JobSteps";
                case "Schedule":
                    if (parent is Job)
                    {
                        return "JobSchedules";
                    }
                    else
                    {
                        return "SharedSchedules";
                    }

                case "Login":
                    if (parent is LinkedServer)
                    {
                        return "LinkedServerLogins";
                    }
                    else
                    {
                        return "Logins";
                    }

                case "SqlAssembly": return "Assemblies";
                case nameof(ExternalLanguage): return "ExternalLanguages";
                case "ExternalLibrary": return "ExternalLibraries";
                case "FullTextIndexColumn": return "IndexedColumns";
                case "DdlTrigger": return "Triggers";
                case "MailProfile": return "Profiles";
                case "MailAccount": return "Accounts";
                case "ServiceQueue": return "Queues";
                case "BrokerService": return "Services";
                case "BrokerPriority": return "Priorities";
                case "ServiceRoute": return "Routes";
                case "EventClass": return "EventClasses";
                case "NotificationClass": return "NotificationClasses";
                case "SubscriptionClass": return "SubscriptionClasses";
                case "SearchProperty": return "SearchProperties";
                case "SecurityPolicy": return "SecurityPolicies";
                case "ExternalDataSource": return "ExternalDataSources";
                case "ExternalFileFormat": return "ExternalFileFormats";
                case "AvailabilityGroupListenerIPAddress": return "AvailabilityGroupListenerIPAddresses";
                case "ResumableIndex": return "ResumableIndexes";

                default:
                    return name + "s";
            }
        }


        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
        internal static AbstractCollectionBase GetChildCollection(SqlSmoObject parent,
            string childUrnSuffix, string categorystr, ServerVersion srvVer)
        {
            // this actually supposes that all child collection are named like this
            // For some classes we have to get the gramatically correct plural
            string childCollectionName = GetPluralName(childUrnSuffix, parent);
            object childCollection = null;

            // Permissions is an internal property, but we do not want to open the
            // door to calls to internals methods so we limit the use of
            // BindingFlags.NonPublic to Permissions
            if (childCollectionName != "Permissions")
            {
                try
                {
                    childCollection = parent.GetType().InvokeMember(childCollectionName,
                                BindingFlags.Default | BindingFlags.GetProperty |
                                BindingFlags.Instance | BindingFlags.Public,
                                null, parent, new object[] { }, SmoApplication.DefaultCulture);
                }
                catch (MissingMethodException)
                {
                    throw new ArgumentException(ExceptionTemplates.InvalidPathChildCollectionNotFound(childUrnSuffix, parent.GetType().Name));
                }
            }
            else
            {
                childCollection = parent.GetType().InvokeMember(childCollectionName,
                        BindingFlags.Default | BindingFlags.GetProperty |
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null, parent, new object[] { }, SmoApplication.DefaultCulture);
            }

            // this should always be true in SMO, a collection will never return null
            // but it can be empty
            Diagnostics.TraceHelper.Assert(null != childCollection, "null == childCollection");

            return (AbstractCollectionBase)childCollection;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="filterUrn"></param>
        /// <param name="categorystr"></param>
        /// <param name="srvVer"></param>
        /// <returns></returns>
        SqlSmoObject GetChildSingleton(SqlSmoObject parent,
            XPathExpression levelFilter, int filterIdx, ServerVersion srvVer)
        {
            string propName = null;
            object childObject = null;

            // Map from Urn filter name to the real System.Type name
            // Handle the case where we are asking about Server by itself (only one level node)
            int nodeCount = levelFilter.Length;
            Type childType = GetChildType(
                (nodeCount > filterIdx) ? levelFilter[filterIdx].Name : parent.GetType().Name,
                (filterIdx > 0) ? levelFilter[filterIdx - 1].Name : parent.GetType().Name);

            // Special case Server itself, if we have the top-level query node to process
            if (childType == typeof(Server))
            {
                return parent;
            }

            // Do we already know what parent property points to the singleton instance?
            if (!s_SingletonTypeToProperty.TryGetValue(childType, out propName))
            {
                SfcMetadataDiscovery metadata = new SfcMetadataDiscovery(parent.GetType());
                foreach (SfcMetadataRelation relation in metadata.Relations)
                {
                    if (relation.Relationship == SfcRelationship.ChildObject ||
                        relation.Relationship == SfcRelationship.Object)
                    {
                        if (childType == relation.Type)
                        {
                            // Found it
                            propName = relation.PropertyName;
                            break;
                        }
                    }
                }

                // Cache this result for the next time we need to lookup this type.
                // Propname will be null if we know it isn't possible to use this type.
                lock (((ICollection)s_SingletonTypeToProperty).SyncRoot)
                {
                    s_SingletonTypeToProperty[childType] = propName;
                }
            }

            if (propName == null)
            {
                throw new ArgumentException(ExceptionTemplates.InvalidPathChildSingletonNotFound(childType.Name, parent.GetType().Name));
            }

            try
            {
                childObject = parent.GetType().InvokeMember(propName,
                            BindingFlags.Default | BindingFlags.GetProperty |
                            BindingFlags.Instance | BindingFlags.Public,
                            null, parent, new object[] { }, SmoApplication.DefaultCulture);
            }
            catch (MissingMethodException)
            {
                throw new ArgumentException(ExceptionTemplates.InvalidPathChildSingletonNotFound(childType.Name, parent.GetType().Name));
            }

            return (SqlSmoObject)childObject;
        }

        // compares the object with the current row
        private int CompareObjectToRow(SqlSmoObject currObj, System.Data.IDataReader currentRow,
            int colIdx, bool isOrderedByID,
            XPathExpression xpath, int xpathIdx)
        {
            // columns and parameters are ordered by ID
            // Q: Why are we comparing by ID, and not by name?
            // A: because comparing by name would have yielded a different order, and
            // we need the ordering given by the ID, because this is how the collection is sorted
            if (isOrderedByID)
            {
                string idRowName;
                // figure out the name of the column that has the ID property in the
                // result table. If we are on the last level of the Urn, this is ID,
                // otherwise enumerator appends the level name to it
                if (xpathIdx == xpath.Length - 1)
                {
                    idRowName = "ID";
                }
                else
                {
                    idRowName = xpath[xpathIdx].Name + "_ID";
                }

                Int32 objectID = Convert.ToInt32(currObj.Properties["ID"].Value, SmoApplication.DefaultCulture);
                int idRowIdx = -1;
                try
                {
                    idRowIdx = currentRow.GetOrdinal(idRowName);
                }
                catch (IndexOutOfRangeException)
                {
                    Diagnostics.TraceHelper.Assert(false, "currentRow.GetOrdinal(" + idRowName + ") failed");
                }
                Int32 rowObjectID = Convert.ToInt32(currentRow.GetValue(idRowIdx), SmoApplication.DefaultCulture);
                return (objectID - rowObjectID);
            }

            ObjectKeyBase rowKey = ObjectKeyBase.CreateKeyOffset(currObj.GetType(), currentRow, colIdx);
            return currObj.KeyComparer.Compare(currObj.key, rowKey);
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
        private bool CompareRows(System.Data.IDataReader reader, object[] parentRow, int columnStartIdx, int columnStopIdx)
        {
            Diagnostics.TraceHelper.Assert(null != reader, "reader == null");
            if (reader.IsClosed)
            {
                return false;
            }

            Diagnostics.TraceHelper.Assert(null != parentRow, "parentRow == null");
            Diagnostics.TraceHelper.Assert(columnStartIdx >= 0, "columnStartIdx < 0");
            Diagnostics.TraceHelper.Assert(columnStopIdx < reader.FieldCount, "columnStopIdx >= reader.FieldCount");


            for (int i = columnStartIdx; i < columnStopIdx; i++)
            {
                if (!reader.GetValue(i).Equals(parentRow[i]))
                {
                    return false;
            }
            }
            return true;
        }

        // Get the infrastructure fields for a given Type, otherwise the queries are way too inefficient
        // and to prevent unwanted reader overlaps from lazy property access.
        // This always returns a string array, although it may be empty if there are no real infrastructure fields.
        internal static string[] GetQueryTypeInfrastructureFields(Type t)
        {
            switch (t.Name)
            {
                // server.Information.Edition should be cached already
                // since all Object Queries do this as a dummy step in InitQueryUrns().
                case "Information":
                    return new string[] { "Edition" };

                case "Database":
                    return new string[] { "CompatibilityLevel", "Collation" };

                case "StoredProcedure":
                case "UserDefinedFunction":
                case "Trigger":
                case "DdlTrigger":
                case "DatabaseDdlTrigger":
                    return new string[] { "ImplementationType" };

                default:
                    return new string[] { };
            }
        }

        // in general, we want our results to come back sorted, and the ordering
        // depends on the object's type. The general idea here is that the objects
        // are returned sorted in the same order they would have in the collection
        private static OrderBy[] GetOrderByList(Type objType)
        {
            // objects that have Schema will be returned ordered by schema and
            // by Name
            if (objType.IsSubclassOf(typeof(ScriptSchemaObjectBase)))
            {
                return new OrderBy[] {     new OrderBy("Schema", OrderBy.Direction.Asc),
                                          new OrderBy("Name", OrderBy.Direction.Asc) };
            }
            else if (objType.Equals(typeof(NumberedStoredProcedure)))
            {
                return new OrderBy[] { new OrderBy("Number", OrderBy.Direction.Asc) };
            }
            // Columns and Parameters should be ordered by ID
            else if (IsOrderedByID(objType) || objType.IsSubclassOf(typeof(MessageObjectBase)))
            {
                return new OrderBy[] { new OrderBy("ID", OrderBy.Direction.Asc) };
            }
            // all other objects ordered by name
            else if (objType.Equals(typeof(FullTextIndex)))
            {
                return new OrderBy[] { };
            }
            else if (objType.IsSubclassOf(typeof(SoapMethodObject)))
            {
                return new OrderBy[] {  new OrderBy("Namespace", OrderBy.Direction.Asc),
                                          new OrderBy("Name", OrderBy.Direction.Asc) };
            }
            else if (objType.Equals(typeof(PhysicalPartition)))
            {
                return new OrderBy[] { new OrderBy("PartitionNumber", OrderBy.Direction.Asc) };
            }
            else if (objType.Equals(typeof(DatabaseReplicaState)))
            {
                return new OrderBy[] { new OrderBy("AvailabilityReplicaServerName", OrderBy.Direction.Asc),
                                       new OrderBy("AvailabilityDatabaseName", OrderBy.Direction.Asc)};
            }
            else if (objType.Equals(typeof(AvailabilityGroupListenerIPAddress)))
            {
                return new OrderBy[] { new OrderBy("IPAddress", OrderBy.Direction.Asc),
                                       new OrderBy("SubnetMask", OrderBy.Direction.Asc),
                                       new OrderBy("SubnetIP", OrderBy.Direction.Asc)};
            }
            else if(objType.Equals(typeof(SecurityPredicate)))
            {
                return new OrderBy[] { new OrderBy("SecurityPredicateID", OrderBy.Direction.Asc) };
            }
            else if (objType.Equals(typeof(ColumnEncryptionKeyValue)))
            {
                return new OrderBy[] { new OrderBy("ColumnMasterKeyID", OrderBy.Direction.Asc) };
            }
            else
            {
                return new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
        }
        }

        internal static bool IsOrderedByID(Type t)
        {
            return (t.IsSubclassOf(typeof(ParameterBase)) ||
                t.Equals(typeof(Column)) ||
                t.Equals(typeof(ForeignKeyColumn)) ||
                t.Equals(typeof(OrderColumn)) ||
                t.Equals(typeof(IndexedColumn)) ||
                t.Equals(typeof(IndexedXmlPath)) ||
                t.Equals(typeof(StatisticColumn)) ||
                t.Equals(typeof(JobStep)) ||
                t.Equals(typeof(PartitionFunctionParameter)) ||
                t.Equals(typeof(PartitionSchemeParameter)));
        }


        // Get the key fields for a given Type as they would be obtained left-to-right in a Urn query.
        // This always returns a string array, although it may be empty if there are no real key fields.
        internal static string[] GetQueryTypeKeyFields(Type t)
        {
            string[] keys;

            // Do we already know what key fields this type has?
            if (!s_TypeToKeyFields.TryGetValue(t, out keys))
            {
                // TODO: FIX_IN_KATMAI Replace with SfcMetadata impl
                if (IsOrderedByID(t))
                {
                    // Objects which use an ID as the major key, and also have a Name as well
                    keys = new string[] { "ID", "Name" };
                }
                else if (t.IsSubclassOf(typeof(NamedSmoObject)) ||
                         t == typeof(Server) || t == typeof(FullTextIndex) ||
                         t == typeof(DatabaseReplicaState) || t == typeof(AvailabilityGroupListenerIPAddress))
                {
                    // Objects that really have a key, or Server
                    StringCollection fields = ObjectKeyBase.GetFieldNames(t);
                    keys = new String[fields.Count];
                    fields.CopyTo(keys, 0);
                }
                else
                {
                    // Some singleton objects have no keys an can contain other objects,
                    // so they can appear at non-leaf locations sometimes.
                    // Example: Server/Setting/OleDbProviderSetting.
                    //
                    // If you leave this null, you will get all the Type_ -prefixed default fields for this level back,
                    // but since we don't use them no point (yet).
                    keys = new string[] { };
                }

                // Cache this result for the next time we need to lookup this type.
                // Propname will be null if we know it isn't possible to use this type.
                lock (((ICollection)s_TypeToKeyFields).SyncRoot)
                {
                    s_TypeToKeyFields[t] = keys;
                }
            }
            return keys;
        }


        // Get the key field count for a given Type as they would be obtained left-to-right in a Urn query.
        internal static int GetQueryTypeKeyFieldsCount(Type t)
        {
            return GetQueryTypeKeyFields(t).Length;
        }


        // TODO: FIX_IN_KATMAI: This function is messed up beyond repair. It needs to be completely rewritten
        // using SfcMetadata

        // this function figures out the child type looking at the urn name and the parent name
        // we need this function because names from urn do not fit the object names all the time
        public static Type GetChildType(string objectName, string parentName)
        {
            // intern the strings for faster comparison
            if (objectName == "Server")
            {
                return typeof(Server);
            }

            // this is the SMO type name
            string realTypeName = string.Empty;

            // the general rule is that the SMO object name coincides with the enumerator
            // name, but for some objects this is not true, alas, so this is a list of exceptions
            switch (objectName)
            {
                // leave this the first statement, so that we can call the function
                // with null as parent name
                case "Server":
                    realTypeName = "Server";
                    break;

                case "Role":
                    if (parentName == "Server")
                    {
                        realTypeName = "ServerRole";
                    }
                    else
                    {
                        realTypeName = "DatabaseRole";
                    }

                    break;

                case "Default":
                    if (parentName == "Column")
                    {
                        realTypeName = "DefaultConstraint";
                    }
                    else
                    {
                        realTypeName = "Default";
                    }

                    break;

                case "File":
                    realTypeName = "DataFile";
                    break;

                case "Column":
                    if (parentName == "ForeignKey")
                    {
                        realTypeName = "ForeignKeyColumn";
                    }
                    else if (parentName == "Statistic")
                    {
                        realTypeName = "StatisticColumn";
                    }
                    else
                    {
                        realTypeName = "Column";
                    }

                    break;

                case "Mail":
                    realTypeName = "SqlMail";
                    break;

                case "Schedule":
                    realTypeName = "JobSchedule";
                    break;

                case "Step":
                    realTypeName = "JobStep";
                    break;

                case "Login":
                    if (parentName == "LinkedServer")
                    {
                        realTypeName = "LinkedServerLogin";
                    }
                    else
                    {
                        realTypeName = "Login";
                    }

                    break;

                case "Param":
                    // The "parentName" itself has to be translated from Urn-speak to Type-speak first
                    if (parentName == "Numbered")
                    {
                        realTypeName = "NumberedStoredProcedureParameter";
                    }
                    else
                    {
                        realTypeName = parentName + "Parameter";
                    }

                    break;

                case "Numbered":
                    realTypeName = "NumberedStoredProcedure";
                    break;

                case "Setting":
                    realTypeName = "Settings";
                    break;

                case "Option":
                    realTypeName = "DatabaseOptions";
                    break;

                case "Method":
                    realTypeName = "SoapPayloadMethod";
                    break;

                case "Soap":
                    realTypeName = "SoapPayload";
                    break;

                case "OleDbProviderSetting":
                    realTypeName = "OleDbProviderSettings";
                    break;
                case "DdlTrigger":
                    if (parentName == "Server")
                    {
                        realTypeName = "ServerDdlTrigger";
                    }
                    else
                    {
                        realTypeName = "DatabaseDdlTrigger";
                    }

                    break;
                case "Permission":
                    realTypeName = "UserPermission";
                    break;

                case "UserOption":
                    realTypeName = "UserOptions";
                    break;

                case "DatabaseMirroring":
                    realTypeName = "DatabaseMirroringPayload";
                    break;

                case "Http":
                    realTypeName = "HttpProtocol";
                    break;

                case "Tcp":
                    realTypeName = "TcpProtocol";
                    break;

                case "MasterKey":
                    if (parentName == "Server")
                    {
                        realTypeName = "ServiceMasterKey";
                    }
                    else
                    {
                        realTypeName = "MasterKey";
                    }

                    break;

                default:
                    realTypeName = objectName;
                    break;
            }

            // when we call GetType() we prefix with the namespace
            // to avoid name clashes

            // the type could be loaded from different namespaces, so we have to attempt
            // to load it multiple times
            Type retval;
            Assembly smoAssembly = typeof(Server).GetAssembly();
            string typeName;
            switch (objectName)
            {
                case "RegisteredServer":
                case "ServerGroup":
                    // The old SMO RegisteredServers now resides in the separate smoextended dll
                    typeName = "Microsoft.SqlServer.Management.Smo.RegisteredServers." + realTypeName +
                        ",Microsoft.SqlServer.SmoExtended";
                    retval = smoAssembly.GetType(typeName, true);
                    break;

                default:
                    typeName = "Microsoft.SqlServer.Management.Smo." + realTypeName;
                    retval = smoAssembly.GetType(typeName, false);
                    if (retval == null)
                    {
                        retval = smoAssembly.GetType("Microsoft.SqlServer.Management.Smo.Agent." + realTypeName, false);
                        if (retval == null)
                        {
                            retval = smoAssembly.GetType("Microsoft.SqlServer.Management.Smo.Mail." + realTypeName, false);
                            if (retval == null)
                            {
                                retval = smoAssembly.GetType("Microsoft.SqlServer.Management.Smo.Broker." + realTypeName, false);
                            }
                        }
                    }
                    break;
            }

            return retval;
        }

        /// <summary>
        ///  this will traverse up the tree for every hit for each instance of Smo Objects
        //   check if this can turn out to be a performance hit, by any chance
        /// </summary>
        public virtual ExecutionManager ExecutionManager
        {
            get
            {
                SqlSmoObject parent = null;

                if (null != this.parentColl)
                {
                    parent = this.parentColl.ParentInstance;
                }
                else if (null != singletonParent)
                {
                    parent = singletonParent;
                }

                //if it is not the Server it must have a parent
                // And Server should override this method to return proper ExecutionManager
                Diagnostics.TraceHelper.Assert(null != parent, "parent == null");

                return parent.ExecutionManager;
            }

        }

        #region Throw Method Helpers

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is above 8.0 (SQL 2000)
        /// </summary>
        protected void ThrowIfAboveVersion80(string exceptionMessage = null)
        {
            if (ServerVersion.Major > 8)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyBelow90 : exceptionMessage).SetHelpContext("SupportedOnlyBelow90");
            }
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is below 8.0 (SQL 2000)
        /// </summary>
        protected void ThrowIfBelowVersion80(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 8)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn80 : exceptionMessage).SetHelpContext("SupportedOnlyOn80");
            }
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is below 9.0 (SQL 2005)
        /// </summary>
        protected void ThrowIfBelowVersion90(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 9)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn90 : exceptionMessage).SetHelpContext("SupportedOnlyOn90");
            }
        }

        /// <summary>
        /// Throws an UnsupportedVersionException if either the source or destination server is below 9.0 (SQL 2005)
        /// </summary>
        /// <param name="targetVersion"></param>
        internal void ThrowIfSourceOrDestBelowVersion90(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            //Source
            ThrowIfBelowVersion90(exceptionMessage);

            //Dest
            ThrowIfBelowVersion90(targetVersion, exceptionMessage);
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is above 10.0 (SQL 2008)
        /// </summary>
        protected void ThrowIfAboveVersion100(string exceptionMessage = null)
        {
            if (ServerVersion.Major > 10)
            {
                throw new UnsupportedFeatureException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyBelow110 : exceptionMessage).SetHelpContext("SupportedOnlyBelow110");
        }
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is below 10.0 (SQL 2008)
        /// </summary>
        protected void ThrowIfBelowVersion100(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 10)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn100 : exceptionMessage).SetHelpContext("SupportedOnlyOn100");
            }
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is below 16.0
        /// </summary>
        protected void ThrowIfBelowVersion160(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 16)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn160 : exceptionMessage).SetHelpContext("SupportedOnlyOn160");
            }
        }

        /// <summary>
        /// Throws an UnsupportedVersionException if either the source or destination server is below 10.0 (SQL 2008)
        /// </summary>
        /// <param name="targetVersion"></param>
        internal void ThrowIfSourceOrDestBelowVersion100(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            //Source
            ThrowIfBelowVersion100(exceptionMessage);

            //Dest
            ThrowIfBelowVersion100(targetVersion, exceptionMessage);
        }

        /// <summary>
        /// Throws an UnsupportedVersionException if either the source or destination server is below 11.0 (SQL 2012)
        /// </summary>
        protected void ThrowIfBelowVersion110(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 11)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn110 : exceptionMessage).SetHelpContext("SupportedOnlyOn110");
            }
        }

        /// <summary>
        /// Throws an exception with text saying the specified property is not supported
        /// if the ServerVersion major version for this object is below 11.0 (SQL 2012)
        /// </summary>
        protected void ThrowIfBelowVersion110Prop(string propertyName)
        {
            if (ServerVersion.Major < 11)
            {
                throw new UnknownPropertyException(propertyName, ExceptionTemplates.NotSupportedForVersionEarlierThan110);
            }
        }

        /// <summary>
        /// Throws an UnsupportedVersionException if either the source or destination server is below 11.0 (SQL 2012)
        /// </summary>
        /// <param name="targetVersion"></param>
        internal void ThrowIfSourceOrDestBelowVersion110(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            //Source
            ThrowIfBelowVersion110(exceptionMessage);

            //Dest
            ThrowIfBelowVersion110(targetVersion, exceptionMessage);
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is below 12.0 (SQL 2014)
        /// </summary>
        protected void ThrowIfBelowVersion120(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 12)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn120 : exceptionMessage).SetHelpContext("SupportedOnlyOn120");
            }
        }

        /// <summary>
        /// Throws an exception with text saying the specified property is not supported
        /// if the ServerVersion major version for this object is below 12.0 (SQL 2014)
        /// </summary>
        protected void ThrowIfBelowVersion120Prop(string propertyName)
        {
            if (ServerVersion.Major < 12)
            {
                throw new UnknownPropertyException(propertyName, ExceptionTemplates.NotSupportedForVersionEarlierThan120);
            }
        }

        /// <summary>
        /// Throws an UnsupportedVersionException if either the source or destination server is below 12.0 (SQL 2014)
        /// </summary>
        /// <param name="targetVersion"></param>
        internal void ThrowIfSourceOrDestBelowVersion120(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            //Source
            ThrowIfBelowVersion120(exceptionMessage);

            //Dest
            ThrowIfBelowVersion120(targetVersion, exceptionMessage);
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is below 13.0 (SQL 2016)
        /// </summary>
        protected void ThrowIfBelowVersion130(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 13)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn130 : exceptionMessage).SetHelpContext("SupportedOnlyOn130");
            }
        }

        /// <summary>
        /// Throws an exception with text saying the specified property is not supported
        /// if the ServerVersion major version for this object is below 13.0 (SQL 2016)
        /// </summary>
        protected void ThrowIfBelowVersion130Prop(string propertyName)
        {
            if (ServerVersion.Major < 13)
            {
                throw new UnknownPropertyException(propertyName, ExceptionTemplates.NotSupportedForVersionEarlierThan130);
            }
        }

        /// <summary>
        /// Throws an exception if the ServerVersion major version for this object is below 14.0 (SQL 2017)
        /// </summary>
        protected void ThrowIfBelowVersion140(string exceptionMessage = null)
        {
            if (ServerVersion.Major < 14)
            {
                throw new UnsupportedVersionException(string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.SupportedOnlyOn140 : exceptionMessage).SetHelpContext("SupportedOnlyOn140");
            }
        }

        /// <summary>
        /// Throws an exception with text saying the specified property is not supported
        /// if the ServerVersion major version for this object is below 14.0 (SQL 2017)
        /// </summary>
        protected void ThrowIfBelowVersion140Prop(string propertyName)
        {
            if (ServerVersion.Major < 14)
            {
                throw new UnknownPropertyException(propertyName, ExceptionTemplates.NotSupportedForVersionEarlierThan140);
            }
        }

        /// <summary>
        /// Throws an UnsupportedVersionException if either the source or destination server is below 13.0 (SQL 2016)
        /// </summary>
        /// <param name="targetVersion"></param>
        internal void ThrowIfSourceOrDestBelowVersion130(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            //Source
            ThrowIfBelowVersion130(exceptionMessage);

            //Dest
            ThrowIfBelowVersion130(targetVersion, exceptionMessage);
        }

        /// <summary>
        /// Throws an exception if the SKU of the server is Express
        /// </summary>
        internal void ThrowIfExpressSku(string uft)
        {
            if (IsExpressSku())
            {
                throw new UnsupportedFeatureException(ExceptionTemplates.UnsupportedFeature(uft));
            }
        }

        /// <summary>
        /// Throws an exception saying that the specified property is unsupported
        /// if the DatabaseEngineType of this object is SqlAzureDatabase
        /// </summary>
        internal void ThrowIfCloudProp(string propertyName)
        {
            if (DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType)
            {
                throw new UnknownPropertyException(propertyName, ExceptionTemplates.PropertyNotSupportedOnCloud(propertyName));
            }
        }

        /// <summary>
        /// Throws an exception if the DatabaseEngineType of this object is SqlAzureDatabase
        /// </summary>
        internal void ThrowIfCloud(string exceptionMessage = null)
        {
            if (DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType)
            {
                throw new UnsupportedFeatureException(string.IsNullOrEmpty(exceptionMessage)
                    ? ExceptionTemplates.NotSupportedOnCloud : exceptionMessage);
            }
        }

        /// <summary>
        /// Checks and throws an UnsupportedFeatureException if the DatabaseEngineType is SqlAzureDatabase and
        /// the major version is below 12 (Sterling).
        /// </summary>
        internal void ThrowIfCloudAndVersionBelow12(string propertyName)
        {
            if (DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType &&
                ServerVersion.Major < 12)
            {
                throw new UnsupportedFeatureException(ExceptionTemplates.PropertyNotSupportedForCloudVersion(propertyName, ServerVersion.ToString()));
            }
        }

        #endregion Throw Method Helpers

        /// <summary>
        /// Returns whether the server containing this object is an Express SKU
        /// </summary>
        /// <returns></returns>
        public bool IsExpressSku()
        {
            if (this.IsDesignMode)
            {
                // We're not connected to a SKU in design mode: return false
                return false;
            }
            Server svr = this.GetServerObject();
            return (svr.Information.ServerVersion.Major >= 9 &&
                    svr.Information.Edition.StartsWith("Express Edition", StringComparison.OrdinalIgnoreCase));
        }

       /// <summary>
       /// Verifies if the current object is in the design mode
       /// </summary>
       internal bool IsDesignMode
       {
           get
           {
               bool isDesignMode = false;

               ISfcSupportsDesignMode supportsDesignMode = this as ISfcSupportsDesignMode;
               if (null != supportsDesignMode)
               {
                   ISfcHasConnection srv = TryGetServerObject() as ISfcHasConnection;
                   if (null != srv)
                   {
                       isDesignMode = (srv.ConnectionContext.Mode == SfcConnectionContextMode.Offline);
                   }
               }

               return isDesignMode;
           }
       }

       /// <summary>
       /// Checks if the current object can be used in design mode
       /// </summary>
       internal bool SupportsDesignMode
       {
           get
           {
               return ((this as ISfcSupportsDesignMode) != null);
           }
       }


        /// <summary>
        /// Checks if the target engine type is Cloud, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfCloud(DatabaseEngineType targetEngineType)
        {
            ThrowIfCloud(targetEngineType, ExceptionTemplates.NotSupportedOnCloud);
        }


        /// <summary>
        /// Checks if the target engine type is cloud, and if so
        /// throws an exception based the exceptionMessage
        /// </summary>
        /// <param name="targetEngineType"></param>
        /// <param name="exceptionMessage"></param>
        internal static void ThrowIfCloud(DatabaseEngineType targetDatabaseEngineType, string exceptionMessage)
        {
            if (targetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                throw new UnsupportedEngineTypeException(exceptionMessage);
            }
        }

        /// <summary>
        /// Checks if the target engine type is not cloud, and if so
        /// throws an exception based the exceptionMessage
        /// </summary>
        /// <param name="targetEngineType">The target database engine type.</param>
        /// <param name="exceptionMessage">Message to include in the exception.</param>
        internal static void ThrowIfNotCloud(DatabaseEngineType targetDatabaseEngineType, string exceptionMessage)
        {
            if (targetDatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
            {
                throw new UnsupportedEngineTypeException(exceptionMessage);
            }
        }

        /// <summary>
        /// Checks if the target engine edition is not SQL DW, and if so
        /// throws an exception based the exceptionMessage
        /// </summary>
        /// <param name="targetEngineType">The target database engine type.</param>
        /// <param name="exceptionMessage">Message to include in the exception.</param>
        internal static void ThrowIfNotSqlDw(DatabaseEngineEdition targetDatabaseEngineEdition, string exceptionMessage)
        {
            if (targetDatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse)
            {
                throw new UnsupportedEngineEditionException(exceptionMessage);
            }
        }

        /// <summary>
        /// Checks if the target engine type is cloud and target version is under 12.0 and if so
        /// throw an exception indicating that this is not supported
        /// </summary>
        /// <param name="targetDatabaseEngineType"></param>
        /// <param name="targetVersion"></param>
        /// <param name="exceptionMessage"></param>
        internal static void ThrowIfCloudAndBelowVersion120(DatabaseEngineType targetDatabaseEngineType,
            SqlServerVersion targetVersion, string exceptionMessage)
        {
            if (targetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version120, exceptionMessage);
            }
        }


        /// <summary>
        /// Checks if the target version is smaller than 9.0, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion90(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version90, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the target version is smaller than 10.0, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion100(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version100, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the target version is smaller than 10.5, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion105(SqlServerVersion targetVersion, string exceptionMessage)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version105, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the target version is smaller than 17.0, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion170(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version170, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the target version is smaller than 11.0, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion110(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version110, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the target version is smaller than 12.0, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion120(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version120, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the target version is smaller than 13.0, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion130(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version130, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the target version is smaller than 8.0, and if so
        /// throw an exception indicating that this is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfBelowVersion80(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version80, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        /// <summary>
        /// Checks if the targetVersion is below the specified upperLimit version and throws an <see cref="UnsupportedVersionException"/> if it is.
        /// </summary>
        /// <param name="targetVersion"></param>
        /// <param name="upperLimit"></param>
        /// <param name="exceptionText"></param>
        private static void ThrowIfBelowVersionLimit(SqlServerVersion targetVersion, SqlServerVersion upperLimit, string exceptionText)
        {
            if (targetVersion < upperLimit)
            {
                throw new UnsupportedVersionException(exceptionText).SetHelpContext("UnsupportedVersionException");
            }
        }

        /// <summary>
        /// Returns the compatibility level of the object. If the object is
        /// has a database context then we return the compatibility level
        /// of the database, otherwise the translated server version.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        internal CompatibilityLevel GetCompatibilityLevel()
        {
            Server srv = GetServerObject();
            Diagnostics.TraceHelper.Assert(null != srv, "srv == null");

            string dbName = GetDBName();
            Diagnostics.TraceHelper.Assert(null != dbName, "dbName == null");

            if (dbName.Length != 0)
            {
                // attempt to obtain the Database object
                // we need to do this to avoid using databases that haven't been
                // created so they are not in the object tree.
                Database db = srv.Databases[dbName];
                if (null != db)
                {
                    // if we have the database let's try to get the compatibility level
                    // note that the default is CompatibilityLevel.Version60 which makes
                    // us go to the server for compat level if the property is not set
                    return db.GetPropValueOptional("CompatibilityLevel",
                                            GetCompatibilityLevel(this.ServerVersion));
                }
            }

            return GetCompatibilityLevel(this.ServerVersion);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ver"></param>
        /// <returns></returns>
        internal static CompatibilityLevel GetCompatibilityLevel(ServerVersion ver)
        {
            switch (ver.Major)
            {
                case 8:
                    return CompatibilityLevel.Version80;
                case 9:
                    return CompatibilityLevel.Version90;
                case 10:
                    return CompatibilityLevel.Version100;
                case 11:
                    return CompatibilityLevel.Version110;
                case 12:
                    return CompatibilityLevel.Version120;
                case 13:
                    return CompatibilityLevel.Version130;
                case 14:
                    return CompatibilityLevel.Version140;
                case 15:
                    return CompatibilityLevel.Version150;
                //Forward Compatibility: An older version SSMS/Smo connecting to a future version sql server database engine.
                //That is why if the ver(ServerVersion) is unknown, we need to set it according to the latest database engine available,
                //so that all Latest-Version-Supported-Features in the Tools work seamlessly for the unknown future version database engines too.
                default:
                    return CompatibilityLevel.Version160;
            }
        }

        /// <summary>
        /// Throws an exception if the CompatabilityLevel for this object is below 130
        /// </summary>
        internal void ThrowIfCompatibilityLevelBelow130()
        {
            ThrowIfCompatibilityLevelBelowLimit(GetCompatibilityLevel(), CompatibilityLevel.Version130);
        }

        /// <summary>
        /// Throws an exception if the CompatabilityLevel for this object is below 120
        /// </summary>
        internal void ThrowIfCompatibilityLevelBelow120()
        {
            ThrowIfCompatibilityLevelBelowLimit(GetCompatibilityLevel(), CompatibilityLevel.Version120);
        }

        /// <summary>
        /// Throws an exception if the CompatabilityLevel for this object is below 100
        /// </summary>
        internal void ThrowIfCompatibilityLevelBelow100()
        {
            ThrowIfCompatibilityLevelBelowLimit(GetCompatibilityLevel(), CompatibilityLevel.Version100);
        }

        /// <summary>
        /// Throws an exception if the CompatabilityLevel for this object is below 90
        /// </summary>
        internal void ThrowIfCompatibilityLevelBelow90()
        {
            ThrowIfCompatibilityLevelBelowLimit(GetCompatibilityLevel(), CompatibilityLevel.Version90);
        }

        /// <summary>
        /// Throws an exception if the CompatabilityLevel for this object is below 80
        /// </summary>
        internal void ThrowIfCompatibilityLevelBelow80()
        {
            ThrowIfCompatibilityLevelBelowLimit(GetCompatibilityLevel(), CompatibilityLevel.Version80);
        }

        /// <summary>
        /// Checks if the targetCompatLevel is below the specified upperLimit and throws an <see cref="UnsupportedVersionException"/> if it is
        /// </summary>
        /// <param name="targetCompatLevel"></param>
        /// <param name="upperLimit"></param>
        private static void ThrowIfCompatibilityLevelBelowLimit(CompatibilityLevel targetCompatLevel, CompatibilityLevel upperLimit)
        {
            if (targetCompatLevel < upperLimit)
            {
                throw new UnsupportedCompatLevelException(ExceptionTemplates.UnsupportedCompatLevelException((long)targetCompatLevel, (long)upperLimit)).SetHelpContext("UnsupportedCompatLevelException");
            }
        }

        /// <summary>
        /// Checks if the target version is smaller than 13.0, and if so
        /// throw an exception indicating that create or alter is not supported.
        /// </summary>
        /// <param name="targetVersion"></param>
        internal static void ThrowIfCreateOrAlterUnsupported(SqlServerVersion targetVersion, string exceptionMessage = null)
        {
            ThrowIfBelowVersionLimit(targetVersion, SqlServerVersion.Version130, string.IsNullOrEmpty(exceptionMessage) ? ExceptionTemplates.UnsupportedVersionException : exceptionMessage);
        }

        internal static string GetSqlServerName(ScriptingPreferences sp)
        {
            if (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return LocalizableResources.EngineCloud;
            } else if (sp.TargetDatabaseEngineType == DatabaseEngineType.Standalone && sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {
                return LocalizableResources.EngineCloudMI;
            }
            return TypeConverters.SqlServerVersionTypeConverter.ConvertToString(sp.TargetServerVersion);
        }

        internal static string GetSqlServerName(SqlSmoObject srv)
        {
            if (srv.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return LocalizableResources.EngineCloud;
            }

            switch (srv.ServerVersion.Major)
            {
                case 7:
                    return LocalizableResources.ServerSphinx;
                case 8:
                    return LocalizableResources.ServerShiloh;
                case 9:
                    return LocalizableResources.ServerYukon;
                case 10:
                    if (srv.ServerVersion.Minor == 0)
                    {
                        return LocalizableResources.ServerKatmai;
                    }
                    else if (srv.ServerVersion.Minor == 50)
                    {
                        return LocalizableResources.ServerKilimanjaro;
                    }
                    else
                    {
                        return string.Empty;
                    }
                case 11:
                    return LocalizableResources.ServerDenali;
                case 12:
                    return LocalizableResources.ServerSQL14;
                case 13:
                    return LocalizableResources.ServerSQL15;
                case 14:
                    return LocalizableResources.ServerSQL2017;
                case 15:
                    return LocalizableResources.ServerSQLv150;
                case 16:
                    return LocalizableResources.ServerSQLv160;
                case 17:
                    return LocalizableResources.ServerSQLv170;
                    // VBUMP
                default:
                    return string.Empty;
            }
        }


        //TODO: refactor this method outside SqlSmoObject to some common utils place
        /// <summary>
        ///    Check if at least one of Source or Destination Engine types is Cloud DB
        /// </summary>
        /// <param name="srcEngineType"></param>
        /// <param name="destEngineType"></param>
        /// <returns></returns>
        internal static bool IsCloudAtSrcOrDest(DatabaseEngineType srcEngineType, DatabaseEngineType destEngineType)
        {
            if (srcEngineType == DatabaseEngineType.SqlAzureDatabase || destEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return true;
            }

            return false;
        }

        internal static string GetDatabaseEngineName(ScriptingPreferences sp)
        {
            switch (sp.TargetDatabaseEngineType)
            {
                case DatabaseEngineType.SqlAzureDatabase:
                    return LocalizableResources.EngineCloud;
                case DatabaseEngineType.Standalone:
                    return LocalizableResources.EngineSingleton;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the sqlserver public name for the current connection, e.g: "SQL Server 2008".
        /// </summary>
        /// <returns></returns>
        public string GetSqlServerVersionName()
        {
            return GetSqlServerName(this);
        }

        //Validates versioning of the request and gets the enumerator Fragmentation substring
        //This is used by EnumFragmentation() in Table, View and Index
        protected string GetFragOptionString(FragmentationOption fragmentationOption)
        {
            string optStr = null;

            switch (fragmentationOption)
            {
                case FragmentationOption.Fast:
                    optStr = "FragmentationFast";
                    break;

                case FragmentationOption.Sampled:
                    if (ServerVersion.Major < 9)
                    {
                        throw new UnsupportedVersionException(ExceptionTemplates.InvalidOptionForVersion("EnumFragmentation", fragmentationOption.ToString(), GetSqlServerVersionName()));
                    }

                    optStr = "FragmentationSampled";
                    break;

                case FragmentationOption.Detailed:
                    if (ServerVersion.Major < 8)
                    {
                        throw new UnsupportedVersionException(ExceptionTemplates.InvalidOptionForVersion("EnumFragmentation", fragmentationOption.ToString(), GetSqlServerVersionName()));
                    }

                    optStr = "FragmentationDetailed";
                    break;

                default:
                    throw new InternalSmoErrorException(ExceptionTemplates.UnknownEnumeration("FragmentationOption"));
            }

            return optStr;
        }

        internal static string FormatSqlVariant(object sqlVariant)
        {
            if (sqlVariant == null || DBNull.Value.Equals(sqlVariant))
            {
                return "NULL";
            }

            // Try to keep the 'if' statements below in "most common first" order

            Type type = sqlVariant.GetType();

            if (type == typeof(Int32))
            {
                return ((Int32)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(byte))
            {
                return ByteArrayToString(new byte[] { (byte)sqlVariant });
            }
            if (type == typeof(decimal))
            {
                return ((decimal)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(string))
            {
                return string.Format(SmoApplication.DefaultCulture, "N'{0}'", SqlString((string)sqlVariant));
            }
            if (type == typeof(Int16))
            {
                return ((Int16)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(Int64))
            {
                return ((Int64)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(double))
            {
                return ((double)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(float))
            {
                return ((float)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(DateTime))
            {
                return string.Format(SmoApplication.DefaultCulture, "N'{0}'", SqlDateString((DateTime)sqlVariant, "yyyy-MM-ddTHH:mm:ss.fff"));
            }
            if (type == typeof(DateTimeOffset))
            {
                return string.Format(SmoApplication.DefaultCulture, "N'{0}'", SqlDateString((DateTimeOffset)sqlVariant));
            }
            if (type == typeof(SqlDateTime))
            {
                if (((SqlDateTime)sqlVariant).IsNull)
                {
                    return "NULL";
                }
                return string.Format(SmoApplication.DefaultCulture, "N'{0}'", SqlDateString(((SqlDateTime)sqlVariant).Value, "yyyy-MM-ddTHH:mm:ss.fff"));
            }
            if (type == typeof(byte[]))
            {
                return ByteArrayToString((byte[])sqlVariant);
            }
            if (type == typeof(SqlBinary))
            {
                if (((SqlBinary)sqlVariant).IsNull)
                {
                    return "NULL";
                }
                return ByteArrayToString(((SqlBinary)sqlVariant).Value);
            }
            if (type == typeof(System.Boolean))
            {
                return string.Format(SmoApplication.DefaultCulture, ((System.Boolean)sqlVariant) ? "1" : "0");
            }
            // formatting the GUID values by enclosing them in N ' '
            if (type == typeof(Guid))
            {
                return string.Format(SmoApplication.DefaultCulture, "{0}", MakeSqlString(sqlVariant.ToString()));
            }

            // This included all cases not caught above, such as unsigned integers etc

            return sqlVariant.ToString();

        }

        /// <summary>
        /// Converts a DateTime to a string in a format that is
        /// unambiguous and can be converted into a sql datetime in
        /// T-SQL code regardless of the server's language setting.
        /// </summary>
        /// <param name="date"></param>
        /// <returns>string</returns>
        public static string SqlDateString(DateTime date)
        {
            //
            // Here we return dates in the ISO 8601 format using the
            // "s" format string when calling DateTime.ToString. This
            // is described as style 126 as listed in Books Online for
            // the CONVERT function. This format is accepted by the
            // sql server regardless of your connection's LANGUAGE or
            // DATEFORMAT settings. This format is very similar to
            // style 120 which is ODBC Canonical, but this ISO format
            // adds the literal letter 'T' between the date and time
            // fields. There are no spaces. It turns out that the sql
            // server does interpret strings like '2001-01-02
            // 14:15:16' differently depending on your DATEFORMAT
            // settings, so if you do 'set DATEFORMAT dmy' then the
            // date string above will be February 1st, not January
            // 2nd. However the ISO format that adds the 'T' character
            // is unambiguous: '2001-01-02T14:15:16'.
            //
            // Note also that we don't return a datetime specifically,
            // but a string that may be cast to a datetime. The reason
            // for this is the string "CAST(N'<datestring>' as
            // datetime)" cannot be passed as to a stored
            // procedure. The value returned here is often used as a
            // value passed in a stored procedure param. The caller
            // can always wrap this return value in a cast if they
            // desire.
            //
            return SqlDateString(date, "s");
        }


        internal static string SqlDateString(DateTime date, string format)
        {
            return date.ToString(format, SmoApplication.DefaultCulture);
        }

        internal static string SqlDateString(DateTimeOffset date)
        {
            return date.ToString(SmoApplication.DefaultCulture);
        }

        private static string ByteArrayToString(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return "NULL";
            }

            //
            // build the string representation (hex) of this byte array
            //
            int length = bytes.Length;
            StringBuilder builder = new StringBuilder("0x", 2 * (length + 1));
            for (int i = 0; i < length; ++i)
            {
                // 2 hex digits for each byte
                builder.Append(bytes[i].ToString("X2", SmoApplication.DefaultCulture));
            }

            return builder.ToString();
        }

        internal void GenerateDataSpaceScript(StringBuilder parentScript, ScriptingPreferences sp)
        {
            if (sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse || this.ParentColl.ParentInstance is UserDefinedTableType)
            {
                return;
            }

            StringBuilder stmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string fileGroupName = String.Empty;

            // FileGroup is marked as expensive property from version 12.
            // GetPropValueOptional will query backend if the value is not retrieved
            // Azure SQL DB only supports the PRIMARY filegroup
            if (null != GetPropValueOptional("FileGroup"))
            {
                fileGroupName = GetPropValueOptional("FileGroup").ToString();
                if (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase && fileGroupName.Length > 0)
                {
                    fileGroupName = "PRIMARY";
                }
            }

            if (!IsSupportedProperty("PartitionScheme", sp))
            {
                // earlier than 9.0, we are looking only at FileGroup
                if (sp.Storage.FileGroup && fileGroupName.Length > 0)
                {
                    stmt.AppendFormat(SmoApplication.DefaultCulture, " ON [{0}]", SqlBraket(fileGroupName));
            }
            }
            else
            {
                // first check for mutually exclusive options
                string partitionSchemeName = String.Empty;
                if (null != Properties.Get("PartitionScheme").Value)
                {
                    partitionSchemeName = (string)Properties["PartitionScheme"].Value;
                }

                if (fileGroupName.Length > 0 && partitionSchemeName.Length > 0)
                {
                    throw new WrongPropertyValueException(
                        ExceptionTemplates.MutuallyExclusiveProperties("PartitionScheme",
                        "FileGroup"));
                }
                else if (sp.Storage.FileGroup && fileGroupName.Length > 0)
                {
                    stmt.AppendFormat(SmoApplication.DefaultCulture, " ON [{0}]", SqlBraket(fileGroupName));
                }
                else if (partitionSchemeName.Length > 0)
                {
                    if (((this is Table) && ((sp.Storage.PartitionSchemeInternal & PartitioningScheme.Table) == PartitioningScheme.Table)) ||
                        ((this is Index) && ((sp.Storage.PartitionSchemeInternal & PartitioningScheme.Index) == PartitioningScheme.Index)))
                    {
                        stmt.AppendFormat(SmoApplication.DefaultCulture, " ON [{0}]", SqlBraket(partitionSchemeName));
                        stmt.AppendFormat(SmoApplication.DefaultCulture, "(");
                        PartitionSchemeParameterCollection pspColl = null;
                        ColumnCollection columns = null;

                        if (this is Table)
                        {
                            pspColl = ((Table)this).PartitionSchemeParameters;
                            columns = ((Table)this).Columns;
                        }
                        else if (this is Index)
                        {
                            pspColl = ((Index)this).PartitionSchemeParameters;
                            columns = ((TableViewBase)((Index)this).Parent).Columns;
                        }

                        // make sure we have params
                        if (pspColl.Count == 0)
                        {
                            throw new FailedOperationException(ExceptionTemplates.NeedPSParams);
                        }

                        int pspCount = 0;
                        foreach (PartitionSchemeParameter psp in pspColl)
                        {
                            // we check to see if the specified column exists,
                            Column colBase = columns[psp.Name];
                            if (null == colBase)
                            {
                                // for views we only enforce checking if the view is not creating as it may not have the columns populated
                                if (!(this.ParentColl.ParentInstance.State == SqlSmoState.Creating && this.ParentColl.ParentInstance is View))
                                {
                                    // the column does not exist, so we need to abort this scripting
                                    throw new SmoException(ExceptionTemplates.ObjectRefsNonexCol(PartitionScheme.UrnSuffix, partitionSchemeName, this.ToString() + ".[" + SqlStringBraket(psp.Name) + "]"));
                                }
                            }

                            // if this column is going to be ignored for scripting skip the whole object
                            if (colBase.IgnoreForScripting)
                            {
                                // flag this object to be ignored for scripting and return from the function
                                this.IgnoreForScripting = true;
                                return;
                            }

                            if (0 < pspCount++)
                            {
                                stmt.Append(Globals.commaspace);
                            }

                            stmt.Append(colBase.FormatFullNameForScripting(sp));
                        }

                        // close the parameter list
                        stmt.AppendFormat(SmoApplication.DefaultCulture, ")");
                    }
                }
            }

            parentScript.Append(stmt.ToString());
        }

        internal void GenerateDataSpaceFileStreamScript(StringBuilder parentScript, ScriptingPreferences sp, bool alterTable)
        {
            if (!IsSupportedProperty("FileStreamFileGroup", sp))
            {
                return;
            }
            //FILESTREAM_ON option for table and clustered index

            StringBuilder stmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string fileGroupName = String.Empty;
            Property pFileStreamFileGroup = Properties.Get("FileStreamFileGroup");
            if (null != pFileStreamFileGroup.Value)
            {
                fileGroupName = (string)pFileStreamFileGroup.Value;
            }

            // first check for mutually exclusive options
            string partitionSchemeName = String.Empty;
            Property pFileStreamPartitionScheme = Properties.Get("FileStreamPartitionScheme");
            if (null != pFileStreamPartitionScheme.Value)
            {
                partitionSchemeName = (string)pFileStreamPartitionScheme.Value;
            }

            if (fileGroupName.Length > 0 && partitionSchemeName.Length > 0)
            {
                throw new WrongPropertyValueException(
                    ExceptionTemplates.MutuallyExclusiveProperties("FileStreamPartitionScheme",
                    "FileStreamFileGroup"));
            }
            else if (sp.Storage.FileStreamFileGroup && sp.Storage.FileStreamColumn && fileGroupName.Length > 0)
            {
                if (!alterTable)
                {
                    stmt.AppendFormat(SmoApplication.DefaultCulture, " FILESTREAM_ON [{0}]", SqlBraket(fileGroupName));
                }
                else
                {
                    stmt.AppendFormat(SmoApplication.DefaultCulture, " SET (FILESTREAM_ON = [{0}])", SqlBraket(fileGroupName));
                }
            }
            else if (sp.Storage.FileStreamColumn && partitionSchemeName.Length > 0)
            {
                if (((this is Table) && ((sp.Storage.PartitionSchemeInternal & PartitioningScheme.Table) == PartitioningScheme.Table)) ||
                    ((this is Index) && ((sp.Storage.PartitionSchemeInternal & PartitioningScheme.Index) == PartitioningScheme.Index)))
                {
                    if (!alterTable)
                    {
                        stmt.AppendFormat(SmoApplication.DefaultCulture, " FILESTREAM_ON [{0}]", SqlBraket(partitionSchemeName));
                    }
                    else
                    {
                        stmt.AppendFormat(SmoApplication.DefaultCulture, " SET (FILESTREAM_ON = [{0}])", SqlBraket(partitionSchemeName));
                    }
                }
            }

            parentScript.Append(stmt.ToString());
        }


        protected StringCollection ScriptImpl()
        {
            CheckObjectState(false);
            ScriptingPreferences sp = new ScriptingPreferences(this);
            sp.SfcChildren = false;
            return ScriptImpl(sp);
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the object. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        /// <param name="sp"></param>
        /// <returns>StringCollection object with the script for the object</returns>
        internal StringCollection ScriptImpl(ScriptingPreferences sp)
        {
            if (sp.IncludeScripts.Data)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScriptImpl(sp));
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the object. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        /// <param name="so"></param>
        /// <returns>StringCollection object with the script for the object</returns>
        protected StringCollection ScriptImpl(ScriptingOptions so)
        {
            if (null == so)
            {
                throw new ArgumentNullException("scriptingOptions");
            }

            if (so.ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            Scripter tmpScripter = new Scripter(this.GetServerObject());

            // if the user has not set the target server version, then we'll
            // set it to match the version of this object
            if (!so.GetScriptingPreferences().TargetVersionAndDatabaseEngineTypeDirty)
            {
                so.SetTargetServerInfo(this, false);
            }
            tmpScripter.Options = so;
            return tmpScripter.Script(this);
        }

        /// <summary>
        /// Returns an IEnumerable<string> object with the script for the object.
        /// </summary>
        /// <param name="so"></param>
        /// <returns>an IEnumerable<string> object with the script for the object.</returns>
        internal IEnumerable<string> EnumScriptImpl(ScriptingPreferences sp)
        {
            CheckObjectState(false);
            try
            {
                return EnumScriptImplWorker(sp);
            }
            catch (PropertyCannotBeRetrievedException e) //if we couldn't retrieve a property
            {
                //return custom error message
                FailedOperationException foe = new FailedOperationException(
                    ExceptionTemplates.FailedOperationExceptionTextScript(SqlSmoObject.GetTypeName(this.GetType().Name), this.ToString()), e);
                //add additional properties
                foe.Operation = ExceptionTemplates.Script;
                foe.FailedObject = this;
                throw foe;
            }
            catch (Exception e) //else a general error message
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Script, this, e);
            }
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        /// <returns>StringCollection object with the script for objects</returns>
        internal StringCollection ScriptImplWorker(ScriptingPreferences sp)
        {
            if (sp.IncludeScripts.Data)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScriptImplWorker(sp));
        }


        /// <summary>
        /// Returns an IEnumerable<string> object with the script for the objects.
        /// </summary>
        /// <returns>IEnumerable<string> object with the script for objects</returns>
        internal IEnumerable<string> EnumScriptImplWorker(ScriptingPreferences sp)
        {
            if (null == sp)
            {
                throw new ArgumentNullException("scriptingPreferences");
            }

            ScriptMaker tmpScriptMaker = new ScriptMaker(this.GetServerObject());

            if (!sp.DependentObjects)
            {
                tmpScriptMaker.Prefetch = !this.InitializedForScripting;
            }

            // if the user has not set the target server version, then we'll
            // set it to match the version of this object
            if (!sp.TargetVersionAndDatabaseEngineTypeDirty)
            {
                sp.SetTargetServerInfo(this,false);
            }

            tmpScriptMaker.Preferences = sp;

            EnumerableContainer scriptEnumerable = new EnumerableContainer();
            scriptEnumerable.Add(tmpScriptMaker.Script(new SqlSmoObject[] { this }));
            return scriptEnumerable;

        }

        protected bool IsVersion80SP3()
        {
            return ((ServerVersion.Major == 8 && ServerVersion.BuildNumber >= 760) ||
                (ServerVersion.Major > 8));
        }

        protected bool IsVersion90AndAbove()
        {
            return (ServerVersion.Major >= 9);
        }


        /// <summary>
        /// Throws an exception if the ServerVersion for this object is below
        /// 8.760 (8.0 SP3)
        /// </summary>
        protected void ThrowIfBelowVersion80SP3()
        {
            if (!IsVersion80SP3())
            {
                throw new SmoException(ExceptionTemplates.SupportedOnlyOn80SP3);
        }
        }

        internal string GetBindRuleScript(ScriptingPreferences sp, string ruleSchema, string ruleName, bool futureOnly)
        {
            return GetBindScript(sp, ruleSchema, ruleName, futureOnly, true);
        }

        internal string GetBindDefaultScript(ScriptingPreferences sp, string defSchema, string defName, bool futureOnly)
        {
            return GetBindScript(sp, defSchema, defName, futureOnly, false);
        }

        private string GetBindScript(ScriptingPreferences sp, string schema, string name, bool futureOnly, bool forRule)
        {
            string objName = string.Empty;

            if (this is Column)
            {
                objName = string.Format(SmoApplication.DefaultCulture, "{0}.{1}", ((ScriptSchemaObjectBase)ParentColl.ParentInstance).FormatFullNameForScripting(sp),
                                                ((Column)this).FormatFullNameForScripting(sp));
            }
            else if (this is UserDefinedDataType && sp.TargetServerVersion <= SqlServerVersion.Version80)
            {
                objName = MakeSqlBraket(((UserDefinedDataType)this).GetName(sp));
            }
            else if (this is ScriptSchemaObjectBase)
            {
                objName = ((ScriptSchemaObjectBase)this).FormatFullNameForScripting(sp);
            }
            else
            {
                objName = ((ScriptNameObjectBase)this).FormatFullNameForScripting(sp);
            }

            //get prefix based on server version
            string prefix = sp.TargetServerVersion <= SqlServerVersion.Version80 ? "dbo" : "sys";

            //format the schema
            //we don't take into consideration sp.IncludeScripts.SchemaQualify because 'sp' are the scriting preferences
            //for the current object, we don't know how the default/rule has been scripted
            //so don't attempt to find the default schema. just use whatever we've got
            schema = null == schema || schema.Length <= 0 ? string.Empty : MakeSqlBraket(SqlString(schema)) + Globals.Dot;

            if (forRule)
            {
                return string.Format(SmoApplication.DefaultCulture, "EXEC {0}.sp_bindrule @rulename=N'{1}[{2}]', @objname=N'{3}' {4}",
                        prefix,
                        schema,
                        SqlStringBraket(name),
                        SqlString(objName),
                        futureOnly ? ", @futureonly='futureonly'" : "");
            }
            return string.Format(SmoApplication.DefaultCulture, "EXEC {0}.sp_bindefault @defname=N'{1}[{2}]', @objname=N'{3}' {4}",
                    prefix,
                    schema,
                    SqlStringBraket(name),
                    SqlString(objName),
                    futureOnly ? ", @futureonly='futureonly'" : "");
        }

        protected void BindRuleImpl(string ruleSchema, string rule, bool bindColumns)
        {
            if (null == rule)
            {
                throw new ArgumentNullException("rule");
            }

            if (rule.Length == 0)
            {
                throw new ArgumentException(ExceptionTemplates.EmptyInputParam("rule", "string"));
            }

            if (null == ruleSchema)
            {
                throw new ArgumentNullException("ruleSchema");
            }

            try
            {
                

                if (!this.IsDesignMode)
                {
                    StringCollection cmds = new StringCollection();
                    cmds.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                    ScriptingPreferences sp = new ScriptingPreferences();
                    sp.SetTargetServerInfo(this);
                    sp.ForDirectExecution = true;
                    cmds.Add(GetBindRuleScript(sp, ruleSchema, rule, bindColumns));

                    this.ExecutionManager.ExecuteNonQuery(cmds);
                }

                if (!this.ExecutionManager.Recording)
                {
                    Properties.Get("Rule").SetValue(rule);
                    Properties.Get("RuleSchema").SetValue(ruleSchema);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Bind, this, e);
            }
        }

        protected void UnbindRuleImpl(bool bindColumns)
        {
            try
            {
                if (!this.IsDesignMode)
                {
                    StringCollection cmds = new StringCollection();

                    cmds.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                    // we take different action if the object is a rule or a default
                    string objName = string.Empty;
                    if (this is Column)
                    {
                        objName = string.Format(SmoApplication.DefaultCulture, "{0}.{1}", ParentColl.ParentInstance.FullQualifiedName, this.FullQualifiedName);
                    }
                    else
                    {
                        objName = this.FullQualifiedName;
                    }

                    cmds.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_unbindrule @objname=N'{0}' {1}",
                        SqlString(objName), bindColumns ? ", @futureonly='futureonly'" : ""));

                    this.ExecutionManager.ExecuteNonQuery(cmds);
                }

                if (!this.ExecutionManager.Recording)
                {
                    Properties.Get("Rule").SetValue(string.Empty);
                    Properties.Get("RuleSchema").SetValue(string.Empty);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Unbind, this, e);
            }
        }

        protected void BindDefaultImpl(string defaultSchema, string defaultName, bool bindColumns)
        {
            try
            {
                if (null == defaultName)
                {
                    throw new ArgumentNullException("defaultName");
                }

                if (defaultName.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.EmptyInputParam("defaultName", "string"));
                }

                if (null == defaultSchema)
                {
                    throw new ArgumentNullException("defaultSchema");
                }

                if (!this.IsDesignMode)
                {
                    StringCollection cmds = new StringCollection();
                    cmds.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                    ScriptingPreferences sp = new ScriptingPreferences();
                    sp.SetTargetServerInfo(this);
                    sp.ForDirectExecution = true;
                    cmds.Add(GetBindDefaultScript(sp, defaultSchema, defaultName, bindColumns));

                    this.ExecutionManager.ExecuteNonQuery(cmds);
                }

                if (!this.ExecutionManager.Recording)
                {
                    Properties.Get("Default").SetValue(defaultName);
                    Properties.Get("DefaultSchema").SetValue(defaultSchema);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Bind, this, e);
            }

        }

        protected void UnbindDefaultImpl(bool bindColumns)
        {
            try
            {
                CheckObjectState(true);

                if (!this.IsDesignMode)
                {
                    StringCollection cmds = new StringCollection();

                    cmds.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                    // we take different action if the object is a rule or a default
                    string objName = string.Empty;
                    if (this is Column)
                    {
                        objName = string.Format(SmoApplication.DefaultCulture, "{0}.{1}", ParentColl.ParentInstance.FullQualifiedName, this.FullQualifiedName);
                    }
                    else
                    {
                        objName = this.FullQualifiedName;
                    }

                    cmds.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_unbindefault @objname=N'{0}' {1}",
                        SqlString(objName),
                        bindColumns ? ", @futureonly='futureonly'" : ""));

                    this.ExecutionManager.ExecuteNonQuery(cmds);
                }

                if (!this.ExecutionManager.Recording)
                {
                    Properties.Get("Default").SetValue(string.Empty);
                    Properties.Get("DefaultSchema").SetValue(string.Empty);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Unbind, this, e);
            }
        }

        internal void CheckCollation(string collationName, ScriptingPreferences sp)
        {
            if (this.IsDesignMode)
            {
                // The collation cannot be checked in design mode
                // TODO: possibly maintain the supported collations in the client code
                return;
            }

            CollationVersion collationVersion = this.GetServerObject().GetCollationVersion(collationName);

            CollationVersion soVersion = CollationVersion.Version150;
            switch (sp.TargetServerVersion)
            {
                case SqlServerVersion.Version80:
                    soVersion = CollationVersion.Version80;
                    break;
                case SqlServerVersion.Version90:
                    soVersion = CollationVersion.Version90;
                    break;
                case SqlServerVersion.Version100:
                        soVersion = CollationVersion.Version100;
                        break;
                case SqlServerVersion.Version105:
                        soVersion = CollationVersion.Version105;
                        break;
                case SqlServerVersion.Version110:
                        soVersion = CollationVersion.Version110;
                        break;
                case SqlServerVersion.Version120:
                        soVersion = CollationVersion.Version120;
                        break;
                case SqlServerVersion.Version130:
                        soVersion = CollationVersion.Version130;
                        break;
                case SqlServerVersion.Version140:
                    soVersion = CollationVersion.Version140;
                    break;
            }
            if (collationVersion > soVersion)
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedCollation(collationName, GetSqlServerName(sp)));
            }

            return;
        }

        /// <summary>
        /// adds
        /// </summary>
        /// <param name="buffer"></param>
        private void AddNewLineFormat(StringBuilder buffer)
        {
            buffer.Append(Globals.commaspace);
            buffer.Append(Globals.newline);
            buffer.Append(Globals.tab);
            buffer.Append(Globals.tab);
        }

        internal void GetBoolParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, ref int count)
        {
            GetBoolParameter(buffer, sp, propName, sqlPropScript, ref count, false);
        }

        internal void GetBoolParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, ref int count, bool valueAsTrueFalse)
        {
            Property prop = Properties.Get(propName);
            if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                string value;
                if (valueAsTrueFalse)
                {
                    value = prop.Value.ToString().ToLower(SmoApplication.DefaultCulture);
                }
                else
                {
                    value = (bool)prop.Value ? "1" : "0";
                }

                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, value);
            }
        }

        internal void GetEnumParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, Type enumtype, ref int count)
        {
            Property prop = Properties.Get(propName);
            if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, Enum.Format(enumtype, prop.Value, "d"));
            }
        }

        internal bool GetDateTimeParameterAsInt(StringBuilder buffer, ScriptingPreferences sp, string propName,
            string sqlPropScript, ref int count)
        {
            Property prop = Properties.Get(propName);
            if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                DateTime dt = (DateTime)prop.Value;

                int date = dt.Year * 10000 + dt.Month * 100 + dt.Day;

                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, SqlString(date.ToString(SmoApplication.DefaultCulture)));
                return true;
            }

            return false;
        }

        internal bool GetTimeSpanParameterAsInt(StringBuilder buffer, ScriptingPreferences sp, string propName,
            string sqlPropScript, ref int count)
        {
            Property prop = Properties.Get(propName);
            if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                TimeSpan ts = (TimeSpan)prop.Value;

                int time = ts.Hours * 10000 + ts.Minutes * 100 + ts.Seconds;

                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, SqlString(time.ToString(SmoApplication.DefaultCulture)));
                return true;
            }

            return false;
        }

        internal void GetDateTimeParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, ref int count)
        {
            Property prop = Properties.Get(propName);
            if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                DateTime propvalue = (DateTime)prop.Value;
                Int32 dateval;
                Int32 timeval;
                if (propvalue == DateTime.MinValue)
                {
                    dateval = 0;
                    timeval = 0;
                }
                else
                {
                    dateval = propvalue.Year * 10000 + propvalue.Month * 100 + propvalue.Day;
                    timeval = propvalue.Hour * 10000 + propvalue.Minute * 100 + propvalue.Second;
                }

                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, "date", dateval);
                buffer.Append(Globals.commaspace);
                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, "time", timeval);
            }
        }

        internal bool GetGuidParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
            string sqlPropScript, ref int count)
        {
            Property prop = Properties.Get(propName);
            if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                Guid guid = (Guid)prop.Value;
                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, SqlString(guid.ToString()));
                return true;
            }

            return false;
        }

        internal bool GetStringParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, ref int count)
        {
            return GetStringParameter(buffer, sp, propName, sqlPropScript, ref count, false);
        }

        internal bool GetStringParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, ref int count, bool throwIfNotSet)
        {
            Property prop = this.GetPropertyOptional(propName);
            if (throwIfNotSet && null == prop.Value)
            {
                throw new PropertyNotSetException(propName);
            }

            if ((null != prop.Value) &&
                (!sp.ScriptForAlter || prop.Dirty) &&
                (sp.ScriptForAlter || ((string)prop.Value).Length > 0))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, SqlString((string)prop.Value));
                return true;
            }

            return false;
        }

        internal void GetParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, ref int count)
        {
            GetParameter(buffer, sp, propName, sqlPropScript, ref count, false);
        }

        internal void GetParameter(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropScript, ref int count, bool throwIfNotSet)
        {
            Property prop = Properties.Get(propName);
            if (throwIfNotSet && null == prop.Value)
            {
                throw new PropertyNotSetException(propName);
            }

            if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
            {
                if (count++ > 0)
                {
                    AddNewLineFormat(buffer);
                }

                buffer.AppendFormat(SmoApplication.DefaultCulture, sqlPropScript, prop.Value);
            }
        }

        internal void CheckPendingState()
        {
            if (this.IsDesignMode)
            {
                //we allow various operations in pending state, when it is design mode. So skip this check
                return;
            }

            if (this.State == SqlSmoState.Pending)
            {
                // first check if parent is not set
                if (parentColl == null)
                {
                    throw new FailedOperationException(ExceptionTemplates.NeedToSetParent);
                }

                // get the field(s) that we need to set
                StringCollection keyFieldNames = GetEmptyKey().GetFieldNames();
                // TODO: FIX_IN_KATMAI: use paramarray here, avoid cascading if
                if (keyFieldNames.Count == 1)
                {
                    throw new FailedOperationException(ExceptionTemplates.OperationNotInPendingState1(keyFieldNames[0]));
                }
                else if (keyFieldNames.Count == 2)
                {
                    throw new FailedOperationException(ExceptionTemplates.OperationNotInPendingState2(keyFieldNames[0], keyFieldNames[1]));
                }
                else if (keyFieldNames.Count == 3)
                {
                    throw new FailedOperationException(ExceptionTemplates.OperationNotInPendingState3(keyFieldNames[0], keyFieldNames[1], keyFieldNames[2]));
                }
                else
                {
                    throw new FailedOperationException(ExceptionTemplates.OperationNotInPendingState);
                }
            }
        }

        #region DataType
        internal DataType GetDataType(ref DataType dataType)
        {
            try
            {
                CheckPendingState();

                if (null == dataType)
                {
                    dataType = new DataType();
                    dataType.Parent = this;
                    // If property bag is available, why not read from it, instead of returning an invalid dataType?
                    // This is a problem for XSchema columns, where the DataType propert remains invlaid
                    // even after all the underlying properties have been set
                    //if (this.State != SqlSmoState.Creating)
                    {
                        // populate the object from the property bag
                        dataType.ReadFromPropBag(this);
                    }
                }

                return dataType;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetDataType, this, e);
            }
        }

        internal void SetDataType(ref DataType targetDataType, DataType sourceDataType)
        {
            try
            {
                CheckPendingState();

                if (null == sourceDataType)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("DataType"));
                }

                //we should always clone the sourceDataType object
                targetDataType = sourceDataType.Clone();

                targetDataType.Parent = this;
                WriteToPropBag(targetDataType);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetDataType, this, e);
            }
        }

        internal void WriteToPropBag(DataType dataType)
        {

            switch (dataType.SqlDataType)
            {
                case SqlDataType.BigInt:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.BigInt);
                    break;
                case SqlDataType.Binary:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Binary);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.Bit:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Bit);
                    break;
                case SqlDataType.Char:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Char);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.DateTime:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.DateTime);
                    break;
                case SqlDataType.Decimal:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Decimal);
                    this.Properties.Get("NumericPrecision").Value = dataType.NumericPrecision;
                    this.Properties.Get("NumericScale").Value = dataType.NumericScale;
                    break;
                case SqlDataType.Numeric:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Numeric);
                    this.Properties.Get("NumericPrecision").Value = dataType.NumericPrecision;
                    this.Properties.Get("NumericScale").Value = dataType.NumericScale;
                    break;
                case SqlDataType.Float:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Float);
                    this.Properties.Get("NumericPrecision").Value = dataType.NumericPrecision;
                    break;
                case SqlDataType.Geography:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Geography);
                    break;
                case SqlDataType.Geometry:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Geometry);
                    break;
                case SqlDataType.Image:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Image);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.Int:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Int);
                    break;
                case SqlDataType.Money:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Money);
                    break;
                case SqlDataType.NChar:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.NChar);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.NText:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.NText);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.NVarChar:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.NVarChar);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.NVarCharMax:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.NVarCharMax);
                    this.Properties.Get("Length").Value = -1;
                    break;
                case SqlDataType.Real:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Real);
                    break;
                case SqlDataType.SmallDateTime:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.SmallDateTime);
                    break;
                case SqlDataType.SmallInt:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.SmallInt);
                    break;
                case SqlDataType.SmallMoney:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.SmallMoney);
                    break;
                case SqlDataType.Text:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Text);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.Timestamp:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Timestamp);
                    break;
                case SqlDataType.TinyInt:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.TinyInt);
                    break;
                case SqlDataType.UniqueIdentifier:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.UniqueIdentifier);
                    break;
                case SqlDataType.UserDefinedDataType:
                    this.Properties.Get("DataType").Value = dataType.Name;
                    if (dataType.Schema.Length > 0)
                    {
                        this.Properties.Get("DataTypeSchema").Value = dataType.Schema;
                    }

                    break;
                case SqlDataType.UserDefinedTableType:
                    this.Properties.Get("DataType").Value = dataType.Name;
                    if (dataType.Schema.Length > 0)
                    {
                        this.Properties.Get("DataTypeSchema").Value = dataType.Schema;
                    }

                    break;
                case SqlDataType.UserDefinedType:
                    this.Properties.Get("DataType").Value = dataType.Name;
                    if (dataType.Schema.Length > 0)
                    {
                        this.Properties.Get("DataTypeSchema").Value = dataType.Schema;
                    }

                    break;
                case SqlDataType.VarBinary:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.VarBinary);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.VarBinaryMax:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.VarBinaryMax);
                    this.Properties.Get("Length").Value = -1;
                    break;
                case SqlDataType.VarChar:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.VarChar);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
                case SqlDataType.VarCharMax:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.VarCharMax);
                    this.Properties.Get("Length").Value = -1;
                    break;
                case SqlDataType.Variant:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Variant);
                    break;
                case SqlDataType.Xml:
                    this.Properties.Get("DataType").Value = "xml";
                    if (dataType.Name.Length > 0)
                    {
                        this.Properties.Get("XmlSchemaNamespace").Value = dataType.Name;
                    }

                    if (dataType.Schema.Length > 0)
                    {
                        this.Properties.Get("XmlSchemaNamespaceSchema").Value = dataType.Schema;
                    }

                    this.Properties.Get("XmlDocumentConstraint").Value = dataType.XmlDocumentConstraint;
                    break;
                case SqlDataType.SysName:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.SysName);
                    break;
                case SqlDataType.Date:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Date);
                    break;
                case SqlDataType.Time:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Time);
                    this.Properties.Get("NumericScale").Value = dataType.NumericScale;
                    break;
                case SqlDataType.DateTimeOffset:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.DateTimeOffset);
                    this.Properties.Get("NumericScale").Value = dataType.NumericScale;
                    break;
                case SqlDataType.DateTime2:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.DateTime2);
                    this.Properties.Get("NumericScale").Value = dataType.NumericScale;
                    break;
                case SqlDataType.HierarchyId:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.HierarchyId);
                    break;
                case SqlDataType.Json:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Json);
                    break;
                case SqlDataType.Vector:
                    this.Properties.Get("DataType").Value = dataType.GetSqlName(SqlDataType.Vector);
                    this.Properties.Get("Length").Value = dataType.MaximumLength;
                    break;
            }
        }
        #endregion

        internal static string GetTypeName(string typeName)
        {
            try
            {
                return ExceptionTemplatesImpl.Keys.GetString(typeName);
            }
            catch
            {
                return typeName;
            }
            
        }

        #region ISfcPropertyProvider Members

        Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertyProvider.GetPropertySet()
        {
            return this.Properties;
        }

        #endregion

        #region ISfcNotifyPropertyMetadataChanged Members

        internal override void OnPropertyMetadataChanged(string propname)
        {
            if (PropertyMetadataChanged != null)
            {
                PropertyMetadataChanged(this, new Microsoft.SqlServer.Management.Sdk.Sfc.SfcPropertyMetadataChangedEventArgs(propname));
            }
        }

        internal override bool ShouldNotifyPropertyMetadataChange
        {
            get { return PropertyMetadataChanged != null; }
        }

        [CLSCompliant(false)]
        public event EventHandler<Microsoft.SqlServer.Management.Sdk.Sfc.SfcPropertyMetadataChangedEventArgs> PropertyMetadataChanged;

        #endregion

        #region INotifyPropertyChanged Members

        internal override void OnPropertyChanged(string propname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propname));
            }
        }

        internal override bool ShouldNotifyPropertyChange
        {
            get { return PropertyChanged != null; }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Dependency Discovery

        /// <summary>
        /// Determines if current object is a system object by checking property bag
        /// for IsSystemObject property. Purely for internal use, during SMO discovery
        /// for serialization.
        /// </summary>
        /// <returns></returns>
        internal bool IsSystemObjectInternal()
        {
            int index = 0;
            bool isSystemObject = false;
            if (this.Properties.PropertiesMetadata.TryPropertyNameToIDLookup("IsSystemObject", out index))
            {
                object boolValue = this.Properties.Get(index).Value;
                if (boolValue != null)
                {
                    isSystemObject = (bool)boolValue;
                }
            }
            return isSystemObject;
        }

        /// <summary>
        /// Best Effort Discovery mechanism : This method
        /// reflects upon metadata and gets all available objects
        /// </summary>
        /// <returns></returns>
        public List<object> Discover()
        {
            //In design mode:
            //1. restrict discovery to only those types that implements ISfcSupportsDesignMode
            //2. Do not prefetch objects, since there is no server connection
            //3. Ignore references. Object query is not supported in design mode, so they cannot be resolved.

            List<object> resultList = new List<object>();
            resultList.Add(this);

            Queue<object> dependentObjects = new Queue<object>();
            dependentObjects.Enqueue(this);

            if (this is Database && !this.IsDesignMode)
            {
                ((Database)this).PrefetchObjects();
            }

            while (dependentObjects.Count > 0)
            {
                object obj = dependentObjects.Dequeue();
                SfcMetadataDiscovery metadata = new SfcMetadataDiscovery(obj.GetType());
                foreach (SfcMetadataRelation relation in metadata.Relations)
                {
                    //if relationship is of no interest to discovery skip it.
                    if (!(relation.Relationship == SfcRelationship.ChildContainer ||
                          relation.Relationship == SfcRelationship.ObjectContainer ||
                          relation.Relationship == SfcRelationship.ChildObject ||
                          relation.Relationship == SfcRelationship.Object) )
                    {
                        continue;
                    }

                    //In design mode, skip relation that points to type that does not support design mode.
                    //Note, this works for collections as well since relation.Type points to
                    //type of collection member. A type supports design mode by implementing ISfcSupportsDesignMode interface
                    if (this.IsDesignMode &&
                        relation.Type.GetInterface(typeof(ISfcSupportsDesignMode).Name) == null)
                    {
                        continue;
                    }

                    //Can optimize by going to property bag first
                    //if there is a guarantee that we'd always be dealing with SqlSmoObject
                    object relationObject = null;
                    try
                    {
                        PropertyInfo pi = obj.GetType().GetProperty(relation.PropertyName);
                        relationObject = pi.GetValue(obj, null);
                    }
                    catch (TargetInvocationException)
                    {
                    }

                    //if cannot retrieve value for current relation, skip it and its references
                    if (relationObject == null)
                    {
                        continue;
                    }

                    if ((relation.Relationship == SfcRelationship.ChildContainer) ||
                        (relation.Relationship == SfcRelationship.ObjectContainer))
                    {
                        try
                        {
                            SmoCollectionBase smoCollection = relationObject as SmoCollectionBase;
                            foreach (object childObject in smoCollection)
                            {
                                if ((childObject != null) && (childObject.GetType().Name != "SystemMessage") &&
                                    !DiscoveryHelper.IsSystemObject(childObject))
                                {
                                    dependentObjects.Enqueue(childObject);
                                    resultList.Add(childObject);
                                }
                            }

                        }
                        catch (EnumeratorException)
                        {
                            continue;
                        }
                    }
                    else if ((relation.Relationship == SfcRelationship.ChildObject) ||
                             (relation.Relationship == SfcRelationship.Object))
                    {
                        if (!DiscoveryHelper.IsSystemObject(relationObject))
                        {
                            dependentObjects.Enqueue(relationObject);
                            resultList.Add(relationObject);
                        }
                        else
                        {
                            //skip exploring references since current object itself is not included.
                            continue;
                        }
                    }

                    //$TODO: In design mode we have don't have reference resolution because object query is not
                    //working in design mode, so we skip it. When we have it, the if-check below should be removed.
                    //
                    if (!this.IsDesignMode)
                    {
                        foreach (Attribute attribute in relation.RelationshipAttributes)
                        {
                            if (attribute is SfcReferenceAttribute)
                            {
                                SfcReferenceAttribute referenceAttribute = attribute as SfcReferenceAttribute;
                                object referenceObject = referenceAttribute.Resolve(this);

                                if (referenceObject != null)
                                {
                                    dependentObjects.Enqueue(referenceObject);
                                    resultList.Add(referenceObject);
                                }
                            }
                        }
                    }
                }
            }

            return resultList;
        }
        #endregion

        #region IAlienObject support
        object IAlienObject.Resolve(string urnString)
        {
            //
            // Copy from original confused and inefficient implementation in SFC.
            // Must be cleaned up and optimized -- specifically to not use reflection
            //

            SqlSmoObject smoObject = this;
            Server server = null;
            SqlStoreConnection sfcSqlConnection = null;
            SfcObjectQuery objectQuery = null;
            object result = null;

            while (true)
            {
                Type objectType = smoObject.GetType();
                PropertyInfo parent = objectType.GetProperty("Parent");
                if (parent == null)//Server
                {
                    server = smoObject as Server;
                    sfcSqlConnection = new SqlStoreConnection(server.ConnectionContext.SqlConnectionObject);
                    break;
                }
                smoObject = objectType.InvokeMember("Parent", BindingFlags.GetProperty, null, smoObject, null) as SqlSmoObject;
            }

            if (server == null)
            {
                server = new Server(sfcSqlConnection.ServerConnection);
            }
            objectQuery = new SfcObjectQuery(server);

            int countObjects = 0;
            foreach (object obj in objectQuery.ExecuteIterator(new SfcQueryExpression(urnString), null, null))
            {
                result = obj;
                countObjects++;
            }
            if (countObjects > 1)
            {
                //TODO: Throw?
            }
            return result;
        }

        List<object> IAlienObject.Discover()
        {
            return this.Discover();
        }

        /// <summary>
        /// Sets the value of given property, if it is writable. If property is not in property bag, reflection is used.
        /// Throws exception if the property is not found.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        void IAlienObject.SetPropertyValue(string propertyName, Type propertyType, object value)
        {
            int index = 0;
            if (this.Properties.PropertiesMetadata.TryPropertyNameToIDLookup(propertyName, out index) &&
                this.Properties.PropertiesMetadata.GetStaticMetadata(index).PropertyType == propertyType)
            {
                Property property = this.Properties.Get(index);
                //must use internal setvalue() so that we can set readonly properties etc as internal client
                property.SetValue(value);
                property.SetRetrieved(true);
             }
            else
            {
                PropertyInfo propInfo = this.GetType().GetProperty(propertyName);
                if (propInfo != null && propInfo.PropertyType == propertyType)
                {
                    if (propInfo.CanWrite)//there are some properties that are not settable, that may have been serialized.
                    {
                        propInfo.SetValue(this, value, null);
                    }
                }
                else
                {
                    throw new FailedOperationException(ExceptionTemplates.PropertyNotFound(propertyName, propertyType.Name));
                }
            }
        }

        object IAlienObject.GetParent()
        {
            return this.GetParentObject();
        }

        Urn IAlienObject.GetUrn()
        {
            return this.Urn;
        }

        /// <summary>
        /// Retrieves value for a given property. Throws if property is not found.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        object IAlienObject.GetPropertyValue(string propertyName, Type propertyType)
        {
            object value = null;
            int index = 0;
            //if in property bag and of expected type, retrieve the value
            if (this.Properties.PropertiesMetadata.TryPropertyNameToIDLookup(propertyName, out index) &&
                this.Properties.PropertiesMetadata.GetStaticMetadata(index).PropertyType == propertyType)
            {
                value = this.Properties.Get(index).Value;

                if (value == null && IsSpeciallyLoaded(this.GetType(), propertyName))
                {
                    // specially loaded value
                    value = GetPropertyValueByReflection(propertyName, propertyType);
                }
            }
            else
            {
                // not in metdata, no choice, have to do it by reflection.
                value = GetPropertyValueByReflection(propertyName, propertyType);
            }

            return value;
        }

        private object GetPropertyValueByReflection(string propertyName, Type propertyType)
        {
            object value = null;
            PropertyInfo propInfo = null;
            if (!SfcMetadataDiscovery.TryGetCachedPropertyInfo(this.GetType().TypeHandle, propertyName, out propInfo))
            {
                propInfo = this.GetType().GetProperty(propertyName);
            }

            if (propInfo != null && propInfo.PropertyType == propertyType)
            {
                value = propInfo.GetValue(this, null);
            }
            else
            {
                throw new FailedOperationException(ExceptionTemplates.PropertyNotFound(propertyName, propertyType.Name));
            }

            return value;
        }

        /// <summary>
        /// In Smo, there are special properties exist in PropertiesMetadata, but not
        /// always loaded (returns null) into the Properties bag. It must be caused
        /// to load by using reflection.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private bool IsSpeciallyLoaded(Type t, string propertyName)
        {
            Diagnostics.TraceHelper.Assert(t != null, "Expect non-null type");
            Diagnostics.TraceHelper.Assert(propertyName != null, "Expect non-null property name");

            switch (t.Name)
            {
                case "Database":
                    return DATABASE_SPECIAL_PROPS.Contains(propertyName);
                default:
                    return System.StringComparer.Ordinal.Compare("IsSystemObject", propertyName) == 0;
            }
        }
        private static readonly IList<string> DATABASE_SPECIAL_PROPS = new List<string>(new string[] { "CompatibilityLevel", "Collation", "AnsiPaddingEnabled", "DatabaseEncryptionKey", "IsSystemObject", "DefaultSchema" });

        /// <summary>
        /// Discovers type of property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        Type IAlienObject.GetPropertyType(string propertyName)
        {
            //Always discover property type by looking at the .Net type surface.
            //Even though we go to property bag for setting the property values, we use
            //.Net type surface for property type discovery since we have property with same name but different types
            //on both the .net object and in underlying property bag. E.g: DataType property on Column object
            PropertyInfo propInfo = this.GetType().GetProperty(propertyName);
            if (propInfo != null)
            {
                return propInfo.PropertyType;
            }

            //$TODO: Needs string review, and move to *.strings file for localization
            throw new FailedOperationException("Cannot discover the property " + propertyName);

        }

        /// <summary>
        /// Sets state of this object based on provided SfcObjectState.
        /// </summary>
        /// <param name="state"></param>
        void IAlienObject.SetObjectState(SfcObjectState state)
        {
            switch (state)
            {
                case SfcObjectState.Dropped:
                    this.SetState(SqlSmoState.Dropped);
                    break;
                case SfcObjectState.Existing:
                    this.SetState(SqlSmoState.Existing);
                    break;
                case SfcObjectState.Pending:
                    this.SetState(SqlSmoState.Pending);
                    break;
                case SfcObjectState.ToBeDropped:
                    this.SetState(SqlSmoState.ToBeDropped);
                    break;
                default:
                    //we have a SFC state whose equivalent does not exist in SMO. Throw.
                    //$TODO: The string needs to be reviewed, moved to the localizable
                    throw new FailedOperationException(String.Format(SmoApplication.DefaultCulture,"Object state cannot be set. Cannot find an equivalent state for '{0}' in SMO", state));
            }
        }

        ISfcDomainLite IAlienObject.GetDomainRoot()
        {
            return this.GetServerObject() as ISfcDomainLite;
        }

        #endregion

        private Dictionary<string, StringCollection> roleToLoginCache = new Dictionary<string, StringCollection>();

        internal void AddLoginToRole(string roleName, string loginName)
        {
            if (roleToLoginCache.ContainsKey(roleName))
            {
                if (!roleToLoginCache[roleName].Contains(loginName))
                {
                    roleToLoginCache[roleName].Add(loginName);
                }
            }
            else
            {
                roleToLoginCache.Add(roleName, new StringCollection());
                roleToLoginCache[roleName].Add(loginName);
            }
        }

        internal void RemoveLoginFromRole(string roleName, string loginName)
        {
            if (roleToLoginCache.ContainsKey(roleName))
            {
                if (roleToLoginCache[roleName].Contains(loginName))
                {
                    roleToLoginCache[roleName].Remove(loginName);
                }
                else
                {
                    throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist("Login", loginName));
                }
            }
            else
            {
                throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist("Role", roleName));
            }
        }

        internal StringCollection EnumLoginsForRole(string roleName)
        {
            if (roleToLoginCache.ContainsKey(roleName))
            {
                return roleToLoginCache[roleName];
            }
            return new StringCollection();
        }

        internal StringCollection EnumRolesForLogin(string loginName)
        {
            StringCollection result = new StringCollection();

            foreach (KeyValuePair<string, StringCollection> kvp in roleToLoginCache)
            {
                if (kvp.Value.Contains(loginName))
                {
                    result.Add(kvp.Key);
                }
            }

            return result;
        }

        /// <summary>
        /// This implements the common pattern for all the custom actions of an object
        /// </summary>
        /// <param name="script">The custom script for the action</param>
        /// <param name="toplevelExceptionMessage">The top level message to display in case of an exception</param>
        internal void DoCustomAction(string script, string toplevelExceptionMessage)
        {
            try
            {
                this.ExecutionManager.ExecuteNonQuery(script);

                //invalidate all the properties, this should force a refresh for all the cached properties if they are accessed afterwards
                this.Properties.SetAllRetrieved(false);
                this.SetState(PropertyBagState.Empty); //empty the property bag

                this.GenerateAlterEvent();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(toplevelExceptionMessage, e);
            }
        }

        /// <summary>
        /// Executes the given action under the given execution modes.
        ///
        /// Resets the original modes after the action completes or fails.
        /// </summary>
        /// <param name="modes">The modes to run the action under.</param>
        /// <param name="action">The action to run.</param>
        public void ExecuteWithModes(SqlExecutionModes modes, Action action)
        {
            var originalExecutionMode = ExecutionManager.ConnectionContext.SqlExecutionModes;

            ExecutionManager.ConnectionContext.SqlExecutionModes = modes;

            try
            {
                action();
            }
            finally
            {
                ExecutionManager.ConnectionContext.SqlExecutionModes = originalExecutionMode;
            }
        }
    }


    // this enum represents the state in which an object might be
    public enum SqlSmoState
    {
        Pending,
        Creating,
        Existing,
        ToBeDropped,
        Dropped
    }

    // Has to be public, since used in protected method of non-sealed classes
    public enum UrnIdOption
    {
        WithId,
        OnlyId,
        NoId
    }

    public interface IExtendedProperties
    {
        ExtendedPropertyCollection ExtendedProperties
        {
            get;
        }
    }

    public interface IScriptable
    {
        // Script object with default scripting options
        System.Collections.Specialized.StringCollection Script();

        // Script object with specific scripting options
        System.Collections.Specialized.StringCollection Script(ScriptingOptions scriptingOptions);
    }

    /// <summary>
    /// Interface implemented by all instance classes that have all or a part of
    /// their definition as text
    /// </summary>
    public interface ITextObject
    {
        /// <summary>
        /// Generates a script that contains the header of the object (name, parameter list etc.)
        /// </summary>
        /// <param name="forAlter"></param>
        /// <returns></returns>
        string ScriptHeader(bool forAlter);

        /// <summary>
        /// Generates a script that contains the header of the object (name, parameter list etc.)
        /// </summary>
        /// <param name="scriptHeaderType"></param>
        /// <returns></returns>
        string ScriptHeader(Microsoft.SqlServer.Management.Smo.ScriptNameObjectBase.ScriptHeaderType scriptHeaderType);

        /// <summary>
        /// Returns the text body of the object (what follows after the AS keyword).
        /// </summary>
        string TextBody { get; set; }

        /// <summary>
        /// Returns the header of the object.
        /// </summary>
        string TextHeader { get; set; }

        /// <summary>
        /// Gets or sets the mode in which we operate with this object's text. True means
        /// the text contains the entire definition of the object, false means that it
        /// contains only the body (so the header is defined trhough the parameter list and
        /// other properties).
        /// </summary>
        bool TextMode { get; set; }
    }

    /// <summary>
    /// Describes the missing property which leads to either a full object initialization
    /// or to loading an expensive property separately from initialization
    /// </summary>
    public class PropertyMissingEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the property
        /// </summary>
        public string PropertyName { get; private set; }
        /// <summary>
        /// Name of the SMO object type
        /// </summary>
        public string TypeName { get; private set; }

        internal PropertyMissingEventArgs(string propertyName, string typeName)
        {
            PropertyName = propertyName;
            TypeName = typeName;
        }
    }

    #region Design Mode Helpers
    internal static class DiscoveryHelper
    {
        internal static bool IsSystemObject(object obj)
        {
            return (obj is SqlSmoObject) &&
                   ((SqlSmoObject)obj).IsSystemObjectInternal();
        }
    }
    #endregion
}

