/*
**** This file has been automaticaly generated. Do not attempt to modify manually! ****
*/
using System;
using System.Collections;
using System.Net;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
/// <summary>
/// Instance class encapsulating : ManagedComputer[@Name='']/ServerInstance/ServerProtocol
/// </summary>

	public sealed partial class ServerProtocol 
	{

		public ServerInstance Parent
		{
			get
			{
				CheckObjectState();
				return base.ParentColl.ParentInstance as ServerInstance;
			}
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
					case "DisplayName": return 0;
					case "HasMultiIPAddresses": return 1;
					case "IsEnabled": return 2;
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
					new StaticMetadata("DisplayName", false, true, typeof(System.String)),
					new StaticMetadata("HasMultiIPAddresses", false, true, typeof(System.Boolean)),
					new StaticMetadata("IsEnabled", false, false, typeof(System.Boolean)),
			};
		}

		public System.String DisplayName
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("DisplayName");
			}
		}

		public System.Boolean HasMultiIPAddresses
		{
			get
			{
				return (System.Boolean)this.Properties.GetValueWithNullReplacement("HasMultiIPAddresses");
			}
		}

		public System.Boolean IsEnabled
		{
			get
			{
				return (System.Boolean)this.Properties.GetValueWithNullReplacement("IsEnabled");
			}

			set
			{
				Properties.SetValueWithConsistencyCheck("IsEnabled", value);
			}
		}
	}
}