/*
**** This file has been automaticaly generated. Do not attempt to modify manually! ****
*/
using System;
using System.Collections;
using System.Net;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
/// <summary>
/// Instance class encapsulating : ManagedComputer[@Name='']/ServerAlias
/// </summary>

	public sealed partial class ServerAlias 
	{

		public ManagedComputer Parent
		{
			get
			{
				CheckObjectState();
				return base.ParentColl.ParentInstance as ManagedComputer;
			}
			set{SetParentImpl(value);}
		}

		internal override Smo.PropertyMetadataProvider GetPropertyMetadataProvider()
		{
			return new PropertyMetadataProvider();
		}

		private class PropertyMetadataProvider : Smo.PropertyMetadataProvider
		{
			public override int PropertyNameToIDLookup(string propertyName)
			{
				switch(propertyName)
				{
					case "ConnectionString": return 0;
					case "ProtocolName": return 1;
					case "ServerName": return 2;
				}
				return -1;
			}

			public override int Count
			{
				get { return 3; }
			}

			public override StaticMetadata GetStaticMetadata(int id)
			{
				return staticMetadata[id];
			}

			static StaticMetadata [] staticMetadata = 
			{
					new StaticMetadata("ConnectionString", false, false, typeof(System.String)),
					new StaticMetadata("ProtocolName", false, false, typeof(System.String)),
					new StaticMetadata("ServerName", false, false, typeof(System.String)),
			};
		}

		public System.String ConnectionString
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("ConnectionString");
			}

			set
			{
				Properties.SetValueWithConsistencyCheck("ConnectionString", value);
			}
		}

		public System.String ProtocolName
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("ProtocolName");
			}

			set
			{
				Properties.SetValueWithConsistencyCheck("ProtocolName", value);
			}
		}

		public System.String ServerName
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("ServerName");
			}

			set
			{
				Properties.SetValueWithConsistencyCheck("ServerName", value);
			}
		}
	}
}