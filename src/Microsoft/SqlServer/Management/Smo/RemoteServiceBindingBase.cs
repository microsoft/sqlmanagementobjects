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
    public partial class RemoteServiceBinding : BrokerObjectBase, IExtendedProperties,
        Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists
    {
        internal RemoteServiceBinding(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "RemoteServiceBinding";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        private void GetDDL(StringCollection queries, ScriptingPreferences sp, bool bCreate)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            /*
                CREATE REMOTE SERVICE BINDING binding-name
                [  AUTHORIZATION owner_name ] 
                TO SERVICE 'remote-service-name'
                WITH USER = user-name [ , ANONYMOUS = { ON | OFF } ] 
				
                ALTER REMOTE SERVICE BINDING binding-name
                    WITH  [ USER = user-name ] [ , ANONYMOUS = { ON | OFF } ] 

            */
            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            if (bCreate && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_REMOTE_SERVICE_BINDING, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} REMOTE SERVICE BINDING {1} ", bCreate ? "CREATE" : "ALTER", FormatFullNameForScripting(sp));
            if (bCreate && sp.IncludeScripts.Owner)
            {
                string s = (string)this.GetPropValueOptional("Owner");
                if (null != s && s.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "  AUTHORIZATION [{0}] ", SqlBraket(s));
                }
            }

            bool bWithAdded = false;

            if (bCreate)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, " TO SERVICE {0} ", MakeSqlString(Properties["RemoteService"].Value.ToString()));

                sb.AppendFormat(SmoApplication.DefaultCulture, " WITH USER = [{0}] ", SqlBraket((string)GetPropValue("CertificateUser")));
                bWithAdded = true;
            }
            else
            {
                //Alter
                string s = (string)this.GetPropValueOptional("CertificateUser");
                if (null != s && s.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " WITH USER = [{0}] ", SqlBraket(s));
                    bWithAdded = true;
                }

            }

            object pbAnonimous = this.GetPropValueOptional("IsAnonymous");
            if (null != pbAnonimous)
            {
                if (bWithAdded)
                {
                    sb.Append(Globals.comma);
                    sb.Append(Globals.space);
                }
                else
                {
                    sb.Append(" WITH ");
                    bWithAdded = true;
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " ANONYMOUS = {0} ", (bool)pbAnonimous ? Globals.On : Globals.Off);
            }

            // add the ddl to create the object
            queries.Add(sb.ToString());
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersion);

            GetDDL(queries, sp, true);
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
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_REMOTE_SERVICE_BINDING, "", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP REMOTE SERVICE BINDING {0}", FormatFullNameForScripting(sp));

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
    }

}



