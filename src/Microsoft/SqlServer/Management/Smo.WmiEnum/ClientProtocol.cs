namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;
	using System.Data;

	internal class ClientProtocol : WmiBase
	{
		public ClientProtocol() : base("ClientNetworkProtocol")
		{
			WmiProperty wp = null;

			wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "ProtocolName";
			AddProperty(wp);

			wp = new WmiProperty("Order", "System.Int32");
			wp.PhysicalName = "ProtocolOrder";
			AddProperty(wp);

			wp = new WmiProperty("DisplayName", "System.String");
			wp.PhysicalName = "ProtocolDisplayName";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("NetworkLibrary", "System.String");
			wp.PhysicalName = "ProtocolDLL";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("IsEnabled", "System.Boolean");
			wp.PhysicalName = "ProtocolOrder";
			AddProperty(wp);
		}

		protected override object GetTranslatedValue(ManagementObject mo, string propPhysName)
		{
			if( "IsEnabled" == propPhysName )
			{				
				return (bool)((int)(mo["ProtocolOrder"]) > 0);
			}
			return base.GetTranslatedValue(mo, propPhysName);
		}
	}
}
