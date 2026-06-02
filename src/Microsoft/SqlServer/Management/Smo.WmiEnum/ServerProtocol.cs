namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal class ServerProtocol : WmiBase
	{
		String m_whereServiceInstance = null;
		
		public ServerProtocol() : base("ServerNetworkProtocol")
		{
			WmiProperty wp = null;

			wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "ProtocolName";
			AddProperty(wp);

			wp = new WmiProperty("InstanceName", "System.String");
			wp.ReadOnly = true;
			wp.Usage = ObjectPropertyUsages.Reserved1;
			AddProperty(wp);

			wp = new WmiProperty("DisplayName", "System.String");
			wp.PhysicalName = "ProtocolDisplayName";
			wp.ReadOnly = true;
			AddProperty(wp);

			wp = new WmiProperty("IsEnabled", "System.Boolean");
			wp.PhysicalName = "Enabled";
			AddProperty(wp);
			
			wp = new WmiProperty("HasMultiIPAddresses", "System.Boolean");
			wp.PhysicalName = "MultiIpConfigurationSupport";
			wp.ReadOnly = true;
			AddProperty(wp);
		}

		
		public override Request RetrieveParentRequest()
		{
			WmiRequest w = new WmiRequest();
			w.RequestingTheWhereClause = true;
			return w;
		}

		protected override void BuildStatementBuilder()
		{
			base.BuildStatementBuilder();

			
			if( m_whereServiceInstance.Length > 0 )
			{
				this.StatementBuilder.AddWhere(m_whereServiceInstance);
			}
			m_whereServiceInstance = null;
		}

		public override EnumResult GetData(EnumResult erParent)
		{
			m_whereServiceInstance = ((WmiEnumResult)erParent).WhereClause;

			return base.GetData(erParent);
		}
	}
}
