/*
**** This file has been automaticaly generated. Do not attempt to modify manually! ****
*/
using System;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Broker
{
/// <summary>
/// Instance class encapsulating : Server[@Name='']/Database/ServiceBroker/ServiceType
/// </summary>

    public sealed class ServiceType : ServiceTypeBase
    {
        internal ServiceType(AbstractCollectionBase parent, string name, SqlSmoState state) : 
                base(parent, name, state)
        {
        }

        public ServiceType() : base() { }

        public ServiceType(ServiceBroker servicebroker, string name) : base()
        {
            this.Name = name;
            this.Parent = servicebroker;
        }

        public ServiceBroker Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as ServiceBroker;
            }
            set{SetParentImpl(value);}
        }

        public System.Int32 ID
        {
            get
            {
                return (System.Int32)(Properties["ID"].Value);
            }
        }

        public System.Byte MajorVersion
        {
            get
            {
                return (System.Byte)(Properties["MajorVersion"].Value);
            }
        }

        public System.Byte MinorVersion
        {
            get
            {
                return (System.Byte)(Properties["MinorVersion"].Value);
            }
        }

        public System.String ShortName
        {
            get
            {
                return Properties["ShortName"].Value as System.String;
            }
        }
    }
}
