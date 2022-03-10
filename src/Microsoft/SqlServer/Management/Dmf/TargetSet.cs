// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the TargetSet class.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class TargetSet : SfcInstance
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "TargetSet");
        internal static readonly string DatabaseLevel = "Server/Database";

        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptCreateReferenceAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterReferenceAction = new SfcTsqlProcFormatter();
        static readonly string smoDatabaseUrnSkeleton = null;

        static TargetSet()
        {
            // Create script
            scriptCreateAction.Procedure = "msdb.dbo.sp_syspolicy_add_target_set";
            // BUGBUG: The sproc shows the policy_name parameter as optional with a default argument. Is the policy_name parameter
            // really required? If so, should the sproc definition change?
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("object_set_name", true));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("type_skeleton", "TargetTypeSkeleton", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("type", "TargetType", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("enabled", "Enabled", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_set_id", "ID", false, true));

            scriptCreateReferenceAction.Procedure = "msdb.dbo.sp_syspolicy_add_target_set_level";
            scriptCreateReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_set_id", true));
            scriptCreateReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("type_skeleton", "TargetTypeSkeleton", true, false));
            scriptCreateReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("level_name", "LevelName", true, false));
            scriptCreateReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("condition_name", "Condition", true, false));
            scriptCreateReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_set_level_id", true));

            // Alter script
            scriptAlterAction.Procedure = "msdb.dbo.sp_syspolicy_update_target_set";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_set_id", "ID", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("enabled", "Enabled", true, false));

            scriptAlterReferenceAction.Procedure = "msdb.dbo.sp_syspolicy_update_target_set_level";
            scriptAlterReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_set_id", true));
            scriptAlterReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("type_skeleton", "TargetTypeSkeleton", true, false));
            scriptAlterReferenceAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("condition_name", "Condition", true, false));


            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_syspolicy_delete_target_set";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_set_id", "ID", true, false));

            // Find out the URN skeleton for SMO Databases so we can
            // tell if a TargetFilter impacts Databases or Database
            // descendants.

            // 1/8/08 GrigoryP fix #180004 commenting out this code till we make it work in SQLCLR
            //                             using literal for now
            //
            //List<string> dbSkeletons = SfcMetadataDiscovery.GetUrnSkeletonsFromType(typeof(SMO.Database));
            //traceContext.DebugAssert(dbSkeletons.Count == 1);
            //smoDatabaseUrnSkeleton = dbSkeletons[0];
            smoDatabaseUrnSkeleton = @"Server/Database";
        }

        #region TypeMetadata support

        sealed class TypeMetadata : SfcTypeMetadata
        {
            static readonly TypeMetadata instance = new TypeMetadata();
            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static TypeMetadata() { }
            TypeMetadata() { }
            public static TypeMetadata Instance
            {
                get { return instance; }
            }

            public override bool IsCrudActionHandledByParent(SfcDependencyAction depAction)
            {
                // Target filter is always handled by its parent, for all CRUD operations
                switch (depAction)
                {
                    case SfcDependencyAction.Create:
                    case SfcDependencyAction.Rename:
                    case SfcDependencyAction.Alter:
                    case SfcDependencyAction.Drop:
                        return true;
                    default:
                        throw new DmfException(ExceptionTemplatesSR.UnsupportedCrudDependencyAction(depAction.ToString()));
                }
            }
        }

        static internal SfcTypeMetadata GetTypeMetadata()
        {
            return TypeMetadata.Instance;
        }

        #endregion
        #region CRUD

        /// <summary>
        /// Script Create the object
        /// </summary>
        internal ISfcScript ScriptCreate(bool declareArguments)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptCreate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(declareArguments);
                SfcTSqlScript dmfScript = new SfcTSqlScript();

                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(this.Parent.Name.GetType(), this.Parent.Name));

                List<SfcTsqlProcFormatter.RuntimeArg> refargs = new List<SfcTsqlProcFormatter.RuntimeArg>();
                refargs.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), "@target_set_id"));
                refargs.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), 0));

                // fix TargetType. It can be that the object has been deserialized,
                // or that we have been built with the default constructor
                // and in both these cases the property is missing
                if (null != this.Properties["TargetTypeSkeleton"].Value)
                {
                    this.Properties["TargetType"].Value = GetTargetType(new Urn(this.Properties["TargetTypeSkeleton"].Value as string).XPathExpression.ExpressionSkeleton);
                }

                string script = scriptCreateAction.GenerateScript(this, args, declareArguments);
                methodTraceContext.TraceVerbose("Script generated: " + script);

                dmfScript.AddBatch(script);

                foreach (TargetSetLevel level in Levels)
                {
                    // TODO: Move this script to the TSL. However, the TS should invoke the TSL and get it's script
                    // Very similar to the ObjectSet and TS interaction
                    string refscript = scriptCreateReferenceAction.GenerateScript(level, refargs, declareArguments);
                    methodTraceContext.TraceVerbose("Script generated: " + refscript);
                    dmfScript.AddBatch(refscript);
                }

                methodTraceContext.TraceParameterOut("returnVal", dmfScript);
                return dmfScript;
            }
        }

        /// <summary>
        /// Script Drop this object
        /// </summary>
        internal ISfcScript ScriptDrop()
        {
            string script = scriptDropAction.GenerateScript(this);
            SfcTSqlScript dmfScript = new SfcTSqlScript(script);
            return dmfScript;
        }

        /// <summary>
        /// Script Alter this object
        /// </summary>
        internal ISfcScript ScriptAlter()
        {
            SfcTSqlScript dmfScript = new SfcTSqlScript("");
            dmfScript.AddBatch(scriptAlterAction.GenerateScript(this));

            List<SfcTsqlProcFormatter.RuntimeArg> refargs = new List<SfcTsqlProcFormatter.RuntimeArg>();
            refargs.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), this.ID));

            foreach (TargetSetLevel level in Levels)
            {
                // TODO: Move this script to the TSL. However, the TS should invoke the TSL and get it's script
                // Very similar to the ObjectSet and TS interaction
                string refscript = scriptAlterReferenceAction.GenerateScript(level, refargs);
                dmfScript.AddBatch(refscript);
            }

            return dmfScript;
        }

        #endregion

        /// <summary>
        /// Default constructor used for deserialization.
        /// </summary>
        public TargetSet()
        {
        }

        private Urn m_urn;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="targetTypeSkeleton"></param>
        public TargetSet(ObjectSet parent, string targetTypeSkeleton)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("TargetSet", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(parent, targetTypeSkeleton);
                m_urn = new Urn(targetTypeSkeleton);

                if (!m_urn.IsValidUrnSkeleton())
                {
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.InvalidUrnSkeleton(targetTypeSkeleton)));
                }

                this.Parent = parent;
                this.Properties["TargetTypeSkeleton"].Value = targetTypeSkeleton;

                this.rootLevel = m_urn.XPathExpression.BlockExpressionSkeleton(0);

                for (int i = 1; i < m_urn.XPathExpression.Length; i++)
                {
                    Levels.Add(new TargetSetLevel(this, m_urn.XPathExpression.BlockExpressionSkeleton(i)));
                }

                this.Properties["TargetType"].Value = GetTargetType(m_urn.XPathExpression.ExpressionSkeleton);
            }
        }

        TargetSetLevelCollection m_Levels;

        /// <summary>
        /// Collection of TargetSetLevel objects.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(TargetSetLevel))]
        public TargetSetLevelCollection Levels
        {
            get
            {
                if (m_Levels == null)
                {
                    ServerComparer cmp = null;

                    SfcConnectionContextMode mode = SfcConnectionContextMode.Offline;
                    // figure out if we are connected
                    if (this.Parent != null && this.Parent.Parent != null)
                    {
                        mode = ((ISfcHasConnection)this.Parent.Parent).ConnectionContext.Mode;
                    }

                    // if state is not pending and we are not disconnected connected
                    if (this.State != SfcObjectState.Pending && mode != SfcConnectionContextMode.Offline)
                    {
                        cmp = new ServerComparer((this.Parent == null || this.Parent.Parent == null || this.Parent.Parent.SqlStoreConnection == null) ? null : this.Parent.Parent.SqlStoreConnection.ServerConnection, "msdb");
                    }

                    m_Levels = new TargetSetLevelCollection(this, cmp);
                }
                return m_Levels;
            }
        }

        private string rootLevel;

        /// <summary>
        /// 
        /// </summary>
        public string RootLevel
        {
            get
            {
                if (null == this.rootLevel)
                {
                    if (null == m_urn)
                    {
                        m_urn = new Urn(this.TargetTypeSkeleton);
                        if (!m_urn.IsValidUrnSkeleton())
                        {
                            throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.InvalidUrnSkeleton(TargetTypeSkeleton)));
                        }
                    }

                    this.rootLevel = m_urn.XPathExpression.BlockExpressionSkeleton(0);
                }
                return rootLevel;
            }
        }

        string GetTargetType(string skeleton)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetTargetType"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(skeleton);
                string targetType;

                Urn urn = new Urn(skeleton);

                // some of the types will not match between the Urn and 
                // the events generated in the server. We are going to
                // convert here so that we can process events correctly
                switch (urn.Type.ToLowerInvariant())
                {
                    case "userdefinedtype":
                        targetType = "TYPE";
                        break;
                    case "userdefineddatatype":
                        targetType = "TYPE";
                        break;
                    case "userdefinedfunction":
                        targetType = "FUNCTION";
                        break;
                    case "storedprocedure":
                        targetType = "PROCEDURE";
                        break;
                    case "applicationrole":
                        targetType = "APPLICATION ROLE";
                        break;
                    case "role":  //Engine returns "SERVER ROLE" as ObjectType for server role events but SMO Urn type is "Role" for both database and server role.
                        targetType = urn.Parent.Type.ToLowerInvariant().Equals("database") ? "ROLE" : "SERVER ROLE";
                        break;
                    default:
                        targetType = urn.Type.ToUpperInvariant();
                        break;
                }

                methodTraceContext.TraceParameterOut("returnVal", targetType);
                return targetType;
            }
        }

        /// <summary>
        /// Filter is an All Database filter
        /// </summary>
        internal bool IsAllDatabasesFilter
        {
            get
            {
                if (0 == String.Compare(TargetSet.DatabaseLevel, this.TargetTypeSkeleton, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (TargetSetLevel level in Levels)
                    {
                        if (!String.IsNullOrEmpty(level.Condition))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }
        }

        #region ISfcDiscoverObject Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException("sink");
            }
            switch (sink.Action)
            {
                case SfcDependencyAction.Serialize:
                case SfcDependencyAction.Create:
                    TargetSetLevelCollection levelColl = this.Levels;
                    if (levelColl != null)
                    {
                        sink.Add(SfcDependencyDirection.Inbound, levelColl.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
                    }
                    break;
                default:
                    break;
            }

            return;
        }

        #endregion


        /// <summary>
        /// Filter is on objects at or under Server/Database
        /// </summary>
        internal bool TargetsDatabaseObjects
        {
            get
            {
                return GetFilter().StartsWith(smoDatabaseUrnSkeleton, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Filter is on objects at Server/Database
        /// </summary>
        internal bool TargetsDatabases
        {
            get
            {
                return String.Equals(TargetSet.DatabaseLevel, this.TargetTypeSkeleton, StringComparison.OrdinalIgnoreCase);
            }
        }

        #region PROPERTIES
        internal const string typeName = "TargetSet";
        /// <summary>
        /// 
        /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
        public sealed class Key : SfcKey
        {
            /// <summary>
            /// Properties
            /// </summary>
            private string targetTypeSkeletonValue;

            /// <summary>
            /// Default constructor for generic key creation
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
                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }
                targetTypeSkeleton = other.targetTypeSkeleton;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="targetTypeSkeleton"></param>
            public Key(string targetTypeSkeleton)
            {
                this.targetTypeSkeleton = targetTypeSkeleton;
            }

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                targetTypeSkeleton = (string)filedDict["TargetTypeSkeleton"];
            }

            /// <summary>
            /// 
            /// </summary>
            public string targetTypeSkeleton
            {
                get
                {
                    return targetTypeSkeletonValue;
                }
                set
                {
                    targetTypeSkeletonValue = value;
                }
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
                return this.targetTypeSkeleton.GetHashCode();
            }

            private bool IsEqual(Key key)
            {
                return (0 == String.Compare(this.targetTypeSkeleton, key.targetTypeSkeleton, StringComparison.OrdinalIgnoreCase));
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return String.Format("{0}[@TargetTypeSkeleton='{1}']", TargetSet.typeName, Urn.EscapeString(this.targetTypeSkeleton));
            }

        } // public class Key

        // Singleton factory class
        class ObjectFactory : SfcObjectFactory
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
                return new TargetSet();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static SfcObjectFactory GetObjectFactory()
        {
            return ObjectFactory.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new ObjectSet Parent
        {
            get { return (ObjectSet)base.Parent; }
            set
            {
                traceContext.TraceVerbose("Setting Parent to: {0}", value);
                base.Parent = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcKey CreateIdentityKey()
        {
            Key key = null;
            // if we don't have our key values we can't create a key
            if (this.TargetTypeSkeleton != null)
            {
                key = new Key(this.TargetTypeSkeleton);
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
        [SfcProperty(SfcPropertyFlags.Data)]
        public Int32 ID
        {
            get
            {
                object value = this.Properties["ID"].Value;
                if (value == null)
                    return 0;
                return (Int32)this.Properties["ID"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        private Int32 ObjectSetID
        {
            get
            {
                return Parent.ID;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        [SfcKey(0)]
        public string TargetTypeSkeleton
        {
            get
            {
                return (string)this.Properties["TargetTypeSkeleton"].Value;
            }
        }

        // TODO: Confirm the SfcProperty Flags, do you need the Required?
        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public bool Enabled
        {
            get
            {
                return (bool)this.Properties["Enabled"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Enabled to: {0}", value);
                this.Properties["Enabled"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public string TargetType
        {
            get
            {
                // fix TargetType. It can be that the object has been deserialized,
                // or that we have been built with the default constructor
                // and in both these cases the property is missing
                if (null == this.Properties["TargetType"].Value &&
                    null != this.Properties["TargetTypeSkeleton"].Value)
                {
                    this.Properties["TargetType"].Value = GetTargetType(new Urn(this.Properties["TargetTypeSkeleton"].Value as string).XPathExpression.ExpressionSkeleton);
                }

                return (string)this.Properties["TargetType"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                case TargetSetLevel.typeName:
                    return this.Levels;
                default: throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
            }
        }

        Type m_type;

        /// <summary>
        /// Type corresponding with TargetTypeSkeleton
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Computed)]
        public Type Type
        {
            get
            {
                if (null == this.m_type)
                {
                    this.m_type = Utils.GetTypeFromUrnSkeleton(this.TargetTypeSkeleton);
                }
                return this.m_type;
            }
        }

        #endregion PROPERTIES


        #region Levels API

        /// <summary>
        /// Constructs URN from Levels' conditions
        /// </summary>
        /// <returns></returns>
        public string GetFilter()
        {
            StringBuilder sb = new StringBuilder(this.RootLevel);

            // need to make sure levels are in the right order
            // call GetLevelsSorted (which sorts levels)
            foreach (TargetSetLevel level in GetLevelsSorted())
            {
                sb.Append("/");
                sb.Append(level.LevelName);
                if (!String.IsNullOrEmpty(level.Condition))
                {
                    sb.Append("[");
                    sb.Append(this.Parent.Parent.Conditions[level.Condition].ExpressionNode.ToStringForUrn());
                    sb.Append("]");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// This method reuses GetFilter logic, but allows to replace filters for a particular levels
        /// it's intended to be used to produce a filter, 
        /// which is an intersection of TS and Category filters (if applicable), and honors SystemObject rules
        /// the level filters have to be calculated by the caller
        /// </summary>
        /// <param name="adjustments"></param>
        /// <returns></returns>
        internal string GetFilterWithNodeReplacement(Dictionary<string, ExpressionNode> adjustments)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetFilterWithNodeReplacement"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(adjustments);
                if (null == adjustments || adjustments.Count == 0)
                {
                    return GetFilter();
                }

                StringBuilder sb = new StringBuilder(this.RootLevel);

                // need to make sure levels are in the right order
                // call GetLevelsSorted (which sorts levels)
                foreach (TargetSetLevel level in GetLevelsSorted())
                {
                    sb.Append("/");
                    sb.Append(level.LevelName);
                    if (adjustments.ContainsKey(level.TargetTypeSkeleton))
                    {
                        sb.Append("[");
                        sb.Append(((ExpressionNode)adjustments[level.TargetTypeSkeleton]).ToStringForUrn());
                        sb.Append("]");
                    }
                    else if (!String.IsNullOrEmpty(level.Condition))
                    {
                        sb.Append("[");
                        sb.Append(this.Parent.Parent.Conditions[level.Condition].ExpressionNode.ToStringForUrn());
                        sb.Append("]");
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns sorted collection of TargetSetLevel objects
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public IList<TargetSetLevel> GetLevelsSorted()
        {
            SortedList<TargetSetLevel, object> levels = new SortedList<TargetSetLevel, object>();

            foreach (TargetSetLevel level in Levels)
            {
                levels.Add(level, null);
            }

            return levels.Keys;
        }

        /// <summary>
        /// Returns reference object for particular skeleton if it exists, null otherwise
        /// </summary>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public TargetSetLevel GetLevel(string skeleton)
        {
            if (this.Levels.Contains(skeleton))
            {
                return this.Levels[skeleton];
            }
            return null;
        }

        /// <summary>
        /// Sets level Condition
        /// </summary>
        /// <param name="level"></param>
        /// <param name="condition"></param>
        public TargetSetLevel SetLevelCondition(TargetSetLevel level, string condition)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("SetLevelCondition", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(level, condition);
                Levels[level.TargetTypeSkeleton].Condition = condition;
                methodTraceContext.TraceParameterOut("returnVal", Levels[level.TargetTypeSkeleton]);
                return Levels[level.TargetTypeSkeleton];
            }
        }

        #endregion Levels API

        /// <summary>
        /// Defines if this filter can be used for Enforce and CoC modes
        /// according to current rules (only allows Name condition on DB level)
        /// </summary>
        /// <returns></returns>
        internal bool IsEventingFilter()
        {
            foreach (TargetSetLevel tsl in Levels)
            {
                if (!String.IsNullOrEmpty(tsl.Condition))
                {
                    if (tsl.TargetTypeSkeleton == DatabaseLevel)
                    {
                        if (NameConditionType.None == this.Parent.Parent.Conditions[tsl.Condition].NameConditionType)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        internal TargetSet Clone(ObjectSet parentObjectSet)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Clone"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(parentObjectSet);
                traceContext.DebugAssert(null != parentObjectSet);
                TargetSet clone = new TargetSet(parentObjectSet, this.TargetTypeSkeleton);
                foreach (TargetSetLevel tsl in Levels)
                {
                    if (!String.IsNullOrEmpty(tsl.Condition))
                    {
                        clone.SetLevelCondition(clone.Levels[tsl.TargetTypeSkeleton], tsl.Condition);
                    }
                }

                Utils.ReplaceSfcProperties(clone, this);

                methodTraceContext.TraceParameterOut("returnVal", clone);
                return clone;
            }
        }
    }
}
