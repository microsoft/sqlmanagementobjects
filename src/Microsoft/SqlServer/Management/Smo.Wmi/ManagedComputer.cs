using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Management;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Diagnostics;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{

    //For the design spec of the WMI 64 bit support please refer to the
    //dev_SQLComputerManager_64bit_support.doc

    //The Provider Architecture
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum ProviderArchitecture
    {
        Default = 0,	// Default WMI provider architecture will be chosen (depends on client architeture)
        Use32bit = 32,	// Connect to 32 bit WMI provider
        Use64bit = 64	// Connect to 64 bit WMI provider
    }

    public sealed class ManagedComputer : WmiSmoObject
    {
        // The possible management path for the machine we are targeting
        // We don't really know the right path until we connect because
        // the paths change according to the version of the server.
        private IList<ManagementPath> managementPaths = null;

        /// <summary>
        /// Compute the possible management paths for the given target server
        /// </summary>
        /// <param name="machineName">The name of the machine we will connect to</param>
        /// <returns>A collection of ManagementPaths</returns>
        /// <remarks>This should be called by the Init() method only</remarks>
        internal static IList<ManagementPath> GetPossibleManagementPaths(string machineName)
        {
            var list = new List<ManagementPath>();

            for (int majorVersion = AssemblyVersionInfo.MajorVersion; majorVersion >= 10; majorVersion--)
            {
                list.Add(new ManagementPath(string.Format(SmoApplication.DefaultCulture, @"\\{0}\ROOT\Microsoft\SqlServer\ComputerManagement{1}", machineName, majorVersion)));
            }

            // Pre-Katmai (=9.0 and below)
            list.Add(new ManagementPath(string.Format(SmoApplication.DefaultCulture, @"\\{0}\ROOT\Microsoft\SqlServer\ComputerManagement", machineName)));

            return list;
        }

        public ManagedComputer()
            : base(string.Empty)
        {
            // By default connects to local machine
            Init(string.Empty); 
        }

        public ManagedComputer(string machineName) : base(machineName)
        {
            if( null == machineName )
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("machineName"));
            
            Init(machineName);
        }

        public ManagedComputer(string machineName, string userName, string password) : 
            base(machineName)
        {
            if( null == machineName )
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("machineName"));

            if( null == userName )
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("userName"));
            
            if( null == password )
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("password"));
            
            Init(machineName);
            m_ManagementScope.Options.Username = userName;
            m_ManagementScope.Options.Password = password;
        }

        public ManagedComputer(string machineName, string userName, string password, ProviderArchitecture providerArchitecture) 
            : base(machineName)
        {
            if (null == machineName)
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("machineName"));

            if (null == userName)
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("userName"));

            if (null == password)
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("password"));

                        
            Init(machineName);

            //The m_WmiconnectionInfo is created in the Init() method above.
            m_WmiConnectionInfo.ProviderArchitecture = providerArchitecture;

            m_ManagementScope.Options.Username = userName;
            m_ManagementScope.Options.Password = password;
        }

        internal override Smo.PropertyMetadataProvider GetPropertyMetadataProvider()
        {
            return new PropertyMetadataProvider();
        }

        private class PropertyMetadataProvider : Smo.PropertyMetadataProvider
        {
            public override int PropertyNameToIDLookup(string propertyName)
            {
                return -1;
            }

            public override int Count
            {
                get { return 0; }
            }

            public override StaticMetadata GetStaticMetadata(int id)
            {
                return StaticMetadata.Empty;
            }

        }

        private void Init( string machineName )
        {
            m_ManagementScope = new ManagementScope();

            // translate special network names into the machine name
            if( machineName.Length == 0 || "." == machineName || "(local)" == machineName )
            {
                machineName = System.Environment.MachineName;
                SetName( machineName );
            }

            managementPaths = GetPossibleManagementPaths(machineName);

            // build the path to the object

            // Note: this is a best-guess. We would not know if this is correct until after we try to connect.
            m_ManagementScope.Path = managementPaths[0];
            m_WmiConnectionInfo = new WmiConnectionInfo(m_ManagementScope, this);

            // Set state to existing, since this is a root object
            SetState(SqlSmoState.Existing);
        }

        private WmiConnectionInfo m_WmiConnectionInfo;
        public WmiConnectionInfo ConnectionSettings 
        {
            get { return m_WmiConnectionInfo; }
        }
        
        // returns the name of the type in the urn expression
        internal static string UrnSuffix
        {
            get 
            {
                return "ManagedComputer";
            }
        }

        /// <summary>
        /// Returns the object with the corresponding Urn
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        public WmiSmoObject GetSmoObject(Urn urn)
        {
            try
            {
                if( null == urn ) 
                    throw new ArgumentNullException("urn");
                
                return GetSmoObjectRec(urn);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.FailedOperationExceptionText(ExceptionTemplates.GetSmoObject, ExceptionTemplates.ManagedComputer, this.Name), e);
            }
        }

        private WmiSmoObject GetSmoObjectRec(Urn urn )
        {
            // stop condition goes first
            // TODO: add code to handle urn's that do not contain server
            if( null == urn.Parent )
            {
                if( urn.Type == "ManagedComputer" )
                {
                    if( urn.GetAttribute("Name") == this.Name )
                    {
                        return this;
                    }
                    else
                    {
                        throw new SmoException(ExceptionTemplates.InvalidServerUrn(urn.GetAttribute("Name")));
                    }
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.InvalidUrn(urn.Type));
                }
            }

            // we are going down one level to get the parent object
            WmiSmoObject parentNode = GetSmoObjectRec(urn.Parent);

            // we'll try to get the child object here. This can fail if parent 
            // does not have this node in the child collection
            string nodeType = urn.Type;
            string nodeName = urn.GetNameForType(nodeType);

            // this actually supposes that all child collection are named like this 
            string childCollectionName = String.Empty;
            if( nodeType == "ServerAlias" )
            {
                childCollectionName = "ServerAliases";
            }
            else if( nodeType == "IPAddress" )
            {
                childCollectionName = "IPAddresses";
            }
            else
            {
                childCollectionName = nodeType + "s";
            }
            
            // get child collection. This should not fail, since the collection 
            // name is hardcoded
            object childCollection = parentNode.GetType().InvokeMember(childCollectionName, 
                BindingFlags.Default  | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public, 
                null, parentNode, new object [] {}, SmoApplication.DefaultCulture );
            if(null==childCollection)
            {
                throw new MissingObjectException(ExceptionTemplates.MissingObjectExceptionText(
                    childCollectionName, parentNode.GetType().ToString(), null )).SetHelpContext("MissingObjectExceptionText");
            }

            // get the child object from child collection
            WmiSmoObject thisNode = (WmiSmoObject)childCollection.GetType().InvokeMember("GetObjectByName", 
                BindingFlags.Default |BindingFlags.InvokeMethod| BindingFlags.Instance |BindingFlags.NonPublic | BindingFlags.Public , 
                null, 
                childCollection, 
                new object [] {nodeName} ,
                SmoApplication.DefaultCulture 
                );
            if(null==thisNode)
            {
                throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist(SqlSmoObject.GetTypeName(nodeType), nodeName));
            }


            return thisNode;
        }

        // this is the equivalent of a connection 
        private ManagementScope m_ManagementScope;
        internal ManagementScope ManagementScope
        {
            get 
            {
                if (!m_ManagementScope.IsConnected)
                {
                    TryConnect();
                }
                return m_ManagementScope;
            }
        }

        Exception TryConnectUsingPath(ManagementPath path, out bool unrecoverableException)
        {
            unrecoverableException = false;

            try
            {
                m_ManagementScope.Path = path;
                m_ManagementScope.Connect();

                // Runs a dummy query (and ideally quick!) to check whether the SQL WMI Provider exists or not
                //
                // Note: we do this because a simple check like
                //   new ManagementObject(m_ManagementScope, new ManagementPath("Win32Provider.Name=\"MSSQL_ManagementProvider\""), new ObjectGetOptions());
                // may be be inconclusive due to a bug in SQL Setup (2019 and earlier), where the WMI Provider is not property uninstalled when SQL was installed.
                using (var mos = new ManagementObjectSearcher(m_ManagementScope, new ObjectQuery("SELECT SQLServiceType FROM SqlService WHERE SQLServiceType = 0")))
                {
                    _ = mos.Get().Count;
                }
            }
            catch (ManagementException managementException)
            {
                string extraMessage;
                if (managementException.ErrorCode == ManagementStatus.InvalidNamespace ||
                    managementException.ErrorCode == ManagementStatus.ProviderLoadFailure)
                {
                    // This error means that WMI provider for SQL Server is not installed on
                    // the server.
                    // Provide a more user-friendly message in that case. this.Name represents 
                    // a machine name here.
                    extraMessage = ExceptionTemplates.WMIProviderNotInstalled(this.Name);
                }
                else
                {
                    // If we are here, it means that most likely we had the right path
                    // and the WMI provider was indeed found on the target machine, however
                    // some other issue happened (e.g. "Access denied").
                    unrecoverableException = true;

                    extraMessage = ExceptionTemplates.InnerWmiException;

                }
                return new SmoException(extraMessage, managementException);
            }
            return null;
        }

        [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Management.ManagementObject")]
        private void TryConnect()
        {
            Exception exception = null;

            // Loop thru all the possible management paths (each version of SQL have its own path) 
            // trying to connect to the corresponding WMI provider.
            // Note: this loop is guaranteed to be executed at least once since managementPaths
            // has at least 1 element.
            foreach(var managementPath in managementPaths)
            {
                bool unrecoverableException;
                exception = TryConnectUsingPath(managementPath, out unrecoverableException);

                if (exception == null)
                {
                    // Refresh the WmiConnection info now that we found a valid WMI Provider
                    m_WmiConnectionInfo = new WmiConnectionInfo(m_ManagementScope, this);
                    return;
                }

                // If the exception was unrecoverable, just stop iterating
                if (unrecoverableException)
                {
                    break;
                }
            }

            // If we get here, exception is not null because managementPaths
            // is guaranteed to have at least one item.
            throw exception;
        }

        ServiceCollection m_Services = null;
        public ServiceCollection Services 
        {
            get 
            { 
                if(m_Services == null)
                {
                    m_Services =  new ServiceCollection(this); 
                }
                return m_Services;
            }
        }
        
        ClientProtocolCollection m_ClientProtocols = null;
        public ClientProtocolCollection ClientProtocols 
        {
            get 
            { 
                if(m_ClientProtocols == null)
                {
                    m_ClientProtocols =  new ClientProtocolCollection(this); 
                }
                return m_ClientProtocols;
            }
        }

        ServerInstanceCollection serverInstances = null;
        public ServerInstanceCollection ServerInstances 
        {
            get 
            { 
                if(serverInstances == null)
                {
                    serverInstances =  new ServerInstanceCollection(this); 
                }
                return serverInstances;
            }
        }

        ServerAliasCollection m_ServerAliases = null;
        public ServerAliasCollection ServerAliases 
        {
            get 
            { 
                if(m_ServerAliases == null)
                {
                    m_ServerAliases =  new ServerAliasCollection(this); 
                }
                return m_ServerAliases;
            }
        }
    }

    /// <summary>
    /// Connection structure that incapsulates ConnectionOptions structure
    /// in the ManagedComputer's Scope object
    /// </summary>
    public sealed class WmiConnectionInfo
    {
        internal WmiConnectionInfo(ManagementScope scope, ManagedComputer parent)
        {
            this.managementScope = scope;
            this.parent = parent;

            // default Timeout is 10 seconds
            this.managementScope.Options.Timeout = new TimeSpan(0, 0, 10);
        }

        internal ConnectionOptions Options
        {
            get { return this.managementScope.Options; }
        }

        public TimeSpan Timeout 
        {
            get { return this.managementScope.Options.Timeout; }
            set { this.managementScope.Options.Timeout = value; }
        }

        public string MachineName
        {
            get { return this.parent.Name; }
            set 
            { 
                if (null == value)
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("MachineName"));

                if (managementScope.IsConnected)
                    throw new SmoException(ExceptionTemplates.PropertyCannotBeChangedAfterConnection("MachineName"));

                // translate special network names into the machine name
                if (value.Length == 0 || "." == value || "(local)" == value)
                {
                    value = System.Environment.MachineName;
                }

                // Note: this is a best-guess. We would not know if this is correct until after we try to connect.
                this.managementScope.Path = ManagedComputer.GetPossibleManagementPaths(value)[0];
                this.parent.SetName(value);             
            }
        }

        public string Username
        {
            get { return this.managementScope.Options.Username; }
            set 
            { 
                if( null == value )
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Username"));

                if (managementScope.IsConnected)
                    throw new SmoException(ExceptionTemplates.PropertyCannotBeChangedAfterConnection("Username"));

                managementScope.Options.Username = value; 
            }
        }

        public void SetPassword(string password)
        {
            if( null == password )
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Password"));

            if (managementScope.IsConnected)
                throw new SmoException(ExceptionTemplates.PropertyCannotBeChangedAfterConnection("Password"));

            this.managementScope.Options.Password = password;
        }
        
        private ProviderArchitecture m_ProviderArchitecture = ProviderArchitecture.Default;
        public ProviderArchitecture ProviderArchitecture
        {
            get {return m_ProviderArchitecture;}
            set 
            {
                m_ProviderArchitecture = value;

                //Set the ProviderArchitecture and RequireArchitecture info on the management scope object
                if (ProviderArchitecture.Default != m_ProviderArchitecture)
                {
                    //This is to force a deterministic behavior for the choice of ProviderArchitecture
                    this.managementScope.Options.Context.Add("__RequiredArchitecture", (object)true);

                    this.managementScope.Options.Context.Add("__ProviderArchitecture",
                        ProviderArchitecture.Use32bit == m_ProviderArchitecture ?
                        (object)((int)ProviderArchitecture.Use32bit) :
                        (object)((int)ProviderArchitecture.Use64bit));
                }
            }
        }

        private ManagementScope managementScope;
        private ManagedComputer parent;
    }

}