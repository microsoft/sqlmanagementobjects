// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using SmoEventSource = Microsoft.SqlServer.Management.Common.SmoEventSource;
using Microsoft.SqlServer.Server;
using Environment = System.Environment;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Provides methods to get SMO objects' scripts
    /// </summary>
    public class ScriptMaker
    {
        /// <summary>
        /// Server of objects to be scripted
        /// </summary>
        public Server Server { get; set; }

        /// <summary>
        /// Prefetch objects scripted or not
        /// </summary>
        public bool Prefetch { get; set; }

        /// <summary>
        /// Database engine edition of the source 
        /// </summary>
        public DatabaseEngineEdition? SourceDatabaseEngineEdition { get; set; } 

        private IDatabasePrefetch currentDatabasePrefetch;
        private string currentlyScriptingDatabase;
        private HashSet<Urn> inputList;
        private SortedList<string, HashSet<Urn>> perDatabaseUrns;
        private SortedList<string, bool> prefetchableObjects;
        private bool multipleDatabases;

        internal IDatabasePrefetch DatabasePrefetch { get; set; }

        private ScriptingErrorEventHandler scriptingError;

        /// <summary>
        /// Event to be fired in case of error while scripting
        /// </summary>
        public event ScriptingErrorEventHandler ScriptingError
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                scriptingError += value;
            }
            remove
            {
                scriptingError -= value;
            }
        }

        int totalObjectsToScript;

        HashSet<Urn> ObjectsToScript;

        private ObjectScriptingEventHandler objectScripting;

        /// <summary>
        /// Event to be fired after every object gets scripted
        /// </summary>
        internal event ObjectScriptingEventHandler ObjectScripting
        {
            add
            {
                objectScripting += value;
            }
            remove
            {
                objectScripting -= value;
            }
        }

        private ScriptingProgressEventHandler scriptingProgress;

        /// <summary>
        /// Event to be fired after every stage of scripting
        /// </summary>
        internal event ScriptingProgressEventHandler ScriptingProgress
        {
            add
            {
                scriptingProgress += value;
            }
            remove
            {
                scriptingProgress -= value;
            }
        }

        private RetryRequestedEventArgs currentRetryArgs;
        private RetryRequestedEventHandler retry;

        /// <summary>
        /// Event to be fired after scripting failure
        /// </summary>
        internal event RetryRequestedEventHandler Retry
        {
            add
            {
                retry += value;
            }
            remove
            {
                retry -= value;
            }
        }

        /// <summary>
        /// ScriptingPreferences for scripts generated
        /// </summary>
        public ScriptingPreferences Preferences { get; set; }

        /// <summary>
        /// The filter to apply before scripting
        /// </summary>
        internal ISmoFilter Filter { get; set; }

        /// <summary>
        /// Writer
        /// </summary>
        private ISmoScriptWriter writer;

        /// <summary>
        /// Discoverer
        /// </summary>
        internal ISmoDependencyDiscoverer discoverer;

        /// <summary>
        /// Gets or sets the object used for dependent object discovery.
        /// </summary>
        public ISmoDependencyDiscoverer Discoverer
        {
            get { return discoverer; }
            set { discoverer = value; }
        }

        /// <summary>
        /// Dictionary to store creating object references
        /// </summary>
        private CreatingObjectDictionary creatingDictionary;

        /// <summary>
        /// Dictionary to store scriptcontainers
        /// </summary>
        private ScriptContainerFactory scriptContainerFactory;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ScriptMaker()
        {
            this.Preferences = new ScriptingPreferences();
            this.discoverer = null;
            this.creatingDictionary = null;
            this.Prefetch = true;
        }

        /// <summary>
        /// Server Based constructor
        /// </summary>
        /// <param name="server">Server</param>
        public ScriptMaker(Server server)
        {
            if (null == server)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("server"));
            }

            this.Server = server;
            this.discoverer = null;
            this.Prefetch = true;
            this.creatingDictionary = new CreatingObjectDictionary(server);
            this.Preferences = new ScriptingPreferences(server);
        }

        /// <summary>
        /// Constructs a new ScriptMaker based on the given Server and ScriptingOptions combination
        /// </summary>
        /// <param name="server"></param>
        /// <param name="scriptingOptions"></param>
        public ScriptMaker(Server server, ScriptingOptions scriptingOptions) : this(server)
        {
            Preferences = scriptingOptions.GetScriptingPreferences();
        }

        #region Script Method overloads

        /// <summary>
        /// Script out object scripts to writer
        /// </summary>
        /// <param name="objects">Objects to be scripted</param>
        /// <param name="writer">Writer</param>
        private void Script(SqlSmoObject[] objects, ISmoScriptWriter writer)
        {
            if (null == objects)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("objects"));
            }

            this.StoreObjects(objects);

            List<Urn> Urns = new List<SqlSmoObject>(objects).ConvertAll(p => { return p.Urn; });
            this.ScriptWorker(Urns, writer);
        }

        /// <summary>
        /// Script out object script to Stringcollection
        /// </summary>
        /// <param name="objects">Objects to script</param>
        /// <returns></returns>
        public StringCollection Script(SqlSmoObject[] objects)
        {
            if (null == objects)
            {
                throw new ArgumentNullException("objects");
            }

            SmoStringWriter writer = new SmoStringWriter();
            this.Script(objects, writer);
            return writer.FinalStringCollection;
        }

        /// <summary>
        /// Script out the object script to writer
        /// </summary>
        /// <param name="urns">Objects' urns</param>
        /// <param name="writer">Writer</param>
        public void Script(Urn[] urns, ISmoScriptWriter writer)
        {
            if (null == urns)
            {
                throw new ArgumentNullException(nameof(urns));
            }

            if (null == writer)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            List<Urn> Urns = new List<Urn>(urns);
            this.ScriptWorker(Urns, writer);
        }

        /// <summary>
        /// Script out object script to StringCollection
        /// </summary>
        /// <param name="urns">Objects' urns</param>
        /// <returns></returns>
        public StringCollection Script(Urn[] urns)
        {
            if (null == urns)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("urns"));
            }

            SmoStringWriter writer = new SmoStringWriter();
            this.Script(urns, writer);
            return writer.FinalStringCollection;
        }

        /// <summary>
        /// Script out object script to writer
        /// </summary>
        /// <param name="list">UrnCollection wiht objects' urns</param>
        /// <param name="writer">Writer</param>
        internal void Script(UrnCollection list, ISmoScriptWriter writer)
        {
            if (null == list)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("list"));
            }

            List<Urn> Urns = new List<Urn>(list);
            this.ScriptWorker(Urns, writer);
        }

        internal void Script(DependencyCollection depList, SqlSmoObject[] objects, ISmoScriptWriter writer)
        {
            if (null == depList)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("list"));
            }

            if (objects != null)
            {
                this.StoreObjects(objects);
            }

            List<Urn> Urns = new List<Urn>();
            foreach (DependencyCollectionNode item in depList)
            {
                Urns.Add(item.Urn);
            }

            this.ScriptWorker(Urns, writer);
        }

        private void StoreObjects(SqlSmoObject[] objects)
        {
            if (objects.Length > 0)
            {
                this.creatingDictionary = new CreatingObjectDictionary(objects[0].GetServerObject());
                foreach (SqlSmoObject obj in objects)
                {
                    this.creatingDictionary.Add(obj);
                }
            }
        }

        /// <summary>
        /// Script out object script to StringCollection
        /// </summary>
        /// <param name="list">UrnCollection wiht objects' urns</param>
        /// <returns></returns>
        public StringCollection Script(UrnCollection list)
        {
            if (null == list)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("list"));
            }

            SmoStringWriter writer = new SmoStringWriter();
            this.Script(list, writer);
            return writer.FinalStringCollection;
        }

        internal StringCollection Script(SqlSmoObject obj)
        {
            throw new InvalidOperationException();
        }

        #endregion

        /// <summary>
        /// Worker Method
        /// </summary>
        /// <param name="urns"></param>
        /// <param name="writer"></param>
        void ScriptWorker(List<Urn> urns, ISmoScriptWriter writer)
        {
            // Only perform expensive string join operation if scripting logging is enabled
            if (SmoEventSource.Log.IsEnabled(EventLevel.Informational, SmoEventSource.Keywords.Scripting))
            {
                SmoEventSource.Log.ScriptWorkerInvoked(urns.Count, string.Join(Environment.NewLine + "\t", urns.Select(urn => urn.Value).ToArray()));
            }

            this.writer = writer ?? throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("writer"));
            this.currentRetryArgs = null;

            this.scriptContainerFactory = null;

            Verify(urns);
            this.OnScriptingProgress(ScriptingProgressStages.VerificationDone, urns);

            if (this.creatingDictionary == null)
            {
                this.creatingDictionary = new CreatingObjectDictionary(this.Server);
            }

            if(this.Preferences.IncludeScripts.ScriptingParameterHeader)
            {
                //For Azure servers don't display the version as it's hardcoded to
                //v12 (SQL 2014), so displaying it is just confusing for the user
                string headerString = string.Format(CultureInfo.CurrentUICulture,
@"/*    =={0}==

    {1}{2} : {3}
    {4} : {5}

    {6}{7} : {8}
    {9} : {10}
*/",
/*{0}*/LocalizableResources.ScriptingParameters,
/*{1}*/this.Server.DatabaseEngineType==DatabaseEngineType.SqlAzureDatabase ? string.Empty : string.Format(CultureInfo.CurrentUICulture, "{0} : {1} ({2}){3}    ",
    /*{0}*/LocalizableResources.SourceServerVersion,
    /*{1}*/Smo.TypeConverters.SqlServerVersionTypeConverter.ConvertToString(ScriptingOptions.ConvertVersion(this.Server.Version)),
    /*{2}*/this.Server.Version,
    /*{3}*/System.Environment.NewLine),
/*{2}*/LocalizableResources.SourceDatabaseEngineEdition,
/*{3}*/Common.TypeConverters.DatabaseEngineEditionTypeConverter.ConvertToString(this.SourceDatabaseEngineEdition ?? this.Server.DatabaseEngineEdition),
/*{4}*/LocalizableResources.SourceDatabaseEngineType,
/*{5}*/Common.TypeConverters.DatabaseEngineTypeTypeConverter.ConvertToString(this.Server.DatabaseEngineType),
/*{6}*/this.Preferences.TargetDatabaseEngineType==DatabaseEngineType.SqlAzureDatabase ? string.Empty : string.Format(CultureInfo.CurrentUICulture, "{0} : {1}{2}    ",
    /*{0}*/LocalizableResources.TargetServerVersion,
    /*{1}*/Smo.TypeConverters.SqlServerVersionTypeConverter.ConvertToString(this.Preferences.TargetServerVersion),
    /*{2}*/System.Environment.NewLine),
/*{7}*/LocalizableResources.TargetDatabaseEngineEdition,
/*{8}*/Common.TypeConverters.DatabaseEngineEditionTypeConverter.ConvertToString(this.Preferences.TargetDatabaseEngineEdition),
/*{9}*/LocalizableResources.TargetDatabaseEngineType,
/*{10}*/Common.TypeConverters.DatabaseEngineTypeTypeConverter.ConvertToString(this.Preferences.TargetDatabaseEngineType));

                // We could use a single-line format string but it makes the layout hard to read for a human, 
                // so if running on Linux we'll take the overhead of replacing the NewLines
                this.writer.Header = Environment.NewLine == "\r\n"
                    ? headerString
                    : headerString.Replace("\r\n", Environment.NewLine);
            }

            if (this.Preferences.ScriptForAlter)
            {
                this.ScriptAlterObjects(urns);
                return;
            }
            

            InitializeCurrentDatabasePrefetch();

            if (!(this.Server.IsDesignMode || this.Server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase) && this.Prefetch && this.currentDatabasePrefetch == null)
            {
                foreach (IEnumerable<Urn> urnSet in this.SingleDatabaseUrns(urns))
                {
                    DiscoverOrderScript(urnSet);
                }
            }
            else
            {
                DiscoverOrderScript(urns);
            }

            this.CleanUp();
        }

        private void CleanUp()
        {
            this.creatingDictionary = null;
            this.scriptContainerFactory = null;
        }

        private void InitializeCurrentDatabasePrefetch()
        {
            this.multipleDatabases = false;

            this.inputList = new HashSet<Urn>();

            if (this.Prefetch)
            {
                this.currentDatabasePrefetch = this.DatabasePrefetch;
            }
            else
            {
                this.currentDatabasePrefetch = null;
            }

            if (this.currentDatabasePrefetch != null)
            {
                this.currentDatabasePrefetch.creatingDictionary = this.creatingDictionary;

                GswDatabasePrefetch gswDatabasePrefetch = this.currentDatabasePrefetch as GswDatabasePrefetch;

                if (gswDatabasePrefetch != null)
                {
                    gswDatabasePrefetch.PrefetchBatchEvent += new PrefetchBatchEventHandler(OnPrefetchBatchEvent);
                }
            }
        }

        /// <summary>
        /// Method to initialize ScriptContainerFactory if required
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnPrefetchBatchEvent(object sender, PrefetchBatchEventArgs e)
        {
            if (e.TotalBatchCount > 1 && this.scriptContainerFactory == null)
            {
                HashSet<UrnTypeKey> filteredUrnTypes = new HashSet<UrnTypeKey>();
                SmoUrnFilter smoUrnFilter = this.Filter as SmoUrnFilter;
                if (smoUrnFilter != null)
                {
                    filteredUrnTypes = smoUrnFilter.filteredTypes;
                }
                this.scriptContainerFactory = new ScriptContainerFactory(this.Preferences, filteredUrnTypes, this.retry);
            }
        }

        private void DiscoverOrderScript(IEnumerable<Urn> urns)
        {
            IEnumerable<Urn> discoveredUrns = this.Discover(urns);
            this.OnScriptingProgress(ScriptingProgressStages.DiscoveryDone, discoveredUrns);

            if (this.multipleDatabases && this.Preferences.DependentObjects)
            {
                //filter out already found objects
                discoveredUrns = this.RemoveDuplicatesDiscovered(discoveredUrns);
            }

            //if filtering needed
            IEnumerable<Urn> filteredUrns = this.FilterUrns(discoveredUrns);

            if (filteredUrns == null)
            {
                return;
            }

            this.OnScriptingProgress(ScriptingProgressStages.FilteringDone, filteredUrns);

            List<Urn> orderedUrns = this.Order(filteredUrns);
            this.OnScriptingProgress(ScriptingProgressStages.OrderingDone, orderedUrns);

            if (this.objectScripting != null)
            {
                this.SetupObjectScriptingProgress(orderedUrns);
            }

            this.ScriptUrns(orderedUrns);
            this.OnScriptingProgress(ScriptingProgressStages.ScriptingCompleted, filteredUrns);
        }

        private IEnumerable<Urn> RemoveDuplicatesDiscovered(IEnumerable<Urn> discoveredUrns)
        {
            foreach (Urn urn in discoveredUrns)
            {
                if (!this.DuplicateUrn(urn))
                {
                    yield return urn;
                    this.inputList.Add(urn);
                }
            }
        }

        private bool DuplicateUrn(Urn urn)
        {
            //check if the database is
            XPathExpression xp = urn.XPathExpression;

            //Check if it is a server object
            if ((xp.Length < 3) || (xp.Length > 1 && xp[1].Name != "Database"))
            {
                return false;
            }

            string dbName = xp[1].GetAttributeFromFilter("Name");

            if (!dbName.Equals(this.currentlyScriptingDatabase))//use server's comparer
            {
                if (this.inputList.Contains(urn))
                {
                    return true;
                }
            }

            return false;
        }

        private void ScriptUrns(List<Urn> orderedUrns)
        {
            //call create/drop
            if (this.Preferences.Behavior == ScriptBehavior.Drop || this.Preferences.Behavior == ScriptBehavior.DropAndCreate)
            {
                List<Urn> reversedUrns = new List<Urn>(orderedUrns);
                reversedUrns.Reverse();

                this.ScriptDropObjects(reversedUrns);
            }

            if (this.Preferences.Behavior == ScriptBehavior.Create || this.Preferences.Behavior == ScriptBehavior.DropAndCreate)
            {
                var restoreExistenceCheck = Preferences.IncludeScripts.ExistenceCheck;
                // Bugfix 11293363 Because we already scripted the Drop, there's no need to check for existence
                // in the Create part of DropAndCreate
                if (this.Preferences.Behavior == ScriptBehavior.DropAndCreate && restoreExistenceCheck)
                {
                    Preferences.IncludeScripts.ExistenceCheck = false;
                }
                //if for create
                this.ScriptCreateObjects(orderedUrns);
                Preferences.IncludeScripts.ExistenceCheck = restoreExistenceCheck;
            }
            if (this.Preferences.Behavior == ScriptBehavior.CreateOrAlter)
            {
                this.ScriptAlterObjects(orderedUrns, isCreateOrAlter: true);
            }
        }

        private void Verify(List<Urn> urns)
        {
            this.CheckForConflictiongPreferences();

            this.VerifyInput(urns);

        }

        private IEnumerable<IEnumerable<Urn>> SingleDatabaseUrns(IEnumerable<Urn> urns)
        {
            List<Urn> serverObjectList = new List<Urn>();
            this.perDatabaseUrns = new SortedList<string, HashSet<Urn>>(this.Server.StringComparer);
            this.prefetchableObjects = new SortedList<string, bool>(this.Server.StringComparer);

            this.BucketizeUrns(urns, serverObjectList);

            if (perDatabaseUrns.Keys.Count < 1)
            {
                this.currentDatabasePrefetch = null;
                this.inputList.Clear();
                if (serverObjectList.Count > 0)
                {
                    yield return serverObjectList;
                }
                yield break;
            }
            else if (perDatabaseUrns.Keys.Count == 1)
            {
                this.currentDatabasePrefetch = this.GetDatabasePrefetch(perDatabaseUrns.Keys.First());
                this.inputList.Clear();
                yield return urns;
                yield break;
            }
            else
            {
                this.multipleDatabases = true;

                if (perDatabaseUrns.ContainsKey("master"))
                {
                    this.currentlyScriptingDatabase = "master";
                    perDatabaseUrns["master"].UnionWith(serverObjectList);
                    this.currentDatabasePrefetch = this.GetDatabasePrefetch("master");
                    yield return perDatabaseUrns["master"];
                    perDatabaseUrns.Remove("master");
                }
                else
                {
                    this.currentDatabasePrefetch = null;
                    this.currentlyScriptingDatabase = string.Empty;
                    if (serverObjectList.Count > 0)
                    {
                        yield return serverObjectList;
                    }
                }

                foreach (string database in perDatabaseUrns.Keys)
                {
                    this.currentlyScriptingDatabase = database;
                    this.currentDatabasePrefetch = this.GetDatabasePrefetch(database);
                    yield return perDatabaseUrns[database];
                }
            }
        }

        /// <summary>
        /// Bucketizes input list of urns based on database name
        /// </summary>
        /// <param name="urns"></param>
        /// <param name="serverObjectList"></param>
        private void BucketizeUrns(IEnumerable<Urn> urns, List<Urn> serverObjectList)
        {
            foreach (Urn urn in urns)
            {
                //add urn to the input list
                this.inputList.Add(urn);

                XPathExpression xp = urn.XPathExpression;

                //Check if it is a server object
                if ((xp.Length < 3) || (xp.Length > 1 && xp[1].Name != "Database"))
                {
                    serverObjectList.Add(urn);
                    continue;
                }

                string dbName = xp[1].GetAttributeFromFilter("Name");

                HashSet<Urn> urnSet;

                if (!this.perDatabaseUrns.TryGetValue(dbName, out urnSet))
                {
                    urnSet = new HashSet<Urn>();
                    this.perDatabaseUrns.Add(dbName, urnSet);
                    this.prefetchableObjects.Add(dbName, false);
                }

                //Creating state Object
                if (!this.creatingDictionary.ContainsKey(urn) && (xp[2].Name == Table.UrnSuffix || xp[2].Name == View.UrnSuffix))
                {
                    this.prefetchableObjects[dbName] = true;
                }

                this.perDatabaseUrns[dbName].Add(urn);
            }
        }

        private IDatabasePrefetch GetDatabasePrefetch(string databaseName)
        {
            IDatabasePrefetch dbPrefetch = null;
            Database db = this.Server.Databases[databaseName];

            if ((db != null) && (this.prefetchableObjects[databaseName]))
            {
                HashSet<UrnTypeKey> filteredTypes = GetFilteredTypes();

                dbPrefetch = new DefaultDatabasePrefetch(db, this.Preferences, filteredTypes);
                dbPrefetch.creatingDictionary = this.creatingDictionary;
            }
            return dbPrefetch;
        }

        private HashSet<UrnTypeKey> GetFilteredTypes()
        {
            HashSet<UrnTypeKey> filteredTypes = new HashSet<UrnTypeKey>();

            if (!this.Preferences.SfcChildren)
            {
                return ScriptingOptions.GetAllFilters(this.Server).filteredTypes;
            }
            else if (this.discoverer != null)
            {
                SmoDependencyDiscoverer depDiscoverer = this.discoverer as SmoDependencyDiscoverer;
                return (depDiscoverer != null) ? depDiscoverer.filteredUrnTypes : filteredTypes;
            }
            else if (this.Filter != null)
            {
                SmoUrnFilter urnFilter = this.Filter as SmoUrnFilter;
                return (urnFilter != null) ? urnFilter.filteredTypes : filteredTypes;
            }
            return filteredTypes;
        }

        private void OnScriptingProgress(ScriptingProgressStages scriptingProgressStages, IEnumerable<Urn> urns)
        {
            SmoEventSource.Log.ScriptingProgress(scriptingProgressStages.ToString());
            if (this.scriptingProgress != null)
            {
                this.scriptingProgress(this, new ScriptingProgressEventArgs(scriptingProgressStages, new List<Urn>(urns)));
            }
        }

        private void SetupObjectScriptingProgress(List<Urn> orderedUrns)
        {
            HashSet<Urn> urnHashSet = new HashSet<Urn>();
            foreach (var item in orderedUrns)
            {
                if (item.Type.Equals("Special"))
                {
                    urnHashSet.Add(item.Parent.Parent);
                }
                else
                {
                    urnHashSet.Add(item);
                }
            }

            this.totalObjectsToScript = urnHashSet.Count;
            this.ObjectsToScript = urnHashSet;
        }

        /// <summary>
        /// Assign SmoDependencyDiscoverer to discoverer
        /// </summary>
        private SmoDependencyDiscoverer SetupDiscoverer()
        {
            SmoDependencyDiscoverer dependencyDiscoverer = new SmoDependencyDiscoverer(this.Server);
            dependencyDiscoverer.Preferences = this.Preferences;
            dependencyDiscoverer.DatabasePrefetch = this.currentDatabasePrefetch;

            SmoUrnFilter smoUrnFilter = Filter as SmoUrnFilter;
            if (smoUrnFilter != null)
            {
                dependencyDiscoverer.filteredUrnTypes = smoUrnFilter.filteredTypes;
            }

            dependencyDiscoverer.creatingDictionary = this.creatingDictionary;
            dependencyDiscoverer.ChildrenDiscovery += new ChildrenDiscoveryEventHandler(this.OnChildrenDiscovery);
            return dependencyDiscoverer;
        }

        /// <summary>
        /// Method to store all discovered children and parent into scriptcontainer factory if required
        /// Also collapse the unique and primary keys into table script if possible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChildrenDiscovery(object sender, ChildrenDiscoveryEventArgs e)
        {
            if (this.scriptContainerFactory != null)
            {
                if (e.Parent.Type.Equals("Table"))
                {
                    Table tb = (Table)this.creatingDictionary.SmoObjectFromUrn(e.Parent);
                    //we need to do check for index collapsing
                    foreach (var item in e.Children)
                    {
                        if (!item.Type.Equals("Index") || (!this.AddIndexToTablePropagationList((Index)this.creatingDictionary.SmoObjectFromUrn(item))))
                        {
                            this.scriptContainerFactory.AddContainer(this.creatingDictionary.SmoObjectFromUrn(item));
                        }
                        else
                        {
                            tb.AddToIndexPropagationList((Index)this.creatingDictionary.SmoObjectFromUrn(item));
                        }
                    }

                    this.scriptContainerFactory.AddContainer(this.creatingDictionary.SmoObjectFromUrn(e.Parent));
                }
                else
                {
                    this.scriptContainerFactory.AddContainer(this.creatingDictionary.SmoObjectFromUrn(e.Parent));
                    foreach (var item in e.Children)
                    {
                        this.scriptContainerFactory.AddContainer(this.creatingDictionary.SmoObjectFromUrn(item));
                    }
                }
            }
        }


        /// <summary>
        /// Method to verify if index script can be combined with table script
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool AddIndexToTablePropagationList(Index index)
        {
            //if no ddl is going to be scripted then collapsing is not required
            if (!this.Preferences.IncludeScripts.Ddl)
            {
                return false;
            }

            //if all tables are going to be filtered we do not need to do collapsing
            SmoUrnFilter smoUrnFilter = this.Filter as SmoUrnFilter;
            if (smoUrnFilter != null)
            {
                if (smoUrnFilter.filteredTypes.Contains(new UrnTypeKey(Table.UrnSuffix)))
                {
                    return false;
                }
            }

            //check if it is a key
            //check clustering if data scripting is being done
            Nullable<IndexKeyType> indexKeyType = index.GetPropValueOptional<IndexKeyType>("IndexKeyType");
            if ((indexKeyType.HasValue) && (indexKeyType.Value != IndexKeyType.None))
            {
                if (!this.Preferences.IncludeScripts.Data || SmoDependencyOrderer.IsFilestreamTable((Table)index.Parent, this.Preferences))
                {
                    return true;
                }
                else
                {
                    bool? indexIsClustered = index.GetPropValueOptional<bool>("IsClustered");
                    if (indexIsClustered.HasValue && indexIsClustered.Value)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Include/Exclude objects based on filter
        /// </summary>
        /// <param name="discoveredObjects"></param>
        /// <returns></returns>
        private IEnumerable<Urn> FilterUrns(IEnumerable<Urn> discoveredObjects)
        {
            if (this.Filter == null)
            {
                return discoveredObjects;
            }

            return this.Filter.Filter(discoveredObjects);
        }

        /// <summary>
        /// Perform dependency discovery
        /// </summary>
        /// <param name="urns"></param>
        /// <returns></returns>
        private IEnumerable<Urn> Discover(IEnumerable<Urn> urns)
        {
            IEnumerable<Urn> discoveredUrns;
            if (this.discoverer == null)
            {
                SmoDependencyDiscoverer dependencyDiscoverer = this.SetupDiscoverer();
                discoveredUrns = dependencyDiscoverer.Discover(urns);
            }
            else
            {
                SmoDependencyDiscoverer dependencyDiscoverer = this.discoverer as SmoDependencyDiscoverer;
                if (dependencyDiscoverer != null)
                {
                    dependencyDiscoverer.creatingDictionary = this.creatingDictionary;
                    dependencyDiscoverer.DatabasePrefetch = this.currentDatabasePrefetch;
                    dependencyDiscoverer.ChildrenDiscovery += new ChildrenDiscoveryEventHandler(OnChildrenDiscovery);
                }
                discoveredUrns = this.discoverer.Discover(urns);
            }
            // Only perform expensive string join operation if scripting logging is enabled
            if (SmoEventSource.Log.IsEnabled(EventLevel.Informational, SmoEventSource.Keywords.Scripting))
            {
                SmoEventSource.Log.UrnsDiscovered(discoveredUrns.Count(), string.Join(Environment.NewLine + "\t", discoveredUrns.Select(urn => urn.Value).ToArray()));
            }
            return discoveredUrns;
        }

        /// <summary>
        /// Executable Order of the objects
        /// </summary>
        /// <param name="filteredObjects"></param>
        /// <returns></returns>
        private List<Urn> Order(IEnumerable<Urn> filteredObjects)
        {
            SmoDependencyOrderer dependencyOrderer = this.SetupOrderer();
            return dependencyOrderer.Order(filteredObjects);
        }

        private SmoDependencyOrderer SetupOrderer()
        {
            SmoDependencyOrderer dependencyOrderer = new SmoDependencyOrderer(this.Server);
            dependencyOrderer.ScriptingPreferences = this.Preferences;
            dependencyOrderer.creatingDictionary = this.creatingDictionary;
            dependencyOrderer.ScriptContainerFactory = this.scriptContainerFactory;
            return dependencyOrderer;
        }

        /// <summary>
        /// Verify input is scriptable
        /// </summary>
        /// <param name="urns"></param>
        private void VerifyInput(List<Urn> urns)
        {
            //verify input supported for scripting
            if (this.Preferences.ContinueOnScriptingError)
            {
                urns.RemoveAll(p => { return (string.IsNullOrEmpty(p) || !this.Scriptable(p)); });
            }
            else
            {
                foreach (Urn item in urns)
                {
                    if (string.IsNullOrEmpty(item) || (!this.Scriptable(item)))
                    {
                        //To-do create new objects
                        throw new FailedOperationException(ExceptionTemplates.Script, this,
                         new SmoException(ExceptionTemplates.CantScriptObject(item ?? string.Empty)));
                    }
                }
            }

        }

        /// <summary>
        /// Call Object's script create
        /// </summary>
        /// <param name="urns"></param>
        private void ScriptCreateObjects(IEnumerable<Urn> urns)
        {
            int count = 0;
            HashSet<Urn> urnHashSet = new HashSet<Urn>();
            // Only perform expensive string join operation if scripting logging is enabled
            if (SmoEventSource.Log.IsEnabled(EventLevel.Informational, SmoEventSource.Keywords.Scripting))
            {
                SmoEventSource.Log.ScriptCreateObjects(urns.Count(), string.Join(Environment.NewLine + "\t", urns.Select(u => u.Value).ToArray()));
            }

            foreach (Urn urn in urns)
            {
                ObjectScriptingType scriptType = ObjectScriptingType.None;
                try
                {
                    try
                    {
                        ScriptCreate(urn, this.Preferences, ref scriptType);
                    }
                    catch (Exception e)
                    {
                        if (e is OutOfMemoryException || this.retry == null)
                        {
                            throw;
                        }
                        else
                        {
                            RetryRequestedEventArgs retryEventArgs = new RetryRequestedEventArgs(urn, (ScriptingPreferences)this.Preferences.Clone());
                            this.retry(this, retryEventArgs);
                            if (retryEventArgs.ShouldRetry == true)
                            {
                                this.currentRetryArgs = retryEventArgs;
                                ScriptCreate(urn, retryEventArgs.ScriptingPreferences, ref scriptType);
                            }
                        }
                    }
                    finally
                    {
                        this.currentRetryArgs = null;
                    }
                }
                catch (Exception e) when (!ThrowException(urn, e))
                {
                }

                if (this.objectScripting != null)
                {
                    Urn objecturn = urn.Type.Equals("Special") ? urn.Parent.Parent : urn;

                    if (urnHashSet.Add(objecturn))
                    {
                        count++;
                    }

                    this.objectScripting(this, new ObjectScriptingEventArgs(objecturn, urn, count, this.totalObjectsToScript, scriptType));
                }
                SmoEventSource.Log.ScriptCreateComplete(urn.ToString());
            }
        }

        private void ScriptCreate(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType)
        {
            ScriptContainer scriptContainer;
            if (!urn.Type.Equals("Special"))
            {
                if (this.scriptContainerFactory != null && this.scriptContainerFactory.TryGetValue(urn, out scriptContainer))
                {
                    ScriptCreateStoredObject(urn, sp, ref scriptType, scriptContainer);
                }
                else
                {
                    ScriptCreateObject(urn, sp, ref scriptType);
                }
            }
            else if (urn.Parent.Type == "UnresolvedEntity")
            {
                ScriptCreateUnresolvedEntity(urn, ref scriptType);
            }
            else
            {
                if (this.scriptContainerFactory != null && this.scriptContainerFactory.TryGetValue(urn.Parent.Parent, out scriptContainer))
                {
                    ScriptDataStoredObject(urn.Parent.Parent, sp, ref scriptType, scriptContainer);
                }
                else
                {
                    ScriptCreateSpecialUrn(urn, sp, ref scriptType);
                }
            }
        }

        private static ScriptingPreferences CloneScriptingPreferencesForSpecialUrns(ScriptingPreferences sp)
        {
            ScriptingPreferences spclone = (ScriptingPreferences)sp.Clone();
            spclone.IncludeScripts.Owner = false;
            spclone.IncludeScripts.Associations = false;
            spclone.IncludeScripts.Permissions = false;
            spclone.IncludeScripts.CreateDdlTriggerDisabled = true;
            return spclone;
        }

        private void ScriptDataStoredObject(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType, ScriptContainer scriptContainer)
        {
            //script data and bindings
            TableScriptContainer tableScriptContainer = (TableScriptContainer)scriptContainer;
            if (sp.IncludeScripts.DatabaseContext)
            {
                this.writer.ScriptContext(tableScriptContainer.DatabaseContext, urn);
            }

            if (tableScriptContainer.DataScript != null)
            {
                this.ScriptDataToWriter(tableScriptContainer.DataScript, urn);
            }

            if (sp.IncludeScripts.Ddl && sp.OldOptions.Bindings)
            {
                ScriptObjectToWriter(tableScriptContainer.BindingsScript.Script, urn);
            }

            scriptType = ObjectScriptingType.Data;
        }

        private void ScriptCreateStoredObject(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType, ScriptContainer scriptContainer)
        {
            if (scriptContainer.CreateScript.Script.Count > 0)
            {
                if (sp.IncludeScripts.DatabaseContext)
                {
                    this.writer.ScriptContext(scriptContainer.DatabaseContext, urn);
                }
                ScriptObjectToWriter(scriptContainer.CreateScript.Script, urn);
            }
            scriptType = ObjectScriptingType.All;
        }

        private void ScriptCreateObject(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType)
        {
            StringCollection createCollection = new StringCollection();

            SqlSmoObject obj = this.creatingDictionary.SmoObjectFromUrn(urn);

            if (IsFiltered(obj, sp))
            {
                return;
            }
            CheckCloudSupport(obj, sp);

            //Initialize properties for scripting
            obj.InitializeKeepDirtyValues();

            obj.ScriptCreateInternal(createCollection, sp, true);
            ScriptObjectToWriterWithContext(createCollection, sp, obj);
            scriptType = ObjectScriptingType.All;
        }

        private void ScriptCreateUnresolvedEntity(Urn urn, ref ObjectScriptingType scriptType)
        {
            StringCollection createCollection = new StringCollection();
            string comment = string.Format(SmoApplication.DefaultCulture, @"/****** Cannot script Unresolved Entities : {0} ******/", urn.Parent.Parent.ToString());
            createCollection.Add(comment);
            ScriptObjectToWriter(createCollection, urn.Parent);
            scriptType = ObjectScriptingType.Comment;
        }

        private void ScriptCreateSpecialUrn(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType)
        {
            StringCollection createCollection = new StringCollection();
            SqlSmoObject obj = this.creatingDictionary.SmoObjectFromUrn(urn.Parent.Parent);

            if (IsFiltered(obj, sp))
            {
                return;
            }
            CheckCloudSupport(obj, sp);

            //Initialize properties for scripting
            obj.InitializeKeepDirtyValues();

            switch (urn.Parent.Type)
            {
                case "Object":
                    obj.ScriptCreateInternal(createCollection, CloneScriptingPreferencesForSpecialUrns(sp), true);
                    ScriptObjectToWriterWithContext(createCollection, sp, obj);
                    scriptType = ObjectScriptingType.Object;
                    break;
                case "Data":
                    this.ScriptDatabaseContextToWriter(obj, sp, false);
                    Table t = obj as Table;
                    if (t != null)
                    {
                        this.ScriptDataToWriter(t.ScriptDataInternal(sp), t.Urn);

                        if (sp.IncludeScripts.Ddl && sp.OldOptions.Bindings)
                        {
                            t.ScriptBindings(createCollection, sp);
                            ScriptObjectToWriterWithContext(createCollection, sp, obj);
                        }
                    }
                    scriptType = ObjectScriptingType.Data;
                    break;
                case "Ownership":
                    NamedSmoObject namedObject = obj as NamedSmoObject;
                    if (namedObject != null)
                    {
                        namedObject.ScriptChangeOwner(createCollection, sp);
                        ScriptObjectToWriterWithContext(createCollection, sp, obj);
                    }
                    scriptType = ObjectScriptingType.OwnerShip;
                    break;
                case "Associations":
                    obj.ScriptAssociationsInternal(createCollection, sp);
                    ScriptObjectToWriterWithContext(createCollection, sp, obj);
                    scriptType = ObjectScriptingType.Association;
                    break;
                case "Permission":
                    obj.AddScriptPermission(createCollection, sp);
                    if (createCollection.Count > 0)
                    {
                        this.ScriptDatabaseContextToWriter(obj, sp, true);
                        ScriptObjectToWriter(createCollection, obj.Urn);
                    }
                    scriptType = ObjectScriptingType.Permission;
                    break;
                case "ddltriggerdatabaseenable":
                case "ddltriggerserverenable":
                    DdlTriggerBase ddlTrigger = obj as DdlTriggerBase;
                    if (ddlTrigger != null)
                    {
                        bool isEnabled = ddlTrigger.GetPropValueOptional<bool>("IsEnabled", true);

                        if (isEnabled)
                        {
                            createCollection.Add(ddlTrigger.ScriptEnableDisableCommand(true, sp));
                            ScriptObjectToWriterWithContext(createCollection, sp, obj);
                        }
                    }
                    scriptType = ObjectScriptingType.Object;
                    break;
                case "databasereadonly":
                    Database database = obj as Database;
                    if (database != null)
                    {
                        bool? prop = database.GetPropValueOptional<bool>("ReadOnly");
                        if (prop.HasValue)
                        {
                            database.ScriptAlterPropReadonly(createCollection, sp, prop.Value);
                            ScriptObjectToWriterWithContext(createCollection, sp, obj);
                        }
                    }
                    scriptType = ObjectScriptingType.Object;
                    break;
                default:
                    break;
            }
            return;
        }

        private void ScriptDataToWriter(IEnumerable<string> dataScripts, Urn urn)
        {
            if (this.currentRetryArgs != null)
            {
                writer.ScriptData(SurroundWithRetryTexts(dataScripts, this.currentRetryArgs), urn);
            }
            else
            {
                writer.ScriptData(dataScripts, urn);
            }
        }

        internal static IEnumerable<string> SurroundWithRetryTexts(IEnumerable<string> dataScripts, RetryRequestedEventArgs retryRequestedEventArgs)
        {
            EnumerableContainer scriptEnumerable = new EnumerableContainer();

            if (!string.IsNullOrEmpty(retryRequestedEventArgs.PreText))
            {
                scriptEnumerable.Add(new string[] { retryRequestedEventArgs.PreText });
            }

            scriptEnumerable.Add(dataScripts);

            if (!string.IsNullOrEmpty(retryRequestedEventArgs.PostText))
            {
                scriptEnumerable.Add(new string[] { retryRequestedEventArgs.PostText });
            }

            return scriptEnumerable;
        }

        private void ScriptObjectToWriterWithContext(StringCollection scriptCollection, ScriptingPreferences sp, SqlSmoObject obj)
        {
            if (scriptCollection.Count > 0)
            {
                this.ScriptDatabaseContextToWriter(obj, sp, false);
                ScriptObjectToWriter(scriptCollection, obj.Urn);
            }
        }

        /// <summary>
        /// Script object's databasecontext
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sp"></param>
        /// <param name="isScriptingPermission"></param>
        private void ScriptDatabaseContextToWriter(SqlSmoObject obj, ScriptingPreferences sp, bool isScriptingPermission)
        {
            if (sp.TargetDatabaseEngineType != DatabaseEngineType.SqlAzureDatabase && sp.IncludeScripts.DatabaseContext)
            {
                string UseDB = ScriptDatabaseContext(obj, isScriptingPermission);
                this.writer.ScriptContext(UseDB, obj.Urn);
            }
        }

        internal static string ScriptDatabaseContext(SqlSmoObject obj, bool isScriptingPermission)
        {
            string dbName = obj.GetDBName();

            // When we want to drop/create a database object, we need
            // to switch to master
            //We have to use obj.GetDBNmae() instead of master when assigning permissions like GRANT CONNECT TO <username>
            if (((obj is Database) && (isScriptingPermission == false)) || (string.IsNullOrEmpty(dbName)))
            {
                dbName = "master";
            }

            string UseDB = string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlSmoObject.SqlBraket(dbName));
            return UseDB;
        }

        /// <summary>
        /// Call object's drop script
        /// </summary>
        /// <param name="urns"></param>
        private void ScriptDropObjects(IEnumerable<Urn> urns)
        {
            int count = 0;
            HashSet<Urn> urnHashSet = new HashSet<Urn>();

            foreach (Urn urn in urns)
            {
                ObjectScriptingType scriptType = ObjectScriptingType.None;
                try
                {
                    try
                    {
                        ScriptDrop(urn, this.Preferences, ref scriptType);
                    }
                    catch (Exception e)
                    {
                        if (e is OutOfMemoryException || this.retry == null)
                        {
                            throw;
                        }
                        else
                        {
                            RetryRequestedEventArgs retryEventArgs = new RetryRequestedEventArgs(urn, (ScriptingPreferences)this.Preferences.Clone());
                            this.retry(this, retryEventArgs);
                            if (retryEventArgs.ShouldRetry == true)
                            {
                                this.currentRetryArgs = retryEventArgs;
                                ScriptDrop(urn, retryEventArgs.ScriptingPreferences, ref scriptType);
                            }
                        }
                    }
                    finally
                    {
                        this.currentRetryArgs = null;
                    }
                }
                catch (Exception e)
                {
                    if (ThrowException(urn, e))
                    {
                        throw;
                    }
                }
                if (this.objectScripting != null)
                {
                    Urn objecturn = urn.Type.Equals("Special") ? urn.Parent.Parent : urn;

                    if (urnHashSet.Add(objecturn))
                    {
                        count++;
                    }

                    this.objectScripting(this, new ObjectScriptingEventArgs(objecturn, urn, count, this.totalObjectsToScript, scriptType));
                }
            }
        }

        private void ScriptDrop(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType)
        {
            ScriptContainer scriptContainer;

            if (!urn.Type.Equals("Special"))
            {
                if (this.scriptContainerFactory != null && this.scriptContainerFactory.TryGetValue(urn, out scriptContainer))
                {
                    ScriptDropStoredObject(urn, sp, ref scriptType, scriptContainer);
                }
                else
                {
                    ScriptDropObject(urn, sp, ref scriptType);
                }
            }
            else if (urn.Parent.Type != "UnresolvedEntity")
            {
                if (this.scriptContainerFactory != null && this.scriptContainerFactory.TryGetValue(urn.Parent.Parent, out scriptContainer))
                {
                    if (!sp.IncludeScripts.Ddl && sp.IncludeScripts.Data)
                    {
                        ScriptDropStoredObject(urn.Parent.Parent, sp, ref scriptType, scriptContainer);
                        scriptType = ObjectScriptingType.Data;
                    }
                }
                else
                {
                    ScriptDropSpecialUrn(urn, sp, ref scriptType);
                }
            }
        }

        private void ScriptDropStoredObject(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType, ScriptContainer scriptContainer)
        {
            if (scriptContainer.DropScript.Script.Count > 0)
            {
                if (sp.IncludeScripts.DatabaseContext)
                {
                    this.writer.ScriptContext(scriptContainer.DatabaseContext, urn);
                }
                ScriptObjectToWriter(scriptContainer.DropScript.Script, urn);
            }
            scriptType = ObjectScriptingType.All;
        }

        private void ScriptDropObject(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType)
        {
            StringCollection dropCollection = new StringCollection();

            SqlSmoObject obj = this.creatingDictionary.SmoObjectFromUrn(urn);

            if (IsFiltered(obj, sp))
            {
                return;
            }
            CheckCloudSupport(obj, sp);

            obj.ScriptDropInternal(dropCollection, sp);
            ScriptObjectToWriterWithContext(dropCollection, sp, obj);
            scriptType = ObjectScriptingType.All;
        }

        private void CheckCloudSupport(SqlSmoObject obj, ScriptingPreferences sp)
        {
            //check is supported for azure or not
            if (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase && !obj.IsCloudSupported)
            {
                throw new UnsupportedEngineTypeException(ExceptionTemplates.UnsupportedEngineTypeException);
            }
        }

        private bool IsFiltered(SqlSmoObject obj, ScriptingPreferences sp)
        {
            return ((!sp.ForDirectExecution) && obj.IgnoreForScripting) || (!sp.SystemObjects && this.IsSystemObject(obj));
        }

        private void ScriptDropSpecialUrn(Urn urn, ScriptingPreferences sp, ref ObjectScriptingType scriptType)
        {
            StringCollection dropCollection = new StringCollection();

            SqlSmoObject obj = this.creatingDictionary.SmoObjectFromUrn(urn.Parent.Parent);

            if (IsFiltered(obj, sp))
            {
                return;
            }
            CheckCloudSupport(obj, sp);

            switch (urn.Parent.Type)
            {
                case "Object":
                    obj.ScriptDropInternal(dropCollection, sp);
                    ScriptObjectToWriterWithContext(dropCollection, sp, obj);
                    scriptType = ObjectScriptingType.Object;
                    break;
                case "Data":
                    if (!sp.IncludeScripts.Ddl && sp.IncludeScripts.Data)
                    {
                        Table table = obj as Table;
                        if (table != null)
                        {
                            ScriptObjectToWriterWithContext(table.ScriptDropData(sp), sp, obj);
                            scriptType = ObjectScriptingType.Data;
                        }
                        scriptType = ObjectScriptingType.Data;
                    }
                    break;
                case "ddltriggerdatabasedisable":
                case "ddltriggerserverdisable":
                    DdlTriggerBase ddlTrigger = obj as DdlTriggerBase;
                    if (ddlTrigger != null)
                    {
                        dropCollection.Add(ScriptDdlTriggerDisable(ddlTrigger, sp));
                        ScriptObjectToWriterWithContext(dropCollection, sp, obj);
                    }
                    scriptType = ObjectScriptingType.Object;
                    break;
                default:
                    break;
            }
        }

        private string ScriptDdlTriggerDisable(DdlTriggerBase ddlTrigger, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder();
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendLine(ddlTrigger.GetIfNotExistStatement(sp, string.Empty));
            }
            sb.AppendLine(ddlTrigger.ScriptEnableDisableCommand(false, sp));
            return sb.ToString();
        }
        
        /// <summary>
        /// Call object's alter script
        /// </summary>
        /// <param name="urns"></param>
        /// <param name="isCreateOrAlter"></param>        
        private void ScriptAlterObjects(List<Urn> urns, bool isCreateOrAlter=false)
        {
            foreach (Urn urn in urns)
            {
                try
                {
                    StringCollection scriptCollection = new StringCollection();
                    SqlSmoObject obj = this.creatingDictionary.SmoObjectFromUrn(urn);

                    if (IsFiltered(obj, this.Preferences))
                    {
                        continue;
                    }

                    if (!isCreateOrAlter)
                    {
                        CheckCloudSupport(obj, this.Preferences);
                        //Initialize properties for scripting
                        obj.InitializeKeepDirtyValues();
                    }
                    
                    if (this.Preferences.IncludeScripts.Ddl)
                    {
                        if (isCreateOrAlter)
                        {
                            // Fix: https://github.com/microsoft/sqlmanagementobjects/issues/11
                            // Try to use CREATE OR ALTER syntax if ScriptingOptions asks for it
                            // Fall back to CREATE for objects that don't support it
                            if (obj is ICreateOrAlterable)
                            {
                                // throw an error when trying to use Create or alter syntax on old SQL versions
                                if (Preferences.TargetDatabaseEngineType == DatabaseEngineType.Standalone)
                                {
                                    SqlSmoObject.ThrowIfBelowVersion130(this.Preferences.TargetServerVersion, ExceptionTemplates.CreateOrAlterNotSupportedVersion);
                                }
                                var existenceCheck = Preferences.IncludeScripts.ExistenceCheck;
                                Preferences.IncludeScripts.ExistenceCheck = false;
                                try
                                {
                                    obj.ScriptCreateOrAlterInternal(scriptCollection, this.Preferences);
                                }
                                finally
                                {
                                    Preferences.IncludeScripts.ExistenceCheck = existenceCheck;
                                }
                            }
                            else
                            {
                                var currentBehavior = Preferences.Behavior;
                                Preferences.Behavior = ScriptBehavior.Create;
                                try
                                {
                                    obj.ScriptCreate(scriptCollection, Preferences);
                                }
                                finally
                                {
                                    Preferences.Behavior = currentBehavior;
                                }
                            }
                        }
                        else
                        {
                            obj.ScriptAlterInternal(scriptCollection, this.Preferences);
                        }
                        ScriptObjectToWriterWithContext(scriptCollection, this.Preferences, obj);
                    }
                }
                catch (Exception e)
                {
                    if (ThrowException(urn, e))
                    {
                        throw;
                    }
                }
            }
        }

        private void ScriptObjectToWriter(StringCollection stringCollection, Urn obj)
        {
            if (stringCollection.Count > 0)
            {
                SurroundWithRetryTexts(stringCollection, this.currentRetryArgs);
                EnumerableContainer scriptEnumerable = new EnumerableContainer();
                scriptEnumerable.Add(stringCollection);
                writer.ScriptObject(scriptEnumerable, obj);
            }
        }

        internal static void SurroundWithRetryTexts(StringCollection stringCollection, RetryRequestedEventArgs retryRequestedEventArgs)
        {
            if (retryRequestedEventArgs != null)
            {
                if (!string.IsNullOrEmpty(retryRequestedEventArgs.PreText))
                {
                    stringCollection.Insert(0, retryRequestedEventArgs.PreText);
                }

                if (!string.IsNullOrEmpty(retryRequestedEventArgs.PostText))
                {
                    stringCollection.Add(retryRequestedEventArgs.PostText);
                }
            }
        }

        private bool ThrowException(Urn urn, Exception e)
        {
            if ((e is OutOfMemoryException) || (!this.Preferences.ContinueOnScriptingError))
            {
                return true;
            }
            else
            {
                if (null != scriptingError)
                {
                    // fire an event
                    scriptingError(this, new ScriptingErrorEventArgs(urn.Type.Equals("Special") ? urn.Parent.Parent : urn, e));
                }
                return false;
            }
        }

        /// <summary>
        /// Check if object is system object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool IsSystemObject(SqlSmoObject obj)
        {
            if (obj.Properties.Contains("IsSystemObject"))
            {
                object isSystemObject = obj.Properties.GetValueWithNullReplacement("IsSystemObject", false, true);
                return (isSystemObject == null) ? false : (bool)isSystemObject;
            }
            else
            {
                return false;
            }
        }

        static readonly HashSet<string> scriptableTypes = new HashSet<string>
            {
                Table.UrnSuffix,
                View.UrnSuffix,
                Default.UrnSuffix,
                Rule.UrnSuffix,
                UserDefinedFunction.UrnSuffix,
                UserDefinedDataType.UrnSuffix,
                UserDefinedTableType.UrnSuffix,
                StoredProcedure.UrnSuffix,
                ExtendedStoredProcedure.UrnSuffix,
                User.UrnSuffix,
                DatabaseRole.UrnSuffix,
                XmlSchemaCollection.UrnSuffix,
                FullTextCatalog.UrnSuffix,
                FullTextStopList.UrnSuffix,
                SearchPropertyList.UrnSuffix,
                SearchProperty.UrnSuffix,
                FullTextIndex.UrnSuffix,
                FullTextIndexColumn.UrnSuffix,
                Schema.UrnSuffix,
                ApplicationRole.UrnSuffix,
                PlanGuide.UrnSuffix,
                DatabaseAuditSpecification.UrnSuffix,
                WorkloadManagementWorkloadGroup.UrnSuffix,
                SensitivityClassification.UrnSuffix,

                // 2nd tier objects can be scripted directly
                Check.UrnSuffix,
                EdgeConstraint.UrnSuffix,
                ForeignKey.UrnSuffix,
                Trigger.UrnSuffix,
                Index.UrnSuffix,
                Statistic.UrnSuffix,
                DefaultConstraint.UrnSuffix,
                DatabaseDdlTrigger.UrnSuffix,
                ServerDdlTrigger.UrnSuffix,

                // DatabaseMail scriptable objects
                Mail.SqlMail.UrnSuffix,
                Mail.MailProfile.UrnSuffix,
                Mail.MailAccount.UrnSuffix,
                Mail.MailServer.UrnSuffix,
                Mail.ConfigurationValue.UrnSuffix,

                // SQLAgent scriptable objects
                Agent.Job.UrnSuffix,
                Agent.JobStep.UrnSuffix,
                Agent.Operator.UrnSuffix,
                Agent.OperatorCategory.UrnSuffix,
                Agent.JobCategory.UrnSuffix,
                Agent.AlertCategory.UrnSuffix,
                Agent.JobSchedule.UrnSuffix,
                Agent.TargetServerGroup.UrnSuffix,
                Agent.Alert.UrnSuffix,
                BackupDevice.UrnSuffix,
                Agent.ProxyAccount.UrnSuffix,
                Agent.JobServer.UrnSuffix,
                Agent.AlertSystem.UrnSuffix,

                // objects that do not belong to a database
                Login.UrnSuffix,
                FullTextService.UrnSuffix,
                Database.UrnSuffix,
                UserDefinedMessage.UrnSuffix,
                LinkedServer.UrnSuffix,
                "HttpEndpoint",
                Endpoint.UrnSuffix,
                Settings.UrnSuffix,
                OleDbProviderSettings.UrnSuffix,
                UserOptions.UrnSuffix,
                "FileStreamSettings",
                Audit.UrnSuffix,
                ServerAuditSpecification.UrnSuffix,
                Server.UrnSuffix,

                // CLR objects
                UserDefinedType.UrnSuffix,
                UserDefinedAggregate.UrnSuffix,
                SqlAssembly.UrnSuffix,

                // External Language
                ExternalLanguage.UrnSuffix,

                // External Library
                ExternalLibrary.UrnSuffix,

                PartitionScheme.UrnSuffix,
                PartitionFunction.UrnSuffix,
                ExtendedProperty.UrnSuffix,
                Synonym.UrnSuffix,
                Sequence.UrnSuffix,

                // Service Broker objects
                Broker.MessageType.UrnSuffix,
                Broker.ServiceContract.UrnSuffix,
                Broker.BrokerService.UrnSuffix,
                Broker.BrokerPriority.UrnSuffix,
                Broker.ServiceQueue.UrnSuffix,
                Broker.ServiceRoute.UrnSuffix,
                Broker.RemoteServiceBinding.UrnSuffix,

                // Symmetric Key objects
                SymmetricKey.UrnSuffix,
                "KeyEncryption",

                // Resource Governor
                ResourceGovernor.UrnSuffix,
                ResourcePool.UrnSuffix,
                ExternalResourcePool.UrnSuffix,
                WorkloadGroup.UrnSuffix,

                CryptographicProvider.UrnSuffix,

                // Unresolved Entities
                "UnresolvedEntity",

                DatabaseEncryptionKey.UrnSuffix,

                // AlwaysOn
                AvailabilityGroup.UrnSuffix,
                AvailabilityReplica.UrnSuffix,
                AvailabilityDatabase.UrnSuffix,
                AvailabilityGroupListener.UrnSuffix,
                AvailabilityGroupListenerIPAddress.UrnSuffix,

                // Smart admin
                SmartAdmin.UrnSuffix,

                // Security Policy
                SecurityPolicy.UrnSuffix,

                // Column Master Key
                ColumnMasterKey.UrnSuffix,

                // Column Encryption Key
                ColumnEncryptionKey.UrnSuffix,

                // External Data Source
                ExternalDataSource.UrnSuffix,

                // External File Format
                ExternalFileFormat.UrnSuffix,

                //Query Store Options :
                QueryStoreOptions.UrnSuffix,

                DatabaseScopedCredential.UrnSuffix,

                WorkloadManagementWorkloadClassifier.UrnSuffix,

                // External Stream 
                ExternalStream.UrnSuffix,

                // External Streaming Job
                ExternalStreamingJob.UrnSuffix,

                //External Model
                ExternalModel.UrnSuffix

            };

        /// <summary>
        /// Verify Object is scriptable
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        private bool Scriptable(Urn urn)
        {
            return scriptableTypes.Contains(urn.Type);
        }

        /// <summary>
        /// Check for conflicting prefernces
        /// </summary>
        private void CheckForConflictiongPreferences()
        {
            // To-do create conflictingScriptingPreferences template and put that here
            if (Preferences.OldOptions.DdlBodyOnly && Preferences.OldOptions.DdlHeaderOnly)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.ConflictingScriptingOptions(
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.DdlBodyOnly),
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.DdlHeaderOnly)));
            }

            // Throw error if both ScriptData and ScriptSchema are false
            // Nothing would be scripted if both these are false, thus we are flagging this as error
            //
            if (!Preferences.IncludeScripts.Data &&
                !Preferences.IncludeScripts.Ddl)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.InvalidScriptingOutput(
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.ScriptData),
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.ScriptSchema)));
            }

            // Throw an error if ScriptSchema is false and ScriptForAlter is true.
            // This is because Data will not be scripted when ScriptForAlter is true, and if ScriptSchema
            // is also false, then nothing would be scripted at all. Flagging this as an error
            //
            if (!Preferences.IncludeScripts.Ddl &&
                Preferences.ScriptForAlter)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.ConflictingScriptingOptions(
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.ScriptSchema),
                    "ScriptForAlter"));
            }
        }
    }

    internal class CreatingObjectDictionary
    {
        /// <summary>
        /// Dictionary to store creating object references
        /// </summary>
        private Dictionary<Urn, SqlSmoObject> objectsStored;

        private Server server;

        public CreatingObjectDictionary(Server server)
        {
            if (server == null)
            {
                throw new SmoException("server", new ArgumentNullException("server"));
            }

            this.server = server;
            this.objectsStored = new Dictionary<Urn, SqlSmoObject>();
        }

        /// <summary>
        /// Add object to dictionary
        /// </summary>
        /// <param name="obj"></param>
        public void Add(SqlSmoObject obj)
        {
            if (((obj.State == SqlSmoState.Creating) || (obj.IsDesignMode)) && (!this.objectsStored.ContainsKey(obj.Urn)))
            {
                this.objectsStored.Add(obj.Urn, obj);
            }
        }

        /// <summary>
        /// Get Smo object from urn
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        public SqlSmoObject SmoObjectFromUrn(Urn urn)
        {
            if (this.objectsStored.ContainsKey(urn))
            {
                return this.objectsStored[urn];
            }
            else
            {
                return this.server.GetSmoObject(urn);
            }
        }

        /// <summary>
        /// Returns true if key is in the dictionary
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        public bool ContainsKey(Urn urn)
        {
            return this.objectsStored.ContainsKey(urn);
        }
    }

    /// <summary>
    /// Delegate to handle object scripting progress
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void ObjectScriptingEventHandler(object sender, ObjectScriptingEventArgs e);

    /// <summary>
    /// Object scripting progress event arguments class
    /// </summary>
    internal class ObjectScriptingEventArgs : EventArgs
    {
        internal ObjectScriptingEventArgs(Urn current, Urn original, int currentCount, int total, ObjectScriptingType scriptType)
        {
            this.Current = current;
            this.Original = original;
            this.CurrentCount = currentCount;
            this.Total = total;
            this.ScriptType = scriptType;
        }

        /// <summary>
        /// The urn of current object which was scripted
        /// </summary>
        public Urn Current { get; private set; }

        /// <summary>
        /// The original urn scripted
        /// </summary>
        public Urn Original { get; private set; }

        /// <summary>
        /// The current count of objects already scripted
        /// </summary>
        public int CurrentCount { get; private set; }

        /// <summary>
        /// Total count of object to be scripted
        /// </summary>
        public int Total { get; private set; }

        /// <summary>
        /// Type of Script
        /// </summary>
        public ObjectScriptingType ScriptType { get; private set; }
    }

    /// <summary>
    /// Enumeration for Script Type
    /// </summary>
    internal enum ObjectScriptingType
    {
        /// <summary>
        /// No Script
        /// </summary>
        None,

        /// <summary>
        /// Only Object Script
        /// </summary>
        Object,

        /// <summary>
        /// Data Script
        /// </summary>
        Data,

        /// <summary>
        /// Ownership Script
        /// </summary>
        OwnerShip,

        /// <summary>
        /// Association Script
        /// </summary>
        Association,

        /// <summary>
        /// Permission Script
        /// </summary>
        Permission,

        /// <summary>
        /// Comment Script
        /// </summary>
        Comment,

        /// <summary>
        /// All types of Script
        /// </summary>
        All
    }

    /// <summary>
    /// Delegate to handle report overall scripting progress
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void ScriptingProgressEventHandler(object sender, ScriptingProgressEventArgs e);

    /// <summary>
    /// Overall scripting progress event arguments class
    /// </summary>
    internal class ScriptingProgressEventArgs : EventArgs
    {
        internal ScriptingProgressEventArgs(ScriptingProgressStages progressStage, List<Urn> urnList)
        {
            this.ProgressStage = progressStage;
            this.urnList = urnList;
        }

        List<Urn> urnList;

        /// <summary>
        /// List of Urns
        /// </summary>
        public IList<Urn> Urns
        {
            get
            {
                return urnList.AsReadOnly();
            }
        }
        /// <summary>
        /// ProgressStage
        /// </summary>
        public ScriptingProgressStages ProgressStage { get; private set; }
    }

    /// <summary>
    /// Enumeration for Scripting Progress stages
    /// </summary>
    internal enum ScriptingProgressStages
    {
        /// <summary>
        /// Verification completed
        /// </summary>
        VerificationDone,

        /// <summary>
        /// Dependency discovery done
        /// </summary>
        DiscoveryDone,

        /// <summary>
        /// Filtering done
        /// </summary>
        FilteringDone,

        /// <summary>
        /// Ordering done
        /// </summary>
        OrderingDone,

        /// <summary>
        /// Scripting Completed
        /// </summary>
        ScriptingCompleted
    }

    /// <summary>
    /// Delegate to handle retrying of failed operation
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void RetryRequestedEventHandler(object sender, RetryRequestedEventArgs e);

    /// <summary>
    /// Retry event arguments
    /// </summary>
    internal class RetryRequestedEventArgs : EventArgs
    {
        public RetryRequestedEventArgs(Urn urn, ScriptingPreferences scriptingPreferences)
        {
            if (scriptingPreferences == null)
            {
                throw new ArgumentNullException("scriptingPreferences");
            }

            this.Urn = urn;
            this.ScriptingPreferences = scriptingPreferences;
        }

        public Urn Urn { get; private set; }

        /// <summary>
        /// Scripting preferences used to script this object, or retried with if changed.
        /// </summary>
        public ScriptingPreferences ScriptingPreferences { get; private set; }

        /// <summary>
        /// Should this scripting operation be retried
        /// </summary>
        public bool ShouldRetry { get; set; }

        /// <summary>
        /// Text which should be included before the script generated by the retry.
        /// This is used for informational comments.
        /// </summary>
        public string PreText { get; set; }

        /// <summary>
        /// Text which should be included after the script generated by the retry.
        /// This is used for information comments.
        /// </summary>
        public string PostText { get; set; }
    }
}
