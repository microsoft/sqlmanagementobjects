// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Dmf
{

    /// <summary>
    /// 
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class EvaluationHistory : SfcInstance
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluationHistory");

        internal const string typeName = "EvaluationHistory";

        /// <summary>
        /// 
        /// </summary>
        public EvaluationHistory()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        internal EvaluationHistory(Policy parent)
        {
            this.Parent = parent;
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public String PolicyName
        {
            get
            {
                string parentName = string.Empty;
                if (this.Parent != null)
                    parentName = this.Parent.Name;
                return parentName;
            }
            internal set
            {
                traceContext.TraceVerbose("Setting PolicyName to: {0}", value);
                this.Properties["PolicyName"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public DateTime StartDate
        {
            get
            {
                object value = this.Properties["StartDate"].Value;
                if (value == null)
                    return DateTime.MinValue;
                return (DateTime)value;
            }
            internal set
            {
                traceContext.TraceVerbose("Setting StartDate to: {0}", value);
                this.Properties["StartDate"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public DateTime EndDate
        {
            get
            {
                object value = this.Properties["EndDate"].Value;
                if (value == null)
                    return DateTime.MinValue;
                return (DateTime)value;
            }
            internal set
            {
                traceContext.TraceVerbose("Setting EndDate to: {0}", value);
                this.Properties["EndDate"].Value = value;
            }
        }



        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public String Exception
        {
            get
            {
                object exception = this.Properties["Exception"].Value;
                if (exception == null)
                    return String.Empty;
                return (String)exception;
            }
            internal set
            {
                traceContext.TraceVerbose("Setting Exception to: {0}", value);
                this.Properties["Exception"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new Policy Parent
        {
            get { return (Policy)base.Parent; }
            internal set
            {
                traceContext.TraceVerbose("Setting Parent to: {0}", value);
                base.Parent = value;
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
                case ConnectionEvaluationHistory.typeName:
                    return this.ConnectionEvaluationHistories;
                default: throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
            }
        }

        ConnectionEvaluationHistoryCollection evaluationHistories;

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ConnectionEvaluationHistory))]
        public ConnectionEvaluationHistoryCollection ConnectionEvaluationHistories
        {
            get
            {
                if (this.evaluationHistories == null)
                {
                    this.evaluationHistories = new ConnectionEvaluationHistoryCollection(this);
                }
                return this.evaluationHistories;
            }
        }

        #region Override the ISfcDiscoverObject Function

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            switch (sink.Action)
            {
                case SfcDependencyAction.Serialize:
                    ConnectionEvaluationHistoryCollection histories = this.ConnectionEvaluationHistories;
                    if (histories != null)
                    {
                        sink.Add(SfcDependencyDirection.Inbound, histories.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
                    }
                    break;
                default:
                    break;
            }

            return;
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        [SfcKey(0)]
        public Int64 ID
        {
            get
            {
                object value = this.Properties["ID"].Value;
                if (value == null)
                    return 0;
                return (Int64)value;
            }
            internal set
            {
                traceContext.TraceVerbose("Setting ID to: {0}", value);
                Properties["ID"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public Boolean Result
        {
            get
            {
                object result = this.Properties["Result"].Value;
                if (result == null)
                    return false;
                return (Boolean)result;
            }
            internal set
            {
                traceContext.TraceVerbose("Setting Result to: {0}", value);
                Properties["Result"].Value = value;
            }
        }

        #region Sfc "Temporary"

        // This is copied from other classes where it also says "Sfc temporary".  
        // Why is this key stuff still around?  Why do we do this copy paste?  

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
            private Int64 id;

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
            public Key(Int64 id)
            {
                ID = id;
            }

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                ID = Convert.ToInt64(filedDict["ID"]);
            }

            /// <summary>
            /// 
            /// </summary>
            public Int64 ID
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
                return String.Format("{0}[@ID='{1}']", typeName, this.ID);
            }

        }


        // public class Key
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
                return new EvaluationHistory();
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
                        throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.UnsupportedCrudDependencyAction(depAction.ToString())));
                }
            }
        }

        static internal SfcTypeMetadata GetTypeMetadata()
        {
            return TypeMetadata.Instance;
        }

        #endregion

        #endregion

    }
}
