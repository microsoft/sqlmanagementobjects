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
    public partial class ServiceRoute : BrokerObjectBase, IExtendedProperties, Cmn.ICreatable,
        Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists
    {
        internal ServiceRoute(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "ServiceRoute";
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
                CREATE ROUTE route-name 
                [  AUTHORIZATION owner_name ]
                WITH 
                    [SERVICE_NAME = �service-name�, ]
                    [ BROKER_INSTANCE = �broker-instance� , ] 
                    [ LIFETIME = route_lifetime , ] 
                    ADDRESS = �next-hop-address�
                    [ , MIRROR_ADDRESS = �mirror-address� ]
                                
                ALTER ROUTE route-name 
                WITH 
                    [ SERVICE_NAME = �service-name�, ]
                    [ BROKER_INSTANCE = �broker-instance� , ] 
                    [ LIFETIME = route_lifetime , ] 
                    [ ADDRESS = �next-hop-address� ]
                    [ , MIRROR_ADDRESS = �mirror-address� ]
            */

            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            if (bCreate && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SERVICE_ROUTE, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            string s = String.Empty;
            sb.AppendFormat(SmoApplication.DefaultCulture,  "{0} ROUTE {1} ",  bCreate?"CREATE" : "ALTER", FormatFullNameForScripting(sp) );
            if (bCreate && sp.IncludeScripts.Owner)
            {
                s = (string)this.GetPropValueOptional("Owner");
                if (null != s && s.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "  AUTHORIZATION [{0}] ", SqlBraket(s));
                }
            }

            bool bWithAdded = false;

            s = (string)this.GetPropValueOptional("RemoteService");
            if (null != s && s.Length > 0)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "  WITH  SERVICE_NAME  = {0} ", MakeSqlString(s) );
                bWithAdded = true;
            }

            s = (string)this.GetPropValueOptional("BrokerInstance");
            if (null != s && s.Length > 0)
            {
                if( bWithAdded )
                {
                    sb.Append(", ");
                }
                else
                {
                    sb.Append(" WITH ");
                    bWithAdded = true;
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, " BROKER_INSTANCE  = {0} ", MakeSqlString( s ) );
            }
        
            Property p = Properties.Get("ExpirationDate");
            if (null != p.Value)
            {
                DateTime expirationDate = (DateTime)p.Value;
                if (expirationDate != DateTime.MinValue) // make sure expiration date is realistic
                {
                    if (bWithAdded)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        sb.Append(" WITH ");
                        bWithAdded = true;
                    }
                    /*Bug 154631 , server stores datetime value in GMT and we need to convert
                    it to local time value,because T-SQL expects timeperiod. */

                    TimeSpan lifetimeSpan = expirationDate.ToLocalTime() - DateTime.Now;
                    sb.AppendFormat(SmoApplication.DefaultCulture, " LIFETIME  = {0} ", Convert.ToInt32(lifetimeSpan.TotalSeconds));
                }
            }

            if(bCreate)
            {
                //Will throw if property is not set
                s = (string)GetPropValue("Address");
            }
            else
            {
                s = (string)GetPropValueOptional("Address");
            }

            if (null != s && s.Length > 0)
            {
                if( bWithAdded )
                {
                    sb.Append(", ");
                }
                else
                {
                    sb.Append(" WITH ");
                    bWithAdded = true;
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " ADDRESS  = {0} ", MakeSqlString( s ) );
            }

            s = (string)GetPropValueOptional("MirrorAddress");
            p = Properties.Get("MirrorAddress");
            if (null != s && s.Length > 0)
            {
                if (bWithAdded)
                {
                    sb.Append(", ");
                }
                else
                {
                    sb.Append(" WITH ");
                    bWithAdded = true;
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " MIRROR_ADDRESS  = {0} ", MakeSqlString( s ) );
            }

            // add the ddl to create the object
            queries.Add( sb.ToString() );
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            GetDDL( queries, sp, true );
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            if( IsObjectDirty() )
            {
                GetDDL( queries, sp, false );
            }

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
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SERVICE_ROUTE, "", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP ROUTE {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
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


