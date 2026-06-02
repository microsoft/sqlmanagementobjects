namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;

	internal class Service : WmiBase
	{
		public Service() : base("SqlService")
		{
			WmiProperty wp = null;

			wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "ServiceName";
			AddProperty(wp);

			wp = new WmiProperty("Type", "Microsoft.SqlServer.Management.Smo.Wmi.ManagedServiceType");
			wp.BaseType = "System.Int32";
			wp.PhysicalName = "SQLServiceType";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("StartMode", "Microsoft.SqlServer.Management.Smo.Wmi.ServiceStartMode");
			wp.BaseType = "System.Int32";
			AddProperty(wp);

			wp = new WmiProperty("AcceptsPause", "System.Boolean");
			wp.PhysicalName = "AcceptPause";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("AcceptsStop", "System.Boolean");
			wp.PhysicalName = "AcceptStop";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("DisplayName", "System.String");
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("ErrorControl", "Microsoft.SqlServer.Management.Smo.Wmi.ServiceErrorControl");
			wp.BaseType = "System.Int32";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("PathName", "System.String");
			wp.PhysicalName = "BinaryPath";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("Description", "System.String");
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("Dependencies", "System.String");
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("ServiceAccount", "System.String");
			wp.PhysicalName = "StartName";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("ServiceState", "Microsoft.SqlServer.Management.Smo.Wmi.ServiceState");
			wp.PhysicalName = "State";
			wp.BaseType = "System.Int32";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("ExitCode", "System.Int32");
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("ProcessId", "System.Int32");
			wp.ReadOnly = true;
			AddProperty(wp);
		}

		private int StringToEnum(string[] strList, ManagementObject mo, string propPhysName)
		{
			string str = (string)base.GetTranslatedValue(mo, propPhysName);
			return GetIndex(strList, str);
		}

		private int GetIndex(string[] strList, string str)
		{
			int i = 0;
			foreach(string s in strList)
			{
				if( s == str )
				{
					break;
				}
				i++;
			}
			return i;
		}
	}
}
