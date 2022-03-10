// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;

using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    internal class JobObjectComparer : ObjectComparerBase
    {
        internal JobObjectComparer(IComparer comparer)
            : base(comparer)
        {
        }

        public override int Compare(object obj1, object obj2)
        {
            JobObjectKey x = obj1 as JobObjectKey;
            JobObjectKey y = obj2 as JobObjectKey;

            if (stringComparer.Compare(x.Name, y.Name) == 0 && x.CategoryID == y.CategoryID)
            {
                return 0;
            }
            return 1;
        }
    }

    internal class JobObjectKeySingleton
    {
        internal StringCollection jobKeyFields;
    }
    internal class JobObjectKey : SimpleObjectKey
    {
        int categoryID;
        static readonly JobObjectKeySingleton jobObjectKeySingleton =
            new JobObjectKeySingleton();

        internal static StringCollection jobKeyFields
        {
            get { return jobObjectKeySingleton.jobKeyFields; }
        }

        public int CategoryID
        {
            get { return categoryID; }
            set { categoryID = value; }
        }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="categoryID"></param>
        public JobObjectKey(string name, int categoryID)
            : base(name)
        {
            this.categoryID = categoryID;
        }


        static JobObjectKey()
        {
            jobObjectKeySingleton.jobKeyFields = new StringCollection();
            jobObjectKeySingleton.jobKeyFields.Add("Name");
            jobObjectKeySingleton.jobKeyFields.Add("CategoryID");
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return name;
        }

        /// <summary>
        /// Urn suffix that identifies this object
        /// </summary>
        /// <value></value>
        public override string UrnFilter
        {
            get
            {
                if (this.categoryID == -1)
                {
                    // This should never get called when all clients are fixed
                    return string.Format(SmoApplication.DefaultCulture, "@Name='{0}'", Urn.EscapeString(name));
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture, "@Name='{0}' and @CategoryID='{1}'", Urn.EscapeString(name), this.categoryID.ToString(SmoApplication.DefaultCulture));
                }
            }
        }

        /// <summary>
        /// Return all fields that are used by this key.
        /// </summary>
        /// <returns></returns>
        public override StringCollection GetFieldNames()
        {
            return jobObjectKeySingleton.jobKeyFields;
        }

        /// <summary>
        /// Clone the object.
        /// </summary>
        /// <returns></returns>
        public override ObjectKeyBase Clone()
        {
            return new JobObjectKey(this.name, this.categoryID);
        }

        internal override void Validate(Type objectType)
        {
        }

        /// <summary>
        /// True if the key is null.
        /// </summary>
        /// <value></value>
        public override bool IsNull
        {
            get { return false; } // Never null
        }

        /// <summary>
        /// Returns string comparer needed to compare the string portion of this key.
        /// </summary>
        /// <param name="stringComparer"></param>
        /// <returns></returns>
        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new JobObjectComparer(stringComparer);
        }
    }

    // here we have this customized collection

    public class JobCollection : ArrayListCollectionBase
    {
        internal JobCollection(SqlSmoObject parent)
            : base(parent)
        {
        }

        /// <summary>
        /// Initializes the storage
        /// </summary>
        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoArrayList(new JobObjectComparer(this.StringComparer), this);
        }

        public bool Contains(string name)
        {
            IEnumerator ie = GetEnumerator();
            while (ie.MoveNext())
            {
                Job mt = (Job)ie.Current;
                if (this.StringComparer.Compare(mt.Name, name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains(string name, int categoryID)
        {
            IEnumerator ie = GetEnumerator();
            while (ie.MoveNext())
            {
                Job mt = (Job)ie.Current;
                if (this.StringComparer.Compare(mt.Name, name) == 0 && mt.CategoryID == categoryID)
                {
                    return true;
                }
            }
            return false;
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            string name = urn.GetAttribute("Name");
            string categoryIDstring = urn.GetAttribute("CategoryID");
            int categoryID = categoryIDstring == null ? -1 : Int32.Parse(categoryIDstring, SmoApplication.DefaultCulture);
            return new JobObjectKey(name, categoryID);
        }

        /// <summary>
        /// ItemById
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Job ItemById(System.Guid id)
        {
            IEnumerator ie = GetEnumerator();
            while (ie.MoveNext())
            {
                Job mt = (Job)ie.Current;

                // check to see if the current object is the one we are looking for
                Property pID = mt.Properties.Get("JobID");
                if (null != pID.Value)
                {
                    if (id.Equals(pID.Value))
                    {
                        return mt;
                    }
                }
                else
                {
                    // if the property's value is null, check to see if the object needs to be initialized
                    if (mt.State != SqlSmoState.Creating)
                    {
                        mt.Initialize(true);
                    }

                    // try to do the comparison for the second time
                    if (null != pID.Value && id.Equals(pID.Value))
                    {
                        return mt;
                    }
                }
            }

            return null;
        }

        public JobServer Parent
        {
            get
            {
                return this.ParentInstance as JobServer;
            }
        }

        public Job this[Int32 index]
        {
            get
            {
                return GetObjectByIndex(index) as Job;
            }
        }

        public Job this[string name]
        {
            get
            {
                return GetObjectByName(name) as Job;
            }
        }

        public Job this[string name, int categoryID]
        {
            get
            {
                IEnumerator ie = GetEnumerator();
                while (ie.MoveNext())
                {
                    Job mt = (Job)ie.Current;
                    if (this.StringComparer.Compare(mt.Name, name) == 0 && mt.CategoryID == categoryID)
                    {
                        return mt;
                    }
                }

                return null;
            }
        }

        public void CopyTo(Job[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        public StringCollection Script()
        {
            return this.Script(new ScriptingOptions());
        }

        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            if (this.Count <= 0)
            {
                return new StringCollection();
            }

            SqlSmoObject[] scriptList = new SqlSmoObject[this.Count];
            int i = 0;
            foreach (SqlSmoObject o in this)
            {
                scriptList[i++] = o;
            }
            Scripter scr = new Scripter(scriptList[0].GetServerObject());
            scr.Options = scriptingOptions;
            return scr.Script(scriptList);
        }

        protected override Type GetCollectionElementType()
        {
            return typeof(Job);
        }

        internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return new Job(this, key, state);
        }

        public void Add(Job job)
        {
            AddImpl(job);
        }

        internal SqlSmoObject GetObjectByName(string name)
        {
            IEnumerator ie = GetEnumerator();
            while (ie.MoveNext())
            {
                Job mt = (Job)ie.Current;
                if (this.StringComparer.Compare(mt.Name, name) == 0)
                {
                    return mt;
                }
            }

            return null;
        }

        internal override SqlSmoObject GetObjectByKey(ObjectKeyBase key)
        {
            JobObjectKey jkey = (JobObjectKey)key;
            // Find the best match
            IEnumerator ie = GetEnumerator();
            while (ie.MoveNext())
            {
                Job mt = (Job)ie.Current;

                if (this.StringComparer.Compare(mt.Name, jkey.Name) == 0)
                {
                    if (jkey.CategoryID == -1)
                    {
                        // As good as it gets -- any will do (Note: this is only for backwards compatibility with old callers that don't use CategoryID in Urn)
                        return mt;
                    }

                    if (mt.CategoryID == jkey.CategoryID)
                    {
                        // Bingo: exact match
                        return mt;
                    }
                }
            }

            return null;

        }

    }

    public partial class Job : AgentObjectBase, Cmn.IAlterable, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IScriptable
    {
        public Job()
            : base()
        {
            this.key = new JobObjectKey(null, 0);
        }

        // Caller beware: use the new ctor instead and pass categoryID explicitly
        public Job(JobServer jobServer, string name)
            : base()
        {
            ValidateName(name);
            this.key = new JobObjectKey(name, 0);     // 0 is for "local"
            this.Parent = jobServer;
        }

        public Job(JobServer jobServer, string name, int categoryID)
            : base()
        {
            ValidateName(name);
            this.key = new JobObjectKey(name, categoryID);
            this.Parent = jobServer;
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return ((JobObjectKey)key).Name;
            }
            set
            {
                try
                {
                    ValidateName(value);
                    ((JobObjectKey)key).Name = value;
                    UpdateObjectState();
                }
                catch (Exception e)
                {
                    FilterException(e);

                    throw new FailedOperationException(ExceptionTemplates.SetName, this, e);
                }

            }
        }

        internal Job(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            jobSteps = null;
            jobSchedules = null;
        }

        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public int CategoryID
        {
            get
            {
                return ((JobObjectKey)key).CategoryID;
            }
            // Because there is no setter, no one can change CategoryID
            // and get it out of sync with Category property. So, we're cool here.
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Job";
            }
        }

        // User might set Category property and then call Alter or Create on the object
        // This function ensures that CategoryID property is in sync with Category property
        // The only reason to use this method and not UpdateCategoryIDFromServer is to avoid
        // unnecessary trip to the server: it is only necessary when Category changes between 
        // consructor and Alter/Create
        private void UpdateCategoryIDFromCategoryProperty()
        {
            Diagnostics.TraceHelper.Assert(this.Parent != null);

            string categoryName = (string)Properties.Get("Category").Value;

            if (categoryName == null)
            {
                // this means that Category name wasn't set and therefore CategoryID can't be out of sync
                // So we don't need to update the CategoryID
            }
            else
            {
                // We need to update CategoryID based on the Category that user has specified
                JobCategory jobcat = this.Parent.JobCategories[categoryName];
                if (jobcat == null)
                {
                    // Last chance: maybe JobCategories are out of date? Refresh the collection and try again
                    this.Parent.JobCategories.Refresh();
                    jobcat = this.Parent.JobCategories[categoryName];
                }

                if (jobcat == null)
                {
                    // A category with such name doesn't exist. We're doomed
                    throw new InternalSmoErrorException(ExceptionTemplates.UnknownCategoryName(categoryName));
                }
                else
                {
                    jobcat.Initialize(true);
                    ((JobObjectKey)key).CategoryID = jobcat.ID;
                }
            }
        }

        // ApplyToTargetServer et al silently change object's CategoryID when the changes from TSX to MSX or vice versa
        // Unlike UpdateCategoryIDFromCategoryProperty, this function always goes to the server to retrieve the data
        // Tough, but necessary.
        private void UpdateCategoryIDFromServer()
        {
            if (!this.ExecutionManager.Recording)
            {
                string oldUrn = this.Urn;

                int oldCategoryID = ((JobObjectKey)this.key).CategoryID;
                int newCategoryID = (int)this.ExecutionManager.ExecuteScalar(string.Format(SmoApplication.DefaultCulture,
                    "select category_id from msdb.dbo.sysjobs_view where job_id='{0}'", this.JobID));

                if (newCategoryID != oldCategoryID)
                {
                    ((JobObjectKey)this.key).CategoryID = newCategoryID;

                    if (!SmoApplication.eventsSingleton.IsNullObjectCreated() && !SmoApplication.eventsSingleton.IsNullObjectDropped())
                    {
                        SmoApplication.eventsSingleton.CallObjectDropped(GetServerObject(), new ObjectDroppedEventArgs(oldUrn));
                        SmoApplication.eventsSingleton.CallObjectCreated(GetServerObject(), new ObjectCreatedEventArgs(this.Urn, this));
                    }
                }
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}[{1}]", UrnSuffix, ((JobObjectKey)key).UrnFilter);
        }

        /// <summary>
        /// Create
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);

            if (sp.ForDirectExecution)
            {
                GetJobCreationScript(createQuery, sp);
                UpdateCategoryIDFromCategoryProperty();
            }
            else
            {
                GetJobScriptingScript(createQuery, sp);
            }
            queries.Add(createQuery.ToString());
        }

        private void DumpStringCollectionToBuilder(StringCollection coll, StringBuilder script)
        {
            foreach (string s in coll)
            {
                script.Append(s);
                script.Append(Globals.newline);
            }
        }

        private void GetJobScriptingScript(StringBuilder createQuery, ScriptingPreferences sp)
        {
            this.Initialize(true);
            createQuery.Append("BEGIN TRANSACTION");
            createQuery.Append(Globals.newline);
            createQuery.Append("DECLARE @ReturnCode INT");
            createQuery.Append(Globals.newline);
            createQuery.Append("SELECT @ReturnCode = 0");
            createQuery.Append(Globals.newline);

            bool inScriptJob = sp.Agent.InScriptJob;
            sp.Agent.InScriptJob= true;

            StringCollection queries = new StringCollection();
            // script Category creation
            if (this.Category.Length > 0)
            {
                JobCategory jobcat = this.Parent.JobCategories[this.Category];
                if (null != jobcat)
                {
                    jobcat.Initialize(true);

                    // force category to be scripted with "if not exists ..."
                    bool includeIfNotExists = sp.IncludeScripts.ExistenceCheck;
                    sp.IncludeScripts.ExistenceCheck = true;
                    jobcat.ScriptCreateInternal(queries, sp);
                    sp.IncludeScripts.ExistenceCheck = includeIfNotExists;
                }
                DumpStringCollectionToBuilder(queries, createQuery);
                queries.Clear();
            }

            // script the job creation
            GetJobCreationScript(createQuery, sp);

            // script the job steps
            foreach (JobStep step in this.JobSteps)
            {
                step.Initialize(true);
                step.ScriptCreateInternal(queries, sp);
                DumpStringCollectionToBuilder(queries, createQuery);
                queries.Clear();
                AddCheckErrorCode(createQuery);
            }

            // update the starting step
            object startStep = this.GetPropValueOptional("StartStepID");
            if (null != startStep)
            {
                createQuery.AppendFormat(
                                         "EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @jobId, @start_step_id = {0}", (int)startStep);
                createQuery.Append(Globals.newline);
                AddCheckErrorCode(createQuery);
            }

            // add JobSchedules
            foreach (JobSchedule sched in this.JobSchedules)
            {
                sched.Initialize(true);
                sched.ScriptCreateInternal(queries, sp);
                DumpStringCollectionToBuilder(queries, createQuery);
                queries.Clear();
                AddCheckErrorCode(createQuery);
            }

            // add the job to target servers
            if (this.State == SqlSmoState.Existing)
            {
                // If the job is targeted at the local server we will translate the 
                // server name to (local). This allows a script to be run against another
                // server without modification
                String targetServerName;

                DataTable targetServers = EnumTargetServers();
                foreach (DataRow row in targetServers.Rows)
                {
                    targetServerName = (string)row["ServerName"];
                    if (String.Compare(targetServerName, this.ExecutionManager.TrueServerName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        targetServerName = "(local)";
                    }
                    createQuery.AppendFormat(SmoApplication.DefaultCulture,
                                             "EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = N'{0}'",
                                             SqlString(targetServerName));
                    createQuery.Append(Globals.newline);
                    AddCheckErrorCode(createQuery);
                }
            }

            createQuery.Append("COMMIT TRANSACTION");
            createQuery.Append(Globals.newline);
            createQuery.Append("GOTO EndSave");
            createQuery.Append(Globals.newline);
            createQuery.Append("QuitWithRollback:");
            createQuery.Append(Globals.newline);
            createQuery.Append("    IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION");
            createQuery.Append(Globals.newline);
            createQuery.Append("EndSave:");
            createQuery.Append(Globals.newline);

            queries.Add(createQuery.ToString());

            //Restore sp.Agent.InScriptJob to its original value
            sp.Agent.InScriptJob = inScriptJob;
        }

        internal static void AddCheckErrorCode(StringBuilder query)
        {
            query.Append("IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback");
            query.Append(Globals.newline);
        }

        internal static string GetReturnCode(ScriptingPreferences sp)
        {
            if (sp.Agent.InScriptJob)
            {
                return "@ReturnCode = ";
            }
            else
            {
                return string.Empty;
            }
        }

        private void GetJobCreationScript(StringBuilder createQuery, ScriptingPreferences sp)
        {
            if (sp.OldOptions.PrimaryObject)
            {
                createQuery.Append("DECLARE @jobId BINARY(16)");
                createQuery.Append(Globals.newline);
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    createQuery.AppendFormat("select @jobId = job_id from msdb.dbo.sysjobs where (name = N'{0}')", SqlString(this.Name));
                    createQuery.Append(Globals.newline);
                    createQuery.Append("if (@jobId is NULL)");
                    createQuery.Append(Globals.newline);
                    createQuery.Append("BEGIN");
                    createQuery.Append(Globals.newline);
                }

                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                                         "EXEC {0} msdb.dbo.sp_add_job @job_name=N'{1}'",
                                         GetReturnCode(sp),
                                         SqlString(this.Name));

                int count = 1;
                GetAllParams(createQuery, sp, /*forAlter=*/ false, ref count);

                createQuery.Append(", @job_id = @jobId OUTPUT");
                if (sp.Agent.InScriptJob)
                {
                    createQuery.Append(Globals.newline);
                    AddCheckErrorCode(createQuery);
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    createQuery.Append(Globals.newline);
                    createQuery.Append("END");
                    createQuery.Append(Globals.newline);
                }

                if (sp.ForDirectExecution)
                {
                    createQuery.Append(Globals.newline);
                    createQuery.Append("select @jobId");
                }
            }
        }

        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, bool forAlter, ref int count)
        {
            GetBoolParameter(sb, sp, "IsEnabled", "@enabled={0}", ref count);
            if (sp.ScriptForAlter || sp.ForDirectExecution)
            {
                GetParameter(sb, sp, "StartStepID", "@start_step_id={0}", ref count);
            }
            GetEnumParameter(sb, sp, "EventLogLevel", "@notify_level_eventlog={0}",
                              typeof(CompletionAction), ref count);
            GetEnumParameter(sb, sp, "EmailLevel", "@notify_level_email={0}",
                              typeof(CompletionAction), ref count);
            GetEnumParameter(sb, sp, "NetSendLevel", "@notify_level_netsend={0}",
                              typeof(CompletionAction), ref count);
            GetEnumParameter(sb, sp, "PageLevel", "@notify_level_page={0}",
                              typeof(CompletionAction), ref count);
            GetEnumParameter(sb, sp, "DeleteLevel", "@delete_level={0}",
                              typeof(CompletionAction), ref count);
            GetStringParameter(sb, sp, "Description", "@description=N'{0}'", ref count);

            // For backwards compatibility reasons, name takes precedence over category id
            // (Old clients used to set name and not ID -- but ID is now part of the key and is always set)
            if (Properties.Get("Category").Value != null)
            {
                GetStringParameter(sb, sp, "Category", "@category_name=N'{0}'", ref count);
            }
            else if (!forAlter && count++ > 0) // sp_update_job doesn't recognize category_id parameter, so don't emit it for Alter
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}{1}{2}{3}@category_id={4}",
                    Globals.commaspace,
                    Globals.newline,
                    Globals.tab,
                    Globals.tab,
                    this.CategoryID);
            }

            GetStringParameter(sb, sp, "OwnerLoginName", "@owner_login_name=N'{0}'", ref count);

            // the three properties below are expensive, so we need to fetch them explicitly 
            // before scripting
            GetPropValueOptional("OperatorToEmail");
            GetStringParameter(sb, sp, "OperatorToEmail", "@notify_email_operator_name=N'{0}'", ref count);
            GetPropValueOptional("OperatorToNetSend");
            GetStringParameter(sb, sp, "OperatorToNetSend", "@notify_netsend_operator_name=N'{0}'", ref count);
            GetPropValueOptional("OperatorToPage");
            GetStringParameter(sb, sp, "OperatorToPage", "@notify_page_operator_name=N'{0}'", ref count);
        }

        private bool keepUnusedSchedules = false; // !keepUnusedSchedule corresponds to the delete_unused_schedule t-sql param while deleting schedule/job.-anchals

        /// <summary>
        /// Drop
        /// </summary>
        /// <param name="keepUnusedSchedules"></param>
        public void Drop(bool keepUnusedSchedules)
        {
            ThrowIfBelowVersion90();

            try
            {
                this.keepUnusedSchedules = keepUnusedSchedules;
                base.DropImpl();
            }
            finally
            { // we want to clear the instance member so this keyword is necessary.
                this.keepUnusedSchedules = false;
            }
        }

        /// <summary>
        /// Drop
        /// </summary>
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
                                Scripts.INCLUDE_EXISTS_AGENT_JOB,
                                "",
                                FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            if (sp.TargetServerVersionInternal <= SqlServerVersionInternal.Version80)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC msdb.dbo.sp_delete_job {0}",
                                JobIdOrJobNameParameter(sp));
            }
            else
            {
                // converting bool to int as the t-sql param needs to be int. -anchals
                int deleteUnusedParam = keepUnusedSchedules ? 0 : 1;
                sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC msdb.dbo.sp_delete_job {0}, @delete_unused_schedule={1}",
                                JobIdOrJobNameParameter(sp), deleteUnusedParam);
            }

            queries.Add(sb.ToString());
        }

        /// <summary>
        /// Alter
        /// </summary>
        public void Alter()
        {
            try
            {
                string oldName = this.Name;
                string oldUrn = this.Urn;

                // When Job's category changes, Rename is the right event to fire, since Category is
                // part of the job's key and conceptually can be seen as part of the name
                bool needRenameEvent = Properties.Get("Category").Dirty;
                AlterImplWorker();

                if (!this.ExecutionManager.Recording)
                {
                    // generate internal events
                    if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                    {
                        if (needRenameEvent)
                        {
                            if (!SmoApplication.eventsSingleton.IsNullObjectRenamed())
                            {
                                SmoApplication.eventsSingleton.CallObjectRenamed(GetServerObject(), new ObjectRenamedEventArgs(this.Urn, this, oldName, this.Name, oldUrn));
                            }
                        }
                        else
                        {
                            if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                            {
                                SmoApplication.eventsSingleton.CallObjectAltered(GetServerObject(), new ObjectAlteredEventArgs(this.Urn, this));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
            }

        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                                    "EXEC msdb.dbo.sp_update_job {0}",
                                    JobIdOrJobNameParameter());

            int count = 1;
            GetAllParams(alterQuery, sp, /*forAlter=*/ true, ref count);

            if (count > 1)
            {
                queries.Add(alterQuery.ToString());

                if (sp.ForDirectExecution)
                {
                    UpdateCategoryIDFromCategoryProperty();
                }
            }
        }

        /// <summary>
        /// Rename
        /// </summary>
        /// <param name="newName"></param>
        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            queries.Add(string.Format(SmoApplication.DefaultCulture,
                                       "EXEC msdb.dbo.sp_update_job {0}, @new_name=N'{1}'",
                                       JobIdOrJobNameParameter(),
                                       SqlString(newName)));
        }

        private JobStepCollection jobSteps;
        /// <summary>
        /// JobSteps
        /// </summary>
        /// <value></value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(JobStep))]
        public JobStepCollection JobSteps
        {
            get
            {
                CheckObjectState();
                if (null == jobSteps)
                {
                    jobSteps = new JobStepCollection(this);
                }
                return jobSteps;
            }
        }

        private JobScheduleCollection jobSchedules;
        /// <summary>
        /// JobSchedules
        /// </summary>
        /// <value></value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(JobSchedule))]
        public JobScheduleCollection JobSchedules
        {
            get
            {
                CheckObjectState();
                if (null == jobSchedules)
                {
                    jobSchedules = new JobScheduleCollection(this);
                    jobSchedules.AcceptDuplicateNames = true;
                }
                return jobSchedules;
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if (null != jobSchedules)
            {
                jobSchedules.MarkAllDropped();
            }

            if (null != jobSteps)
            {
                jobSteps.MarkAllDropped();
            }
        }

        internal System.Guid JobIDInternal
        {
            get { return (System.Guid)Properties["JobID"].Value; }
        }

        internal string JobIdOrJobNameParameter()
        {
            return JobIdOrJobNameParameter(null);
        }

        // this is an overload for JobIDOrJobNameParameter which always returns the prefixed assignment Code (@job_id=...) -anchals
        internal string JobIdOrJobNameParameter(ScriptingPreferences sp)
        {
            return JobIdOrJobNameParameter(sp, true);
        }

        // This function tries to emit JobID and not name always, except for cases when the user explicitly asks not to use it
        // (Setting AgentJobId to false)
        // prefixAssignmentCode when true would prefix the return string with @job_id=. otherwise the jobid is returned as string -anchals
        internal string JobIdOrJobNameParameter(ScriptingPreferences sp, bool prefixAssignmentCode)
        {
            if (sp != null && sp.Agent.InScriptJob)
            {
                return prefixAssignmentCode ? ("@job_id=@jobId")
                    : "@jobId";
            }
            else
            {
                if (sp == null || sp.ScriptForCreateDrop || sp.Agent.JobId) // beware: option AgentJobId should really be deprecated, as Jobs are not uniquely identifiable by names
                {
                    string outFormat;
                    if (prefixAssignmentCode)
                    {
                        outFormat = "@job_id=N'{0}'";
                    }
                    else
                    {
                        outFormat = "N'{0}'";
                    }
                    // check if @job_id parameter value is available, if not use @job_name instead
                    object pID = GetPropValueOptional("JobID");
                    if (null != pID)
                    {
                        return String.Format(SmoApplication.DefaultCulture, outFormat, SqlString(this.JobIDInternal.ToString("D", SmoApplication.DefaultCulture)));
                    }
                }

                return String.Format(SmoApplication.DefaultCulture, "@job_name=N'{0}'", SqlString(this.Name));
            }
        }

        /// <summary>
        /// ApplyToTargetServer adds an execution target to the list of targets 
        /// maintained for the referenced Agent job.
        /// </summary>
        /// <param name="serverName">Name of the target server. When null or
        /// empty string the job is applied against local server.</param>
        public void ApplyToTargetServer(string serverName)
        {
            try
            {
                CheckObjectState();

                this.ExecutionManager.ExecuteNonQuery(string.Format(SmoApplication.DefaultCulture,
                                           "EXEC msdb.dbo.sp_add_jobserver {0}, @server_name = {1}",
                                           JobIdOrJobNameParameter(),
                                           String.IsNullOrEmpty(serverName) ? "NULL" : MakeSqlString(serverName)));

                UpdateCategoryIDFromServer();
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ApplyToTargetServer, this, e);
            }
        }

        /// <summary>
        /// ApplyToTargetServerGroup adds one or more execution targets to the 
        /// list of targets maintained for the referenced Agent job.
        /// </summary>
        /// <param name="groupName"></param>
        public void ApplyToTargetServerGroup(string groupName)
        {
            try
            {
                if (null == groupName)
                {
                    throw new ArgumentNullException("groupName");
                }

                CheckObjectState();

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                           "EXEC msdb.dbo.sp_apply_job_to_targets {0}, @target_server_groups = N'{1}'",
                                           JobIdOrJobNameParameter(),
                                           SqlString(groupName)));

                this.ExecutionManager.ExecuteNonQuery(queries);
                UpdateCategoryIDFromServer();
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ApplyToTargetServerGroup, this, e);
            }
        }

        /// <summary>
        /// This method returns a DataTable object that enumerates the 
        /// Agent alerts that cause automated execution of the referenced job.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumAlerts()
        {
            try
            {
                CheckObjectState(true);

                Request r = new Request(this.Urn.Parent + "/Alert[@JobID='" + this.JobID.ToString() + "']");
                return this.ExecutionManager.GetEnumeratorData(r);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumAlerts, this, e);
            }
        }

        /// <summary>
        /// This method returns a DateTable object that enumerates the 
        /// execution history of the referenced  Agent job.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public DataTable EnumHistory(JobHistoryFilter filter)
        {
            try
            {
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData(filter.GetEnumRequest(this));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumHistory, this, e);
            }
        }

        /// <summary>
        /// This method returns a DateTable object that enumerates the 
        /// execution history of the referenced Agent job.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumHistory()
        {
            try
            {
                CheckObjectState(true);
                Request req = new Request(this.Urn + "/History");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumHistory, this, e);
            }
        }

        /// <summary>
        /// This method returns a DateTable object that enumerates the job step execution 
        /// output logs if they were saved to the table
        /// </summary>
        /// <returns></returns>
        public DataTable EnumJobStepLogs()
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/Step/OutputLog"));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobStepOutputLogs, this, e);
            }

        }

        /// <summary>
        /// This method returns a DateTable object that enumerates the job step execution 
        /// output logs if they were saved to the table filtering by step ID
        /// </summary>
        /// <returns></returns>
        public DataTable EnumJobStepLogs(int stepId)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + string.Format(SmoApplication.DefaultCulture, "/Step[@ID={0}]/OutputLog", stepId)));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobStepOutputLogs, this, e);
            }

        }

        /// <summary>
        /// This method returns a DateTable object that enumerates the job step execution 
        /// output logs if they were saved to the table filtering by step name
        /// </summary>
        /// <returns></returns>
        public DataTable EnumJobStepLogs(string stepName)
        {
            try
            {
                if (null == stepName)
                {
                    throw new ArgumentNullException("stepName");
                }

                ThrowIfBelowVersion90();
                CheckObjectState(true);

                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + string.Format(SmoApplication.DefaultCulture, "/Step[@Name='{0}']/OutputLog", Urn.EscapeString(stepName))));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobStepOutputLogs, this, e);
            }

        }

        /// <summary>
        /// This method returns a DateTable object that enumerates the execution 
        /// targets of the referenced Agent job.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumTargetServers()
        {
            try
            {
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/TargetServer"));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumTargetServers, this, e);
            }

        }

        /// <summary>
        /// EnumJobStepsByID
        /// </summary>
        /// <returns></returns>
        public JobStep[] EnumJobStepsByID()
        {
            try
            {
                CheckObjectState(true);
                DataTable dt = this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/Step", new string[] { "Name" },
                                                                                     new OrderBy[] { new OrderBy("ID", OrderBy.Direction.Asc) }));
                JobStep[] steps = new JobStep[dt.Rows.Count];
                int idx = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    steps[idx++] = this.JobSteps[(string)dr["Name"]];
                }

                return steps;
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumJobSteps, this, e);
            }
        }

        /// <summary>
        /// Delete job step logs old than specified date
        /// </summary>
        public void DeleteJobStepLogs(DateTime olderThan)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture,
                                       "EXEC msdb.dbo.sp_delete_jobsteplog @job_name=N'{0}', @older_than='{1}'",
                                       SqlString(this.Name), olderThan.ToString("MM/dd/yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo));
                this.ExecutionManager.ExecuteNonQuery(statement.ToString());
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
        public void DeleteJobStepLogs(int largerThan)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture,
                                       "EXEC msdb.dbo.sp_delete_jobsteplog @job_name=N'{0}', @larger_than={1}",
                                       SqlString(this.Name), largerThan);
                this.ExecutionManager.ExecuteNonQuery(statement.ToString());
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DeleteJobStepLogs, this, e);
            }
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public void Invoke()
        {
            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_sqlagent_notify @op_type = N'J', @job_id = N'{0}', @schedule_id = NULL, @alert_id = NULL, @action_type = N'S'",
                                          SqlString(this.JobIDInternal.ToString("D", SmoApplication.DefaultCulture))));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Invoke, this, e);
            }

        }

        /// <summary>
        /// This method removes system records maintaining execution history 
        /// for the referenced Agent job.
        /// </summary>
        public void PurgeHistory()
        {
            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_purge_jobhistory {0}",
                                           JobIdOrJobNameParameter()));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.PurgeHistory, this, e);
            }
        }

        /// <summary>
        /// AddSharedSchedule
        /// </summary>
        public void AddSharedSchedule(int scheduleId)
        {
            try
            {
                ThrowIfBelowVersion90();

                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                           "EXEC msdb.dbo.sp_attach_schedule {0},@schedule_id={1}",
                                           JobIdOrJobNameParameter(), scheduleId));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddSharedSchedule, this, e);
            }
        }

        /// <summary>
        /// Get schedule object associated with this job for given schedule Id
        /// if invalid id is passed in, ItemById() API can return null
        /// this helper method throws missing object exception if get into that situation
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <returns></returns>
        private JobSchedule GetJobScheduleByID(int scheduleId)
        {
            // it is possible that cached copy might be stale, refreshing the collection before looking up using indexer
            JobSchedules.Refresh();
            JobSchedule jobSchedule = JobSchedules.ItemById(scheduleId);

            if (null == jobSchedule)
            {
                throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist("ScheduleId", scheduleId.ToString()));
            }

            return jobSchedule;
        }

        /// <summary>
        /// RemoveSharedSchedule
        /// </summary>
        public void RemoveSharedSchedule(int scheduleId)
        {
            try
            {
                // we don't need a version check since JobSchedule.Drop would take care of it.
                // schedule drop would also take care of extra scripting options also. (and it will have our default behaviour)
                // which is to remove the schedule also if its no longer used.-anchals
                this.GetJobScheduleByID(scheduleId).Drop();
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveSharedSchedule, this, e);
            }
        }

        /// <summary>
        /// RemoveSharedSchedule
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="keepUnusedSchedules"></param>
        public void RemoveSharedSchedule(int scheduleId, bool keepUnusedSchedules)
        {
            try
            {
                // we don't need a version check since JobSchedule.Drop would take care of it. -anchals
                this.GetJobScheduleByID(scheduleId).Drop(keepUnusedSchedules);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveSharedSchedule, this, e);
            }
        }

        /// <summary>
        /// RemoveAllJobSchedules
        /// </summary>
        public void RemoveAllJobSchedules()
        {
            try
            {
                while (JobSchedules != null && JobSchedules.Count > 0)
                {
                    // by default Jobchedule::Drop only drops the schedule if its not shared. -anchals
                    JobSchedules[0].Drop();
                }

                jobSchedules = null;
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveAllJobSchedules, this, e);
            }
        }

        /// <summary>
        /// RemoveAllJobSchedules
        /// </summary>
        /// <param name="keepUnusedSchedules"></param>
        public void RemoveAllJobSchedules(bool keepUnusedSchedules)
        {
            try
            {
                // delegate the drop responsibility to each JobSchedule.
                // Note: sp_delete_jobschedule has been deprecated in BOL.-anchals
                while (JobSchedules != null && JobSchedules.Count > 0)
                {
                    // by default Jobchedule::Drop only drops the schedule if its not shared.
                    JobSchedules[0].Drop(keepUnusedSchedules);
                }
                jobSchedules = null;
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveAllJobSchedules, this, e);
            }
        }

        /// <summary>
        /// RemoveAllJobSteps
        /// </summary>
        public void RemoveAllJobSteps()
        {
            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                           "EXEC msdb.dbo.sp_delete_jobstep {0}, @step_id = 0",
                                           JobIdOrJobNameParameter()));

                this.ExecutionManager.ExecuteNonQuery(queries);

                jobSteps = null;
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveAllJobSteps, this, e);
            }
        }

        /// <summary>
        /// RemoveFromTargetServer
        /// </summary>
        /// <param name="serverName"></param>
        public void RemoveFromTargetServer(string serverName)
        {
            try
            {
                if (null == serverName)
                {
                    throw new ArgumentNullException("serverName");
                }

                CheckObjectState(true);

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                           "EXEC msdb.dbo.sp_delete_jobserver {0}, @server_name = N'{1}'",
                                           JobIdOrJobNameParameter(),
                                           SqlString(serverName)));

                this.ExecutionManager.ExecuteNonQuery(queries);
                UpdateCategoryIDFromServer();
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveFromTargetServer, this, e);
            }
        }

        /// <summary>
        /// RemoveFromTargetServerGroup
        /// </summary>
        /// <param name="groupName"></param>
        public void RemoveFromTargetServerGroup(string groupName)
        {
            try
            {
                if (null == groupName)
                {
                    throw new ArgumentNullException("groupName");
                }

                CheckObjectState(true);

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                           "EXEC msdb.dbo.sp_remove_job_from_targets {0}, @target_server_groups = N'{1}'",
                                           JobIdOrJobNameParameter(),
                                           SqlString(groupName)));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveFromTargetServerGroup, this, e);
            }
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="jobStepName">starting job step</param>
        public void Start(string jobStepName)
        {
            try
            {
                if (null == jobStepName)
                {
                    throw new ArgumentNullException("jobStepName");
                }

                CheckObjectState(true);

                StartImpl(jobStepName);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Start, this, e);
            }
        }

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            try
            {
                CheckObjectState(true);
                StartImpl(null);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Start, this, e);
            }
        }

        internal void StartImpl(string jobStepName)
        {
            StringCollection queries = new StringCollection();
            StringBuilder command = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            command.AppendFormat(SmoApplication.DefaultCulture,
                                 "EXEC msdb.dbo.sp_start_job {0}",
                                 JobIdOrJobNameParameter());

            if (null != jobStepName)
            {
                command.AppendFormat(SmoApplication.DefaultCulture, ", @step_name=N'{0}' ", SqlString(jobStepName));
            }
            queries.Add(command.ToString());

            this.ExecutionManager.ExecuteNonQuery(queries);
        }

        /// <summary>
        /// Stop
        /// </summary>
        public void Stop()
        {
            try
            {
                CheckObjectState(true);

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                           "EXEC msdb.dbo.sp_stop_job {0}",
                                           JobIdOrJobNameParameter()));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Stop, this, e);
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

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType, 
            Cmn.ServerVersion version, 
            Cmn.DatabaseEngineType databaseEngineType, 
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            string[] fields = {   
                         "JobID" };
            List<string> list = GetSupportedScriptFields(typeof(Job.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }

    public sealed class JobHistoryFilter
    {
        private System.Guid jobID = Guid.Empty;
        public System.Guid JobID
        {
            get { return jobID; }
            set { jobID = value; }
        }

        private string jobName = null;
        public string JobName
        {
            get { return jobName; }
            set { jobName = value; }
        }

        private int minimumRetries = 0;
        public int MinimumRetries
        {
            get { return minimumRetries; }
            set { minimumRetries = value; }
        }

        private int minimumRunDuration = 0;
        public int MinimumRunDuration
        {
            get { return minimumRunDuration; }
            set { minimumRunDuration = value; }
        }

        private bool oldestFirst = false;
        public bool OldestFirst
        {
            get { return oldestFirst; }
            set { oldestFirst = value; }
        }

        private CompletionResult outcomeTypes;
        bool outcomeDirty = false;
        public CompletionResult OutcomeTypes
        {
            get { return outcomeTypes; }
            set { outcomeDirty = true; outcomeTypes = value; }
        }

        private Int32 messageID = -1;
        public Int32 SqlMessageID
        {
            get { return messageID; }
            set { messageID = value; }
        }

        private Int32 severity = -1;
        public Int32 SqlSeverity
        {
            get { return severity; }
            set { severity = value; }
        }

        bool startRunDateDirty = false;
        private DateTime startRunDate;
        public DateTime StartRunDate
        {
            get { return startRunDate; }
            set { startRunDateDirty = true; startRunDate = value; }
        }

        bool endRunDateDirty = false;
        private DateTime endRunDate;
        public DateTime EndRunDate
        {
            get { return endRunDate; }
            set { endRunDateDirty = true; endRunDate = value; }
        }

        internal Request GetEnumRequest(Job job)
        {
            Request req = new Request();

            StringBuilder builder = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            builder.Append(job.ParentColl.ParentInstance.Urn);
            builder.AppendFormat(SmoApplication.DefaultCulture, "/Job[@JobID= '{0}']/History", job.JobIDInternal);
            GetRequestFilter(builder);
            req.Urn = builder.ToString();

            return req;
        }

        internal Request GetEnumRequest(JobServer jobServer)
        {
            Request req = new Request();
            StringBuilder builder = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            builder.Append(jobServer.Urn);
            builder.AppendFormat(SmoApplication.DefaultCulture, "/Job/History");
            GetRequestFilter(builder);
            req.Urn = builder.ToString();

            return req;
        }

        internal int GetDateInt(DateTime dateTime)
        {
            return (dateTime.Year * 10000 + dateTime.Month * 100 + dateTime.Day);
        }

        internal int GetTimeInt(DateTime dateTime)
        {
            return (dateTime.Hour * 10000 + dateTime.Minute * 100 + dateTime.Second);
        }

        private void GetRequestFilter(StringBuilder builder)
        {
            int count = 0;
            if (Guid.Empty != jobID)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@JobID='{0}'", jobID.ToString("D", SmoApplication.DefaultCulture));
            }

            if (null != jobName && jobName.Length > 0)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@JobName='{0}'", Urn.EscapeString(jobName));
            }

            if (minimumRetries > 0)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@RetriesAttempted > {0}", minimumRetries);
            }

            if (minimumRunDuration > 0)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@RunDuration > {0}", minimumRunDuration);
            }

            if (outcomeDirty)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@RunStatus = {0}", Enum.Format(typeof(CompletionResult), outcomeTypes, "d"));
            }

            if (messageID > 0)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@SqlMessageID = {0}", messageID);
            }

            if (severity > 0)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@SqlSeverity= {0}", severity);

            }

            if (startRunDateDirty)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@RunDate >= '{0}'", SqlSmoObject.SqlDateString(startRunDate));
                //builder.AppendFormat(SmoApplication.DefaultCulture,  "@RunDateInt >= {0} and @RunTimeInt >= {1}",GetDateInt(this.StartRunDate),GetTimeInt(this.StartRunDate) );
            }

            if (endRunDateDirty)
            {
                if (count++ > 0)
                {
                    builder.Append(" and ");
                }
                else
                {
                    builder.Append("[");
                }

                builder.AppendFormat(SmoApplication.DefaultCulture, "@RunDate <= '{0}'", SqlSmoObject.SqlDateString(endRunDate));
                //builder.AppendFormat(SmoApplication.DefaultCulture, "@RunDateInt <= {0} and @RunTimeInt <= {1}", GetDateInt(this.EndRunDate), GetTimeInt(this.EndRunDate));
            }

            if (count > 0)
            {
                builder.Append("]");
            }
        }

        /// <summary>
        /// Returns the filter used by PurgeJobHistory.
        /// </summary>
        /// <returns></returns>
        internal string GetPurgeFilter()
        {
            string filter = String.Empty;
            if (Guid.Empty != this.jobID)
            {
                filter = String.Format(SmoApplication.DefaultCulture, " @job_id=N'{0}'", jobID.ToString("D", SmoApplication.DefaultCulture));
            }
            else
            {
                // if JobID was not specified we try to filter by name
                if (null != jobName && jobName.Length > 0)
                {
                    if (0 < filter.Length)
                    {
                        filter += ",";
                    }
                    filter += String.Format(SmoApplication.DefaultCulture, " @job_name='{0}'", SqlSmoObject.SqlString(jobName));
                }
            }

            if (endRunDateDirty)
            {
                if (0 < filter.Length)
                {
                    filter += ",";
                }
                filter += String.Format(SmoApplication.DefaultCulture, " @oldest_date='{0}'", SqlSmoObject.SqlDateString(endRunDate));
            }
            return filter;
        }
    }

}

