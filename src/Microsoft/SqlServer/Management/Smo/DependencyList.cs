// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// DependencyNode is a base node class
    /// Contains the urn of the node and the dependency type (SchemaBound/NonSchemaBound) the node has with its parent
    /// </summary>
    public class DependencyNode
	{
		internal protected DependencyNode()
		{
		}

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal DependencyNode(Urn urn, bool isSchemaBound)
		{
			this.Urn = urn;
            this.schemaBound = isSchemaBound;
		}

		private Urn urn;
		public virtual Urn Urn
		{
			get
			{
				return urn;
			}
            set
            {
                urn = value;
            }
        }

        private bool schemaBound;
        public virtual bool IsSchemaBound
        {
            get
            {
                return schemaBound;
            }
            set
            {
                schemaBound = value;
            }
        }
	}

	/// <summary>
	/// DependencyCollectionNode class represents an individual entry in the dependency list
	/// </summary>
	public class DependencyCollectionNode : DependencyNode
	{
        private bool isRootNode;

        /// <summary>
        /// Creates a new node.
        /// </summary>
        /// <param name="urn"></param>
        /// <param name="isSchemaBound"></param>
        /// <param name="fRoot">True if this is a root node.</param>
        internal DependencyCollectionNode( Urn urn, bool isSchemaBound, bool fRoot ) : base (urn, isSchemaBound)
		{
            this.IsRootNode = fRoot;
        }

		public bool IsRootNode
		{
            get { return isRootNode; }
            set { isRootNode = value; }
        }

        /// <summary>
        /// Flag that indicates if the object can be created/dropped as part of a user transaction.
        /// </summary>
		internal bool Transactable
		{
			get{
                switch(Urn.Type)
                {
                    case "Login" :
                    case "User" :
                    case "ApplicationRole" :
                    case "Role" :
                    case "Endpoint":
                    case "FullTextCatalog":
                    case "FullTextStopList":
                    case "SearchPropertyList":
                        return false;
                }

                return true;
            }
		}
	}

	/// <summary>
	/// Dependency tree returned as a result of dependency discovery phase
	/// </summary>
    public class DependencyCollection : IList<DependencyCollectionNode>
	{
        public bool ContainsUrn(Urn urn, Server srv)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (0 == srv.CompareUrn(urn, this[i].Urn))
                {
                    return true;
                }
            }
            return false;
        }

        List<DependencyCollectionNode> innerColl = null;

        #region ctors
        public DependencyCollection()
        {
            innerColl = new List<DependencyCollectionNode>();
        }
        #endregion

        #region IList implementation
        public int IndexOf(DependencyCollectionNode dependencyCollectionNode)
        {
            return innerColl.IndexOf(dependencyCollectionNode);
        }

        public void Insert(int index, DependencyCollectionNode dependencyCollectionNode)
        {
            innerColl.Insert(index, dependencyCollectionNode);
        }

        public void RemoveAt(int index)
        {
            innerColl.RemoveAt(index);
        }

        public DependencyCollectionNode this[int index] 
        { 
            get
            {
                return innerColl[index];
            }
            set
            {
                innerColl[index] = value;
            }
        }

        public void Add(DependencyCollectionNode dependencyCollectionNode)
        {
            innerColl.Add(dependencyCollectionNode);
        }

        public void AddRange( IEnumerable<DependencyCollectionNode> dependencyCollectionNodeCollection)
        {
            innerColl.AddRange(dependencyCollectionNodeCollection);
        }

        public void Clear()
        {
            innerColl.Clear();
        }
        public bool Contains(DependencyCollectionNode dependencyCollectionNode)
        {
            return innerColl.Contains(dependencyCollectionNode);
        }

        public void CopyTo(DependencyCollectionNode[] array, int arrayIndex)
        {
            innerColl.CopyTo(array, arrayIndex);
        }

        public bool Remove(DependencyCollectionNode dependencyCollectionNode)
        {
            return innerColl.Remove(dependencyCollectionNode);
        }

        public int Count
        {
            get
            {
                return innerColl.Count;
            }
        }

        bool ICollection<DependencyCollectionNode>.IsReadOnly
        {
            get
            {
                return ((ICollection<DependencyCollectionNode>)innerColl).IsReadOnly;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)innerColl).GetEnumerator();
        }

        public IEnumerator<DependencyCollectionNode> GetEnumerator()
        {
            return innerColl.GetEnumerator();
        }
        #endregion

	}
}

