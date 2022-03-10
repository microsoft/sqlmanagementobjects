// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Management;

namespace Microsoft.SqlServer.Management.Common
{
    internal class WmiMgmtScopeConnection : ConnectionInfoBase
	{
		public WmiMgmtScopeConnection() : base(ConnectionType.WmiManagementScope)
		{
		}

		public WmiMgmtScopeConnection(ManagementScope managementScope) : base(ConnectionType.WmiManagementScope)
		{
			this.ManagementScope = managementScope;
		}

		WmiMgmtScopeConnection(WmiMgmtScopeConnection conn) : base(ConnectionType.WmiManagementScope)
		{
			this.ManagementScope = conn.ManagementScope;
		}

		private ManagementScope managementScope;
		public ManagementScope ManagementScope
		{
			get
			{
				return managementScope;
			}

			set
			{
				managementScope = value;
			}
		}

		/// <summary>
		/// Deep copy
		/// </summary>
		/// <returns></returns>
		public WmiMgmtScopeConnection Copy()
		{
			return new WmiMgmtScopeConnection(this);
		}

		protected override void ConnectionParmsChanged()
		{
		}
	}
}
