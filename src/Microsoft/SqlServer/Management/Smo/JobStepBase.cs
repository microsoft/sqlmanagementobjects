// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Agent
{
    [SfcElementType("Step")]
    public partial class JobStep : AgentObjectBase, Cmn.IAlterable, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IRenamable, IScriptable
    {
        internal JobStep(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key,state)
        {
        }


        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "Step";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // we use the JobIdOrJobNameParameter overload so that we don't get the automatically prefixed assignment code.-anchals
                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_JOBSTEP,
                    "NOT",
                    ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp, false),
                    StepIDInternal);
                createQuery.Append(sp.NewLine);
            }
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC {0}msdb.dbo.sp_add_jobstep {1}, @step_name=N'{2}'",
                    Job.GetReturnCode(sp),
                    ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp),
                    SqlString(this.Name));

            int count = 2;
            GetParameter(createQuery, sp, "ID", "@step_id={0}", ref count);
            GetAllParams(createQuery, sp, ref count);

            queries.Add(createQuery.ToString());
        }

        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, ref int count)
        {
            GetParameter( sb, sp, "CommandExecutionSuccessCode", "@cmdexec_success_code={0}", ref count);
            GetEnumParameter( sb, sp, "OnSuccessAction", "@on_success_action={0}", 
                              typeof( StepCompletionAction) , ref count);
            GetParameter( sb, sp, "OnSuccessStep", "@on_success_step_id={0}", ref count);
            GetEnumParameter( sb, sp, "OnFailAction", "@on_fail_action={0}", 
                              typeof( StepCompletionAction) , ref count);
            GetParameter( sb, sp, "OnFailStep", "@on_fail_step_id={0}", ref count);
            GetParameter( sb, sp, "RetryAttempts", "@retry_attempts={0}", ref count);
            GetParameter( sb, sp, "RetryInterval", "@retry_interval={0}", ref count);
            GetEnumParameter( sb, sp, "OSRunPriority", "@os_run_priority={0}", 
                              typeof(OSRunPriority), ref count);

            Property propSubSystem = Properties.Get("SubSystem");
            if( (null != propSubSystem.Value) && ( !sp.ScriptForAlter || propSubSystem.Dirty ) )
            {
                if( count++ > 0 )
                {
                    sb.Append(Globals.commaspace);
                }
                // this is the default subsystem
                string subSystemName = "TSQL";
                switch( (AgentSubSystem)propSubSystem.Value )
                {	
                    case AgentSubSystem.ActiveScripting:    subSystemName = "ActiveScripting"; break;
                    case AgentSubSystem.AnalysisCommand :	subSystemName = "ANALYSISCOMMAND";
                                                            //force test that for this subsytem the Server property must be set
                                                            this.GetPropValue("Server");
                                                            break;
                    case AgentSubSystem.AnalysisQuery :		subSystemName = "ANALYSISQUERY";break;
                    case AgentSubSystem.CmdExec:            subSystemName = "CmdExec"; break;
                    case AgentSubSystem.Distribution:       subSystemName = "Distribution"; break;
                    case AgentSubSystem.Ssis :				subSystemName = "SSIS";break;
                    case AgentSubSystem.LogReader:          subSystemName = "LogReader"; break;
                    case AgentSubSystem.Merge:              subSystemName = "Merge"; break;
                    case AgentSubSystem.QueueReader:        subSystemName = "QueueReader"; break;
                    case AgentSubSystem.Snapshot:           subSystemName = "Snapshot"; break;
                    case AgentSubSystem.TransactSql :		subSystemName = "TSQL";break;
                    case AgentSubSystem.PowerShell:         subSystemName = "PowerShell"; break;
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,  "@subsystem=N'{0}'", SqlString(subSystemName));
            }

            GetStringParameter( sb, sp, "Command", "@command=N'{0}'" , ref count);

            // Don't script /SERVER for the Managed Instances - it's not used.
            //
            if (sp.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlManagedInstance)
            {
                GetStringParameter(sb, sp, "Server", "@server=N'{0}'", ref count);
            }

            GetStringParameter( sb, sp, "DatabaseName", "@database_name=N'{0}'" , ref count);
            GetStringParameter( sb, sp, "DatabaseUserName", "@database_user_name=N'{0}'" , ref count);
            GetStringParameter( sb, sp, "OutputFileName", "@output_file_name=N'{0}'" , ref count);
            GetEnumParameter(sb, sp, "JobStepFlags", "@flags={0}", typeof(JobStepFlags), ref count);

            if (ServerVersion.Major >= 9 && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                GetStringParameter(sb, sp, "ProxyName", "@proxy_name=N'{0}'", ref count);
            }

            // If we're targeting a pre-90 server, we can't have
            // jobstep flags that don't exist before 90.
            if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90)
            {
                Property prop = Properties.Get("JobStepFlags");

                if (prop.Value != null && ((JobStepFlags)prop.Value) >= JobStepFlags.LogToTableWithOverwrite)
                {
                    throw new UnsupportedVersionException(
                                                            ExceptionTemplates.InvalidPropertyValueForVersion(
                                                            this.GetType().Name,
                                                            "JobStepFlags",
                                                            prop.Value.ToString(), 
                                                            GetSqlServerVersionName()));  

                }
            }
        }


        // we will use this exclusively to perform post drop cleanup, because we cannot 
        // access the property bag after the object is marked as being dropped
        private int stepIDInternal = 0;

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
            stepIDInternal = this.StepIDInternal;

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // we use the JobIdOrJobNameParameter overload so that we don't get the automatically prefixed assignment code.-anchals
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_JOBSTEP,
                    "",
                    ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp, false),
                    stepIDInternal);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                                    "EXEC msdb.dbo.sp_delete_jobstep {0}, @step_id={1}",
                                    ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp),
                                    stepIDInternal);

            queries.Add(sb.ToString());
        }

        protected override void PostDrop()
        {
            // when a step gets dropped, the id's of all steps that follow it are decremented
            // if we are in direct execution mode, we will iterate through the steps 
            // collection and fix the ID's 
            if( !this.ExecutionManager.Recording )
            {
                // get to the steps collection
                JobStepCollection steps = (JobStepCollection)this.ParentColl;
                // if the collection is not initialized, there is nothing to fix
                if( steps.initialized )
                {
                    Property propID = null;
                    Int32 thisStepID = stepIDInternal;
                    foreach(JobStep js in steps )
                    {
                        propID = js.Properties.Get("ID");
                        // it might be possible that we did not retrieve ID yet, then we will not
                        // update this property
                        if( propID.Value != null && (Int32)propID.Value >= stepIDInternal )
                        {
                            propID.SetValue((Int32)propID.Value - 1 );
                        }
                    }
                }
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                                    "EXEC msdb.dbo.sp_update_jobstep {0}, @step_id={1} ",
                                    ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(),
                                    this.StepIDInternal);

            int count = 2;
            GetAllParams(alterQuery, sp, ref count);

            if (count > 2)
            {
                queries.Add(alterQuery.ToString());
            }
        }

        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            StringBuilder renameQuery = new StringBuilder( Globals.INIT_BUFFER_SIZE );
            renameQuery.AppendFormat(SmoApplication.DefaultCulture,  
                                    "EXEC msdb.dbo.sp_update_jobstep {0}, @step_id={1}, @step_name=N'{2}'", 
                                    ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(),
                                    this.StepIDInternal,
                                    SqlString(newName));

            queries.Add( renameQuery.ToString() );
        }

        internal int StepIDInternal
        {
            get { return (int)Properties["ID"].Value;}
        }

        /// <summary>
        /// This method returns a DateTable object that enumerates the job step execution 
        /// output logs if they were saved to the table
        /// </summary>
        /// <returns></returns>
        public DataTable EnumLogs()
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/OutputLog"));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobStepOutputLogs, this, e);
            }

        }

        /// <summary>
        /// Delete job step logs older than specified date
        /// </summary>
        public void DeleteLogs(DateTime olderThan)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_delete_jobsteplog @job_name=N'{0}', @step_name=N'{1}', @older_than='{2}'",
                            SqlString(this.Parent.Name), SqlString(this.Name), olderThan.ToString("MM/dd/yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DeleteJobStepLogs, this, e);
            }
        }

        /// <summary>
        /// Delete job step logs before certain log number
        /// </summary>
        public void DeleteLogs(int largerThan)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_delete_jobsteplog @job_name=N'{0}', @step_name=N'{1}', @larger_than='{2}'",
                            SqlString(this.Parent.Name),SqlString(this.Name), largerThan);
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DeleteJobStepLogs, this, e);
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

