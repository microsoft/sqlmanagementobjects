// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Broker
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.Broker.BrokerLocalizableResources", true)]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    public partial class MessageType : BrokerObjectBase, IExtendedProperties, Cmn.ICreatable,
        Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists
    {
        internal MessageType(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "MessageType";
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        public void Create()
        {
            base.CreateImpl();
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        private void ScriptMessageType(StringCollection queries, ScriptingPreferences sp, bool bForCreate)
        {
            /*

            CREATE MESSAGE TYPE message-type-name
                [ AUTHORIZATION owner_name ] 
                [ VALIDATION = {  NONE | EMPTY | WELL_FORMED_XML | 
                    VALID_XML WITH SCHEMA COLLECTION schema-collection_name } ]   


            ALTER MESSAGE TYPE message-type-name
                [ VALIDATION = {  NONE | EMPTY | WELL_FORMED_XML | 
                    VALID_XML WITH SCHEMA COLLECTION schema-collection_name }   ]


            */
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            if (bForCreate && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_MESSAGE_TYPE, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} MESSAGE TYPE {1}", bForCreate ? "CREATE" : "ALTER", FormatFullNameForScripting(sp));

            if (bForCreate && sp.IncludeScripts.Owner)
            {
                Property owner = Properties.Get("Owner");
                if (null != owner.Value)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " AUTHORIZATION [{0}]", SqlBraket(Convert.ToString(owner.Value, SmoApplication.DefaultCulture)));
                }
            }

            Property validation = Properties.Get("MessageTypeValidation");
            if (null != validation.Value)
            {
                string validationString = string.Empty;

                switch ((MessageTypeValidation)validation.Value)
                {
                    case MessageTypeValidation.None:
                        validationString = "NONE";
                        break;
                    case MessageTypeValidation.Xml:
                        validationString = "WELL_FORMED_XML";
                        break;
                    case MessageTypeValidation.Empty:
                        validationString = "EMPTY";
                        break;
                    case MessageTypeValidation.XmlSchemaCollection:
                        {
                            //The validation schema collection is a must have, thus getting its value using
                            //GetPropValue(), which would throw in case it is not set.
                            //On the other hand, Validatoin schema collection schema is optional, and thus 
                            //may not necessarilty set by users.
                            object validationSchemaCollection = GetPropValue("ValidationXmlSchemaCollection");
                            object validationSchemaCollectionSchema = GetPropValueOptional("ValidationXmlSchemaCollectionSchema");

                            StringBuilder b = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                            b.AppendFormat(SmoApplication.DefaultCulture, "VALID_XML WITH SCHEMA COLLECTION ");

                            if (null == validationSchemaCollectionSchema)
                            {
                                b.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(validationSchemaCollection.ToString()));
                            }
                            else
                            {
                                b.AppendFormat(SmoApplication.DefaultCulture, "[{0}].[{1}]", SqlBraket(validationSchemaCollectionSchema.ToString()),
                                    SqlBraket(validationSchemaCollection.ToString()));
                            }

                            validationString = b.ToString();

                        }
                        break;

                }

                sb.AppendFormat(SmoApplication.DefaultCulture, " VALIDATION = {0}", validationString);
            }

            queries.Add(sb.ToString());

        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersion);

            ScriptMessageType(queries, sp, true);
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptMessageType(queries, sp, false);
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
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_MESSAGE_TYPE, "", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP MESSAGE TYPE {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

    }


}


