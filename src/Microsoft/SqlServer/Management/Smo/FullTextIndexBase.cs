// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class FullTextIndex : SqlSmoObject, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        public FullTextIndex()
            : base()
        {

        }

        public FullTextIndex(TableViewBase parent) : base(new SimpleObjectKey(parent.Name), SqlSmoState.Creating)
        {
            singletonParent = parent;
            SetServerObject(parent.GetServerObject());

            m_comparer = parent.StringComparer;
        }

        internal FullTextIndex(TableViewBase parent, ObjectKeyBase key, SqlSmoState state) :
            base(key, state)
        {
            singletonParent = parent;
            SetServerObject(parent.GetServerObject());

            m_comparer = parent.StringComparer;
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        [SfcParent("Table")]
        [SfcParent("View")]
        public TableViewBase Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as TableViewBase;
            }
            set{SetParentImpl(value);}
        }

        internal override void ValidateParent(SqlSmoObject newParent)
        {

            singletonParent = (TableViewBase)newParent;

            m_comparer = newParent.StringComparer;
            SetServerObject(newParent.GetServerObject());

            // Full text indexes are not supported on
            // views in 80. Duplicate logic in TableViewBase.FullTextIndex
            // they should be kept in sync.
            if (newParent is View)
            {
                ThrowIfBelowVersion90();
            }
        }

        internal override void UpdateObjectState()
        {
            if (this.State == SqlSmoState.Pending && null != singletonParent)
            {
                SetState(SqlSmoState.Creating);
            }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "FullTextIndex";
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        FullTextIndexColumnCollection fullTextIndexColumns = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(FullTextIndexColumn))]
        public FullTextIndexColumnCollection IndexedColumns
        {
            get
            {
                CheckObjectState();
                if (null == fullTextIndexColumns)
                {
                    fullTextIndexColumns = new FullTextIndexColumnCollection(this);
                }
                return fullTextIndexColumns;
            }
        }

        // ??? Is this method needed?
        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != fullTextIndexColumns)
            {
                fullTextIndexColumns.MarkAllDropped();
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        public void Create(bool noPopulation)
        {
            try
            {
                this.noPopulation = noPopulation;
                base.CreateImpl();
            }
            finally
            {
                noPopulation = false;
            }
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptCreateFullTextIndex(queries, sp);
            Property isEnabled = this.properties.Get("IsEnabled");

            bool enabledCheck = (isEnabled.Value != null) ? (bool)isEnabled.Value : true;
            if (!enabledCheck)
            {
                ScriptDisable(queries, sp);
            }
        }

        protected override void PostCreate()
        {
            this.Parent.m_bFullTextIndexInitialized = true;
            this.Parent.m_FullTextIndex = this as FullTextIndex;
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal bool noPopulation = false;

        public void Alter(bool noPopulation)
        {
            try
            {
                this.noPopulation = noPopulation;
                base.AlterImpl();
            }
            finally
            {
                noPopulation = false;
            }
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (IsObjectDirty())
            {
                ScriptAlterFullTextIndex(alterQuery, sp);
            }
        }


        private void ScriptCreateFullTextIndex(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, string.Empty, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            TableViewBase parentObj = this.Parent;
            string tableName = parentObj.FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_INDEX,
                                SqlString(tableName), "=");
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_INDEX90,
                                "not", SqlString(tableName));
                }
                sb.Append(sp.NewLine);
            }

            Property property1;

            // Target version >= 9
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                // CREATE FULLTEXT INDEX ON <tablename>
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE FULLTEXT INDEX ON {0}", tableName);

                if (IndexedColumns.Count > 0)
                {
                    sb.Append("(");
                }

                // Columns
                int ccount = 0;
                foreach (FullTextIndexColumn col in IndexedColumns)
                {
                    if (!col.IgnoreForScripting)
                    {
                        if (0 != ccount++)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(sp.NewLine);
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", col.FormatFullNameForScripting(sp));

                        // TYPE COLUMN <typecolname>
                        object propType = col.GetPropValueOptional("TypeColumnName");
                        if (propType != null)
                        {
                            string typeColumn = (string)propType;
                            if (typeColumn.Length > 0)
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, " TYPE COLUMN [{0}]", SqlBraket(typeColumn));
                            }
                        }

                        // LANGUAGE <language_string> is only supported on Shiloh or bigger
                        if (this.ServerVersion.Major >= 8 && //current server bigger than 7 and
                            sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version80) //scripting for server bigger than 7
                        {
                            object propLan = col.GetPropValueOptional("Language");
                            if (propLan != null)
                            {
                                string language = (string)propLan;
                                if (language.Length > 0)
                                {
                                    sb.AppendFormat(SmoApplication.DefaultCulture, @" LANGUAGE '{0}'", SqlBraket(language));
                                }
                            }
                        }


                        // STATISTICAL SEMANTICS is only supported on Denali or higher
                        if (this.ServerVersion.Major >= 11 &&                                       // current server bigger than 11 AND
                            sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version110)  //scripting for server bigger than 11
                        {
                            object propSemantic = col.GetPropValueOptional("StatisticalSemantics");
                            if (propSemantic != null)
                            {
                                int semantics = (int)propSemantic;
                                if (semantics > 0)  // Statistical_Semantics is either ON (1) or absent (0)
                                {
                                    sb.AppendFormat(SmoApplication.DefaultCulture, " STATISTICAL_SEMANTICS");
                                }
                            }
                        }
                    }
                }

                if (IndexedColumns.Count > 0)
                {
                    if(ccount == 0)
                    {
                        //No columns were actually scripted (due to IgnoreForScripting being true)
                        //so instead trim the initial paren
                        sb.Length--;
                    }
                    else
                    {
                        sb.Append(")");
                    }
                }
                sb.Append(sp.NewLine);

                // KEY INDEX <indexname>
                string columnName = this.GetPropValue("UniqueIndexName") as string;
                if (null == columnName || columnName.Length <= 0)
                {
                    throw new PropertyNotSetException("UniqueIndexName");
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, "KEY INDEX [{0}]", SqlBraket(columnName));

                if (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version90)
                {
                    // ON <fulltext_catalog_name>
                    if ((null != (property1 = this.Properties.Get("CatalogName")).Value) && (property1.Value.ToString().Length > 0))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " ON [{0}]", SqlBraket(property1.Value.ToString()));
                    }
                    sb.Append(sp.NewLine);

                    // WITH CHANGE_TRACKING
                    if (null != (property1 = this.Properties.Get("ChangeTracking")).Value)
                    {
                        sb.Append("WITH CHANGE_TRACKING ");
                        ChangeTracking ct = (ChangeTracking)property1.Value;
                        switch (ct)
                        {
                            case ChangeTracking.Automatic: sb.Append("AUTO"); break;
                            case ChangeTracking.Manual: sb.Append("MANUAL"); break;
                            case ChangeTracking.Off:
                                sb.Append("OFF");
                                if (noPopulation)
                                {
                                    sb.Append(", NO POPULATION");
                                }

                                break;
                            default:
                                throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("Change Tracking"));
                        }
                        sb.Append(sp.NewLine);
                    }
                }
                else
                {
                    // ON (<ftcatalog_filegroup_option>)
                    StringBuilder onClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    String strCatalog = this.Properties.Get("CatalogName").Value as String;

                    if (null != strCatalog && strCatalog.Length > 0)
                    {
                        onClause.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(strCatalog));
                    }

                    if (this.ServerVersion.Major >= 10)
                    {
                        String sFilegroup = this.Properties.Get("FilegroupName").Value as String;
                        if (null != sFilegroup && sFilegroup.Length > 0)
                        {
                            if (onClause.Length > 0)
                            {
                                onClause.Append(Globals.commaspace);
                            }
                            onClause.AppendFormat(SmoApplication.DefaultCulture, "FILEGROUP {0}", MakeSqlBraket(sFilegroup));
                        }
                    }

                    if (onClause.Length > 0)
                    {
                        sb.Append("ON (");
                        sb.Append(onClause);
                        sb.Append(")");
                        sb.Append(sp.NewLine);
                    }

                    // WITH(CHANGE_TRACKING = <change_tacking option> STOPLIST = <stoplist_option>)
                    StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    Property propertyChangeTracking = this.Properties.Get("ChangeTracking");
                    if (null != propertyChangeTracking.Value)
                    {
                        withClause.Append("CHANGE_TRACKING = ");
                        ChangeTracking ct = (ChangeTracking)propertyChangeTracking.Value;
                        switch (ct)
                        {
                            case ChangeTracking.Automatic:
                                withClause.Append("AUTO");
                                break;
                            case ChangeTracking.Manual:
                                withClause.Append("MANUAL");
                                break;
                            case ChangeTracking.Off:
                                withClause.Append("OFF");
                                if (noPopulation)
                                {
                                    withClause.Append(", NO POPULATION");
                                }

                                break;
                            default:
                                throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("Change Tracking"));

                        }
                    }

                    if (this.ServerVersion.Major >= 10)
                    {
                        Property propertyStopListOption = this.Properties.Get("StopListOption");
                        String strStopListName = this.Properties.Get("StopListName").Value as String;

                        if (null != propertyStopListOption.Value)
                        {
                            if (withClause.Length > 0)
                            {
                                withClause.Append(Globals.commaspace);
                            }
                            withClause.Append("STOPLIST = ");
                            StopListOption slOption = (StopListOption)propertyStopListOption.Value;
                            switch (slOption)
                            {
                                case StopListOption.Off:
                                    withClause.Append("OFF");
                                    if ((null != strStopListName) && (strStopListName.Length > 0))
                                    {
                                        throw new SmoException(ExceptionTemplates.PropertyNotValidException("StopListName", "StopListOption", "OFF"));
                                    }
                                    break;
                                case StopListOption.System:
                                    withClause.Append("SYSTEM");
                                    if ((null != strStopListName) && (strStopListName.Length > 0))
                                    {
                                        throw new SmoException(ExceptionTemplates.PropertyNotValidException("StopListName", "StopListOption", "SYSTEM"));
                                    }
                                    break;
                                case StopListOption.Name:
                                    if ((null != strStopListName) && (strStopListName.Length > 0))
                                    {
                                        withClause.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(strStopListName));
                                    }
                                    else
                                    {
                                        throw new PropertyNotSetException("StopListName");
                                    }
                                    break;
                                default:
                                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("StopList Name"));
                            }
                        }
                        else if ((null != strStopListName) && (strStopListName.Length > 0))
                        {
                            if (withClause.Length > 0)
                            {
                                withClause.Append(Globals.commaspace);
                            }
                            withClause.Append("STOPLIST = ");
                            withClause.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(strStopListName));
                        }
                    }

                    // including SEARCH PROPERTY LIST [ = ] property_list_name in WITH clause
                    if (VersionUtils.IsSql11OrLater(sp.TargetServerVersionInternal, this.ServerVersion))
                    {

                        String searchPropertyListName = this.Properties.Get("SearchPropertyListName").Value as String;


                        if (searchPropertyListName != null && searchPropertyListName.Length > 0)
                        {
                            if (withClause.Length > 0)
                            {
                                withClause.Append(Globals.commaspace);
                            }
                            withClause.Append(SearchPropertyListConstants.SearchPropertyList + " = ");
                            withClause.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(searchPropertyListName));

                        }

                    }

                    if (withClause.Length > 0)
                    {
                        sb.Append("WITH (");
                        sb.Append(withClause);
                        sb.Append(")");
                        sb.Append(sp.NewLine);
                    }
                }
                sb.Append(sp.NewLine);
                queries.Add(sb.ToString());
            }
            // Target version < 9
            else
            {
                Database db = (Database)parentObj.ParentColl.ParentInstance;

                string indexName = this.GetPropValue("UniqueIndexName") as string;
                if (null == indexName || indexName.Length <= 0)
                {
                    throw new PropertyNotSetException("UniqueIndexName");
                }

                // Create index
                string catalogName = string.Empty;
                if (null != (property1 = this.Properties.Get("CatalogName")).Value)
                {
                    catalogName = (string)property1.Value;
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'create', @keyname=N'{1}'",
                    SqlString(tableName), SqlString(indexName));
                if (catalogName.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", @ftcat=N'{0}'", SqlString(catalogName));
                }

                queries.Add(sb.ToString());
                sb.Length = 0;

                // Add columns
                foreach (FullTextIndexColumn col in IndexedColumns)
                {
                    if (!col.IgnoreForScripting)
                    {
                        col.ScriptCreateFullTextIndexColumn(queries, sp);
                    }
                }

                // Change tracking
                if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version80)
                {
                    if (null != (property1 = this.Properties.Get("ChangeTracking")).Value)
                    {
                        ChangeTracking ct = (ChangeTracking)property1.Value;
                        switch (ct)
                        {
                            case ChangeTracking.Automatic:
                                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                        "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'start_change_tracking'",
                                        SqlString(tableName)));
                                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                        "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'start_background_updateindex'",
                                        SqlString(tableName)));
                                break;
                            case ChangeTracking.Manual:
                                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                        "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'start_change_tracking'",
                                        SqlString(tableName)));
                                break;
                            case ChangeTracking.Off:
                                break;
                            default:
                                throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("Change Tracking"));
                        }
                    }
                }
            }
        }

        private void ScriptAlterFullTextIndex(StringCollection queries, ScriptingPreferences sp)
        {
            TableViewBase table = this.Parent;
            Property property1;

            // Target version >= 9
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                // ALTER FULLTEXT INDEX ON <tablename>
                // SET CHANGE_TRACKING
                if (null != (property1 = this.Properties.Get("ChangeTracking")).Value && property1.Dirty)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                            "ALTER FULLTEXT INDEX ON {0} SET CHANGE_TRACKING ",
                            table.FullQualifiedName);
                    if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100)
                    {
                        sb.Append("= ");
                    }
                    ChangeTracking ct = (ChangeTracking)property1.Value;
                    switch (ct)
                    {
                        case ChangeTracking.Automatic: sb.Append("AUTO"); break;
                        case ChangeTracking.Manual: sb.Append("MANUAL"); break;
                        case ChangeTracking.Off:
                            sb.Append("OFF");
                            break;
                        default:
                            throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("Change Tracking"));
                    }
                    queries.Add(sb.ToString());
                }
            }
            // Target version < 9
            else
            {
                if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version80)
                {
                    if (null != (property1 = this.Properties.Get("ChangeTracking")).Value && property1.Dirty)
                    {
                        ChangeTracking ctNew = (ChangeTracking)property1.Value;
                        ChangeTracking ctOld = (ChangeTracking)GetRealValue(property1, oldChangeTrackingValue);

                        // need to take into account the old values and to script the proper state change
                        if (ctNew != ctOld)
                        {
                            StringCollection actions = new StringCollection();
                            if (ctOld == ChangeTracking.Automatic && ctNew == ChangeTracking.Manual)
                            {
                                actions.Add("stop_background_updateindex");
                            }
                            else if (ctOld == ChangeTracking.Automatic && ctNew == ChangeTracking.Off)
                            {
                                actions.Add("stop_background_updateindex");
                                actions.Add("stop_change_tracking");
                            }
                            else if (ctOld == ChangeTracking.Manual && ctNew == ChangeTracking.Automatic)
                            {
                                actions.Add("start_background_updateindex");
                            }
                            else if (ctOld == ChangeTracking.Manual && ctNew == ChangeTracking.Off)
                            {
                                actions.Add("stop_change_tracking");
                            }
                            else if (ctOld == ChangeTracking.Off && ctNew == ChangeTracking.Automatic)
                            {
                                actions.Add("start_change_tracking");
                                actions.Add("start_background_updateindex");
                            }
                            else if (ctOld == ChangeTracking.Off && ctNew == ChangeTracking.Manual)
                            {
                                actions.Add("start_change_tracking");
                            }

                            foreach (string action in actions)
                            {
                                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                        "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'{1}'",
                                        SqlString(table.FormatFullNameForScripting(sp)), action));
                            }
                        }
                    }
                }
            }

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100 && ServerVersion.Major >= 10)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                Property propertyStopListOption = this.Properties.Get("StopListOption");
                Property propertyStopListName = this.Properties.Get("StopListName");

                String strStopListName = this.Properties.Get("StopListName").Value as String;
                StopListOption slOption = (StopListOption)propertyStopListOption.Value;

                // SET STOPLIST {OFF | SYSTEM | <stoplist_name> }
                if (null != propertyStopListOption.Value && propertyStopListOption.Dirty)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                    "ALTER FULLTEXT INDEX ON {0} SET STOPLIST = ",
                    table.FullQualifiedName);

                    switch (slOption)
                    {
                        case StopListOption.Off:
                            sb.Append("OFF");
                            if (null != strStopListName && propertyStopListName.Dirty && strStopListName.Length > 0)
                            {
                                throw new SmoException(ExceptionTemplates.PropertyNotValidException("StopListName", "StopListOption", "OFF"));
                            }
                            break;

                        case StopListOption.System:
                            sb.Append("SYSTEM");
                            if (null != strStopListName && propertyStopListName.Dirty && strStopListName.Length > 0)
                            {
                                throw new SmoException(ExceptionTemplates.PropertyNotValidException("StopListName", "StopListOption", "SYSTEM"));
                            }
                            break;

                        case StopListOption.Name:
                            if ((null != strStopListName) && (strStopListName.Length > 0))
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(strStopListName));
                            }
                            else
                            {
                                throw new PropertyNotSetException("StopListName");
                            }
                            break;
                        default:
                            throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("StopList NAme"));
                    }
                    sb.Append(sp.NewLine);

                    // WITH NO POPULATION
                    if (this.noPopulation)
                    {
                        sb.Append("WITH NO POPULATION");
                        sb.Append(sp.NewLine);
                    }
                }
                else if (null != strStopListName && propertyStopListName.Dirty && strStopListName.Length > 0)
                {
                    if (slOption == StopListOption.Off)
                    {
                        throw new SmoException(ExceptionTemplates.PropertyNotValidException("StopListName", "StopListOption", "OFF"));
                    }
                    else if (slOption == StopListOption.System)
                    {
                        throw new SmoException(ExceptionTemplates.PropertyNotValidException("StopListName", "StopListOption", "SYSTEM"));
                    }
                    else if (slOption == StopListOption.Name)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                        "ALTER FULLTEXT INDEX ON {0} SET STOPLIST = {1}",
                        table.FullQualifiedName, MakeSqlBraket(strStopListName));
                        sb.Append(sp.NewLine);

                        // WITH NO POPULATION
                        if (this.noPopulation)
                        {
                            sb.Append("WITH NO POPULATION");
                            sb.Append(sp.NewLine);
                        }
                    }
                }

                if (sb.Length > 0)
                {
                    queries.Add(sb.ToString());
                }
            }

            if (VersionUtils.IsSql11OrLater(sp.TargetServerVersionInternal, this.ServerVersion))
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                Property searchPropertyListProperty = this.Properties.Get("SearchPropertyListName");
                String searchPropertyListName = searchPropertyListProperty.Value as String;

                // SET SEARCH PROPERTY LIST { OFF | property_list_name }
                if (searchPropertyListProperty.Dirty && searchPropertyListName != null)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                    "ALTER FULLTEXT INDEX ON {0} SET {1} = ",
                    table.FullQualifiedName, SearchPropertyListConstants.SearchPropertyList);


                    if (searchPropertyListName.Length == 0)
                    {
                        sb.Append("OFF");
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(searchPropertyListName));
                    }

                    sb.Append(sp.NewLine);

                    // WITH NO POPULATION
                    if (this.noPopulation)
                    {
                        sb.Append("WITH NO POPULATION");
                        sb.Append(sp.NewLine);
                    }

                    queries.Add(sb.ToString());
                }
            }
        }

        public void Drop()
        {
           if (!this.ExecutionManager.Recording)
            {
                TableViewBase tvb = this.Parent as TableViewBase;
                tvb.DropFullTextIndexRef();
            }
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            bool fIsInvalidForDrop = (this.State == SqlSmoState.Dropped) ||
                                         (this.State == SqlSmoState.Creating && !this.ExecutionManager.Recording) ||
                                         (this.State == SqlSmoState.Pending && !this.IsDesignMode);

            if (!this.ExecutionManager.Recording && !fIsInvalidForDrop)
            {
                TableViewBase tvb = this.Parent as TableViewBase;
                tvb.DropFullTextIndexRef();
            }
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, string.Empty, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            TableViewBase table = this.Parent;
            string tableName = table.FormatFullNameForScripting(sp);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_INDEX, SqlString(tableName), "<>");
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_INDEX90, string.Empty, SqlString(tableName));
                }

                sb.Append(sp.NewLine);
            }

            // Target version >= 9
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP FULLTEXT INDEX ON {0}", tableName);
            }

            // Target version < 9
            else
            {
                Database db = (Database)table.ParentColl.ParentInstance;
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_table @tabname={0}, @action=N'drop'",
                    table.FormatFullNameForScripting(sp, false));
            }
            sb.Append(sp.NewLine);

            dropQuery.Add(sb.ToString());
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting options
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            bool bWithScript = action != PropagateAction.Create;
            return new PropagateInfo[] {
                new PropagateInfo(IndexedColumns, bWithScript)
                    };
        }


        public void Disable()
        {
            try
            {
                StringCollection statements = new StringCollection();
                ScriptDisable(statements);
                this.ExecutionManager.ExecuteNonQuery(statements);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Disable, this, e);
            }
        }

        internal void ScriptDisable(StringCollection queries)
        {
            ScriptDisable(queries, null);
        }

        internal void ScriptDisable(StringCollection queries, ScriptingPreferences sp)
        {
            TableViewBase table = this.Parent;
            Database db = (Database)table.ParentColl.ParentInstance;

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);


            if (sp != null && sp.IncludeScripts.DatabaseContext)
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture, "USE [{0}]", SqlBraket(db.Name)));
            }

            if (sp != null && sp.IncludeScripts.ExistenceCheck)
            {
                string tableName = table.FormatFullNameForScripting(sp);

                if (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_INDEX, SqlString(tableName), "<>");
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_INDEX90, string.Empty, SqlString(tableName));
                }

                sb.Append(sp.NewLine);
            }

            if (sp != null && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "ALTER FULLTEXT INDEX ON {0} DISABLE", table.FullQualifiedName);
            }
            else
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'deactivate'",
                    SqlString(table.FullQualifiedName));
            }

            queries.Add(sb.ToString());

        }

        public void Enable()
        {
            try
            {
                StringCollection statements = new StringCollection();

                TableViewBase table = this.Parent;
                Database db = (Database)table.ParentColl.ParentInstance;

                if (ServerVersion.Major >= 9)
                {
                    statements.Add(string.Format(SmoApplication.DefaultCulture, "USE [{0}]", SqlBraket(db.Name)));
                    statements.Add(string.Format(SmoApplication.DefaultCulture,
                        "ALTER FULLTEXT INDEX ON {0} ENABLE", table.FullQualifiedName));
                }
                else
                {
                    statements.Add(string.Format(SmoApplication.DefaultCulture, "USE [{0}]", SqlBraket(db.Name)));
                    statements.Add(string.Format(SmoApplication.DefaultCulture,
                        "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'activate'",
                        SqlString(table.FullQualifiedName)));
                }
                this.ExecutionManager.ExecuteNonQuery(statements);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Enable, this, e);
            }
        }

        public void StartPopulation(IndexPopulationAction action)
        {
            try
            {
                if (ServerVersion.Major >= 8)
                {
                    StringCollection statements = new StringCollection();

                    TableViewBase table = this.Parent;
                    Database db = (Database)table.ParentColl.ParentInstance;

                    if (ServerVersion.Major >= 9)
                    {
                        statements.Add(string.Format(SmoApplication.DefaultCulture, "USE [{0}]", SqlBraket(db.Name)));
                        string actionString = string.Empty;
                        switch (action)
                        {
                            case IndexPopulationAction.Full: actionString = "FULL"; break;
                            case IndexPopulationAction.Incremental: actionString = "INCREMENTAL"; break;
                            default: actionString = "UPDATE"; break;
                        }
                        statements.Add(string.Format(SmoApplication.DefaultCulture,
                            "ALTER FULLTEXT INDEX ON {0} START {1} POPULATION",
                            table.FullQualifiedName, actionString));
                    }
                    else
                    {
                        statements.Add(string.Format(SmoApplication.DefaultCulture, "USE [{0}]", SqlBraket(db.Name)));
                        string actionString = string.Empty;
                        switch (action)
                        {
                            case IndexPopulationAction.Full: actionString = "start_full"; break;
                            case IndexPopulationAction.Incremental: actionString = "start_incremental"; break;
                            default: actionString = "update_index"; break;
                        }
                        statements.Add(string.Format(SmoApplication.DefaultCulture,
                            "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'{1}'",
                            SqlString(table.FullQualifiedName), actionString));
                    }

                    this.ExecutionManager.ExecuteNonQuery(statements);
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.StartPopulation, this, e);
            }
        }

        public void StopPopulation()
        {
            try
            {
                if (ServerVersion.Major >= 8)
                {
                    StringCollection statements = new StringCollection();

                    TableViewBase table = this.Parent;
                    Database db = (Database)table.ParentColl.ParentInstance;

                    if (ServerVersion.Major >= 9)
                    {
                        statements.Add(string.Format(SmoApplication.DefaultCulture, "USE [{0}]", SqlBraket(db.Name)));
                        statements.Add(string.Format(SmoApplication.DefaultCulture,
                            "ALTER FULLTEXT INDEX ON {0} STOP POPULATION", table.FullQualifiedName));
                    }
                    else
                    {
                        statements.Add(string.Format(SmoApplication.DefaultCulture, "USE [{0}]", SqlBraket(db.Name)));
                        statements.Add(string.Format(SmoApplication.DefaultCulture,
                            "EXEC dbo.sp_fulltext_table @tabname=N'{0}', @action=N'stop'",
                            SqlString(table.FullQualifiedName)));
                    }

                    this.ExecutionManager.ExecuteNonQuery(statements);
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.StopPopulation, this, e);
            }
        }

        // old ChangeTracking value
        internal object oldChangeTrackingValue = null;

        /// <summary>
        /// Refresh the object.
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            oldChangeTrackingValue = null;
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "ChangeTracking" && !prop.Dirty)
            {
                oldChangeTrackingValue = prop.Value;
            }
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
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            string[] fields = {
                        "IsEnabled",
                        "UniqueIndexName",
                        "CatalogName",
                        "ChangeTracking",
                        "FilegroupName",
                        "StopListName",
                        "StopListOption",
                        "SearchPropertyListName"};
            List<string> list = GetSupportedScriptFields(typeof(FullTextIndex.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();

        }
    }

}



