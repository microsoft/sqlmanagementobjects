// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class JobServer : SqlSmoObject, Cmn.IAlterable, IScriptable
    {
        internal JobServer(Server parentsrv, ObjectKeyBase key, SqlSmoState state) : 
            base(key, state)
        {
            // even though we called with the parent collection of the column, we will 
            // place the JobServer under the right collection
            singletonParent = parentsrv as Server;
            
            // WATCH OUT! we are setting the m_server value here, because JobServer does
            // not live in a collection, but directly under the Database
            SetServerObject( parentsrv.GetServerObject());
            m_comparer = parentsrv.Databases["msdb"].StringComparer;

            jobCategories = null;
            alertCategories = null;
            operatorCategories = null;
            alertSystem = null;
            alerts = null;
            operators = null;
            targetServers = null;
            targetServerGroups = null;
            jobs = null;
            sharedSchedules = null;
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get 
            {
                CheckObjectState();
                return singletonParent as Server;
            }
        }

        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]        
        public string Name
        {
            get
            {
                return ((SimpleObjectKey)key).Name;
            }
        }
        
        /// <summary>
        /// The GetJobByID method returns a SQL-DMO Job object referencing the SQL Server Agent job identified by the specified job identifier.
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public Job GetJobByID(Guid jobId)
        {
            try
            {
                foreach(Job j in this.Jobs)
                {
                    if( jobId == j.JobID )
                    {
                        return j;
                    }
                }
                return null;
            }
            catch(Exception e)
            {
                FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.GetJobByID, this, e);
            }
        }

        /// <summary>
        /// The RemoveJobByID method drops the SQLServerAgent job identified and removes the referencing Job object from the Jobs collection.
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public void RemoveJobByID(Guid jobId)
        {
            try
            {

                foreach(Job j in this.Jobs)
                {
                    if( jobId == j.JobID )
                    {
                        j.Drop();
                        return;
                    }
                }
            }
            catch(Exception e)
            {
                FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.RemoveJobByID, this, e);
            }
        }

        /// <summary>
        /// The RemoveJobsByLogin method drops all SQLServerAgent jobs owned by the login identified and removes the referencing Job objects from the Jobs collection.
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public void RemoveJobsByLogin(string login)
        {
            if( null == login )
            {
                throw new ArgumentNullException(nameof(login));
            }
            try
            {
                var sb = new StringBuilder();
                sb.Append("EXEC msdb.dbo.sp_manage_jobs_by_login @action = N'DELETE', @current_owner_login_name = ");
                sb.Append(SqlSmoObject.MakeSqlString(login));

                this.ExecutionManager.ExecuteNonQuery(sb.ToString());

                this.Jobs.Refresh();
            }
            catch(Exception e)
            {
                FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.RemoveJobsByLogin, this, e);
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

        internal protected override string CollationDatabaseInServer => GetDBName();

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return nameof(JobServer);
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        private void ScriptProperties(StringCollection queries, ScriptingPreferences sp )
        {
            var statement = new StringBuilder( Globals.INIT_BUFFER_SIZE );
            statement.Append("EXEC msdb.dbo.sp_set_sqlagent_properties ");

            var count = 0;
            GetBoolParameter( statement, sp, "SqlServerRestart", "@sqlserver_restart={0}", ref count);
            GetBoolParameter(statement, sp, "SqlAgentRestart", "@monitor_autostart={0}", ref count);
            GetBoolParameter(statement, sp, "SqlAgentAutoStart", "@auto_start={0}", ref count);
            GetParameter(statement, sp, "MaximumHistoryRows", "@jobhistory_max_rows={0}", ref count);
            GetParameter(statement, sp, "MaximumJobHistoryRows", "@jobhistory_max_rows_per_job={0}", ref count);
            GetStringParameter(statement, sp, "ErrorLogFile", "@errorlog_file=N'{0}'", ref count);
            GetEnumParameter(statement, sp, "AgentLogLevel", "@errorlogging_level={0}", 
                             typeof(AgentLogLevels), ref count);
            GetStringParameter(statement, sp, "NetSendRecipient", "@error_recipient=N'{0}'", ref count);
            GetParameter(statement, sp, "AgentShutdownWaitTime", "@job_shutdown_timeout={0}", ref count);
            GetStringParameter(statement, sp, "SqlAgentMailProfile", "@email_profile=N'{0}'", ref count);
            GetBoolParameter( statement, sp, "SaveInSentFolder", "@email_save_in_sent_folder={0}", ref count);
            GetBoolParameter( statement, sp, "WriteOemErrorLog", "@oem_errorlog={0}", ref count);
            GetBoolParameter( statement, sp, "IsCpuPollingEnabled", "@cpu_poller_enabled={0}", ref count);
            GetParameter(statement, sp, "IdleCpuPercentage", "@idle_cpu_percent={0}", ref count);
            GetParameter(statement, sp, "IdleCpuDuration", "@idle_cpu_duration={0}", ref count);
            GetParameter(statement, sp, "LoginTimeout", "@login_timeout={0}", ref count);
            GetStringParameter(statement, sp, "LocalHostAlias", "@local_host_server=N'{0}'", ref count);

            if (this.ServerVersion.Major >= 9 &&
                sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                GetBoolParameter(statement, sp, "ReplaceAlertTokensEnabled", "@alert_replace_runtime_tokens={0}", ref count);
            }

            // DatabaseMail properties 
            // if Server version is sql 11 and target scripting version is sql 11 and greater use sp_set_sqlagent_properties
            // for all other scenarios, generate script based on xp_instance_regwrite calls
            if (this.ServerVersion.Major >= 11 &&
               sp.TargetServerVersion >= SqlServerVersion.Version110)
            {
                // If target server is SQL 11 and script version is also is SQL 11 and greater then rely on sp_set_sqlagent_properties to set  database mail settings
                GetStringParameter(statement, sp, "DatabaseMailProfile", "@databasemail_profile=N'{0}'", ref count);
                GetEnumParameter(statement, sp, "AgentMailType", "@use_databasemail={0}", typeof(AgentMailType), ref count);
            }
            else
            {
                // if yukon to pre-SQL 11 then use xp_regwrite calls
                var prop = Properties.Get("AgentMailType");
                if ((null != prop.Value) && (prop.Dirty || !sp.ScriptForAlter))
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                                @"EXEC master.dbo.xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'SOFTWARE\Microsoft\MSSQLServer\SQLServerAgent', N'UseDatabaseMail', N'REG_DWORD', {0}",
                                                Enum.Format(typeof(AgentMailType), (AgentMailType)prop.Value, "d")));

                }

                prop = Properties.Get("DatabaseMailProfile");
                if ((null != prop.Value) && (prop.Dirty || !sp.ScriptForAlter))
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                                @"EXEC master.dbo.xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'SOFTWARE\Microsoft\MSSQLServer\SQLServerAgent', N'DatabaseMailProfile', N'REG_SZ', N'{0}'",
                                                SqlString(prop.Value.ToString())));
                }
            }
            

            if (count > 0)
            {
                queries.Add(statement.ToString());
            }
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

        // this is going to eliminate "Name" from any fields and OrderBy clause
        // because JobServer object does not have such a property
        protected override bool ImplInitialize(string[] fields, OrderBy[] orderby)
        {
            var newFields = fields;
            var newOrderBy = orderby;

            var foundName = false;
            if( null != fields )
            {
                foreach (var s in fields)
                {
                    if (s == "Name")
                    {
                        foundName = true;
                        break;
                    }
                }

                if( foundName && 1 < fields.Length )
                {
                    newFields = new string[fields.Length - 1];
                    var count = 0;
                    foreach (var s in fields)
                    {
                        if (s != "Name")
                        {
                            newFields[count++] = s;
                        }
                    }
                }
            }

            foundName = false;
            if( null != orderby )
            {
                foreach (var ob in orderby)
                {
                    if (ob.Field == "Name")
                    {
                        foundName = true;
                    }
                }

                if( foundName && 1 < orderby.Length )
                {
                    newOrderBy = new OrderBy[orderby.Length - 1];
                    var count = 0;
                    foreach (var ob in orderby)
                    {
                        if (ob.Field != "Name")
                        {
                            newOrderBy[count++] = ob;
                        }
                    }
                }
            }
            // calls into the base function, after with the input parameters cleaned
            return base.ImplInitialize( newFields, newOrderBy );
        }

        /// <summary>
        /// Tests the mail profile. Returns true when a failure occurs. In that case the 
        /// errorMessage will contain the related error message.
        /// </summary>
        public void TestMailProfile( string profileName )
        {
            ThrowIfAboveVersion100(); // SQLMail was deleted in SQL11

            try{
                if( null == profileName || 0 == profileName.Length )
                {
                    profileName = "Outlook"; // default profile name
                }

                this.ExecutionManager.ExecuteNonQuery(
                                                      string.Format(SmoApplication.DefaultCulture, "EXECUTE master.dbo.xp_sqlagent_notify N'N',null,null,null,N'M',N'{0}' ",SqlString(profileName)));
            }
            catch(Exception e)
            {
                FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.TestMailProfile, this, e);
            }
        }

        /// <summary>
        /// Tests the net send. 
        /// </summary>
        [Obsolete]
        public void TestNetSend()
        {
            try
            {
                var NetSendRecipient = Properties["NetSendRecipient"].Value as string;
                if (0 == NetSendRecipient.Length) 
                {
                    throw new PropertyNotSetException("NetSendRecipient");
                }
                this.ExecutionManager.ExecuteNonQuery(string.Format(SmoApplication.DefaultCulture, "EXECUTE master.dbo.xp_sqlagent_notify N'N',null,null,null,N'N',N'{0}' ",SqlString(NetSendRecipient)));
            }
            catch (Exception e)
            {
                FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.TestNetSend, this, e);
            }
        }

        /// <summary>
        /// Purges the entire job history.
        /// </summary>
        public void PurgeJobHistory()
        {
            try
            {
                this.ExecutionManager.ExecuteNonQuery( "EXEC msdb.dbo.sp_purge_jobhistory");
            }
            catch (Exception e)
            {
                FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.PurgeJobHistory, this, e);
            }
        }

        /// <summary>
        /// Sets the SQL Server Account and Password that is used to login to SQL Server.
        /// </summary>
        [Obsolete]
        public void SetHostLoginAccount(string loginName, string password)
        {
            // Shiloh only feature
            // check to see if server version is Yukon - throw exception if so
            ThrowIfAboveVersion80();
            if (null == loginName)
            {
                throw new ArgumentNullException(nameof(loginName));
            }

            if (null == password)
            {
                throw new ArgumentNullException(nameof(password));
            }
            try
            {


                var stmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                
                stmt.Append( "declare @arg varbinary(512)");
                stmt.Append(Globals.newline);
                stmt.AppendFormat( SmoApplication.DefaultCulture, 
                                   "set @arg = cast (N'{0}' as varbinary(512))", SqlString(password));
                stmt.Append(Globals.newline);
                stmt.AppendFormat( SmoApplication.DefaultCulture, 
                                   "EXEC msdb.dbo.sp_set_sqlagent_properties @host_login_name=N'{0}', @host_login_password=@arg, @regular_connections = 1",
                                   SqlString(loginName));
                this.ExecutionManager.ExecuteNonQuery( stmt.ToString() );
                if (!this.ExecutionManager.Recording)
                {
                    // update the property in tha bag
                    Properties.Get("HostLoginName").SetValue(loginName);
                }
                stmt.Length = 0;
            }
            catch (Exception e)
            {
                FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.SetHostLoginAccount, this, e);
            }
        }

        /// <summary>
        /// Clears the SQL Server account and use integrated security to login to SQL Server.
        /// </summary>
        [Obsolete]
        public void ClearHostLoginAccount()
        {
            try
            {
                this.ExecutionManager.ExecuteNonQuery( "EXEC msdb.dbo.sp_set_sqlagent_properties @regular_connections = 0");
                if (!this.ExecutionManager.Recording)
                {
                    // update the property in tha bag
                    Properties.Get("HostLoginName").SetValue(string.Empty);
                }
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ClearHostLoginAccount, this, e);
            }
        }

        /// <summary>
        /// Sets the master server account. This is available only for SQL Server 2000.
        /// </summary>
        [Obsolete]
        public void SetMsxAccount(string account, string password)
        {
            try
            {
                ThrowIfBelowVersion80SP3();
                if (this.ServerVersion.Major >= 9)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }
                if( null == account )
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("account"));
                }

                if ( null == password )
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("password"));
                }

                var domainName = new StringBuilder();
                var userName = new StringBuilder();
                ParseAccountName( account, domainName, userName );
                this.ExecutionManager.ExecuteNonQuery( string.Format(
                                                                     SmoApplication.DefaultCulture, 
                                                                     "EXEC master.dbo.xp_sqlagent_msx_account N'SET', N'{0}', N'{1}', N'{2}'", 
                                                                     SqlString( domainName.ToString()), 
                                                                     SqlString(userName.ToString()), 
                                                                     SqlString(password)));
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetMsxAccount, this, e);
            }
        }

        /// <summary>
        /// Sets the master server account credential name that is used to store the MSX account information using sp_msx_set_account.
        /// </summary>
        /// <param name="credentialName"></param>
        public void SetMsxAccount(string credentialName)
        {
            if (null == credentialName)
            {
                throw new ArgumentNullException(nameof(credentialName));
            }
            try
            {
                this.ExecutionManager.ExecuteNonQuery(string.Format(
                                                                    SmoApplication.DefaultCulture,
                                                                    "EXEC msdb.dbo.sp_msx_set_account @credential_name = {0}", MakeSqlString(credentialName)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetMsxAccount, this, e);
            }
        }

        /// <summary>
        /// Clears the master server account and use integrated security 
        /// to login to the master server using sp_msx_set_account. 
        /// </summary>
        public void ClearMsxAccount()
        {
            try
            {
                this.ExecutionManager.ExecuteNonQuery("EXEC msdb.dbo.sp_msx_set_account");
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ClearMsxAccount, this, e);
            }
        }

        private void ParseAccountName(string accountName, StringBuilder domainName, StringBuilder userName)
        {
            var res = accountName.Split(new char[] {'\\'});
            if( res.Length == 2 )
            {
                domainName.Append(res[0]);
                userName.Append(res[1]);
            }
            else if( res.Length == 1 )
            {
                userName.Append(res[0]);
            }
            else
            {
                throw new SmoException(ExceptionTemplates.InvalidAcctName);
            }
        }
 
        /// <summary>
        /// Cycles SQLAgent error log
        /// </summary>
        public void CycleErrorLog()
        {
            try
            {
                this.ExecutionManager.ExecuteNonQuery("EXEC msdb.dbo.sp_cycle_agent_errorlog");
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CycleErrorLog, this, e);
            }
        }


        /// <summary>
        /// Gets the Agent error logs
        /// </summary>
        public DataTable EnumErrorLogs()
        {
            try
            {
                var req = new Request(this.Urn.Value + "/ErrorLog");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumErrorLogs, this, e);
            }
        }

        /// <summary>
        /// Reads the current Agent error log
        /// </summary>
        public DataTable ReadErrorLog()
        {
            try
            {
                // 0 is always the current archive 
                return ReadErrorLog(0);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReadErrorLog, this, e);
            }
        }
        
        /// <summary>
        /// Reads the specified Agent error log
        /// </summary>
        public DataTable ReadErrorLog(int logNumber)
        {
            try
            {
                var req = new Request(string.Format(SmoApplication.DefaultCulture,  "{0}/ErrorLog[@ArchiveNo='{1}']/LogEntry", this.Urn, logNumber ));
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReadErrorLog, this, e);
            }
        }


        private JobCategoryCollection jobCategories;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(JobCategory))]
        public JobCategoryCollection JobCategories
        {
            get 
            {
                CheckObjectState();
                if( null == jobCategories )
                {
                    jobCategories = new JobCategoryCollection(this);
                }
                return jobCategories;
            }
        }
        
        private OperatorCategoryCollection operatorCategories;
        [SfcObject( SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(OperatorCategory))]
        public OperatorCategoryCollection OperatorCategories
        {
            get 
            {
                CheckObjectState();
                if( null == operatorCategories )
                {
                    operatorCategories = new OperatorCategoryCollection(this);
                }
                return operatorCategories;
            }
        }
        
        private AlertCategoryCollection alertCategories;
        [SfcObject( SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(AlertCategory))]
        public AlertCategoryCollection AlertCategories
        {
            get 
            {
                CheckObjectState();
                if( null == alertCategories )
                {
                    alertCategories = new AlertCategoryCollection(this);
                }
                return alertCategories;
            }
        }

        private AlertSystem alertSystem;
        [SfcObject( SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public AlertSystem AlertSystem 
        {
            get 
            {
                CheckObjectState();
                if( null == alertSystem )
                {
                    alertSystem = new AlertSystem( this, new SimpleObjectKey(this.Name), SqlSmoState.Existing);
                }

                return alertSystem;
            }
        }

        private AlertCollection alerts;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Alert))]
        public AlertCollection Alerts 
        {
            get 
            {
                CheckObjectState();
                if( null == alerts )
                {
                    alerts = new AlertCollection( this );
                }

                return alerts;
            }
        }

        private OperatorCollection operators;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Operator))]
        public OperatorCollection Operators 
        {
            get 
            {
                CheckObjectState();
                if( null == operators )
                {
                    operators = new OperatorCollection( this );
                }

                return operators;
            }
        }

        private TargetServerCollection targetServers;
        [SfcObject( SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(TargetServer))]
        public TargetServerCollection TargetServers 
        {
            get 
            {
                CheckObjectState();
                if( null == targetServers )
                {
                    targetServers = new TargetServerCollection( this );
                }

                return targetServers;
            }
        }

        private TargetServerGroupCollection targetServerGroups;
        [SfcObject( SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(TargetServerGroup))]
        public TargetServerGroupCollection TargetServerGroups 
        {
            get 
            {
                CheckObjectState();
                if( null == targetServerGroups )
                {
                    targetServerGroups = new TargetServerGroupCollection( this );
                }

                return targetServerGroups;
            }
        }

        private JobCollection jobs;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Job))]
        public JobCollection Jobs
        {
            get 
            {
                CheckObjectState();
                if( null == jobs )
                {
                    jobs = new JobCollection( this );
                }
                return jobs;
            }
        }

        private JobScheduleCollection<JobServer> sharedSchedules;
        [SfcObject( SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(JobSchedule))]
        public JobScheduleCollection<JobServer> SharedSchedules
        {
            get 
            {
                CheckObjectState();
                ThrowIfBelowVersion90();
                if( null == sharedSchedules )
                {
                    sharedSchedules = new JobScheduleCollection<JobServer>(this)
                    {
                        AcceptDuplicateNames = true
                    };
                }

                return sharedSchedules;
            }
        }

        private ProxyAccountCollection proxyAccounts;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ProxyAccount))]
        public ProxyAccountCollection ProxyAccounts
        {
            get 
            {
                CheckObjectState();
                ThrowIfBelowVersion90();
                if( null == proxyAccounts )
                {
                    proxyAccounts = new ProxyAccountCollection( this );
                }

                return proxyAccounts;
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool SysAdminOnly
        {
            get
            {
                if (this.ServerVersion.Major >= 9) // Property is not supported on Yukon server
                {
                    throw new PropertyCannotBeRetrievedException("SysAdminOnly", this, ExceptionTemplates.ReasonPropertyIsNotSupportedOnCurrentServerVersion);
                }
                return (bool)this.Properties.GetValueWithNullReplacement("SysAdminOnly");
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if( null != alertSystem )
            {
                alertSystem.MarkDroppedInternal();
            }

            if ( null != jobCategories )
            {
                jobCategories.MarkAllDropped();
            }

            if ( null != alertCategories )
            {
                alertCategories.MarkAllDropped();
            }

            if ( null != operatorCategories )
            {
                operatorCategories.MarkAllDropped();
            }

            if ( null != alerts )
            {
                alerts.MarkAllDropped();
            }

            if ( null != operators )
            {
                operators.MarkAllDropped();
            }

            if ( null != targetServers )
            {
                targetServers.MarkAllDropped();
            }

            if ( null != targetServerGroups )
            {
                targetServerGroups.MarkAllDropped();
            }

            if ( null != jobs )
            {
                jobs.MarkAllDropped();
            }

            if ( null != sharedSchedules )
            {
                sharedSchedules.MarkAllDropped();
            }
        }

        /// <summary>
        /// The EnumHistory method returns a DataTable object that enumerates the 
        /// execution history of all jobs.
        /// </summary>
        /// <param name="filter">A JobHistoryFilter object that restricts result 
        /// set membership.</param>
        /// <returns></returns>
        public DataTable EnumJobHistory(JobHistoryFilter filter )
        {
            if (null == filter)
            {
                throw new ArgumentNullException(nameof(filter));
            }
            try
            {
                return this.ExecutionManager.GetEnumeratorData( filter.GetEnumRequest(this) );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobHistory, this, e);
            }
        }

        public DataTable EnumJobHistory( )
        {
            try
            {
                CheckObjectState(true);
                var req = new Request(this.Urn + "/Job/History");
                return this.ExecutionManager.GetEnumeratorData( req );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobHistory, this, e);
            }
        }

        /// <summary>
        /// The EnumJobs method returns a DataTable object that enumerates all 
        /// jobs defined for a server.
        /// </summary>
        /// <param name="filter">A JobFilter object that restricts result set 
        /// membership.</param>
        /// <returns></returns>
        public DataTable EnumJobs(JobFilter filter)
        {
            DataTable jobsTable = null;
            if( null == filter )
            {
                throw new ArgumentNullException(nameof(filter));
            }

            try
            {
                jobsTable = this.ExecutionManager.GetEnumeratorData( filter.GetEnumRequest(this) );

                if( null != jobsTable)
                {
                    // Apply Execution status filter
                    this.FilterJobsByExecutionStatus(filter, ref jobsTable);

                    // Apply job type filter 
                    this.FilterJobsByJobType(filter, ref jobsTable);

                    // Apply subsystems filter
                    this.FilterJobsBySubSystem(filter, ref jobsTable);
                }

            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobs, this, e);
            }

            return jobsTable;
        }

        /// <summary>
        /// Filter Jobs by current Execution status
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobsTable"></param>
        private void FilterJobsByExecutionStatus(JobFilter filter, ref DataTable jobsTable)
        {
            // Apply filter based on execution Status
            // this property is not supported for filtering through SMO enumerator. 
            // Longer term, we need to re-write this querying / filtering  mechanism using better approaches like LINQ & System.Expression
            // filtering to fix the bug# 98285 - JobServer.EnumJobs does not allow JobFilter on CurrentExecutionStatus
            if (filter.currentExecutionStatusDirty)
            {
                var dropList = new List<DataRow>();

                foreach (DataRow dr in jobsTable.Rows)
                {
                    var j = this.Jobs.ItemById((Guid)dr["JobID"]);
                    if (null != j)
                    {
                        if (j.CurrentRunStatus != filter.CurrentExecutionStatus)
                        {
                            dropList.Add(dr);
                        }
                    }
                }

                // remove all the rows that don't match the executionstatus
                foreach (var dr in dropList)
                {
                    jobsTable.Rows.Remove(dr);
                }
            }

        }

        /// <summary>
        /// Filter Jobs by current Job Type
        /// </summary>  
        /// <param name="filter"></param>
        /// <param name="jobsTable"></param>
        /// <returns></returns>
        private void FilterJobsByJobType(JobFilter filter, ref DataTable jobsTable)
        {
            // Apply filter based on Job Type
            // this property is not supported for filtering through SMO enumerator. 
            if (filter.jobTypeDirty)
            {
                var dropList = new List<DataRow>();

                foreach (DataRow dr in jobsTable.Rows)
                {
                    var j = this.Jobs.ItemById((Guid)dr["JobID"]);
                    if (null != j)
                    {
                        if (j.JobType != filter.JobType)
                        {
                            dropList.Add(dr);
                        }
                    }
                }

                // remove all the rows that don't match the job type
                foreach (var dr in dropList)
                {
                    jobsTable.Rows.Remove(dr);
                }
            }

        }

        /// <summary>
        /// Filter Jobs  - Look  if any one of the step has given subsystem type
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobsTable"></param>
        /// <returns></returns>
        private void FilterJobsBySubSystem(JobFilter filter, ref DataTable jobsTable)
        {
            // if we need to filter for steps with a certain subsystem
            // we'll scan the result table and look for steps
            if (filter.stepSubsystemDirty)
            {
                var dropList = new List<DataRow>();

                foreach (DataRow dr in jobsTable.Rows)
                {
                    var j = this.Jobs.ItemById((Guid)dr["JobID"]);
                    var hasStep = false;
                    if (null != j)
                    {
                        foreach (JobStep step in j.JobSteps)
                        {
                            if (step.SubSystem == filter.StepSubsystem)
                            {
                                hasStep = true;
                            }
                        }
                    }
                    // if the job does not have the step then we remove the row
                    if (!hasStep)
                    {
                        dropList.Add(dr);
                    }
                }

                // remove all the rows that don't have that step
                foreach (var dr in dropList)
                {
                    jobsTable.Rows.Remove(dr);
                }
            }

        }

        public DataTable EnumJobs()
        {
            try
            {
                var req = new Request(this.Urn + "/Job");
                return this.ExecutionManager.GetEnumeratorData( req );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobs, this, e);
            }
        }

        /// <summary>
        /// The EnumSubSystems method returns a DataTable object that 
        /// enumerates installed execution subsystems.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumSubSystems()
        {
            try
            {
                var queries = new StringCollection
                {
                    "EXEC msdb.dbo.sp_enum_sqlagent_subsystems"
                };
                return this.ExecutionManager.ExecuteWithResults( queries ).Tables[0];
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumSubSystems, this, e);
            }
        }
         
        /// <summary>
        /// The MSXDefect method ends SQL Server Agent participation in a 
        /// multiserver administration group.
        /// </summary>
        public void MsxDefect()
        {
            try
            {
                var queries = new StringCollection
                {
                    "EXEC msdb.dbo.sp_msx_defect"
                };
                this.ExecutionManager.ExecuteNonQuery( queries );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.MsxDefect, this, e);
            }
        }

        /// <summary>
        /// The MSXDefect method ends SQL Server Agent participation in a 
        /// multiserver administration group.
        /// </summary>
        public void MsxDefect(bool forceDefection)
        {
            try
            {
                var queries = new StringCollection();
                var paramStr = forceDefection ? "1" : "0";
                var query = "EXEC msdb.dbo.sp_msx_defect @forced_defection = " + paramStr;
                queries.Add(query);

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.MsxDefect, this, e);
            }
        }

        /// <summary>
        /// The MSXEnlist method initiates SQL Server Agent participation as a 
        /// target for multiserver administration.
        /// </summary>
        /// <param name="masterServer">String naming a registered instance of 
        /// SQL Server.The instance must be configured as a multiserver 
        /// administration master server.</param>
        /// <param name="location">String documenting the enlisting server's 
        /// location. Used for user assistance only.</param>
        public void MsxEnlist(string masterServer , string location )
        {
            if (null == masterServer)
            {
                throw new ArgumentNullException(nameof(masterServer));
            }

            if (null == location)
            {
                throw new ArgumentNullException(nameof(location));
            }
            try
            {
                var queries = new StringCollection
                {
                    string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_msx_enlist @msx_server_name = N'{0}', @location = N'{1}'",
                                           SqlString(masterServer), SqlString(location))
                };

                this.ExecutionManager.ExecuteNonQuery( queries );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.MsxEnlist, this, e);
            }
        }

        /// <summary>
        /// The PurgeJobHistory method removes system records maintaining 
        /// execution history for all jobs, or those matching the filter 
        /// criteria specified.
        /// </summary>
        /// <param name="filter">A JobHistoryFilter object that constrains 
        /// record removal to those records identified by the criteria set 
        /// in the object.</param>
        public void PurgeJobHistory(JobHistoryFilter filter)
        {
            try
            {
                if( null == filter )
                {
                    throw new ArgumentNullException(nameof(filter));
                }

                var queries = new StringCollection
                {
                    "EXEC msdb.dbo.sp_purge_jobhistory " + filter.GetPurgeFilter()
                };

                this.ExecutionManager.ExecuteNonQuery( queries );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.PurgeJobHistory, this, e);
            }
        }

        /// <summary>
        /// The ReAssignJobsByLogin method changes ownership for any 
        /// SQLServerAgent jobs currently owned by a login.
        /// </summary>
        /// <param name="oldLogin">String that specifies a login currently 
        /// owning jobs.</param>
        /// <param name="newLogin">String that specifies a login with job 
        /// creation rights. The login specified will receive ownership.</param>
        public void ReassignJobsByLogin( string oldLogin, string newLogin )
        {
            if (null == oldLogin)
            {
                throw new ArgumentNullException(nameof(oldLogin));
            }

            if (null == newLogin)
            {
                throw new ArgumentNullException(nameof(newLogin));
            }

            try
            {

                var queries = new StringCollection
                {
                    string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_manage_jobs_by_login @action = N'REASSIGN', @current_owner_login_name = N'{0}', @new_owner_login_name = N'{1}'",
                                           SqlString(oldLogin), SqlString(newLogin))
                };

                this.ExecutionManager.ExecuteNonQuery( queries );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReassignJobsByLogin, this, e);
            }
        }

        /// <summary>
        /// Drops the SQLServerAgent job identified and removes the referencing 
        /// Job object from the Jobs collection.
        /// </summary>
        /// <param name="jobid"></param>
        [Obsolete("Use RemoveJobByID")]
        public void DropJobByID( Guid jobid )
        {
            Job job;
            for( var i=0; i<Jobs.Count; i++ )
            {
                job = Jobs[i];
                if( jobid == job.JobID )
                {
                    job.Drop();
                    break; // we are done here
                }
            }
        }

        /// <summary>
        /// Drops all SQLServerAgent jobs owned by the login identified and removes 
        /// the referencing Job objects from the Jobs collection.
        /// </summary>
        /// <param name="login"></param>
        public void DropJobsByLogin( string login )
        {
            if (null == login)
            {
                throw new ArgumentNullException(nameof(login));
            }
            try
            {
                var queries = new StringCollection
                {
                    string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_manage_jobs_by_login @action = N'DELETE', @current_owner_login_name = N'{0}'",
                SqlString(login))
                };

                this.ExecutionManager.ExecuteNonQuery( queries );
                jobs.Refresh();
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropJobsByLogin, this, e);
            }
        }

        /// <summary>
        /// Drops all the jobs that originate on the specified server
        /// </summary>
        /// <param name="serverName"></param>
        public void DropJobsByServer( string serverName )
        {
            if (null == serverName)
            {
                throw new ArgumentNullException(nameof(serverName));
            }

            try
            {

                Job job;
                for( var i=0; i<Jobs.Count; i++ )
                {
                    job = Jobs[i];
                    if( 0 == string.Compare(serverName,job.OriginatingServer,StringComparison.OrdinalIgnoreCase) )
                    {
                        job.Drop();
                        i--; // all jobs in the collection are now shifted by one, step back
                    }
                }
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropJobsByServer, this, e);
            }
        }

        /// <summary>
        /// The StartMonitor method begins monitoring of the local SQLServerAgent 
        /// service by an instance of SQL Server.
        /// </summary>
        /// <param name="netSendAddress">Not used</param>
        /// <param name="restartAttempts"></param>
        public void StartMonitor( string netSendAddress, int restartAttempts)
        {
            try
            {
                var queries = new StringCollection
                {
                    string.Format(SmoApplication.DefaultCulture, $"EXEC master.dbo.xp_sqlagent_monitor N'START', N'', {restartAttempts}")
                };

                this.ExecutionManager.ExecuteNonQuery( queries );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.StartMonitor, this, e);
            }
        }

        /// <summary>
        /// The StopMonitor method ends monitoring of the local SQLServerAgent 
        /// service by an instance of SQL Server.
        /// </summary>
        public void StopMonitor()
        {
            try
            {
                var queries = new StringCollection
                {
                    "EXEC master.dbo.xp_sqlagent_monitor N'STOP'"
                };

                this.ExecutionManager.ExecuteNonQuery( queries );
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.StopMonitor, this, e);
            }
        }
        
        internal DataTable EnumPerfInfoInternal(string objectName, string counterName, string instanceName)
        {
            var sb = new StringBuilder();
            var bFilterAdded = false;
            if( null != objectName )
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "@ObjectName = '{0}'", Urn.EscapeString(objectName));
                bFilterAdded = true;
            }
            if( null != counterName )
            {
                if( bFilterAdded )
                {
                    sb.Append(" and ");
                }
                sb.AppendFormat(CultureInfo.InvariantCulture, "@CounterName = '{0}'", Urn.EscapeString(counterName));
                bFilterAdded = true;
            }
            if( null != instanceName )
            {
                if( bFilterAdded )
                {
                    sb.Append(" and ");
                }
                sb.AppendFormat(CultureInfo.InvariantCulture, "@InstanceName = '{0}'", Urn.EscapeString(instanceName));
            }

            var sbUrn = new StringBuilder();
            sbUrn.Append(this.Urn.Value);
            sbUrn.Append("/PerfInfo");
            if( sb.Length > 0 )
            {
                sbUrn.Append("[");
                sbUrn.Append(sb.ToString());
                sbUrn.Append("]");
            }
            var req = new Request(sbUrn.ToString());
            return this.ExecutionManager.GetEnumeratorData(req);
        }

        /// <summary>
        /// Returns a listing of all performance counters for SQL Agent
        /// </summary>
        /// <returns>A DataTable with columns named "ObjectName", "CounterName", "InstanceName" </returns>
        public DataTable EnumPerformanceCounters()
        {
            try
            {
                return EnumPerfInfoInternal(null, null, null);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPerformanceCounters, this, e);
            }

        }

        /// <summary>
        /// Returns a listing of performance counters with the given object name
        /// </summary>
        /// <returns>A DataTable with columns named "ObjectName", "CounterName", "InstanceName" </returns>

        public DataTable EnumPerformanceCounters(string objectName)
        {
            if (null == objectName)
            {
                throw new ArgumentNullException(nameof(objectName));
            }
            try
            {
                return EnumPerfInfoInternal(objectName, null, null);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPerformanceCounters, this, e);
            }
                
        }

        /// <summary>
        /// Returns a listing of performance counter instances for the given object name and counter name
        /// </summary>
        /// <returns>A DataTable with columns named "ObjectName", "CounterName", "InstanceName" </returns>
        public DataTable EnumPerformanceCounters(string objectName, string counterName)
        {
            if (null == objectName)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            if (null == counterName)
            {
                throw new ArgumentNullException(nameof(counterName));
            }

            try
            {
                return EnumPerfInfoInternal(objectName, counterName, null);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPerformanceCounters, this, e);
            }
            
        }       

        public DataTable EnumPerformanceCounters(string objectName, string counterName, string instanceName)
        {
            if (null == objectName)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            if (null == counterName)
            {
                throw new ArgumentNullException(nameof(counterName));
            }

            if (null == instanceName)
            {
                throw new ArgumentNullException(nameof(instanceName));
            }

            try
            {
                return EnumPerfInfoInternal(objectName, counterName, instanceName);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPerformanceCounters, this, e);
            }
        }       
    }

    // we need this class to implement methods that link correctly the object into 
    // the tree
    public class AgentObjectBase : NamedSmoObject
    {

        internal AgentObjectBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            SetServerObject( parentColl.ParentInstance.GetServerObject());

            // set the comparer used by the collections
            if (ParentColl.ParentInstance is JobServer server)
            {
                m_comparer = server.Parent.StringComparer;
            }
            else
            {
                m_comparer = ((AgentObjectBase)(ParentColl.ParentInstance)).StringComparer;
            }
        }

        internal AgentObjectBase(ObjectKeyBase key, SqlSmoState state) : 
            base(key, state)
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal AgentObjectBase(string name)
            :     base()
        {
            this.Name = name;
        }

        protected internal AgentObjectBase() : base()   {}
        
        internal protected override string GetDBName()
        {
            return "msdb";
        }
    }


    /// <summary>
    /// The JobFilter object is used to constrain the output of the EnumJobs 
    /// method of the JobServer object.
    /// </summary>
    public sealed class JobFilter
    {
        private string category = null;
        public string Category 
        {
            get { return category; }
            set { category = value; }
        }
        
        private bool enabled;
        private bool enabledDirty = false;
        public bool Enabled 
        {
            get { return enabled; }
            set { enabledDirty = true; enabled = value; }
        }
        
        private JobExecutionStatus currentExecutionStatus;
        internal bool currentExecutionStatusDirty = false;
        public JobExecutionStatus CurrentExecutionStatus
        {
            get { return currentExecutionStatus; }
            set { currentExecutionStatusDirty = true; currentExecutionStatus = value; }
        }

        private string owner = null;
        public string Owner 
        {
            get { return owner; }
            set { owner = value; }
        }

        private FindOperand dateFindOperand = FindOperand.EqualTo;
        public FindOperand DateFindOperand
        {
            get { return dateFindOperand; }
            set { dateFindOperand = value; }
        }
        
        private AgentSubSystem stepSubsystem;
        internal bool stepSubsystemDirty = false;
        public AgentSubSystem StepSubsystem 
        {
            get { return stepSubsystem; }
            set { stepSubsystemDirty = true; stepSubsystem = value; }
        }
        
        private DateTime dateJobCreated;
        private bool dateJobCreatedDirty = false;
        public DateTime DateJobCreated
        {
            get { return dateJobCreated; }
            set { dateJobCreatedDirty = true; dateJobCreated = value; }
        }
        
        private JobType  jobType;
        internal bool jobTypeDirty = false;
        public JobType JobType
        {
            get { return jobType; }
            set { jobTypeDirty = true; jobType = value; }
        }
        
        private DateTime dateJobLastModified;
        private bool dateJobLastModifiedDirty = false;
        public DateTime DateJobLastModified
        {
            get { return dateJobLastModified; }
            set { dateJobLastModifiedDirty = true; dateJobLastModified = value; }
        }

        /// <summary>
        /// Construct enumerator request with Urn Filter
        /// </summary>
        /// <param name="jobServer"></param>
        /// <returns></returns>
        internal Request GetEnumRequest( JobServer jobServer )
        {
            var req = new Request();
            
            var builder = new StringBuilder( Globals.INIT_BUFFER_SIZE );

            builder.Append( jobServer.Urn);
            builder.AppendFormat(SmoApplication.DefaultCulture,  "/Job" );
            this.GetRequestFilter(builder);
            req.Urn = builder.ToString();

            return req;
            
        }

        private void GetRequestFilter(StringBuilder builder )
        {
            var count = 0;
            
            if( category != null )
            {
                if( count++ > 0 )
                {
                    builder.Append( " and " );
                }
                else
                {
                    builder.Append( "[" );
                }

                builder.AppendFormat(SmoApplication.DefaultCulture,  "@Category='{0}'", Urn.EscapeString( category ) );
            }
        
            if( enabledDirty) 
            {
                if( count++ > 0 )
                {
                    builder.Append( " and " );
                }
                else
                {
                    builder.Append( "[" );
                }

                builder.AppendFormat(SmoApplication.DefaultCulture,  "@IsEnabled={0}", enabled?1:0);
            }
                     
            
            if( owner != null )
            {
                if( count++ > 0 )
                {
                    builder.Append( " and " );
                }
                else
                {
                    builder.Append( "[" );
                }

                builder.AppendFormat(SmoApplication.DefaultCulture,  "@OwnerLoginName='{0}'", Urn.EscapeString( owner ) );
            }
            
            if( dateJobCreatedDirty )
            {
                if( count++ > 0 )
                {
                    builder.Append( " and " );
                }
                else
                {
                    builder.Append( "[" );
                }

                builder.AppendFormat(SmoApplication.DefaultCulture,  "@DateCreated {0} '{1}'",
                                     GetStringOperand(dateFindOperand), dateJobCreated.ToString(SmoApplication.DefaultCulture) );
            }
            
            if( dateJobLastModifiedDirty )
            {
                if( count++ > 0 )
                {
                    builder.Append( " and " );
                }
                else
                {
                    builder.Append( "[" );
                }

                builder.AppendFormat(SmoApplication.DefaultCulture,  "@DateLastModified {0} '{1}'",
                                     GetStringOperand(dateFindOperand), dateJobLastModified.ToString(SmoApplication.DefaultCulture) );
            }
            
            if( count > 0 )
            {
                builder.Append( "]" );
            }
        }

        private string GetStringOperand( FindOperand fo )
        {
            switch( fo )
            {
                case FindOperand.EqualTo : return "="; 
                case FindOperand.LessThan : return "<";
                case FindOperand.GreaterThan : return ">";
            }

            return "=";
        }

    }

    /// <summary>
    /// This directs evaluation of the DateJobCreated and DateJobLastModified 
    /// filter properties.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum FindOperand
    {
        EqualTo = 1,
        GreaterThan = 2,
        LessThan = 3
    }
}


