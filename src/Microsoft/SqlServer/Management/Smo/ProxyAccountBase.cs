// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class ProxyAccount : AgentObjectBase, Cmn.IAlterable, Cmn.ICreatable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IScriptable
    {
        
        internal ProxyAccount(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "ProxyAccount";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            //90 server
            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_PROXY,
                    "NOT",
                    FormatFullNameForScripting(sp, false));
            }

            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC msdb.dbo.sp_add_proxy @proxy_name=N'{0}',",
                                SqlString(this.Name));

            string credentialName = Properties["CredentialName"].Value as System.String;
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                                "@credential_name=N'{0}'", SqlString(credentialName));

            int count = 2;
            GetBoolParameter(createQuery, sp, "IsEnabled", "@enabled={0}", ref count);
            GetStringParameter(createQuery, sp, "Description", "@description=N'{0}'", ref count);

            queries.Add(createQuery.ToString());

            /*Enumeration of Logins,Server Roles etc. can be done on already created Proxy only*/
            if (this.State != SqlSmoState.Creating)
            {
                DataTable dt = this.EnumSubSystems();
                foreach (DataRow dr in dt.Rows)
                {
                    createQuery.Length = 0;
                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_grant_proxy_to_subsystem @proxy_name={0}, @subsystem_id={1}",
                             MakeSqlString(this.Name), Enum.Format(typeof(AgentSubSystem), Enum.Parse(typeof(AgentSubSystem), dr["Name"].ToString(), true), "d"));
                    queries.Add(createQuery.ToString());
                }

                dt = this.EnumLogins();
                foreach (DataRow dr in dt.Rows)
                {
                    createQuery.Length = 0;
                    Diagnostics.TraceHelper.Assert(!(string.IsNullOrEmpty(dr["Name"].ToString())), "Invalid login name");
                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_grant_login_to_proxy @proxy_name={0}, @login_name={1}",
                            MakeSqlString(this.Name), MakeSqlString(dr["Name"].ToString()));
                    queries.Add(createQuery.ToString());
                }

                dt = this.EnumServerRoles();
                foreach (DataRow dr in dt.Rows)
                {
                    createQuery.Length = 0;
                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                                        "EXEC msdb.dbo.sp_grant_login_to_proxy @proxy_name={0}, @fixed_server_role={1}",
                                        MakeSqlString(this.Name), MakeSqlString(dr["Name"].ToString()));
                    queries.Add(createQuery.ToString());
                }

                dt = this.EnumMsdbRoles();
                foreach (DataRow dr in dt.Rows)
                {
                    createQuery.Length = 0;
                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                        "EXEC msdb.dbo.sp_grant_login_to_proxy @proxy_name={0}, @msdb_role={1}",
                        MakeSqlString(this.Name), MakeSqlString(dr["Name"].ToString()));
                    queries.Add(createQuery.ToString());
                }
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);


            statement.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_update_proxy @proxy_name=N'{0}',", SqlString(this.Name));

            string credentialName = Properties["CredentialName"].Value as System.String;
            statement.AppendFormat(SmoApplication.DefaultCulture,
                            "@credential_name=N'{0}'", SqlString(credentialName));

            int count = 1;
            GetBoolParameter(statement, sp, "IsEnabled", "@enabled={0}", ref count);
            GetStringParameter(statement, sp, "Description", "@description=N'{0}'", ref count);

            queries.Add(statement.ToString());
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

            //90 server
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_PROXY,
                    "",
                    FormatFullNameForScripting(sp, false));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_delete_proxy @proxy_name=N'{0}'",
                            SqlString(this.Name));
            queries.Add(sb.ToString());

        }
        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            StringBuilder renameQuery = new StringBuilder( Globals.INIT_BUFFER_SIZE );

            renameQuery.AppendFormat(SmoApplication.DefaultCulture,  
                                    "EXEC msdb.dbo.sp_update_proxy @proxy_name=N'{0}', @new_name=N'{1}'", 
                                    SqlString(this.Name),
                                    SqlString(newName));
        
            queries.Add( renameQuery.ToString() );
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

        /// <summary>
        /// Adds proxy-subsystem association
        /// </summary>
        public void AddSubSystem( AgentSubSystem subSystem )
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();
                    
                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, 
                            "EXEC msdb.dbo.sp_grant_proxy_to_subsystem @proxy_name=N'{0}', @subsystem_id={1}", 
                            SqlString(this.Name), Enum.Format(typeof(AgentSubSystem), subSystem, "d"));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddSubSystems, this, e);
            }
        }
        
        /// <summary>
        /// Removes proxy-subsystem association
        /// </summary>
        public void RemoveSubSystem( AgentSubSystem subSystem )
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();
                    
                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, 
                    "EXEC msdb.dbo.sp_revoke_proxy_from_subsystem @proxy_name=N'{0}', @subsystem_id={1}", 
                    SqlString(this.Name), Enum.Format(typeof(AgentSubSystem), subSystem, "d"));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveSubSystems, this, e);
            }
        }
        
        /// <summary>
        /// Retrieves subsystems associated with this proxy
        /// </summary>
        public DataTable EnumSubSystems()
        {
            try
            {
                ThrowIfBelowVersion90();
                Request req = new Request(this.Urn + "/AgentSubSystem");
                return this.ExecutionManager.GetEnumeratorData( req );

            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumSubSystems, this, e);
            }
        }


        /// <summary>
        /// Adds proxy-login association
        /// </summary>
        public void AddLogin( System.String loginName )	
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();
                    
                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, 
                        "EXEC msdb.dbo.sp_grant_login_to_proxy @proxy_name=N'{0}', @login_name={1}", 
                        SqlString(this.Name), MakeSqlString(loginName));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddLoginToProxyAccount, this, e);
            }
        }
        
        private void RemovePrincipal(System.String principalName, string exceptionString)	
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();
                    
                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, 
                    "EXEC msdb.dbo.sp_revoke_login_from_proxy @proxy_name=N'{0}', @name={1}", 
                    SqlString(this.Name), MakeSqlString(principalName));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(exceptionString, this, e);
            }

        }

        /// <summary>
        /// Removes proxy-Login association
        /// </summary>
        public void RemoveLogin(System.String loginName)	
        {
            RemovePrincipal(loginName, ExceptionTemplates.RemoveLoginFromProxyAccount);
        }

        /// <summary>
        /// Retrieves logins associated with this proxy
        /// </summary>
        public DataTable EnumLogins()	
        {
            try 
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData( new Request( this.Urn + "/ProxyAccountPrincipal[@Flag=0]") );
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumLoginsOfProxyAccount, this, e);
            }
        }
        
        /// <summary>
        /// Adds proxy-server role association
        /// </summary>
        public void AddServerRole(System.String serverRoleName)	
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();
                    
                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, 
                    "EXEC msdb.dbo.sp_grant_login_to_proxy @proxy_name={0}, @fixed_server_role={1}", 
                    MakeSqlString(this.Name), MakeSqlString(serverRoleName));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddServerRoleToProxyAccount, this, e);
            }
        }

        /// <summary>
        /// Removes proxy-server role association
        /// </summary>
        public void RemoveServerRole(System.String serverRoleName)
        {
            RemovePrincipal(serverRoleName, ExceptionTemplates.RemoveServerRoleFromProxyAccount);
        }

        /// <summary>
        /// Retrieves server roles associated with this proxy
        /// </summary>
        public DataTable EnumServerRoles()	
        {
            try 
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData( new Request( this.Urn + "/ProxyAccountPrincipal[@Flag=1]") );
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumServerRolesOfProxyAccount, this, e);
            }
        }

        /// <summary>
        /// Adds proxy-msdb role association
        /// </summary>
        public void AddMsdbRole(System.String msdbRoleName)	
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();
                    
                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, 
                    "EXEC msdb.dbo.sp_grant_login_to_proxy @proxy_name={0}, @msdb_role={1}", 
                    MakeSqlString(this.Name), MakeSqlString(msdbRoleName));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddMSDBRoleToProxyAccount, this, e);
            }
        }

        /// <summary>
        /// Removes proxy-msdb role association
        /// </summary>
        public void RemoveMsdbRole(System.String msdbRoleName)	
        {
            RemovePrincipal(msdbRoleName, ExceptionTemplates.RemoveMSDBRoleFromProxyAccount);
        }

        /// <summary>
        /// Retrieves msdb roles associated with this proxy
        /// </summary>
        public DataTable EnumMsdbRoles()	
        {
            try 
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData( new Request( this.Urn + "/ProxyAccountPrincipal[@Flag=2]") );
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumMSDBRolesOfProxyAccount, this, e);
            }
        }

        /// <summary>
        /// Reassign job steps that refer to current proxy account name to a new proxy account name
        /// supported on SQL 11 onwards - depends on stored proc 'sp_reassign_proxy' in msdb
        /// </summary>
        /// <param name="targetProxyAccountName"></param>
        /// <returns></returns>
        public void Reassign(string targetProxyAccountName)
        {
            ThrowIfBelowVersion110();
            CheckObjectState(true);

            StringCollection query = new StringCollection();
            // Build query to reassign proxy account
            StringBuilder statement = new StringBuilder();
            statement.AppendFormat(SmoApplication.DefaultCulture,
                "EXEC msdb.dbo.sp_reassign_proxy @current_proxy_name={0}, @target_proxy_name={1}",
                MakeSqlString(this.Name),
                MakeSqlString(targetProxyAccountName));
            query.Add(statement.ToString());

            this.ExecutionManager.ExecuteNonQuery(query);
            return;
        }
       
    }

}


