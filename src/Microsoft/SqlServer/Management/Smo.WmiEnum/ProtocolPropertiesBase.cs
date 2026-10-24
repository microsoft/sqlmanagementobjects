namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Text;
	using System.Management;
	using System.Data;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal abstract class ProtocolPropertiesBase : WmiChildObjectBase
	{
		public ProtocolPropertiesBase(string className) : base(className)
		{
			WmiProperty wp = null;

			wp = new WmiProperty("Name", "System.String");
			wp.PhysicalName = "PropertyName";
			AddProperty(wp);

			wp = new WmiProperty("Type", "System.UInt32");
			wp.PhysicalName = "PropertyType";
			AddProperty(wp);

			wp = new WmiProperty("ValType", "Microsoft.SqlServer.Management.Smo.Wmi.PropertyType");
			wp.PhysicalName = "PropertyValType";
			wp.BaseType = "System.UInt32";
			AddProperty(wp);

			wp = new WmiProperty("StrValue", "System.String");
			wp.PhysicalName = "PropertyStrVal";
			AddProperty(wp);

			wp = new WmiProperty("NumValue", "System.Int32");
			wp.PhysicalName = "PropertyNumVal";
			AddProperty(wp);

			wp = new WmiProperty("ProtocolName", "System.String");
			AddProperty(wp);
		}

	}
	
	internal abstract class WmiChildObjectBase : WmiBase
	{
		private DataTable m_dataParent = null;

		protected abstract string[]  GetParentFields();
		protected abstract string GetWhereClauseTemplate();
		
		public WmiChildObjectBase(string className) : base(className)
		{
		}

		public override Request RetrieveParentRequest()
		{
			WmiRequest w = new WmiRequest();
			w.Fields = GetParentFields();
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
				sbParentWhere.AppendFormat(GetWhereClauseTemplate(), row.ItemArray);
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
