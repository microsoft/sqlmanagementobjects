// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the Condition class.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class Condition : SfcInstance, ISfcCreatable, ISfcDroppable, ISfcAlterable, ISfcRenamable, IRenamable
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "Condition");
     
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptRenameAction = new SfcTsqlProcFormatter();

        static Condition()
        {
            // Create script
            scriptCreateAction.Procedure = "msdb.dbo.sp_syspolicy_add_condition";
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("facet", "Facet", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("expression", "Expression", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("is_name_condition", true));   // passed in as runtime arg
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("obj_name", true));            // passed in as runtime arg
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("condition_id", "ID", false, true));

            // Update script
            scriptAlterAction.Procedure = "msdb.dbo.sp_syspolicy_update_condition";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("condition_id", "ID", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("facet", "Facet", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("expression", "Expression", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("is_name_condition", true));    // passed in as runtime arg
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("obj_name", true));             // passed in as runtime arg

            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_syspolicy_delete_condition";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("condition_id", "ID", true, false));

            // Rename script
            scriptRenameAction.Procedure = "msdb.dbo.sp_syspolicy_rename_condition";
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("new_name", true));
        }

        /// <summary>
        /// Default constructor used for deserialization. VSTS 55852.
        /// </summary>
        public Condition()
        {
            this.Description = String.Empty;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        public Condition(PolicyStore parent, string name)
        {
            traceContext.TraceMethodEnter("Condition");

            SetName(name);
            this.Parent = parent;
            this.Description = String.Empty;
            traceContext.TraceMethodExit("Condition");
        }

        /// <summary>
        /// Aggregated evaluation mode supported by condition's expressions facets
        /// </summary>
        /// <returns></returns>
        public AutomatedPolicyEvaluationMode GetSupportedEvaluationMode()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetSupportedEvaluationMode", System.Diagnostics.TraceEventType.Information))
            {
                AutomatedPolicyEvaluationMode mode = AutomatedPolicyEvaluationMode.None;

                // We will support whatever evaluation modes our facet supports, unless this Condition
                // runs scripts as part of its evaluation. In that case it can only run On Demand, or
                // CheckOnSchedule
                if (!String.IsNullOrEmpty(this.Facet))
                {
                    Type facet = FacetRepository.GetFacetType(this.Facet);
                    mode |= FacetRepository.GetFacetEvaluationMode(facet);

                    if (this.HasScript)
                    {
                        mode &= AutomatedPolicyEvaluationMode.CheckOnSchedule;
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", mode);
                return mode;
            }
        }

        /// <summary>
        /// Aggregated evaluation mode supported by condition's expressions facets
        /// </summary>
        /// <param name="domain">The target domain for the evaluation</param>
        /// <returns></returns>
        public AutomatedPolicyEvaluationMode GetSupportedEvaluationModeOnDomain(string domain)
        {
            if (String.Equals(domain, "Utility", StringComparison.Ordinal))
            {
                return AutomatedPolicyEvaluationMode.CheckOnSchedule;
            }
            else
            {
                return GetSupportedEvaluationMode();
            }
        }

        ExpressionNode expressionNode = null;

        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public ExpressionNode ExpressionNode
        {
            get
            {
                if (null == expressionNode)
                {
                    string exp = (string)this.Expression;
                    if (false == String.IsNullOrEmpty(exp))
                    {
                        try
                        {
                            expressionNode = ExpressionNode.Deserialize(exp);
                        }
                        catch (System.Xml.XmlException ex)
                        {
                            throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.InvalidOrMissingXMLReader, ex));
                        }
                    }
                }
                return expressionNode;
            }
            set
            {
                traceContext.TraceVerbose("Setting ExpressionNode to: {0}", value);
                expressionNode = value;
                if (expressionNode == null)
                {
                    this.Properties["Expression"].Value = null;
                }
                else
                {
                    this.Properties["Expression"].Value = ExpressionNode.SerializeNode(expressionNode);
                }
            }
        }

        /// <summary>
        /// Returns true if the condition can be used as a target set level filter condition, otherwise false.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Computed)]
        public bool IsEnumerable
        {
            get
            {
                return (null == ExpressionNode) ? false : ExpressionNode.FilterNodeCompatible;
            }
        }

        /// <summary>
        /// Since this is the actual property which needs storage and the above one is a helper property, this one needs to exist
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        internal string Expression
        {
            get
            {
                return this.Properties["Expression"].Value as string;
            }
            set
            {
                traceContext.TraceVerbose("Setting Expression to: {0}", value);
                this.Properties["Expression"].Value = value;
            }
        }

        /// <summary>
        /// Returns true if the condition contains an expression that runs a dynamic script, which
        /// is potentially dangerous.
        /// </summary>
        public bool HasScript
        {
            get
            {
                if (null == this.ExpressionNode)
                {
                    return false;
                }
                return this.ExpressionNode.HasScript;
            }
        }

        private bool m_bStatesInitialized = false;

        /// <summary>
        /// 
        /// </summary>
        protected override void InitializeUIPropertyState()
        {
            //Prevent the cyclic calling between this function and the SetEnabled function in case this one is called directly
            if (m_bStatesInitialized)
                return;
            m_bStatesInitialized = true;
            UpdateUIPropertyState();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void UpdateUIPropertyState()
        {
            /*
             * If the user is not interested in the states changes, then the InitializeUIPropertyState was not called before, and so
             * we are not interested in updating the states, so we won't update anything until the user is actually interested in doing this
             * which is done either by trying to get the enabled value, or setting it, or registering to the metadata changes event.
             * 
             */
            if (!m_bStatesInitialized)
                return;

            SfcProperty name = this.Properties["Name"];

            //States rules
            if (this.State == SfcObjectState.Pending)
                name.Enabled = true;
            else
                name.Enabled = false;
        }

        #region CRUD support

        /// <summary>
        /// 
        /// </summary>
        public void Create()
        {
            traceContext.TraceMethodEnter("Create");
            Validate(ValidationMethod.Create);
            base.CreateImpl();
            traceContext.TraceMethodExit("Create");
        }

        /// <summary>
        /// Scripts creation of the object
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptCreate()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptCreate", System.Diagnostics.TraceEventType.Information))
            {
                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>(2);
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(Int16), (Int16)NameConditionType));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(string), ObjectName));

                string script = scriptCreateAction.GenerateScript(this, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        /// <summary>
        /// Perform post-create action
        /// </summary>
        protected override void PostCreate(object executionResult)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("PostCreate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(executionResult);
                // Guard against disconnected (Offline) mode, which always returns a null executionResult since there is no server communication.
                if (this.GetDomain().ConnectionContext.Mode != SfcConnectionContextMode.Offline)
                {
                    this.Properties["ID"].Value = executionResult;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Alter()
        {
            traceContext.TraceMethodEnter("Alter");
            Validate(ValidationMethod.Alter);
            base.AlterImpl();
            traceContext.TraceMethodExit("Alter");
        }

        /// <summary>
        /// Skpis validation to allow overriding Condition with already validated one (Import scenario)
        /// </summary>
        internal void AlterNoValidation()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Scripts all changes on the object
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptAlter()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptAlter", System.Diagnostics.TraceEventType.Information))
            {
                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>(2);
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(Int16), (Int16)NameConditionType));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(string), ObjectName));

                string script = scriptAlterAction.GenerateScript(this, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Scripts deletion of the object
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptDrop()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptDrop", System.Diagnostics.TraceEventType.Information))
            {
                string script = scriptDropAction.GenerateScript(this);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        public void Rename(string name)
        {
            traceContext.TraceMethodEnter("Rename");
            // Tracing Input Parameters
            traceContext.TraceParameters(name);
            Validate(ValidationMethod.Rename);
            base.RenameImpl(new Condition.Key(name));
            traceContext.TraceMethodExit("Rename");
        }

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        /// <param name="key"></param>
        void ISfcRenamable.Rename(SfcKey key)
        {
            base.RenameImpl(key);
        }

        ISfcScript ISfcRenamable.ScriptRename(SfcKey key)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ISfcRenamable.ScriptRename"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(key);
                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();

                Condition.Key tkey = (key as Condition.Key);
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(tkey.Name.GetType(), tkey.Name));

                string script = scriptRenameAction.GenerateScript(this, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        #endregion

        #region ISfcDiscoverObject Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            // Inbound Policy references
            // TODO: limit this via mode and action flags for ancestor dependency views only, or something
            //policycollection policycoll = this.parent.policies;
            //policycoll.refresh();
            //foreach (policy policy in policycoll)
            //{
            //    string policycondname = policy.condition;
            //    if (policycondname != null && string.compareordinal(policycondname, this.name))
            //    {
            //        sink.add(dependencydirection.Inbound, policy, sfctyperelation.weakreference, false);
            //    }

            return;
        }

        #endregion

        /// <summary>
        /// Returns a collection of Policy objects that are dependent
        /// on this Condition.
        /// </summary>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public ReadOnlyCollection<Policy> EnumDependentPolicies()
        {
            List<Policy> list = new List<Policy>();

            foreach (Policy p in this.Parent.Policies)
            {
                if (p.Condition == this.Name)
                {
                    list.Add(p);
                }
            }
            return new ReadOnlyCollection<Policy>(list);
        }

        #region Generated Part To Be Removed
        internal const string typeName = "Condition";
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
                if (other == null)
                {
                    throw traceContext.TraceThrow(new ArgumentNullException("other"));
                }
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

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                keyName = (string)filedDict["Name"];
            }

            /// <summary>
            /// 
            /// </summary>
            public string Name
            {
                get
                {
                    return this.keyName;
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
                return String.Format("{0}[@Name='{1}']", Condition.typeName, SfcSecureString.EscapeSquote(Name));
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
                return new Condition();
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
        public new PolicyStore Parent
        {
            get { return (PolicyStore)base.Parent; }
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
            traceContext.TraceMethodEnter("SetName");
            // Tracing Input Parameters
            traceContext.TraceParameters(name);
            this.Properties["Name"].Value = name;
            traceContext.TraceMethodExit("SetName");
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public int ID
        {
            get
            {
                object value = this.Properties["ID"].Value;
                if (value == null)
                    return 0;
                return (int)value;
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
                traceContext.TraceVerbose("Setting Description to: {0}", value);
                this.Properties["Description"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public DateTime CreateDate
        {
            get
            {
                object value = this.Properties["CreateDate"].Value;
                if (value == null)
                    return DateTime.MinValue;
                return (DateTime)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public string CreatedBy
        {
            get
            {
                return (string)this.Properties["CreatedBy"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public DateTime DateModified
        {
            get
            {
                object value = this.Properties["DateModified"].Value;
                if (value == null)
                    return DateTime.MinValue;
                return (DateTime)value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public string ModifiedBy
        {
            get
            {
                return (string)this.Properties["ModifiedBy"].Value;
            }
        }


        string cachedFacet = null;

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string Facet
        {
            get
            {
                // remember original Condition Facet
                if (null == cachedFacet)
                {
                    cachedFacet = (string)this.Properties["Facet"].Value;

                }
                return (string)this.Properties["Facet"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Facet to: {0}", value);
                // in case of Set with no preceeding Get
                if (null == cachedFacet)
                {
                    cachedFacet = (string)this.Properties["Facet"].Value;

                    if (String.IsNullOrEmpty(cachedFacet))
                    {
                        cachedFacet = (String)value;
                    }
                }
                this.Properties["Facet"].Value = value;
            }
        }



        /// <summary>
        /// 
        /// </summary>
        internal NameConditionType NameConditionType
        {
            get
            {
                return (null != this.ExpressionNode) ? this.ExpressionNode.NameConditionType : NameConditionType.None;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal string ObjectName
        {
            get
            {
                return (null != this.ExpressionNode) ? this.ExpressionNode.ObjectName : null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public bool IsSystemObject
        {
            get
            {
                object value = this.Properties["IsSystemObject"].Value;
                if (value == null)
                    return false;
                return (bool)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                default: throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
            }
        }
        #endregion

        internal bool IsConfigurable
        {
            get
            {
                if (String.IsNullOrEmpty(Facet) || null == ExpressionNode)
                {
                    return false;
                }

                List<ConfigurationItem> configList = new List<ConfigurationItem>();
                try
                {
                    ExpressionNode.AnalyzeForConfiguration(configList);
                    Type facetType = FacetRepository.GetFacetType(this.Facet);
                    foreach (ConfigurationItem ci in configList)
                    {
                        if (!FacetRepository.IsPropertyConfigurable(facetType, ci.Property))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (ExpressionNodeNotConfigurableException)
                {
                    return false;
                }
                catch (ExpressionNodeNotConfigurableOperatorException)
                {
                    return false;
                }
            }
        }

        internal bool CanBeConfigured(ref string message)
        {
            traceContext.TraceMethodEnter("CanBeConfigured");
            // Tracing Input Parameters
            traceContext.TraceParameters(message);
            if (String.IsNullOrEmpty(Facet) || null == ExpressionNode)
            {
                traceContext.TraceParameterOut("returnVal", false);
                return false;
            }

            List<ConfigurationItem> configList = new List<ConfigurationItem>();
            try
            {
                ExpressionNode.AnalyzeForConfiguration(configList);
                Type facetType = FacetRepository.GetFacetType(this.Facet);
                foreach (ConfigurationItem ci in configList)
                {
                    if (!FacetRepository.IsPropertyConfigurable(facetType, ci.Property))
                    {
                        message = ExceptionTemplatesSR.PropertyCannotBeSet(ci.Property);
                        traceContext.TraceParameterOut("returnVal", false);
                        return false;
                    }
                }

                traceContext.TraceParameterOut("returnVal", true);
                return true;
            }
            catch (ExpressionNodeNotConfigurableException nce)
            {
                traceContext.TraceCatch(nce);
                message = nce.Message;
                traceContext.TraceParameterOut("returnVal", false);
                return false;
            }
            catch (ExpressionNodeNotConfigurableOperatorException ncoe)
            {
                traceContext.TraceCatch(ncoe);
                message = ncoe.Message;
                traceContext.TraceParameterOut("returnVal", false);
                return false;
            }
        }

        internal void Configure(object target)
        {
            traceContext.TraceMethodEnter("Configure");
            // Tracing Input Parameters
            traceContext.TraceParameters(target);
            FacetEvaluationContext facetEvaluationContext = FacetEvaluationContext.GetFacetEvaluationContext(Facet, target);

            List<ConfigurationItem> configList = new List<ConfigurationItem>();
            ExpressionNode.AnalyzeForConfiguration(configList);

            foreach (ConfigurationItem ci in configList)
            {
                facetEvaluationContext.SetPropertyValue(ci.Property, ci.DesiredValue);
            }

            facetEvaluationContext.Alter();
            facetEvaluationContext.Refresh();
            traceContext.TraceMethodExit("Configure");
        }

        internal string ProduceConfigureScript(object target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ProduceConfigureScript"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target);
                string header = String.Empty;

                // At this point we can only script SMO objects

                if (false == target is SqlSmoObject)
                {
                    // There potentially could be multiple scripting languages
                    // We use most commom comment designators here
                    return "/*  " + ExceptionTemplatesSR.CannotProduceScript + "  */";
                }
                else
                {
                    // NOTE: ConvertUrnToPath escapes */ so injection is not possible here
                    header = "/*  Scripting: '" + SfcSqlPathUtilities.ConvertUrnToPath(((SqlSmoObject)target).Urn) + "'  */ ";
                }

                // Evaluate directly - don't want to raise events
                FacetEvaluationContext facetEvaluationContext = FacetEvaluationContext.GetFacetEvaluationContext(Facet, target);
                if ((bool)ExpressionNode.Evaluate(facetEvaluationContext))
                {
                    // object complies - nothing to configure
                    return header + "\n/*  " + ExceptionTemplatesSR.NoConfigureScriptForCompliantObject + "  */";
                }

                List<ConfigurationItem> configList = new List<ConfigurationItem>();
                // This will throw if configuring is not possible
                ExpressionNode.AnalyzeForConfiguration(configList);

                foreach (ConfigurationItem ci in configList)
                {
                    facetEvaluationContext.SetPropertyValue(ci.Property, ci.DesiredValue);
                }

                StringBuilder script = new StringBuilder();
                ServerConnection connectionContext = ((SqlSmoObject)target).GetServerObject().ConnectionContext;
                SqlExecutionModes connectionExecutionMode = connectionContext.SqlExecutionModes;
                connectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;

                try
                {
                    script.AppendLine(header);

                    facetEvaluationContext.Alter();

                    foreach (string s in connectionContext.CapturedSql.Text)
                    {
                        script.AppendLine(s);
                    }
                }
                finally
                {
                    // Restore original executionMode and restore the object state
                    connectionContext.SqlExecutionModes = connectionExecutionMode;
                    connectionContext.CapturedSql.Clear();
                    facetEvaluationContext.Refresh();
                }

                return script.ToString();
            }
        }

        internal bool Evaluate(object target, AdHocPolicyEvaluationMode evaluationMode)
        {
            FacetEvaluationContext facetEvaluationContext = null;
            Exception evaluationException = null;
            bool evaluationResult = false;
            bool isConfigurable = false;
            string message = String.Empty;

            try
            {
                facetEvaluationContext = FacetEvaluationContext.GetFacetEvaluationContext(Facet, target);
                evaluationResult = (bool)ExpressionNode.Evaluate(facetEvaluationContext,
                    evaluationMode == AdHocPolicyEvaluationMode.CheckSqlScriptAsProxy);
            }
            catch (Exception e)
            {
                traceContext.TraceCatch(e);
                evaluationException = e;
            }

            if (EvaluateCondition != null)
            {
                if (!evaluationResult && !Microsoft.SqlServer.Server.SqlContext.IsAvailable)
                {
                    isConfigurable = this.CanBeConfigured(ref message);
                }
                EvaluateCondition(this,
                    new ConditionEvaluationEventArgs(facetEvaluationContext, Facet, target, evaluationResult, isConfigurable, message, evaluationException));
            }

            if (evaluationException != null)
            {
                throw traceContext.TraceThrow(evaluationException);
            }

            return evaluationResult;
        }

        internal delegate void ConditionEvaluationEventHandler(Condition sender, ConditionEvaluationEventArgs args);
        internal event ConditionEvaluationEventHandler EvaluateCondition;


        internal static List<SfcInstanceSerializedData> UpgradeInstance(List<SfcInstanceSerializedData> sfcInstanceData, int fileVersion)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("UpgradeInstance"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(sfcInstanceData, fileVersion);
                List<SfcInstanceSerializedData> list = new List<SfcInstanceSerializedData>(sfcInstanceData);

                if (fileVersion < 3)
                {
                    foreach (SfcInstanceSerializedData sfcInstanceSerializedData in sfcInstanceData)
                    {
                        if (sfcInstanceSerializedData.Name == "IsNameCondition" || sfcInstanceSerializedData.Name == "ObjectName")
                        {
                            list.Remove(sfcInstanceSerializedData);
                        }
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", list);
                return list;
            }
        }
    }

    internal class ConditionEvaluationEventArgs : EventArgs
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ConditionEvaluationEventArgs");
        private string targetUrn = string.Empty;
        private string targetUrnOnlyId = string.Empty;
        private string targetPsPath = string.Empty;
        private string serverName;
        private bool evaluationResult;
        private bool isConfigurable;
        private string configMessage = string.Empty;
        private object target;
        private Exception exception;

        internal ConditionEvaluationEventArgs(FacetEvaluationContext facetEvaluationContext, string facetType, object target, bool evaluationResult, bool isConfigurable, string configMessage, Exception exception)
        {
            traceContext.TraceMethodEnter("ConditionEvaluationEventArgs");
            // Tracing Input Parameters
            traceContext.TraceParameters(facetEvaluationContext, facetType, target, evaluationResult, isConfigurable, configMessage, exception);
            serverName = string.Empty;
            if (target is IDmfObjectInfo)
            {
                targetPsPath = ((IDmfObjectInfo)target).ObjectPath;
                serverName = ((IDmfObjectInfo)target).RootPath;
            }
            else if ((facetEvaluationContext != null) && (facetEvaluationContext.Target is IDmfObjectInfo))
            {
                this.targetPsPath = ((IDmfObjectInfo)facetEvaluationContext.Target).ObjectPath;
                this.serverName = ((IDmfObjectInfo)facetEvaluationContext.Target).RootPath;
            }
            else
            {
                this.targetUrn = SfcUtility.GetUrn(target);
                if (target is SqlSmoObject)
                {
                    serverName = ((SqlSmoObject)target).GetServerObject().ConnectionContext.ServerInstance;
                    if (((SqlSmoObject)target).IsDesignMode == false)
                    {
                        this.targetUrnOnlyId = ((SqlSmoObject)target).UrnOnlyId.ToString();
                    }
                }
                else if (target is SfcInstance)
                {
                    if (((SfcInstance)target).GetDomain().GetConnection() != null)
                    {
                        serverName = ((SfcInstance)target).GetDomain().GetConnection().ServerInstance;
                    }
                    else
                    {
                        serverName = ((SfcInstance)target).GetDomain().DomainInstanceName;
                    }
                }

                string domain = SfcRegistration.GetRegisteredDomainForType(target.GetType().FullName, false);
                if (!string.IsNullOrEmpty(domain))
                {
                    this.targetPsPath = SfcSqlPathUtilities.ConvertUrnToPath(targetUrn);
                }
            }

            this.evaluationResult = evaluationResult;
            this.isConfigurable = isConfigurable;
            if (!String.IsNullOrEmpty(configMessage))
            {
                this.configMessage = configMessage;
            }
            this.target = target;
            this.exception = exception;
            traceContext.TraceMethodExit("ConditionEvaluationEventArgs");
        }

        internal string TargetUrn { get { return targetUrn; } }

        internal string TargetUrnOnlyId { get { return targetUrnOnlyId; } }

        internal string TargetPsPath { get { return targetPsPath; } }

        internal string ServerName { get { return serverName; } }

        internal bool EvaluationResult { get { return evaluationResult; } }

        internal bool IsConfigurable { get { return isConfigurable; } }

        internal string ConfigurationErrorMessage { get { return configMessage; } }

        internal object Target { get { return target; } }

        internal Exception EvaluationException { get { return exception; } }
    }

    /// <summary>
    /// 
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class TargetEvaluation
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "TargetEvaluation");
        private object target;
        private ExpressionNode result;
        internal TargetEvaluation(object target, ExpressionNode result)
        {
            traceContext.TraceMethodEnter("TargetEvaluation");
            // Tracing Input Parameters
            traceContext.TraceParameters(target, result);
            this.target = target;
            this.result = result;
            traceContext.TraceMethodExit("TargetEvaluation");
        }

        /// <summary>
        /// 
        /// </summary>
        public object Target
        {
            get
            {
                return target;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ExpressionNode Result
        {
            get
            {
                return result;
            }
        }
    }
}
