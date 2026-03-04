// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Diagnostics.STrace;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Information about a policy category.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyCategoryInformation
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyCategoryInformation");
        Policy policy;
        bool targetSubscribes;
        PolicyCategory policyCategory;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="policyCategory"></param>
        internal PolicyCategoryInformation(PolicyCategory policyCategory)
        {
            traceContext.TraceMethodEnter("PolicyCategoryInformation");
            // Tracing Input Parameters
            traceContext.TraceParameters(policyCategory);
            this.policy = null;
            this.targetSubscribes = false;
            this.policyCategory = policyCategory;
            traceContext.TraceMethodExit("PolicyCategoryInformation");
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="policyCategory"></param>
        /// <param name="policy"></param>
        /// <param name="targetSubscribes"></param>
        internal PolicyCategoryInformation(PolicyCategory policyCategory, Policy policy, bool targetSubscribes)
        {
            traceContext.TraceMethodEnter("PolicyCategoryInformation");
            // Tracing Input Parameters
            traceContext.TraceParameters(policyCategory, policy, targetSubscribes);
            this.policy = policy;
            this.targetSubscribes = targetSubscribes;
            this.policyCategory = policyCategory;
            traceContext.TraceMethodExit("PolicyCategoryInformation");
        }

        /// <summary>
        /// Policy Category ID
        /// </summary>
        public int ID
        {
            get
            {
                if (policyCategory != null)
                {
                    traceContext.DebugAssert(policyCategory.ID != 0);
                }

                return (policyCategory == null) ? -1 : policyCategory.ID;
            }
        }

        /// <summary>
        /// Policy name
        /// </summary>
        public string Name
        {
            get
            {
                if (policyCategory != null)
                {
                    traceContext.DebugAssert(policyCategory.ID != 0);
                }

                return (policyCategory == null) ? string.Empty : policyCategory.Name;
            }
        }

        /// <summary>
        /// Is subscribed
        /// </summary>
        public bool IsSubscribed
        {
            get
            {
                return this.targetSubscribes;
            }
        }

        /// <summary>
        /// The category is active on the server or not.
        /// </summary>
        public bool MandateDatabaseSubscriptions
        {
            get
            {
                return (policyCategory == null) ? true : policyCategory.MandateDatabaseSubscriptions;
            }
        }

        /// <summary>
        /// Policy ID
        /// </summary>
        public int PolicyId
        {
            get
            {
                if (policy == null)
                    return 0;
                return policy.ID;
            }
        }

        /// <summary>
        /// The category does contain any policy or not.
        /// </summary>
        public bool IsEmptyCategory
        {
            get
            {
                return this.policy == null;
            }
        }

        /// <summary>
        /// Policy name
        /// </summary>
        public string PolicyName
        {
            get
            {
                if (policy == null)
                    return null;
                return policy.Name;
            }
        }

        /// <summary>
        /// Policy Enabled
        /// </summary>
        public bool PolicyEnabled
        {
            get
            {
                if (policy == null)
                    return false;
                return policy.Enabled;
            }
        }

        /// <summary>
        /// Policy Evaluation Mode
        /// </summary>
        public AutomatedPolicyEvaluationMode EvaluationMode
        {
            get
            {
                if (policy == null)
                    return AutomatedPolicyEvaluationMode.None;
                return policy.AutomatedPolicyEvaluationMode;
            }
        }

        /// Used for sorting
        internal static int CompareByCategoryIDPolicyName(PolicyCategoryInformation left, PolicyCategoryInformation right)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CompareByCategoryIDPolicyName"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(left, right);
                if (left.ID == right.ID)
                {
                    if (left.policy != null && right.policy != null)
                        return left.PolicyName.CompareTo(right.PolicyName);
                    else
                        methodTraceContext.TraceParameterOut("returnVal", 0);
                    return 0;
                }
                return left.ID.CompareTo(right.ID);
            }
        }
    }
}
