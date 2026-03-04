namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;
	using System.Data;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal class WmiProperty : ObjectProperty
	{
		string m_PhysicalName;
		string m_BaseType;

		public WmiProperty(string name, string type)
		{
			this.Name = name;
			this.PhysicalName = name;
			this.Type = type;
			this.BaseType = type;
			this.Usage = ObjectPropertyUsages.Request | ObjectPropertyUsages.Filter | ObjectPropertyUsages.Reserved1;
			this.ExtendedType = false;
		}
		public string PhysicalName
		{
			get
			{
				return m_PhysicalName;
			}

			set
			{
				m_PhysicalName = value;
			}
		}

		public string BaseType
		{
			get
			{
				return m_BaseType;
			}

			set
			{
				this.ExtendedType = true;
				m_BaseType = value;
			}
		}
	}
}
