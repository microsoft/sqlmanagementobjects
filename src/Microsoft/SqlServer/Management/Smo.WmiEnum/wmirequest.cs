namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal class WmiRequest : Request
	{
		bool bRequestingTheWhereClause = false;

		internal bool RequestingTheWhereClause
		{

			get { return bRequestingTheWhereClause; }
			set { bRequestingTheWhereClause = value; }
		}
	}
}
			
