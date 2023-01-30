// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the TargetSetLevel class.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class TargetSetLevel : SfcInstance, IComparable<TargetSetLevel>
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "TargetSetLevel");
        /// <summary>
        /// Default constructor used for deserialization.
        /// </summary>
        public TargetSetLevel()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="TargetTypeSkeleton"></param>
        internal TargetSetLevel(TargetSet parent, string TargetTypeSkeleton)
        {
            traceContext.TraceMethodEnter("TargetSetLevel");
            // Tracing Input Parameters
            traceContext.TraceParameters(TargetTypeSkeleton);
            this.Parent = parent;
            this.Properties["TargetTypeSkeleton"].Value = TargetTypeSkeleton;

            m_type = Utils.GetTypeFromUrnSkeleton(TargetTypeSkeleton);

            Urn urn = new Urn(TargetTypeSkeleton);
            this.Properties["LevelName"].Value = urn.Type;

            this.Condition = String.Empty;
            traceContext.TraceMethodExit("TargetSetLevel");
        }

        private Type m_type;

        /// <summary>
        /// 
        /// </summary>
        public Type TargetType
        {
            get
            {
                if (null == m_type)
                {
                    m_type = Utils.GetTypeFromUrnSkeleton(this.TargetTypeSkeleton);
                }
                return m_type;
            }
        }

        #region IComparable & Equals
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (null == obj || !(obj is TargetSetLevel))
            {
                return false;
            }

            return this.TargetTypeSkeleton.Equals(((TargetSetLevel)obj).TargetTypeSkeleton);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.TargetTypeSkeleton.GetHashCode();
        }

        /// <summary>
        /// Comparison based on the length of TargetTypeSkeleton
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public int CompareTo(TargetSetLevel level)
        {
            if (null == level)
            {
                throw new ArgumentNullException("level");
            }

            return this.TargetTypeSkeleton.Length - level.TargetTypeSkeleton.Length;
        }
        #endregion IComparable & Equals

        // TODO: Determine if we need all of this region. Used for delegating to the parent for CRUD.
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

        /// <summary>
        /// Returns filtering properties supported by the SMO
        /// type represented by the given expression, or null if it's
        /// not a valid SMO type.
        /// </summary>
        public static PropertyInfo[] GetTypeFilterProperties(string skeleton)
        {
            return SMO.SmoDmfAdapter.GetTypeFilterProperties(skeleton);
        }

        #region PROPERTIES
        internal const string typeName = "TargetSetLevel";
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
                return String.Format("{0}[@TargetTypeSkeleton='{1}']", TargetSetLevel.typeName, this.targetTypeSkeleton);
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
                return new TargetSetLevel();
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
        public new TargetSet Parent
        {
            get { return (TargetSet)base.Parent; }
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
        private Int32 TargetSetID
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

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string LevelName
        {
            get
            {
                return (string)this.Properties["LevelName"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        [SfcReference(typeof(Condition), "PolicyStore/Condition[@Name='{0}']", new string[] { "Condition" })]
        public string Condition
        {
            get
            {
                return (string)this.Properties["Condition"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Condition to: {0}", value);
                this.Properties["Condition"].Value = value;
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
                default: throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
            }
        }
        #endregion PROPERTIES

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
                throw traceContext.TraceThrow(new ArgumentNullException("sink"));
            }
            if (sink.Action == SfcDependencyAction.Serialize)
            {
                // Condition reference
                ConditionCollection condColl = this.Parent.Parent.Parent.Conditions;
                if (!String.IsNullOrEmpty(this.Condition) && condColl.Contains(this.Condition))
                {
                    sink.Add(SfcDependencyDirection.Outbound, condColl[this.Condition], SfcTypeRelation.StrongReference, false);
                }
            }

            return;
        }
        #endregion
    }
}
