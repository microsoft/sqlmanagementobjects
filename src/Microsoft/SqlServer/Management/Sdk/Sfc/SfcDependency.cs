// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

/*//////////////////////////////////////////////////////////////////////////////////////
SFC Dependency Engine

All internal storage is canonicalized to Keychains regardless of the input population type.
Population is a set of Keychains obtained from
1. SFCInstance
2. SFCDomainRootInstance, URN
3. Keychain
4. SFCObjectQuery(Builder) - not in v1

Based on relation tuples derived from
1. IDiscoverDomain - bulk discovery for nodes in the same DomainContext (keychain1.Rootkey == keychain2.Rootkey)
2. IDiscoverType - bulk discovery for nodes of the same DomainContext and DomainType (keychain.Type == keychain2.Type)
3. IDiscoverObject = individual discovery for each instance of any DomainType

Discovery proceeds until all DepNodes are marked as Discovered.

Iterators are stateful based on DepIterator.
1. Fwd and bwd flavors
2. Optional DomainContext filtering (i.e., iterating by distinct DomainContext instead of engine-wide)
//////////////////////////////////////////////////////////////////////////////////////*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    #region Public enums
    /// <summary>
    /// The SfcTypeRelation enum describes the type of relation between two objects. 
    /// It defines whether the relation is of a parent-child vs. referential nature, 
    /// the direction of the relation if any, and determines which types of relations are applicable 
    /// in different DepedencyDIscoveryMode processing scenarios.
    /// </summary>
    public enum SfcTypeRelation
    {
        /// <summary>
        /// A direct containment relation which is not required for completeness of the parent container.
        /// </summary>
        ContainedChild = 0,
        /// <summary>
        /// A direct containment relation which is required for completeness of the parent container. 
        /// These relations are proxy candidates where applicable.
        /// </summary>
        RequiredChild,
        /// <summary>
        /// An indirect reference relation which has a direction for ordering.
        /// </summary>
        StrongReference,
        /// <summary>
        /// An indirect reference relation which has no inherent directionality to it. It represents discovery with no particular ordering ties.
        /// </summary>
        WeakReference
    }

    /// <summary>
    /// The SfcDependencyAction enum indicates the intended action for which this graph instance is to applied. it is made available to the client code
    /// as discovery, filtering and results enumeration are processed to make use of this information. It is assumed that domain-specific decisions
    /// will take this intent into account as processing proceeds.
    /// This is a fixed enum which may not be extended at present.
    /// </summary>
    public enum SfcDependencyAction
    {
        /// <summary>
        /// An unknown or generic action.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// A serialization action.
        /// </summary>
        Serialize,
        /// <summary>
        /// A create action.
        /// </summary>
        Create,
        /// <summary>
        /// A drop action.
        /// </summary>
        Drop,
        /// <summary>
        ///  An alter action.
        /// </summary>
        Alter,
        /// <summary>
        /// A rename action involving a new SfcKey.
        /// </summary>
        Rename,
        /// <summary>
        /// A move action involving a new SfcParent.
        /// </summary>
        Move,
        /// <summary>
        /// A combination of Create, Drop, Alter and Rename as needed to merge object trees.
        /// </summary>
        Merge,
        /// <summary>
        /// A difference comparison action.
        /// </summary>
        Diff,
        /// <summary>
        /// A copy or transfer action.
        /// </summary>
        Copy
    }

    /// <summary>
    /// The SfcDependencyDirection enum indicates the direction of the relation relative to the current object. 
    /// This is used when the direction is not already somehow implied.
    /// </summary>
    public enum SfcDependencyDirection
    {
        /// <summary>
        /// No direction implies association of two objects without a particular ordering.
        /// </summary>
        None,
        /// <summary>
        /// An inbound relation means another object is dependent on the current object. 
        /// </summary>
        Inbound,
        /// <summary>
        /// An outbound relation means the current object is dependent on another object.
        /// </summary>
        Outbound
    }

    /// <summary>
    /// The DiscoveryMode enum indicates the types of relationships which should be processed in the dependency graph.
    /// These are pre-defined permutations of the lower-level relation flags which we currently do not expose direct control over picking.
    /// It is the responsibility of the domains and objects called on to add relations to honor this mode.
    /// 1. Children - follow all children of the root objects recursively. No references are followed.
    /// 2. Full - follow all children and all references of root objects recursively.
    /// 3. Propagate - follow all children to emulate what PropagateInfo does in SMO in Yukon.
    /// 3. UsedBy - follow direct parent and inbound references of root objects. No recursion.
    /// 4. Uses - follow direct children and outbound references of root objects. No recursion.
    /// </summary>
    public enum SfcDependencyDiscoveryMode
    {
        /// <summary>
        /// Include only required children for minimal completeness of parent objects.
        /// </summary>
        Children = 0,
        /// <summary>
        /// Include all children even those not necessary for minimal completeness of parent objects.
        /// </summary>
        Full,
        /// <summary>
        /// Include all children as needed for PropagateInfo emulation.
        /// </summary>
        Propagate,
        /// <summary>
        /// Include only immmediate parents of required children and immediate inbound references.
        /// </summary>
        UsedBy,
        /// <summary>
        /// Include only immediate required children and immediate outbound references.
        /// </summary>
        Uses
    }

    #endregion // Public enums

    #region Public interfaces

    /// <summary>
    /// The ISfcDependencyDiscoveryObjectSink interface is implemented by the SfcDependencyEngine class. 
    /// It is passed to SfcInstance objects as a sink to add relationships into the graph. 
    /// Note that the target objects receiving the interface are always one side of the relation tuple, 
    /// hence limiting addition to directly-related relationships.
    /// </summary>
    public interface ISfcDependencyDiscoveryObjectSink
    {
        /// <summary>
        /// Get the discovery action this dependency session is processing. 
        /// This may affect which relationships are added.
        /// </summary>
        SfcDependencyAction Action
        {
            get;
        }

        #region Add relations
        /// <summary>
        /// Add a directional reference from this object to the target object.
        /// The relation type and discovered state is applied to the target object.
        /// </summary>
        void Add(SfcDependencyDirection direction, SfcInstance targetObject, SfcTypeRelation relation, bool discovered);

        /// <summary>
        /// Add a directional reference from this object to each of the target objects or keychains.
        /// The relation type and discovered state is applied to each target object or keychain added.
        /// </summary>
        void Add(SfcDependencyDirection direction, IEnumerator targetObjects, SfcTypeRelation relation, bool discovered);

        /// <summary>
        /// Add a directional reference from this object to each of the target objects or keychains.
        /// The relation type and discovered state is applied to each target object or keychain added.
        /// </summary>
        void Add<T>(SfcDependencyDirection direction, IEnumerable<T> targetObjects, SfcTypeRelation relation, bool discovered) where T : SfcInstance;
        #endregion

    }
    #endregion // Public interfaces

    #region internal class DepStackNode
    internal class DepStackNode
    {
        /// <summary>
        /// The processingState indicates the progress as to which part of processing a graph node we are currently on.
        /// A forward iteration through the graph visits: node ancestors recursively, then the node itself is returned, then node children recursively.
        /// </summary>
        public enum ProcessingState
        {
            OnSelf = 0,
            OnAncestors,
            OnChildren
        }

        DepStackNode()
        {
            this.node = null;
            this.state = DepStackNode.ProcessingState.OnAncestors;
            this.index = 0;
        }

        public DepStackNode(DepNode node)
        {
            this.node = node;
            this.state = DepStackNode.ProcessingState.OnAncestors;
            this.index = 0;
        }

        /// <summary>
        /// The current graph DepNode we are processing.
        /// </summary>
        private DepNode node;
        public DepNode Node
        {
            get { return node; }
            set { node = value; }
        }

        /// <summary>
        /// The current iteration state of the stack node. Either we are on ourself, looking at our ancestors via index, or at our children via index.
        /// </summary>
        private ProcessingState state;
        public ProcessingState State
        {
            get { return state; }
            set { state = value; }
        }

        /// <summary>
        /// The next index slot to process. Depends on State to determine whether the index is for the ancestor or child list.
        /// </summary>
        private int index;
        public int Index
        {
            get { return index; }
            set { index = value; }
        }
    }
    #endregion

    #region internal class DepNode
    /// <summary>
    /// The DepNode class is the basic node storage unit in the SfcDependencyEngine graph. It is expected
    /// to be used in a KeyedCollection which maps embedded key identity (via a Keychain) to the DepNode for that object.
    /// A list of parent and child nodes related to this node are kept, as well as a flag for discovery visitation.
    /// </summary>
    internal class DepNode
    {
        private DepNode()
        {
        }

        public DepNode(SfcInstance obj)
        {
            // TODO: Remember the object for v1, we may change this and not store both obj and keychain in v2.
            // This is only to get around the issue of kc.GetObject() returning a new non-canonical dupe object since we don't have
            // an Object Cache yet. Also, root objects can't be found again via keychain alone yet.
            sfcobj = obj;
            keychain = obj.KeyChain;
        }

        // Use of this ctor is discouraged in v1 due to issues around no Object Cache and rehydration via keychain resulting in dupes made.
        // Also, server-level root objects simply cannot be found via keychain hydration in v1.
        internal DepNode(SfcKeyChain kc)
        {
            // TODO: Get the object up-front in v1 since we always need it anyhow.
            sfcobj = kc.GetObject();
            keychain = kc;
        }

        public void AddAncestor(DepNode node, bool isPhysicalRelation)
        {
            if (ancestors == null)
            {
                physicalAncestorMask = new List<bool>();
                ancestors = new List<DepNode>();
            }
            physicalAncestorMask.Add(isPhysicalRelation);
            ancestors.Add(node);
        }

        public void AddChild(DepNode node, bool isPhysicalRelation)
        {
            if (children == null)
            {
                physicalChildMask = new List<bool>();
                children = new List<DepNode>();
            }
            physicalChildMask.Add(isPhysicalRelation);
            children.Add(node);
        }

        private SfcInstance sfcobj = null;
        public SfcInstance Instance
        {
            get { return sfcobj; }
        }

        /// <summary>
        /// The embedded Keychain key which cannot be changed after construction
        /// </summary>
        private SfcKeyChain keychain = null;
        internal SfcKeyChain Keychain
        {
            get { return keychain; }
        }

        /// <summary>
        /// List of ancestor DepNodes
        /// </summary>
        private List<DepNode> ancestors = null;
        public List<DepNode> Ancestors
        {
            get { return ancestors; }
            set { ancestors = value; }
        }

        /// <summary>
        /// Physical Ancestor relations mask indicating which Ancestors are related in a Parent-Child manner (i.e. not References).
        /// Since we don't have named field indexers in C# just have a get and set method.
        /// </summary>
        private List<bool> physicalAncestorMask = null;
        public List<bool> PhysicalAncestorMask
        {
            get { return physicalAncestorMask; }
            set { physicalAncestorMask = value; }
        }

        public bool IsPhysicalAncestor(int i)
        {
            return physicalAncestorMask[i];
        }
        public void SetPhysicalAncestor(int i, bool b)
        {
            physicalAncestorMask[i] = b;
        }
        public int CountPhysicalAncestors
        {
            get
            {
                int c = 0;
                foreach (bool b in physicalAncestorMask)
                {
                    if (b)
                    {
                        c++;
                    }
                }

                return c;
            }
        }

        /// <summary>
        /// List of children DepNodes
        /// </summary>
        private List<DepNode> children = null;
        public List<DepNode> Children
        {
            get { return children; }
            set { children = value; }
        }

        /// <summary>
        /// Physical Child relations mask indicating which Children are related in a Parent-Child manner (i.e. not References).
        /// Since we don't have named field indexers in C# just have a get and set method.
        /// </summary>
        private List<bool> physicalChildMask = null;
        public List<bool> PhysicalChildMask
        {
            get { return physicalChildMask; }
            set { physicalChildMask = value; }
        }

        public bool IsPhysicalChild(int i)
        {
            return physicalChildMask[i];
        }
        public void SetPhysicalChild(int i, bool b)
        {
            physicalChildMask[i] = b;
        }
        public int CountPhysicalChildren
        {
            get
            {
                int c = 0;
                foreach (bool b in physicalChildMask)
                {
                    if (b)
                    {
                        c++;
                    }
                }

                return c;
            }
        }

        /// <summary>
        /// Whether this DepNode has been visited for discovery purposes
        /// (currently this is all or nothing; someday we could support partial discovery on some relations)
        /// </summary>
        private bool discovered = false;
        public bool Discovered
        {
            get
            {
                return discovered;
            }
            set
            {
                discovered = value;
            }
        }

    }
    #endregion // DepNode

    #region private class NodeGraph
    internal class NodeGraph : KeyedCollection<SfcKeyChain, DepNode>
    {
        // The lookup dictionary will always be present since we don't set a threshold in the ctor
        public NodeGraph() : base() { }

        // This tells the collection how to get the key embedded in the value, since we want the Keychain
        // accessible when we only have a DepNode so we make it part of the value.
        protected override SfcKeyChain GetKeyForItem(DepNode node)
        {
            return node.Keychain;
        }

#if !NETCOREAPP
        // KeyedCollection is missing TryGetValue like a Dictionary has
        public bool TryGetValue(SfcKeyChain kc, out DepNode node)
        {
            // Use dictionary if present, otherwise scan the list
            if (this.Dictionary == null)
            {
                node = null;
                foreach (DepNode temp in this)
                {
                    if (kc == GetKeyForItem(temp))
                    {
                        node = temp;
                        return true;
                    }
                }
                return false;
            }

            return this.Dictionary.TryGetValue(kc, out node);
        }
#endif
    }
    #endregion // NodeGraph

    #region public sealed class SfcDependencyRootList
    internal sealed class SfcDependencyRootList : KeyedCollection<SfcKeyChain, SfcInstance>
    {
        // The lookup dictionary will always be present since we don't set a threshold in the ctor
        public SfcDependencyRootList() : base() { }

        // Copy ctor to populate a new SfcDependencyRootList with an existing collection's references
        public SfcDependencyRootList(ICollection<SfcInstance> collection)
        {
            foreach (SfcInstance obj in collection)
            {
                this.Add(obj);
            }
        }

        // This tells the collection how to get the key embedded in the value, since we want the Keychain
        // from the object.
        protected override SfcKeyChain GetKeyForItem(SfcInstance obj)
        {
            return obj.KeyChain;
        }

#if !NETCOREAPP
        // KeyedCollection is missing TryGetValue like a Dictionary has
        public bool TryGetValue(SfcKeyChain kc, out SfcInstance obj)
        {
            // Use dictionary if present, otherwise scan the list
            if (this.Dictionary == null)
            {
                obj = null;
                foreach (SfcInstance temp in this)
                {
                    if (kc == GetKeyForItem(temp))
                    {
                        obj = temp;
                        return true;
                    }
                }
                return false;
            }

            return this.Dictionary.TryGetValue(kc, out obj);
        }
#endif
    }
    #endregion // SfcDependencyRootList

    #region public sealed class SfcDependencyNode
    /// <summary>
    /// The SfcDependencyNode is used for enumerator access to the Sfc objects and their KeyChains resulting from a SfcDependencyEngine discovery.
    /// </summary>
    public sealed class SfcDependencyNode
    {
        private DepNode depNode;

        internal SfcDependencyNode(DepNode depNode)
        {
            this.depNode = depNode;
        }

        /// <summary>
        /// The SfcKeyChain of this dependency node.
        /// </summary>
        internal SfcKeyChain SfcKeyChain
        {
            get { return depNode.Keychain; }
        }

        /// <summary>
        /// The Instance of this dependency node.
        /// </summary>
        public SfcInstance Instance
        {
            get { return depNode.Instance; }
        }

        /// <summary>
        /// The discovered state of this node.
        /// Domain-level discovery should set this accordingly per node, whereas object-level discovery will have it set automatically for self.
        /// </summary>
        public bool Discovered
        {
            get { return depNode.Discovered; }
            set { depNode.Discovered = value; }
        }

        /// <summary>
        /// The ancestor relation state of this node.
        /// Indicates whether this ancestor is a physical relation or not. Filter via this flag to process only Parent-Child relations.
        /// </summary>
        public bool IsPhysicalAncestor(int index)
        {
            return depNode.IsPhysicalAncestor(index);
        }

        /// <summary>
        /// The child relation state of this node.
        /// Indicates whether this child is a physical relation or not. Filter via this flag to process only Parent-Child relations.
        /// </summary>
        public bool IsPhysicalChild(int index)
        {
            return depNode.IsPhysicalChild(index);
        }

        /// <summary>
        /// Enumerate over the children of this dependency node.
        /// </summary>
        /// <returns>A enumerator for the children of the current node, or null if there are no children.</returns>
        public IEnumerable<SfcDependencyNode> Children
        {
            get
            {
                if (depNode.Children == null || depNode.Children.Count == 0)
                {
                    return null;
                }
                return new SfcDependencyEngine.DependencyNodeEnumerator(depNode.Children.GetEnumerator());
            }
        }

        /// <summary>
        /// The number of children of the this dependency node.
        /// </summary>
        /// <returns></returns>
        public int ChildCount
        {
            get { return depNode.Children == null ? 0 : depNode.Children.Count; }
        }

        /// <summary>
        /// Enumerate over the ancestors of this dependency node.
        /// </summary>
        /// <returns>A enumerator for the ancestors of the current node, or null if there are no ancestors.</returns>
        public IEnumerable<SfcDependencyNode> Ancestors
        {
            get
            {
                if (depNode.Ancestors == null || depNode.Ancestors.Count == 0)
                {
                    return null;
                }
                return new SfcDependencyEngine.DependencyNodeEnumerator(depNode.Ancestors.GetEnumerator());
            }
        }

        /// <summary>
        /// The number of ancestors of the this dependency node.
        /// </summary>
        /// <returns></returns>
        public int AncestorCount
        {
            get { return depNode.Ancestors == null ? 0 : depNode.Ancestors.Count; }
        }
#if false // CC_NOT_USED
        /// <summary>
        /// Enumerate over the physical children of this dependency node.
        /// </summary>
        /// <returns>An enumerator for the physical children of the current node, or null if there are no physical children.</returns>
        public IEnumerable<SfcDependencyNode> PhysicalChildren
        {
            get
            {
                if (depNode.Children == null || depNode.Children.Count == 0 || depNode.CountPhysicalChildren == 0)
                {
                    return null;
                }
                return new SfcDependencyEngine.DependencyNodeEnumerator(depNode.Children.GetEnumerator(), depNode.PhysicalChildMask);
            }
        }

        /// <summary>
        /// The number of physical children of the this dependency node.
        /// </summary>
        /// <returns></returns>
        public int PhysicalChildCount
        {
            get { return depNode.Children == null ? 0 : depNode.CountPhysicalChildren; }
        }

        /// <summary>
        /// Enumerate over the physical ancestors of this dependency node.
        /// </summary>
        /// <returns>An enumerator for the physical ancestors of the current node, or null if there are no physical ancestors.</returns>
        public IEnumerable<SfcDependencyNode> PhysicalAncestors
        {
            get
            {
                if (depNode.Ancestors == null || depNode.Ancestors.Count == 0 || depNode.CountPhysicalAncestors == 0)
                {
                    return null;
                }
                return new SfcDependencyEngine.DependencyNodeEnumerator(depNode.Ancestors.GetEnumerator(), depNode.PhysicalAncestorMask);
            }
        }

        /// <summary>
        /// The number of physical ancestors of the this dependency node.
        /// </summary>
        /// <returns></returns>
        public int PhysicalAncestorCount
        {
            get { return depNode.Ancestors == null ? 0 : depNode.CountPhysicalAncestors; }
        }
#endif
    }
    #endregion


    #region public sealed class SfcDependencyEngine
    /// <summary>
    /// The DepEngine class provides the graph manipulation, population, storage and discovery execution logic to 
    /// perform complete discovery services, and is then accessed from DepIterators afterwards to walk results in various ways.
    /// </summary>
    public sealed class SfcDependencyEngine : ISfcDependencyDiscoveryObjectSink, IDisposable
    {

        #region Public class DependencyListEnumerator

        public class DependencyListEnumerator : IEnumerator<SfcDependencyNode>, IEnumerable<SfcDependencyNode>
        {
            private Stack<DepStackNode> visitStack;
            private DepNode curNode;
            private SfcDependencyEngine depEngine;
            private List<DepNode> startNodes;
            private Dictionary<SfcKeyChain, bool> visited;

            internal DependencyListEnumerator(SfcDependencyEngine depEngine)
            {
                this.depEngine = depEngine;
                Reset();
            }

            #region IEnumerator<SfcDependencyNode> Members

            public SfcDependencyNode Current
            {
                get { return new SfcDependencyNode(curNode); }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                visitStack = null;
                curNode = null;
                this.depEngine = null;
                this.startNodes = null;
                this.visited = null;
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get { return (object)new SfcDependencyNode(curNode); }
            }

            public bool MoveNext()
            {
                // Clear the current node which only will be set again if we have one below
                curNode = null;

            processNode:
                // If we need more nodes check for any more starting root nodes we haven't processed
                if (visitStack.Count == 0)
                {
                    DepNode rootNode = null;
                    foreach (DepNode rootCheck in startNodes)
                    {
                        if (!visited.ContainsKey(rootCheck.Keychain))
                        {
                            rootNode = rootCheck;
                            break;
                        }
                    }

                    // We are completely done if we have no more starting nodes
                    if (rootNode == null)
                    {
                        return false;
                    }
                    else
                    {
                        visitStack.Push(new DepStackNode(rootNode));
                    }
                }

                // Get the current top stack context
                // If we need to change state, or push or pop below, we do so then go back to processNode and check all over again
                DepStackNode top = visitStack.Peek();
                DepNode topNode = top.Node;

                switch (top.State)
                {
                    case DepStackNode.ProcessingState.OnAncestors:
                        {
                            bool pushedMore = false;
                            if (topNode.Ancestors != null)
                            {
                                // Push the next non-visited child in the current node's child list
                                DepNode nextNode;
                                int ancestorCount = topNode.Ancestors.Count;
                                while (top.Index < ancestorCount)
                                {
                                    nextNode = topNode.Ancestors[top.Index];
                                    top.Index++;
                                    if (!visited.ContainsKey(nextNode.Keychain))
                                    {
                                        visitStack.Push(new DepStackNode(nextNode));
                                        visited.Add(nextNode.Keychain, true);
                                        pushedMore = true;
                                        break;
                                    }
                                }
                            }
                            if (!pushedMore)
                            {
                                // Reset the index counter to 0 since we may have a non-zero value left over from using it for Ancestor scanning state
                                top.Index = 0;
                                top.State = DepStackNode.ProcessingState.OnSelf;
                            }
                        }
                        break;

                    case DepStackNode.ProcessingState.OnSelf:
                        // This is the node we want to treat as Current
                        curNode = top.Node;
                        top.State = DepStackNode.ProcessingState.OnChildren;
                        // This may be redundant if all avenues by which we get this node onto the stack already marked it visited.
                        // But it won't hurt.
                        if (!visited.ContainsKey(top.Node.Keychain))
                        {
                            visited.Add(top.Node.Keychain, true);
                        }
                        break;

                    case DepStackNode.ProcessingState.OnChildren:
                        {
                            bool pushedMore = false;
                            if (topNode.Children != null)
                            {
                                // Push the next non-visited child in the current node's child list
                                DepNode nextNode;
                                int childCount = topNode.Children.Count;

                                while (top.Index < childCount)
                                {
                                    nextNode = topNode.Children[top.Index];
                                    top.Index++;
                                    if (!visited.ContainsKey(nextNode.Keychain))
                                    {
                                        visitStack.Push(new DepStackNode(nextNode));
                                        visited.Add(nextNode.Keychain, true);
                                        pushedMore = true;
                                        break;
                                    }
                                }
                            }
                            if (!pushedMore)
                            {
                                visitStack.Pop();
                            }
                        }
                        break;
                }

                // Either we go back until we run out of start node candidates, or we exit successfully below once curNode is set
                if (curNode == null)
                {
                    goto processNode;
                }

                return true;
            }

            public void Reset()
            {
                visitStack = new Stack<DepStackNode>();
                curNode = null;
                this.startNodes = depEngine.FindUnparentedNodes();
                this.visited = new Dictionary<SfcKeyChain, bool>();
            }

            #region IEnumerable methods

            IEnumerator<SfcDependencyNode> IEnumerable<SfcDependencyNode>.GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            #endregion

            #endregion
        }

        #endregion

        #region Public class DependencyTreeEnumerator

        /// <summary>
        /// The DependencyTreeEnumerator is the top-level enumerator obtained from a SfcDependencyEngine to traverse results of the graph.
        /// Use the Children property of the current SfcDependencyNode item to request a DependencyNodeEnumerator for the item children, if any.
        /// The Current item is presented as a SfcDependencyNode, and the client is responsible for traversal in whatever manner desired.
        /// </summary>
        public class DependencyTreeEnumerator : IEnumerator<SfcDependencyNode>, IEnumerable<SfcDependencyNode>
        {
            private SfcDependencyEngine depEngine;
            private IEnumerator<DepNode> nodeEnumerator;

            internal DependencyTreeEnumerator(SfcDependencyEngine depEngine)
            {
                this.depEngine = depEngine;
                this.nodeEnumerator = depEngine.FindPhysicallyUnparentedNodes().GetEnumerator();
                Reset();
            }

            #region IEnumerator<SfcDependencyNode> Members

            SfcDependencyNode IEnumerator<SfcDependencyNode>.Current
            {
                get { return new SfcDependencyNode(nodeEnumerator.Current); }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                this.depEngine = null;
                this.nodeEnumerator = null;
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get { return (object)((IEnumerator<SfcDependencyNode>)this).Current; }
            }

            public bool MoveNext()
            {
                return nodeEnumerator.MoveNext();
            }

            public void Reset()
            {
                nodeEnumerator.Reset();
            }

            #region IEnumerable methods

            IEnumerator<SfcDependencyNode> IEnumerable<SfcDependencyNode>.GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            #endregion

            #endregion

        }

        #endregion

        #region Public class DependencyNodeEnumerator

        /// <summary>
        /// The DependencyNodeEnumerator is general graph node enumerator which can be obtained in two ways:
        /// 1. From the current item of a DependencyTreeEnumerator via the Children property.
        /// 2. From the current item of a DependencyNodeEnumerator via the Ancestors or Children property. 
        /// The Current item is presented as a SfcKeyChain, and the client is responsible for traversal in whatever manner desired.
        /// </summary>
        public class DependencyNodeEnumerator : IEnumerator<SfcDependencyNode>, IEnumerable<SfcDependencyNode>
        {
            private IEnumerator<DepNode> nodeEnumerator;
            private List<bool> physicalMask;
            private int index;

            internal DependencyNodeEnumerator(IEnumerator<DepNode> nodeEnumerator)
            {
                this.nodeEnumerator = nodeEnumerator;
                this.physicalMask = null;
                Reset();
            }

            /// <summary>
            /// Pass a physicalMask from the node we are coming from if you want physical relations only.
            /// </summary>
            /// <param name="nodeEnumerator"></param>
            /// <param name="physicalMask"></param>
            internal DependencyNodeEnumerator(IEnumerator<DepNode> nodeEnumerator, List<bool> physicalMask)
            {
                this.nodeEnumerator = nodeEnumerator;
                this.physicalMask = physicalMask;
                Reset();
            }

            #region IEnumerator<SfcDependencyNode> Members

            SfcDependencyNode IEnumerator<SfcDependencyNode>.Current
            {
                get { return new SfcDependencyNode(nodeEnumerator.Current); }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                this.nodeEnumerator = null;
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get { return (object)((IEnumerator<SfcDependencyNode>)this).Current; }
            }

            public bool MoveNext()
            {
                if (physicalMask == null)
                {
                    return nodeEnumerator.MoveNext();
                }
                else
                {
                    // Skip non-physical relations (i.e. References)
                    while (nodeEnumerator.MoveNext())
                    {
                        if (physicalMask[index])
                        {
                            index++;
                            return true;
                        }
                        index++;
                    }
                    return false;
                }
            }

            public void Reset()
            {
                nodeEnumerator.Reset();
                index = 0;
            }

            #region IEnumerable methods

            IEnumerator<SfcDependencyNode> IEnumerable<SfcDependencyNode>.GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            #endregion

            #endregion

        }

        #endregion

        #region Constructors
        public SfcDependencyEngine(SfcDependencyDiscoveryMode mode, SfcDependencyAction action)
        {
            m_mode = mode;
            m_action = action;
        }
        #endregion

        #region Data and properties
        /// <summary>
        /// The discovery mode for the dependency engine instance. This is set at construction and cannnot be changed.
        /// </summary>
        private SfcDependencyDiscoveryMode m_mode;

        /// <summary>
        /// The intended action for the dependency engine instance. This is set at construction and cannnot be changed.
        /// </summary>
        private SfcDependencyAction m_action;
        public SfcDependencyAction Action
        {
            get { return m_action; }
        }

        /// <summary>
        /// The set of SfcInstance objects to act as originating roots for the dependency graph.
        /// The graph will expand starting from these objects according to the Mode set.
        /// Getting or setting the root list is always done via copying the internal list.
        /// </summary>
        private SfcDependencyRootList m_DependencyRootList = new SfcDependencyRootList();
        internal SfcDependencyRootList SfcDependencyRootList
        {
            get { return new SfcDependencyRootList(m_DependencyRootList); }
            set { m_DependencyRootList = new SfcDependencyRootList(value); }
        }

        /// <summary>
        /// The default indexer for the dependency engine is to expose a SfcDependencyNode object which can be used to enumerate
        /// ancestors or children, and to get or set the discovered state of a node. You can set properties of the node but cannot assign the node itself.
        /// </summary>
        /// <param name="kc"></param>
        /// <returns></returns>
        SfcDependencyNode this[SfcKeyChain kc]
        {
            get
            {
                DepNode node;
                if (!nodeDict.TryGetValue(kc, out node))
                {
                    return null;
                }
                return new SfcDependencyNode(node);
            }
        }

        /// <summary>
        /// Map a node to its list of Ancestors (predecessors) and Children (successors).
        /// The embedded Keychain unique key and the discovered flag are also in the DepNode value.
        /// This is the graph core data structure.
        /// </summary>
        private NodeGraph nodeDict = new NodeGraph();

        /// <summary>
        /// Queue for nodes waiting to be discovered.
        /// All adds to nodedict also add to queue. Dupes are skipped since it always looks up the keychain stored in the queue to find
        /// the internal DepNode to process, and check its Discovered flag.
        /// All removes from nodedict do not affect the queue, again since it will attempt to lookup each keychain when it comes to it
        /// and removed ones will not be found so will be skipped.
        /// </summary>
        private Queue<SfcKeyChain> nodeQueue = new Queue<SfcKeyChain>();

        /// <summary>
        /// The current DepNode for use by sink methods to know which node we are currently processing.
        /// </summary>
        private DepNode m_currentNode = null;
        #endregion

        #region Private helpers
        /// <summary>
        /// Determine the list of DepNodes which are traversal starters since they have no parent relation.
        /// </summary>
        /// <returns></returns>
        private List<DepNode> FindUnparentedNodes()
        {
            List<DepNode> startNodes = new List<DepNode>();

            // The only nodes we are interested in are ones that are parents but not children of any other node
            foreach (DepNode node in this.nodeDict)
            {
                // If this node is not anyone's child, it's a root.
                // This includes single nodes with no relation to either parents or children.
                if (node.Ancestors == null || node.Ancestors.Count == 0)
                {
                    startNodes.Add(node);
                }
            }

            return startNodes;
        }

        /// <summary>
        /// Determine the list of DepNodes which are children-only traversal starters since they have no non-reference parent relation.
        /// </summary>
        /// <returns></returns>
        private List<DepNode> FindPhysicallyUnparentedNodes()
        {
            List<DepNode> startNodes = new List<DepNode>();

            // The only nodes we are interested in are ones that are parents but not physical children of any other node
            foreach (DepNode node in this.nodeDict)
            {
                // If this node is not anyone's physical child, it's a physical root.
                // This includes single nodes with no relation to either parents or children, and nodes with only referential ancestors.
                if (node.Ancestors == null || node.Ancestors.Count == 0 || node.CountPhysicalAncestors == 0)
                {
                    startNodes.Add(node);
                }
            }

            return startNodes;
        }

#if DISCOVER_PROXY
    /// <summary>
    /// 
    /// </summary>
    /// <param name="kc">The object to start with and step up its ancestry until we reach a non-proxy point.</param>
    /// <returns>The final object we promoted the starting object to effectively be.</returns>
    private SfcKeyChain PromoteProxy(SfcKeyChain kc)
    {
        while (kc != null && kc.TypeDescriptor.Parent.Archetype == SfcTypeRelation.RequiredChild)
        {
            // We have to bump up to the closest non-proxy Ancestor, performing discovery
            // as we go on the individual objects in between otherwise we would have to remember who we used to be
            // throughout the Engine run.
            DepNode node;
            if (nodeDict.TryGetValue(kc, out node) && !node.Discovered)
            {
                // Mark the node discovered so we don't do it twice from different angles.
                // Also mark it asap, so it doesn't get into recursion issues while doing its own Discover()
                node.Discovered = true;
                kc.Object.Discover(this);
            }
            kc = kc.Parent;
        }

        return kc;
    }
#endif // DISCOVER_PROXY
        #endregion

        #region Add graph nodes

        /// <summary>
        /// Add a new tuple relation of a parent node and its child node which depends on it.
        /// A node can be added multiple times as a parent, child or single node in successive calls.
        /// </summary>
        /// <param name="objParent">The parent node of the dependent child node.</param>
        /// <param name="objChild">The child node dependent on the existence of the parent node.</param>
        /// <param name="relation"></param>
        public void Add(SfcInstance objParent, SfcInstance objChild, SfcTypeRelation relation)
        {
            if (objParent == null || objChild == null)
            {
                return;
            }

#if DISCOVER_PROXY
        // Proxy check for promotion of the parent in the tuple
        kcParent = PromoteProxy(kcParent);

        // Proxy check for promotion of the child in the tuple
        kcChild =  PromoteProxy(kcChild);
        }
#endif // DISCOVER_PROXY

            // Make sure parent node in graph
            DepNode nodeParent;
            if (!nodeDict.TryGetValue(objParent.KeyChain, out nodeParent))
            {
                nodeParent = new DepNode(objParent);
                nodeDict.Add(nodeParent);
                // Only enqueue for processing if it isn't the current node in the client sink.
                if (m_currentNode == null || m_currentNode.Keychain != objParent.KeyChain)
                {
                    nodeQueue.Enqueue(objParent.KeyChain);
                }
            }

            // Make sure child node in graph
            DepNode nodeChild;
            if (!nodeDict.TryGetValue(objChild.KeyChain, out nodeChild))
            {
                nodeChild = new DepNode(objChild);
                nodeDict.Add(nodeChild);
                // Only enqueue for processing if it isn't the current node in the client sink.
                if (m_currentNode == null || m_currentNode.Keychain != objChild.KeyChain)
                {
                    nodeQueue.Enqueue(objChild.KeyChain);
                }
            }

            // Track Parent-Child physical relations for eventual use in Tree enumeration of results
            bool isPhysicalRelation = (relation == SfcTypeRelation.ContainedChild || relation == SfcTypeRelation.RequiredChild);

            // Only add the subordinating cross-references to each other if not a Weak Reference relation
            // (i. e., break the directional arrow right now by never storing it).
            // This does mean you cannot auto-Remove related nodes already disassociated in this way.
            if (relation != SfcTypeRelation.WeakReference)
            {
                nodeChild.AddAncestor(nodeParent, isPhysicalRelation);
                nodeParent.AddChild(nodeChild, isPhysicalRelation);
            }

        }

        /// <summary>
        /// Add a new single object node with no directional relation to either a parent or child node.
        /// If the node already exists in ther graph any existing relations will not be disturbed
        /// since Add() always only augments but never detracts.
        /// A node can be added multiple times as a parent, child or single node in successive calls.
        /// </summary>
        /// <param name="obj">The single node.</param>
        public void Add(SfcInstance obj)
        {
            DepNode node;

            // Make sure orphan node is in graph
            if (!nodeDict.TryGetValue(obj.KeyChain, out node))
            {
                node = new DepNode(obj);
                nodeDict.Add(node);
                // Only enqueue for processing if it isn't the current node in the client sink.
                if (m_currentNode == null || m_currentNode.Keychain != obj.KeyChain)
                {
                    nodeQueue.Enqueue(obj.KeyChain);
                }
            }
        }
        #endregion

        /// <summary>
        /// Perform complete dependency relation discovery. Start with asking each DomainContext to do what it can, then each DomainContext Type
        /// and finally resort to asking each node about its own relationships.
        /// 
        /// Currently we only ask for bulk per DomainContext once, then simply do individual discovery until all nodes have had a discovery pass.
        /// 
        /// For v2, we would introduce bulk DomainContext Type discovery after doing the DomainContext discovery, and maybe even repeat bulk
        /// discovery until no new nodes are contributed from it, then resort to individual discovery.
        /// 
        /// Also we may need to give bulk handlers an iterator, unless we want to rely on handing them acess to the DepEngine
        /// itself as a sink for adding new nodes discovered as well as traversing directly on the nodes. This is okay as long as we assume (or know)
        /// that the bulk handlers themselves just make a temp collection to hold the nodes before making the query. They have to be done walking the graph
        /// before they start adding nodes. Just like any other iterator use, if they try to violate this order and add nodes before they are finished Next'ing
        /// on the iterator, the iterator will throw (which is what we want to happen).
        /// </summary>
        public void Discover()
        {
            // Add the roots into the graph as orphans.
            foreach (SfcInstance obj in m_DependencyRootList)
            {
                this.Add(obj);
            }

            // Hand a domain a sink with a copy of the root list for bulk discovery

            // Determine how many different domain instances (ISfcDomains) we are dealing with in our node dictionary
            // and gather all of our undiscovered nodes into clusters around each different domain instance.
            Dictionary<ISfcDomain, List<SfcDependencyNode>> domainDict = new Dictionary<ISfcDomain, List<SfcDependencyNode>>();
            List<SfcDependencyNode> nodeList = null;
            ISfcDomain prevRoot = null;
            foreach (DepNode node in nodeDict)
            {
                // Reuse the last node list found if we have the same root again
                if (nodeList == null || prevRoot != node.Keychain.Domain)
                {
                    if (!domainDict.TryGetValue(node.Keychain.Domain, out nodeList))
                    {
                        nodeList = new List<SfcDependencyNode>();
                        domainDict.Add(node.Keychain.Domain, nodeList);
                    }
                    prevRoot = node.Keychain.Domain;
                }

                // Add to the proper domain instance node list
                nodeList.Add(new SfcDependencyNode(node));
            }

            // Perform individual discovery on every node not already marked as done
            while (nodeQueue.Count > 0)
            {
                SfcKeyChain kc = nodeQueue.Dequeue();
                DepNode node;

                // Skip nodes that are no longer in the nodedict (meaning they have been removed since being enqueued)
                if (!nodeDict.TryGetValue(kc, out node))
                {
                    continue;
                }

                // Skip nodes already discovered. This may be a duplicate in the queue, or just one that has been processed since enqueueing.
                if (node.Discovered)
                {
                    continue;
                }

                // TODO: In v1, we will always have the object already
                SfcInstance obj = node.Instance;
                if (obj != null)
                {
                    ISfcDiscoverObject objDiscovery = obj as ISfcDiscoverObject;
                    if (objDiscovery != null)
                    {
                        // Set the currentNode for use by the sink methods sicne they need to know who "this" is
                        m_currentNode = node;
                        objDiscovery.Discover((ISfcDependencyDiscoveryObjectSink)this);
                        m_currentNode = null;

                        // We mark individual discoveries since we don't allow them to return without considering them done
                        node.Discovered = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            nodeDict.Clear();
            nodeQueue.Clear();
            m_DependencyRootList.Clear();
            m_currentNode = null;
        }

        #region ISfcDependencyDiscoveryObjectSink Members

        void ISfcDependencyDiscoveryObjectSink.Add(SfcDependencyDirection direction, SfcInstance targetObject, SfcTypeRelation relation, bool discovered)
        {
            if (direction == SfcDependencyDirection.Inbound)
            {
                // The calling object acts as the parent
                this.Add(m_currentNode.Instance, targetObject, relation);
            }
            else
            {
                // The calling object acts as the child
                this.Add(targetObject, m_currentNode.Instance, relation);
            }
            if (discovered)
            {
                this.nodeDict[targetObject.KeyChain].Discovered = true;
            }
        }

        void ISfcDependencyDiscoveryObjectSink.Add(SfcDependencyDirection direction, IEnumerator targetObjects, SfcTypeRelation relation, bool discovered)
        {
            while (targetObjects.MoveNext())
            {
                SfcInstance obj = (SfcInstance)targetObjects.Current; // this cast cannot fail, since we only deal with SFC objects
                ((ISfcDependencyDiscoveryObjectSink)this).Add(direction, obj, relation, discovered);
            }
        }

        void ISfcDependencyDiscoveryObjectSink.Add<T>(SfcDependencyDirection direction, IEnumerable<T> targetObjects, SfcTypeRelation relation, bool discovered)
        {
            foreach (T targetObject in targetObjects)
            {
                ((ISfcDependencyDiscoveryObjectSink)this).Add(direction, targetObject, relation, discovered);
            }
        }

        #endregion

        /// <summary>
        /// Return a DependencyListEnumerator which can be used to walk the graph as a list by the client.
        /// </summary>
        /// <returns></returns>
        public DependencyListEnumerator GetListEnumerator()
        {
            return new DependencyListEnumerator(this);
        }

        /// <summary>
        /// Return a DependencyTreeEnumerator which can be used to walk the graph tree by the client.
        /// </summary>
        /// <returns></returns>
        public DependencyTreeEnumerator GetTreeEnumerator()
        {
            return new DependencyTreeEnumerator(this);
        }

    }

    #endregion // SfcDependencyEngine class

}

