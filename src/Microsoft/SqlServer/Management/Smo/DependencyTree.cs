// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// DependencyTreeNode class represents an individual entry in the dependency tree
    /// </summary>
    public class DependencyTreeNode : DependencyNode
	{

		internal protected DependencyTreeNode()
		{
		}

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal DependencyTreeNode(int index, DependencyChainCollection siblings)
		{
			// error checking
			if( 0 > index )
			{
				throw new ArgumentOutOfRangeException("index");
			}

			if( 0 >= siblings.Count )
			{
				throw new FailedOperationException( ExceptionTemplates.EmptyInputParam("siblings", "DependencyChainCollection")).SetHelpContext("EmptyInputParam");
			}

			this.Index = index;
			this.Siblings = siblings;

			Dependency dep = this.Siblings[this.Index];
            this.Urn = dep.Urn;
            this.IsSchemaBound = dep.IsSchemaBound;
        }

		private int index;
        internal protected int Index
        {
            get
            {
                return index;
            }
            set
            {
                index = value;
            }
        }

        private DependencyChainCollection siblings;
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal protected DependencyChainCollection Siblings
        {
            get
            {
                return siblings;
            }
            set
            {
                siblings = value;
            }
        }

        public virtual int NumberOfSiblings
		{
			get
			{
				return this.Siblings.Count;
			}
		}

		public virtual bool HasChildNodes
		{
			get
			{
				Dependency dep = this.Siblings[this.Index];
				return (dep.Links.Count > 0);
			}
		}

		public virtual DependencyTreeNode FirstChild
		{
			get
			{
				DependencyTreeNode depNode = null;

				// get yourself from a list of siblings first
				Dependency dep = this.Siblings[this.Index];
				if( dep.Links.Count > 0 )
				{
					// create wrapper for a child dependency
					depNode = new DependencyTreeNode( 0, dep.Links );
				}

				return depNode;
			}
		}

		public virtual DependencyTreeNode NextSibling
		{
			get
			{
				DependencyTreeNode depNode = null;

				if( this.Siblings.Count > this.Index + 1 )
				{
					// create wrapper for a sibling dependency
					depNode = new DependencyTreeNode( this.Index+1, this.Siblings );
				}

				return depNode;
			}
		}
	}

	/// <summary>
	/// Dependency tree returned as a result of dependency discovery phase
	/// </summary>
	public class DependencyTree : DependencyTreeNode
	{
        private DependencyChainCollection dependencies;
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal protected DependencyChainCollection Dependencies
        {
            get
            {
                return dependencies;
            }
            set
            {
                dependencies = value;
            }
        }

        private DependencyChainCollection roots;
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal protected DependencyChainCollection Roots
        {
            get
            {
                return roots;
            }
            set
            {
                roots = value;
            }
        }

        private bool dependsOnParents;
        internal protected bool DependsOnParents
        {
            get
            {
                return dependsOnParents;
            }
            set
            {
                dependsOnParents = value;
            }
        }

        private Server server;

		internal DependencyTree( Urn [] urns, DependencyChainCollection dependencies, bool fParents, Server server)
		{
			// error checking
			if( 0 >= urns.Length )
			{
				throw new FailedOperationException( ExceptionTemplates.EmptyInputParam("urns", "Urn[]")).SetHelpContext("EmptyInputParam");
			}

			this.server = server;
			
			this.Dependencies = dependencies;
			this.DependsOnParents = fParents;
            this.IsSchemaBound = true;      // The root has schema-bound dependency on itself

			// create root dependencies collection using supplied Urns
			this.Roots = new DependencyChainCollection();

			ArrayList deplist = new ArrayList(this.Dependencies.Count);
			for( int listIdx = 0; listIdx < this.Dependencies.Count; listIdx++ )
			{
				deplist.Add(this.Dependencies[listIdx]);
			}
			deplist.Sort( new DependencyComparer(this.server));
			
			SortedList slDeps = new SortedList( new UrnComparer(this.server));
			for( int depIdx = 0; depIdx < this.Dependencies.Count; depIdx++ )
			{
				// slDeps[(deplist[depIdx] as Dependency).Urn] = deplist[depIdx];
				slDeps.Add( (deplist[depIdx] as Dependency).Urn, deplist[depIdx] );
				
			}


			for( int i=0; i<urns.Length; i++ )
			{
				Dependency dep = slDeps[urns[i]] as Dependency;
				if( null != dep )
                {
                    this.Roots.Add(dep);
                }

                if ( this.Roots.Count <= i ) // sanity check
				{
					// if the Urn is missing from the depencency list, let's check if it's a system object
					SqlSmoObject sysobj = this.server.GetSmoObject(urns[i]);
					if( sysobj.Properties.Contains("IsSystemObject") && 
						 sysobj.Properties["IsSystemObject"].Value != null &&
						 (bool)sysobj.Properties["IsSystemObject"].Value )
					{
						throw new FailedOperationException(ExceptionTemplates.NoDepForSysObjects(urns[i].ToString())).SetHelpContext("NoDepForSysObjects");
					}
					else
					{
						throw new FailedOperationException(ExceptionTemplates.UrnMissing(urns[i].ToString())).SetHelpContext("UrnMissing");
					}
				}
			}
		}

		public DependencyTree( DependencyTree tree )
		{
			this.Dependencies = new DependencyChainCollection( tree.Dependencies );
			this.Roots = new DependencyChainCollection( tree.Roots );
			this.DependsOnParents = tree.DependsOnParents;
		}

		public override int NumberOfSiblings
		{
			get{ return 1; }
		}

		public int Count
		{
			get
			{
				return Dependencies.Count;
			}
		}

		public DependencyTree Copy()
		{
			return new DependencyTree(this);
		}

		public override Urn Urn
		{
			get
			{
				return null;
			}
		}

		public override bool HasChildNodes
		{
			get
			{
				return true;
			}
		}

		public override DependencyTreeNode FirstChild
		{
			get
			{
				return new DependencyTreeNode( 0, this.Roots );
			}
		}

		public override DependencyTreeNode NextSibling
		{
			get
			{
				return null;
			}
		}

		public void Remove( DependencyTreeNode depNode )
		{
			int i, j;
			Dependency dep;

			// remove node from the depencencies list and any links pointing to it
			for( i=0; i < Dependencies.Count; i++ )
			{
				dep = this.Dependencies[i];

				if( depNode.Urn == dep.Urn ) // this is the node we are trying to kill, remove it
				{
					this.Dependencies.RemoveAt(i);

					// count now needs to be decremented by one
					i--; 
				}
				else // go through all the links and kill any references to the node being removed
				{
					for( j=0; j<dep.Links.Count; j++ )
					{
						if( depNode.Urn == dep.Links[j].Urn ) // link to our node, remove it
						{
							dep.Links.RemoveAt(j);
							break;
						}
					}
				}
			}

			// also remove it from the roots
			for( i=0; i < this.Roots.Count; i++ )
			{
				dep = this.Roots[i];

				if( depNode.Urn == dep.Urn ) // this is the node we are trying to kill, remove it
				{
					this.Roots.RemoveAt(i);
					break;
				}
			}
		}
	}

	internal class UrnComparer : IComparer
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="collation">SQL Server collation name</param>
		internal UrnComparer( Server srv)
		{
			this.server = srv;
		}

		/// <summary>
		/// The IComparer implementation
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public int Compare(  object x,  object y)
		{
			if( null == x && null == y )
			{
				return 0;
			}
			else if ( null != x && null == y ) 
			{
				return 1;
			}
			else if( null == x && null != y ) 
			{
				return -1;
			}
			else
			{
				return server.CompareUrn((Urn)x, (Urn)y);
			}
		}

		Server server;

	}

	internal class DependencyComparer : IComparer
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="collation">SQL Server collation name</param>
		internal DependencyComparer( Server srv)
		{
			this.server = srv;
		}

		/// <summary>
		/// The IComparer implementation
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public int Compare(  object x,  object y)
		{
			if( null == x && null == y )
			{
				return 0;
			}
			else if ( null != x && null == y ) 
			{
				return 1;
			}
			else if( null == x && null != y ) 
			{
				return -1;
			}
			else
			{
				Dependency depx = (Dependency)x;
				Dependency depy = (Dependency)y;
				return server.CompareUrn(depx.Urn, depy.Urn);
			}
		}

		Server server;

	}

	

}

