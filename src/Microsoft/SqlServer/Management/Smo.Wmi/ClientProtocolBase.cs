using System;
using System.Management;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	public partial class ClientProtocol : ProtocolBase
	{
		internal ClientProtocol(WmiCollectionBase parentColl, string name) : 
			base(parentColl, name)
		{
		}

		// returns the name of the type in the urn expression
		internal static string UrnSuffix
		{
			get 
			{
				return "ClientProtocol";
			}
		}

		// This function returns the management object that we can use to 
		// alter the protocol object
		protected override ManagementObject GetManagementObject()
		{
			try
			{
				ManagementScope ms = GetManagedComputer().ManagementScope;
				// note that protocol is indexed by name
				ManagementPath mp = new ManagementPath(string.Format(SmoApplication.DefaultCulture, "ClientNetworkProtocol.ProtocolName=\"{0}\"", Name));
				return new ManagementObject(ms, mp, new ObjectGetOptions() );
			}
			catch( Exception e )
			{
				throw new ServiceRequestException( ExceptionTemplates.InnerWmiException, e);
			}
		}

		// This function returns the management object that we can use to 
		// alter the protocol properties
		protected override ManagementObject GetPropertyManagementObject( ProtocolProperty pp)
		{
			try
			{
				ManagementScope ms = GetManagedComputer().ManagementScope;
				// protocol property is indexed by protocol name, index and type
				ManagementPath mp = new ManagementPath(string.Format(SmoApplication.DefaultCulture, "ClientNetworkProtocolProperty.PropertyIdx={0},PropertyType={1},ProtocolName=\"{2}\"", 
																	((ClientProtocolProperty)pp).Index, pp.PropertyType, Name));
				return new ManagementObject(ms, mp, new ObjectGetOptions() );
			}
			catch( Exception e )
			{
				throw new ServiceRequestException( ExceptionTemplates.InnerWmiException, e);
			}
		}

		protected override ProtocolPropertyCollection CreateProtocolPropertyCollection()
		{
			return new ClientProtocolPropertyCollection();
		}

		protected override ProtocolProperty GetPropertyObject(PropertyCollection properties, DataRow dr, object propValue)
		{
			ClientProtocolProperty p = new ClientProtocolProperty(properties.Get((string)dr["Name"]));

			// set its fields, Value among them
			p.PropertyType = (UInt32)dr["Type"];
			p.Index = (UInt32)dr["Idx"];
			p.SetValue(propValue);
			p.SetRetrieved(true);

			return p;
		}

		NetLibInfo netLibInfo = null;
		/// <summary>
		/// The encapsulated NetLibInfo object
		/// </summary>
		public NetLibInfo NetLibInfo
		{
			get
			{
				if (null == netLibInfo)
				{
					RefreshNetLibInfo();
				}

				return netLibInfo;
			}
		}

		// returns false if the protocol does not have netlibinfo
		internal bool RefreshNetLibInfo()
		{
			// compute the Urn for the NetLibInfo
			Urn urnNetLibInfo = string.Empty;

			urnNetLibInfo = string.Format(SmoApplication.DefaultCulture, "{0}/ClientNetLibInfo[@ProtocolName='{1}']",
				GetManagedComputer().Urn, Urn.EscapeString(this.Name));

			// call enumerator to get object's properties
			DataSet ds = Proxy.ProcessRequest(new Request(urnNetLibInfo));

			// fill the structure
			if (ds.Tables[0].Rows.Count == 0)
			{
				netLibInfo = null;
				return false;
			}

			netLibInfo = new NetLibInfo();

			DataRow dr = ds.Tables[0].Rows[0];
			netLibInfo.fileName = (string)dr["FileName"];
			netLibInfo.version = (string)dr["Version"];

			// here we have to do a conversion between the value that we get from 
			// the provider and a DateTime structure
			// The value obtained from the provider represents the high order DWORD 
			// of a FILETIME structure, that measures the time as 100 nanosecond ticks 
			// since January 1, 1601 (UTC). 
			// DateTime constructors gets an int64 that measures the 100 nanosec ticks
			// from 12:00:00 midnight, January 1, 0001 C.E. (Common Era)
			// so we need to convert and offset the provider value 
			netLibInfo.date = new DateTime(Convert.ToInt64((UInt32)dr["Date"], SmoApplication.DefaultCulture) << 32);
			// get the time span between 0 A.D. and 1601 A.D.
			TimeSpan offset = (new DateTime(1601, 1, 1, 12, 0, 0)) - (new DateTime(1, 1, 1, 12, 0, 0));
			netLibInfo.date = netLibInfo.date + offset;

			netLibInfo.size = Convert.ToInt32(dr["Size"], SmoApplication.DefaultCulture);

			return true;
		}

		/// <summary>
		/// Refreshes the property bag and the ProtocolProperties collection
		/// </summary>
		public override void Refresh()
		{
			base.Refresh();
			netLibInfo = null;
		}

	}
	
}

