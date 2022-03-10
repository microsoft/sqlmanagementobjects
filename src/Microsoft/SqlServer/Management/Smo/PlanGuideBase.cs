// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a sql server database plan
    ///</summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class PlanGuide : NamedSmoObject, Cmn.ICreatable, Cmn.IAlterable,
        Cmn.IDroppable, Cmn.IDropIfExists, IScriptable, IExtendedProperties
    {
        internal PlanGuide(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "PlanGuide";
            }
        }

        internal override void ValidateName(string name)
        {
            base.ValidateName(name);
            if (0 == name.Length)
            {
                throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(name));
            }
            CheckPlanGuideName(name);
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion100();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major > 9 ? ExtendedProperties : null, true, ExtendedProperty.UrnSuffix) };
        }

        /// <summary>
        /// Alter plan.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            Property p = Properties.Get("IsDisabled");
            if (p.Dirty && p.Value != null)
            {
                bool isDisabled = (bool)p.Value;

                alterQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SP_CONTROLPLANGUIDE_NAME,
                    isDisabled ? MakeSqlString("DISABLE") : MakeSqlString("ENABLE"), MakeSqlString(MakeSqlBraket(this.Name))));
            }
        }

        /// <summary>
        /// Create Plan.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder statement = new StringBuilder();

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                statement.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, this.Name, DateTime.Now.ToString(GetDbCulture())));
                statement.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // perform check for existing object
                statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_PLANGUIDE, "NOT", MakeSqlString(this.Name));
                statement.Append(sp.NewLine);
                statement.Append(Scripts.BEGIN);
                statement.Append(sp.NewLine);
            }

            statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.SP_CREATEPLANGUIDE,
                       MakeSqlString(MakeSqlBraket(this.Name)));

            string pStatement = (string)this.Properties.GetValueWithNullReplacement("Statement");
            if (pStatement != null)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @stmt = {1}",
                       Globals.comma, MakeSqlString(pStatement));
            }
            else
            {
                throw new PropertyNotSetException("Statement");
            }

            object pType = this.Properties.GetValueWithNullReplacement("ScopeType");
            PlanGuideType pgType;
            string type = string.Empty;
            if (pType != null)
            {
                type = pType.ToString().ToUpperInvariant();
                pgType = (PlanGuideType)pType;
                if (!Enum.IsDefined(typeof(PlanGuideType), pgType))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("PlanGuideType"));
                }
                statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @type = {1}",
                       Globals.comma, MakeSqlString(type));
            }
            else
            {
                throw new PropertyNotSetException("ScopeType");
            }

            string scopeObjectName = string.Empty;
            string scopeSchemaName = string.Empty;
            string scopeBatch = string.Empty;
            bool isScopeBatchDirty = false;
            bool isScriptForYukon = sp.TargetServerVersionInternal == SqlServerVersionInternal.Version90;

            if (this.Properties.Get("ScopeObjectName").Value != null)
            {
                scopeObjectName = (string)Properties["ScopeObjectName"].Value;
            }
            if (this.Properties.Get("ScopeSchemaName").Value != null)
            {
                scopeSchemaName = (string)Properties["ScopeSchemaName"].Value;
            }
            if (this.Properties.Get("ScopeBatch").Value != null)
            {
                scopeBatch = (string)Properties["ScopeBatch"].Value;
                isScopeBatchDirty = Properties["ScopeBatch"].Dirty;
            }

            switch (pgType)
            {
                case PlanGuideType.Object:
                    if (scopeBatch.Length > 0)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.PropertyNotValidException("ScopeBatch", "ScopeType", type));
                    }

                    if (scopeObjectName.Length > 0)
                    {
                        if (scopeSchemaName.Length > 0)
                        {
                            statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @module_or_batch = N'{1}{2}{3}'",
                       Globals.comma, SqlString(MakeSqlBraket(scopeSchemaName)), Globals.Dot, SqlString(MakeSqlBraket(scopeObjectName)));
                        }
                        else
                        {
                            statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @module_or_batch = {1}",
                       Globals.comma, MakeSqlString(MakeSqlBraket(scopeObjectName)));
                        }
                    }
                    else
                    {
                        throw new PropertyNotSetException("ScopeObjectName");
                    }
                    break;
                case PlanGuideType.Sql:
                    if (scopeObjectName.Length > 0 || scopeSchemaName.Length > 0)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.PropertiesNotValidException("ScopeObjectName and ScopeSchemaName", "ScopeType", type));
                    }
                    if (scopeBatch.Length > 0)
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @module_or_batch = {1}",
                       Globals.comma, MakeSqlString(scopeBatch));
                    }
                    else if (isScriptForYukon)
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @module_or_batch = NULL", Globals.comma);
                    }
                    break;
                case PlanGuideType.Template:
                    //Template - Scope Batch has to be null when user sets it but engine saves the value after creation hence during scripting we ignore it.
                    if ((isScopeBatchDirty && scopeBatch.Length > 0) || scopeObjectName.Length > 0 || scopeSchemaName.Length > 0)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.PropertiesNotValidException("ScopeBatch, ScopeObjectName and ScopeSchemaName", "ScopeType", type));
                    }
                    else if (isScriptForYukon)
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @module_or_batch = NULL", Globals.comma);
                    }
                    break;
            }

            string parameters = (string)this.Properties.Get("Parameters").Value as string;
            if (null != parameters && 0 < parameters.Length)
            {
                if (pgType == PlanGuideType.Object)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.PropertyNotValidException("Parameters", "ScopeType", type));
                }
                else
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @params = {1}",
                           Globals.comma, MakeSqlString(parameters));
                }
            }
            else if (pgType == PlanGuideType.Template)
            {
                throw new PropertyNotSetException("Parameters");
            }
            else if (isScriptForYukon)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @params = NULL", Globals.comma);
            }

            string hints = (string)this.Properties.Get("Hints").Value as string;
            if (null != hints && 0 < hints.Length)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @hints = {1}",
                       Globals.comma, MakeSqlString(hints));
            }
            else if (isScriptForYukon)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "{0} @hints = NULL", Globals.comma);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.Append(sp.NewLine);
                statement.Append(Scripts.END);
                statement.Append(sp.NewLine);
            }

            createQuery.Add(statement.ToString());

            //If plan is disabled script it also since at time of creation plan is enabled.
            object pDisabled = this.Properties.Get("IsDisabled").Value as object;
            if (pDisabled != null && (bool)pDisabled)
            {
                createQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SP_CONTROLPLANGUIDE_NAME,
                    MakeSqlString("DISABLE"), MakeSqlString(MakeSqlBraket(this.Name))));
            }
        }

        ///<summary>
        /// Drop plan.
        ///</summary>
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

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder statement = new StringBuilder();

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                statement.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, this.Name, DateTime.Now.ToString(GetDbCulture())));
                statement.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // perform check for existing object
                statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_PLANGUIDE, "", MakeSqlString(this.Name));
                statement.Append(sp.NewLine);
                statement.Append(Scripts.BEGIN);
                statement.Append(sp.NewLine);
            }

            statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.SP_CONTROLPLANGUIDE_NAME,
                                    MakeSqlString("DROP"), MakeSqlString(MakeSqlBraket(this.Name)));

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.Append(sp.NewLine);
                statement.Append(Scripts.END);
                statement.Append(sp.NewLine);
            }
            dropQuery.Add(statement.ToString());
        }

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

        private void CheckPlanGuideName(string planGuideName)
        {
            if (planGuideName.StartsWith("#", StringComparison.Ordinal))
            {
                // plan name cannot start with the number sign (#).
                throw new WrongPropertyValueException(ExceptionTemplates.PlanGuideNameCannotStartWithHash(planGuideName));
            }
        }

        /// <summary>
        /// Validate Plan Guide
        /// </summary>
        /// <returns></returns>
        public bool ValidatePlanGuide()
        {
            DataRow dr;
            return this.ValidatePlanGuide(out dr);
        }

        /// <summary>
        /// Validate Plan Guide
        /// </summary>
        /// <param name="dr">out parameter</param>
        /// <returns></returns>
        public bool ValidatePlanGuide(out DataRow errorInfo)
        {
            try
            {
                CheckObjectStateImpl(true);
                ThrowIfBelowVersion100();
                StringCollection queries = new StringCollection();
                AddDatabaseContext(queries, new ScriptingPreferences(this));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "select * from sys.fn_validate_plan_guide({0})", this.ID));
                DataTable dt = this.ExecutionManager.ExecuteWithResults(queries).Tables[0];
                errorInfo = null;
                bool result = true;
                if (dt.Rows.Count > 0)
                {
                    errorInfo = dt.Rows[0];
                    result = false;
                }
                return result;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.PlanGuide, this, e);
            }
        }
    }
}

