namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;

	internal class CNetLibInfo : WmiBase
	{
		public CNetLibInfo() : base("ClientNetLibInfo")
		{
			WmiProperty wp = null;

			wp = new WmiProperty("FileName", "System.String");
			AddProperty(wp);

			wp = new WmiProperty("Version", "System.String");
			AddProperty(wp);

			wp = new WmiProperty("Date", "System.UInt32");
			AddProperty(wp);

			wp = new WmiProperty("Size", "System.UInt32");
			AddProperty(wp);
			
			wp = new WmiProperty("ProtocolName", "System.String");
			AddProperty(wp);
		}

	}
}
