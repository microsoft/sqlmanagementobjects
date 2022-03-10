// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo.Broker
{
    [CLSCompliant(false)]
    public class BrokerServiceExtender : SmoObjectExtender<BrokerService>, ISfcValidate
    {
        public BrokerServiceExtender() : base() { }

        public BrokerServiceExtender(BrokerService brokerService) : base(brokerService) { }

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
        [ExtendedPropertyAttribute()]
        public Smo.Broker.ServiceContractMappingCollection ServiceContractMappings
        {
            get
            {
                return this.Parent.ServiceContractMappings;
            }
        }

        

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }


    [CLSCompliant(false)]
    public class BrokerPriorityExtender : SmoObjectExtender<BrokerPriority>, ISfcValidate
    {
        public BrokerPriorityExtender() : base() { }

        public BrokerPriorityExtender(BrokerPriority brokerPriority) : base(brokerPriority) { }

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

    [CLSCompliant(false)]
    public class ServiceRouteExtender : SmoObjectExtender<Smo.Broker.ServiceRoute>, ISfcValidate
    {
        public ServiceRouteExtender() : base() { }

        public ServiceRouteExtender(Smo.Broker.ServiceRoute route) : base(route) { }

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

    
    [CLSCompliant(false)]
    public class ServiceQueueExtender : SmoObjectExtender<ServiceQueue>, ISfcValidate
    {
        public ServiceQueueExtender() : base() { }

        public ServiceQueueExtender(ServiceQueue serviceQueue) : base(serviceQueue) { }

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

        [ExtendedPropertyAttribute()]
        public Version ServerVersion
        {
            get
            {
                return this.Parent.GetServerObject().Version;
            }
        }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }

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

        [ExtendedPropertyAttribute()]
        public Smo.Broker.MessageTypeMappingCollection MessageTypeMappings
        {
            get
            {
                return this.Parent.MessageTypeMappings;
            }
        }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }

    
    [CLSCompliant(false)]
    public class RemoteServiceBindingExtender : SmoObjectExtender<RemoteServiceBinding>, ISfcValidate
    {
        public RemoteServiceBindingExtender() : base() { }

        public RemoteServiceBindingExtender(RemoteServiceBinding remoteServiceBinding) : base(remoteServiceBinding) { }

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

    
    [CLSCompliant(false)]
    public class MessageTypeExtender : SmoObjectExtender<Smo.Broker.MessageType>, ISfcValidate
    {
        public MessageTypeExtender() : base() { }

        public MessageTypeExtender(Smo.Broker.MessageType messageType) : base(messageType) { }

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
