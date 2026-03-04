// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Reflection;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using XmlTextReader = System.Xml.XmlTextReader;

#if !STRACE
using STrace = System.Diagnostics.Trace;
#endif

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// Represents a group of servers in the Registered Servers store.
    /// </summary>
    public sealed partial class ServerGroup : ServerGroupParent, ISfcValidate, ISfcCreatable, ISfcAlterable, ISfcDroppable, ISfcRenamable, IRenamable, ISfcMovable
    {
        #region Script generation
        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptRenameAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptMoveAction = new SfcTsqlProcFormatter();

        static ServerGroup()
        {
            // Create script
            scriptCreateAction.Procedure = "msdb.dbo.sp_sysmanagement_add_shared_server_group";
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("parent_id", true));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_type", "ServerType", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_group_id", "ID", false, true));

            // Update script
            scriptAlterAction.Procedure = "msdb.dbo.sp_sysmanagement_update_shared_server_group";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_group_id", "ID", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));

            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_sysmanagement_delete_shared_server_group";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_group_id", "ID", true, false));

            // Rename script
            scriptRenameAction.Procedure = "msdb.dbo.sp_sysmanagement_rename_shared_server_group";
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_group_id", "ID", true, false));
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("new_name", true));

            // Move script
            scriptMoveAction.Procedure = "msdb.dbo.sp_sysmanagement_move_shared_server_group";
            scriptMoveAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_group_id", "ID", true, false));
            scriptMoveAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("new_parent_id", true));
        }

        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        public ServerGroup()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public ServerGroup(string name)
        {
            SetName(name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        public ServerGroup(ServerGroup parent, string name)
            : this(name)
        {
            this.Parent = parent;
            this.ServerType = ((ServerGroup)Parent).ServerType;
        }

        internal ServerGroup(RegisteredServersStore parent, string name)
        {
            SetName(name);
            this.Parent = parent;
        }

        #endregion

        #region Child Collections
        private RegisteredServerCollection registeredServers;
        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(RegisteredServer))]
        public RegisteredServerCollection RegisteredServers
        {
            get
            {
                if (registeredServers == null)
                {
                    IComparer<string> comparer = null;
                    // KeyChain will be null during deserialization
                    if ((this.KeyChain != null) && !IsLocal)
                    {
                        comparer = new ServerComparer(GetStore().ServerConnection, "msdb");
                    }
                    registeredServers = new RegisteredServerCollection(this, comparer);

                    if ((this.KeyChain != null) && !IsLocal && (0 == this.RegisteredServerChildCount))
                    {
                        registeredServers.SkipInitialSqlLoad = true;
                    }
                }
                return registeredServers;
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
                if (serverGroups == null)
                {
                    IComparer<string> comparer = null;
                    // KeyChain will be null during deserialization
                    if ((this.KeyChain != null) && !IsLocal)
                    {
                        comparer = new ServerComparer(GetStore().ServerConnection, "msdb");
                    }
                    serverGroups = new ServerGroupCollection(this, comparer);
                    if ((this.KeyChain != null) && !IsLocal && (0 == this.ServerGroupChildCount))
                    {
                        serverGroups.SkipInitialSqlLoad = true;
                    }
                }
                return serverGroups;
            }
        }
        #endregion

        #region SFC Boiler Plate
        internal const string typeName = "ServerGroup";
        /// <summary>
        /// 
        /// </summary>
        public sealed class Key : SfcKey
        {
            /// <summary>
            /// Properties
            /// </summary>
            private string keyName;

            /// <summary>
            /// Default constructor for generic Key generation
            /// </summary>
            public Key()
            {
            }

            /// <summary>
            /// Constructors
            /// </summary>
            /// <param name="other"></param>
            public Key(Key other)
            {
                keyName = other.Name;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            public Key(string name)
            {
                keyName = name;
            }

            /// <summary>
            /// 
            /// </summary>
            public string Name
            {
                get
                {
                    return keyName;
                }
            }

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                keyName = (string)filedDict["Name"];
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
                if (System.Object.ReferenceEquals(leftOperand, rightOperand))
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

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return this.Name.GetHashCode();
            }

            private bool IsEqual(Key key)
            {
                return string.CompareOrdinal(this.Name, key.Name) == 0;
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return String.Format("{0}[@Name='{1}']", ServerGroup.typeName, SfcSecureString.EscapeSquote(Name));
            }

        } // public class Key

        // Singleton factory class
        sealed class ObjectFactory : SfcObjectFactory
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
                return new ServerGroup();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static SfcObjectFactory GetObjectFactory()
        {
            return ObjectFactory.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new ServerGroupParent Parent
        {
            get { return (ServerGroupParent)base.Parent; }
            set
            {
                base.Parent = value;
                if (value is ServerGroup)
                {
                    this.ServerType = ((ServerGroup)Parent).ServerType;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal override SfcKey CreateIdentityKey()
        {
            Key key = null;
            // if we don't have our key values we can't create a key
            if (this.Name != null)
            {
                key = new Key(this.Name);
            }
            return key;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [SfcIgnore]
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required | SfcPropertyFlags.ReadOnlyAfterCreation)]
        [SfcKey(0)]
        public string Name
        {
            get
            {
                return (string)this.Properties["Name"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetName(string name)
        {
            this.Properties["Name"].Value = name;
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public int ID
        {
            get
            {
                return (int)this.Properties["ID"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        private int ServerGroupChildCount
        {
            get
            {
                object val = this.Properties["ServerGroupChildCount"].Value;
                if (null == val)
                {
                    return 0;
                }
                return (int)val;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        private int RegisteredServerChildCount
        {
            get
            {
                object val = this.Properties["RegisteredServerChildCount"].Value;
                if (null == val)
                {
                    return 0;
                }
                return (int)val;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string Description
        {
            get
            {
                return (string)this.Properties["Description"].Value;
            }
            set
            {
                this.Properties["Description"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public ServerType ServerType
        {
            get
            {
                return (ServerType)this.Properties["ServerType"].Value;
            }
            internal set
            {
                this.Properties["ServerType"].Value = value;
            }
        }

        /// <summary>
        /// Display name for the group
        /// </summary>
        [SfcIgnore]
        public string DisplayName
        {
            get
            {
                // result for groups that aren't well-known is the group name
                string result = this.Name;

                // if this is a built-in group, use the localized display name
                RegisteredServersStore store = this.Parent as RegisteredServersStore;

                if (store != null)
                {
                    if (this == store.AnalysisServicesServerGroup)
                    {
                        result = RegSvrStrings.AnalysisServicesServerGroupDisplayName;
                    }
                    else if (this == store.CentralManagementServerGroup)
                    {
                        result = RegSvrStrings.CentralManagementServerGroupDisplayName;
                    }
                    else if (this == store.DatabaseEngineServerGroup)
                    {
                        result = RegSvrStrings.DatabaseEngineServerGroupDisplayName;
                    }
                    else if (this == store.IntegrationServicesServerGroup)
                    {
                        result = RegSvrStrings.IntegrationServicesServerGroupDisplayName;
                    }
                    else if (this == store.ReportingServicesServerGroup)
                    {
                        result = RegSvrStrings.ReportingServicesServerGroupDisplayName;
                    }
                    else
                    {
                        Debug.Assert(false, "unexpected group");
                    }
                }

                return result;
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
                case RegisteredServer.typeName:
                    return this.RegisteredServers;
                default:
                    throw new RegisteredServerException(RegSvrStrings.NoSuchCollection(elementType));
            }
        }
        #endregion

        #region ISfcValidate implementation

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public ValidationState Validate(string methodName, params object[] arguments)
        {
            ValidationState validationState = new ValidationState();

            Validate(methodName, false, validationState);

            return validationState;
        }

        internal void Validate(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            if (this.Parent != null)
            {
                ServerGroup parent = this.Parent as ServerGroup;

                if (parent != null &&
                    parent.Name.Equals(RegisteredServersStore.centralManagementServerGroupName))
                {
                    throw new RegisteredServerException(RegSvrStrings.CannotCreateAServerGroupUnderneathCentralManagementServerGroup);
                }
            }

            if (String.IsNullOrEmpty(Name))
            {
                Exception ex = new SfcPropertyNotSetException("Name");
                if (throwOnFirst)
                {
                    throw ex;
                }
                else
                {
                    validationState.AddError(ex, "Name");
                }
            }
        }


        #endregion

        #region Create

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ISfcScript ISfcCreatable.ScriptCreate()
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
            STrace.Assert(this.Parent is ServerGroup);
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                ((ServerGroup)this.Parent).ID.GetType(), ((ServerGroup)this.Parent).ID));

            return new SfcTSqlScript(scriptCreateAction.GenerateScript(this, args));
        }

        /// <summary>
        /// 
        /// </summary>
        public void Create()
        {
            ServerGroupCollection serverGroupCollection = null;
            if (this.Parent is ServerGroup)
            {
                serverGroupCollection = ((ServerGroup)this.Parent).ServerGroups;
            }
            else if (this.Parent is RegisteredServersStore)
            {
                serverGroupCollection = ((RegisteredServersStore)this.Parent).ServerGroups;
            }

            if (serverGroupCollection.Contains(this.Name))
            {
                throw new RegisteredServerException(RegSvrStrings.ServerGroupAlreadyExists(this.Name));
            }

            Validate(ValidationMethod.Create);

            base.CreateImpl();

            if (IsLocal && GetStore().IsSerializeOnCreation)
            {
                GetStore().Serialize();
            }
        }

        /// <summary>
        /// Perform post-create action
        /// </summary>
        protected override void PostCreate(object executionResult)
        {
            if (!IsLocal)
            {
                this.Properties["ID"].Value = executionResult;
            }
        }

        #endregion


        #region Alter
        /// <summary>
        /// 
        /// </summary>
        public void Alter()
        {
            Validate(ValidationMethod.Alter);
            base.AlterImpl();

            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }

        ISfcScript ISfcAlterable.ScriptAlter()
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            string script = scriptAlterAction.GenerateScript(this);
            return new SfcTSqlScript(script);
        }
        #endregion

        #region Drop

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ISfcScript ISfcDroppable.ScriptDrop()
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            string script = scriptDropAction.GenerateScript(this);
            return new SfcTSqlScript(script);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Drop()
        {
            if (this.IsSystemServerGroup)
            {
                throw new RegisteredServerException(RegSvrStrings.CannotDropSystemServerGroup(this.Name));
            }

            base.DropImpl();

            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }

        #endregion

        #region Rename
        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        public void Rename(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException(RegSvrStrings.ArgumentNullOrEmpty("Name"));
            }

            if (this.IsSystemServerGroup)
            {
                throw new RegisteredServerException(RegSvrStrings.CannotRenameSystemServerGroup(this.Name));
            }

            Rename(new Key(name));
        }

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        /// <param name="key"></param>
        public void Rename(SfcKey key)
        {
            base.RenameImpl(key);

            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }

        ISfcScript ISfcRenamable.ScriptRename(SfcKey key)
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            Key tkey = (Key)key;
            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();

            args.Add(new SfcTsqlProcFormatter.RuntimeArg(tkey.Name.GetType(), tkey.Name));

            string script = scriptRenameAction.GenerateScript(this, args);
            return new SfcTSqlScript(script);
        }

        #endregion

        #region Move

        /// <summary>
        /// Moves the ServerGroup to be a child of another ServerGroup.
        /// </summary>
        /// <param name="newParent"></param>
        public void Move(ServerGroup newParent)
        {
            ((ISfcMovable)this).Move(newParent);
        }

        void ISfcMovable.Move(SfcInstance newParent)
        {
            if (newParent == null)
            {
                throw new ArgumentNullException("newParent");
            }

            if (!(newParent is ServerGroup))
            {
                throw new InvalidArgumentException("newParent");
            }
            base.MoveImpl(newParent);
            STrace.Assert(this.Parent == newParent);
            ServerGroup newParentGroup = (ServerGroup)newParent;
            STrace.Assert(newParentGroup.ServerGroups.Contains(this));

            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }

        ISfcScript ISfcMovable.ScriptMove(SfcInstance newParent)
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            STrace.Assert(newParent is ServerGroup);
            ServerGroup newParentGroup = (ServerGroup)newParent;
            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                                                         newParentGroup.ID.GetType(), newParentGroup.ID));

            string moveScript = scriptMoveAction.GenerateScript(this, args);
            return new SfcTSqlScript(moveScript);
        }

        #endregion

        private RegisteredServersStore GetStore()
        {
            RegisteredServersStore store = KeyChain.RootKey.Domain as RegisteredServersStore;
            STrace.Assert(store != null);
            return store;
        }

        /// <summary>
        /// Returns the IsLocal property of the RegisteredServersStore
        /// that this instance is associated with.
        /// </summary>
        [SfcIgnore]
        public bool IsLocal
        {
            get
            {
                ISfcDomain domain = this.KeyChain.RootKey.Domain;
                STrace.Assert(domain is RegisteredServersStore);
                return ((RegisteredServersStore)domain).IsLocal;
            }
        }


        /// <summary>
        /// Returns true if this servergroup is one among the standard 
        /// server groups.
        /// </summary>
        [SfcIgnore]
        public bool IsSystemServerGroup
        {
            get
            {
                if ((this.Parent != null) && (this.Parent.GetType().Equals(typeof(RegisteredServersStore))) &&
                     this.IsStandardServerGroup)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if this server group is one among the standard 
        /// server groups whose parent is not RegisteredServerStore.
        /// </summary>
        [SfcIgnore]
        internal bool IsStandardServerGroup
        {
            get
            {
                if (this.Name.Equals(RegisteredServersStore.databaseEngineServerGroupName) ||
                    this.Name.Equals(RegisteredServersStore.analysisServicesServerGroupName) ||
                    this.Name.Equals(RegisteredServersStore.reportingServicesServerGroupName) ||
                    this.Name.Equals(RegisteredServersStore.sqlServerCompactEditionServerGroupName) ||
                    this.Name.Equals(RegisteredServersStore.integrationServicesServerGroupName))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns if SFC believes this is a dropped object.
        /// </summary>
        [SfcIgnore]
        public bool IsDropped
        {
            get
            {
                return this.State == SfcObjectState.Dropped;
            }
        }

        /// <summary>
        /// Method to flatten the hierarchy for a ServerGroup and return 
        /// the complete list of descendant RegisteredServers.
        /// </summary>
        /// <returns></returns>
        public List<RegisteredServer> GetDescendantRegisteredServers()
        {
            List<RegisteredServer> regSrvList = new List<RegisteredServer>();

            GetDescendantRegisteredServersRec(regSrvList);

            return regSrvList;
        }

        /// <summary>
        /// Walks the tree recursively gathering the registered servers.
        /// </summary>
        /// <param name="regSrvList"></param>
        private void GetDescendantRegisteredServersRec(List<RegisteredServer> regSrvList)
        {
            regSrvList.AddRange(this.RegisteredServers);
            foreach (ServerGroup group in this.ServerGroups)
            {
                group.GetDescendantRegisteredServersRec(regSrvList);
            }
        }

        #region ISfcDiscoverObject Members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            if (sink.Action == SfcDependencyAction.Serialize)
            {
                foreach (RegisteredServer rs in this.RegisteredServers)
                {
                    sink.Add(SfcDependencyDirection.Inbound, rs, SfcTypeRelation.ContainedChild, false);
                }

                foreach (ServerGroup sg in this.ServerGroups)
                {
                    sink.Add(SfcDependencyDirection.Inbound, sg, SfcTypeRelation.ContainedChild, false);
                }
            }

            return;
        }

        #endregion


        /// <summary>
        /// Exports the content of the group to a file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cpt"></param>
        public void Export(string file, CredentialPersistenceType cpt)
        {
            RegisteredServersStore.Export(this, file, cpt);
        }

        /// <summary>
        /// The event that is raise when user attempts to import a registered server or server group that already 
        /// exists in the local store
        /// </summary>
        public event EventHandler<DuplicateFoundEventArgs> DuplicateFound;

        /// <summary>
        /// Raise the DuplicateFound event and capture reaction from the argument.
        /// </summary>
        /// <param name="obj">The registered server or the server group object to be replace</param>
        /// <param name="param">The parameters to supply to the method info</param>
        /// <param name="isLocal">Indicate whether this object is in the local store</param>
        /// <param name="applyToAll">Check if user had selected apply to all previously</param>
        /// <param name="confirm">The confirmation state to use if apply to all is used previously</param>
        /// <returns>Yes - object is deleted, Cancel - abort the operation, No - object is not deleted</returns>
        private DialogResult DeleteObject(object obj, object[] param, bool isLocal, ref bool applyToAll, ref bool confirm)
        {
            if (this.DuplicateFound != null)
            {
                DuplicateFoundEventArgs eventArgs = null;
                MethodInfo mi = obj.GetType().GetMethod("Drop");

                if (applyToAll)
                {
                    if (confirm)
                    {
                        if (mi != null)
                        {
                            mi.Invoke(obj, param);

                            if (isLocal)
                            {
                                this.RaiseSfcAppObjectDroppedEvent((SfcInstance)obj);
                            }
                        }
                    }
                    else
                    {
                        return DialogResult.No;
                    }
                }
                else
                {
                    eventArgs = new DuplicateFoundEventArgs();
                    this.DuplicateFound(obj, eventArgs);

                    if (eventArgs.Cancel)
                    {
                        return DialogResult.Cancel;
                    }

                    if (eventArgs.ApplyToAll)
                    {
                        applyToAll = true;
                        confirm = eventArgs.Confirm;
                    }

                    if (eventArgs.Confirm)
                    {
                        if (mi != null)
                        {
                            mi.Invoke(obj, param);

                            if (isLocal)
                            {
                                this.RaiseSfcAppObjectDroppedEvent((SfcInstance)obj);
                            }
                        }
                    }
                    else
                    {
                        return DialogResult.No;
                    }
                }
            }

            return DialogResult.Yes;
        }

        /// <summary>
        /// Imports groups and servers saved in the XML file and adds them
        /// as children.
        /// </summary>
        /// <param name="file"></param>
        public void Import(string file)
        {
            try
            {
                if (null == file)
                {
                    throw new ArgumentNullException("file");
                }

                bool applyToAll = false;
                bool confirm = false;

                using (XmlTextReader reader = new XmlTextReader(file) { DtdProcessing = DtdProcessing.Prohibit })
                {
                    SfcSerializer sfcSerializer = new SfcSerializer();

                    // for a local store we want to make the deserializer 
                    // initialize the objects in Existing state
                    SfcObjectState state = IsLocal ? SfcObjectState.Existing : SfcObjectState.Pending;

                    //Note:Moving objects are allowed only on existing parent objects; 
                    this.State = SfcObjectState.Existing;

                    object group = sfcSerializer.Deserialize(reader, state);
                    if (group is RegisteredServer)
                    {
                        RegisteredServer rs = group as RegisteredServer;

                        rs.Parent = this;
                        if (!IsLocal)
                        {
                            DialogResult result = DialogResult.Yes;

                            if (rs.Parent.RegisteredServers.Contains(rs.Name))
                            {
                                result = this.DeleteObject(
                                    rs.Parent.RegisteredServers[rs.Name],
                                    null,
                                    IsLocal,
                                    ref applyToAll,
                                    ref confirm);
                            }

                            if (DialogResult.Cancel == result)
                            {
                                return;
                            }
                            else if (DialogResult.Yes == result)
                            {
                                // create to save the server to the store
                                rs.Create();
                            }
                        }
                        else
                        {
                            DialogResult result = DialogResult.Yes;

                            if (this.RegisteredServers.Contains(rs.Name))
                            {
                                result = this.DeleteObject(
                                    this.RegisteredServers[rs.Name],
                                    null,
                                    IsLocal,
                                    ref applyToAll,
                                    ref confirm);
                            }

                            if (DialogResult.Cancel == result)
                            {
                                return;
                            }
                            else if (DialogResult.Yes == result)
                            {
                                this.RegisteredServers.Add(rs);
                                // call create event for UI update in the local store
                                this.RaiseSfcAppObjectCreatedEvent((SfcInstance)rs);
                            }
                        }
                    }
                    else if (group is ServerGroup)
                    {
                        ServerGroup childGroup = group as ServerGroup;
                        childGroup.Parent = this;

                        if (!IsLocal)
                        {
                            if (childGroup.IsStandardServerGroup)
                            {
                                List<RegisteredServer> servers = new List<RegisteredServer>(childGroup.RegisteredServers);
                                foreach (RegisteredServer rs in servers)
                                {
                                    rs.Parent = this;

                                    DialogResult result = DialogResult.Yes;

                                    if (rs.Parent.RegisteredServers.Contains(rs.Name))
                                    {
                                        result = this.DeleteObject(
                                            rs.Parent.RegisteredServers[rs.Name],
                                            null,
                                            IsLocal,
                                            ref applyToAll,
                                            ref confirm);
                                    }

                                    if (DialogResult.Cancel == result)
                                    {
                                        return;
                                    }
                                    else if (DialogResult.Yes == result)
                                    {
                                        // create to save the server to the store
                                        rs.Create();
                                    }
                                }

                                List<ServerGroup> localGroups = new List<ServerGroup>(childGroup.ServerGroups);
                                foreach (ServerGroup sg in localGroups)
                                {
                                    sg.Parent = this;

                                    DialogResult result = DialogResult.Yes;

                                    if (this.ServerGroups.Contains(sg.Name))
                                    {
                                        result = this.DeleteObject(
                                            this.ServerGroups[sg.Name],
                                            null,
                                            IsLocal,
                                            ref applyToAll,
                                            ref confirm);
                                    }

                                    if (DialogResult.Cancel == result)
                                    {
                                        return;
                                    }
                                    else if (DialogResult.Yes == result)
                                    {
                                        // create the group and its children            
                                        sg.DeepCreate();
                                    }
                                }
                            }
                            else
                            {
                                DialogResult result = DialogResult.Yes;

                                if (this.ServerGroups.Contains(childGroup.Name))
                                {
                                    result = this.DeleteObject(
                                        this.ServerGroups[childGroup.Name],
                                        null,
                                        IsLocal,
                                        ref applyToAll,
                                        ref confirm);
                                }

                                if (DialogResult.Cancel == result)
                                {
                                    return;
                                }
                                else if (DialogResult.Yes == result)
                                {
                                    childGroup.DeepCreate();
                                }
                            }
                        }
                        else
                        {
                            if (childGroup.IsStandardServerGroup)
                            {
                                List<ServerGroup> localGroups = new List<ServerGroup>(childGroup.ServerGroups);
                                foreach (ServerGroup sg in localGroups)
                                {
                                    DialogResult result = DialogResult.Yes;

                                    if (this.ServerGroups.Contains(sg.Name))
                                    {
                                        result = this.DeleteObject(
                                            this.ServerGroups[sg.Name],
                                            null,
                                            IsLocal,
                                            ref applyToAll,
                                            ref confirm);
                                    }

                                    if (DialogResult.Cancel == result)
                                    {
                                        return;
                                    }
                                    else if (DialogResult.Yes == result)
                                    {
                                        sg.Move(this);
                                        this.RaiseSfcAppObjectCreatedEvent((SfcInstance)sg);
                                    }
                                }

                                List<RegisteredServer> localRegSrvGrp = new List<RegisteredServer>(childGroup.RegisteredServers);
                                foreach (RegisteredServer regSrv in localRegSrvGrp)
                                {
                                    DialogResult result = DialogResult.Yes;

                                    if (this.RegisteredServers.Contains(regSrv.Name))
                                    {
                                        result = this.DeleteObject(
                                            this.RegisteredServers[regSrv.Name],
                                            null,
                                            IsLocal,
                                            ref applyToAll,
                                            ref confirm);
                                    }

                                    if (DialogResult.Cancel == result)
                                    {
                                        return;
                                    }
                                    else if (DialogResult.Yes == result)
                                    {
                                        regSrv.Move(this);
                                        this.RaiseSfcAppObjectCreatedEvent((SfcInstance)regSrv);
                                    }
                                }
                            }
                            else
                            {
                                DialogResult result = DialogResult.Yes;

                                if (this.ServerGroups.Contains(childGroup.Name))
                                {
                                    result = this.DeleteObject(
                                        this.ServerGroups[childGroup.Name],
                                        null,
                                        IsLocal,
                                        ref applyToAll,
                                        ref confirm);
                                }

                                if (DialogResult.Cancel == result)
                                {
                                    return;
                                }
                                else if (DialogResult.Yes == result)
                                {
                                    this.ServerGroups.Add(childGroup);
                                    GetStore().Serialize();
                                    this.RaiseSfcAppObjectCreatedEvent((SfcInstance)childGroup);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RegisteredServersStore.FilterException(e);

                throw new RegisteredServerException(RegSvrStrings.FailedOperation(RegSvrStrings.Import), e);
            }
        }


        /// <summary>
        /// Propagates the Create() call to all the subobjects
        /// </summary>
        internal void DeepCreate()
        {

            // cache the child objects since Create wipes them out
            List<RegisteredServer> servers = new List<RegisteredServer>(this.RegisteredServers.Count);
            foreach (RegisteredServer rs in this.RegisteredServers)
            {
                servers.Add(rs);
            }

            List<ServerGroup> groups = new List<ServerGroup>(this.ServerGroups.Count);
            foreach (ServerGroup sg in this.ServerGroups)
            {
                groups.Add(sg);
            }

            this.Create();

            foreach (RegisteredServer server in servers)
            {
                server.Create();
            }

            foreach (ServerGroup subGroup in groups)
            {
                subGroup.DeepCreate();
            }
        }

        internal void RaiseSfcAppObjectCreatedEvent(SfcInstance obj)
        {
            SfcApplication.Events.OnObjectCreated(obj, new SfcObjectCreatedEventArgs(obj.Urn, obj));
        }

        internal void RaiseSfcAppObjectDroppedEvent(SfcInstance obj)
        {
            SfcApplication.Events.OnObjectDropped(obj, new SfcObjectDroppedEventArgs(obj.Urn, obj));
        }
    }

    /// <summary>
    ///   Specifies identifiers to indicate the return value of a dialog box.
    ///   This is used only as a private variable and does not need to come from Windows.Forms
    /// </summary>
    internal enum DialogResult
    {
        //
        // Summary:
        //     The dialog box return value is Cancel (usually sent from a button labeled
        //     Cancel).
        Cancel = 0,
        //
        // Summary:
        //     The dialog box return value is Yes (usually sent from a button labeled Yes).
        Yes = 1,
        //
        // Summary:
        //     The dialog box return value is No (usually sent from a button labeled No).
        No = 2,
    }

    /// <summary>
    /// The event args of the event raised when user attempts to import a registered server or a server group that 
    /// already exists in the local store
    /// </summary>
    public class DuplicateFoundEventArgs : EventArgs
    {
        bool applyToAll = false;
        bool confirm = false;
        bool cancel = false;

        /// <summary>
        /// Apply the Confirm state to the rest of the object during the import.
        /// </summary>
        public bool ApplyToAll
        {
            get { return this.applyToAll; }
            set { this.applyToAll = value; }
        }

        /// <summary>
        /// Confirm to overwrite the object.
        /// </summary>
        public bool Confirm
        {
            get { return this.confirm; }
            set { this.confirm = value; }
        }

        /// <summary>
        /// Cancel the import operation.
        /// </summary>
        public bool Cancel
        {
            get { return this.cancel; }
            set { this.cancel = value; }
        }
    }
}
