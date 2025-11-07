namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;

	internal class ServiceAdvancedProperty : WmiBase
	{
		public ServiceAdvancedProperty() : base("SqlServiceAdvancedProperty")
		{
			WmiProperty wp = null;

			//IsReadOnly = TRUE;
			//PropertyIndex = 3;
			//PropertyName = "INSTALLPATH";
			//PropertyStrValue = "C:\\Program Files\\Microsoft SQL Server\\MSSQL.1\\MSSQL";
			//PropertyValueType = 0;
			//ServiceName = "MSSQL$YUKON625";
			//SqlServiceType = 1;


			wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "PropertyName";
			AddProperty(wp);

			wp = new WmiProperty("ServiceType", "Microsoft.SqlServer.Management.Smo.Wmi.ManagedServiceType");
			wp.BaseType = "System.Int32";
			wp.PhysicalName = "SqlServiceType";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("ServiceName", "System.String");
			wp.PhysicalName = "ServiceName";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("StringValue", "System.String");
			wp.PhysicalName = "PropertyStrValue";
			AddProperty(wp);

			wp = new WmiProperty("NumericValue", "System.Int64");
			wp.PhysicalName = "PropertyNumValue";
			AddProperty(wp);

			wp = new WmiProperty("ValueType", "Microsoft.SqlServer.Management.Smo.Wmi.PropertyType");
			wp.PhysicalName = "PropertyValueType";
			wp.BaseType = "System.UInt32";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("Idx", "System.UInt32");
			wp.PhysicalName = "PropertyIndex";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("IsReadOnly", "System.Boolean");
			wp.PhysicalName = "IsReadOnly";
			wp.ReadOnly = true;
			AddProperty(wp);

		}

	}
}
