// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class that handles Creating, Altering, Dropping and Scripting the WorkloadManagement Workload group instance
    /// </summary>
    public partial class WorkloadManagementWorkloadGroup : ScriptNameObjectBase, Cmn.ICreatable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IAlterable, IScriptable
    {

        #region Constructors

        internal WorkloadManagementWorkloadGroup(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
            {

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

        public void Alter()
        {
            base.AlterImpl();
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

        internal void ThrowIfTargetNotDw(bool targetEngineIsAzureSqlDw) {
            if (!targetEngineIsAzureSqlDw)
            {
                throw new FailedOperationException(ExceptionTemplates.UnsupportedVersionException, this, new UnsupportedEngineEditionException());
            }
        }

        /// <summary>
        /// Generates Queries for Creating WorkloadManagement Workload group
        /// </summary>
        /// <param name="queries">Queries string collection that has T-SQL queries</param>
        /// <param name="sp">Scripting Options</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfTargetNotDw(sp.TargetEngineIsAzureSqlDw());

            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header)
            {
                ScriptIncludeHeaders(createQuery, sp, UrnSuffix);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // Include following check
                // IF NOT EXISTS ( SELECT name FROM sys.workload_management_workload_groups WHERE name = N'name')
                // BEGIN
                // ..
                // END
                createQuery.Append(IncludeExistsWorkloadManagementWorkloadGroup(false, FormatFullNameForScripting(sp, false)));
                createQuery.Append(sp.NewLine);
                createQuery.Append(Scripts.BEGIN);
                createQuery.Append(sp.NewLine);
            }

            // DDL to create a workload Group
            // Ex: CREATE WORKLOAD GROUP foo
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
            "CREATE WORKLOAD GROUP {0}",
            FormatFullNameForScripting(sp));
            createQuery.Append(sp.NewLine);

            int count = 0;
            GetAllParams(createQuery, sp, ref count);

            if (sp.IncludeScripts.ExistenceCheck)
            {
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
        /// <param name="sp">Scripting Options</param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfTargetNotDw(sp.TargetEngineIsAzureSqlDw());

            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(alterQuery, sp, UrnSuffix);

            alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER WORKLOAD GROUP {0}",
                FormatFullNameForScripting(sp));

            int count = 0;
            GetAllParams(alterQuery, sp, ref count);

            queries.Add(alterQuery.ToString());
        }

        /// <summary>
        /// Generates Queries for Dropping Workload group
        /// </summary>
        /// <param name="queries">Queries string collection that has T-SQL queries</param>
        /// <param name="sp">Scripting Options</param>
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfTargetNotDw(sp.TargetEngineIsAzureSqlDw());

            StringBuilder dropQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // Include following check
                // IF EXISTS ( SELECT name FROM sys.workload_management_workload_groups WHERE name = N'name')
                // BEGIN
                // ..
                // END
                dropQuery.Append(IncludeExistsWorkloadManagementWorkloadGroup(true, FormatFullNameForScripting(sp, false)));
                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.BEGIN);
                dropQuery.Append(sp.NewLine);
            }

            dropQuery.AppendFormat(SmoApplication.DefaultCulture,
            "DROP WORKLOAD GROUP {0}",
            FormatFullNameForScripting(sp));

            if (sp.IncludeScripts.ExistenceCheck)
            {
                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.END);
                dropQuery.Append(sp.NewLine);
            }

            queries.Add(dropQuery.ToString());
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Retrieve the properties that were set and generate appropriate T-SQL fragments
        /// </summary>
        /// <param name="sb">T-SQL string fragment</param>
        /// <param name="sp">Scripting Options</param>
        /// <param name="count">Count of parameters read</param>
        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, ref int count)
        {
            StringBuilder parameters = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            GetParameter(parameters, sp, "MinPercentageResource", "min_percentage_resource={0}", ref count);
            GetParameter(parameters, sp, "CapPercentageResource", "cap_percentage_resource={0}", ref count);
            GetParameter(parameters, sp, "RequestMinResourceGrantPercent", "request_min_resource_grant_percent={0}", ref count);
            GetParameter(parameters, sp, "RequestMaxResourceGrantPercent", "request_max_resource_grant_percent={0}", ref count);
            GetImportanceTSql(parameters, ref count);
            GetParameter(parameters, sp, "QueryExecutionTimeoutSec", "query_execution_timeout_sec={0}", ref count);

            sb.AppendFormat(SmoApplication.DefaultCulture,
                   "WITH({0})",
                   parameters.ToString());
        }

        /// <summary>
        /// Get TSQL value for Importance
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count">Count of parameters read</param>
        private void GetImportanceTSql(StringBuilder buffer, ref int count)
        {
            Property prop = this.GetPropertyOptional("Importance");
            if (null == prop.Value)
            {
                return;
            }
            if (count++ > 0)
            {
                buffer.Append(Globals.commaspace);
                buffer.Append(Globals.newline);
                buffer.Append(Globals.tab);
                buffer.Append(Globals.tab);
            }
            TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(WorkloadManagementImportance));

            buffer.AppendFormat(SmoApplication.DefaultCulture, "importance={0}", typeConverter.ConvertToInvariantString(prop.Value));
        }

        /// <summary>
        /// Generate existence predicate T-SQL
        /// </summary>
        /// <param name="fExists">Indicates whether the existence predicate is negated</param>
        /// <param name="workloadGroupName">Name of workloadGroup</param>
        private string IncludeExistsWorkloadManagementWorkloadGroup(bool fExists, string workloadGroupName)
        {
            return $"IF{(fExists ? string.Empty : " NOT")} EXISTS (SELECT name FROM sys.workload_management_workload_groups WHERE name = {workloadGroupName})";
        }

        #endregion

        #region Internal Methods
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
                    "CapPercentageResource",
                    "GroupId",
                    "HasClassifier",
                    "Importance",
                    "IsSystemObject",
                    "MinPercentageResource",
                    "Name",
                    "QueryExecutionTimeoutSec",
                    "RequestMaxResourceGrantPercent",
                    "RequestMinResourceGrantPercent"
                };
            List<string> list = GetSupportedScriptFields(typeof(View.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
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
                return nameof(WorkloadManagementWorkloadGroup);
            }
        }

        #endregion

    }
}
