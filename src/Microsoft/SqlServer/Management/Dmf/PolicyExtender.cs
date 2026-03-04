// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Decorator for the Policy object. Used add additional properties to the base Policy object
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyExtender : SfcObjectExtender<Policy>
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyExtender");
        List<PolicyCategory> policyCategories;
        PolicyCategory defaultCategory;
        string policyFilePath = String.Empty;

        /// <summary>
        /// default ctor
        /// </summary>
        public PolicyExtender()
            : base()
        {
        }

        /// <summary>
        /// ctor. Takes parent Policy object to aggregate on
        /// </summary>
        /// <param name="policy"></param>
        public PolicyExtender(Policy policy)
            : base(policy)
        {
        }

        /// <summary>
        /// ctor. Create a new Policy object and aggregates on it.
        /// </summary>
        /// <param name="policyStore"></param>
        /// <param name="name"></param>
        public PolicyExtender(PolicyStore policyStore, string name)
            : base(new Policy(policyStore, name))
        {
        }

        /// <summary>
        /// Translates parent's 'Condition' string property into Condition object property
        /// </summary>
        [ExtendedProperty("Condition")]
        public Condition ConditionInstance
        {
            get
            {
                if (this.Parent.Parent.Conditions.Count > 0)
                {
                    string condition = this.Parent.Condition;
                    if (string.IsNullOrEmpty(condition))
                    {
                        return null;
                    }
                    return this.Parent.Parent.Conditions[condition];
                }
                return null;
            }
            set
            {
                traceContext.TraceVerbose("Setting ConditionInstance to: {0}", value);
                string newValue = value == null ? null : value.Name;
                if (this.Parent.Condition != newValue)
                {
                    this.Parent.Condition = newValue;

                    // we have to reset exec mode here.
                    // Should it be on the object level?
                    this.Parent.AutomatedPolicyEvaluationMode = AutomatedPolicyEvaluationMode.None;
                }
            }
        }

        /// <summary>
        /// Returns supported evaluation modes for selected Condition
        /// </summary>
        [ExtendedProperty("Condition")]
        public AutomatedPolicyEvaluationMode SupportedPolicyEvaluationMode
        {
            get
            {
                Condition c = this.ConditionInstance;
                if (c != null)
                {
                    return c.GetSupportedEvaluationMode();
                }
                return AutomatedPolicyEvaluationMode.None;
            }
        }

        /// <summary>
        /// Translates parent's 'PolicyCategory' string property into PolicyCategory object property
        /// </summary>
        [ExtendedProperty("PolicyCategory")]
        public PolicyCategory Category
        {
            get
            {
                string val = this.Parent.PolicyCategory;
                if (string.IsNullOrEmpty(val))
                {
                    return this.DefaultCategory;
                }
                else
                {
                    PolicyCategory pg = this.Parent.Parent.PolicyCategories[val];
                    if (pg == null)
                    {
                        return this.DefaultCategory;
                    }
                    return pg;
                }
            }
            set
            {
                traceContext.TraceVerbose("Setting Category to: {0}", value);
                string newValue;
                if (value == null)
                {
                    newValue = string.Empty;
                }
                else if (value == this.DefaultCategory)
                {
                    newValue = string.Empty;
                }
                else
                {
                    newValue = value.Name;
                }

                if (this.Parent.PolicyCategory != newValue)
                {
                    this.Parent.PolicyCategory = newValue;
                }
            }
        }

        /// <summary>
        /// Provides a list of available categories
        /// </summary>
        [ExtendedProperty()]
        public List<PolicyCategory> Categories
        {
            get
            {
                if (this.policyCategories == null)
                {
                    if (this.Parent.Parent != null)
                    {
                        this.policyCategories = new List<PolicyCategory>(this.Parent.Parent.PolicyCategories);

                        this.policyCategories.Insert(0, this.DefaultCategory);
                    }
                }
                return this.policyCategories;
            }
        }

        PolicyCategory DefaultCategory
        {
            get
            {
                if (this.defaultCategory == null)
                {
                    this.defaultCategory = new PolicyCategory(this.Parent.Parent, PolicyCategory.DefaultCategory);
                }
                return this.defaultCategory;
            }
        }


        /// <summary>
        /// Current Filters
        /// </summary>
        [ExtendedProperty()]
        public TargetSetCollection Filters
        {
            get
            {
                // TODO: Need to return the target sets from the object set that is referenced by the policy
                // this propertty is a collection.
                // in current design collections can't be accessed using ISfcPropertyProvider
                if (this.Parent.Parent.ObjectSets.Count > 0)
                {
                    string objectSet = this.Parent.ObjectSet;
                    if (string.IsNullOrEmpty(objectSet))
                    {
                        return null;
                    }
                    return this.Parent.Parent.ObjectSets[this.Parent.ObjectSet].TargetSets;
                }
                return null;
            }
        }

        /// <summary>
        /// Policy File path in offline mode
        /// </summary>
        [ExtendedProperty()]
        public string PolicyFilePath
        {
            get
            {
                return this.policyFilePath;
            }
            set
            {
                traceContext.TraceVerbose("Setting PolicyFilePath to: {0}", value);
                if (value != this.policyFilePath)
                {
                    this.policyFilePath = value;
                }
            }
        }

        /// <summary>
        /// Offline mode or not
        /// </summary>
        [ExtendedProperty()]
        public bool OfflineMode
        {
            get
            {
                bool offlineMode = false;

                Policy policyInstance = this.Parent;

                if (null != policyInstance)
                {
                    if (policyInstance.Parent == null
                        || ((ISfcHasConnection)policyInstance.Parent).ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                    {
                        offlineMode = true;
                    }
                    else
                    {
                        offlineMode = false;
                    }
                }

                return offlineMode;
            }
        }

        /// <summary>
        /// Root name of the policy object
        /// </summary>
        [ExtendedProperty("Condition")]
        public string RootName
        {
            get 
            {
                if (this.Parent.Parent.ObjectSets.Count > 0)
                {
                    string objectSet = this.Parent.ObjectSet;
                    
                    if (!string.IsNullOrEmpty(objectSet))
                    {
                        return this.Parent.Parent.ObjectSets[objectSet].RootLevel;
                    }
                }
                // NOTE !!! 
                // This string shoudn't be localized.
                // It is always 'Server' in all languages.
                return "Server";
            }
        }

        /// <summary>
        /// Root name of the policy object
        /// </summary>
        [ExtendedProperty("Condition")]
        public bool EnableRootRestriction
        {
            get
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(this.RootName);

                    SfcDomainInfo targetDomain  = SfcRegistration.Domains[this.RootName];
                    if (targetDomain!=null)
                    {
                        System.Diagnostics.Debug.WriteLine(targetDomain.Name);
                        System.Diagnostics.Debug.WriteLine(targetDomain.RootType.FullName);
                        System.Diagnostics.Debug.WriteLine("Root Facets:");
                        foreach (Type rt in FacetRepository.GetRootFacets(targetDomain.RootType))
                        {
                            System.Diagnostics.Debug.WriteLine("\t"+rt.FullName);

                        }

                        return FacetRepository.GetRootFacets(targetDomain.RootType).Count > 0;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

    }
}
