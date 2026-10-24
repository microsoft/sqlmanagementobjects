using System;
using System.Management;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
    public partial class ServerProtocol : ProtocolBase
    {
        internal ServerProtocol(WmiCollectionBase parentColl, string name) : 
            base(parentColl, name)
        {
        }

        // returns the name of the type in the urn expression
        internal static string UrnSuffix
        {
            get 
            {
                return "ServerProtocol";
            }
        }

        ServerIPAddressCollection ipAddressCollection = null;
        public ServerIPAddressCollection IPAddresses
        {
            get 
            { 
                if(ipAddressCollection == null)
                {
                    ipAddressCollection =  new ServerIPAddressCollection(this); 
                }
                return ipAddressCollection;
            }
        }

        // This function returns the management object that we can use to 
        // alter the protocol's properties
        protected override ManagementObject GetManagementObject()
        {
            try
            {
                ManagementScope ms = GetManagedComputer().ManagementScope;
                ManagementPath mp = new ManagementPath(string.Format(SmoApplication.DefaultCulture, 
                                    "ServerNetworkProtocol.InstanceName=\"{0}\",ProtocolName=\"{1}\"", 
                                    Parent.Name, Name));
                return new ManagementObject(ms, mp, new ObjectGetOptions() );
            }
            catch( Exception e )
            {
                throw new ServiceRequestException( ExceptionTemplates.InnerWmiException, e);
            }
        }

        protected override ManagementObject GetPropertyManagementObject( ProtocolProperty pp)
        {
            try
            {
                ManagementScope ms = GetManagedComputer().ManagementScope;
                ServerInstance parent = (ServerInstance)this.ParentColl.ParentInstance;
                ManagementPath mp = new ManagementPath(string.Format(SmoApplication.DefaultCulture, 
                                        "ServerNetworkProtocolProperty.InstanceName=\"{0}\",IPAddressName=\"\",PropertyName=\"{1}\",PropertyType={2},ProtocolName=\"{3}\"", 
                                        Urn.EscapeString(parent.Name), 
                                        Urn.EscapeString(pp.Name),
                                        pp.PropertyType, 
                                        Urn.EscapeString(Name)));
                
                return new ManagementObject(ms, mp, new ObjectGetOptions() );
            }
            catch( Exception e )
            {
                throw new ServiceRequestException( ExceptionTemplates.InnerWmiException, e);
            }
        }

        protected override ProtocolPropertyCollection CreateProtocolPropertyCollection()
        {
            return new ServerProtocolPropertyCollection();
        }

        protected override ProtocolProperty GetPropertyObject(PropertyCollection properties, DataRow dr, object propValue)
        {
            ProtocolProperty p = new ServerProtocolProperty(properties.Get((string)dr["Name"]));

            // set its fields, Value among them
            p.PropertyType = (UInt32)dr["Type"];
            p.SetValue(propValue);
            p.SetRetrieved(true);

            return p;
        }

        internal override void AlterImplWorker()
        {
            // ServerProtocol.Alter() causes applying changes for all IPAddress-es minor subobjects
            // and for all thier properties (there is no public .Alter() method on an IPAddress object)
            //
            // exceptions thrown by protocol base .AlterImplWorker() or internal ip address .AlterImplWorker()
            // are already filtered so we will not wrap and rethrow them
            foreach (ServerIPAddress ipa in this.IPAddresses)
            {
                System.Diagnostics.Debug.Assert(null != ipa, "invalid/null ip address inside collection");
                ipa.AlterImplWorker();
            }

            base.AlterImplWorker();
        }
        
    }
    
}

