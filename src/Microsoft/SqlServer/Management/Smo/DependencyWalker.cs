// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Server;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // attribute this as being deprecated
    //public delegate void ProgressReportDelegate(Urn Current, Urn Parent, int SubTotalCount, int SubTotal, int TotalCount, int Total);

    public delegate void ProgressReportEventHandler(object sender, ProgressReportEventArgs e );

public delegate bool ScriptingFilter( Urn urn );

/// <summary>
/// Instance class encapsulating SQL Server database
/// </summary>
	public class DependencyWalker
	{
		public DependencyWalker()
		{
			this.server = null;
		}

		public DependencyWalker( Server server)
		{
			if( null == server )
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("server"));
            }

            this.Server = server;
		}

		private Server server;
		public Server Server
		{
			get 
			{
				return server;
			}
			
			set 
			{
				if( null == value )
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Server"));
                }

                server = value;
			}
		}

	    private DependencyTree tree;
        internal protected DependencyTree DependencyTree
        {
            get
            {
                return tree;
            }

            set
            {
                tree = value;
            }
        }

        private int totalCount;
        internal protected int TotalCount
        {
            get
            {
                return totalCount;
            }

            set
            {
                totalCount = value;
            }
        }

        private int total;
        internal protected int Total
        {
            get
            {
                return total;
            }

            set
            {
                total = value;
            }
        }

        private Hashtable knownObjectsList;
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal protected Hashtable KnownObjectsList
        {
            get
            {
                return knownObjectsList;
            }
            set
            {
                knownObjectsList = value;
            }
        }

        protected Server GetServerObject()
        {
			if( null == this.Server )
            {
                throw new PropertyNotSetException("Server");
            }

            return this.Server;
		}

		// attribute as deprecated
		//public event ProgressReportDelegate ProgressEvent;

		// this is the new event 
        private ProgressReportEventHandler discoveryProgress;
        public event ProgressReportEventHandler DiscoveryProgress
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                discoveryProgress += value;
            }
            remove
            {
                discoveryProgress -= value;
            }
        }

        private ScriptingFilter filterCallbackFunction;
        public ScriptingFilter FilterCallbackFunction
		{
			get 
            {
                return  filterCallbackFunction;
            }
            set
            {
                filterCallbackFunction = value;
            }
        }

		public DependencyTree DiscoverDependencies( UrnCollection list, DependencyType dependencyType)
		{
			return DiscoverDependencies(list, dependencyType == DependencyType.Parents ? true :false);
		}

		public DependencyTree DiscoverDependencies( UrnCollection list, bool parents )
		{
			Urn [] urns = new Urn[ list.Count ];

			for( int i=0; i<list.Count; i++ )
			{
				urns[i] = list[i];
			}
			return DiscoverDependencies( urns, parents );
		}

		public DependencyTree DiscoverDependencies( SqlSmoObject [] objects, DependencyType dependencyType)
		{
			return DiscoverDependencies(objects, dependencyType == DependencyType.Parents ? true :false);
		}

		public DependencyTree DiscoverDependencies( SqlSmoObject [] objects, bool parents )
		{
			Urn [] urns = new Urn[ objects.Length ];

			for( int i=0; i<objects.Length; i++ )
			{
                SqlSmoObject current = objects[i];
                if (current.Properties.Contains("ID"))
                {
                    // if the object has ID we should try to pass it to the enumerator
                    // because the dependency discovery works with IDs and we would 
                    // have to issue a query to retrieve it.
                    Property id = current.Properties.Get("ID");
                    if (null != id.Value)
                    {
                        urns[i] = current.UrnWithId;
                    }
                    else
                    {
                        urns[i] = current.Urn;
                    }
                }
                else
                {
                    urns[i] = current.Urn;
                }
            }
			return DiscoverDependencies( urns, parents );
		}

		public DependencyTree DiscoverDependencies( Urn [] urns, DependencyType dependencyType)
		{
			return DiscoverDependencies(urns, dependencyType == DependencyType.Parents ? true :false);
		}

		public DependencyTree DiscoverDependencies( Urn [] urns, bool parents )
		{
			try
			{
				// assure all dependency root urn's are fixed
				for( int i=0; i<urns.Length; i++ )
				{
					SqlSmoObject obj = GetServerObject().GetSmoObject( urns[i] );
					urns[i] = obj.Urn;
					string sServerName = urns[i].GetNameForType("Server");

					// after we have transformed the object URN, we are checking if they are 
					// on the same server
					if( 0 != NetCoreHelpers.StringCompare(sServerName, GetServerObject().ExecutionManager.TrueServerName, 
											true, SmoApplication.DefaultCulture ) )
					{
						throw new ArgumentException(ExceptionTemplates.MismatchingServerName(GetServerObject().ExecutionManager.TrueServerName, sServerName));
					}
				}

				DependencyRequest rd = new DependencyRequest();
				rd.Urns = urns;
				rd.ParentDependencies = parents;

				DependencyChainCollection deps = GetServerObject().ExecutionManager.GetDependencies(rd);

				return new DependencyTree( urns, deps, parents, GetServerObject() );
			}				
			catch(Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.DiscoverDependencies, this, e);
			}
		}

		private bool ObjectEncounteredBefore( Urn newUrn )
		{
			return this.KnownObjectsList.ContainsKey(newUrn);
		}

		private void WalkDependentChildren( DependencyTree tree, DependencyTreeNode depParent, DependencyCollection depList )
		{
			int iSubTotalCount = 0;

			bool fRootNode;
			DependencyTreeNode rootNode;
			DependencyTreeNode depChild = depParent.FirstChild;
			while( null != depChild )
			{
				if( !ObjectEncounteredBefore( depChild.Urn ) )
				{
					// add object to known objects list
					this.KnownObjectsList[depChild.Urn] = depChild.Urn;

					// first assume it's not a root node, then verify
					fRootNode = false;
					rootNode = tree.FirstChild;
					while( null != rootNode )
					{
						if( rootNode.Urn == depChild.Urn ) // it's a root node after all
						{
							fRootNode = true;
							break;
						}
						rootNode = rootNode.NextSibling;
					}

					// check if user wants to filter out this object
					bool fApplyFilter = false;
                    if (this.FilterCallbackFunction != null)
                    {
                        fApplyFilter = this.FilterCallbackFunction(depChild.Urn);
                    }

					if( fApplyFilter )
					{
						this.Total--;
					}
					else // don't filter out the object
					{
                        if (null != discoveryProgress)
						{
							// increment object counters
							iSubTotalCount++;
							this.TotalCount++;

							// fire an event
                            discoveryProgress(this, new ProgressReportEventArgs(depChild.Urn, 
											fRootNode? depChild.Urn:depParent.Urn, 
                                            depChild.IsSchemaBound, 
											iSubTotalCount, 
											depChild.NumberOfSiblings, 
											this.TotalCount, 
											this.Total ));
						}

						if( depChild.HasChildNodes ) // walk all dependents of this object
						{
							WalkDependentChildren( tree, depChild, depList );
						}

						// add itself to a list of dependencies
						depList.Add( new DependencyCollectionNode( depChild.Urn, depChild.IsSchemaBound, fRootNode ) );
					}
				}
                else if (null != discoveryProgress) 
				{
					// object was already scripted so simply implement the counter
					iSubTotalCount++;
				}
				depChild = depChild.NextSibling;
			}
		}

		public DependencyCollection WalkDependencies( DependencyTree tree )
		{
			// set the following member variables for the duration of the walk
            this.DependencyTree = tree;
            this.TotalCount = 0;
			this.Total = tree.Count;
			this.KnownObjectsList = new Hashtable();

			// create empty dependency list object
			DependencyCollection depList = new DependencyCollection();

			// start recursively building dependency list
			WalkDependentChildren( tree, (DependencyTreeNode)tree, depList );

			// reset member variables for the next run
            this.DependencyTree = null;
            this.TotalCount = 0;
			this.Total = 0;
			this.KnownObjectsList = null;

			return depList;
		}
	}

	public class ProgressReportEventArgs : EventArgs
	{
		public ProgressReportEventArgs(Urn current, Urn parent, int subTotalCount, int subTotal, int totalCount, int total)
		{
			this.current = current;
			this.parent = parent;
            this.schemaBound = false;
			this.subTotalCount = subTotalCount;
			this.subTotal = subTotal;
			this.totalCount = totalCount;
			this.total = total;
		}

        public ProgressReportEventArgs(Urn current, Urn parent, bool isSchemaBound, int subTotalCount, int subTotal, int totalCount, int total)
        {
            this.current = current;
            this.parent = parent;
            this.schemaBound = isSchemaBound;
            this.subTotalCount = subTotalCount;
            this.subTotal = subTotal;
            this.totalCount = totalCount;
            this.total = total;
        }

        bool schemaBound;
        public bool IsSchemaBound { get { return schemaBound; } }

		Urn current;
		public Urn Current 	{get	{return current;}	}
		
		Urn parent;
		public Urn Parent 	{ get { return parent;}}
		
		int subTotalCount;
		public int SubTotalCount { get { return subTotalCount;}}
		
		int subTotal;
		public int SubTotal { get { return subTotal;} }
		
		int totalCount;
		public int TotalCount { get { return totalCount; }}
		
		int total;
		public int Total { get { return total;} }
	}

}

