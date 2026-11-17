/*
**** This file has been automaticaly generated. Do not attempt to modify manually! ****
*/
using System;
using System.Collections;
using System.Net;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
/// <summary>
/// Instance class encapsulating : ManagedComputer[@Name='']/Service
/// </summary>

	public sealed partial class Service 
	{

		public ManagedComputer Parent
		{
			get
			{
				CheckObjectState();
				return base.ParentColl.ParentInstance as ManagedComputer;
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
					case "AcceptsPause": return 0;
					case "AcceptsStop": return 1;
					case "Dependencies": return 2;
					case "Description": return 3;
					case "DisplayName": return 4;
					case "ErrorControl": return 5;
					case "ExitCode": return 6;
					case "PathName": return 7;
					case "ProcessId": return 8;
					case "ServiceAccount": return 9;
					case "ServiceState": return 10;
					case "StartMode": return 11;
					case "Type": return 12;
				}
				return -1;
			}

			public override int Count
			{
				get { return 13; }
			}

			public override StaticMetadata GetStaticMetadata(int id)
			{
				return staticMetadata[id];
			}

			static StaticMetadata [] staticMetadata = 
			{
					new StaticMetadata("AcceptsPause", false, true, typeof(System.Boolean)),
					new StaticMetadata("AcceptsStop", false, true, typeof(System.Boolean)),
					new StaticMetadata("Dependencies", false, true, typeof(System.String)),
					new StaticMetadata("Description", false, true, typeof(System.String)),
					new StaticMetadata("DisplayName", false, true, typeof(System.String)),
					new StaticMetadata("ErrorControl", false, true, typeof(Microsoft.SqlServer.Management.Smo.Wmi.ServiceErrorControl)),
					new StaticMetadata("ExitCode", false, true, typeof(System.Int32)),
					new StaticMetadata("PathName", false, true, typeof(System.String)),
					new StaticMetadata("ProcessId", false, true, typeof(System.Int32)),
					new StaticMetadata("ServiceAccount", false, true, typeof(System.String)),
					new StaticMetadata("ServiceState", false, true, typeof(Microsoft.SqlServer.Management.Smo.Wmi.ServiceState)),
					new StaticMetadata("StartMode", false, false, typeof(Microsoft.SqlServer.Management.Smo.Wmi.ServiceStartMode)),
					new StaticMetadata("Type", false, true, typeof(Microsoft.SqlServer.Management.Smo.Wmi.ManagedServiceType)),
			};
		}

		public System.Boolean AcceptsPause
		{
			get
			{
				return (System.Boolean)this.Properties.GetValueWithNullReplacement("AcceptsPause");
			}
		}

		public System.Boolean AcceptsStop
		{
			get
			{
				return (System.Boolean)this.Properties.GetValueWithNullReplacement("AcceptsStop");
			}
		}

		public System.String Description
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("Description");
			}
		}

		public System.String DisplayName
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("DisplayName");
			}
		}

		public Microsoft.SqlServer.Management.Smo.Wmi.ServiceErrorControl ErrorControl
		{
			get
			{
				return (Microsoft.SqlServer.Management.Smo.Wmi.ServiceErrorControl)this.Properties.GetValueWithNullReplacement("ErrorControl");
			}
		}

		public System.Int32 ExitCode
		{
			get
			{
				return (System.Int32)this.Properties.GetValueWithNullReplacement("ExitCode");
			}
		}

		public System.String PathName
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("PathName");
			}
		}

		public System.Int32 ProcessId
		{
			get
			{
				return (System.Int32)this.Properties.GetValueWithNullReplacement("ProcessId");
			}
		}

		public System.String ServiceAccount
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("ServiceAccount");
			}
		}

		public Microsoft.SqlServer.Management.Smo.Wmi.ServiceState ServiceState
		{
			get
			{
				return (Microsoft.SqlServer.Management.Smo.Wmi.ServiceState)this.Properties.GetValueWithNullReplacement("ServiceState");
			}
		}

		public Microsoft.SqlServer.Management.Smo.Wmi.ServiceStartMode StartMode
		{
			get
			{
				return (Microsoft.SqlServer.Management.Smo.Wmi.ServiceStartMode)this.Properties.GetValueWithNullReplacement("StartMode");
			}

			set
			{
				Properties.SetValueWithConsistencyCheck("StartMode", value);
			}
		}

		public Microsoft.SqlServer.Management.Smo.Wmi.ManagedServiceType Type
		{
			get
			{
				return (Microsoft.SqlServer.Management.Smo.Wmi.ManagedServiceType)this.Properties.GetValueWithNullReplacement("Type");
			}
		}

		internal Smo.PropertyMetadataProvider GetPropertyMetadataProviderAdvanced()
		{
			return new PropertyMetadataProviderAdvanced();
		}

		private class PropertyMetadataProviderAdvanced : Smo.PropertyMetadataProvider
		{
			public override int PropertyNameToIDLookup(string propertyName)
			{
				switch(propertyName)
				{
					case "Idx": return 0;
					case "IsReadOnly": return 1;
					case "Name": return 2;
					case "NumericValue": return 3;
					case "ServiceName": return 4;
					case "ServiceType": return 5;
					case "StringValue": return 6;
					case "ValueType": return 7;
				}
				return -1;
			}

			public override int Count
			{
				get { return 8; }
			}

			public override StaticMetadata GetStaticMetadata(int id)
			{
				return staticMetadata[id];
			}

			static StaticMetadata [] staticMetadata = 
			{
					new StaticMetadata("Idx", false, true, typeof(System.UInt32)),
					new StaticMetadata("IsReadOnly", false, true, typeof(System.Boolean)),
					new StaticMetadata("Name", false, false, typeof(System.String)),
					new StaticMetadata("NumericValue", false, false, typeof(System.Int64)),
					new StaticMetadata("ServiceName", false, true, typeof(System.String)),
					new StaticMetadata("ServiceType", false, true, typeof(Microsoft.SqlServer.Management.Smo.Wmi.ManagedServiceType)),
					new StaticMetadata("StringValue", false, false, typeof(System.String)),
					new StaticMetadata("ValueType", false, true, typeof(Microsoft.SqlServer.Management.Smo.Wmi.PropertyType)),
			};
		}
	}
}