// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Xml;
using System.IO;
using System.Data.Common;

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using XmlTextReader = System.Xml.XmlTextReader;
using System.Linq;
#if !STRACE
using STrace = System.Diagnostics.Trace;
#endif

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ServerGroupParent : SfcInstance
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed partial class RegisteredServersStore : ServerGroupParent, ISfcDomain
    {
        private static RegisteredServersStore localFileStore;
        private static object lockObject = new object();
        private bool isSerializeOnCreation = true;

        /// <summary>
        /// Delegate declaration for handling any exceptions in UI scenarios
        /// This is useful in cases where UI should show a user dialog and continue
        ///   with the rest of the code past the exception.
        /// </summary>
        public delegate bool ExceptionDelegate(object obj);

        /// <summary>
        /// Delegate member to hold the list of delegate handlers
        /// </summary>
        public static ExceptionDelegate ExceptionDelegates;

        /// <summary>
        /// This constructor is purely a place holder to allow the serialize to construct this
        /// object and set other properties on it.  This constructor will not be used for directly
        /// connecting to the local store. Instead we will use a static method to initialize the
        /// store from the XML file.
        /// </summary>
        public RegisteredServersStore()
        {
            // Pretend we have an xml storage file so that we're
            // considered IsLocal right away.
            localXmlStorageFile = string.Empty;
        }

        /// <summary>
        /// This constructor is used for a Store that represents storage in a SQL server instead
        /// of a file.
        /// </summary>
        /// <param name="sharedRegisteredServersStoreConnection"></param>
        public RegisteredServersStore(ServerConnection sharedRegisteredServersStoreConnection)
        {
            // Mark us as not local
            this.localXmlStorageFile = null;
            this.serverConnection = sharedRegisteredServersStoreConnection;
            MarkRootAsConnected();
        }

        /// <summary>
        /// This static property returns a Singleton instance representing the local file
        /// storage.
        /// </summary>
        public static RegisteredServersStore LocalFileStore
        {
            get
            {
                lock (lockObject)
                {
                    if (null == localFileStore)
                    {
                        localFileStore = InitializeLocalRegisteredServersStore(null);
                    }
                }

                return localFileStore;
            }
        }

        /// <summary>
        /// Discard the current local file store and reload it from disk
        /// </summary>
        /// <remarks>
        /// This isn't called "Refresh" because SfcInstance.Refresh does something
        /// completely different, like populating property bags, validating connections, etc.  
        /// Also, this method is just reloading the local file store.  Any non-local storage
        /// is unaffected by the method.
        /// </remarks>
        public static void ReloadLocalFileStore()
        {
            lock (lockObject)
            {
                localFileStore = null;

            }

            if (LocalFileStoreReloaded != null)
            {
                LocalFileStoreReloaded(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Event that is raised after the local file store is refreshed.  When
        /// this event is raised, clients need to release their references to 
        /// the store and reinitialize their UI for the new local file store. 
        /// </summary>
        public static event EventHandler LocalFileStoreReloaded;

        /// <summary>
        /// Initializes the store from the given configuration file.
        /// </summary>
        /// <param name="registeredServersXmlFile">The path to the XML file containing the registered servers. If null, the default file in the user profile will be used.</param>
        /// <returns></returns>
        public static RegisteredServersStore InitializeLocalRegisteredServersStore(string registeredServersXmlFile)
        {
            var localXmlStorageFile = registeredServersXmlFile ?? GetLocalXmlFilePath();

            // If the LocalXmlFile does not exist, and we are asked to use the default SSMS file, we try to see if an older file
            // (from a previous version of SSMS) exists on the machine for this user.
            // If it does, then we promote it to be the current file.
            if (registeredServersXmlFile == null && !File.Exists(localXmlStorageFile))
            {
                MigrateLocalXmlFileFromLegacySSMS(localXmlStorageFile);
            }

            // init the store from the file
            var root = InitChildObjects(localXmlStorageFile);

            // cache the file name so we know we're dealing with a local store
            root.localXmlStorageFile = localXmlStorageFile;

            return root;
        }

        /// <summary>
        /// Migrate the RegServer Store file from older versions of SSMS (18.x and earlier)
        /// - Try to deserialize older files that may be on the machine (e.g. installed by older versions of SSMS)
        /// - If the deserialization succeeds, then copy that file to the current location
        /// Note: starting with SSMS 18.0, the location is not versioned anymore.
        /// </summary>
        /// <param name="pathToMigratedLocalXmlFile"></param>
        private static void MigrateLocalXmlFileFromLegacySSMS(string pathToMigratedLocalXmlFile)
        {
            var possiblePaths = legacyFileNames.Select(p => Path.Combine(GetSettingsDir(savePathSuffix), p)).Where(path => File.Exists(path)).ToList();
            possiblePaths.AddRange(from ver in new[] { 140, 130, 120, 110, 100 }
                                   let possiblePath = Path.Combine(GetSettingsDir(string.Format(savePathLegacySuffixFormat, ver)), "RegSrvr.xml")
                                   where File.Exists(possiblePath)
                                   select possiblePath);
            foreach (var possiblePath in possiblePaths)
            {
                if (TryDeserializeLocalXmlFile(possiblePath, out var _, out var _))
                {
                    try
                    {
                        // Create all the intermediate folders to the target file, in case they do not exist
                        // (it we don't do that, File.Copy() is going to fail). Typically, this is when SSMS
                        // was not installed/run on the machine.
                        new FileInfo(pathToMigratedLocalXmlFile).Directory.Create();
                        File.Copy(possiblePath, pathToMigratedLocalXmlFile);
                    }
                    catch
                    {
                        // Swallow the exception and stop trying:
                        // we had a good file, but for some reason we were not able to copy it over.
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Tries to deserialize Local XML file.
        /// </summary>
        /// <param name="pathToLocalXmlFile">The full path to the file we are trying to deserialize</param>
        /// <param name="registeredServersStore">The object that represents the deserialized file</param>
        /// <param name="exception">The exception that was caught (if any); if no exception happened and the deserialization was successful, this is set to null</param>
        /// <returns>True if the file existed and could be deserialized; false otherwise</returns>
        private static bool TryDeserializeLocalXmlFile(string pathToLocalXmlFile, out RegisteredServersStore registeredServersStore, out Exception exception)
        {
            registeredServersStore = null;
            exception = null;

            if (File.Exists(pathToLocalXmlFile))
            {
                using (var reader = new XmlTextReader(pathToLocalXmlFile) { DtdProcessing = DtdProcessing.Prohibit })
                {
                    var sfcSerializer = new SfcSerializer();

                    try
                    {
                        registeredServersStore = (RegisteredServersStore)sfcSerializer.Deserialize(reader, SfcObjectState.Existing);
                        return true;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Helper function that inits the root's child objects
        /// from a file
        /// </summary>
        /// <param name="file"></param>
        private static RegisteredServersStore InitChildObjects(string file)
        {
            RegisteredServersStore registeredServersStore;
            Exception e;

            if (TryDeserializeLocalXmlFile(file, out registeredServersStore, out e))
            {
                return registeredServersStore;
            }
            else
            {
                // If an exception happened while trying to deserialize the file, allow the Delegates to handle it first.
                if (e != null)
                {
                    var regSvrException = new RegisteredServerException(RegSvrStrings.FailedToDeserialize, e);

                    if ((null == ExceptionDelegates)
                       || (!ExceptionDelegates(regSvrException)))
                    {
                        // If any of the delegates returned false, just throw the exception
                        throw regSvrException;
                    }
                }

                // If we could not deserialize we will return an empty store
                // Typically, this is the case when the file did not exist.
                return new RegisteredServersStore();
            }
        }

        /// <summary>
        /// Helper function that flushes the entire tree on the disk.
        /// </summary>
        internal void Serialize()
        {
            if (string.IsNullOrEmpty(this.localXmlStorageFile))
            {
                this.localXmlStorageFile = GetLocalXmlFilePath();
            }
            using (var writer = new XmlTextWriter(this.localXmlStorageFile, null))
            {
                writer.Formatting = Formatting.Indented;
                var sfcSerializer = new SfcSerializer();
                sfcSerializer.Serialize(this);
                try
                {
                    sfcSerializer.Write(writer);
                }
                catch (SfcSerializationException)
                {
                    // $ISSUE - If the write failed, then an invalid
                    // xml file is now on disk. We could delete it, or
                    // write to a temp file and then try to rename it
                    // if the write succeeds.
                    throw;
                }
            }
        }

        /// <summary>
        /// Exports the object and its subtree to a file.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="file"></param>
        /// <param name="cpt"></param>
        internal static void Export(SfcInstance obj, string file, CredentialPersistenceType cpt)
        {
            STrace.Assert((obj is RegisteredServer) || (obj is ServerGroup));

            try
            {
                if (null == file)
                {
                    throw new ArgumentNullException("file");
                }

                // set the store-wide option that controls how 
                // credentials are persisted
                GetStore(obj).CredentialPersistenceType = cpt;
                GetStore(obj).UseStoreCredentialPersistenceType = true;

                using (var writer = new XmlTextWriter(file, null))
                {
                    writer.Formatting = Formatting.Indented;
                    var sfcSerializer = new SfcSerializer();
                    sfcSerializer.FilterPropertyHandler += new FilterPropertyHandler(FilterProperty);
                    sfcSerializer.Serialize(obj);
                    sfcSerializer.Write(writer);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                try
                {
                    // if there was a problem try to clean up the file
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // swallow the exception - we don't want to propagate
                    // this one upward
                }

                throw new RegisteredServerException(RegSvrStrings.FailedOperation(RegSvrStrings.Export), e);
            }
            finally
            {
                GetStore(obj).UseStoreCredentialPersistenceType = false;
            }
        }

        /// <summary>
        /// This function will be called during serialization to provide 
        /// values for the properties of the object being serialized. 
        /// We are using it to override the value of 
        /// ConnectionStringWithEncryptedPassword according to a session setting
        /// that instructs whether password and user name should be saved.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="propertyArgs"></param>
        /// <returns></returns>
        internal static object FilterProperty(SfcSerializer serializer, FilterPropertyEventArgs propertyArgs)
        {
            if (null == serializer)
            {
                throw new ArgumentNullException("serializer");
            }

            if (null == propertyArgs)
            {
                throw new ArgumentNullException("propertyArgs");
            }

            if (propertyArgs.Instance.GetType() == typeof(RegisteredServer) &&
                propertyArgs.PropertyName == "ConnectionStringWithEncryptedPassword")
            {
                // we need to morph the ConnectionStringWithEncryptedPassword
                // as instructed by the user for serialization
                var rs = propertyArgs.Instance as RegisteredServer;
                var cpt = rs.CredentialPersistenceType;
                if (rs.GetStore().UseStoreCredentialPersistenceType)
                {
                    cpt = rs.GetStore().CredentialPersistenceType;
                }

                return rs.GetConnectionStringWithEncryptedPassword(cpt);
            }
            else if (propertyArgs.Instance.GetType() == typeof(RegisteredServer) &&
                propertyArgs.PropertyName == "CredentialPersistenceType")
            {
                var rs = propertyArgs.Instance as RegisteredServer;
                // update CredentialPersistenceType as well because the connection
                // string is serialized according to this setting
                if (rs.GetStore().UseStoreCredentialPersistenceType)
                {
                    var cpt = rs.GetStore().CredentialPersistenceType;
                    if (rs.CredentialPersistenceType == CredentialPersistenceType.PersistLoginName &&
                        cpt == CredentialPersistenceType.PersistLoginNameAndPassword)
                    {
                        // if we're being asked to save the login name and 
                        // password, but all we had to begin with was the 
                        // user name then we don't want to make it look like 
                        // we have the password
                        return CredentialPersistenceType.PersistLoginName;
                    }
                    else
                    {
                        return cpt;
                    }
                }
                else
                {
                    return rs.CredentialPersistenceType;
                }
            }
            else
            {
                return propertyArgs.Instance.Properties[propertyArgs.PropertyName].Value;
            }
        }


        /// <summary>
        /// Helper to detect exceptions that we can't recover from therefore 
        /// it does not make sense to rethrow.
        /// </summary>
        /// <param name="e"></param>
        internal static void FilterException(Exception e)
        {
            if (e is OutOfMemoryException)
                throw e;
        }

        // tells if the server-wide setting should override individual values
        // for registered servers
        private bool useStoreCredentialPersistenceType;

        internal bool UseStoreCredentialPersistenceType
        {
            get { return useStoreCredentialPersistenceType; }
            set { useStoreCredentialPersistenceType = value; }
        }

        // controls the server-wide setting for credential persistance
        // this is a session setting
        private CredentialPersistenceType credentialPersistenceType;

        internal CredentialPersistenceType CredentialPersistenceType
        {
            get { return credentialPersistenceType; }
            set { credentialPersistenceType = value; }
        }


        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public bool IsLocal
        {
            get
            {
                return this.LocalXmlStorageFile != null;
            }
        }

        /// <summary>
        /// Display name for this store
        /// </summary>
        [SfcIgnore]
        public string DisplayName
        {
            get
            {
                return (this.IsLocal ? RegSvrStrings.LocalServerStoreDisplayName : RegSvrStrings.CentralManagementServersDisplayName);
            }
        }

        /// <summary>
        /// The localized name of the local server store
        /// </summary>
        /// <remarks>
        /// String compararer in GUI needs to know this so it can sort the local
        /// server store differently from other nodes
        /// </remarks>
        public static string LocalServerStoreDisplayName
        {
            get
            {
                return RegSvrStrings.LocalServerStoreDisplayName;
            }
        }

        /// <summary>
        /// The localized name of the shared server store
        /// </summary>
        /// <remarks>
        /// String compararer in GUI needs to know this so it can sort the shared
        /// server store differently from other nodes
        /// </remarks>
        public static string CentralManagementServersDisplayName
        {
            get
            {
                return RegSvrStrings.CentralManagementServersDisplayName;
            }
        }

        /// <summary>
        /// Used in RegisterServer::Create() method
        /// to allow creation of objects without serialization
        /// </summary>
        [SfcIgnore]
        internal bool IsSerializeOnCreation
        {
            get
            {
                return this.isSerializeOnCreation;
            }
        }


        /// Name of the builtin DatabaseEngine group 
        internal static readonly string databaseEngineServerGroupName = "DatabaseEngineServerGroup";
        /// Name of the builtin AnalysisServices group 
        internal static readonly string analysisServicesServerGroupName = "AnalysisServicesServerGroup";
        /// Name of the builtin ReportingServices group
        internal static readonly string reportingServicesServerGroupName = "ReportingServicesServerGroup";
        /// Name of the builtin IntegrationServices group
        internal static readonly string integrationServicesServerGroupName = "IntegrationServicesServerGroup";
        /// Name of the builtin SqlServerCompactEdition group
        internal static readonly string sqlServerCompactEditionServerGroupName = "SqlServerCompactEditionServerGroup";
        /// Name of the builtin CentralManagementServer group
        internal static readonly string centralManagementServerGroupName = "CentralManagementServerGroup";

        /// <summary>
        /// 
        /// </summary>
        public string DatabaseEngineServerGroupName
        {
            get
            {
                return databaseEngineServerGroupName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AnalysisServicesServerGroupName
        {
            get
            {
                return analysisServicesServerGroupName;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string ReportingServicesServerGroupName
        {
            get
            {
                return reportingServicesServerGroupName;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string IntegrationServicesServerGroupName
        {
            get
            {
                return integrationServicesServerGroupName;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string SqlServerCompactEditionServerGroupName
        {
            get
            {
                return sqlServerCompactEditionServerGroupName;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string CentralManagementServerGroupName
        {
            get
            {
                return centralManagementServerGroupName;
            }
        }



        private ServerGroupCollection serverGroups;

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ServerGroup))]
        public ServerGroupCollection ServerGroups
        {
            get
            {
                if (null == serverGroups)
                {
                    serverGroups = new ServerGroupCollection(this, this.IsLocal ? null : new ServerComparer(this.ServerConnection, "msdb"));
                }

                return serverGroups;
            }
        }

        private ServerGroup databaseEngineServerGroup;
        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ServerGroup DatabaseEngineServerGroup
        {
            get
            {
                if (databaseEngineServerGroup == null)
                {
                    databaseEngineServerGroup = ServerGroups[DatabaseEngineServerGroupName];
                    if (null == databaseEngineServerGroup && IsLocal)
                    {
                        // if this is a local group we will initialize the 
                        // group when it does not exist
                        databaseEngineServerGroup =
                            new ServerGroup(this, DatabaseEngineServerGroupName);
                        databaseEngineServerGroup.ServerType = ServerType.DatabaseEngine;
                        databaseEngineServerGroup.Create();
                    }
                }

                return databaseEngineServerGroup;
            }
        }

        private ServerGroup centralManagementServerGroup;
        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ServerGroup CentralManagementServerGroup
        {
            get
            {
                if (!IsLocal)
                {
                    // this is a local only group
                    return null;
                }

                if (null == centralManagementServerGroup)
                {
                    centralManagementServerGroup = ServerGroups[CentralManagementServerGroupName];
                    if (null == centralManagementServerGroup)
                    {
                        centralManagementServerGroup =
                            new ServerGroup(this, CentralManagementServerGroupName);
                        centralManagementServerGroup.ServerType = ServerType.DatabaseEngine;
                        centralManagementServerGroup.Create();
                    }
                }
                return centralManagementServerGroup;
            }
        }

        private AzureDataStudioConnectionStore azureDataStudioConnectionStore;
        /// <summary>
        /// Contains the set of connections and groups stored in the user's Azure Data Studio settings
        /// </summary>
        [SfcIgnore]
        public AzureDataStudioConnectionStore AzureDataStudioConnectionStore
        {
            get
            {
                if (!IsLocal)
                {
                    // this is a local only group
                    return null;
                }

                return azureDataStudioConnectionStore ?? (azureDataStudioConnectionStore =
                    AzureDataStudioConnectionStore.LoadAzureDataStudioConnections(settingsFile:null));
            }
        }

        private ServerGroup analysisServicesServerGroup;
        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ServerGroup AnalysisServicesServerGroup
        {
            get
            {
                if (analysisServicesServerGroup == null)
                {
                    analysisServicesServerGroup = ServerGroups[AnalysisServicesServerGroupName];
                    if (null == analysisServicesServerGroup && IsLocal)
                    {
                        analysisServicesServerGroup =
                            new ServerGroup(this, AnalysisServicesServerGroupName);
                        analysisServicesServerGroup.ServerType = ServerType.AnalysisServices;
                        analysisServicesServerGroup.Create();
                    }
                }

                return analysisServicesServerGroup;
            }
        }

        private ServerGroup reportingServicesServerGroup;
        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ServerGroup ReportingServicesServerGroup
        {
            get
            {
                if (reportingServicesServerGroup == null)
                {
                    reportingServicesServerGroup = ServerGroups[ReportingServicesServerGroupName];

                    if (null == reportingServicesServerGroup && IsLocal)
                    {
                        reportingServicesServerGroup =
                            new ServerGroup(this, ReportingServicesServerGroupName);
                        reportingServicesServerGroup.ServerType = ServerType.ReportingServices;
                        reportingServicesServerGroup.Create();
                    }
                }

                return reportingServicesServerGroup;
            }
        }

        private ServerGroup integrationServicesServerGroup;
        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ServerGroup IntegrationServicesServerGroup
        {
            get
            {
                if (integrationServicesServerGroup == null)
                {
                    integrationServicesServerGroup = ServerGroups[IntegrationServicesServerGroupName];
                    if (null == integrationServicesServerGroup && IsLocal)
                    {
                        integrationServicesServerGroup =
                            new ServerGroup(this, IntegrationServicesServerGroupName);
                        integrationServicesServerGroup.ServerType = ServerType.IntegrationServices;
                        integrationServicesServerGroup.Create();
                    }
                }

                return integrationServicesServerGroup;
            }
        }

        private ServerGroup sqlServerCompactEditionServerGroup;
        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ServerGroup SqlServerCompactEditionServerGroup
        {
            get
            {
                if (sqlServerCompactEditionServerGroup == null)
                {
                    sqlServerCompactEditionServerGroup = ServerGroups[SqlServerCompactEditionServerGroupName];
                    if (null == sqlServerCompactEditionServerGroup && IsLocal)
                    {
                        sqlServerCompactEditionServerGroup =
                            new ServerGroup(this, SqlServerCompactEditionServerGroupName);
                        sqlServerCompactEditionServerGroup.ServerType = ServerType.SqlServerCompactEdition;
                        sqlServerCompactEditionServerGroup.Create();
                    }
                }

                return sqlServerCompactEditionServerGroup;
            }
        }

        private ServerConnection serverConnection;
        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ServerConnection ServerConnection
        {
            get
            {
                return serverConnection;
            }
        }

        private string localXmlStorageFile;
        private string LocalXmlStorageFile
        {
            get
            {
                return this.localXmlStorageFile;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal override SfcKey CreateIdentityKey()
        {
            return new Key(this);
        }

        #region ISfcDomain Members

        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public string DomainInstanceName
        {
            get
            {
                if (null == this.ServerConnection)
                {
                    STrace.Assert(!string.IsNullOrEmpty(this.LocalXmlStorageFile));
                    return this.LocalXmlStorageFile;
                }
                return this.ServerConnection.TrueName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public string DomainName
        {
            get { return "RegisteredServers"; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISfcExecutionEngine GetExecutionEngine()
        {
            if (IsLocal)
            {
                return NopExecutionEngine;
            }
            else
            {
                return new SfcTSqlExecutionEngine(this.ServerConnection);
            }
        }

        static RegisteredServersStore()
        {
            nopExecutionEngine = new NopExecutionEngine();
        }

        private static NopExecutionEngine nopExecutionEngine;

        internal static NopExecutionEngine NopExecutionEngine
        {
            get
            {
                return nopExecutionEngine;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urnFragment"></param>
        /// <returns></returns>
        public SfcKey GetKey(IUrnFragment urnFragment)
        {
            switch (urnFragment.Name)
            {
                case typeName: return new RegisteredServersStore.Key(this);
                case ServerGroup.typeName: return new ServerGroup.Key(urnFragment.FieldDictionary);
                case RegisteredServer.typeName: return new RegisteredServer.Key(urnFragment.FieldDictionary);
                default:
                    throw new InvalidArgumentException(urnFragment.Name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public Type GetType(string typeName)
        {
            switch (typeName)
            {
                case RegisteredServersStore.typeName: return typeof(RegisteredServersStore);
                case ServerGroup.typeName: return typeof(ServerGroup);
                case RegisteredServer.typeName: return typeof(RegisteredServer);
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public SfcTypeMetadata GetTypeMetadata(string typeName)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool UseSfcStateManagement()
        {
            return true;
        }

        /// <summary>
        /// Returns the logical version of the domain
        /// </summary>
        /// <returns></returns>
        int ISfcDomainLite.GetLogicalVersion()
        {
            return 1;      // logical version changes only when the schema of domain changes
        }

        #endregion

        #region ISfcHasConnection Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activeQueriesMode"></param>
        /// <returns></returns>
        ISfcConnection ISfcHasConnection.GetConnection(SfcObjectQueryMode activeQueriesMode)
        {
            switch (activeQueriesMode)
            {
                case SfcObjectQueryMode.SingleActiveQuery:
                    return this.ServerConnection;
                case SfcObjectQueryMode.MultipleActiveQueries:
                    if ((this.ServerConnection != null) && (this.ServerConnection.MultipleActiveResultSets))
                    {
                        return this.ServerConnection;
                    }
                    return null;
                case SfcObjectQueryMode.CachedQuery:
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Sets the active connection.
        /// </summary>
        void ISfcHasConnection.SetConnection(ISfcConnection connection)
        {
            throw new NotSupportedException(); // Unless we need to support it, it will throw for now.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ISfcConnection ISfcHasConnection.GetConnection()
        {
            return this.ServerConnection;
        }

        private SfcConnectionContext connectionContext;
        /// <summary>
        /// 
        /// </summary>
        SfcConnectionContext ISfcHasConnection.ConnectionContext
        {
            get
            {
                if (connectionContext == null)
                {
                    // If our connection is still null when this is called, we are forced into Offline mode.
                    connectionContext = new SfcConnectionContext(this);
                }
                return connectionContext;
            }
        }
        #endregion

        #region SFC Boiler Plate
        internal const string typeName = "RegisteredServersStore";

        /// Internal key class
        public sealed class Key : DomainRootKey
        {
            /// <summary>
            /// Default constructor for generic Key generation
            /// </summary>
            public Key()
                : base(null) // Caller has to remember to set Root!
            {
            }

            internal Key(ISfcDomain root)
                : base(root)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return this.GetType().GetHashCode();
            }

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return this == obj;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj1"></param>
            /// <param name="obj2"></param>
            /// <returns></returns>
            public new static bool Equals(object obj1, object obj2)
            {
                return (obj1 as Key) == (obj2 as Key);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public override bool Equals(SfcKey key)
            {
                return this == key;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(object obj, Key rightOperand)
            {
                if (obj == null || obj is Key)
                    return (Key)obj == rightOperand;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, object obj)
            {
                if (obj == null || obj is Key)
                    return leftOperand == (Key)obj;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, Key rightOperand)
            {
                // If both are null, or both are same instance, return true.
                if (ReferenceEquals(leftOperand, rightOperand))
                    return true;

                // If one is null, but not both, return false.
                if (((object)leftOperand == null) || ((object)rightOperand == null))
                    return false;

                return leftOperand.IsEqual(rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(object obj, Key rightOperand)
            {
                return !(obj == rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, object obj)
            {
                return !(leftOperand == obj);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, Key rightOperand)
            {
                return !(leftOperand == rightOperand);
            }

            private bool IsEqual(Key other)
            {
                return (this.Domain == other.Domain);
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return typeName;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        protected internal override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                case ServerGroup.typeName:
                    return this.ServerGroups;
                default:
                    throw new RegisteredServerException(RegSvrStrings.NoSuchCollection(elementType));
            }
        }
        #endregion

        #region file utilities

        /// This is the suffix that we add to the %appdata% variable
        /// to construct the directory location where we're persisting
        /// our local xml file.
        private const string savePathSuffix = "\\Microsoft\\SQL Server Management Studio";
        private const string savePathLegacySuffixFormat = "\\Microsoft\\Microsoft SQL Server\\{0}\\Tools\\Shell";
#if MICROSOFTDATA
        // Sort this array from newest version to oldest. 
        // SSMS 19 -> regsrvr16.xml with SqlClient 3 compatible strings
        // SSMS 20 -> regsrvr17.xml with SqlClient 5 compatible strings
        // When the file format or connection string format changes in a way not compatible with prior versions of SSMS, 
        // change the value of RegisteredServersFileName and put the old value in the front of this array.
        // VBUMP
        private static readonly string[] legacyFileNames = { "RegSrvr16.xml", "RegSrvr.xml" };
        /// <summary>
        /// Name of the registered servers file used by Sql Server Management Studio and stored in the user profile.
        /// </summary>
        public const string RegisteredServersFileName = "RegSrvr17.xml";
#else
        // SSMS 18 has no legacy file names in the same folder
        private static readonly string[] legacyFileNames = { };
        /// <summary>
        /// Name of the registered servers file used by Sql Server Management Studio and stored in the user profile.
        /// </summary>
        public const string RegisteredServersFileName = "RegSrvr.xml";
#endif
        private static string GetLocalXmlFilePath()
        {
            return Path.Combine(GetSettingsDir(savePathSuffix), RegisteredServersFileName);
        }

        private static string GetSettingsDir(string suffix)
        {
            var directory = string.Format(CultureInfo.InvariantCulture, "{0}{1}",
                                             System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                                             suffix);
            EnsureDirExists(directory);
            return directory;
        }

        /// <summary>
        /// makes sure that the directory exists
        /// </summary>
        private static void EnsureDirExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
#endregion

#region Utilities
        internal static RegisteredServersStore GetStore(SfcInstance instance)
        {
            STrace.Assert(instance is ServerGroup || instance is RegisteredServer);

            ServerGroup theGroup = null;
            if (instance is RegisteredServer)
            {
                theGroup = ((RegisteredServer)instance).Parent as ServerGroup;
            }
            else
            {
                theGroup = instance as ServerGroup;
            }

            // we will use this as a counter to avoid spinning if the 
            // tree is corrupted with a cycle
            var maxDepth = 0;

            // walk the chain to the parent
            while (!(theGroup.Parent is RegisteredServersStore) && maxDepth < 10000)
            {
                theGroup = theGroup.Parent as ServerGroup;
                maxDepth++;
            }

            return theGroup.Parent as RegisteredServersStore;
        }
#endregion

#region ISfcDiscoverObject Members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            if (sink.Action == SfcDependencyAction.Serialize)
            {
                foreach (var sg in this.ServerGroups)
                {
                    sink.Add(SfcDependencyDirection.Inbound, sg, SfcTypeRelation.ContainedChild, false);
                }
            }

            return;
        }

#endregion

        // These are the GUIDs used by the downlevel registered servers file
        private const string DatabaseEngineServerTypeGuid = "8c91a03d-f9b4-46c0-a305-b5dcc79ff907";
        private const string SqlServerCompactEditionServerTypeGuid = "6b04a4a7-9b37-4028-ac2d-f0a39e50fb57";
        private const string AnalysisServicesServerTypeGuid = "1396ffcb-10d7-4f8c-aaef-4696d541f554";
        private const string IntegrationServicesServerTypeGuid = "19d20860-9e9a-4aff-a80d-f72b41b5e931";
        private const string ReportingServicesServerTypeGuid = "3a0f2e46-847b-4332-9b7e-fc78e43a49b0";
    }

    internal class NopExecutionEngine : ISfcExecutionEngine
    {
        public object Execute(ISfcScript script)
        {
            return null;
        }
    }
}
