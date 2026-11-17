// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Broker
{
    public sealed partial class BrokerServiceCollection : SimpleObjectCollectionBase<BrokerService, ServiceBroker>
    {

        //has custom string comparer
        StringComparer m_comparer;

        //must initialize in constructor
        internal BrokerServiceCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base((ServiceBroker)parentInstance)
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
