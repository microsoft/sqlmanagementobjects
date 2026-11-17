// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Broker
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.Broker.BrokerLocalizableResources", true)]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    public partial class BrokerPriority : BrokerObjectBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        internal BrokerPriority(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "BrokerPriority";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion100(sp.TargetServerVersion);

            // retrieve the DDL 
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

        private void GetDDL(StringCollection queries, ScriptingPreferences sp, bool bCreate)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            /*
                CREATE BROKER PRIORITY ConversationPriorityName
                FOR CONVERSATION
                [ SET ( [ CONTRACT_NAME = {ContractName | ANY } ]
                    [ [ , ] LOCAL_SERVICE_NAME = {LocalServiceName | ANY } ]
                    [ [ , ] REMOTE_SERVICE_NAME = {'RemoteServiceName' | ANY } ]
                    [ [ , ] PRIORITY_LEVEL = {PriorityValue | DEFAULT } ]
                   )
                ]
                [;]

                ALTER BROKER PRIORITY ConversationPriorityName
                FOR CONVERSATION
                { SET ( [ CONTRACT_NAME = {ContractName | ANY } ]
                        [ [ , ] LOCAL_SERVICE_NAME = {LocalServiceName | ANY } ]
                        [ [ , ] REMOTE_SERVICE_NAME = {'RemoteServiceName' | ANY } ]
                        [ [ , ] PRIORITY_LEVEL = { PriorityValue | DEFAULT } ]
                              )
                }
                [;]

            */

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            // retrieve full scripting name
            string sFullScriptingName = FormatFullNameForScripting(sp);

            if (bCreate && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_BROKER_PRIORITY, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} BROKER PRIORITY {1} {2}", bCreate ? "CREATE" : "ALTER", sFullScriptingName, Globals.newline);
            sb.AppendFormat(SmoApplication.DefaultCulture, "FOR CONVERSATION");
            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} SET {1}", Globals.space, Globals.LParen);

            String name = String.Empty;

            name = (string)GetPropValueOptional("ContractName");
            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} CONTRACT_NAME = {1} {2}", Globals.newline, String.IsNullOrEmpty(name) ? Scripts.ANY : MakeSqlBraket(name), Globals.comma);

            name = (string)GetPropValueOptional("LocalServiceName");
            sb.AppendFormat(SmoApplication.DefaultCulture, "{0}  LOCAL_SERVICE_NAME = {1} {2}", Globals.newline, String.IsNullOrEmpty(name) ? Scripts.ANY : MakeSqlBraket(name), Globals.comma);

            name = (string)GetPropValueOptional("RemoteServiceName");
            sb.AppendFormat(SmoApplication.DefaultCulture, "{0}  REMOTE_SERVICE_NAME = {1} {2}", Globals.newline, String.IsNullOrEmpty(name) ? Scripts.ANY : MakeSqlString(name), Globals.comma);


            object p = GetPropValueOptional("PriorityLevel");
            sb.AppendFormat(SmoApplication.DefaultCulture, "{0}  PRIORITY_LEVEL = {1} {2} ", Globals.newline, (p == null) ? Scripts.DEFAULT : ((System.Byte)p).ToString(SmoApplication.DefaultCulture), Globals.RParen);

            // add the ddl to create the object
            queries.Add(sb.ToString());
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion100(sp.TargetServerVersion);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_BROKER_PRIORITY,
                    "",
                    FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP BROKER PRIORITY {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion100(sp.TargetServerVersion);
            if (IsObjectDirty())
            {
                GetDDL(queries, sp, false);
            }
        }
   

    }


}


