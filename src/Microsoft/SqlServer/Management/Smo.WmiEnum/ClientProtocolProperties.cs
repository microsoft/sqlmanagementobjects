namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;

	internal class ClientProtocolProperties : ProtocolPropertiesBase
	{
		public ClientProtocolProperties() : base("ClientNetworkProtocolProperty")
		{
			WmiProperty wp = null;
			
			wp = new WmiProperty("Idx", "System.UInt32");
			wp.PhysicalName = "PropertyIdx";
			AddProperty(wp);
		}

		protected override string[]  GetParentFields()
		{
			return new String [] {  "Name"};
		}
		
		protected override string GetWhereClauseTemplate()
		{
			return "(ProtocolName=\"{0}\")";
		}
	}
}
