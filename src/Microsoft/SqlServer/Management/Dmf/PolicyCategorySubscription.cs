// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo;
using SFC = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the PolicyCategorySubscription class.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class PolicyCategorySubscription : SfcInstance, ISfcCreatable, ISfcDroppable, ISfcAlterable
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyCategorySubscription");

        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();

        static PolicyCategorySubscription()
        {
            // Create script
            scriptCreateAction.Procedure = "msdb.dbo.sp_syspolicy_add_policy_category_subscription";
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_type", "TargetType", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_object", "Target", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category", "PolicyCategory", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category_subscription_id", "ID", false, true));

            // Update script
            scriptAlterAction.Procedure = "msdb.dbo.sp_syspolicy_update_policy_category_subscription";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category_subscription_id", "ID", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_type", "TargetType", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_object", "Target", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category", "PolicyCategory", false, false));

            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_syspolicy_delete_policy_category_subscription";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category_subscription_id", "ID", true, false));
        }

        /// <summary>
        /// Default constructor used for deserialization. VSTS 55852.
        /// </summary>
        public PolicyCategorySubscription()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public PolicyCategorySubscription(PolicyStore parent)
        {
            traceContext.TraceMethodEnter("PolicyCategorySubscription");

            this.Parent = parent;
            traceContext.TraceMethodExit("PolicyCategorySubscription");
        }

        /// <summary>
        /// Constructor, accepting SmoObject (has to be a Database)
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="obj"></param>
        public PolicyCategorySubscription(PolicyStore parent, SqlSmoObject obj)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("PolicyCategorySubscription", System.Diagnostics.TraceEventType.Information))
            {
                if (obj == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("obj"));
                }

                if (obj is Database)
                {
                    this.Parent = parent;
                    this.Target = ((Database)obj).Name;
                    this.TargetType = typeof(Database).Name.ToUpperInvariant();
                }
                else
                {
                    throw methodTraceContext.TraceThrow(new UnsupportedObjectTypeException(obj.GetType().Name, this.GetType().Name));
                }
            }
        }

        /// <summary>
        /// Constructor, accepting target and the category name
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="target"></param>
        /// <param name="policyCategory"></param>
        internal PolicyCategorySubscription(PolicyStore parent, SfcQueryExpression target, string policyCategory)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("PolicyCategorySubscription"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(parent, target, policyCategory);
                SFC.Urn targetUrn = new SFC.Urn(target.ToString());
                SFC.XPathExpression xp = targetUrn.XPathExpression;

                if (xp.Length <= 1 || xp[1].Name != "Database")
                {
                    throw methodTraceContext.TraceThrow(new UnsupportedObjectTypeException(xp[1].Name, this.GetType().Name));
                }

                // make sure only the @Name='value' filter is being used
                SFC.FilterNodeOperator op = xp[1].Filter as SFC.FilterNodeOperator;
                if (null == op ||
                    !(op.Left is SFC.FilterNodeAttribute) ||
                    !(op.Right is SFC.FilterNodeConstant) ||
                    !(((SFC.FilterNodeAttribute)op.Left).Name == "Name") ||
                    (String.IsNullOrEmpty(((SFC.FilterNodeConstant)op.Right).ValueAsString)))
                {
                    throw new BadExpressionTreeException(
                        ExceptionTemplatesSR.UnsupportedTargetFilter(target.ToString()));
                }

                this.Parent = parent;
                this.Target = ((SFC.FilterNodeConstant)op.Right).ValueAsString;
                this.PolicyCategory = policyCategory;
                this.TargetType = typeof(Database).Name.ToUpperInvariant();
            }
        }

        #region SFC temporary
        internal const string typeName = "PolicyCategorySubscription";
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
            private int id;

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
                ID = other.ID;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="id"></param>
            public Key(int id)
            {
                ID = id;
            }

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                ID = Convert.ToInt32(filedDict["ID"]);
            }

            /// <summary>
            /// 
            /// </summary>
            public int ID
            {
                get
                {
                    return this.id;
                }
                set
                {
                    this.id = value;
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
                return this.ID.GetHashCode();
            }

            private bool IsEqual(Key key)
            {
                return this.ID == key.ID;
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return String.Format("{0}[@ID='{1}']", PolicyCategorySubscription.typeName, this.ID);
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
                return new PolicyCategorySubscription();
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
            try
            {
                Key key = new Key(this.ID);
                return key;
            }
            catch (NullReferenceException)
            {
                traceContext.TraceError("Caught a general Exception of type NullReferenceException");
                // if Properties["ID"] is null, accessing this.ID will throw

                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetChildCollection"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(elementType);
                switch (elementType)
                {
                    default: throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
                }
            }
        }
        #endregion

        #region properties
        /// <summary>
        /// 
        /// </summary>
        private void SetID(Int32 id)
        {
            traceContext.TraceMethodEnter("SetID");
            // Tracing Input Parameters
            traceContext.TraceParameters(id);
            this.Properties["ID"].Value = id;
            traceContext.TraceMethodExit("SetID");
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(Data = true, Required = false)]
        [SfcKey(0)]
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
        [SfcProperty(SfcPropertyFlags.Required)]
        public string TargetType
        {
            get
            {
                return (string)this.Properties["TargetType"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting TargetType to: {0}", value);
                this.Properties["TargetType"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string Target
        {
            get
            {
                return (string)this.Properties["Target"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Target to: {0}", value);
                this.Properties["Target"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        [SfcReference(typeof(Condition), "PolicyStore/PolicyCategory[@Name='{0}']", new string[] { "PolicyCategory" })]
        public string PolicyCategory
        {
            get
            {
                return (string)this.Properties["PolicyCategory"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting PolicyCategory to: {0}", value);
                this.Properties["PolicyCategory"].Value = value;
            }
        }
        #endregion

        #region CRUD
        /// <summary>
        /// Create the object
        /// </summary>
        public void Create()
        {
            traceContext.TraceMethodEnter("Create");
            Validate(ValidationMethod.Create);
            //Since ID is a key it has to be set to something in order to create an object
            this.Properties["ID"].Value = -1;
            base.CreateImpl();
            traceContext.TraceMethodExit("Create");
        }

        ISfcScript ISfcCreatable.ScriptCreate()
        {
            string script = scriptCreateAction.GenerateScript(this);
            return new SfcTSqlScript(script);
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
                    // We just created an object and changed its key to what we got from back-end
                    // Need to synchronize the new key to the one sitting in the collection (the one we set in Create -1)
                    ResetKey();
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

        ISfcScript ISfcAlterable.ScriptAlter()
        {
            string script = scriptAlterAction.GenerateScript(this);
            return new SfcTSqlScript(script);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Drop()
        {
            base.DropImpl();
        }

        ISfcScript ISfcDroppable.ScriptDrop()
        {
            string script = scriptDropAction.GenerateScript(this);
            return new SfcTSqlScript(script);
        }

        #endregion
    }
}
