// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public class ScheduleBase : AgentObjectBase
    {
        internal ScheduleBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState eState) :
            base(parentColl, key, eState)
        {
        }

        internal protected ScheduleBase() : base() { }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Int32 ID
        {
            get
            {
                // If the value is both in 'key' and property bag, and they are
                // different, then the one in the 'key' takes precedence.
                // The user can only change the ID through the property bag,
                // so inconsistency should not be a problem.
                int id = ((ScheduleObjectKey)key).ID;
                if( id == JobScheduleCollectionBase.GetDefaultID() )
                {
                    Property propID = this.Properties["ID"];
                    if( propID.Retrieved || propID.Dirty )
                    {
                        id = (int)propID.Value;
                    }
                }
                return id;
            }
        }

        protected void SetId(int id)
        {
            ((ScheduleObjectKey)key).ID = id;
        }

        internal override ObjectKeyBase GetEmptyKey()
        {
            return new ScheduleObjectKey(null, JobScheduleCollectionBase.GetDefaultID());
        }
        
    }

    [SfcElementType("Schedule")]
    public partial class JobSchedule : ScheduleBase, Cmn.IAlterable, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IScriptable
    {
        public JobSchedule() : base(){ }

        public JobSchedule(SqlSmoObject parent, string name) : base()
        {
            ValidateName(name);
            this.key = new ScheduleObjectKey(name, JobScheduleCollectionBase.GetDefaultID());
            this.Parent = parent;
        }

        internal JobSchedule(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key,state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "Schedule";
            }
        }
        
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            bool isSharedSched = IsShared;
            int count;

            ExecuteForScalar = true;
            if (isSharedSched)
            {
                // shared schedules cannot be scripted for 7.0 and 8.0
                ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

                if (sp.ForDirectExecution)
                {
                    createQuery.Append("DECLARE @schedule_id int");
                    createQuery.Append(Globals.newline);
                }
                //We are expecting the schedule_id to be returned by this query
                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_add_schedule @schedule_name=N'{0}'", SqlString(this.Name));
                count = 1;
            }
            else
            {
                if (sp.TargetServerVersionInternal > SqlServerVersionInternal.Version80)
                {
                    if (sp.ForDirectExecution)
                    {
                        createQuery.Append("DECLARE @schedule_id int");
                        createQuery.Append(Globals.newline);
                    }
                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                        "EXEC {0}msdb.dbo.sp_add_jobschedule {1}, @name=N'{2}'",
                        Job.GetReturnCode(sp),
                        ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp),
                        SqlString(this.Name));

                    count = 2;
                }
                else
                {
                    if (sp.ForDirectExecution)
                    {
                        // The Shiloh stored procedure does not return the schedule id, sp 
                        // we will obtain it by querying for the highest id 
                        createQuery.Append("begin transaction");
                        createQuery.Append(Globals.newline);
                        createQuery.Append("create table #tmp_sp_help_jobschedule1 (schedule_id int null, schedule_name nvarchar(128) null, enabled int null, freq_type int null, freq_interval int null, freq_subday_type int null, freq_subday_interval int null, freq_relative_interval int null, freq_recurrence_factor int null, active_start_date int null, active_end_date int null, active_start_time int null, active_end_time int null, date_created datetime null, schedule_description nvarchar(4000) null, next_run_date int null, next_run_time int null, job_id uniqueidentifier null)");
                        createQuery.Append(Globals.newline);
                        createQuery.Append("DECLARE @schedule_id int");
                        createQuery.Append(Globals.newline);
                    }

                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                        "EXEC {0}msdb.dbo.sp_add_jobschedule {1}, @name=N'{2}'",
                        Job.GetReturnCode(sp),
                        ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp),
                        SqlString(this.Name));

                    count = 2;
                }
            }

            GetAllParams(createQuery, sp, ref count);

            // Add the @schedule_uid parameter if the creation sproc supports it. SQL 2008
            // (Version100) supports it, but 2005 (Version90) only supported it for shared
            // schedules. SQL 2000 (Version80) did not support it at all.
            if (this.ServerVersion.Major >= 9 &&
                ((sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100) ||
                 (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version90 && isSharedSched)))
            {
                GetParameter(createQuery, sp, "ScheduleUid", "@schedule_uid=N'{0}'", ref count);
            }


            //get the schedule_id if we are in execution mode
            if (sp.ForDirectExecution)
            {
                if (sp.TargetServerVersionInternal > SqlServerVersionInternal.Version80)
                {
                    createQuery.Append(", @schedule_id = @schedule_id OUTPUT");
                    createQuery.Append(Globals.newline);
                    createQuery.Append("select @schedule_id");
                }
                else
                {
                    // The Shiloh stored procedure does not return the schedule id, sp 
                    // we will obtain it by querying for the highest id 
                    createQuery.Append(Globals.newline);
                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                        "insert into #tmp_sp_help_jobschedule1 (schedule_id, schedule_name, enabled, freq_type, freq_interval, freq_subday_type, freq_subday_interval, freq_relative_interval, freq_recurrence_factor, active_start_date, active_end_date, active_start_time, active_end_time, date_created, schedule_description, next_run_date, next_run_time) 	exec msdb.dbo.sp_help_jobschedule  {0}",
                        ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp));
                    createQuery.Append(Globals.newline);

                    createQuery.Append("select max(schedule_id) from #tmp_sp_help_jobschedule1");
                    createQuery.Append(Globals.newline);
                    createQuery.Append("drop table #tmp_sp_help_jobschedule1");
                    createQuery.Append(Globals.newline);
                    createQuery.Append("commit transaction");
                }
            }

            queries.Add(createQuery.ToString());
        }

        
        private bool IsShared
        {
            get 
            {
                return this.ParentColl.ParentInstance is JobServer;
            }
        }

        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, ref int count)
        {
            GetBoolParameter(sb, sp, "IsEnabled", "@enabled={0}", ref count);
            GetEnumParameter( sb, sp, "FrequencyTypes", "@freq_type={0}", 
                            typeof(FrequencyTypes), ref count );
            GetParameter( sb, sp, "FrequencyInterval", "@freq_interval={0}", ref count);
            GetEnumParameter( sb, sp, "FrequencySubDayTypes", "@freq_subday_type={0}",
                            typeof(FrequencySubDayTypes), ref count)  ;
            GetParameter( sb, sp, "FrequencySubDayInterval", "@freq_subday_interval={0}", ref count);
            GetEnumParameter( sb, sp, "FrequencyRelativeIntervals", "@freq_relative_interval={0}",
                            typeof( FrequencyRelativeIntervals), ref count);
            GetParameter( sb, sp, "FrequencyRecurrenceFactor", "@freq_recurrence_factor={0}", ref count);
            GetDateTimeParameterAsInt( sb, sp, "ActiveStartDate", "@active_start_date={0}", ref count);
            GetDateTimeParameterAsInt( sb, sp, "ActiveEndDate", "@active_end_date={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "ActiveStartTimeOfDay", "@active_start_time={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "ActiveEndTimeOfDay", "@active_end_time={0}", ref count);
        }

        //get the schedule_id if this is a shared schedule
        protected override void PostCreate()
        {
            if( !this.ExecutionManager.Recording )
            {
                SetId((int)(ScalarResult[1]));
            }
        }
        private bool keepUnusedSchedule = false; // !keepUnusedSchedule corresponds to the delete_unused_schedule t-sql param while deleting schedule/job.-anchals

        /// <summary>
        /// Drop
        /// </summary>
        /// <param name="keepUnusedSchedule"></param>
        public void Drop(bool keepUnusedSchedule)
        {
            ThrowIfBelowVersion90();

            try
            {
                // since there is no direct way of passing keepUnusedSchedule
                // as a parameter to ScriptDrop overriden method; we use a private variable.-anchals
                this.keepUnusedSchedule = keepUnusedSchedule;
                base.DropImpl();
            }
            finally
            {
                this.keepUnusedSchedule = false;
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
            if (IsShared)
            {
                // shared schedules cannot be scripted for 7.0 and 8.0
                ThrowIfBelowVersion90(sp.TargetServerVersionInternal);
                // if keepUnusedSchedule is true we keep the schedule in any
                // case and don't drop it. (we don't detach the schedule form
                // the job in this case below).
                if (keepUnusedSchedule)
                {
                    return;
                }
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                        Scripts.INCLUDE_EXISTS_AGENT_SCHEDULE,
                        "", ID);
                    sb.Append(sp.NewLine);
                }

                if (!IsShared)
                {
                    // we detach the schedule from the parent job but we remove the
                    // schedule only if it is desired.-anchals
                    int deleteUnusedSchedule = keepUnusedSchedule ? 0 : 1;
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                        "EXEC msdb.dbo.sp_detach_schedule {0}, @schedule_id={1}, @delete_unused_schedule={2}",
                                ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp), ID, deleteUnusedSchedule);

                }
                else
                {
                    // This means that this doesn't have a parent Job. It could be because
                    // user got the schedule from the JobServer directly. If that be the case
                    // and the schedule is shared the t-sql sp_delete_schedule would fail since
                    // we are not forcing the operation. Thus we don't do any unintended data loss.-anchals
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                        "EXEC msdb.dbo.sp_delete_schedule @schedule_id={0}", ID);
                }
            }
            else
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(Scripts.INCLUDE_EXISTS_AGENT_JOBSCHEDULE,
                        "", ID);
                    sb.Append(sp.NewLine);
                }
                // regular schedules - we need to detach them from the jobs before dropping
                // this is done because if we have duplicate names sp_delete_jobschedule 
                // will not be able to to the unmapping since the schedule is identified by name
                sb.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_delete_jobschedule {0}, @name=N'{1}'",
                            ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp),
                            SqlString(this.Name));
            }

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            int count;
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                                        "EXEC msdb.dbo.sp_update_schedule @schedule_id={0}", ID);
                count = 1;
                GetAllParams(alterQuery, sp, ref count);

                if (count > 1)
                {
                    queries.Add(alterQuery.ToString());
                }
            }
            else
            {
                alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                                        "EXEC msdb.dbo.sp_update_jobschedule {0}, @name=N'{1}'",
                                        ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(),
                                        SqlString(this.Name));

                count = 2;
                GetAllParams(alterQuery, sp, ref count);

                if (count > 2)
                {
                    queries.Add(alterQuery.ToString());
                }
            }
        }
        
        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            if(sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                queries.Add( string.Format(SmoApplication.DefaultCulture,  
                                "EXEC msdb.dbo.sp_update_schedule @schedule_id={0}, @new_name=N'{1}'", 
                                ID, SqlString(newName)));
            }
            else
            {
            queries.Add( string.Format(SmoApplication.DefaultCulture,  
                                           "EXEC msdb.dbo.sp_update_jobschedule {0}, @name=N'{1}', @new_name=N'{2}'", 
                                           ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(),
                                           SqlString( this.Name), 
                                           SqlString(newName)));
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

        public Guid[] EnumJobReferences()
        {
            try
            {
                ThrowIfBelowVersion90();

                CheckObjectState(true);
                Request r; 
                    
                if( IsShared)
                {
                    string reqStr = string.Format(SmoApplication.DefaultCulture, "{0}/Job/Schedule[@ID={1}]", this.Urn.Parent, ID);
                    r = new Request(reqStr);
                }
                else
                {
                    string reqStr = string.Format(SmoApplication.DefaultCulture, "{0}/Job/Schedule[@ID={1}]", this.Parent.Urn.Parent, ID);
                    r = new Request(reqStr);
                }

                r.Fields = new string[]{};
                r.ParentPropertiesRequests = new PropertiesRequest[1];
                PropertiesRequest parentProps = new PropertiesRequest();
                parentProps.Fields = new String[]{ "JobID"};
                r.ParentPropertiesRequests[0] = parentProps;
        
                DataTable jobs = this.ExecutionManager.GetEnumeratorData( r );

                Guid[] guids = new Guid[jobs.Rows.Count];
                for(int i = 0; i < jobs.Rows.Count; i++)
                {
                    DataRow row = jobs.Rows[i];
                    guids[i] = (Guid)row[0];	
                }

                return guids;
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumReferences, this, e);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Guid ScheduleUid
        {
            get
            {
                return (System.Guid)this.Properties.GetValueWithNullReplacement("ScheduleUid");
            }
            set
            {
                if(this.State != SqlSmoState.Creating)
                {
                    throw new PropertyReadOnlyException("ScheduleUid");
                }
                Properties.SetValueWithConsistencyCheck("ScheduleUid", value);
            }
        }
    }
}

