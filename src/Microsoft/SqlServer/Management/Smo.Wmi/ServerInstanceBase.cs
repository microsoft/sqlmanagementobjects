namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	public partial class ServerInstance : WmiSmoObject
	{
		internal ServerInstance(WmiCollectionBase parentColl, string name) : 
			base(parentColl, name)
		{
		}

		// returns the name of the type in the urn expression
		internal static string UrnSuffix
		{
			get 
			{
				return "ServerInstance";
			}
		}
		
		ServerProtocolCollection m_ServerProtocols = null;
		public ServerProtocolCollection ServerProtocols 
		{
			get 
			{ 
				if(m_ServerProtocols == null)
				{
					m_ServerProtocols =  new ServerProtocolCollection(this); 
				}
				return m_ServerProtocols;
			}
		}

	}
}

