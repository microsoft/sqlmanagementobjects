// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Broker
{
    public partial class ServiceBroker : SqlSmoObject
    {
        internal ServiceBroker(Database parentdb, ObjectKeyBase key, SqlSmoState state) :
            base(key, state)
        {
            // even though we called with the parent collection of the column, we will 
            // place the ServiceBroker under the right collection
            singletonParent = parentdb as Database;

            // WATCH OUT! we are setting the m_server value here, because ServiceBroker does
            // not live in a collection, but directly under the Database
            SetServerObject(parentdb.GetServerObject());

            m_comparer = parentdb.StringComparer;

            //m_EnvironmentServices = new StringCollection();

            m_MessageTypes = null;
            m_ServiceContracts = null;
            m_ServiceQueues = null;
            m_BrokerServices = null;
            m_ServiceRoutess = null;
            m_BrokerPriorities = null;
        }

        internal ServiceBroker() : base() { }

        [SfcObject(SfcObjectRelationship.ParentObject)]
		public Database Parent
		{
			get
			{
                return singletonParent as Database;
			}
            internal set
            {
                SetParentImpl(value);
            }
		}

        internal override void ValidateParent(SqlSmoObject newParent)
        {
            singletonParent = newParent;
            m_comparer = newParent.StringComparer;
            SetServerObject(newParent.GetServerObject());
            this.ThrowIfNotSupported(typeof(ServiceBroker));
        }

		// returns the name of the type in the urn expression
		public static string UrnSuffix
		{
			get 
			{
				return "ServiceBroker";
			}
		}

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        internal protected override string GetDBName()
        {
            return Parent.Name;
        }

        MessageTypeCollection m_MessageTypes;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(MessageType))]
        public MessageTypeCollection MessageTypes
        {
            get
            {
                CheckObjectState();
                if (m_MessageTypes == null)
                {
                    m_MessageTypes = new MessageTypeCollection(this, GetComparerFromCollation("Latin1_General_BIN"));
                }
                return m_MessageTypes;
            }
        }

        ServiceContractCollection m_ServiceContracts;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ServiceContract))]
        public ServiceContractCollection ServiceContracts
        {
            get
            {
                CheckObjectState();
                if (m_ServiceContracts == null)
                {
                    m_ServiceContracts = new ServiceContractCollection(this, GetComparerFromCollation("Latin1_General_BIN"));
                }
                return m_ServiceContracts;
            }
        }

        BrokerServiceCollection m_BrokerServices;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(BrokerService))]
        public BrokerServiceCollection Services
        {
            get
            {
                CheckObjectState();
                if (m_BrokerServices == null)
                {
                    m_BrokerServices = new BrokerServiceCollection(this, GetComparerFromCollation("Latin1_General_BIN"));
                }
                return m_BrokerServices;
            }
        }

        ServiceQueueCollection m_ServiceQueues;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ServiceQueue))]
        public ServiceQueueCollection Queues
        {
            get
            {
                CheckObjectState();
                if (m_ServiceQueues == null)
                {
                    m_ServiceQueues = new ServiceQueueCollection(this);
                }
                return m_ServiceQueues;
            }
        }

        ServiceRouteCollection m_ServiceRoutess;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ServiceRoute))]
        public ServiceRouteCollection Routes
        {
            get
            {
                CheckObjectState();
                if (m_ServiceRoutess == null)
                {
                    m_ServiceRoutess = new ServiceRouteCollection(this);
                }
                return m_ServiceRoutess;
            }
        }

        RemoteServiceBindingCollection m_RemoteServiceBindings;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(RemoteServiceBinding))]
        public RemoteServiceBindingCollection RemoteServiceBindings
        {
            get
            {
                CheckObjectState();
                if (m_RemoteServiceBindings == null)
                {
                    m_RemoteServiceBindings = new RemoteServiceBindingCollection(this);
                }
                return m_RemoteServiceBindings;
            }
        }

        BrokerPriorityCollection m_BrokerPriorities;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(BrokerPriority))]
        public BrokerPriorityCollection Priorities
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion100();
                if (m_BrokerPriorities == null)
                {
                    m_BrokerPriorities = new BrokerPriorityCollection(this, GetComparerFromCollation("Latin1_General_BIN"));
                }
                return m_BrokerPriorities;
            }
        }

    }

    // we need this class to implement methods that link correctly the object into 
    // the tree
    public class BrokerObjectBase : ScriptNameObjectBase, IScriptable
    {
        internal BrokerObjectBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            SetServerObject(parentColl.ParentInstance.GetServerObject());
            // set the comparer used by the collections
            if (ParentColl.ParentInstance is ServiceBroker)
            {
                m_comparer = ((ServiceBroker)(ParentColl.ParentInstance)).Parent.StringComparer;
            }
            else
            {
                m_comparer = ((BrokerObjectBase)(ParentColl.ParentInstance)).StringComparer;
            }
        }

        protected internal BrokerObjectBase() : base() { }

        internal protected override string GetDBName()
        {
            if (ParentColl.ParentInstance is ServiceBroker)
            {
                return ((ServiceBroker)(ParentColl.ParentInstance)).Parent.Name;
            }
            else
            {
                return ((BrokerObjectBase)(ParentColl.ParentInstance)).GetDBName();
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Name of UserDefinedAggregateParameter
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }
    }


}


