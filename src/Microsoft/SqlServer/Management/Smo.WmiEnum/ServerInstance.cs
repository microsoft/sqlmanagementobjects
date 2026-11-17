namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal class ServerInstance : WmiBase
	{
        public ServerInstance()
            : base("ServerSettings")
        {
			WmiProperty wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "InstanceName";
			AddProperty(wp);
		}

		public override EnumResult GetData(EnumResult erParent)
		{
			WmiRequest w = this.Request as WmiRequest;
			if( null != w && true == w.RequestingTheWhereClause )
			{
				WmiEnumResult er = new WmiEnumResult(((WmiEnumResult)erParent).Scope);
				er.WhereClause = GetXpathFilter();
				return er;				
			}
			return base.GetData(erParent);
		}
	}
}

