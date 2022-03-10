using System;
using System.Management;

using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
    public abstract class ProtocolBase : WmiSmoObject, Cmn.IAlterable
    {
        internal ProtocolBase(WmiCollectionBase parentColl, string name) : 
            base(parentColl, name)
        {
        }

        // This function returns the management object that we can use to 
        // alter the protocol's properties
        protected abstract ManagementObject GetManagementObject();

        
        ///<summary>
        /// changes the object according to the modification of its members
        ///</summary>
        public void Alter()	
        {
            try
            {
                AlterImplWorker();

                // generate internal events
                if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                {
                    SmoApplication.eventsSingleton.CallObjectAltered(GetManagedComputer(), 
                        new ObjectAlteredEventArgs(this.Urn, this));
                }

            }
            catch( Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
            }
        }

        internal virtual void AlterImplWorker()
        {
            ManagementObject mgmtObject = GetManagementObject();

            Property pEnabled = Properties.Get("IsEnabled");
            if( pEnabled.Dirty )
            {
                if((bool)pEnabled.Value )
                {
                    InvokeMgmtMethod(mgmtObject, "SetEnable", null);
                }
                else
                {
                    InvokeMgmtMethod(mgmtObject, "SetDisable", null);
                }
            }

            // for ServerProtocol, this property will be missing
            // this is why we need to check for tis existance
            if( Properties.Contains("Order"))
            {
                Property pOrder = Properties.Get("Order");
                if( pOrder.Dirty )
                {
                    // set the new value
                    InvokeMgmtMethod(mgmtObject, "SetOrderValue", 
                        new object[] { SmoApplication.ConvertInt32ToUInt32((Int32)pOrder.Value) });
                }
            }

            AlterProtocolProperties(ProtocolProperties);
        }



        /// <summary>
        /// This is a property bag that holds the Server and Client 
        /// Protocol properties
        /// </summary>
        public ProtocolPropertyCollection ProtocolProperties 
        { 
            get 
            { 
                if(null==m_protocolProperties)
                {
                    m_protocolProperties = GetProtocolPropertyCollection();
                }

                return m_protocolProperties;
            } 
        }
        internal ProtocolPropertyCollection m_protocolProperties = null;
    

        /// <summary>
        /// Refreshes the property bag and the ProtocolProperties collection
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            m_protocolProperties = null;
        }
    }

    public sealed class NetLibInfo
    {
        internal NetLibInfo()
        {
        }

        
        internal string fileName;		//	The filename of the network library.
        public string FileName { get { return fileName;}}
        
        internal string version;		//	The version of the network library
        public string Version { get { return version; }}
        
        internal DateTime date;		//Date/time of the network library.
        public DateTime Date { get { return date; }}
        
        internal int size;				//	Size of the network library.
        public int Size { get { return size; }}

    }


}



