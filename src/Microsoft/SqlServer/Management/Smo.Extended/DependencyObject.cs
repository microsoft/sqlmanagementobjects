// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Smo
{
    //
    // Simple dependency graph orderer based on being fed parent-child tuples of SqlSmoObjects.
    // It is assumed that these are fabricated based on local knowledge the caller has in asking for these relations to be preserved in the
    // final ordered traversal.
    //
    // Someday, it would be nice if each Smo object itself knew how what tuples it needs to represent its local state relations to fee this.
    //

    internal class DependencyObject
    {
        public DependencyObject()
        {
        }

        public void AddAncestor(SqlSmoObject obj)
        {
            if (Ancestors == null)
            {
                Ancestors = new List<SqlSmoObject>();
            }
            Ancestors.Add(obj);
        }

        public void AddChild(SqlSmoObject obj)
        {
            if (Children == null)
            {
                Children = new List<SqlSmoObject>();
            }
            Children.Add(obj);
        }

        private bool visited = false;
        public bool Visited
        {
            get
            {
                return visited;
            }
            set
            {
                visited = value;
            }
        }

        /// <summary>
        /// List of ancestor SqlSmoObjects
        /// </summary>
        private List<SqlSmoObject> ancestors = null;
        public List<SqlSmoObject> Ancestors
        {
            get
            {
                return ancestors;
            }
            set
            {
                ancestors = value;
            }
        }

        /// <summary>
        /// List of children SqlSmoObjects
        /// </summary>
        private List<SqlSmoObject> children = null;
        public List<SqlSmoObject> Children
        {
            get
            {
                return children;
            }
            set
            {
                children = value;
            }
        }
    }

    internal class DependencyObjects
    {
        /// <summary>
        /// Map each node to the list of ancestor (parent) and dependent (child) nodes which must exist before self can
        /// </summary>
        private Dictionary<SqlSmoObject, DependencyObject> nodeDict = new Dictionary<SqlSmoObject, DependencyObject>();
        /// <summary>
        /// The dependency list in order
        /// </summary>
        private List<SqlSmoObject> dependencyList = new List<SqlSmoObject>();

        /// <summary>
        /// Constructor
        /// </summary>
        public DependencyObjects()
        {
        }

        /// <summary>
        /// Add a new tuple relation of a parent node and its child node which depends on it.
        /// A node can be added multiple times as a parent, child or single node in successive calls.
        /// </summary>
        /// <param name="node">The parent node of the dependent child node.</param>
        /// <param name="dependent">The child node dependent on the existence of the parent node.</param>
        public void Add(SqlSmoObject node, SqlSmoObject dependent)
        {
            DependencyObject dobj;

            // Add relations in both directions

            if (!nodeDict.TryGetValue(node, out dobj))
            {
                dobj = new DependencyObject();
                nodeDict.Add(node, dobj);
            }
            dobj.AddChild(dependent);

            if (!nodeDict.TryGetValue(dependent, out dobj))
            {
                dobj = new DependencyObject();
                nodeDict.Add(dependent, dobj);
            }
            dobj.AddAncestor(node);
        }

        /// <summary>
        /// Add a new single node with no relation to either a parent or child node.
        /// A node can be added multiple times as a parent, child or single node in successive calls.
        /// </summary>
        /// <param name="node">The single node.</param>
        public void Add(SqlSmoObject node)
        {
            DependencyObject dobj;

            // Add the single node with no parent or child relations added in. If it already exists do nothing.
            if (!nodeDict.TryGetValue(node, out dobj))
            {
                dobj = new DependencyObject();
                nodeDict.Add(node, dobj);
            }
        }

        /// <summary>
        /// Clear the internal state to be reused.
        /// </summary>
        public void Clear()
        {
            nodeDict.Clear();
            dependencyList.Clear();
        }

        /// <summary>
        /// Return the list of dependencies such that each node is guaranteed to depend on no more than the preceding nodes (if any).
        /// The ancestor and child lists could be returned with each node but it currently isn't needed.
        /// This can be called as many times as you want with node tuples being added as well. If you want to start over completely, call Clear().
        /// </summary>
        /// <returns>The List of SqlSmoObjects in dependency order.</returns>
        public List<SqlSmoObject> GetDependencies()
        {
            for (SqlSmoObject root = StartNode(); root != null; root = StartNode())
            {
                VisitNode(root);
            }
            dependencyList.Reverse();
            return dependencyList;
        }

        /// <summary>
        /// Get a valid starting node for a dependency descent until there are no more valid candidates (signifying we are done).
        /// </summary>
        /// <returns>A valid SqlSmoObject, or null if no more valid root nodes are left to process.</returns>
        private SqlSmoObject StartNode()
        {
            // The only nodes we are interested in are ones that are parents but not children of any other node
            foreach (KeyValuePair<SqlSmoObject, DependencyObject> kvp in nodeDict)
            {
                if (kvp.Value.Visited)
                {
                    continue;
                }

                // If this node is not anyone's child, it's a root.
                // This includes single nodes with no relation to either parents or children.
                if (kvp.Value.Ancestors == null || kvp.Value.Ancestors.Count == 0)
                {
                    return kvp.Key;
                }
            }

            // No more root nodes to descend from
            return null;
        }

        private void VisitNode(SqlSmoObject node)
        {
            DependencyObject dobj = nodeDict[node];

            if (dobj.Visited)
            {
                return;
            }

            dobj.Visited = true;

            if (dobj.Ancestors != null)
            {
                foreach (SqlSmoObject ancestor in dobj.Ancestors)
                {
                    VisitNode(ancestor);
                }
            }

            dependencyList.Add(node);

            if (dobj.Children != null)
            {
                foreach (SqlSmoObject child in dobj.Children)
                {
                    VisitNode(child);
                }
            }

            return;
        }
    }
}

