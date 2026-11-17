namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;

	internal class ServerProtocolProperties : ProtocolPropertiesBase
	{
		public ServerProtocolProperties() : base("ServerNetworkProtocolProperty")
		{
		}

		protected override string[]  GetParentFields()
		{
			return new String [] { "Name", "InstanceName" };
		}
		
		protected override string GetWhereClauseTemplate()
		{
			return "(IPAddressName=\"\" and ProtocolName=\"{0}\" and InstanceName=\"{1}\")";
		}
	}
}
