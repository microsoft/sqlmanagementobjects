// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Text;

using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class AlertSystem : AgentObjectBase, Cmn.IAlterable, IScriptable
    {
        internal AlertSystem(JobServer parentsrv, ObjectKeyBase key, SqlSmoState state) : 
            base(key, state)
        {
            // even though we called with the parent collection of the column, we will 
            // place the ServiceBroker under the right collection
            singletonParent = parentsrv as JobServer;
            
            // WATCH OUT! we are setting the m_server value here, because ServiceBroker does
            // not live in a collection, but directly under the Database
            SetServerObject( parentsrv.GetServerObject());
            m_comparer = parentsrv.Parent.Databases["msdb"].StringComparer;
        }

        protected internal override string CollationDatabaseInServer => "msdb";

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public JobServer Parent
        {
            get 
            {
                CheckObjectState();
                return singletonParent as JobServer;
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        internal protected override string GetDBName()
        {
            return "msdb";
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "AlertSystem";
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        private void ScriptProperties(StringCollection queries, ScriptingPreferences sp)
        {
            Initialize(true);

            StringBuilder updateQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            updateQuery.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_MSsetalertinfo ");
            int count = 0;
            GetStringParameter(updateQuery, sp, "FailSafeOperator", "@failsafeoperator=N'{0}'", ref count);

            GetEnumParameter(updateQuery, sp, "NotificationMethod", "@notificationmethod={0}", typeof(NotifyMethods), ref count);

            GetStringParameter(updateQuery, sp, "ForwardingServer", "@forwardingserver=N'{0}'", ref count);
            GetStringParameter(updateQuery, sp, "PagerToTemplate", "@pagertotemplate=N'{0}'", ref count);
            GetStringParameter(updateQuery, sp, "PagerCCTemplate", "@pagercctemplate=N'{0}'", ref count);
            GetStringParameter(updateQuery, sp, "PagerSubjectTemplate", "@pagersubjecttemplate=N'{0}'", ref count);
            GetBoolParameter(updateQuery, sp, "IsForwardedAlways", "@forwardalways={0}", ref count);
            GetBoolParameter(updateQuery, sp, "PagerSendSubjectOnly", "@pagersendsubjectonly={0}", ref count);
            GetParameter(updateQuery, sp, "ForwardingSeverity", "@forwardingseverity={0}", ref count);
            GetStringParameter(updateQuery, sp, "FailSafeEmailAddress", "@failsafeemailaddress=N'{0}'", ref count);
            GetStringParameter(updateQuery, sp, "FailSafePagerAddress", "@failsafepageraddress=N'{0}'", ref count);
            GetStringParameter(updateQuery, sp, "FailSafeNetSendAddress", "@failsafenetsendaddress=N'{0}'", ref count);

            if (count > 0)
            {
                queries.Add(updateQuery.ToString());
            }
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

    }

}
