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
    /// Represents a SQL server external resource pool object.
    /// </summary>
    public partial class ExternalResourcePool : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IAlterable, IScriptable
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalResourcePool"/> class.
        /// </summary>
        /// <param name="parentColl">Parent Collection.</param>
        /// <param name="key">The key.</param>
        /// <param name="state">The state.</param>
        internal ExternalResourcePool(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
            m_comparer = ((ResourceGovernor)(parentColl.ParentInstance)).Parent.StringComparer;
        }

        #endregion

        #region Properties and their Public Accessors

        /// <summary>
        /// Name of the external resource pool
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

        ExternalResourcePoolAffinityInfo affinityInfo = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public ExternalResourcePoolAffinityInfo ExternalResourcePoolAffinityInfo
        {
            get
            {
                if (affinityInfo == null)
                {
                    affinityInfo = new ExternalResourcePoolAffinityInfo(this);
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
        /// Generates queries for creating external resource pool
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(typeof(ExternalResourcePool), sp);

            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, include following check during Create:
                // IF NOT EXISTS ( SELECT name FROM sys.resource_governor_external_resource_pools WHERE name = 'name')
                // BEGIN
                // ..
                // END
                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_RG_EXTERNALRESOUREPOOL,
                    "NOT",
                    FormatFullNameForScripting(sp, false));

                createQuery.Append(sp.NewLine);
                createQuery.Append(Scripts.BEGIN);
                createQuery.Append(sp.NewLine);
            }

            // DDL to create an external resource pool
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE EXTERNAL RESOURCE POOL {0}",
                FormatFullNameForScripting(sp));

            int count = 0;
            GetAllParams(createQuery, sp, ref count);
            createQuery.Append(sp.NewLine);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, match the BEGIN clause with an END clause
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
            this.ThrowIfNotSupported(typeof(ExternalResourcePool), sp);

            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(alterQuery, sp, UrnSuffix);

            // DDL to alter an external resource pool
            alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER EXTERNAL RESOURCE POOL {0}",
                FormatFullNameForScripting(sp));

            int count = 0;
            GetAllParams(alterQuery, sp, ref count);
            alterQuery.Append(sp.NewLine);

            // We need to issue Alter only if there was at least one property change
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
            this.ThrowIfNotSupported(typeof(ExternalResourcePool), sp);

            StringBuilder dropQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, include following check during Drop
                // IF EXISTS ( SELECT name FROM sys.resource_governor_external_resource_pools WHERE name = 'name')
                // BEGIN
                // ..
                // END
                dropQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_RG_EXTERNALRESOUREPOOL,
                    String.Empty,
                    FormatFullNameForScripting(sp, false));

                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.BEGIN);
                dropQuery.Append(sp.NewLine);
            }

            // DDL to drop an external resource pool
            dropQuery.AppendFormat(SmoApplication.DefaultCulture,
                "DROP EXTERNAL RESOURCE POOL {0}",
                FormatFullNameForScripting(sp));

            dropQuery.Append(sp.NewLine);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // If IncludeIfNotExists is set in scripting option, match the BEGIN clause with an END clause
                dropQuery.Append(sp.NewLine);
                dropQuery.Append(Scripts.END);
                dropQuery.Append(sp.NewLine);
            }

            queries.Add(dropQuery.ToString());
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

            GetParameter(parameters, sp, "MaximumCpuPercentage", "max_cpu_percent={0}", ref count);
            GetParameter(parameters, sp, "MaximumMemoryPercentage", "max_memory_percent={0}", ref count);
            GetParameter(parameters, sp, "MaximumProcesses", "max_processes={0}", ref count);

            // get the affinity info script
            StringCollection sc = this.ExternalResourcePoolAffinityInfo.DoAlterInternal(sp);
            if (sc != null && sc.Count > 0)
            {
                // we have affinity information to script
                if (count++ > 0)
                {
                    // append a comma and a new line
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

            if (count > 0)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    " WITH ({0})",
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
                return "ExternalResourcePool";
            }
        }

        #endregion
    }
}




