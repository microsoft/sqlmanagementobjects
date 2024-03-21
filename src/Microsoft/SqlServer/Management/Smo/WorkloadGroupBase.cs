// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class that handles Creating, Altering, Dropping and Scripting the Workload group instance
    /// </summary>
    [Facets.StateChangeEvent("CREATE_WORKLOAD_GROUP", "WORKLOADGROUP", "WORKLOAD GROUP")]
    [Facets.StateChangeEvent("ALTER_WORKLOAD_GROUP", "WORKLOADGROUP", "WORKLOAD GROUP")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class WorkloadGroup : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable, IScriptable
    {
        #region Constructors
        private const string InternalPoolName = "internal";

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadGroup"/> class.
        /// </summary>
        /// <param name="parentColl">The parent coll.</param>
        /// <param name="key">The key.</param>
        /// <param name="state">The state.</param>
        internal WorkloadGroup(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
            m_comparer = ((ResourcePool)(parentColl.ParentInstance)).Parent.Parent.StringComparer;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates this instance.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Alters this instance.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Drops this instance.
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

        /// <summary>
        /// Name of WorkloadGroup
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
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
        /// Move Workload group to specified resource pool
        /// </summary>
        /// <param name="poolName"></param>
        /// <returns></returns>
        public void MoveToPool(string poolName)
        {
            CheckObjectState(true);
            MoveToPoolImpl(poolName);
        }

        /// <summary>
        /// Script Move Workload group to specified resource pool
        /// </summary>
        /// <param name="poolName"></param>
        /// <returns></returns>
        public StringCollection ScriptMoveToPool(string poolName)
        {
            StringCollection query = new StringCollection();

            query.Add(string.Format(SmoApplication.DefaultCulture,
                "ALTER WORKLOAD GROUP {0} USING {1}",
                MakeSqlBraket(SqlString(this.Name)),
                MakeSqlBraket(SqlString(poolName))));

            return query;
        }
        #endregion
        #region Protected Methods

        /// <summary>
        /// Generates Queries for Creating Workload group 
        /// </summary>
        /// <param name="queries">Queries string collection that has T-SQL queries</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, Include following check during Create
                // IF NOT EXISTS ( SELECT name FROM sys.resource_governor_workload_groups WHERE name = 'name')
                // BEGIN
                // ..
                // END
                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_RG_WORKLOADGROUP,
                    "NOT",
                    FormatFullNameForScripting(sp, false));

                createQuery.Append(sp.NewLine);
                createQuery.Append(Scripts.BEGIN);
                createQuery.Append(sp.NewLine);
            }

            // DDL to create a workload Group
            // Ex: CREATE WORKLOAD GROUP foo
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE WORKLOAD GROUP {0}",
                FormatFullNameForScripting(sp));

            int count = 0;
            GetAllParams(createQuery, sp, ref count);

            // Append Parameters, Ex:
            // USING( GroupMaximumRequests = 1, Importance='Medium')
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                " USING {0}",
                MakeSqlBraket(this.Parent.Name));

            // Starting SQL15, we added external pool option
            if (IsSupportedProperty("ExternalResourcePoolName"))
            {
                int countUsing = 0;
                StringBuilder parameters = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                GetParameter(parameters, sp, "ExternalResourcePoolName", "{0}", ref countUsing);

                if (0 < countUsing)
                {
                    createQuery.AppendFormat(SmoApplication.DefaultCulture, ", EXTERNAL {0}", MakeSqlBraket(parameters.ToString()));
                }
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, 
                // match the BEGIN clause with END
                createQuery.Append(sp.NewLine);
                createQuery.Append(Scripts.END);
                createQuery.Append(sp.NewLine);
            }

            queries.Add(createQuery.ToString());
        }

        /// <summary>
        /// Generates Queries for Altering Workload group 
        /// </summary>
        /// <param name="queries">Queries string collection that has T-SQL queries</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(alterQuery, sp, UrnSuffix);

            alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER WORKLOAD GROUP {0}",
                FormatFullNameForScripting(sp));

            int countParams = 0;
            GetAllParams(alterQuery, sp, ref countParams);

            // Starting SQL15, we added external pool option
            int countUsing = 0;
            if (IsSupportedProperty("ExternalResourcePoolName"))
            {
                StringBuilder parameters = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                GetParameter(parameters, sp, "ExternalResourcePoolName", "{0}", ref countUsing);

                if (0 < countUsing)
                {
                    alterQuery.AppendFormat(SmoApplication.DefaultCulture, " USING EXTERNAL {0}", MakeSqlBraket(parameters.ToString()));
                }
            }

            if (0 < countParams || 0 < countUsing)
            {
                queries.Add(alterQuery.ToString());
            }
        }

        /// <summary>
        /// Generates Queries for Dropping Workload group 
        /// </summary>
        /// <param name="queries">Queries string collection that has T-SQL queries</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder dropQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, Include following check during Drop
                // IF EXISTS ( SELECT name FROM sys.resource_governor_workload_groups WHERE name = 'name')
                // BEGIN
                // ..
                // END
                dropQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_RG_WORKLOADGROUP,
                    String.Empty,
                    FormatFullNameForScripting(sp, false));

                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.BEGIN);
                dropQuery.Append(sp.NewLine);
            }

            dropQuery.AppendFormat(SmoApplication.DefaultCulture,
                "DROP WORKLOAD GROUP {0}",
                FormatFullNameForScripting(sp));

            // If IncludeIfNotExists is set in scripting option, 
            // match the BEGIN clause with END
            if (sp.IncludeScripts.ExistenceCheck)
            {
                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.END);
                dropQuery.Append(sp.NewLine);
            }

            queries.Add(dropQuery.ToString());
        }

        /// <summary>
        /// Touch all the properties of this object
        /// </summary>
        protected override void TouchImpl()
        {
            this.GroupMaximumRequests = this.GroupMaximumRequests;
            this.Importance = this.Importance;
            this.RequestMaximumCpuTimeInSeconds = this.RequestMaximumCpuTimeInSeconds;
            if (IsSupportedProperty(nameof(RequestMaximumMemoryGrantPercentageAsDouble)))
            {
                this.RequestMaximumMemoryGrantPercentageAsDouble = this.RequestMaximumMemoryGrantPercentageAsDouble;
            }
            else
            {
                this.RequestMaximumMemoryGrantPercentage = this.RequestMaximumMemoryGrantPercentage;
            }
            this.RequestMemoryGrantTimeoutInSeconds = this.RequestMemoryGrantTimeoutInSeconds;
            this.MaximumDegreeOfParallelism = this.MaximumDegreeOfParallelism;
        }

        #endregion

        #region Private Methods

        private void InitComparer()
        {
            m_comparer = this.Parent.Parent.Parent.StringComparer;
        }

        /// <summary>
        /// Retrieve the properties that were set and generate appropriate T-SQL fragments
        /// If user has made some changes in values of WorkloadGroup, 
        /// then only GetParameter will increase "count" and 
        /// count increment will tell us that parameter value accepted, otherwise count remain unchanged. 
        /// </summary>
        /// <param name="sb">T-SQL string fragment</param>
        /// <param name="so">Scripting Options</param>
        /// <param name="count">The count.</param>
        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, ref int count)
        {
            StringBuilder parameters = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            GetParameter(parameters, sp, nameof(GroupMaximumRequests), "group_max_requests={0}", ref count);
            GetParameter(parameters, sp, nameof(Importance), "importance={0}", ref count);
            GetParameter(parameters, sp, nameof(RequestMaximumCpuTimeInSeconds), "request_max_cpu_time_sec={0}", ref count);
            var tempCount = count;
            if (IsSupportedProperty(nameof(RequestMaximumMemoryGrantPercentageAsDouble),sp))
            {
                GetParameter(parameters, sp, nameof(RequestMaximumMemoryGrantPercentageAsDouble), "request_max_memory_grant_percent={0}", ref count);
            }
            if (tempCount == count)
            {
                GetParameter(parameters, sp, nameof(RequestMaximumMemoryGrantPercentage), "request_max_memory_grant_percent={0}", ref count);
            }
            GetParameter(parameters, sp, nameof(RequestMemoryGrantTimeoutInSeconds), "request_memory_grant_timeout_sec={0}", ref count);
            GetParameter(parameters, sp, nameof(MaximumDegreeOfParallelism), "max_dop={0}", ref count);

            if (0 < count)
            {
                // Ex:
                // WITH(GroupMaximumRequests=1, MaximumDegreeOfParallelism=3)
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    " WITH({0})",
                    parameters.ToString());
            }
        }

        /// <summary>
        /// Helper method to move Workload group to specified resource pool
        /// </summary>
        /// <param name="poolName"></param>
        /// <returns></returns>
        private void MoveToPoolImpl(string poolName)
        {
            try
            {
                string workloadGroupName = this.Name;

                if (null == poolName)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("poolName"));
                }

                if (null == m_comparer)
                {
                    this.InitComparer();
                }

                // check if this pool is not the internal pool
                if (m_comparer.Compare(poolName, InternalPoolName) == 0)
                {
                    string exceptionMessage = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        ExceptionTemplates.CannotMoveToInternalResourcePool, workloadGroupName);

                    throw new SmoException(ExceptionTemplates.InnerException,
                        new ArgumentException(exceptionMessage, "poolName"));
                }

                // Set the Parent Resource Pool as the target pool
                ResourcePool rpTargetPool = (ResourcePool)this.Parent.Parent.ResourcePools[poolName];

                // Check if the pool exists 
                if (null == rpTargetPool)
                {
                    string exceptionMessage = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        ExceptionTemplates.ResourcePoolNotExist, poolName);

                    throw new SmoException(ExceptionTemplates.InnerException,
                        new ArgumentException(exceptionMessage, "poolName"));
                }

                // Check if we are trying to move group to pool that is already in 
                if (0 == m_comparer.Compare(poolName, this.Parent.Name))
                {
                    string exceptionMessage = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        ExceptionTemplates.CannotMoveToSamePool, poolName);

                    throw new SmoException(ExceptionTemplates.InnerException,
                        new ArgumentException(exceptionMessage, "poolName"));
                }

                StringCollection query = this.ScriptMoveToPool(poolName);

                this.ExecutionManager.ExecuteNonQuery(query);

                // Set Parent for this group as the targetPool
                this.SetState(SqlSmoState.Pending);
                this.Parent = rpTargetPool;
                this.SetState(SqlSmoState.Existing);

            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.MoveToPool, this, e);
            }
        }
        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the urn suffix in the urn expression
        /// </summary>
        /// <value>The urn suffix.</value>
        public static string UrnSuffix
        {
            get
            {
                return "WorkloadGroup";
            }
        }

        /// <summary>
        /// This function is introduced for the WorkloadGroup special case for DMF, see PolicyEvaluationHelper.GetTargetQueryExpression().
        /// It is introduced as a workaround to VSTS 222405.
        /// The code has special knowledge of the WorkloadGroup object.  WorkloadGroups are uniquely named across all ResourcePools.  
        /// Thus, the code takes advanatage of this knowledge to retrive the WorkloadGroup
        /// by issuing the query "Server[@Name=serverName]/ResourceGovernor/ResourcePool/WorkloadGroup[@Name=workloadgroup]"
        /// This will return one single object, and using that object we can get the unique Urn that contains the name
        /// of the ResourcePool.  With the Urn, we are able to retrive the Powershell path.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        internal static WorkloadGroup GetWorkloadGroup(Server server, string groupName)
        {
            String path = string.Format(SmoApplication.DefaultCulture, server.Urn + "/ResourceGovernor/ResourcePool/WorkloadGroup[@Name='{0}']", Urn.EscapeString(groupName));
            SfcObjectQuery targetSetQuery = new SfcObjectQuery(server);
            IEnumerable targetObjects = targetSetQuery.ExecuteIterator(new SfcQueryExpression(path), null, null);
            WorkloadGroup workloadGroup = null;
            foreach (object targetObject in targetObjects)
            {
                workloadGroup = targetObject as WorkloadGroup;
                break; // there should only be one object, and we will break here just in case
            }

            if (workloadGroup == null)
            {
                throw new SmoException(ExceptionTemplates.CouldNotFindManagementObject("WorkloadGroup", groupName));
            }
            return workloadGroup;
        }

        #endregion

    }
}

