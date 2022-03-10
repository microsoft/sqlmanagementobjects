// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class that handles Creating, Altering, Dropping and Scripting the Resource Pool instance
    /// </summary>
    [Facets.StateChangeEvent("CREATE_RESOURCE_POOL", "RESOURCEPOOL", "RESOURCE POOL")]
    [Facets.StateChangeEvent("ALTER_RESOURCE_POOL", "RESOURCEPOOL", "RESOURCE POOL")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class ResourcePool : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable, IScriptable
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePool"/> class.
        /// </summary>
        /// <param name="parentColl">Parent Collection.</param>
        /// <param name="key">The key.</param>
        /// <param name="state">The state.</param>
        internal ResourcePool(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
            m_comparer = ((ResourceGovernor)(parentColl.ParentInstance)).Parent.StringComparer;
        }

        #endregion

        #region Properties and their Public Accessors

        WorkloadGroupCollection m_WorkloadGroups = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(WorkloadGroup))]
        public WorkloadGroupCollection WorkloadGroups
        {
            get
            {
                CheckObjectState();
                if (this.m_WorkloadGroups == null)
                {
                    this.m_WorkloadGroups = new WorkloadGroupCollection(this);
                }

                return this.m_WorkloadGroups;
            }
        }

        /// <summary>
        /// Name of ResourcePool
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

        ResourcePoolAffinityInfo affinityInfo = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public ResourcePoolAffinityInfo ResourcePoolAffinityInfo
        {
            get
            {
                if (null == affinityInfo)
                {
                    affinityInfo = new ResourcePoolAffinityInfo(this);
                }
                return affinityInfo;
            }
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
        /// Alters this instance.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
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

        #endregion

        #region Protected Methods

        /// <summary>
        /// Generates Queries for Creating Resource Pool
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, Include following check during Create
                // IF NOT EXISTS ( SELECT name FROM sys.resource_governor_resource_pools WHERE name = 'name')
                // BEGIN
                // ..
                // END
                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_RG_RESOUREPOOL,
                    "NOT",
                    FormatFullNameForScripting(sp, false));

                createQuery.Append(sp.NewLine);
                createQuery.Append(Scripts.BEGIN);
                createQuery.Append(sp.NewLine);
            }

            // DDL to create a Resource Pool
            // Ex: CREATE RESOURCE POOL [foo]
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE RESOURCE POOL {0}",
                FormatFullNameForScripting(sp));

            int count = 0;
            GetAllParams(createQuery, sp, ref count);
            createQuery.Append(sp.NewLine);

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
        /// Scripts the alter operation.
        /// </summary>
        /// <param name="queries">T-SQL string fragment</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(alterQuery, sp, UrnSuffix);

            // DDL to alter a Resource Pool
            // Ex: ALTER RESOURCE POOL [foo]
            alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER RESOURCE POOL {0}",
                FormatFullNameForScripting(sp));

            int count = 0;
            GetAllParams(alterQuery, sp, ref count);
            alterQuery.Append(sp.NewLine);


            // We need to issue Alter only if there was atleast one property change
            if (0 < count)
            {
                queries.Add(alterQuery.ToString());
            }
        }

        /// <summary>
        /// Scripts the drop operation.
        /// </summary>
        /// <param name="queries">T-SQL string fragment</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder dropQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, Include following check during Drop
                // IF EXISTS ( SELECT name FROM sys.resource_governor_resource_pools WHERE name = 'name')
                // BEGIN
                // ..
                // END
                dropQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_RG_RESOUREPOOL,
                    String.Empty,
                    FormatFullNameForScripting(sp, false));

                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.BEGIN);
                dropQuery.Append(sp.NewLine);
            }

            // DDL to Drop a Resource Pool
            // Ex: DROP RESOURCE POOL [foo]
            dropQuery.AppendFormat(SmoApplication.DefaultCulture,
                "DROP RESOURCE POOL {0}",
                FormatFullNameForScripting(sp));

            dropQuery.Append(sp.NewLine);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, 
                // match the BEGIN clause with END
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
            this.MinimumCpuPercentage = this.MinimumCpuPercentage;
            this.MaximumCpuPercentage = this.MaximumCpuPercentage;
            this.MinimumMemoryPercentage = this.MinimumMemoryPercentage;
            this.MaximumMemoryPercentage = this.MaximumMemoryPercentage;
            this.CapCpuPercentage = this.CapCpuPercentage;
            this.MinimumIopsPerVolume = this.MinimumIopsPerVolume;
            this.MaximumIopsPerVolume = this.MaximumIopsPerVolume;
        }

        /// <summary>
        /// Marks the children(Workloadgroup collection) as dropped.
        /// </summary>
        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if (null != this.m_WorkloadGroups)
            {
                this.m_WorkloadGroups.MarkAllDropped();
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            if (this.affinityInfo != null)
            {
                this.affinityInfo.Refresh();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Retrieve the properties that were set and generate appropriate T-SQL fragments
        /// </summary>
        /// <param name="sb">T-SQL string fragment</param>
        /// <param name="so">Scripting Options</param>
        /// <param name="count">The count.</param>
        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, ref int count)
        {
            StringBuilder parameters = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            GetParameter(parameters, sp, "MinimumCpuPercentage", "min_cpu_percent={0}", ref count);
            GetParameter(parameters, sp, "MaximumCpuPercentage", "max_cpu_percent={0}", ref count);
            GetParameter(parameters, sp, "MinimumMemoryPercentage", "min_memory_percent={0}", ref count);
            GetParameter(parameters, sp, "MaximumMemoryPercentage", "max_memory_percent={0}", ref count);

            //Starting Denali, we added CPU CAPS and scheduler affinity
            if (IsSupportedProperty("CapCpuPercentage"))
            {
                GetParameter(parameters, sp, "CapCpuPercentage", "cap_cpu_percent={0}", ref count);

                //get the affinity info script
                StringCollection sc = this.ResourcePoolAffinityInfo.DoAlterInternal(sp);
                if (sc != null && sc.Count > 0)
                {
                    //we have affinity information to script
                    if (count++ > 0)
                    {
                        //append a comma and a new line
                        parameters.Append(Globals.commaspace);
                        parameters.Append(Globals.newline);
                        parameters.Append(Globals.tab);
                        parameters.Append(Globals.tab);
                    }

                    foreach (string s in sc)
                    {
                        parameters.AppendLine(s);
                    }
                }
            }

            // Starting SQL14, we added Min/Max IOPS per volume for IO RG.
            if (IsSupportedProperty("MinimumIopsPerVolume") &&
                IsSupportedProperty("MaximumIopsPerVolume"))
            {
                GetParameter(parameters, sp, "MinimumIopsPerVolume", "min_iops_per_volume={0}", ref count);
                GetParameter(parameters, sp, "MaximumIopsPerVolume", "max_iops_per_volume={0}", ref count);
            }

            if (count > 0)
            {
                // Ex:
                // WITH(MinimumCpuPercentage=10, MaximumMemoryPercentage=30)
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    " WITH({0})",
                    parameters.ToString());
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
                return "ResourcePool";
            }
        }

        #endregion
    }
}




