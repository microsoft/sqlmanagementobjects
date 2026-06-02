using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Collections.Specialized;
using System.Data;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Broker
{
    [CLSCompliant(false)]
    public class ServiceContractExtender : SmoObjectExtender<ServiceContract>, ISfcValidate
    {
        public ServiceContractExtender() : base() { }

        public ServiceContractExtender(ServiceContract serviceContract) : base(serviceContract) { }

        [ExtendedPropertyAttribute()]
        public string Name
        {
            get
            {
                return this.Parent.Name;
            }
            set
            {
                this.Parent.Name = value;
            }
        }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }
}
