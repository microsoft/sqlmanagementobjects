// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// XEStore is the root for all metadata classes and runtime classes.
    /// </summary>
    public abstract partial class BaseXEStore : SfcInstance, ISfcDomain
    {
        private const string NameSpace = "http://schemas.microsoft.com/sqlserver/2008/07/extendedeventsconfig";
        private const string DomainName = "XEvent";
        private const string typeName = "XEStore";

        private static Guid package0Guid = new Guid("60AA9FBF-673B-4553-B7ED-71DCA7F5E972");

        private ISfcConnection originalConnection;
        private ISfcConnection connection;
        private SfcConnectionContext context;
        private IComparer<string> comparer;

        private SessionCollection sessions;
        private PackageCollection packages;

        private IXEStoreProvider storeProvider;

        /// <summary>
        /// Gets provider to perform Store operations.
        /// </summary>
        protected abstract IXEStoreProvider GetStoreProvider();

        /// <summary>
        /// Gets provider to perform Session operations.
        /// </summary>
        protected abstract ISessionProvider GetSessionProivder(Session session);

        /// <summary>
        /// Gets provider to perform Target operations.
        /// </summary>
        protected abstract ITargetProvider GetTargetProvider(Target target);

        /// <summary>
        /// Gets provider to perform Event operations.
        /// </summary>
        protected abstract IEventProvider GetEventProvider(Event xevent);

        /// <summary>
        /// Gets provider to perform Session operations.
        /// </summary>
        internal ISessionProvider GetSessionProviderInternal(Session session)
        {
            return this.GetSessionProivder(session);
        }

        /// <summary>
        /// Gets provider to perform Target operations.
        /// </summary>
        internal ITargetProvider GetTargetProviderInternal(Target target)
        {
            return this.GetTargetProvider(target);
        }

        /// <summary>
        /// Gets provider to perform Event operations.
        /// </summary>
        internal IEventProvider GetEventProviderInternal(Event xevent)
        {
            return this.GetEventProvider(xevent);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseXEStore"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public BaseXEStore(ISfcConnection connection)
            : this()
        {
            TraceHelper.TraceContext.TraceParameterIn("Connection", connection);
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            this.originalConnection = connection;

            this.InitConnection();
        }

        /// <summary>
        /// Don't ever call this, or if you do remember to set SfcConnection
        /// </summary>
        protected BaseXEStore()
        {
        }

        #region Public properties

        /// <summary>
        /// Gets the name of XEStore.
        /// </summary>
        /// <value>The name.</value>
        public virtual string Name
        {
            get
            {
                if (this.SfcConnection == null)
                    return null;
                return this.SfcConnection.ServerInstance;
            }
            protected set
            {

            }
        }

        /// <summary>
        /// Gets the name of XEStore.
        /// </summary>
        /// <value>The name.</value>
        public virtual string ServerName
        {
            get
            {
                if (this.SfcConnection == null)
                    return null;
                return this.SfcConnection.ServerInstance;
            }
        }

        /// <summary>
        /// Gets the sessions.
        /// </summary>
        /// <value>The sessions.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Session))]
        public SessionCollection Sessions
        {
            get
            {
                if (this.sessions == null)
                {
                    this.sessions = new SessionCollection(this);
                }
                return this.sessions;
            }
        }

        /// <summary>
        /// Gets the packages.
        /// </summary>
        /// <value>The packages.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(Package))]
        public PackageCollection Packages
        {
            get
            {
                if (this.packages == null)
                {
                    this.packages = new PackageCollection(this);
                }
                return this.packages;
            }
        }


        /// <summary>
        /// Gets the running session count.
        /// </summary>
        /// <value>The running session count.</value>
        [SfcProperty(Data = true)]
        public int RunningSessionCount
        {
            get
            {
                SfcProperty p = this.Properties["RunningSessionCount"];
                return (int)p.Value;
            }
        }

        #endregion

        #region Readonly packages and targets

        /// <summary>
        /// Gets the package0 package.
        /// </summary>
        /// <value>The package0 package.</value>
        [SfcIgnore]
        public Package Package0Package
        {
            get { return Packages["package0"]; }
        }


        /// <summary>
        /// Gets the histogram target info.
        /// </summary>
        /// <value>The histogram target info.</value>
        [SfcIgnore]
        public TargetInfo HistogramTargetInfo
        {
            get { return Packages["package0"].TargetInfoSet["histogram"]; }
        }

        /// <summary>
        /// Gets the event_file target info.
        /// </summary>
        /// <value>The event_file target info.</value>
        [SfcIgnore]
        public TargetInfo EventFileTargetInfo
        {
            get { return Packages["package0"].TargetInfoSet["event_file"]; }
        }

        /// <summary>
        /// Gets the etw_classic_sync_target target info.
        /// </summary>
        /// <value>The etw_classic_sync_target target info.</value>
        [SfcIgnore]
        public TargetInfo EtwClassicSyncTargetInfo
        {
            get { return Packages["package0"].TargetInfoSet["etw_classic_sync_target"]; }
        }

        /// <summary>
        /// Gets the pair_matching target info.
        /// </summary>
        /// <value>The pair_matching target info.</value>
        [SfcIgnore]
        public TargetInfo PairMatchingTargetInfo
        {
            get { return Packages["package0"].TargetInfoSet["pair_matching"]; }
        }

        /// <summary>
        /// Gets the ring_buffer target info.
        /// </summary>
        /// <value>The ring_buffer target info.</value>
        [SfcIgnore]
        public TargetInfo RingBufferTargetInfo
        {
            get { return Packages["package0"].TargetInfoSet["ring_buffer"]; }
        }

        /// <summary>
        /// Gets the event_counter target info.
        /// </summary>
        /// <value>The event_counter target info.</value>
        [SfcIgnore]
        public TargetInfo EventCounterTargetInfo
        {
            get { return Packages["package0"].TargetInfoSet["event_counter"]; }
        }


        #endregion

        #region ISfcHasConnection Members

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
        /// Sets the active connection.
        /// </summary>
        protected void SetConnection(ISfcConnection connection)
        {
            // We expect it to be SqlStoreConnection.
            // However Powershell provider uses ServerConnection, so we have to allow it.

            ISfcConnection storeConnection = connection as SqlStoreConnection;

            if (storeConnection == null)
            {
                storeConnection = connection as ServerConnection;
            }

            if (storeConnection == null)
            {
                throw new ArgumentException("", "connection");
            }

            this.originalConnection = storeConnection;
            this.storeProvider = this.GetStoreProvider();

            this.InitConnection();
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

        /// <summary>
        /// The string identity of a policy store is the associated Server name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // ToString is overriden here since the default from SfcInstance is to use whatever the Key.ToString() does,
            // and since XEStore doesn't have any Key fields per se, we override it on the class itself and make our own string.
            return String.Format(CultureInfo.InvariantCulture, "{0} (Server='{1}')", BaseXEStore.typeName,
                this.SfcConnection != null ? SfcSecureString.EscapeSquote(this.SfcConnection.ServerInstance) : "");
        }

        /// <summary>
        ///  This is used by SFC.
        /// </summary>
        /// <returns></returns>
        [SfcIgnore]
        public virtual SfcKey IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// This is used by SFC.
        /// </summary>
        /// <returns></returns>
        protected override SfcKey CreateIdentityKey()
        {
            return new Key(this);
        }


        /// <summary>
        /// This is used by SFC.
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            TraceHelper.TraceContext.TraceParameterIn("elementType", elementType);
            switch (elementType)
            {
                case Session.TypeTypeName:
                    return this.Sessions;
                case Package.TypeTypeName:
                    return this.Packages;
                default:
                    TraceHelper.TraceContext.TraceError("No such collection for type {0}", elementType);
                    throw new XEventException(ExceptionTemplates.NoSuchCollection(elementType));
            }
        }

        /// <summary>
        ///  Gets the comparer for the child collections
        /// </summary>
        public IComparer<string> GetComparer()
        {
            return this.comparer;
        }

        /// <summary>
        /// Gets connection used to instantiate the store
        /// </summary>
        public ISfcConnection OriginalConnection
        {
            get
            {
                return this.originalConnection;
            }
            protected set
            {
                this.originalConnection = value;
            }
        }

        /// <summary>
        /// Initializes connections and related objects.
        /// Should not be called directly.
        /// </summary>
        protected void InitConnection()
        {
            this.comparer = this.StoreProvider.GetComparer();
            this.execEngine = this.StoreProvider.GetExecutionEngine();

            // This is a strange looking circular dependency -
            // Provider is instantiated from store's connection set a few lines above.
            // The intention is to give Provider a chance to override connection
            // (For SqlAzure Provider will switch connection to target DB).
            this.connection = this.StoreProvider.GetConnection(SfcObjectQueryMode.SingleActiveQuery);

            try
            {
                MarkRootAsConnected(); // setting a connection makes the server "live"                
            }
            catch (InvalidVersionEnumeratorException e)
            {
                throw new XEventException(ExceptionTemplates.InvalidVersion(this.connection.ServerVersion.ToString()), e);
            }
        }

        /// <summary>
        /// Gets or sets the SQL store connection.
        /// </summary>
        /// <value>The SQL store connection.</value>
        [SfcIgnore]
        public ISfcConnection SfcConnection
        {
            get
            {
                return this.connection;
            }
        }

        private SfcObjectQuery objectQuery = null;

        /// <summary>
        /// Gets the SFC object query.
        /// </summary>
        /// <value>The SFC object query.</value>
        [SfcIgnore]
        internal SfcObjectQuery SfcObjectQuery
        {
            get
            {
                if (this.objectQuery == null)
                {
                    this.objectQuery = new SfcObjectQuery(this);
                }
                return this.objectQuery;
            }
        }

        #region ISfcDomain Members

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        Type ISfcDomain.GetType(string typeName)
        {
            return null;
        }

        /// <summary>
        /// returns the Key object given Urn fragment
        /// </summary>
        /// <param name="urnFragment">The urn fragment.</param>
        /// <returns>SfcKey</returns>
        SfcKey ISfcDomain.GetKey(IUrnFragment urnFragment)
        {
            return null;
        }

        private ISfcExecutionEngine execEngine;

        /// <summary>
        /// Gets ExecutionEngine to perform operations on the Store.
        /// </summary>
        protected ISfcExecutionEngine ExecutionEngine
        {
            get { return this.execEngine; }
        }

        /// <summary>
        /// Gets the execution engine.
        /// </summary>
        /// <returns></returns>
        ISfcExecutionEngine ISfcDomain.GetExecutionEngine()
        {
            return this.execEngine;
        }

        SfcTypeMetadata ISfcDomain.GetTypeMetadata(string typeName)
        {
            return null;
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
            get { return BaseXEStore.DomainName; }
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
                    return BaseXEStore.DomainName;
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
                //SessionStore has valid name only in connected mode, so we add the Name attribute only in that case
                if (this.Domain.ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    return BaseXEStore.typeName;
                }
                else
                {
                    return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", BaseXEStore.typeName, this.Domain.DomainInstanceName);
                }
            }
        }

        /// <summary>
        /// Formats the field value based on the type information.
        /// </summary>
        /// <param name="fieldValue"> string represenation of Value of EventField, TargetField or PredValue </param>
        /// <param name="typePackageID"> identity of the package containing the type name</param>
        /// <param name="typeName"> represents the corresponding managed type</param>
        /// <returns></returns>
        public virtual string FormatFieldValue(string fieldValue, Guid typePackageID, string typeName)
        {
            if (this.Packages[typePackageID].MapInfoSet.Contains(typeName))
            {
                return string.Format("({0}{1}{0})", int.TryParse(fieldValue, out _) ? "" : "'", fieldValue);
            }

            if (typePackageID.Equals(package0Guid)) // we only know how to process types under package0
            {
                switch (typeName)
                {
                    case "boolean":
                        return fieldValue.Equals("true", StringComparison.OrdinalIgnoreCase) || fieldValue == "1" ? "(1)" : "(0)";

                    case "activity_id":
                    case "activity_id_xfer":
                    case "cpu_cycle":
                    case "float32":
                    case "float64":
                    case "int8":
                    case "int16":
                    case "int32":
                    case "int64":
                    case "uint8":
                    case "uint16":
                    case "uint32":
                    case "uint64":
                    case "ptr":
                        return "(" + fieldValue + ")";

                    case "ansi_string":
                    case "ansi_string_ptr":
                    case "callstack":
                    case "char":
                    case "guid":
                    case "guid_ptr":
                    case "filetime":
                    case "sos_context":
                        return "'" + SfcTsqlProcFormatter.EscapeString(fieldValue, '\'') + "'";

                    case "binary_data":
                        if (fieldValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            return fieldValue;
                        return "0x" + fieldValue;

                    case "unicode_string":
                    case "unicode_string_ptr":
                    case "wchar":
                    case "xml":
                        return "N'" + SfcTsqlProcFormatter.EscapeString(fieldValue, '\'') + "'";

                }
            }

            throw new XEventException(ExceptionTemplates.UnknownType, new ArgumentOutOfRangeException("typeName", typeName, "Unknown type."));
        }


        /// <summary>
        /// Gets the string representation of the predicate expression.
        /// </summary>
        /// <param name="predExpr"></param>
        /// <returns></returns>
        public virtual string FormatPredicateExpression(PredExpr predExpr)
        {
            switch (predExpr.GetType().Name)
            {
                case "PredLogicalExpr":
                    PredLogicalExpr logicalExpr = (PredLogicalExpr)predExpr;
                    switch (logicalExpr.Operator)
                    {
                        case PredLogicalExpr.LogicalOperatorType.Not:
                            return string.Format(CultureInfo.InvariantCulture, "(NOT {0})", this.FormatPredicateExpression(logicalExpr.LeftExpr));
                        case PredLogicalExpr.LogicalOperatorType.And:
                            return string.Format(CultureInfo.InvariantCulture, "({0} AND {1})",
                                this.FormatPredicateExpression(logicalExpr.LeftExpr), this.FormatPredicateExpression(logicalExpr.RightExpr));
                        case PredLogicalExpr.LogicalOperatorType.Or:
                            return string.Format(CultureInfo.InvariantCulture, "({0} OR {1})",
                                this.FormatPredicateExpression(logicalExpr.LeftExpr), this.FormatPredicateExpression(logicalExpr.RightExpr));
                    }

                    break;

                case "PredFunctionExpr":
                    PredFunctionExpr funcExpr = (PredFunctionExpr)predExpr;

                    string operandString = funcExpr.Operand.ToString();
                    string valueString = this.FormatFieldValue(funcExpr.Value.ToString(), funcExpr.Operand.TypePackageId, funcExpr.Operand.TypeName);

                    // if pkgName.objName is not unique
                    if (this.ObjectInfoSet.GetAll<PredCompareInfo>(funcExpr.Operator.Parent.Name, funcExpr.Operator.Name).Count > 1)
                    {
                        return String.Format(CultureInfo.InvariantCulture, "([{4}].{0}.{1}({2},{3}))",
                            SfcTsqlProcFormatter.MakeSqlBracket(funcExpr.Operator.Parent.Name), SfcTsqlProcFormatter.MakeSqlBracket(funcExpr.Operator.Name), operandString, valueString, funcExpr.Operator.Parent.ModuleID.ToString());
                    }

                    return String.Format(CultureInfo.InvariantCulture, "({0}.{1}({2},{3}))",
                        SfcTsqlProcFormatter.MakeSqlBracket(funcExpr.Operator.Parent.Name), SfcTsqlProcFormatter.MakeSqlBracket(funcExpr.Operator.Name), operandString, valueString);

                case "PredCompareExpr":
                    PredCompareExpr compExpr = (PredCompareExpr)predExpr;
                    StringBuilder sb = new StringBuilder(50);
                    sb.Append("(");
                    sb.Append(compExpr.Operand.ToString());

                    switch (compExpr.Operator)
                    {
                        case PredCompareExpr.ComparatorType.EQ:
                            sb.Append("=");
                            break;
                        case PredCompareExpr.ComparatorType.NE:
                            sb.Append("<>");
                            break;
                        case PredCompareExpr.ComparatorType.GT:
                            sb.Append(">");
                            break;
                        case PredCompareExpr.ComparatorType.GE:
                            sb.Append(">=");
                            break;
                        case PredCompareExpr.ComparatorType.LT:
                            sb.Append("<");
                            break;
                        case PredCompareExpr.ComparatorType.LE:
                            sb.Append("<=");
                            break;
                    }

                    sb.Append(this.FormatFieldValue(compExpr.Value.ToString(), compExpr.Operand.TypePackageId, compExpr.Operand.TypeName));

                    sb.Append(")");
                    return sb.ToString();
            }

            TraceHelper.TraceContext.Assert(false, "Unrecognized PredExpr");
            return null;
        }

        /// <summary>
        /// Gets provider for store operations.
        /// </summary>
        protected IXEStoreProvider StoreProvider
        {
            get
            {
                if (this.storeProvider == null)
                {
                    this.storeProvider = this.GetStoreProvider();
                }

                return this.storeProvider;
            }
        }
    }
}
