// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class TargetServerGroup : AgentObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IRenamable
    {
        internal TargetServerGroup(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "TargetServerGroup";
            }
        }


        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_TARGETSERVERGROUP,
                    "NOT",
                    FormatFullNameForScripting(sp, false));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_targetservergroup @name=N'{0}'", SqlString(this.Name));

            queries.Add(sb.ToString());
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
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_TARGETSERVERGROUP,
                    "",
                    FormatFullNameForScripting(sp, false));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_delete_targetservergroup @name=N'{0}'", SqlString(this.Name));
            queries.Add(sb.ToString());
        }

        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_TARGETSERVERGROUP,
                    "",
                    FormatFullNameForScripting(sp, false));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_update_targetservergroup @name=N'{0}', @new_name=N'{1}'",
                                    SqlString(this.Name), SqlString(newName));

            queries.Add(sb.ToString());
        }

        /// <summary>
        /// The AddMemberServer method assigns target server (Tsx) group 
        /// membership to the target server specified.
        /// </summary>
        /// <param name="srvname"></param>
        public void AddMemberServer(string srvname)
        {
            try
            {
                if (null == srvname)
                {
                    throw new ArgumentNullException("srvname");
                }

                StringCollection coll = new StringCollection();

                coll.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_targetsvrgrp_member @group_name=N'{0}', @server_name=N'{1}'",
                                    SqlString(this.Name), SqlString(srvname)));

                this.ExecutionManager.ExecuteNonQuery(coll);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddMemberServer, this, e);
            }
        }

        /// <summary>
        /// The EnumMemberServers method returns a NameList object that enumerates 
        /// the member target servers (TSXs) of the multiserver administration, 
        /// Tsx server group referenced.
        /// </summary>
        /// <returns></returns>
        public TargetServer[] EnumMemberServers()
        {
            try
            {
                Request req = new Request(this.Urn + "/Member", new string[] { "Urn" });
                DataTable dtresult = this.ExecutionManager.GetEnumeratorData(req);

                TargetServer[] retarray = new TargetServer[dtresult.Rows.Count];
                int idx = 0;
                foreach (DataRow dr in dtresult.Rows)
                {
                    retarray[idx++] = (TargetServer)GetServerObject().GetSmoObject((Urn)(string)dr["Urn"]);
                }

                return retarray;
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumMemberServers, this, e);
            }
        }

        /// <summary>
        /// The DropMemberServer method drops the indicated multiserver 
        /// administration target server (Tsx) from the group referenced.
        /// </summary>
        /// <param name="srvname"></param>
        public void RemoveMemberServer(string srvname)
        {
            try
            {
                if (null == srvname)
                {
                    throw new ArgumentNullException("srvname");
                }

                StringCollection coll = new StringCollection();

                coll.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_delete_targetsvrgrp_member @group_name=N'{0}', @server_name=N'{1}'",
                                    SqlString(this.Name), SqlString(srvname)));

                this.ExecutionManager.ExecuteNonQuery(coll);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveMemberServer, this, e);
            }
        }

        /// <summary>
        /// Generate object creation script using default scripting options
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }
    }
}



