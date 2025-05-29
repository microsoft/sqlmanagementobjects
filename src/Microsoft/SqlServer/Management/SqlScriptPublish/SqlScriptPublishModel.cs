// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Reflection;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// Class for describing the set of database objects to script and how to script them
    /// </summary>
    public class SqlScriptPublishModel
    {
        #region Constructor
        private SqlScriptPublishModel()
        {
            SqlScriptPublishModelTraceHelper.SetDefaultLevel(ComponentName, Microsoft.SqlServer.Management.Sdk.Sfc.SQLToolsCommonTraceLvl.L1);
        }

        /// <summary>
        /// Constructor for SSMS context.
        /// </summary>
        /// <param name="sqlConnectionInfo">SQL connection info</param>
        /// <param name="databaseName">database name</param>
        /// <param name="shellScriptingOptions">Default shell scripting options</param>
        public SqlScriptPublishModel(SqlConnectionInfo sqlConnectionInfo, string databaseName, IScriptPublishOptions shellScriptingOptions)
            : this()
        {
            if (sqlConnectionInfo == null)
            {
                throw new ArgumentNullException("sqlConnectionInfo");
            }
            if (shellScriptingOptions == null)
            {
                throw new ArgumentNullException("shellScriptingOptions");
            }

            this.shellScriptingOptions = shellScriptingOptions;

            this.databaseName = string.IsNullOrEmpty(databaseName) ? "master" : databaseName;
            try
            {
                ServerConnection serverConnection;
                if (sqlConnectionInfo is SqlConnectionInfoWithConnection sciwc)
                {
                    serverConnection = sciwc.ServerConnection.Copy().GetDatabaseConnection(databaseName);
                }
                else
                {
                    serverConnection = new ServerConnection(sqlConnectionInfo).GetDatabaseConnection(databaseName);
                }
                smoServer = new Smo.Server(serverConnection);

            }
            catch (Exception ex)
            {
                throw new SqlScriptPublishException(SR.UnableToConnect(sqlConnectionInfo.ServerName), ex);
            }
            InitServer();
            this.sqlQueryHandler = new SqlQueryHandler(this);

        }

        /// <summary>
        /// Constructor with connection string (VS/Powershell)
        /// </summary>
        /// <param name="connectionString">connection string</param>
        public SqlScriptPublishModel(string connectionString)
            : this()
        {
            SqlScriptPublishModelTraceHelper.Assert(!string.IsNullOrEmpty(connectionString), "connectionString is empty");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            SqlConnectionStringBuilder connBuilder = new SqlConnectionStringBuilder(connectionString);
            string serverName = connBuilder.DataSource;

            // Verify that we have a valid connection
            try
            {
                SqlConnectionInfo sci;
                if (connBuilder.IntegratedSecurity)
                {
                    sci = new SqlConnectionInfo(connBuilder.DataSource);
                }
                else
                {
                    sci = new SqlConnectionInfo(connBuilder.DataSource, connBuilder.UserID, connBuilder.Password);
                }
                this.smoServer = new Smo.Server(new ServerConnection(sci));
                InitServer();
                this.databaseName = connBuilder.InitialCatalog;
                if (string.IsNullOrEmpty(this.databaseName))
                {
                    this.databaseName = "master"; // default db if not specified
                }
            }
            catch (Exception ex)
            {
                throw new SqlScriptPublishException(SR.UnableToConnect(serverName), ex);
            }

            this.sqlQueryHandler = new SqlQueryHandler(this);
        }

        private void InitServer()
        {
            // tell the server object to initialize the Database with some expensive properties in one query
            // these are "least common denominator" since we don't know the edition of the db
            var initFields = Server.GetDefaultInitFields(typeof(Smo.Database), smoServer.DatabaseEngineEdition);
            initFields.AddRange(new string[] { "CompatibilityLevel", "IsMirroringEnabled", "Collation", nameof(Database.AnsiPaddingEnabled), "DefaultSchema" });
            smoServer.SetDefaultInitFields(typeof(Smo.Database), initFields);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Server name
        /// </summary>
        public string ServerName
        {
            get { return this.smoServer.Name; }
        }

        /// <summary>
        /// Currently selected database name
        /// </summary>
        public string DatabaseName
        {
            get { return this.databaseName; }
        }

        /// <summary>
        /// Object list for scripting that user selected
        /// </summary>
        public UrnCollection SelectedObjects
        {
            get
            {
                if (this.selectedObjects == null)
                {
                    this.selectedObjects = new UrnCollection();
                }
                return this.selectedObjects;
            }
        }

        /// <summary>
        /// Flag indicating if we should script the entire database
        /// </summary>
        public bool ScriptAllObjects
        {
            get { return this.scriptAllObjects; }
            set { this.scriptAllObjects = value; }
        }

        /// <summary>
        /// Flag indicating if we should skip the create database statement
        /// Normally this is set based on scriptAllObjects and EngineType
        /// but there are cases where it isn't desired, mostly testing.
        /// </summary>
        public bool SkipCreateDatabase
        {
            get { return this.skipCreateDatabase; }
            set { this.skipCreateDatabase = value; }
        }

        /// <summary>
        /// Type for generate/publish script
        /// </summary>
        public OutputType OutputType
        {
            get { return this.outputType; }
            set { this.outputType = value; }
        }

        /// <summary>
        /// Advanced scripting/publishing options
        /// </summary>
        public SqlScriptOptions AdvancedOptions
        {
            get
            {
                return this.GetAdvancedScriptingOptions();
            }
        }

        /// <summary>
        /// Get/sets raw script content which is used for clipboard/editor.
        /// </summary>
        public string RawScript
        {
            get { return this.rawScript; }
            set { this.rawScript = value; }
        }

        /// <summary>
        /// Gets/sets the option to script system objects
        /// </summary>
        public bool AllowSystemObjects { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Generate scripts to file, clipboard, or query window.
        /// </summary>
        public void GenerateScript(ScriptOutputOptions outputOptions)
        {
            if (string.IsNullOrEmpty(outputOptions.SaveFileName) &&
                (outputOptions.ScriptDestination == ScriptDestination.ToSingleFile || outputOptions.ScriptDestination == ScriptDestination.ToFilePerObject || outputOptions.ScriptDestination == ScriptDestination.ToNotebook))
            {
                throw new SqlScriptPublishException(SR.ValueIsNullOrEmpty("SaveFileName"));
            }

            SqlScriptGenerator scripter = new SqlScriptGenerator(this);
            scripter.GetUrnList();

            scripter.DoScript(outputOptions);
        }

        /// <summary>
        /// Returns eligible database object type names.
        /// </summary>
        /// <returns>Database object type names</returns>
        public IEnumerable<DatabaseObjectType> GetDatabaseObjectTypes()
        {
            return this.sqlQueryHandler.GetDatabaseObjectTypes();
        }

        /// <summary>
        /// Returns all children's object names and urns for the object type.
        /// </summary>
        /// <param name="objectType">Object type such as tables, views, etc</param>
        /// <returns>Object names and urns for the object type</returns>
        public IEnumerable<KeyValuePair<string, string>> EnumChildrenForDatabaseObjectType(DatabaseObjectType objectType)
        {
            return this.sqlQueryHandler.EnumChildrenForDatabaseObjectType(objectType);
        }
        #endregion

        #region Public Events
        /// <summary>
        /// Progress event for generate or publish script
        /// </summary>
        public event EventHandler<ScriptEventArgs> ScriptProgress;

        /// <summary>
        /// Error event for generate or publish script
        /// </summary>
        public event EventHandler<ScriptEventArgs> ScriptError;

        /// <summary>
        /// Database object items are all discovered
        /// </summary>
        public event EventHandler<ScriptItemsArgs> ScriptItemsCollected;
        #endregion

        #region Internal members
        /// <summary>
        /// SMO server object
        /// </summary>
        internal Smo.Server Server
        {
            get
            {
                SqlScriptPublishModelTraceHelper.Assert(this.smoServer != null);
                return this.smoServer;
            }
        }

        /// <summary>
        /// Shell scripting options
        /// </summary>
        internal IScriptPublishOptions ShellScriptingOptions
        {
            get { return this.shellScriptingOptions; }
        }

        /// <summary>
        /// Refresh database object containers (such as Tables, Views, etc.)
        /// </summary>
        public void RefreshDatabaseCache()
        {
            Database db = this.smoServer.Databases[this.databaseName];
            SfcMetadataDiscovery metadata = new SfcMetadataDiscovery(db.GetType());
            foreach (SfcMetadataRelation relation in metadata.Relations)
            {
                if (relation.Relationship == SfcRelationship.ObjectContainer)
                {
                    //Can optimize by going to property bag first
                    //if there is a guarantee that we'd always be dealing with SqlSmoObject
                    object relationObject = null;
                    try
                    {
                        PropertyInfo pi = db.GetType().GetProperty(relation.PropertyName);
                        relationObject = pi.GetValue(db, null);
                    }
                    catch (TargetInvocationException)
                    {
                    }

                    //if cannot retrieve value for current relation, skip it and its references
                    if (relationObject == null)
                    {
                        continue;
                    }

                    try
                    {
                        SmoCollectionBase smoCollection = relationObject as SmoCollectionBase;
                        smoCollection.Refresh();
                    }
                    catch (EnumeratorException)
                    {
                    }
                }
            }
        }

        internal void OnScriptError(object sender, Microsoft.SqlServer.Management.Smo.ScriptingErrorEventArgs e)
        {
            if (ScriptError != null)
            {
                Urn progressItem = this.GetSelectedItem(e.Current);
                this.erroredItems.Add(progressItem);
                ScriptEventArgs arg = new ScriptEventArgs(progressItem, e.InnerException);
                ScriptError(sender, arg);
            }
        }

        internal void OnScriptingProgress(object sender, Microsoft.SqlServer.Management.Smo.ScriptingProgressEventArgs e)
        {
            if (e.ProgressStage == ScriptingProgressStages.FilteringDone)
            {
                // Validate will throw if things aren't OK
                ValidateUrnList(e.Urns);

                return;
            }

            this.step = (this.AdvancedOptions.ScriptCreateDrop == SqlScriptOptions.ScriptCreateDropOptions.ScriptCreateDrop) ? 2 : 1;
            if ((ScriptItemsCollected != null) && (e.ProgressStage == ScriptingProgressStages.OrderingDone))
            {
                this.urnItemCount = new Dictionary<Urn, int>();
                List<Urn> uniqueUrns = new List<Urn>();

                foreach (Urn item in e.Urns)
                {
                    Urn progressItem = this.GetSelectedItem(item);
                    int count;
                    if (this.urnItemCount.ContainsKey(progressItem))
                    {
                        this.urnItemCount[progressItem] += this.step;
                    }
                    else
                    {
                        count = this.step;
                        this.urnItemCount.Add(progressItem, count);
                        uniqueUrns.Add(progressItem);
                    }
                }

                ScriptItemsCollected.Invoke(this, new ScriptItemsArgs(uniqueUrns));
            }
        }

        internal void OnObjectScriptingProgress(object sender, Microsoft.SqlServer.Management.Smo.ObjectScriptingEventArgs e)
        {
            if (ScriptProgress != null)
            {
                Urn progressItem = this.GetSelectedItem(e.Original);
                this.urnItemCount[progressItem]--;
                if(!this.erroredItems.Contains(progressItem))
                {
                    ScriptEventArgs arg = new ScriptEventArgs(progressItem, null, (this.urnItemCount[progressItem] == 0));
                    ScriptProgress(sender, arg);
                }
            }
        }
        #endregion

        #region Private functions
        private SqlScriptOptions GetAdvancedScriptingOptions()
        {
            if (this.scriptAdvancedOptions == null)
            {
                this.scriptAdvancedOptions = new SqlScriptOptions(this.Server.Version);
                IScriptPublishOptions shellScriptingOptions = this.ShellScriptingOptions;
                if (shellScriptingOptions != null)
                {
                    this.scriptAdvancedOptions.LoadShellScriptingOptions(shellScriptingOptions, this.smoServer);
                }

                if (this.ScriptAllObjects)
                {
                    SqlTransferOptions newOptions = new SqlTransferOptions(this.Server.Version);
                    newOptions.Copy(this.scriptAdvancedOptions);
                    newOptions.GenerateScriptForDependentObjects = SqlScriptOptions.BooleanTypeOptions.True;
                    newOptions.ScriptIndexes = SqlScriptOptions.BooleanTypeOptions.True;
                    this.scriptAdvancedOptions = newOptions;
                }
            }
            else if ((this.ScriptAllObjects && !(this.scriptAdvancedOptions is SqlTransferOptions))
                    || (!this.ScriptAllObjects && (this.scriptAdvancedOptions is SqlTransferOptions)))
            {
                // this is really dangerous that we just toss out all existing changes to the scriptAdvancedOptions
                // ideally we would just throw here or change the design but not sure of the impact on existing systems
                // so we'll assert for now
                SqlScriptPublishModelTraceHelper.Assert(false, "WARNING: all changes to AdvancedOptions have been lost due to changes in ScriptAllObjects.");
                this.scriptAdvancedOptions = null;
                return GetAdvancedScriptingOptions();
            }

            return this.scriptAdvancedOptions;
        }

        /// <summary>
        /// Checks the passed in URN list to make sure it is valid for the target server
        /// if not it throws an exception
        /// </summary>
        private void ValidateUrnList(IEnumerable<Urn> urnList)
        {
            // Raise errors if trying to script unsupported objects from box to cloud
            if (this.scriptAdvancedOptions.TargetDatabaseEngineType == SqlScriptOptions.ScriptDatabaseEngineType.SqlAzure
                && this.smoServer.DatabaseEngineType == DatabaseEngineType.Standalone)
            {
                HashSet<string> invalidTypeNames = new HashSet<string>();

                // get the list of invalid types and convert them into URN "string" types
                // so we can just quickly check the type of each URN in the passed in list against the invalid list
                DatabaseEngineEdition edition = DatabaseEngineEdition.SqlDatabase;
                switch (scriptAdvancedOptions.TargetDatabaseEngineEdition)
                {
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlDatawarehouseEdition:
                        edition = DatabaseEngineEdition.SqlDataWarehouse;
                        break;
                    case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerOnDemandEdition:
                        edition = DatabaseEngineEdition.SqlDatabase;
                        break;
                }

                foreach (DatabaseObjectType objectType in this.sqlQueryHandler.InvalidObjectTypesForAzure(edition))
                {
                    // if the URN type is not the same as the textual version of the DatabaseObjectType enum
                    // special handling shoudl be added here. That is not the case yet so we just use the string names
                    invalidTypeNames.Add(objectType.ToString());
                }

                // walk over each URN in the list and check it against the invalid names
                foreach (Urn urn in urnList)
                {
                    if (invalidTypeNames.Contains(urn.Type))
                    {
                        if (ScriptError != null)
                        {
                            // we need to send a ScriptError event so the progress bar will be updated correctly
                            // but then we also need to throw an exception so that the scripting process will stop
                            SqlScriptPublishException publishException = new SqlScriptPublishException(SR.InvalidObjectTypeForVersion(urn.GetAttribute("Name"), urn.Type));
                            ScriptEventArgs errorArgs = new ScriptEventArgs(null, publishException);
                            ScriptError(this, errorArgs);
                            throw publishException;
                        }
                    }
                }
            }

        }

        private Urn GetSelectedItem(Urn urn)
        {
            if (urn.Type.Equals("Special"))
            {
                return this.GetSelectedItem(urn.Parent.Parent);
            }
            else if ((urn.Parent == null) || (urn.Parent.Type.Equals("Server")) || (urn.Parent.Type.Equals("Database")))
            {
                return urn;
            }
            else
            {
                return this.GetSelectedItem(urn.Parent);
            }
        }
        #endregion

        #region Fields
        internal const string ComponentName = "DPW";
        private readonly SqlQueryHandler sqlQueryHandler;
        private readonly Smo.Server smoServer;
        private readonly string databaseName;
        private UrnCollection selectedObjects;
        private bool scriptAllObjects;
        private bool skipCreateDatabase;
        private string rawScript;
        private OutputType outputType = OutputType.GenerateScript;
        private IScriptPublishOptions shellScriptingOptions;
        private SqlScriptOptions scriptAdvancedOptions;
        private Dictionary<Urn, int> urnItemCount;
        private readonly HashSet<Urn> erroredItems = new HashSet<Urn>();
        private int step;
        #endregion

    }

    /// <summary>
    /// Output options for Scripting
    /// </summary>
    public class ScriptOutputOptions
    {
        public ScriptOutputOptions()
        {
        }

        /// <summary>
        /// Generate script output destination
        /// </summary>
        public ScriptDestination ScriptDestination { get; set; }

        /// <summary>
        /// Generate script unicode/ansi type
        /// </summary>
        public ScriptFileType SaveFileType { get; set; }

        /// <summary>
        /// Generate script file overwrite option. If ScriptDestination is set to ToNotebook, this
        /// property is ignored and any existing file will be overwritten.
        /// </summary>
        public ScriptFileMode SaveFileMode { get; set; }

        /// <summary>
        /// Output file name for save file option
        /// </summary>
        public string SaveFileName { get; set; }
    
        /// <summary>
        /// For file types that support it, whether to emit 
        /// human-friendly formatting instead of compacted text
        /// </summary>
        public bool Indented { get; set; }

        /// <summary>
        /// When ScriptDestination is set to ToCustomWriter, provides the ISmoScriptWriter implementation.
        /// </summary>
        public ISmoScriptWriter CustomSmoScriptWriter { get; set; }
    }

    /// <summary>
    /// Script event args for progress and error events
    /// </summary>
    public class ScriptEventArgs : EventArgs
    {
        private Urn urn;
        private Exception error;
        private bool continueScripting;
        private bool completed;

        public ScriptEventArgs(Urn urn, Exception error)
        {
            if (urn != null)
            {
                this.urn = urn;
            }
            this.error = error;
        }

        public ScriptEventArgs(Urn urn, Exception error, bool completed)
            : this(urn, error)
        {
            this.completed = completed;
        }

        public Urn Urn
        {
            get { return this.urn; }
        }

        public Exception Error
        {
            get { return this.error; }
        }

        public bool ContinueScripting
        {
            get { return this.continueScripting; }
            set { this.continueScripting = value; }
        }

        public bool Completed
        {
            get { return this.completed; }
        }
    }

    /// <summary>
    /// Script Urn items args
    /// </summary>
    public class ScriptItemsArgs : EventArgs
    {
        private IEnumerable<Urn> urns;

        public ScriptItemsArgs(IEnumerable<Urn> urns)
        {
            this.urns = urns;
        }

        public IEnumerable<Urn> Urns
        {
            get { return this.urns; }
        }
    }
}
