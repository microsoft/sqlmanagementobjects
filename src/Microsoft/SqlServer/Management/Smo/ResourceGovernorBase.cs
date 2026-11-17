// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class that handles Altering and Scripting the current state of Resource Governor
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class ResourceGovernor : SqlSmoObject, Cmn.IAlterable, IScriptable
    {
        #region Constructors

        public ResourceGovernor()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceGovernor"/> class.
        /// </summary>
        /// <param name="parentsrv">SMO Server instance.</param>
        /// <param name="key">The key.</param>
        /// <param name="state">The state.</param>
        internal ResourceGovernor(Server parentsrv, ObjectKeyBase key, SqlSmoState state)
            :
            base(key, state)
        {
            singletonParent = parentsrv;
            SetServerObject(parentsrv.GetServerObject());

            m_comparer = parentsrv.StringComparer;
        }

        #endregion

        #region Properties and their Public Accessors

        /// <summary>
        /// Gets the parent Object. In this case it is Server
        /// </summary>
        /// <value>The parent.</value>
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Server;
            }
            internal set
            {
                SetParentImpl(value);
            }
        }

        ResourcePoolCollection m_ResourcePools = null;
        /// <summary>
        /// Gets the resource pool Collection
        /// </summary>
        /// <value>The resource pools.</value>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ResourcePool))]
        public ResourcePoolCollection ResourcePools
        {
            get
            {
                CheckObjectState();
                if (this.m_ResourcePools == null)
                {
                    this.m_ResourcePools = new ResourcePoolCollection(this);
                }

                return this.m_ResourcePools;
            }
        }

        ExternalResourcePoolCollection m_ExternalResourcePools = null;
        /// <summary>
        /// Gets the external resource pool Collection
        /// </summary>
        /// <value>The external resource pools.</value>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalResourcePool))]
        public ExternalResourcePoolCollection ExternalResourcePools
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(ExternalResourcePool));
                if (this.m_ExternalResourcePools == null)
                {
                    this.m_ExternalResourcePools = new ExternalResourcePoolCollection(this);
                }

                return this.m_ExternalResourcePools;
            }
        }

        // old Enabled value
        internal object oldEnabledValue = null;

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "Enabled" && !prop.Dirty)
            {
                oldEnabledValue = prop.Value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Alters this instance.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Scripts this instance.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting optiions
        /// </summary>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }



        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the Full Urn by traversing parent hierarchy
        /// </summary>
        /// <param name="urnbuilder">The urnbuilder.</param>
        /// <param name="idOption">The id option.</param>
        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        /// <summary>
        /// Generates Queries for Create operation
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="sp"></param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        /// <summary>
        /// Generates Queries for Alter operation
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="sp"></param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptProperties(queries, sp);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates corresponding T-SQL based on the set properties
        /// </summary>
        /// <param name="queries">Queries string collection</param>
        /// <param name="sp"></param>
        private void ScriptProperties(StringCollection queries, ScriptingPreferences sp)
        {
            bool reconfigureResourceGovernor = false;

            StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            Property propClassifierFunction = Properties.Get("ClassifierFunction");

            if (propClassifierFunction.Dirty || !sp.ScriptForAlter)
            {
                string propValue = @"NULL";

                if (null != propClassifierFunction.Value && !String.IsNullOrEmpty(propClassifierFunction.Value.ToString()))
                {
                    propValue = SqlString(propClassifierFunction.Value.ToString());
                }

                // Set Classifier Function
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                    "ALTER RESOURCE GOVERNOR WITH (CLASSIFIER_FUNCTION = {0});",
                    propValue));

                // Updates to classifier function requires reconfigure
                reconfigureResourceGovernor = true;
            }

            // MaxOutstandingIOPerVolume property is only supported after SQL14.
            if (IsSupportedProperty("MaxOutstandingIOPerVolume", sp))
            {
                Property propMaxOutstandingIo = Properties.Get("MaxOutstandingIOPerVolume");

                // If the property has changed or this is a create sript, we need to script it.
                if (propMaxOutstandingIo != null && (propMaxOutstandingIo.Dirty || !sp.ScriptForAlter))
                {
                    string propValue = @"DEFAULT";

                    if (propMaxOutstandingIo.Value != null && (int)propMaxOutstandingIo.Value != 0)
                    {
                        propValue = propMaxOutstandingIo.Value.ToString();
                    }

                    // Set Max Outstanding IO Per Volume
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                        "ALTER RESOURCE GOVERNOR WITH (MAX_OUTSTANDING_IO_PER_VOLUME = {0});",
                        propValue));

                    // Updates to max outstanding IO requires reconfigure
                    reconfigureResourceGovernor = true;
                }
            }

            // Check if Reconfigure Pending property is set on Resource Governor
            // If set, reconfigure Resource Governor
            Property reconfigurePending = Properties.Get("ReconfigurePending");
            if ((null != reconfigurePending) && (null != reconfigurePending.Value))
            {
                if ((bool)reconfigurePending.Value)
                {
                    reconfigureResourceGovernor = true;
                }
            }

            Property propEnabled = Properties.Get("Enabled");

            bool resourceGovernorEnabled;
            bool appendDisableQuery = false;

            if (null != propEnabled.Value)
            {
                // store the current value of "Enabled" property
                resourceGovernorEnabled = (bool)propEnabled.Value;

                // Compare with previous copy of Enabled property to check if it was changed
                if (this.HasEnabledPropertyChanged(resourceGovernorEnabled) || !sp.ScriptForAlter)
                {
                    if (resourceGovernorEnabled)
                    {
                        reconfigureResourceGovernor = true;
                    }
                    else
                    {
                        appendDisableQuery = true;
                    }
                }
            }

            // If reconfigureResourceGovernor flag is set, Append DDL to reconfigure Resource Governor
            if (reconfigureResourceGovernor)
            {
                // Resource Governor changes requires reconfiguring resource governor
                // ALTER RESOURCE GOVERNOR RECONFIGURE;
                queries.Add(Scripts.RESOURCE_GOVERNOR_RECONFIGURE);
            }

            // If RG needs to be disabled, it should be the last command
            if (appendDisableQuery)
            {
                queries.Add("ALTER RESOURCE GOVERNOR DISABLE;");
            }
        }

        #endregion

        /// <summary>
        /// Checks if Enable property was changed
        /// </summary>
        /// <param name="newValue">new value</param>
        private bool HasEnabledPropertyChanged(bool newValue)
        {
            bool propertyChanged = true;

            if (null != this.oldEnabledValue)
            {
                if (newValue == (bool)this.oldEnabledValue)
                {
                    propertyChanged = false;
                }
                else
                {
                    propertyChanged = true;
                }
            }

            return propertyChanged;
        }

        #region Properties

        /// <summary>
        /// Gets the urn suffix in the urn expression
        /// </summary>
        /// <value>The urn suffix.</value>
        public static string UrnSuffix
        {
            get
            {
                return "ResourceGovernor";
            }
        }

        #endregion
    }
}

