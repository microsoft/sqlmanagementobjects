// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Broker
{
    public sealed partial class ServiceContractCollection : SimpleObjectCollectionBase<ServiceContract, ServiceBroker>
    {
        //has custom string comparer
        private readonly StringComparer m_comparer;

        //must initialize in constructor
        internal ServiceContractCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base((ServiceBroker)parentInstance)
        {
            m_comparer = comparer;
        }

        override internal StringComparer StringComparer
        {
            get 
            {
                return m_comparer;
            }
        }
    }
}
