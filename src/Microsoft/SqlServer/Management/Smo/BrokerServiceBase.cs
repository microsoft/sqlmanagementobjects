// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Broker
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.Broker.BrokerLocalizableResources", true)]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    public partial class BrokerService : BrokerObjectBase, IExtendedProperties, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists
    {
        internal BrokerService(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "BrokerService";
            }
        }

        private void GetDDL(StringCollection queries, ScriptingPreferences sp, bool bCreate)
        {

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            /*
            CREATE SERVICE service-name 
                [ AUTHORIZATION owner_name ] 
                ON QUEUE [schema.]queue-name
                [ (contract-name [ ,...n ] ) ]

                ALTER SERVICE 
                service-name 
                [ON QUEUE [schema.]queue_name]
                ( < opt_arg > [ , ...n ] )

            < opt_arg > ::= 
            ADD CONTRACT  contract-name
            |
            DROP CONTRACT contract-name 
			
           */

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (bCreate && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_BROKER_SERVICE, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} SERVICE {1} ", bCreate ? "CREATE" : "ALTER", FormatFullNameForScripting(sp));
            if (bCreate && sp.IncludeScripts.Owner)
            {
                Property pOwner = Properties.Get("Owner");

                if (null != pOwner.Value)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " AUTHORIZATION [{0}] ", SqlBraket(pOwner.Value as string));
                }
            }

            Property pQueue = Properties["QueueName"];
            string queueSchema = GetPropValueOptional("QueueSchema", string.Empty);

            if (null != pQueue.Value)
            {
                if (queueSchema.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " ON QUEUE [{0}].[{1}] ",
                                                            SqlBraket(queueSchema),
                                                            SqlBraket(pQueue.Value as string));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " ON QUEUE [{0}] ",
                                                            SqlBraket(pQueue.Value as string));
                }
            }

            if (bCreate && ServiceContractMappings.Count > 0)
            {
                sb.Append(Globals.LParen);
                bool bComma = false;
                foreach (ServiceContractMapping scm in ServiceContractMappings)
                {
                    if (bComma)
                    {
                        sb.Append(Globals.comma);
                        sb.Append(Globals.newline);
                    }
                    else
                    {
                        bComma = true;
                    }

                    sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(scm.Name));
                }

                sb.Append(Globals.RParen);
            }
            else if (!bCreate)
            {
                bool openParen = false;
                bool comma = false;
                foreach (ServiceContractMapping scm in ServiceContractMappings)
                {
                    if (scm.State == SqlSmoState.Creating)
                    {
                        if (!openParen)
                        {
                            sb.Append(Globals.LParen);
                            openParen = true;
                        }

                        if (comma)
                        {
                            sb.Append(Globals.comma);
                            sb.Append(Globals.newline);
                        }
                        else
                        {
                            comma = true;
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture, "ADD CONTRACT [{0}]", SqlBraket(scm.Name));
                    }
                    else if (scm.State == SqlSmoState.ToBeDropped)
                    {
                        if (!openParen)
                        {
                            sb.Append(Globals.LParen);
                            openParen = true;
                        }

                        if (comma)
                        {
                            sb.Append(Globals.comma);
                            sb.Append(Globals.newline);
                        }
                        else
                        {
                            comma = true;
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture, "DROP CONTRACT [{0}]", SqlBraket(scm.Name));
                    }
                }
                if (openParen)
                {
                    sb.Append(Globals.RParen);
                }
            }
            // add the ddl to create the object
            queries.Add(sb.ToString());
        }



		public void Create()
		{
			base.CreateImpl();
		}

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersion);

            GetDDL(queries, sp, true);
        }

		// this function is meant to be overriden by derived classes, if they have to do
		// supplimentary actions after object creation
		protected override void PostCreate()
		{
			UpdateCollectionState2(ServiceContractMappings);
		}
		
		public void Drop()
		{
			base.DropImpl();
		}

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersion);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_BROKER_SERVICE, "", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP SERVICE {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

		public void Alter()
		{
			base.AlterImpl();
		}

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (IsObjectDirty())
            {
                GetDDL(alterQuery, sp, false);
            }
        }


		protected override bool IsObjectDirty()
		{
			return base.IsObjectDirty() || IsCollectionDirty(ServiceContractMappings);
		}

		ServiceContractMappingCollection m_ServiceContractMappings;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ServiceContractMapping))]
		public ServiceContractMappingCollection ServiceContractMappings
		{
			get
			{
				CheckObjectState();
				if(m_ServiceContractMappings==null) 
				{
					m_ServiceContractMappings = new ServiceContractMappingCollection(this);
				}
				return m_ServiceContractMappings;
			}
		}

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
		public ExtendedPropertyCollection ExtendedProperties
		{
			get 
			{
				ThrowIfBelowVersion80();
				CheckObjectState();
				if( null == m_ExtendedProperties )
				{
					m_ExtendedProperties = new ExtendedPropertyCollection(this);
				}
				return m_ExtendedProperties;
			}
		}

	}
}



