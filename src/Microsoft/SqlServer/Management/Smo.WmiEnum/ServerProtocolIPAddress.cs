namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Data;
	using System.Text;
	using System.Management;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal class ServerProtocolIPAddress : WmiBase
	{
		DataTable m_dataParent = null;

		public ServerProtocolIPAddress() : base("ServerNetworkProtocolIPAddress")
		{
			WmiProperty wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "IpAddressName";
			AddProperty(wp);

			wp = new WmiProperty("InstanceName", "System.String");
			wp.ReadOnly = true;
			wp.Usage = ObjectPropertyUsages.Reserved1;
			AddProperty(wp);

			wp = new WmiProperty("ProtocolName", "System.String");
			wp.ReadOnly = true;
			wp.Usage = ObjectPropertyUsages.Reserved1;
			AddProperty(wp);
		}

		public override Request RetrieveParentRequest()
		{
			WmiRequest w = new WmiRequest();
			w.Fields = new String [] { "Name", "InstanceName" };
			return w;
		}

		protected override void BuildStatementBuilder()
		{
			base.BuildStatementBuilder();

			StringBuilder sbParentWhere = new StringBuilder();
			foreach(DataRow row in m_dataParent.Rows)
			{
				if( sbParentWhere.Length > 0 )
				{
					sbParentWhere.Append(" OR ");
				}
				sbParentWhere.AppendFormat("(ProtocolName=\"{0}\" and InstanceName=\"{1}\")", row[0], row[1]);
			}
			m_dataParent = null;
			this.StatementBuilder.AddWhere(sbParentWhere.ToString());
		}

		public override EnumResult GetData(EnumResult erParent)
		{
			m_dataParent = (DataTable)erParent;
			return base.GetData(erParent);
		}
	}
}
