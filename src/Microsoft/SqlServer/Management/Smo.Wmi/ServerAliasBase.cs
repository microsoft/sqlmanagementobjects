using System;
using System.Management;

using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	public partial class ServerAlias : WmiSmoObject, Cmn.ICreatable, Cmn.IDroppable
	{
		internal ServerAlias(WmiCollectionBase parentColl, string name) : 
			base(parentColl, name)
		{
		}

		public ServerAlias(ManagedComputer managedComputer, string name) : 
			base()
		{
			this.Parent = managedComputer;
			this.SetName(name);
		}

		public ServerAlias() : base()
		{
		}

		// returns the name of the type in the urn expression
		internal static string UrnSuffix
		{
			get 
			{
				return "ServerAlias";
			}
		}

		public void Create()
		{
			try
			{
				CreateImplWorker();

				// generate internal events
                if (!SmoApplication.eventsSingleton.IsNullObjectCreated())
				{
                    SmoApplication.eventsSingleton.CallObjectCreated(GetManagedComputer(), 
						new ObjectCreatedEventArgs(this.Urn, this));
				}

			}
			catch( Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.Create, this, e);
			}
		}

		private void CreateImplWorker()
		{
			CheckObjectState();
			if ( State != SqlSmoState.Creating )
			{
				throw new SmoException(ExceptionTemplates.ObjectAlreadyExists(UrnSuffix, this.Name));
			}
			
			ManagementClass mcls = new ManagementClass( GetManagedComputer().ManagementScope,
														new ManagementPath("SqlServerAlias"), new ObjectGetOptions() );
			// create an instance of the Alias class
			ManagementObject mo = mcls.CreateInstance();

			// set its properties
			mo["AliasName"] = this.Name;

			SetMOProp( mo, "ServerName", "ServerName" );
			SetMOProp( mo, "ProtocolName", "ProtocolName" );
			SetMOProp( mo, "ConnectionString", "ConnectionString" );

			// commit the instance
			mo.Put();

			// mark the object as existing
			this.SetState(SqlSmoState.Existing );

			// add the alias to the parent collection
			this.ParentColl.AddInternal(this);
		}

		// sets the properties, and throws if the property is missing 
		private void SetMOProp( ManagementObject mo, string moPropName, string propName )
		{
			Property p = Properties.Get( propName );

			if( null != p.Value )
			{
				mo[moPropName] = p.Value;
			}
			else
			{
				throw new PropertyNotSetException(propName);
			}
		}
		public void Drop()
		{
			try
			{
				DropImplWorker();

				// generate internal events
                if (!SmoApplication.eventsSingleton.IsNullObjectDropped())
				{
                    SmoApplication.eventsSingleton.CallObjectDropped(GetManagedComputer(), 
						new ObjectDroppedEventArgs(this.Urn));
				}

			}
			catch( Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.Drop, this, e);
			}
		}

		private void DropImplWorker()
		{
			CheckObjectState();
			
			ManagementScope ms = GetManagedComputer().ManagementScope;
			ManagementPath mp = new ManagementPath(string.Format(SmoApplication.DefaultCulture, "SqlServerAlias.AliasName=\"{0}\"", Name));
			ManagementObject mo = new ManagementObject( ms, mp, new ObjectGetOptions() );
			mo.Delete();

			// update object state
			SetState(SqlSmoState.Dropped);

			// remove the object from collection
			ParentColl.RemoveInternal(this.Name);
		}
	}
	
}

