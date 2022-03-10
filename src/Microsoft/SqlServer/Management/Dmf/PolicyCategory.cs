// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the PolicyCategory class.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class PolicyCategory : SfcInstance, ISfcCreatable, ISfcDroppable, ISfcAlterable, ISfcRenamable, IRenamable
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyCategory");
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptRenameAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();

        static PolicyCategory()
        {
            // Create script
            scriptCreateAction.Procedure = "msdb.dbo.sp_syspolicy_add_policy_category";
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category_id", "ID", false, true));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("mandate_database_subscriptions", "MandateDatabaseSubscriptions", false, false));

            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_syspolicy_delete_policy_category";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category_id", "ID", true, false));

            // Rename script
            scriptRenameAction.Procedure = "msdb.dbo.sp_syspolicy_rename_policy_category";
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("new_name", true));

            // Alter script
            scriptAlterAction.Procedure = "msdb.dbo.sp_syspolicy_update_policy_category";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("mandate_database_subscriptions", "MandateDatabaseSubscriptions", false, false));
        }

        /// <summary>
        /// Default constructor used for deserialization. VSTS 55852.
        /// </summary>
        public PolicyCategory()
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        public PolicyCategory(PolicyStore parent, string name)
        {
            traceContext.TraceMethodEnter("PolicyCategory");

            SetName(name);
            this.Parent = parent;
            this.MandateDatabaseSubscriptions = true;
            traceContext.TraceMethodExit("PolicyCategory");
        }

        #region properties
        /// <summary>
        /// 
        /// </summary>
        public static string DefaultCategory
        {
            get
            {
                return ExceptionTemplatesSR.DefaultCategory;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(ReadOnlyAfterCreation = true)]
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
        [SfcProperty(Data = true, Required = false)]
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
        [SfcProperty(Required = false, Data = true)]
        public bool MandateDatabaseSubscriptions
        {
            get
            {
                SfcProperty p = this.Properties["MandateDatabaseSubscriptions"];

                if (p.Value == null && this.State == SfcObjectState.Pending)
                {
                    p.Value = false;
                }

                return (Boolean)p.Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting MandateDatabaseSubscriptions to: {0}", value);
                this.Properties["MandateDatabaseSubscriptions"].Value = value;
            }
        }
        #endregion

        #region SFC temporary

        internal const string typeName = "PolicyCategory";

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
                keyName = other.Name;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            public Key(string name)
            {
                this.keyName = name;
            }

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
                    return keyName;
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
                return (0 == String.Compare(this.Name, key.Name, StringComparison.Ordinal));
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return String.Format("{0}[@Name='{1}']", PolicyCategory.typeName, SfcSecureString.EscapeSquote(Name));
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
                return new PolicyCategory();
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
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
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

        /// <summary>
        /// Creates the object on the server
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
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
                }
            }
        }

        /// <summary>
        /// Drops the object from the server
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

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        public void Rename(string name)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Rename", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(name);
                if (String.IsNullOrEmpty(name))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("Name")));
                }
                base.RenameImpl(new PolicyCategory.Key(name));
            }
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

                PolicyCategory.Key tkey = (key as PolicyCategory.Key);
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(tkey.Name.GetType(), tkey.Name));

                string script = scriptRenameAction.GenerateScript(this, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Alter()
        {
            traceContext.TraceMethodEnter("Alter");
            Validate();
            base.AlterImpl();
            traceContext.TraceMethodExit("Alter");
        }

        /// <summary>
        /// 
        /// </summary>
        ISfcScript ISfcAlterable.ScriptAlter()
        {
            string script = scriptAlterAction.GenerateScript(this);
            return new SfcTSqlScript(script);
        }
    }
}
