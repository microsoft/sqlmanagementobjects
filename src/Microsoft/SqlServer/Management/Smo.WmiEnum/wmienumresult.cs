namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal class WmiEnumResult : EnumResult
	{
		string m_scope;
		string m_whereClause;

		public string Scope
		{
			get { return m_scope; }
			set { m_scope = value; }
		}

		public string WhereClause
		{
			get { return m_whereClause; }
			set { m_whereClause = value; }
		}


		public WmiEnumResult(string scope) : base(null, ResultType.Reserved1)
		{
			this.Scope = scope;
		}
	}
}
			
