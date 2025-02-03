namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;
	using System.Data;

	internal class ServerProtocolIPAddressProperty : ProtocolPropertiesBase
	{
		public ServerProtocolIPAddressProperty() : base("ServerNetworkProtocolProperty")
		{
		}

		protected override string[]  GetParentFields()
		{
			return new String [] { "Name", "ProtocolName", "InstanceName" };
		}
		
		protected override string GetWhereClauseTemplate()
		{
			return "(IPAddressName=\"{0}\" and ProtocolName=\"{1}\" and InstanceName=\"{2}\")";
		}
		
	}
}

