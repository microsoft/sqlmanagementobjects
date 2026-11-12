// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Broker
{
    public sealed partial class BrokerPriorityCollection : SimpleObjectCollectionBase<BrokerPriority, ServiceBroker>
    {

        //has custom string comparer
        private readonly StringComparer m_comparer;

        //must initialize in constructor
        internal BrokerPriorityCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base((ServiceBroker)parentInstance)
        {
            m_comparer = comparer;
        }

        internal override StringComparer StringComparer
        {
            get 
            {
                return m_comparer;
            }
        }
    }
}
