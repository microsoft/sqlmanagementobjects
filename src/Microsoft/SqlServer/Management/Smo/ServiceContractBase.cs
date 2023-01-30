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
    public partial class ServiceContract : BrokerObjectBase, IExtendedProperties, Cmn.ICreatable,
        Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists
    {
        internal ServiceContract(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "ServiceContract";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SERVICE_CONTRACT, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE CONTRACT {0} ", FormatFullNameForScripting(sp));

            if (sp.IncludeScripts.Owner)
            {
                string s = (string)this.GetPropValueOptional("Owner");
                if (null != s && s.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "AUTHORIZATION [{0}] ", SqlBraket(s));
                } 
            }
            // if the messagetype mappings list is empty, the contract cannot be created
            if (MessageTypeMappings.Count < 1)
            {
                throw new SmoException(ExceptionTemplates.EmptyMapping("ServiceContract", "MessageTypeMapping"));
            }

            sb.Append(Globals.LParen);
            bool bComma = false;
            foreach (MessageTypeMapping mtm in MessageTypeMappings)
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

                MessageSource messageSource = (MessageSource)mtm.GetPropValue("MessageSource");
                string sentBy = string.Empty;
                switch (messageSource)
                {
                    case MessageSource.Initiator:
                        sentBy = "INITIATOR";
                        break;
                    case MessageSource.Target:
                        sentBy = "TARGET";
                        break;
                    case MessageSource.InitiatorAndTarget:
                        sentBy = "ANY";
                        break;
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}] SENT BY {1}", SqlBraket(mtm.Name), sentBy);
            }

            sb.Append(Globals.RParen);

            queries.Add(sb.ToString());
        }

        // this function is meant to be overriden by derived classes, if they have to do
        // supplimentary actions after object creation
        protected override void PostCreate()
        {
            UpdateCollectionState2(MessageTypeMappings);
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
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SERVICE_CONTRACT, "", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP CONTRACT {0}", FormatFullNameForScripting(sp));
            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            string s = (string)this.GetPropValueOptional("Owner");

            if (null != s && s.Length > 0)
            {
                alterQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER AUTHORIZATION ON CONTRACT::{0} TO [{1}] ", FormatFullNameForScripting(sp), SqlBraket(s)));
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            bool bWithScript = action != PropagateAction.Create;

            PropagateInfo [] pi;

            pi = new PropagateInfo[] { 
                                         new PropagateInfo(m_ExtendedProperties, bWithScript, ExtendedProperty.UrnSuffix ),
                                         new PropagateInfo(m_MessageTypeMappings, false)
                                     };
            return pi;

        }
                                     

        MessageTypeMappingCollection m_MessageTypeMappings;
        [SfcObject( SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(MessageTypeMapping))]
        public MessageTypeMappingCollection MessageTypeMappings
        {
            get
            {
                CheckObjectState();
                if(m_MessageTypeMappings==null) 
                {
                    m_MessageTypeMappings = new MessageTypeMappingCollection(this);
                }
                return m_MessageTypeMappings;
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

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if (null != m_MessageTypeMappings)
            {
                m_MessageTypeMappings.MarkAllDropped();
            }
        }
    }


}


