using System;
using System.Data;
using System.Management;
using Microsoft.SqlServer.Management.Sdk.Sfc;


namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	public partial class ServerIPAddress : WmiSmoObject
	{
		internal ServerIPAddress(WmiCollectionBase parentColl, string name) : 
			base(parentColl, name)
		{
		}

		// returns the name of the type in the urn expression
		internal static string UrnSuffix
		{
			get 
			{
				return "IPAddress";
			}
		}

		/// <summary>
		/// This is a property bag that holds the Server and Client 
		/// Protocol properties
		/// </summary>
		public IPAddressPropertyCollection IPAddressProperties 
		{ 
			get 
			{ 
				if(null==m_protocolProperties)
				{
					m_protocolProperties = (IPAddressPropertyCollection)GetProtocolPropertyCollection();
				}

 				return m_protocolProperties;
			} 
		}
		internal IPAddressPropertyCollection m_protocolProperties;

		public System.Net.IPAddress IPAddress
		{
			get 
			{
				if( IPAddressProperties.Contains("IpAddress"))
				{
					IPAddressProperty pp = (IPAddressProperty)IPAddressProperties["IpAddress"];
					return System.Net.IPAddress.Parse((string)pp.Value);
				}
				else
					return System.Net.IPAddress.Any;
			}
		}
		
		protected override ProtocolPropertyCollection CreateProtocolPropertyCollection()
		{
			return new IPAddressPropertyCollection();
		}

		protected override ProtocolProperty GetPropertyObject(PropertyCollection properties, DataRow dr, object propValue)
		{
			ProtocolProperty p = new IPAddressProperty(properties.Get((string)dr["Name"]));

			// set its fields, Value among them
			p.PropertyType = (UInt32)dr["Type"];
			p.SetValue(propValue);
			p.SetRetrieved(true);

			return p;
		}
		
		protected override ManagementObject GetPropertyManagementObject( ProtocolProperty pp)
		{
			try
			{
				ManagementScope ms = GetManagedComputer().ManagementScope;
				ServerProtocol parent = (ServerProtocol)this.ParentColl.ParentInstance;
				ManagementPath mp = new ManagementPath(string.Format(SmoApplication.DefaultCulture, "ServerNetworkProtocolProperty.InstanceName=\"{0}\",IPAddressName=\"{1}\",PropertyName=\"{2}\",PropertyType={3},ProtocolName=\"{4}\"", 
																	Urn.EscapeString(parent.Parent.Name), 
																	Urn.EscapeString(Name),
																	Urn.EscapeString(pp.Name),
																	pp.PropertyType, 
																	Urn.EscapeString(parent.Name)));
				return new ManagementObject(ms, mp, new ObjectGetOptions() );
			}
			catch( Exception e )
			{
				throw new ServiceRequestException( ExceptionTemplates.InnerWmiException, e);
			}
		}

		/// <summary>
		/// this is an internal method equivalent with IAlterable.Alter() implementation
		/// this method however is not exposed as public since ServerIPAddress is a minor object
		/// changes applied to ServerIPAddress or to its .IPAddressProperties collection
		/// are submited to WMI only when user call-s Alter() on parent WMI Server Protocol
		/// </summary>
		internal void AlterImplWorker()
		{
			try
			{
				AlterProtocolProperties(IPAddressProperties);
			}
			catch (Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
			}
		}

	}
}

