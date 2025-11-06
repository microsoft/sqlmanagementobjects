// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class to which finds referenced,refrencing objects and children
    /// </summary>
    public class SmoDependencyDiscoverer : ISmoDependencyDiscoverer
    {
        /// <summary>
        /// Urn types to be excluded while discovering children
        /// </summary>
        internal HashSet<UrnTypeKey> filteredUrnTypes;

        internal IDatabasePrefetch DatabasePrefetch { get; set; }

        /// <summary>
        /// Dictionary to store creating state objects' urn
        /// </summary>
        internal CreatingObjectDictionary creatingDictionary;

        /// <summary>
        /// Sets or gets the preferences that indicate the type of scripting being performed
        /// </summary>
        public ScriptingPreferences Preferences { get; set; }

        /// <summary>
        /// When generating script for DW tables, Clustered Indexes and Clustered Columnstore inline statements are generated inline to CREATE TABLE statement.
        /// Therefore, in order not to script them twice, URNs for CCI and CI are not added to the list of objects to be scripted.
        /// </summary>
        private static HashSet<UrnTypeKey> DwFilteredUrnTypes = new HashSet<UrnTypeKey>{new UrnTypeKey("ClusteredIndex")};

        private ChildrenDiscoveryEventHandler childrenDiscovery;

        /// <summary>
        /// Event to be fired after every list of sfc children get discovered for object
        /// </summary>
        internal event ChildrenDiscoveryEventHandler ChildrenDiscovery
        {
            add
            {
                childrenDiscovery += value;
            }
            remove
            {
                childrenDiscovery -= value;
            }
        }

        /// <summary>
        /// Initialize with server
        /// </summary>
        /// <param name="server"></param>
        public SmoDependencyDiscoverer(Server server)
        {
            Server = server;
            filteredUrnTypes = new HashSet<UrnTypeKey>();
            this.Preferences = new ScriptingPreferences();
            this.Preferences.DependentObjects = true;
        }

        /// <summary>
        /// Constructs a new SmoDependencyDiscoverer for a Server and ScriptingOptions combination. 
        /// The Preferences will be pre-populated based on the ScriptingOptions provided
        /// </summary>
        /// <param name="server"></param>
        /// <param name="so"></param>
        public SmoDependencyDiscoverer(Server server, ScriptingOptions so) : this(server)
        {
            Preferences = so.GetScriptingPreferences();
            filteredUrnTypes = so.GetSmoUrnFilterForDiscovery(server).filteredTypes;
        }

        #region ISmoDependencyDiscoverer Members

        /// <summary>
        /// Server for the urns provided
        /// </summary>
        public Server Server { get; set; }

        /// <summary>
        /// Discover the dependencies of given input urns
        /// </summary>
        /// <param name="urns">input urns</param>
        /// <returns></returns>
        public IEnumerable<Urn> Discover(IEnumerable<Urn> urns)
        {
            HashSet<Urn> discoveredUrns = new HashSet<Urn>(urns);

            if (this.Preferences.DependentObjects)
            {
                discoveredUrns = this.ReferenceDiscovery(discoveredUrns);
            }

            if (this.Preferences.SfcChildren)
            {
                discoveredUrns = this.SfcChildrenDiscovery(discoveredUrns);
            }
            else if (this.DatabasePrefetch != null)
            {
                return new HashSet<Urn>(this.DatabasePrefetch.PrefetchObjects(discoveredUrns));
            }
            return discoveredUrns;
        }

        #endregion

        /// <summary>
        /// Discover Children using propagate info
        /// </summary>
        /// <param name="discoveredUrns"></param>
        /// <returns></returns>
        private HashSet<Urn> SfcChildrenDiscovery(HashSet<Urn> discoveredUrns)
        {

            SqlSmoObject.PropagateAction propagateAction = this.GetPropagateAction();

            if (propagateAction == SqlSmoObject.PropagateAction.Drop || propagateAction == SqlSmoObject.PropagateAction.Alter)
            {
                return discoveredUrns;
            }

            IEnumerable<Urn> Urns = (this.DatabasePrefetch != null) ? this.DatabasePrefetch.PrefetchObjects(discoveredUrns) : discoveredUrns;
            HashSet<Urn> childUrns = new HashSet<Urn>();
            foreach (Urn urn in Urns)
            {
                if (urn.Type.Equals("UnresolvedEntity"))
                {
                    continue;
                }
                List<SqlSmoObject.PropagateInfo> propInfoList = new List<SqlSmoObject.PropagateInfo>();
                SqlSmoObject obj = this.creatingDictionary.SmoObjectFromUrn(urn);
                // Treat CreateOrAlter as Create for non-ICreateOrAlter implementations.
                SqlSmoObject.PropagateInfo[] propInfoArray = obj.GetPropagateInfoForDiscovery(propagateAction == SqlSmoObject.PropagateAction.CreateOrAlter && !(obj is ICreateOrAlterable) ? SqlSmoObject.PropagateAction.Create : propagateAction);
                if (propInfoArray != null)
                {
                    propInfoList.AddRange(propInfoArray);
                }

                List<Urn> childrenList = this.GetScriptableChildren(propInfoList, propagateAction);
                childUrns.UnionWith(childrenList);

                if (this.childrenDiscovery != null)
                {
                    this.childrenDiscovery(this, new ChildrenDiscoveryEventArgs(urn, childrenList));
                }
            }
            discoveredUrns.UnionWith(childUrns);
            return discoveredUrns;
        }

        /// <summary>
        /// Recursively Get all children using propagateinfo
        /// </summary>
        /// <param name="propInfoList"></param>
        /// <param name="propagateAction"></param>
        /// <returns></returns>
        private List<Urn> GetScriptableChildren(List<SqlSmoObject.PropagateInfo> propInfoList, SqlSmoObject.PropagateAction propagateAction)
        {
            List<Urn> childrenUrns = new List<Urn>();
            foreach (SqlSmoObject.PropagateInfo pi in propInfoList)
            {
                ICollection smoObjCol = null;
                if (null != pi.col)
                {
                    smoObjCol = pi.col;
                }
                else if (null != pi.obj)
                {
                    smoObjCol = new SqlSmoObject[] { pi.obj };
                }
                else
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(pi.UrnTypeKey) && filteredUrnTypes.Contains(new UrnTypeKey(pi.UrnTypeKey)))
                {
                    continue;
                }


                //saving the expensive initialization process of
                //collection class (esp PhyisicalPartitionCollection) inside next foreach loop
                if (pi.bWithScript || pi.bPropagateScriptToChildLevel)
                {
                    IEnumerator enumerator;
                    var smoCollection = smoObjCol as SmoCollectionBase;
                    if (smoCollection != null)
                    {
                        enumerator = smoCollection.GetEnumerator(this.Preferences);
                    }
                    else
                    {
                        enumerator = smoObjCol.GetEnumerator();
                    }

                    while (enumerator.MoveNext())
                    {
                        SqlSmoObject obj = (SqlSmoObject) enumerator.Current;
                        if(obj.IsSupportedObject(obj.GetType(), this.Preferences) == false)
                        {
                            continue;
                        }

                        if (pi.bWithScript && !(obj is Index && (obj as Index).IsSqlDwIndex && DwFilteredUrnTypes.Contains(new UrnTypeKey(pi.UrnTypeKey))))
                        {
                            this.creatingDictionary.Add(obj);
                            childrenUrns.Add(obj.Urn);
                        }

                        SqlSmoObject.PropagateInfo[] propInfoArray = obj.GetPropagateInfoForDiscovery(propagateAction == SqlSmoObject.PropagateAction.CreateOrAlter && !(obj is ICreateOrAlterable) ? SqlSmoObject.PropagateAction.Create : propagateAction);
                        if (propInfoArray != null)
                        {
                            childrenUrns.AddRange(this.GetScriptableChildren(new List<SqlSmoObject.PropagateInfo>(propInfoArray), propagateAction));
                        }
                    }
                }
            }
            return childrenUrns;
        }

        /// <summary>
        /// Gets Propagate action for children
        /// </summary>
        /// <returns></returns>
        private SqlSmoObject.PropagateAction GetPropagateAction()
        {
            switch (this.Preferences.Behavior)
            {
                case ScriptBehavior.Create:
                    return SqlSmoObject.PropagateAction.Create;
                case ScriptBehavior.Drop:
                    return SqlSmoObject.PropagateAction.Drop;
                case ScriptBehavior.CreateOrAlter:
                    return SqlSmoObject.PropagateAction.CreateOrAlter;
                case ScriptBehavior.DropAndCreate:
                    return SqlSmoObject.PropagateAction.Create;
                default:
                    Diagnostics.TraceHelper.Assert(false, "Invalid Condition");
                    return SqlSmoObject.PropagateAction.Create;
            }
        }

        /// <summary>
        /// Discover reference objects using DependencyWalker
        /// </summary>
        /// <param name="urns"></param>
        /// <returns></returns>
        private HashSet<Urn> ReferenceDiscovery(HashSet<Urn> urns)
        {
            if (this.Preferences.IgnoreDependencyError)
            {
                List<Urn> discoverSupportedObjects = new List<Urn>();

                foreach (var item in urns)
                {
                    if (this.DiscoverSupported(item) && (!this.creatingDictionary.ContainsKey(item)))
                    {
                        discoverSupportedObjects.Add(item);
                    }
                }

                urns.UnionWith(this.CallDependencyWalker(discoverSupportedObjects.ToArray()));
            }
            else
            {
                Urn[] urnarray = new Urn[urns.Count];
                urns.CopyTo(urnarray);
                urns = this.CallDependencyWalker(urnarray);
            }

            return urns;
        }

        private HashSet<Urn> CallDependencyWalker(Urn[] urns)
        {
            HashSet<Urn> visitedUrns = new HashSet<Urn>();

            if (urns.Length > 0)
            {
                DependencyCollection depList = new DependencyCollection();
                bool deporder = (this.Preferences.Behavior == ScriptBehavior.Create);
                DependencyWalker dependencyWalker = new DependencyWalker(this.Server);

                // discover dependencies
                DependencyTree tree = dependencyWalker.DiscoverDependencies(urns, deporder);

                // add all dependencies for scripting               
                foreach (Dependency item in tree.Dependencies)
                {
                    visitedUrns.Add(item.Urn);
                }
                return visitedUrns;
            }
            return visitedUrns;
        }       

        /// <summary>
        /// Verify Object is Supported by dependency discovery
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        private bool DiscoverSupported(Urn urn)
        {
            switch (urn.Type)
            {
                case "Table":
                case "UserDefinedFunction":
                case "View":
                case "StoredProcedure":
                case "Default":
                case "Rule":
                case "Trigger":
                case "UserDefinedAggregate":
                case "Synonym":
                case "Sequence":
                case "UserDefinedDataType":
                case "XmlSchemaCollection":
                case "UserDefinedType":
                case "SqlAssembly":
                case nameof(ExternalLanguage):
                case "ExternalLibrary":
                case "PartitionScheme":
                case "PartitionFunction":
                case "UserDefinedTableType":
                case "UnresolvedEntity":
                case "DdlTrigger":
                case "PlanGuide":
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Delegate to handle children discovery progress
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void ChildrenDiscoveryEventHandler(object sender, ChildrenDiscoveryEventArgs e);

    /// <summary>
    /// Children discovery progress event arguments class
    /// </summary>
    internal class ChildrenDiscoveryEventArgs : EventArgs
    {
        internal ChildrenDiscoveryEventArgs(Urn parent, IEnumerable<Urn> children)
        {
            this.Parent = parent;
            this.Children = children;
        }

        public Urn Parent { get; private set; }

        public IEnumerable<Urn> Children { get; private set; }
    }
}
