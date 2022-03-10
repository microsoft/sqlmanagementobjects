// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class that handles creating, altering, dropping, and scripting the Workload Management Workload Classifier Object
    /// </summary>
    public partial class WorkloadManagementWorkloadClassifier : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, IScriptable
    {
        /// <summary>
        /// Constructs Workload Classifier object.
        /// </summary>
        /// <param name="parentColl">Parent collection.</param>
        /// <param name="key">Object key.</param>
        /// <param name="state">Object state.</param>
        internal WorkloadManagementWorkloadClassifier(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

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
        /// Drop this instance.
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
        /// Generates Queries for creating workload management workload classiifers
        /// </summary>
        /// <param name="queries">Queries string collection that has T-SQL queries</param>
        /// <param name="sp">Scripting Preferences</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfTargetNotDw(sp.TargetEngineIsAzureSqlDw());

            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                createQuery.Append(WorkloadManagementWorkloadClassifier.IncludeExistsWorkloadClassifier(FormatFullNameForScripting(sp, false)));
                createQuery.Append(sp.NewLine);
                createQuery.Append(Scripts.BEGIN);
                createQuery.Append(sp.NewLine);
            }

            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE WORKLOAD CLASSIFIER {0}",
                FormatFullNameForScripting(sp));

            if (sp.IncludeScripts.ExistenceCheck)
            {
                createQuery.Append(sp.NewLine);
                createQuery.Append(Scripts.END);
                createQuery.Append(sp.NewLine);
            }

            int count = 0;
            GetAllParams(createQuery, sp, ref count);

            queries.Add(createQuery.ToString());
        }


        /// <summary>
        /// Generates Queries for dropping workload management workload classiifers
        /// </summary>
        /// <param name="queries">Queries string collection that has T-SQL queries</param>
        /// <param name="sp">Scripting Preferences</param>
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfTargetNotDw(sp.TargetEngineIsAzureSqlDw());
            
            StringBuilder dropQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                dropQuery.Append(WorkloadManagementWorkloadClassifier.IncludeExistsWorkloadClassifier(FormatFullNameForScripting(sp, false)));
                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.BEGIN);
                dropQuery.Append(sp.NewLine);
            }

            dropQuery.AppendFormat(SmoApplication.DefaultCulture,
            "DROP WORKLOAD CLASSIFIER {0}",
            FormatFullNameForScripting(sp));

            if (sp.IncludeScripts.ExistenceCheck)
            {
                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.END);
                dropQuery.Append(sp.NewLine);
            }

            queries.Add(dropQuery.ToString());
        }

        /// <summary>
        /// Gets the urn suffix in the urn expression
        /// </summary>
        /// <value>The urn suffix.</value>
        public static string UrnSuffix => "WorkloadManagementWorkloadClassifier";


        public static string IncludeExistsWorkloadClassifier(string name)
        {
            return $"IF EXISTS ( SELECT name FROM sys.workload_management_workload_classifiers WHERE name = {name})";
        }

        /// <summary>
        /// Sets a valid start time and end time for the workload classifier based on starting hours and minutes and the duration.
        /// </summary>
        /// <param name="hours">start time hours</param>
        /// <param name="minutes">start time minutes</param>
        /// <param name="duration">duration of window</param>
        public void SetActivityWindow(int hours, int minutes, TimeSpan duration)
        {
            string startHours = hours.ToString().PadLeft(2, '0');
            string startMinutes = minutes.ToString().PadLeft(2, '0');
            this.StartTime = startHours + ':' + startMinutes;

            duration += TimeSpan.FromHours(hours);
            duration += TimeSpan.FromMinutes(minutes);
            string endTime = duration.ToString(@"hh\:mm");
            this.EndTime = endTime;
        }

        internal void ThrowIfTargetNotDw(bool targetEngineIsAzureSqlDw)
        {
            if (!targetEngineIsAzureSqlDw)
            {
                throw new FailedOperationException(ExceptionTemplates.UnsupportedVersionException, this, new UnsupportedEngineEditionException());
            }
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

            buffer.AppendFormat(SmoApplication.DefaultCulture, "IMPORTANCE={0}", typeConverter.ConvertToInvariantString(prop.Value));
        }

        /// <summary>
        /// Retrieve the properties that were set and generate appropriate T-SQL fragments
        /// </summary>
        /// <param name="sb">T-SQL string fragment</param>
        /// <param name="so">Scripting Options</param>
        /// <param name="count">The count.</param>
        private void GetAllParams(StringBuilder sb, ScriptingPreferences so, ref int count)
        {
            StringBuilder parameters = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            GetParameter(parameters, so, "GroupName", "WORKLOAD_GROUP='{0}'", ref count);
            GetParameter(parameters, so, "MemberName", "MEMBERNAME='{0}'", ref count);
            GetParameter(parameters, so, "WlmLabel", "WLM_LABEL='{0}'", ref count);
            GetParameter(parameters, so, "WlmContext", "WLM_CONTEXT='{0}'", ref count);
            GetParameter(parameters, so, "StartTime", "START_TIME='{0}'", ref count);
            GetParameter(parameters, so, "EndTime", "END_TIME='{0}'", ref count);
            GetImportanceTSql(parameters, ref count);

            if (0 < count)
            {
                // Ex:
                // WITH(GroupMaximumRequests=1, MaximumDegreeOfParallelism=3)
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    " WITH({0})",
                    parameters.ToString());
            }
        }
    }
}
