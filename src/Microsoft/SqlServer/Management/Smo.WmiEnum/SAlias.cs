namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;

	internal class SAlias : WmiBase
	{
		public SAlias() : base("SqlServerAlias")
		{
			WmiProperty wp = null;

			wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "AliasName";
			AddProperty(wp);

			wp = new WmiProperty("ServerName", "System.String");
			AddProperty(wp);

			wp = new WmiProperty("ProtocolName", "System.String");
			AddProperty(wp);

			wp = new WmiProperty("ConnectionString", "System.String");
			AddProperty(wp);
		}
	}
}
