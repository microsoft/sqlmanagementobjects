/*
**** This file has been automaticaly generated. Do not attempt to modify manually! ****
*/
using System;
using System.Collections;
using System.Net;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
/// <summary>
/// Instance class encapsulating : ManagedComputer[@Name='']/ServerInstance/ServerProtocol/IPAddress
/// </summary>

	public sealed partial class ServerIPAddress 
	{

		public ServerProtocol Parent
		{
			get
			{
				CheckObjectState();
				return base.ParentColl.ParentInstance as ServerProtocol;
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
				return -1;
			}

			public override int Count
			{
				get { return 0; }
			}

			public override StaticMetadata GetStaticMetadata(int id)
			{
				return staticMetadata[id];
			}

			static StaticMetadata [] staticMetadata = 
			{
			};
		}
	}
}