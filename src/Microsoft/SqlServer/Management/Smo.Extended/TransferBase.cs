// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Internal;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Base class for transfer
    /// </summary>
    public abstract class TransferBase
    {
        public TransferBase()
        {
            this.Init();
        }

        public TransferBase(Database database)
            : this()
        {
            SetDatabase(database);
        }

        #region public properties

        private Database database;
        public Database Database
        {
            get
            {
                return database;
            }

            set
            {
                SetDatabase(value);
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        private void SetDatabase(Database database)
        {
            if (null == database)
                throw new FailedOperationException(ExceptionTemplates.SetDatabase, this, new ArgumentNullException("database"));

            if (database.State == SqlSmoState.Pending ||
                database.State == SqlSmoState.Creating)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.TransferCtorErr,
                                                       new ArgumentException("database"));

            }

            this.database = database;
        }

        private ArrayList objectList = null;
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ArrayList ObjectList
        {
            get
            {
                if (null == objectList)
                    objectList = new ArrayList();
                return objectList;
            }
            set
            {
                if (null == value)
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("ObjectList"));

                objectList = value;
            }
        }

        private readonly List<Urn> incompatibleObjects = new List<Urn>();
        public IList<Urn> IncompatibleObjects
        {
            get { return incompatibleObjects; }
        }

        private void Init()
        {
            this.CopyAllObjects = true;
            this.CopySchema = true;
            this.CopyData = true;
            this.DestinationDatabase = string.Empty;
            this.DestinationLoginSecure = true;
            this.DestinationTranslateChar = true;
            this.SourceTranslateChar = true;
            this.TargetDatabaseFilePath = string.Empty;
            this.TargetLogFilePath = string.Empty;
            this.PrefetchObjects = true;
        }

        // copy all objects by default
        public bool CopyAllObjects { get; set; }

        public bool CopyAllFullTextCatalogs { get; set; }
        public bool CopyAllFullTextStopLists { get; set; }
        public bool CopyAllSearchPropertyLists { get; set; }
        public bool CopyAllTables { get; set; }
        public bool CopyAllViews { get; set; }
        public bool CopyAllStoredProcedures { get; set; }
        public bool CopyAllUserDefinedFunctions { get; set; }
        public bool CopyAllUserDefinedDataTypes { get; set; }
        public bool CopyAllUserDefinedTableTypes { get; set; }
        public bool CopyAllSecurityPolicies { get; set; }
        public bool CopyAllPlanGuides { get; set; }
        public bool CopyAllRules { get; set; }
        public bool CopyAllDefaults { get; set; }
        public bool CopyAllUsers { get; set; }
        public bool CopyAllRoles { get; set; }
        public bool CopyAllPartitionSchemes { get; set; }
        public bool CopyAllPartitionFunctions { get; set; }
        public bool CopyAllXmlSchemaCollections { get; set; }
        public bool CopyAllSqlAssemblies { get; set; }
        public bool CopyAllUserDefinedAggregates { get; set; }
        public bool CopyAllUserDefinedTypes { get; set; }
        public bool CopyAllSchemas { get; set; }
        public bool CopyAllSynonyms { get; set; }
        public bool CopyAllSequences { get; set; }
        public bool CopyAllDatabaseTriggers { get; set; }
        public bool CopyAllDatabaseScopedCredentials { get; set; }
        public bool CopyAllExternalFileFormats { get; set; }
        public bool CopyAllExternalDataSources { get; set; }
        public bool CopyAllLogins { get; set; }
        public bool CopyAllExternalLanguages { get; set; }
        public bool CopyAllExternalLibraries { get; set; }
        public bool CopySchema { get; set; }
        public bool CopyData { get; set; }
        public bool DropDestinationObjectsFirst { get; set; }
        public bool CreateTargetDatabase { get; set; }
        public bool DestinationTranslateChar { get; set; }
        public bool SourceTranslateChar { get; set; }
        public bool UseDestinationTransaction { get; set; }
        public bool PreserveLogins { get; set; }
        public bool PrefetchObjects { get; set; }
        public bool CopyAllColumnMasterkey { get; set; }
        public bool CopyAllColumnEncryptionKey { get; set; }
        /// <summary>
        /// Sets or gets a value indicating whether external tables will be scripted.
        /// By default we don't script them in Transfer because we are unable to also script
        /// the correct credential secret to access the external data source.
        /// The UI will set this to true for Generate Scripts wizard.
        /// </summary>
        public bool CopyExternalTables { get; set; }

        public bool CopyAllExternalStream { get; set; }

        public bool CopyAllExternalStreamingJob { get; set; }

        // Added in Yukon SP2, default is false for backcompat
        public bool PreserveDbo { get; set; }

        ///<summary>
        /// If set to true, Sql Server Authentication is used.
        /// If not set, Login and Password are ignored.
        /// </summary>
        public bool DestinationLoginSecure { get; set; }

        /// <summary>
        /// The name of the destination database
        /// </summary>
        public string DestinationDatabase { get; set; }

        private string destinationLogin = string.Empty;
        /// <summary>
        /// The login name to use to connect to the destination server
        /// </summary>
        public string DestinationLogin
        {
            get { return destinationLogin; }
            set
            {
                if (null == value)
                    throw new ArgumentNullException("DestinationLogin");
                destinationLogin = value;
            }
        }

        private SqlSecureString destinationPassword = string.Empty;
        /// <summary>
        /// The password to use to connect to the destination server
        /// </summary>
        public string DestinationPassword
        {
            get
            {
                return (string)destinationPassword;
            }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("DestinationPassword");
                }
                destinationPassword = new SqlSecureString(value);
            }
        }

        private string destinationServer = string.Empty;
        /// <summary>
        /// The name of the destination server
        /// </summary>
        public string DestinationServer
        {
            get { return destinationServer; }
            set
            {
                destinationServer = value ?? throw new ArgumentNullException(nameof(DestinationServer));
            }
        }


        /// <summary>
        /// Provides a connection to the destination server. When set, the
        /// DestinationServer, DestinationLogin, DestinationPassword, and DestinationLogin values are not used.
        /// </summary>
        public ServerConnection DestinationServerConnection
        {
            get;set;
        }

        /// <summary>
        /// The folder on the destination used to store database data files
        /// </summary>        
        public string TargetDatabaseFilePath { get; set; }

        /// <summary>
        /// The folder on the destination used to store database log fles
        /// </summary>
        public string TargetLogFilePath { get; set; }

        // Target database files (i.e. DataFiles and LogFiles) provided by the user
        // and that replace the current paths originally used by the source database,
        // which is being used as a template when scripting the target database.
        private DatabaseFileMappingsDictionary databaseFileMappings = new DatabaseFileMappingsDictionary();
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public DatabaseFileMappingsDictionary DatabaseFileMappings
        {
            get { return databaseFileMappings; }
            set
            {
                if (value == null)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.WrongPropertyValueException("DatabaseFileMappings", "NULL"));
                }
                databaseFileMappings = value;
            }
        }

        #endregion

        #region Scripter_methods

        private Scripter scripter = null;
        protected Scripter Scripter
        {
            get
            {
                if (null == scripter)
                {
                    if (null == this.Database)
                        throw new PropertyNotSetException("Database");

                    scripter = new Scripter(this.Database.Parent);
                    scripter.PrefetchObjects = false;
                    scripter.Options.WithDependencies = true;
                }

                return scripter;
            }
        }

        /// <summary>
        /// Specifies the options that control script generation for the Transfer
        /// </summary>
        public ScriptingOptions Options
        {
            get
            {
                return this.Scripter.GetOptions();
            }
            set
            {
                this.Scripter.Options = value;
            }
        }

        /// <summary>
        /// Sets the target server version based on the Destination server information
        /// </summary>
        protected void SetTargetServerInfo()
        {
            //Get the destination connection. This is needed to set the login information
            var destinationConn = GetDestinationServerConnection();
            try
            {
                destinationConn.Connect();
                this.Scripter.Options.SetTargetServerVersion(destinationConn.ServerVersion);
            }
            finally
            {
                destinationConn.Disconnect();
            }
        }

        /// <summary>
        /// Returns a ServerConnection for the destination server
        /// </summary>
        /// <returns></returns>
        protected ServerConnection GetDestinationServerConnection()
        {
            if (DestinationServerConnection != null)
            {
                return DestinationServerConnection;
            }
            var connection = new ServerConnection(DestinationServer)
            {
                LoginSecure = DestinationLoginSecure,
                NonPooledConnection = true
            };

            if (!connection.LoginSecure)
            {
                connection.Login = DestinationLogin;
                connection.Password = DestinationPassword;
            }

            return connection;
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        /// <returns>StringCollection object with the script for objects</returns>
        public StringCollection ScriptTransfer()
        {
            if (null == this.Database)
                throw new PropertyNotSetException("Database");

            if (Options.ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            try
            {
                StringCollection sc = EnumerableContainer.IEnumerableToStringCollection(EnumScriptTransfer());
                return sc;
            }
            catch (Exception e) //else a general error message
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.ScriptTransfer, this, e);
            }
        }

        /// <summary>
        /// Returns an IEnumerable&lt;string&gt; object with the script for the objects.
        /// </summary>
        /// <returns>an IEnumerable&lt;string&gt; object with the script for the objects</returns>
        public IEnumerable<string> EnumScriptTransfer() // transfer with no parameters applies to the entire database
        {
            if (null == this.Database)
                throw new PropertyNotSetException("Database");

            bool originalWithDependencies = this.Scripter.Options.WithDependencies;
            bool incDbContext = this.Options.IncludeDatabaseContext;

            try
            {
#if DEBUG
                SqlSmoObject.Trace("Transfer: Entering");
                SqlSmoObject.Trace("Transfer: Script all discovered objects");
#endif
                EnumerableContainer queryEnumerable = new EnumerableContainer();
                this.Scripter.PrefetchObjects = false;
                this.Options.IncludeDatabaseContext = false;

                //Get the TargetServerVersion from the destination server if the server is specified.
                //Otherwise the target version will either be the default or a value set by users.
                if (!String.IsNullOrEmpty(this.DestinationServer))
                {
                    this.SetTargetServerInfo();
                }

                if (incDbContext)
                {
                    queryEnumerable.Add(new List<string> { string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlSmoObject.SqlBraket(string.IsNullOrEmpty(this.DestinationDatabase) ? this.Database.Name : this.DestinationDatabase)) });
                }

                Scripter.ScriptingError += ScripterOnScriptingError;
                queryEnumerable.Add(this.Scripter.EnumScriptWithList(GetObjectList(false)));
                Scripter.ScriptingError -= ScripterOnScriptingError;
                if (Options.ContinueScriptingOnError)
                {
                    queryEnumerable.Add(IncompatibleObjects.Select(n =>string.Format(@"-- {0}", DatabaseRestorePlannerSR.IncompatibleObject(n.Type, n.GetNameForType(n.Type)))));
                }

                return queryEnumerable;
            }
            catch (PropertyCannotBeRetrievedException e) //if we couldn't retrive a property
            {
                //return custom error message
                FailedOperationException foe = new FailedOperationException(
                                                                           ExceptionTemplates.FailedOperationExceptionTextScript(SqlSmoObject.GetTypeName(this.Database.GetType().Name), this.Database.ToString()), e);
                //add additional properties
                foe.Operation = ExceptionTemplates.ScriptTransfer;
                foe.FailedObject = this;

                throw foe;
            }
            catch (Exception e) //else a general error message
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ScriptTransfer, this, e);
            }
            finally
            {
                this.Options.IncludeDatabaseContext = incDbContext;
                this.Scripter.Options.WithDependencies = originalWithDependencies;
            }
        }

        private void ScripterOnScriptingError(object sender, ScriptingErrorEventArgs e)
        {
            incompatibleObjects.Add(e.Current);
        }

        #endregion

        #region EnumObject methods

        /// <summary>
        /// Gets URNs to be transferred
        /// </summary>
        /// <returns></returns>
        public UrnCollection EnumObjects()
        {
            return this.EnumObjects(true);
        }

        /// <summary>
        /// Gets URNs to be transferred
        /// </summary>
        /// <param name="ordered"></param>
        /// <returns></returns>
        public UrnCollection EnumObjects(bool ordered)
        {
            DependencyCollection objList = GetObjectList(ordered);
            UrnCollection urnColl = new UrnCollection();
            foreach (DependencyCollectionNode dep in objList)
            {
                urnColl.Add(dep.Urn);
            }

            return urnColl;
        }

        // computes the list of objects that we are going to transfer
        internal DependencyCollection GetObjectList(bool ordered)
        {
            DependencyCollection depList = null;
            HashSet<Urn> depDiscInputList = new HashSet<Urn>();

            //Discover unsupportedobjects
            HashSet<Urn> nonDepDiscList = new HashSet<Urn>();
            Dictionary<string, HashSet<Urn>> nonDepListDictionary = new Dictionary<string, HashSet<Urn>>();
            nonDepListDictionary.Add(User.UrnSuffix, new HashSet<Urn>());
            nonDepListDictionary.Add(DatabaseRole.UrnSuffix, new HashSet<Urn>());
            nonDepListDictionary.Add(ApplicationRole.UrnSuffix, new HashSet<Urn>());
            nonDepListDictionary.Add(Login.UrnSuffix, new HashSet<Urn>());
            nonDepListDictionary.Add(Endpoint.UrnSuffix, new HashSet<Urn>());

            //anything else
            HashSet<Urn> nonDepList = new HashSet<Urn>();

            ScriptingPreferences preferences = this.Options.GetScriptingPreferences();

            this.SeparateDiscoverySupportedObjects(depDiscInputList, nonDepDiscList, nonDepListDictionary, nonDepList);

            Dictionary<Type, StringCollection> originalDefaultFields = new Dictionary<Type, StringCollection>();

            try
            {
                // we will honor the CopyAll*** flags, by adding some of them to the
                // dependency discovery input. Duplicates will be filtered out by the discovery
                // algorithm, so we can just add all the objects based on their type, without checking
                // if they already are in objectList

                // Add extended properties for database
                //
                if (preferences.IncludeScripts.ExtendedProperties && this.IsSupportedObject<ExtendedProperty>(preferences) && CopyAllObjects)
                {
                    this.AddAllObjects<ExtendedProperty>(nonDepList, Database.ExtendedProperties);
                }

                this.AddDiscoverableObjects(depDiscInputList, originalDefaultFields);

                depList = new DependencyCollection();

                // perform dependency discovery
                bool doDependencyDiscovery = (this.Options.WithDependencies && !this.CopyAllObjects);
                if (0 < depDiscInputList.Count && doDependencyDiscovery)
                {
                    depDiscInputList = this.DiscoverDependencies(depDiscInputList);
                }

                this.PrefetchSecurityObjects(originalDefaultFields);

                this.AddDiscoveryUnsupportedObjects(depDiscInputList, nonDepDiscList);

                //Check if ordering has been requested
                if (ordered)
                {
                    depList = GetDependencyOrderedCollection(depDiscInputList);
                    this.AddSecurityObjectsInOrder(depList, nonDepListDictionary);
                }
                else
                {
                    foreach (Urn item in depDiscInputList)
                    {
                        depList.Add(new DependencyCollectionNode(item, true, true));
                    }
                    this.AddSecurityObjectsWithoutOrder(depList, nonDepListDictionary);
                }

                CheckDownLevelScripting(depList, preferences);

                // add objects that did not go through dependency discovery
                foreach (Urn item in nonDepList)
                {
                    // insert at the beginning of the list, because the other objects
                    // might depend on them
                    depList.Insert(0, new DependencyCollectionNode(item, true, true));
                }
            }
            finally
            {
                RestoreDefaultInitFields(originalDefaultFields);
            }

            return depList;
        }

        //Do dependency discovery and filter out object of other database
        private HashSet<Urn> DiscoverDependencies(HashSet<Urn> depDiscInputList)
        {
            HashSet<Urn> result = new HashSet<Urn>();
            // Get the list of objects in the right order with
            // Dependency discovery
#if DEBUG
            SqlSmoObject.Trace("Transfer: Discovering dependencies");
#endif
#if INCLUDE_PERF_COUNT
                    DateTime now = DateTime.Now;
#endif
            // if no filtering is needed then we don't have to redo
            // the topological sorting on the client side
            Sdk.Sfc.TraceHelper.Assert(null != this.Database, "null == this.Database");

            bool getDropDependencies = this.Options.ScriptSchema && this.Options.ScriptDrops;
            Urn[] urnArray = new Urn[depDiscInputList.Count];
            depDiscInputList.CopyTo(urnArray);
            DependencyTree depTree = this.Scripter.DiscoverDependencies(urnArray, !getDropDependencies);

            DependencyChainCollection deps = depTree.Dependencies;

            Sdk.Sfc.TraceHelper.Assert(null != deps, "GetDependencies() returned null");

            // generate flat dependencies list for scripting
            HashSet<Urn> orderedList = new HashSet<Urn>();
            if (depTree.FirstChild != null)
            {
                TreeTraversal(depTree.FirstChild, orderedList);
            }

#if INCLUDE_PERF_COUNT
                    if ( PerformanceCounters.DoCount )
                        PerformanceCounters.DiscoverDependenciesDuration += DateTime.Now - now;
#endif
            Urn rootDbUrn = "Server/Database[@Name='" + Urn.EscapeString(this.Database.Name) + "']";

            foreach (Urn urn in orderedList)
            {
                // skip default constraints which are defined on tables
                // These will be scripted while scripting table
                // hence we don't need to pass them seperately
                //
                if (urn.Type.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                    urn.Parent.Type.Equals("Column", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // we transfer only those objects which are in the same database
                // finding dependencies will return us objects on other database too
                // we dont sent them to the scripter
                Urn objectDbUrn = urn.Parent;  // this should give me the parent database Urn
                while (objectDbUrn.XPathExpression.Length > 2)  // in case it dint, then we keep going to the parent to obtain Urn which has two XPathExpressions
                {
                    objectDbUrn = objectDbUrn.Parent;
                }
                if (this.Database.Parent.CompareUrn(rootDbUrn, objectDbUrn) == 0)
                {
                    //depList.Add(new DependencyCollectionNode(urn, dep.IsSchemaBound, true));
                    if (urn.Type != "UnresolvedEntity")
                    {
                        result.Add(urn);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Adds objects to the scripting list after the dependency discovery.
        /// </summary>
        /// <param name="depList"></param>
        /// <param name="node"></param>
        ///
        private void AddWithoutDependencyDiscovery(DependencyCollection depList, DependencyCollectionNode node)
        {
            depList.Insert(0, node);
        }

        // Adds collection objects to the scripting list after the dependency discovery.
        private void AddWithoutDependencyDiscoveryCollection(DependencyCollection depList, HashSet<Urn> nonDepHashSet)
        {
            // add objects that did not go through dependency discovery
            foreach (Urn item in nonDepHashSet)
            {
                AddWithoutDependencyDiscovery(depList, new DependencyCollectionNode(item, true, true));
            }
        }

        //Add discovery unsupported objects uniquely
        private void AddDiscoveryUnsupportedObjects(HashSet<Urn> list, HashSet<Urn> nonDepDiscList)
        {
            ScriptingPreferences Preferences = this.Options.GetScriptingPreferences();

            // Add full text catalogs.
            if (this.IsSupportedObject<FullTextCatalog>(Preferences)
                && (CopyAllObjects || CopyAllFullTextCatalogs || this.Options.FullTextCatalogs))
            {
                // add each fullTextCatalog that is missing from the original list
                foreach (FullTextCatalog ftc in Database.FullTextCatalogs)
                {
                    list.Add(ftc.Urn);
                }
            }

            // Add full text stoplists.
            if (this.IsSupportedObject<FullTextStopList>(Preferences)
                && (CopyAllObjects || CopyAllFullTextStopLists || this.Options.FullTextStopLists))
            {
                // add each fullTextStopList that is missing from the original list
                foreach (FullTextStopList ftsl in Database.FullTextStopLists)
                {
                    list.Add(ftsl.Urn);
                }
            }

            // Add search property lists
            if (this.IsSupportedObject<SearchPropertyList>(Preferences)
                && (CopyAllObjects || CopyAllSearchPropertyLists))
            {
                // add each searchPropertyList that is missing from the original list
                foreach (SearchPropertyList spl in Database.SearchPropertyLists)
                {
                    list.Add(spl.Urn);
                }
            }

            if (this.IsSupportedObject<Schema>(Preferences)
                && (CopyAllObjects || CopyAllSchemas))
            {
                foreach (Schema schema in Database.Schemas)
                {
                    // we only need to transfer user created schemas
                    // schema.ID <= 4 are system schemas - dbo and sys for the moment
                    // schemaID in [16384,16400) are schemas tied to fixed server roles
                    if ((schema.ID > 4) && ((schema.ID < 16384) || (schema.ID >= 16400)))
                    {
                        list.Add(schema.Urn);
                    }
                }
            }

            list.UnionWith(nonDepDiscList);
        }

        private void PrefetchSecurityObjects(Dictionary<Type, StringCollection> originalDefaultFields)
        {
            ScriptingPreferences Preferences = this.Options.GetScriptingPreferences();

            if (PrefetchObjects && CopySchema)
            {
                if (CopyAllObjects || this.CopyAllUsers)
                {
                    Database.PrefetchUsers(Preferences);
                }
                if (CopyAllObjects || this.CopyAllRoles)
                {
                    Database.PrefetchDatabaseRoles(Preferences);
                }

                if (CopyAllObjects || this.CopyAllSchemas)
                {
                    Database.PrefetchSchemas(Preferences);
                }
            }
            else
            {
                // These fields are accessed by the code below.
                SetDefaultInitFields(originalDefaultFields, typeof(User), "IsSystemObject");
                SetDefaultInitFields(originalDefaultFields, typeof(Schema), "ID");
                SetDefaultInitFields(originalDefaultFields, typeof(DatabaseRole), "Owner", "IsFixedRole");
            }
        }

        private void AddSecurityObjectsInOrder(DependencyCollection depList, Dictionary<string, HashSet<Urn>> nonDepListDictionary)
        {
            //
            // users and roles need to be inserted in the head of the list so when we
            // script permissions on individual objects they already exist
            //
            //must be scripted before schemas  so that scripting authorization on schemas does not fail

            if (CopyAllObjects || CopyAllRoles)
            {
                DependencyObjects depObjects = new DependencyObjects();
                foreach (DatabaseRole role in Database.Roles)
                {
                    // Add a solo entry for each role node just in case it has no members (doesn't hurt if it does though)
                    depObjects.Add(role);

                    // Add an entry for role owner
                    string owner = role.Owner;

                    // We can get both User and Role strings back as members, add them all correctly
                    if (Database.Roles.Contains(owner))
                    {
                        // Add the entry only if owner is not a fixed role
                        if (!Database.Roles[owner].IsFixedRole)
                        {
                            depObjects.Add(Database.Roles[owner], role);
                        }
                    }
                    else if (Database.Users.Contains(owner))
                    {
                        // Add the entry only if owner is not a system user
                        if (!Database.Users[owner].IsSystemObject)
                        {
                            depObjects.Add(Database.Users[owner], role);
                        }
                    }

                    foreach (string roleMember in role.EnumMembers())
                    {
                        // We can get both User and Role strings back as members, add them all correctly
                        if (Database.Roles.Contains(roleMember))
                        {
                            depObjects.Add(role, Database.Roles[roleMember]);
                        }
                        else if (Database.Users.Contains(roleMember))
                        {
                            depObjects.Add(role, Database.Users[roleMember]);
                        }
                    }
                }

                // Use our own dependency ordering based on roles referencing other roles
                List<SqlSmoObject> depRolesList = depObjects.GetDependencies();
                foreach (SqlSmoObject smoObj in depRolesList)
                {
                    // We have a mix of both Users and DatabaseRoles from dependency checking, we only want other Roles here
                    // Note that Users need to be left in the dependency tupling since sometimes the only reference to a DatabaseRole
                    // may be a lone User member in it.
                    if (smoObj is DatabaseRole)
                    {
                        DatabaseRole role = (DatabaseRole)smoObj;
                        if (!role.IsFixedRole && role.Name != "public")
                        {
                            AddWithoutDependencyDiscovery(depList, new DependencyCollectionNode(role.Urn, true, true));
                            if (nonDepListDictionary[DatabaseRole.UrnSuffix].Contains(role.Urn))
                            {
                                nonDepListDictionary[DatabaseRole.UrnSuffix].Remove(role.Urn);
                            }
                        }
                    }
                }
            }

            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[DatabaseRole.UrnSuffix]);

            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[ApplicationRole.UrnSuffix]);
            ScriptingPreferences Preferences = this.Options.GetScriptingPreferences();
            if ((CopyAllObjects || CopyAllRoles) && this.IsSupportedObject<ApplicationRole>(Preferences))
            {
                foreach (ApplicationRole role in Database.ApplicationRoles)
                {
                    if (!nonDepListDictionary[ApplicationRole.UrnSuffix].Contains(role.Urn))
                    {
                        AddWithoutDependencyDiscovery(depList, new DependencyCollectionNode(role.Urn, true, true));
                    }
                }
            }

            //must be scripted before schemas  so that scripting authorization on schemas does not fail
            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[User.UrnSuffix]);
            if (CopyAllObjects || CopyAllUsers)
            {
                // Don't put any smarts here. Just collect the users no matter what.
                // GetObjectList returns the complete list, it should not care how this list will be used.
                foreach (User user in Database.Users)
                {
                    // Bug 420630: Similar to Userbase.cs we need to only filter out IsystemObject, but not necessarily
                    // all Users with Login empty (NO LOGIN, FOR CERTIFICATE, FOR ASYMMETRIC KEY all would have this state), otherwise these
                    // types of Users won't get scripted when Transfer is called to GetObjects(). I know of no cases so far where it is
                    // not sufficient to only filter on IsSystemObject.
                    //
                    // 'guest' is *not* flagged as IsSystemObject but we need to skip it here as well since it really is
                    // (you cannot DROP it for example, only disable it)
                    if (!user.IsSystemObject && 0 != string.Compare(user.Name, "guest", StringComparison.OrdinalIgnoreCase)
                        && (!nonDepListDictionary[User.UrnSuffix].Contains(user.Urn)))
                    {
                        AddWithoutDependencyDiscovery(depList, new DependencyCollectionNode(user.Urn, true, true));
                    }
                }
            }

            // Yukon SP2: We only transfer Endpoints if they are specifically requested. No dep discovery or CopyAllEndpoints || CopyAllObjects
            // support is there. Endpoints should be prepended to the list ahead of the Logins, so Logins will be in front of them since
            // an Endpoint can depend on an AUTHORIZATION login
            //
            // The Login associated must already be in the nonDepListLogin list, since being fed from the outside is the only way to have
            // the desired destination Login name in case it has a different name on the destination due to user preference or auto-renaming
            // such as machine accounts being transferred cross-machine. Both the Endpoint.Authorization and Login.Name properties would
            // already have the right name in them for scripting.
            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[Endpoint.UrnSuffix]);

            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[Login.UrnSuffix]);
            if (CopyAllLogins)
            {
                foreach (Login l in Database.Parent.Logins)
                {
                    if ((!l.IsSystemObject) && (!nonDepListDictionary[Login.UrnSuffix].Contains(l.Urn)))
                    {
                        AddWithoutDependencyDiscovery(depList, new DependencyCollectionNode(l.Urn, true, true));
                    }
                }
            }
        }

        private void AddSecurityObjectsWithoutOrder(DependencyCollection depList, Dictionary<string, HashSet<Urn>> nonDepListDictionary)
        {
            //we are not bothered about order here we will just add whatever is requested

            if (CopyAllObjects || CopyAllRoles)
            {
                foreach (DatabaseRole role in Database.Roles)
                {
                    if (!role.IsFixedRole && role.Name != "public")
                    {
                        nonDepListDictionary[DatabaseRole.UrnSuffix].Add(role.Urn);
                    }
                }
            }

            ScriptingPreferences Preferences = this.Options.GetScriptingPreferences();
            if ((CopyAllObjects || CopyAllRoles) && this.IsSupportedObject<ApplicationRole>(Preferences))
            {
                foreach (ApplicationRole role in Database.ApplicationRoles)
                {
                    nonDepListDictionary[ApplicationRole.UrnSuffix].Add(role.Urn);
                }
            }



            //must be scripted before schemas  so that scripting authorization on schemas does not fail
            if (CopyAllObjects || CopyAllUsers)
            {
                // Don't put any smarts here. Just collect the users no matter what.
                // GetObjectList returns the complete list, it should not care how this list will be used.
                foreach (User user in Database.Users)
                {
                    // Bug 420630: Similar to Userbase.cs we need to only filter out IsystemObject, but not necessarily
                    // all Users with Login empty (NO LOGIN, FOR CERTIFICATE, FOR ASYMMETRIC KEY all would have this state), otherwise these
                    // types of Users won't get scripted when Transfer is called to GetObjects(). I know of no cases so far where it is
                    // not sufficient to only filter on IsSystemObject.
                    //
                    // 'guest' is *not* flagged as IsSystemObject but we need to skip it here as well since it really is
                    // (you cannot DROP it for example, only disable it)
                    if (!user.IsSystemObject && 0 != string.Compare(user.Name, "guest", StringComparison.OrdinalIgnoreCase))
                    {
                        nonDepListDictionary[User.UrnSuffix].Add(user.Urn);
                    }
                }
            }

            // Yukon SP2: We only transfer Endpoints if they are specifically requested. No dep discovery or CopyAllEndpoints || CopyAllObjects
            // support is there. Endpoints should be prepended to the list ahead of the Logins, so Logins will be in front of them since
            // an Endpoint can depend on an AUTHORIZATION login
            //
            // The Login associated must already be in the nonDepListLogin list, since being fed from the outside is the only way to have
            // the desired destination Login name in case it has a different name on the destination due to user preference or auto-renaming
            // such as machine accounts being transferred cross-machine. Both the Endpoint.Authorization and Login.Name properties would
            // already have the right name in them for scripting.

            if (CopyAllLogins)
            {
                foreach (Login l in Database.Parent.Logins)
                {
                    if (!l.IsSystemObject)
                    {
                        nonDepListDictionary[Login.UrnSuffix].Add(l.Urn);
                    }
                }
            }

            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[User.UrnSuffix]);
            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[DatabaseRole.UrnSuffix]);
            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[ApplicationRole.UrnSuffix]);
            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[Endpoint.UrnSuffix]);
            AddWithoutDependencyDiscoveryCollection(depList, nonDepListDictionary[Login.UrnSuffix]);

        }

        // NOTE: If this.Prefetch is false, the given prefetch delegates will not be called here.
        // Instead, only the types supported by GswDatabasePrefetch will be pre-fetched during 
        // Generate Scripts/Transfer. You should consider adding types that could have many instances
        // in a database to here and to GswDatabasePrefetch in SMO/DatabasePrefetch.cs
        private void AddDiscoverableObjects(HashSet<Urn> depDiscInputList, Dictionary<Type, StringCollection> originalDefaultFields)
        {
            ScriptingPreferences preferences = this.Options.GetScriptingPreferences();
            // partition functions must come before partition schemes,
            // which must come before tables

            // adding all PartitionFunctions
            if (this.IsSupportedObject<PartitionFunction>(preferences))
            {
                this.AddAllObjects<PartitionFunction>(depDiscInputList, Database.PartitionFunctions,
                    CopyAllPartitionFunctions, Database.PrefetchPartitionFunctions, originalDefaultFields);
            }

            // adding all partition schemes
            if (this.IsSupportedObject<PartitionScheme>(preferences))
            {
                this.AddAllObjects<PartitionScheme>(depDiscInputList, Database.PartitionSchemes,
                   CopyAllPartitionSchemes, Database.PrefetchPartitionSchemes, originalDefaultFields);
            }

            if (this.IsSupportedObject<DatabaseScopedCredential>(preferences))
            {
                this.AddAllObjects<DatabaseScopedCredential>(depDiscInputList, Database.DatabaseScopedCredentials,
                    CopyAllDatabaseScopedCredentials, Database.PrefetchDatabaseScopedCredentials, null);
            }

            if (this.IsSupportedObject<ExternalFileFormat>(preferences))
            {
                this.AddAllObjects<ExternalFileFormat>(depDiscInputList, Database.ExternalFileFormats,
                    CopyAllExternalFileFormats, Database.PrefetchExternalFileFormats, null);
            }

            if (this.IsSupportedObject<ExternalDataSource>(preferences))
            {
                this.AddAllObjects<ExternalDataSource>(depDiscInputList, Database.ExternalDataSources,
                    CopyAllExternalDataSources, Database.PrefetchExternalDataSources, null);
            }

            // adding tables
            if (CopyAllObjects || CopyAllTables)
            {
                Table dummyTable = new Table(Database, Guid.NewGuid().ToString());
                if (PrefetchObjects)
                {
                    Database.PrefetchTables(preferences);
                }
                else
                {
                    // Make sure ID and IsSystemObject are retrieved when populating Database.Tables
                    // Note: ID is needed when the output is consumed by Prefetch class
                    var defaultFields = new List<string> { nameof(Table.ID), nameof(Table.IsSystemObject) };
                    if (this.IsSupportedObject<ExternalDataSource>(preferences)) { defaultFields.Add(nameof(Table.IsExternal)); }
                    if (dummyTable.IsSupportedProperty(nameof(Table.LedgerType))) { defaultFields.Add(nameof(Table.LedgerType)); }

                    SetDefaultInitFields(originalDefaultFields, typeof(Table), defaultFields.ToArray());
                }

                this.AddAllNonSystemObjects<Table>(depDiscInputList, Database.Tables,
                    (T) => { return !(T.IsSupportedProperty(nameof(Table.LedgerType)) && T.LedgerType == LedgerTableType.HistoryTable); });
            }

            if (this.IsSupportedObject<View>(preferences))
            {
                View dummyView = new View(Database, Guid.NewGuid().ToString());
                var defaultFields = new List<string> { nameof(View.ID), nameof(View.IsSystemObject) };
                if (dummyView.IsSupportedProperty(nameof(View.LedgerViewType))) { defaultFields.Add(nameof(View.LedgerViewType)); }

                this.AddAllNonSystemObjects<View>(List: depDiscInputList, collection: Database.Views,
                    copyAll: CopyAllViews, prefetch: Database.PrefetchViews, originalDefaultFields: originalDefaultFields, filterLedgerObjects: (T) => { return !(T.IsSupportedProperty(nameof(View.LedgerViewType)) && T.LedgerViewType.Equals(LedgerViewType.LedgerView)); }, fields: defaultFields.ToArray());
            }

            // adding all stored procedures
            if (this.IsSupportedObject<StoredProcedure>(preferences))
            {
                this.AddAllNonSystemObjects<StoredProcedure>(List: depDiscInputList, collection: Database.StoredProcedures,
                    copyAll: CopyAllStoredProcedures, prefetch: Database.PrefetchStoredProcedures, originalDefaultFields: originalDefaultFields, fields: nameof(StoredProcedure.IsSystemObject));
            }

            if (this.IsSupportedObject<UserDefinedFunction>(preferences))
            {
                this.AddAllNonSystemObjects<UserDefinedFunction>(List: depDiscInputList, collection: Database.UserDefinedFunctions,
                    copyAll: CopyAllUserDefinedFunctions, prefetch: Database.PrefetchUserDefinedFunctions, originalDefaultFields: originalDefaultFields, fields: new string[] { nameof(UserDefinedFunction.IsSystemObject), nameof(UserDefinedFunction.FunctionType) });
            }

            if (this.IsSupportedObject<SecurityPolicy>(preferences))
            {
                this.AddAllObjects<SecurityPolicy>(depDiscInputList, Database.SecurityPolicies,
                    CopyAllSecurityPolicies, Database.PrefetchSecurityPolicy, null);
            }

            //adding all user XmlSchemaCollections
            if (this.IsSupportedObject<XmlSchemaCollection>(preferences))
            {
                this.AddAllObjects<XmlSchemaCollection>(depDiscInputList, Database.XmlSchemaCollections,
                    CopyAllXmlSchemaCollections, Database.PrefetchXmlSchemaCollections, null);
            }

            // adding all assemblies
            if (this.IsSupportedObject<SqlAssembly>(preferences))
            {
                this.AddAllNonSystemObjects<SqlAssembly>(List: depDiscInputList, collection: Database.Assemblies,
                    copyAll: CopyAllSqlAssemblies, prefetch: Database.PrefetchSqlAssemblies, originalDefaultFields: originalDefaultFields, fields: nameof(SqlAssembly.IsSystemObject));
            }

            // adding all user defined aggregates
            if (this.IsSupportedObject<UserDefinedAggregate>(preferences))
            {
                this.AddAllObjects<UserDefinedAggregate>(depDiscInputList, Database.UserDefinedAggregates,
                    CopyAllUserDefinedAggregates, Database.PrefetchUserDefinedAggregates, null);
            }

            // adding all column Master Keys
            if (this.IsSupportedObject<ColumnMasterKey>(preferences))
            {
                this.AddAllObjects<ColumnMasterKey>(depDiscInputList, Database.ColumnMasterKeys,
                    CopyAllColumnMasterkey, prefetch: null, originalDefaultFields: null);
            }

            // adding all column Encryption Keys
            if (this.IsSupportedObject<ColumnEncryptionKey>(preferences))
            {
                this.AddAllObjects<ColumnEncryptionKey>(depDiscInputList, Database.ColumnEncryptionKeys,
                    CopyAllColumnEncryptionKey, Database.PrefetchColumnEncryptionKey, originalDefaultFields: null);
            }

            // adding all user defined types
            if (this.IsSupportedObject<UserDefinedType>(preferences))
            {
                this.AddAllObjects<UserDefinedType>(depDiscInputList, Database.UserDefinedTypes,
                    CopyAllUserDefinedTypes, Database.PrefetchUserDefinedTypes, null);
            }

            if (this.IsSupportedObject<PlanGuide>(preferences))
            {
                this.AddAllObjects<PlanGuide>(depDiscInputList, Database.PlanGuides,
                    CopyAllPlanGuides, null, null);
            }

            if (this.IsSupportedObject<UserDefinedTableType>(preferences))
            {
                this.AddAllObjects<UserDefinedTableType>(depDiscInputList, Database.UserDefinedTableTypes,
                    CopyAllUserDefinedTableTypes, Database.PrefetchUserDefinedTableTypes, null);
            }

            if (this.IsSupportedObject<Synonym>(preferences))
            {
                this.AddAllObjects<Synonym>(depDiscInputList, Database.Synonyms,
                    CopyAllSynonyms, null, null);
            }

            if (this.IsSupportedObject<Sequence>(preferences))
            {
                this.AddAllObjects<Sequence>(depDiscInputList, Database.Sequences,
                    CopyAllSequences, null, null);
            }

            if (this.IsSupportedObject<DatabaseDdlTrigger>(preferences))
            {
                this.AddAllObjects<DatabaseDdlTrigger>(depDiscInputList, Database.Triggers,
                    CopyAllDatabaseTriggers, null, null);
            }

            if (this.IsSupportedObject<ExternalLanguage>(preferences))
            {
                this.AddAllNonSystemObjects<ExternalLanguage>(List: depDiscInputList, collection: Database.ExternalLanguages,
                   copyAll: CopyAllExternalLanguages, prefetch: Database.PrefetchExternalLanguages, originalDefaultFields: originalDefaultFields, fields: nameof(ExternalLanguage.IsSystemObject));
            }

            if (this.IsSupportedObject<ExternalLibrary>(preferences))
            {
                this.AddAllObjects<ExternalLibrary>(depDiscInputList, Database.ExternalLibraries,
                    CopyAllExternalLibraries, Database.PrefetchExternalLibraries, null);
            }

            if (this.IsSupportedObject<UserDefinedDataType>(preferences))
            {
                this.AddAllObjects<UserDefinedDataType>(depDiscInputList, Database.UserDefinedDataTypes,
                    CopyAllUserDefinedDataTypes, null, null);
            }

            if (this.IsSupportedObject<Rule>(preferences))
            {
                this.AddAllObjects<Rule>(depDiscInputList, Database.Rules,
                    CopyAllRules, Database.PrefetchRules, null);
            }

            if (this.IsSupportedObject<Default>(preferences))
            {
                this.AddAllObjects<Default>(depDiscInputList, Database.Defaults,
                    CopyAllDefaults, Database.PrefetchDefaults, null);
            }

            if (this.IsSupportedObject<ExternalStream>(preferences))
            {
                this.AddAllObjects<ExternalStream>(depDiscInputList, Database.ExternalStreams,
                    CopyAllExternalStream, null, null);
            }

            if (this.IsSupportedObject<ExternalStreamingJob>(preferences))
            {
                this.AddAllObjects<ExternalStreamingJob>(depDiscInputList, Database.ExternalStreamingJobs,
                    CopyAllExternalStreamingJob, null, null);
            }
        }

        private void AddAllObjects<T>(ICollection<Urn> List, SmoCollectionBase collection)
            where T : SqlSmoObject
        {
#if DEBUG
            SqlSmoObject.Trace(string.Format(SmoApplication.DefaultCulture, "Transfer: Adding all objects {0} to dependency list", collection.GetType().Name));
#endif
            foreach (T item in collection)
            {
                List.Add(item.Urn);
            }
        }

        private void AddAllObjects<T>(ICollection<Urn> List, SmoCollectionBase collection, bool copyAll,
            Action<ScriptingPreferences> prefetch, Dictionary<Type, StringCollection> originalDefaultFields, params string[] fields)
            where T : SqlSmoObject
        {
            if (this.CopyAllObjects || copyAll)
            {
                if (PrefetchObjects && CopySchema)
                {
                    if (prefetch != null)
                    {
                        prefetch(this.Options.GetScriptingPreferences());
                    }
                }
                else if (originalDefaultFields != null)
                {
                    SetDefaultInitFields(originalDefaultFields, typeof(T), fields);
                }

                this.AddAllObjects<T>(List, collection);
            }
        }

        private void AddAllNonSystemObjects<T>(ICollection<Urn> List, SmoCollectionBase collection, Func<T, bool> filterLedgerObjects)
           where T : SqlSmoObject
        {
#if DEBUG
            SqlSmoObject.Trace(string.Format(SmoApplication.DefaultCulture, "Transfer: Adding all objects in {0} to dependency list", collection.GetType().Name));
#endif
            foreach (T item in collection)
            {
                if (!item.GetPropValueOptional<bool>("IsSystemObject", false) && filterLedgerObjects(item))
                {
                    List.Add(item.Urn);
                }
            }
        }

        private void AddAllNonSystemObjects<T>(ICollection<Urn> List, SmoCollectionBase collection, bool copyAll,
            Action<ScriptingPreferences> prefetch, Dictionary<Type, StringCollection> originalDefaultFields, Func<T, bool> filterLedgerObjects = null, params string[] fields)
            where T : SqlSmoObject
        {
            if (this.CopyAllObjects || copyAll)
            {
                if (PrefetchObjects && CopySchema)
                {
                    if (prefetch != null)
                    {
                        prefetch(this.Options.GetScriptingPreferences());
                    }
                }
                else if (originalDefaultFields != null)
                {
                    SetDefaultInitFields(originalDefaultFields, typeof(T), fields);
                }

                this.AddAllNonSystemObjects<T>(List, collection, filterLedgerObjects != null ? filterLedgerObjects : (t) => true);
            }
        }

        private bool IsSupportedObject<T>(ScriptingPreferences sp)
            where T : SqlSmoObject
        {
            return SmoUtility.IsSupportedObject(typeof(T), this.Database.ServerVersion, this.Database.DatabaseEngineType, this.Database.DatabaseEngineEdition)
                && SmoUtility.IsSupportedObject(typeof(T), ScriptingOptions.ConvertToServerVersion(sp.TargetServerVersion), sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition);
        }

        private void SeparateDiscoverySupportedObjects(HashSet<Urn> depDiscInputList, HashSet<Urn> nonDepDiscList, Dictionary<string, HashSet<Urn>> nonDepListDictionary, HashSet<Urn> nonDepList)
        {
            // filter the input objects to build the list of objects that we need to pass
            // through the dependency discovery
            foreach (object smoObject in ObjectList)
            {
                if (!(smoObject is SqlSmoObject))
                {
                    if (this.Scripter.Options.ContinueScriptingOnError)
                    {
                        continue;
                    }
                    else
                    {
                        if (smoObject == null)
                        {
                            throw new ArgumentNullException();
                        }
                        else
                        {
                            throw new SmoException(ExceptionTemplates.NeedToPassObject(smoObject.GetType().ToString()));
                        }
                    }
                }


                // TODO: filter out the rest of the objects that cannot be transferred
                switch (smoObject.GetType().Name)
                {
                    case "View":
                    case "StoredProcedure":
                    case "UserDefinedFunction":
                    case "PartitionScheme":
                    case "PartitionFunction":
                    case "XmlSchemaCollection":
                    case "UserDefinedAggregate":
                    case "UserDefinedType":
                    case "SqlAssembly":
                    case "Synonym":
                    case "Sequence":
                    case "PlanGuide":
                    case "UserDefinedTableType":
                    case "Rule":
                    case "Default":
                    case "DdlTrigger":
                    case "Trigger":
                    case "UserDefinedDataType":
                    case "Table":
                        depDiscInputList.Add(((SqlSmoObject)smoObject).Urn);
                        break;
                    case "FullTextCatalog":
                    case "FullTextStopList":
                    case "SearchPropertyList":
                    case "Schema":
                        nonDepDiscList.Add(((SqlSmoObject)smoObject).Urn);
                        break;
                    case "User":
                    case "ApplicationRole":
                    case "Role":
                    case "Login":
                    case "Endpoint":
                        nonDepListDictionary[((SqlSmoObject)smoObject).Urn.Type].Add(((SqlSmoObject)smoObject).Urn);
                        break;

                    default:
                        nonDepList.Add(((SqlSmoObject)smoObject).Urn);
                        break;
                }
            }
        }

        private void CheckDownLevelScripting(DependencyCollection depList, ScriptingPreferences preferences)
        {

            // will keep here objects that can't be scripted for
            // downlevel and need to be removed from the list
            var incompatibleNodes = new List<DependencyCollectionNode>();

            foreach (DependencyCollectionNode node in depList)
            {
                if ((this.Options.TargetServerVersionInternal < SqlServerVersionInternal.Version100 &&
                     this.Database.CompatibilityLevel >= CompatibilityLevel.Version90 &&
                     !CanScriptDownlevel(node.Urn, this.Options.TargetServerVersionInternal))
                    || !CanScriptCrossPlatform(node.Urn, preferences))
                {
                    incompatibleNodes.Add(node);
                }
            }

            foreach (DependencyCollectionNode node in incompatibleNodes)
            {
                depList.Remove(node);
                incompatibleObjects.Add(node.Urn);
            }

            if (incompatibleNodes.Any() && !this.Options.ContinueScriptingOnError)
            {
                throw new SmoException(ExceptionTemplates.UnsupportedVersionException);
            }
        }

        private bool CanScriptCrossPlatform(Urn urn, ScriptingPreferences preferences)
        {
            SqlSmoObject smoObject = null;
            if (preferences.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse
                && Database.Parent.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse
                && urn.Type == UserDefinedFunction.UrnSuffix)
            {
                smoObject = this.Database.Parent.GetSmoObject(urn);
                var udf = smoObject as UserDefinedFunction;
                if (udf != null && udf.FunctionType != UserDefinedFunctionType.Scalar)
                {
                    return false;
                }
            }

            if (!CopyExternalTables && ((Database.Parent.DatabaseEngineType != DatabaseEngineType.Standalone ||
                Database.Parent.VersionMajor >= 13)
                && urn.Type == Table.UrnSuffix))
            {
                smoObject = smoObject ?? this.Database.Parent.GetSmoObject(urn);
                var table = smoObject as Table;
                if (table != null)
                {
                    // external tables require working credentials, and databases created via Transfer don't have
                    // valid credentials
                    if (table.IsSupportedProperty("IsExternal") && table.IsExternal)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void TreeTraversal(DependencyTreeNode node, HashSet<Urn> visitedUrns)
        {
            if (visitedUrns.Add(node.Urn))
            {
                if (node.NextSibling != null)
                {
                    TreeTraversal(node.NextSibling, visitedUrns);
                }

                if (node.HasChildNodes)
                {
                    TreeTraversal(node.FirstChild, visitedUrns);
                }
            }
        }

        /// <summary>
        /// Used by GetObjectList() to update Default Init Fields for SMO types.
        /// Stores original Default Init Fields so that they can be restored later
        /// </summary>
        /// <param name="originalDefaultFields">Dictionary that contains {Type,DefaultFields} pairs</param>
        /// <param name="type">SMO Type</param>
        /// <param name="fields">Array of Default Init Fields for the type</param>
        private void SetDefaultInitFields(Dictionary<Type, StringCollection> originalDefaultFields, Type type, params string[] fields)
        {
            // Save original default fields
            originalDefaultFields[type] = Database.GetServerObject().GetDefaultInitFields(type);
            // Set new default fields
            Database.GetServerObject().SetDefaultInitFields(type, fields);
        }

        /// <summary>
        /// Used at the end of GetObjectList() to restore any Default Init Fields for SMO types
        /// that may have been changed by the funtion
        /// </summary>
        /// <param name="originalDefaultFields">Dictionary that contains {Type,DefaultFields} pairs</param>
        private void RestoreDefaultInitFields(Dictionary<Type, StringCollection> originalDefaultFields)
        {
            if (originalDefaultFields != null)
            {
                foreach (KeyValuePair<Type, StringCollection> pair in originalDefaultFields)
                {
                    Database.GetServerObject().SetDefaultInitFields(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>
        /// Returns true if this object can be scripted on downlevel versions
        /// Currently it's Yukon objects that may or may not be creatable on Shiloh
        /// databases
        /// (e.g. a sproc that receives an XML parameter will fail the test).
        /// </summary>
        /// <param name="urn"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        private bool CanScriptDownlevel(Urn urn, SqlServerVersionInternal targetVersion)
        {
            Sdk.Sfc.TraceHelper.Assert(null != urn, "null == urn");

            SqlSmoObject smoObject = this.Database.Parent.GetSmoObject(urn);

            if (smoObject is Table)
            {
                if (ContainsUnsupportedType(((Table) smoObject).Columns, targetVersion))
                {
                    return false;
                }
            }


            if (smoObject is View &&
                ContainsUnsupportedType(((View)smoObject).Columns, targetVersion))
            {
                return false;
            }

            if (smoObject is StoredProcedure)
            {
                StoredProcedure o = (StoredProcedure)smoObject;
                if (ContainsUnsupportedType(o.Parameters, targetVersion) ||
                    ((o.ImplementationType == ImplementationType.SqlClr) && (targetVersion < SqlServerVersionInternal.Version90)))
                {
                    return false;
                }
            }

            if (smoObject is UserDefinedFunction)
            {
                UserDefinedFunction o = (UserDefinedFunction)smoObject;
                if (ContainsUnsupportedType(o.Parameters, targetVersion) ||
                   ((o.DataType != null) && (o.FunctionType == UserDefinedFunctionType.Scalar) && IsUnsupportedType(o.DataType.SqlDataType, targetVersion)) ||
                   ((o.ImplementationType == ImplementationType.SqlClr) && (targetVersion < SqlServerVersionInternal.Version90)))
                {
                    return false;
                }
            }
            

            if (smoObject is UserDefinedDataType)
            {
                // do we need to check for other base types?
                if ((((UserDefinedDataType)smoObject).SystemType == "xml") && (targetVersion < SqlServerVersionInternal.Version90))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsUnsupportedType(SqlDataType type, SqlServerVersionInternal targetVersion)
        {
            if (targetVersion == SqlServerVersionInternal.Version80)
            {
                switch (type)
                {
                    case SqlDataType.Xml:
                    case SqlDataType.VarCharMax:
                    case SqlDataType.NVarCharMax:
                    case SqlDataType.VarBinaryMax:
                    case SqlDataType.HierarchyId:
                    case SqlDataType.UserDefinedTableType:
                    case SqlDataType.Geometry:
                    case SqlDataType.Geography:
                        return true;

                    default:
                        return false;
                }
            }
            else //SqlServerVersionInternal.Version90
            {
                switch (type)
                {
                    //all new katmai datatypes should be here
                    case SqlDataType.HierarchyId:
                    case SqlDataType.UserDefinedTableType:
                    case SqlDataType.Geometry:
                    case SqlDataType.Geography:
                        return true;

                    default:
                        return false;
                }
            }

        }

        private bool ContainsUnsupportedType(ParameterCollectionBase parms, SqlServerVersionInternal targetVersion)
        {
            foreach (Parameter p in parms)
            {
                if (IsUnsupportedType(p.DataType.SqlDataType, targetVersion))
                {
                    return true;
                }
            }
            return false;
        }

        private bool ContainsUnsupportedType(ColumnCollection cols, SqlServerVersionInternal targetVersion)
        {
            foreach (Column col in cols)
            {
                if (IsUnsupportedType(col.DataType.SqlDataType, targetVersion))
                {
                    return true;
                }
            }
            return false;
        }

        private DependencyCollection GetDependencyOrderedCollection(HashSet<Urn> transferSet)
        {
            SmoDependencyOrderer dependencyOrderer = new SmoDependencyOrderer(this.Database.GetServerObject());
            dependencyOrderer.ScriptingPreferences = (ScriptingPreferences)this.Options.GetScriptingPreferences().Clone();
            dependencyOrderer.ScriptingPreferences.IncludeScripts.Data = this.CopyData;
            dependencyOrderer.ScriptingPreferences.IncludeScripts.Ddl = this.CopySchema;
            dependencyOrderer.ScriptingPreferences.Behavior = ScriptBehavior.Create;
            dependencyOrderer.ScriptingPreferences.IncludeScripts.Associations = false;
            dependencyOrderer.ScriptingPreferences.IncludeScripts.Permissions = false;
            dependencyOrderer.ScriptingPreferences.IncludeScripts.Owner = false;
            dependencyOrderer.creatingDictionary = new CreatingObjectDictionary(this.Database.GetServerObject());

            List<Urn> transferList = new List<Urn>();
            transferList.AddRange(transferSet);

            List<Urn> orderedList = dependencyOrderer.Order(transferList);

            DependencyCollection result = new DependencyCollection();

            if (this.CopySchema)
            {
                foreach (var urn in orderedList)
                {
                    //remove special entries
                    if (!urn.Type.Equals("Special"))
                    {
                        result.Add(new DependencyCollectionNode(urn, true, true));
                    }
                    else if (urn.Parent.Type == "Object")
                    {
                        //Add special entries if they are object ones
                        result.Add(new DependencyCollectionNode(urn.Parent.Parent, true, true));
                    }
                }
            }
            else
            {
                foreach (var urn in orderedList)
                {
                    //Change data entries to table entries

                    Sdk.Sfc.TraceHelper.Assert((urn.Type.Equals("Special") && urn.Parent.Type == "Data"), "only data entries expected");

                    result.Add(new DependencyCollectionNode(urn.Parent.Parent, true, true));
                }
            }

            return result;
        }

#endregion

#region Scripting Error Reporting and Events
        public event ProgressReportEventHandler DiscoveryProgress
        {
            add { this.Scripter.DiscoveryProgress += value; }
            remove { this.Scripter.DiscoveryProgress -= value; }
        }

        public event ProgressReportEventHandler ScriptingProgress
        {
            add { this.Scripter.ScriptingProgress += value; }
            remove { this.Scripter.ScriptingProgress -= value; }
        }

        public event ScriptingErrorEventHandler ScriptingError
        {
            add { this.Scripter.ScriptingError += value; }
            remove { this.Scripter.ScriptingError -= value; }
        }

#endregion
    }
}

