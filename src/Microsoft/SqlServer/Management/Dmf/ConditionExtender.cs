// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Decorator for the Condition object. Used add additional properties to the base Condition object
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ConditionExtender : SfcObjectExtender<Condition>
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ConditionExtender");
        /// <summary>
        /// Collection of all facets in Policy store
        /// </summary>
        private FacetInfoCollection facetsCollection;

        /// <summary>
        /// Collection of facets exposed.
        /// </summary>
        private ReadOnlyCollection<FacetInfo> facets;
        private ReadOnlyCollection<FacetInfo> rootFacets;
        ReadOnlyCollection<Policy> dependentPolicies;

        /// <summary>
        /// default ctor
        /// </summary>
        public ConditionExtender()
            : base()
        {
        }

        /// <summary>
        /// ctor. Takes parent Condition object to aggregate on
        /// </summary>
        /// <param name="condition"></param>
        public ConditionExtender(Condition condition)
            : base(condition)
        {
        }

        /// <summary>
        /// ctor. Create a new Condition object and aggregates on it.
        /// </summary>
        /// <param name="policyStore"></param>
        /// <param name="name"></param>
        public ConditionExtender(PolicyStore policyStore, string name)
            : base(new Condition(policyStore, name))
        {
        }

        /// <summary>
        /// Translates parent's string 'Expression' property into
        /// Expression node property
        /// </summary>
        [ExtendedProperty("Expression")]
        public ExpressionNode ExpressionNode
        {
            get
            {
                return this.Parent.ExpressionNode;
            }
            set
            {
                traceContext.TraceVerbose("Setting ExpressionNode to: {0}", value);
                if (this.Parent.ExpressionNode != value)
                {
                    this.Parent.ExpressionNode = value;
                }
            }
        }


        /// <summary>
        /// Translates parent's 'Facet' string property into FacedInfo object property
        /// </summary>
        [ExtendedProperty("Facet")]
        public FacetInfo FacetInfo
        {
            get
            {
                string facet = this.Parent.Facet;
                if (string.IsNullOrEmpty(facet))
                {
                    return null;
                }

                // KJ
                // User conditions cannot be defined over Utility facets
                // If system condition (on Utility facet) is unmarked
                // FacetsCollection will not contain condition's facet 
                // (Utility facets are filtered out for user conditions) 
                //
                // Add missing Facet to the collection
                if (!FacetsCollection.Contains(facet))
                {
                    this.facetsCollection.Add (new FacetInfo (facet));
                }

                return FacetsCollection[facet];
            }
            set
            {
                traceContext.TraceVerbose("Setting FacetInfo to: {0}", value);
                string newValue = value == null ? null : value.Name;
                if (this.Parent.Facet != newValue)
                {
                    this.Parent.Facet = newValue;

                    // When a Facet is changed, expresssionnode property that was
                    // set before is not valid for the new facet, so setting it as null
                    this.Parent.ExpressionNode = null;
                }
            }
        }

        /// <summary>
        /// Provides a list of available facets
        /// </summary>
        [ExtendedProperty()]
        public ReadOnlyCollection<FacetInfo> Facets
        {
            get
            {
                if (this.facets == null)
                {
                    this.facets = new ReadOnlyCollection<FacetInfo>(FacetsCollection);
                }
                return this.facets;
            }
        }

        /// <summary>
        /// provides a list of dependant policies
        /// </summary>
        [ExtendedProperty()]
        public ReadOnlyCollection<Policy> DependentPolicies
        {
            get
            {
                if (this.dependentPolicies == null)
                {
                    List<Policy> list = new List<Policy>();

                    string name = this.Parent.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        foreach (Policy p in this.Parent.Parent.Policies)
                        {
                            if (p.DependsOnCondition(name))
                            {
                                list.Add(p);
                            }
                        }
                    }

                    this.dependentPolicies = new ReadOnlyCollection<Policy>(list);
                }
                return this.dependentPolicies;
            }
        }

        /// <summary>
        /// since PolicyStore.Facets re-creates collection on every call, we have to cache it here
        /// </summary>
        FacetInfoCollection FacetsCollection
        {
            get
            {
                if (this.facetsCollection == null)
                {
                    if (this.Parent.IsSystemObject)
                    {
                        this.facetsCollection = PolicyStore.Facets;
                    }
                    else
                    {
                        this.facetsCollection = PolicyStore.EnumDomainFacets("SMO", null);
                    }
                }
                return this.facetsCollection;
            }
        }

        /// <summary>
        /// Expose collection of facets that are attributed as root facets
        /// </summary>
        [ExtendedProperty()]
        public ReadOnlyCollection<FacetInfo> RootFacets
        {
            get
            {
                if (this.rootFacets == null)
                {
                    //Dependency on SMO. Hardcoding rootType now, in the future should get root type from SFC
                    FacetInfoCollection rootFacetCollection = PolicyStore.EnumRootFacets(typeof(Smo.Server));
                    this.rootFacets = new ReadOnlyCollection<FacetInfo>(rootFacetCollection);
                }
                return this.rootFacets;
            }
        }
    }
}
