// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// XEStore is the root for all metadata classes and runtime classes.
    /// </summary>
    public sealed class XEStore : BaseXEStore, ISfcDomain
    {
        /// <summary>
        /// Type name.
        /// </summary>
        public const string TypeTypeName = "XEStore";

        private const string NameSpace = "http://schemas.microsoft.com/sqlserver/2008/07/extendedeventsconfig";
        private const string DomainName = "XEvent";

        /// <summary>
        /// Don't ever call this, or if you do remember to set SfcConnection
        /// </summary>
        internal XEStore()
            : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XEStore"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public XEStore(SqlStoreConnection connection)
            : base(connection)
        {
        }

        /// <summary>
        /// The string identity of a policy store is the associated Server name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // ToString is overriden here since the default from SfcInstance is to use whatever the Key.ToString() does,
            // and since XEStore doesn't have any Key fields per se, we override it on the class itself and make our own string.
            return String.Format(CultureInfo.InvariantCulture, "{0} (Server='{1}')", TypeTypeName,
                this.SfcConnection != null ? SfcSecureString.EscapeSquote(this.SfcConnection.ServerInstance) : "");            
        }

        /// <summary>
        /// This is used by SFC.
        /// </summary>
        /// <returns></returns>
        protected override SfcKey CreateIdentityKey()
        {
            return new ServerKey(this);
        }


        /// <summary>
        ///  This is used by SFC.
        /// </summary>
        /// <returns></returns>
        [SfcIgnore]
        public override SfcKey IdentityKey
        {
            get { return (ServerKey)this.AbstractIdentityKey; }
        }

        #region ISfcHasConnection Members

        private SfcConnectionContext context = null;

        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting a single serial query, so the query must end before another one may begin.
        /// </summary>
        /// <returns></returns>
        ISfcConnection ISfcHasConnection.GetConnection()
        {
            return this.SfcConnection;
        }

        /// <summary>
        /// Sets the active connection.
        /// </summary>
        void ISfcHasConnection.SetConnection(ISfcConnection connection)
        {
            this.SetConnection(connection);
        }

        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting either a single serial query or multiple simultaneously open queries as requested.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns>The connection to use, or null to use Cache mode. Cache mode avoids connection and open data reader issues.</returns>
        ISfcConnection ISfcHasConnection.GetConnection(SfcObjectQueryMode mode)
        {
            return this.StoreProvider.GetConnection(mode);
        }

       
        /// <summary>
        /// 
        /// </summary>
        SfcConnectionContext ISfcHasConnection.ConnectionContext
        {
            get
            {
                if (context == null)
                {
                    // If our SqlStoreConnection is still null when this is called, we are forced into Offline mode.
                    context = new SfcConnectionContext(this);
                }
                return context;
            }
        }

        #endregion

        #region ISfcDomain Members

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        Type ISfcDomain.GetType(string typeName)
        {
            switch (typeName)
            {
                case XEStore.TypeTypeName: return typeof(XEStore);
                case Package.TypeTypeName: return typeof(Package);
                case EventInfo.TypeTypeName: return typeof(EventInfo);
                case EventColumnInfo.TypeTypeName: return typeof(EventColumnInfo);
                case DataEventColumnInfo.TypeTypeName: return typeof(DataEventColumnInfo);
                case ReadOnlyEventColumnInfo.TypeTypeName: return typeof(ReadOnlyEventColumnInfo);
                case ActionInfo.TypeTypeName: return typeof(ActionInfo);
                case TargetInfo.TypeTypeName: return typeof(TargetInfo);
                case TargetColumnInfo.TypeTypeName: return typeof(TargetColumnInfo);
                case PredSourceInfo.TypeTypeName: return typeof(PredSourceInfo);
                case PredCompareInfo.TypeTypeName: return typeof(PredCompareInfo);
                case TypeInfo.TypeTypeName: return typeof(TypeInfo);
                case MapInfo.TypeTypeName: return typeof(MapInfo);
                case MapValueInfo.TypeTypeName: return typeof(MapValueInfo);
                case Session.TypeTypeName: return typeof(Session);
                case Event.TypeTypeName: return typeof(Event);
                case Action.TypeTypeName: return typeof(Action);
                case EventField.TypeTypeName: return typeof(EventField);
                case Target.TypeTypeName: return typeof(Target);
                case TargetField.TypeTypeName: return typeof(TargetField);
            }

            return null;
        }

        /// <summary>
        /// returns the Key object given Urn fragment
        /// </summary>
        /// <param name="urnFragment">The urn fragment.</param>
        /// <returns>SfcKey</returns>
        SfcKey ISfcDomain.GetKey(IUrnFragment urnFragment)
        {
            switch (urnFragment.Name)
            {
                case XEStore.TypeTypeName: return new XEStore.ServerKey(this);
                case Package.TypeTypeName: return new Package.Key(urnFragment.FieldDictionary);
                case EventInfo.TypeTypeName: return new EventInfo.Key(urnFragment.FieldDictionary);
                case EventColumnInfo.TypeTypeName: return new EventColumnInfo.Key(urnFragment.FieldDictionary);
                case DataEventColumnInfo.TypeTypeName: return new DataEventColumnInfo.Key(urnFragment.FieldDictionary);
                case ReadOnlyEventColumnInfo.TypeTypeName: return new ReadOnlyEventColumnInfo.Key(urnFragment.FieldDictionary);
                case ActionInfo.TypeTypeName: return new ActionInfo.Key(urnFragment.FieldDictionary);
                case TargetInfo.TypeTypeName: return new TargetInfo.Key(urnFragment.FieldDictionary);
                case TargetColumnInfo.TypeTypeName: return new TargetColumnInfo.Key(urnFragment.FieldDictionary);
                case PredSourceInfo.TypeTypeName: return new PredSourceInfo.Key(urnFragment.FieldDictionary);
                case PredCompareInfo.TypeTypeName: return new PredCompareInfo.Key(urnFragment.FieldDictionary);
                case TypeInfo.TypeTypeName: return new TypeInfo.Key(urnFragment.FieldDictionary);
                case MapInfo.TypeTypeName: return new MapInfo.Key(urnFragment.FieldDictionary);
                case MapValueInfo.TypeTypeName: return new MapValueInfo.Key(urnFragment.FieldDictionary);
                case Session.TypeTypeName: return new Session.Key(urnFragment.FieldDictionary);
                case Event.TypeTypeName: return new Event.Key(urnFragment.FieldDictionary);
                case Action.TypeTypeName: return new Action.Key(urnFragment.FieldDictionary);
                case EventField.TypeTypeName: return new EventField.Key(urnFragment.FieldDictionary);
                case Target.TypeTypeName: return new Target.Key(urnFragment.FieldDictionary);
                case TargetField.TypeTypeName: return new TargetField.Key(urnFragment.FieldDictionary);
            }
            throw new XEventException(ExceptionTemplates.UnsupportedKey(urnFragment.Name));
        }

        /// <summary>
        /// Gets the execution engine.
        /// </summary>
        /// <returns></returns>
        ISfcExecutionEngine ISfcDomain.GetExecutionEngine()
        {
            return this.ExecutionEngine;
        }

        SfcTypeMetadata ISfcDomain.GetTypeMetadata(string typeName)
        {
            switch (typeName)
            {
                case Event.TypeTypeName:
                    return Event.GetTypeMetadata();
                case Target.TypeTypeName:
                    return Target.GetTypeMetadata();
                case Action.TypeTypeName:
                    return Action.GetTypeMetadata();
                case EventField.TypeTypeName:
                    return EventField.GetTypeMetadata();
                case TargetField.TypeTypeName:
                    return TargetField.GetTypeMetadata();
                case Session.TypeTypeName:
                case XEStore.TypeTypeName:
                    return null;
                default:
                    TraceHelper.TraceContext.TraceError("Unknown typeName.");
                    throw new XEventException(ExceptionTemplates.InvalidParameter("typeName"));
            }
        }

        bool ISfcDomain.UseSfcStateManagement()
        {
            return true;    // XEvent uses SFC-provided state management
        }

        /// <summary>
        /// Returns the logical version of the domain
        /// </summary>
        /// <returns></returns>
        int ISfcDomainLite.GetLogicalVersion()
        {
            // 1 = Katmai CTP5
            // 2 = Katmai CTP6
            return 3;      // logical version changes only when the schema of domain changes
        }

        /// <summary>
        /// Gets the name of the domain.
        /// </summary>
        /// <value>The name of the domain.</value>
        [SfcIgnore]
        string ISfcDomainLite.DomainName
        {
            get { return XEStore.DomainName; }
        }

        /// <summary>
        /// Gets the name of the domain instance.
        /// </summary>
        /// <value>The name of the domain instance.</value>
        [SfcIgnore]
        string ISfcDomainLite.DomainInstanceName
        {
            get
            {
                if ((this as ISfcHasConnection).ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    return XEStore.DomainName;
                }
                else
                {
                    return this.StoreProvider.DomainInstanceName;
                }
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public sealed class ServerKey : DomainRootKey
        {
            /// <summary>
            /// Default constructor for generic Key generation
            /// </summary>
            public ServerKey()
                : base(null) // Caller has to remember to set Root!
            {
            }

            internal ServerKey(ISfcDomain root)
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
            public static new bool Equals(object obj1, object obj2)
            {
                return (obj1 as ServerKey) == (obj2 as ServerKey);
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
            public static bool operator ==(object obj, ServerKey rightOperand)
            {
                if (obj == null || obj is ServerKey)
                    return (ServerKey)obj == rightOperand;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator ==(ServerKey leftOperand, object obj)
            {
                if (obj == null || obj is ServerKey)
                    return leftOperand == (ServerKey)obj;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(ServerKey leftOperand, ServerKey rightOperand)
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
            public static bool operator !=(object obj, ServerKey rightOperand)
            {
                return !(obj == rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator !=(ServerKey leftOperand, object obj)
            {
                return !(leftOperand == obj);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(ServerKey leftOperand, ServerKey rightOperand)
            {
                return !(leftOperand == rightOperand);
            }

            private bool IsEqual(ServerKey other)
            {
                return (this.Domain == other.Domain);
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                // SessionStore has valid name only in connected mode, so we add the Name attribute only in that case
                if (this.Domain.ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    return XEStore.TypeTypeName;
                }
                else
                {
                    return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", XEStore.TypeTypeName, this.Domain.DomainInstanceName);
                }
            }
        }

        /// <summary>
        /// Gets provider to perform operations on the Store.
        /// </summary>
        protected override IXEStoreProvider GetStoreProvider()
        {
            return new ServerXEStoreProvider(this);
        }

        /// <summary>
        /// Gets provider to perform Session operations.
        /// </summary>
        protected override ISessionProvider GetSessionProivder(Session session)
        {
            return new ServerSessionProvider(session);
        }

        /// <summary>
        /// Gets provider to perform Target operations.
        /// </summary>
        protected override ITargetProvider GetTargetProvider(Target target)
        {
            return new ServerTargetProvider(target);
        }

        /// <summary>
        /// Gets provider to perform Event operations.
        /// </summary>
        protected override IEventProvider GetEventProvider(Event xevent)
        {
            return new ServerEventProvider(xevent);
        }
    }
}
