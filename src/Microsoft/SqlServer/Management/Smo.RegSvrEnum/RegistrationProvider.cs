//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
#if STRACE
using Microsoft.SqlServer.Management.Diagnostics;
#endif

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// This class supports the SMO implementation and should not be used for applications
    /// </summary>
    public sealed class RegistrationProvider
    {
        /// <summary>
        /// Not for use by applications
        /// </summary>
        public enum RegistrationProviderStore
        {
            Default,
            SQLServerManagementStudio,
            ReplicationMonitor
        }

        private static RegistrationProviderStore _ProviderStore = RegistrationProviderStore.Default;

        /// <summary>
        /// Not for use by applications
        /// </summary>
        public static RegistrationProviderStore ProviderStore
        {
            set
            {
                //Note: This must be called before the singleton is created otherwise an exception will thrown.
                //This was a quick fix to allow Repl Monitor to target a different private app data file - RegReplSrvr.xml.

                //This property can only be set once. If this was allowed we would need to find a way to 
                //invalidate existing reg server objects so they couldn't write back to the file
                // Not providing an exceptin message because this isn't exposed to clients
                if (_ProviderStore != RegistrationProviderStore.Default)
                {
                    throw new InvalidOperationException();
                }

                _ProviderStore = value;
            }
        }

        /// <summary>
        /// 
        ///  </summary>
        /// Our rampant use of InternalsVisibleTo prevents us from having our own AssemblyVersionInfo so provide a way
        /// for our callers to set the version. It was previously hard coded to 90.
        public static string ProviderStoreVersion { get; set; }

        private static string providerStoreCredentialVersion = "18";
        /// <summary>
        /// Determines part of the key name used to save the password to Windows Credential Store.
        /// Typically this is set to the SSMS version hosting the DLL.
        /// Set this to override the default value of 18.
        /// </summary>
        public static string ProviderStoreCredentialVersion { get { return providerStoreCredentialVersion; } set { providerStoreCredentialVersion = value; } }

        /// <summary>
        /// 
        /// </summary>
        public const string FileExtension = "regsrvr";
        private const string regSqlServerFileName = "RegSrvr.xml";
        private const string regReplServerFileName = "RegReplSrvr.xml";

        
#region Singleton Implementation
        private static volatile RegistrationProvider singleton;
        private static object syncRoot = new object();      

#region Singleton Creation
        private static RegistrationProvider Instance
        {
            get
            {
                if(singleton == null)
                {
                    lock(syncRoot)
                    {
                        if(singleton == null)
                        {
                            singleton = new RegistrationProvider();
                            //Set the default to Management Studios app data file
                            if (_ProviderStore == RegistrationProviderStore.Default)
                                _ProviderStore = RegistrationProviderStore.SQLServerManagementStudio;

                            singleton.Load();
                        }
                    }
                }
                return singleton;
            }
        }
#endregion

#region Static Methods and properties
        /// <summary>
        /// 
        /// </summary>
        public static event RegistrationEventHandler AddedNode;
        /// <summary>
        /// 
        /// </summary>
        public static event RegistrationEventHandler RemovedNode;
        /// <summary>
        /// 
        /// </summary>
        public static event RegistrationEventHandler NodeModified;

        /// <summary>
        /// Exports a subtree of the server registration
        /// </summary>
        /// <param name="reg">The root node of the export</param>
        /// <param name="filename">The location to export to</param>
        /// <param name="includeNames">Whether to include usernames in the exported file</param>
        public static void Export(RegistrationInfo reg, string filename, bool includeNames)
        {
            
            if(reg == null)
            {
#if STRACE
                STrace.LogExThrow();
#endif
                throw new ArgumentNullException("reg");
            }
            
            // open the file, and wipe out whatever was in there before
            // if we wanted to save it, then we already have

            try
            {
                using (XmlTextWriter writer = new XmlTextWriter(
                       new StreamWriter(File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None))))
                {
                    writer.Formatting = Formatting.Indented;

                    writer.WriteStartDocument();
                    writer.WriteStartElement("Export", null);
                    writer.WriteAttributeString("serverType", reg.ServerType.ToString());

                    reg.SaveNodeToXml(writer, includeNames);
                    writer.WriteEndElement();
                }
            }
            catch(NotSupportedException invalidPath)
            {

                throw new RegisteredServerException(SRError.PathNotSupported(filename), invalidPath);
            }
            catch(DirectoryNotFoundException noDirectory)
            {

                throw new RegisteredServerException(
                    SRError.DirectoryNotFound(filename), noDirectory);
            }
            catch(UnauthorizedAccessException noAccess)
            {
                throw new RegisteredServerException(SRError.AccessDenied(filename), noAccess);
            } 
            catch (Exception ex)
            {
                throw new RegisteredServerException(SRError.ErrUnableToExport(filename), ex);
            }
        }

        
        /// <summary>
        /// reads registration information from the specified file and returns the
        /// node that corresponds to the root node read from the file. It will let
        /// whatever exception that might be encountered to fly out
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Node that corresponds to the root node read from the file</returns>
        public static RegistrationInfo Import(string fileName)
        {
            if (fileName == null)
            {
#if STRACE
                STrace.LogExThrow();
#endif
                throw new ArgumentNullException("fileName");
            }

            using (FileStream fileStream = File.OpenRead(fileName))
            {
                XmlTextReader reader = new XmlTextReader(fileStream)
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                // scan to the Export element
                reader.MoveToContent();
            
                // import the node contained in the export
                XmlTextReader innerNode = new XmlTextReader( new StringReader(reader.ReadInnerXml()))
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                
                return RegistrationInfo.LoadNodeFromXml(innerNode);
            }
        }

        /// <summary>
        /// allow disabling of the save operation. This property will perform automatic Save
        /// operation if Save used to be disabled and became enabled as a result of the call
        /// </summary>
        public static bool SaveIsDisabled
        {
            get
            {
                return Instance.saveDisabled;
            }

            set
            {
                bool wasSaveDisabled = Instance.saveDisabled;

                Instance.saveDisabled = value;

                //if it was disabled and became enabled - save right away
                if (wasSaveDisabled && !value)
                {
                    Instance.Save();
                }
            }
        }

        private void Save()
        {           

            if (saveDisabled)
            {
                return;
            }

            Utils.EnsureSettingsDirExists(ProviderStoreVersion ?? "90");
            string appFilePath = GetProviderFilePath();

            XmlTextWriter writer = new XmlTextWriter(
                new StreamWriter(File.Open(appFilePath, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                IndentChar = '\t',
                Indentation = 1,
                Formatting = Formatting.Indented
            };


            writer.WriteStartDocument();
            writer.WriteStartElement("RegisteredServers");

            try
            {

                foreach(RegistrationInfo node in RootNode.Children)
                {
                    node.SaveNodeToXml(writer, true);
                }
                writer.WriteEndElement();
            }
            catch(Exception ex)
            {
#if STRACE
                STrace.LogExCatch(ex);
#endif
                throw new RegisteredServerException(SRError.NotSaveRegisteredServers, ex);
            }
            finally
            {
                writer.Close();
            }
        }

        private void Load()
        {

            ArrayList list = new ArrayList();
            XmlTextReader reader = null;
            try
            {
                string registeredServersFilePath = GetProviderFilePath();

                if (File.Exists(registeredServersFilePath))
                {
                    reader = new XmlTextReader(
                        new StreamReader(File.OpenRead(registeredServersFilePath)))
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        WhitespaceHandling = WhitespaceHandling.None
                    };

                    // advance into the registered servers element
                    while(reader.Read())
                    {
                        if( reader.NodeType == XmlNodeType.Element && 
                            reader.LocalName == "RegisteredServers" )
                        {
                            RegistrationInfo node = null;                   
                            reader.Read();
                            while( (node = RegistrationInfo.LoadNodeFromXml(reader)) != null )
                            {
                                if(!(node is ServerTypeRegistrationInfo))
                                {
#if STRACE
                                    STrace.Trace(Tracing.RegProvider, Tracing.Error, 
                                        "Expected node of type {0} as root node in registered servers file" +
                                        ", instead we have node of type {1}", typeof(ServerTypeRegistrationInfo).FullName, 
                                        node.GetType().FullName);
#endif
                                    throw new RegisteredServerException(SRError.RegSvrDatafileInvalid(registeredServersFilePath));
                                }
#if STRACE
                                STrace.Trace(Tracing.RegProvider, Tracing.NormalTrace, 
                                    "Found Server Type: {0}, with {1} direct descendents", node.FriendlyName, 
                                    (node as ServerTypeRegistrationInfo).Children.Count);
#endif
                                AddServerTypeNode(node as ServerTypeRegistrationInfo);

                            }
                        }
                    }
                }
            }
            catch(XmlException)
            {
#if STRACE
                STrace.Trace(Tracing.RegProvider, Tracing.Error, 
                    "Error found in Registered Servers file, Registered Server info may be corrupt/incomplete");
#endif
            }
            catch(FileNotFoundException)
            {
#if STRACE
                STrace.Trace(Tracing.RegProvider, Tracing.Warning,
                    "Could not find Registered Servers file, a new one will be generated.");
#endif
            } 
            catch (DirectoryNotFoundException)
            {
#if STRACE
                STrace.Trace(Tracing.RegProvider, Tracing.Warning,
                    "Could not find directory with registered servers file, a new one will be generated.");
#endif
            } 
            finally
            {
                if(reader != null)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Returns the path to the XML file where the provider stores data
        /// </summary>
        /// <returns></returns>
        public static string GetProviderFilePath()
        {
            var appFile = (_ProviderStore == RegistrationProviderStore.ReplicationMonitor) ? regReplServerFileName : regSqlServerFileName;
            return Path.Combine( Utils.GetYukonSettingsDirName(ProviderStoreVersion ?? "90"), appFile);
        }

        public static void Refresh()
        {
            lock (syncRoot)
            {
                Instance.root.Children.Clear();
                Instance.serverTypes.Clear();
                Instance.Load();
            }
        }

        public static ParentRegistrationInfo RootNode
        {
            get
            {
                return Instance.root;
            }
        }
        
        public static bool ServerTypeNodeExists(Guid serverType)
        {

            return Instance.serverTypes.Contains(serverType);
        }
        

        public static void MarkNodeModified(RegistrationInfo node)
        {
            if(NodeModified != null)
            {
                NodeModified(node, new RegistrationEventArgs(node));
            }

            Instance.Save();
        }

        
        // these static methods route into the instance version of the methods which 
        // are define below


        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns>newly created registered node if registration succeeded, null if user cancelled</returns>
        public static RegistrationInfo AddServerTypeNode(ServerTypeRegistrationInfo node)
        {
            RegistrationInfo added = null;
            lock(syncRoot)
            {
                added = Instance.AddServerTypeNodeInternal(node);
            }
            return added;
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        /// <param name="parent"></param>
        /// <param name="behavior"></param>
        /// <returns>newly created registration info if add succeeded, null if user cancelled</returns>
        public static RegistrationInfo AddGroupNode(string name, string desc, ParentRegistrationInfo parent,
            RegistrationAddBehavior behavior)
        {
            RegistrationInfo node = null;
            lock(syncRoot)
            {
                 node = Instance.AddGroupNodeInternal(name, desc, parent, behavior);
            }

            if (node != null)
            {
                OnAddedNode(node);
            }
            

            return node;
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="parent"></param>
        /// <param name="behavior"></param>
        /// <returns>newly added node if succeeded, null if user cannceled</returns>
        public static RegistrationInfo AddNode(RegistrationInfo reg, ParentRegistrationInfo parent, 
            RegistrationAddBehavior behavior)
        {
            RegistrationInfo node = null;
            lock(syncRoot)
            {
                node = Instance.AddNodeInternal(reg, parent, behavior);
            }

            if (node != null)
            {
                OnAddedNode(node);
            }

            return node;
        }


        /// <summary>
        /// removes given node from the registered servers and notifies clients about this event
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="parent"></param>
        public static void RemoveNode(RegistrationInfo reg, ParentRegistrationInfo parent)
        {
            lock(syncRoot)
            {
                Instance.RemoveNodeInternal(reg, parent);
            }

            OnRemovedNode(reg);
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="newParent"></param>
        /// <param name="behavior"></param>
        /// <returns>true if move succeeded, false if canceled by user (in case there were duplicates)</returns>
        public static bool MoveNode(RegistrationInfo reg, ParentRegistrationInfo newParent, 
            RegistrationAddBehavior behavior)
        {

            // check to make sure we are not moving a node into itself
            if(reg == newParent)
            {
                throw new RegisteredServerException(SRError.ErrCannotMoveToSelf);
            }

            // check to make sure we are not moving a node into one of its children
            foreach(RegistrationInfo ancestor in newParent.Ancestors)
            {
                if(ancestor == reg)
                {
                    throw new RegisteredServerException(SRError.RecursiveMove);
                }
            }

            lock(syncRoot)
            {
                ParentRegistrationInfo formerParent = reg.Parent;
                if (formerParent == newParent)
                {
                    return true;
                }

                //we need to first remove node and after that to add it. The problem is that Add
                //might be cancelled if node with such name already exists in the new parent.
                //So, we need to check such situations ourselves.

                if (newParent.HasChildWithName(reg.FriendlyName) && behavior == RegistrationAddBehavior.Prompt)
                {
                    return false;
                }

                RemoveNode(reg, formerParent);
                AddNode(reg, newParent, behavior);

                return true;
            }
        }


        /// <summary>
        /// gets synchronization object
        /// </summary>
        public static object SyncRoot
        {
            get
            {
                return syncRoot;
            }
        }


        private static void OnRemovedNode(RegistrationInfo node)
        {
            if(RemovedNode != null)
            {
                RemovedNode(null, new RegistrationEventArgs(node));
            }

            Instance.Save();
        }

        private static void OnAddedNode(RegistrationInfo node)
        {
            if(AddedNode != null)
            {
                AddedNode(null, new RegistrationEventArgs(node));
            }

            Instance.Save();
        }
        
        
#endregion
                
#endregion
        
#region Private Data

        private ParentRegistrationInfo root;
        private HybridDictionary serverTypes;
        private bool saveDisabled;

#endregion
        
#region Private Constants
        private const int GroupImageIndex = 0;
        private const int ServerImageIndex = 1;
#endregion

#region Constructors
        
        private RegistrationProvider()      
        {       
            root = new ServerTypeRegistrationInfo();
            // REMOVE:
            //this.images = new ImageList();
            serverTypes = new HybridDictionary();
        }
#endregion

#region Private Methods

        private RegistrationInfo AddServerTypeNodeInternal(ServerTypeRegistrationInfo node)
        {
            
            // check to see if we already have this server type registered from a previous
            // load operation
            if(!serverTypes.Contains(node.ServerType))
            {               
                root.AddChild(node);
            
                serverTypes[node.ServerType] = node;
                return node;
            }
            // add this node's children

            ServerTypeRegistrationInfo reg = serverTypes[node.ServerType] as ServerTypeRegistrationInfo;
            foreach(RegistrationInfo n in node.Children)                
            {
                reg.AddChild(n);                
            }
            return null;
        }

        
        
        /// <summary>
        /// added node that represents group with given name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        /// <param name="parent"></param>
        /// <param name="behavior"></param>
        /// <returns>newly created node if successfull, null if cancelled by user</returns>
        private RegistrationInfo AddGroupNodeInternal(string name, string desc, ParentRegistrationInfo parent,
            RegistrationAddBehavior behavior)
        {
            
            GroupRegistrationInfo node = new GroupRegistrationInfo();
            node.FriendlyName = name;
            node.Description = desc;
            node.ServerType = parent.ServerType;

            return AddNodeInternal(node, parent, behavior);
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="parent"></param>
        /// <param name="behavior"></param>
        /// <returns>node that was added (if success), null if cancelled by user</returns>
        private RegistrationInfo AddNodeInternal(RegistrationInfo reg, ParentRegistrationInfo parent,
            RegistrationAddBehavior behavior)
        {
            if(reg.ServerType == Guid.Empty)
            {
                reg.ServerType = parent.ServerType;
            }


            //see if parent already has child with such name. If so, then prompt user
            RegistrationInfo existingNode = parent.Children[reg.FriendlyName];
            if (existingNode != null)
            {
                if (behavior == RegistrationAddBehavior.Prompt)
                {
                    if (existingNode != null)
                    {
                        return null;
                    }
                } else if (behavior == RegistrationAddBehavior.CreateCopy) 
                {
                    reg.FriendlyName = parent.GenerateUniqueName(reg.FriendlyName);
                } else if (behavior == RegistrationAddBehavior.Overwrite) 
                {
                    RemoveNode(existingNode, parent);
                }
            }

            parent.AddChild(reg);
            return reg;
        }


        private void RemoveNodeInternal(RegistrationInfo reg, ParentRegistrationInfo parent)
        {
            parent.RemoveChild(reg);
        }
#endregion
    }
}
