// Copyright (c) Microsoft Corporation.
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
                    Property propID = Properties["ID"];
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
            key = new ScheduleObjectKey(name, JobScheduleCollectionBase.GetDefaultID());
            Parent = parent;
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
            var createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            var isSharedSched = IsShared;
            int count;

            var returnCode = Job.GetReturnCode(sp);
            ExecuteForScalar = true;
            if (isSharedSched)
            {

                if (sp.ForDirectExecution)
                {
                    _ = createQuery.Append("DECLARE @schedule_id int");
                    _ = createQuery.Append(Globals.newline);
                }
                //We are expecting the schedule_id to be returned by this query
                _ = createQuery.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_add_schedule @schedule_name=N'{0}'", SqlString(Name));
                count = 1;
            }
            else
            {
                if (sp.ForDirectExecution)
                {
                    _ = createQuery.Append("DECLARE @schedule_id int");
                    _ = createQuery.Append(Globals.newline);
                }
                _ = createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC {0}msdb.dbo.sp_add_jobschedule {1}, @name=N'{2}'",
                    returnCode,
                    ((Job)ParentColl.ParentInstance).JobIdOrJobNameParameter(sp),
                    SqlString(Name));

                count = 2;
            }

            GetAllParams(createQuery, sp, ref count);

            // Add the @schedule_uid parameter if the creation sproc supports it. SQL 2008
            // (Version100) supports it, but 2005 (Version90) only supported it for shared
            // schedules. SQL 2000 (Version80) did not support it at all.
            if ((sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100) ||
                 (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version90 && isSharedSched))
            {
                GetParameter(createQuery, sp, "ScheduleUid", "@schedule_uid=N'{0}'", ref count);
            }


            //get the schedule_id if we are in execution mode
            if (sp.ForDirectExecution)
            {
                _ = createQuery.Append(", @schedule_id = @schedule_id OUTPUT");
                _ = createQuery.Append(Globals.newline);
            }
            var ownerLoginName = GetPropValueOptional(nameof(OwnerLoginName), string.Empty);
            if (sp.IncludeScripts.Owner && !string.IsNullOrEmpty(ownerLoginName))
            {
                if (!isSharedSched && sp.Agent.InScriptJob)
                {
                    _ = createQuery.Append(Globals.newline);
                    Job.AddCheckErrorCode(createQuery);
                }
                _ = createQuery.Append(Globals.newline);
                _ = createQuery.Append($"exec {returnCode}msdb.dbo.sp_update_schedule @name=N'{SqlString(Name)}', @owner_login_name=N'{SqlString(ownerLoginName)}'");
                _ = createQuery.Append(Globals.newline);
            }
            //get the schedule_id if we are in execution mode
            if (sp.ForDirectExecution)
            {
                _ = createQuery.Append("select @schedule_id");
            }
            _ = queries.Add(createQuery.ToString());
        }

        
        private bool IsShared
        {
            get 
            {
                return ParentColl.ParentInstance is JobServer;
            }
        }

        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, ref int count)
        {
            GetBoolParameter(sb, sp, nameof(IsEnabled), "@enabled={0}", ref count);
            GetEnumParameter( sb, sp, nameof(FrequencyTypes), "@freq_type={0}", 
                            typeof(FrequencyTypes), ref count );
            GetParameter( sb, sp, nameof(FrequencyInterval), "@freq_interval={0}", ref count);
            GetEnumParameter( sb, sp, nameof(FrequencySubDayTypes), "@freq_subday_type={0}",
                            typeof(FrequencySubDayTypes), ref count)  ;
            GetParameter( sb, sp, nameof(FrequencySubDayInterval), "@freq_subday_interval={0}", ref count);
            GetEnumParameter( sb, sp, nameof(FrequencyRelativeIntervals), "@freq_relative_interval={0}",
                            typeof( FrequencyRelativeIntervals), ref count);
            GetParameter( sb, sp, nameof(FrequencyRecurrenceFactor), "@freq_recurrence_factor={0}", ref count);
            _ = GetDateTimeParameterAsInt(sb, sp, nameof(ActiveStartDate), "@active_start_date={0}", ref count);
            _ = GetDateTimeParameterAsInt(sb, sp, nameof(ActiveEndDate), "@active_end_date={0}", ref count);
            _ = GetTimeSpanParameterAsInt(sb, sp, nameof(ActiveStartTimeOfDay), "@active_start_time={0}", ref count);
            _ = GetTimeSpanParameterAsInt(sb, sp, nameof(ActiveEndTimeOfDay), "@active_end_time={0}", ref count);
            if (sp.ScriptForAlter && sp.IncludeScripts.Owner)
            {
                _ = GetStringParameter(sb, sp, nameof(OwnerLoginName), "@owner_login_name=N'{0}'", ref count);
            }
        }

        //get the schedule_id if this is a shared schedule
        protected override void PostCreate()
        {
            if( !ExecutionManager.Recording )
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
                            SqlString(Name));
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
            alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                                    "EXEC msdb.dbo.sp_update_schedule @schedule_id={0}", ID);
            count = 1;
            GetAllParams(alterQuery, sp, ref count);
            
            if (count > 1)
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
            queries.Add(string.Format(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_update_schedule @schedule_id={0}, @new_name=N'{1}'",
                            ID, SqlString(newName)));
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
                    string reqStr = string.Format(SmoApplication.DefaultCulture, "{0}/Job/Schedule[@ID={1}]", Urn.Parent, ID);
                    r = new Request(reqStr);
                }
                else
                {
                    string reqStr = string.Format(SmoApplication.DefaultCulture, "{0}/Job/Schedule[@ID={1}]", Parent.Urn.Parent, ID);
                    r = new Request(reqStr);
                }

                r.Fields = new string[]{};
                r.ParentPropertiesRequests = new PropertiesRequest[1];
                PropertiesRequest parentProps = new PropertiesRequest();
                parentProps.Fields = new String[]{ "JobID"};
                r.ParentPropertiesRequests[0] = parentProps;
        
                DataTable jobs = ExecutionManager.GetEnumeratorData( r );

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
                return (System.Guid)Properties.GetValueWithNullReplacement("ScheduleUid");
            }
            set
            {
                if(State != SqlSmoState.Creating)
                {
                    throw new PropertyReadOnlyException("ScheduleUid");
                }
                Properties.SetValueWithConsistencyCheck("ScheduleUid", value);
            }
        }
    }
}

