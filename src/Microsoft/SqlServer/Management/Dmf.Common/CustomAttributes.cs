// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Dmf;

namespace Microsoft.SqlServer.Management.Facets
{
    /// <summary>
    /// Custom attribute that describes what events are of interest for 
    /// adapters that implement facets
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class StateChangeEventAttribute : System.Attribute
    {
        private string eventName;
        private string targetType;
        private string targetTypeAlias;

        /// <summary>
        /// ctor short
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="targetType"></param>
        public StateChangeEventAttribute(string eventName, string targetType)
        {
            this.eventName = eventName;
            this.targetType = targetType;
            this.targetTypeAlias = String.Empty;
        }

        /// <summary>
        /// ctor with alias
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="targetType"></param>
        /// <param name="targetTypeAlias"></param>
        public StateChangeEventAttribute (string eventName, string targetType, string targetTypeAlias)
        {
            this.eventName = eventName;
            this.targetType = targetType;
            this.targetTypeAlias = targetTypeAlias;
        }

        /// <summary>
        /// prop
        /// </summary>
        public string EventName
        {
            get { return eventName; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string TargetType
        {
            get { return targetType; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string TargetTypeAlias
        {
            get { return targetTypeAlias; }
        }
    }

    /// <summary>
    /// Custom attribute that declares property source types for facet
    /// </summary>
    [AttributeUsage (AttributeTargets.All, AllowMultiple = true)]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PropertySourceSubObjectTypeAttribute : System.Attribute
    {
        private Type type;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="sourceType"></param>
        public PropertySourceSubObjectTypeAttribute (Type sourceType)
        {
            this.type = sourceType;
        }

        /// <summary>
        /// prop
        /// </summary>
        public Type SourceType 
        {
            get { return type; }
        }
    }

    /// <summary>
    /// Custom attribute that describes what evaluation modes are supported 
    /// by adapters
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class EvaluationModeAttribute : System.Attribute
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="evaluationModes"></param>
        public EvaluationModeAttribute(AutomatedPolicyEvaluationMode evaluationModes)
        {
            this.evaluationModes = evaluationModes;
        }

        /// <summary>
        /// 
        /// </summary>
        public AutomatedPolicyEvaluationMode EvaluationModes
        {
            get
            {
                return evaluationModes;
            }
        }

        private AutomatedPolicyEvaluationMode evaluationModes;
        
        /// <summary>
        /// prop
        /// </summary>
        public AutomatedPolicyEvaluationMode AutomatedPolicyEvaluationMode
        {
            get { return evaluationModes; }
            set { evaluationModes = value; }
        }
    }

}
